using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonSlash
{
    public sealed class DungeonGenerator
    {
        private readonly DungeonGenerationSettings settings;
        public DungeonGenerator(DungeonGenerationSettings generationSettings) => settings = generationSettings;

        public DungeonRunState Generate(int seed, int floor = 1)
        {
            for (var attempt = 0; attempt < settings.maxGenerationAttempts; attempt++)
            {
                var random = new Random(seed + attempt * 7919);
                var graph = CreateGraph(random);
                if (TryAssignRooms(graph, random, floor)) return new DungeonRunState(seed, graph, floor);
            }
            throw new InvalidOperationException("Unable to create a dungeon that satisfies the configured distance constraints.");
        }

        private DungeonGraph CreateGraph(Random random)
        {
            var graph = new DungeonGraph();
            graph.AddRoom(new DungeonPosition(0, 0));
            var targetCount = random.Next(settings.minRooms, settings.maxRooms + 1);
            while (graph.Rooms.Count < targetCount)
            {
                var candidates = graph.Rooms.OrderBy(_ => random.Next()).ToList();
                var placed = false;
                foreach (var parent in candidates)
                {
                    var offsetStart = random.Next(4);
                    for (var i = 0; i < 4; i++)
                    {
                        var position = parent.Position + DirectionUtility.Offsets[(offsetStart + i) % 4];
                        if (graph.HasRoom(position)) continue;
                        graph.Connect(parent, graph.AddRoom(position));
                        placed = true;
                        break;
                    }
                    if (placed) break;
                }
            }

            foreach (var room in graph.Rooms.ToList())
            {
                foreach (var offset in DirectionUtility.Offsets)
                {
                    if (random.NextDouble() <= settings.loopChance)
                        graph.Connect(room, graph.At(room.Position + offset));
                }
            }
            return graph;
        }

        private bool TryAssignRooms(DungeonGraph graph, Random random, int floor)
        {
            var start = graph.StartRoom;
            var distances = graph.DistancesFrom(start);
            if (distances.Count != graph.Rooms.Count) return false;
            foreach (var room in graph.Rooms) { room.Type = RoomEncounterType.Empty; room.IsMajorRoom = false; room.IsRevealed = false; room.ChestContent = ChestContent.Relic; }
            start.Type = RoomEncounterType.Start; start.IsRevealed = true;

            var boss = SelectAtDistance(graph, distances, settings.minimumBossDistance, random);
            if (boss == null) return false;
            AssignMajor(boss, RoomEncounterType.Boss);
            var unassigned = graph.Rooms.Where(room => room != start && room != boss).ToList();

            // The former major-gold room is now an ordinary-looking chest at a prominent location.
            // Floor one guarantees one gold chest here, while later floors roll a hidden chest outcome.
            for (var i = 0; i < settings.chestRoomCount; i++)
            {
                var candidate = unassigned.Where(room => distances[room.RoomId] >= settings.minimumChestDistance).OrderBy(_ => random.Next()).FirstOrDefault();
                if (candidate == null) return false;
                AssignMajor(candidate, RoomEncounterType.Chest);
                candidate.ChestContent = floor == 1 ? ChestContent.Gold : RollChestContent(random);
                unassigned.Remove(candidate);
            }
            for (var i = 0; i < settings.eliteRoomCount; i++)
            {
                var candidate = unassigned.OrderByDescending(room => distances[room.RoomId]).ThenBy(_ => random.Next()).FirstOrDefault();
                if (candidate == null) return false;
                AssignMajor(candidate, RoomEncounterType.Elite); unassigned.Remove(candidate);
            }

            AssignMinimum(unassigned, RoomEncounterType.Reward, settings.minimumRewardRooms, random);
            AssignMinimum(unassigned, RoomEncounterType.Fountain, settings.minimumFountainRooms, random);
            AssignMinimum(unassigned, RoomEncounterType.Shop, settings.minimumShopRooms, random);
            AssignMinimum(unassigned, RoomEncounterType.Goddess, settings.minimumGoddessRooms, random);
            var combatCount = Math.Max(1, (int)Math.Ceiling(graph.Rooms.Count * (settings.combatRoomRatioMin + settings.combatRoomRatioMax) * .5f));
            AssignMinimum(unassigned, RoomEncounterType.Combat, Math.Min(combatCount, unassigned.Count), random);
            foreach (var room in unassigned) room.Type = random.NextDouble() < .55 ? RoomEncounterType.Combat : RoomEncounterType.Empty;
            AddFloorEventVariants(graph, random, floor);
            return true;
        }

        private static void AddFloorEventVariants(DungeonGraph graph, Random random, int floor)
        {
            // Every floor receives a second visible chest. Floor one therefore has exactly two: gold and relic.
            var candidates = graph.Rooms.Where(room => !room.IsMajorRoom && (room.Type is RoomEncounterType.Reward or RoomEncounterType.Empty)).OrderBy(_ => random.Next()).ToList();
            if (candidates.Count > 0)
            {
                candidates[0].Type = RoomEncounterType.Chest;
                candidates[0].ChestContent = floor == 1 ? ChestContent.Relic : RollChestContent(random);
                candidates[0].IsRevealed = true;
            }
            if (floor < 2) return;
            if (random.NextDouble() > .65) return;
            var toxicCandidate = graph.Rooms.Where(room => !room.IsMajorRoom && room.Type == RoomEncounterType.Empty).OrderBy(_ => random.Next()).FirstOrDefault();
            if (toxicCandidate != null) toxicCandidate.Type = RoomEncounterType.PoisonFountain;
        }

        private static ChestContent RollChestContent(Random random)
        {
            var roll = random.NextDouble();
            // Across the two floor-2+ chests, this keeps the old mix close to one gold chest and
            // one relic-or-mimic chest, without revealing which chest carries which result.
            return roll < .5d ? ChestContent.Gold : roll < .825d ? ChestContent.Relic : ChestContent.Mimic;
        }

        private static DungeonRoom SelectAtDistance(DungeonGraph graph, IReadOnlyDictionary<int, int> distances, int minDistance, Random random) => graph.Rooms.Where(room => distances[room.RoomId] >= minDistance).OrderBy(_ => random.Next()).FirstOrDefault();
        private static void AssignMajor(DungeonRoom room, RoomEncounterType type) { room.Type = type; room.IsMajorRoom = true; room.IsRevealed = true; }
        private static void AssignMinimum(List<DungeonRoom> rooms, RoomEncounterType type, int count, Random random)
        {
            for (var i = 0; i < count && rooms.Count > 0; i++)
            {
                var index = random.Next(rooms.Count); rooms[index].Type = type; rooms.RemoveAt(index);
            }
        }
    }

    public static class DungeonGraphValidator
    {
        public static bool HasExpectedStructure(DungeonRunState state, DungeonGenerationSettings settings)
        {
            var graph = state.Graph;
            var distances = graph.DistancesFrom(graph.StartRoom);
            return graph.Rooms.Count >= settings.minRooms && graph.Rooms.Count <= settings.maxRooms &&
                   distances.Count == graph.Rooms.Count &&
                   graph.Rooms.Any(room => room.Type == RoomEncounterType.Boss && distances[room.RoomId] >= settings.minimumBossDistance) &&
                   graph.Rooms.Count(room => room.Type == RoomEncounterType.Chest) >= settings.chestRoomCount + 1;
        }
    }
}
