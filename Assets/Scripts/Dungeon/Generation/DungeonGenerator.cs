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
                var graph = CreateGraph(random, floor);
                if (TryAssignRooms(graph, random, floor)) return new DungeonRunState(seed, graph, floor);
            }
            throw new InvalidOperationException("Unable to create a dungeon that satisfies the configured distance constraints.");
        }

        private DungeonGraph CreateGraph(Random random, int floor)
        {
            var graph = new DungeonGraph();
            graph.AddRoom(new DungeonPosition(0, 0));
            var targetCount = random.Next(settings.minRooms, settings.maxRooms + 1);
            var opening = floor == 1 ? Math.Min(settings.firstActOpeningCombats, targetCount - 2) : 0;
            var tip = graph.StartRoom;
            for (var step = 0; step < opening; step++)
            {
                var next = graph.AddRoom(new DungeonPosition(0, step + 1));
                graph.Connect(tip, next); tip = next;
            }
            while (graph.Rooms.Count < targetCount)
            {
                var candidates = graph.Rooms.OrderBy(_ => random.Next()).ToList();
                var placed = false;
                foreach (var parent in candidates)
                {
                    if (floor == 1 && !CanAddExit(graph, parent, opening)) continue;
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
                // Degree limits can leave every endpoint spatially blocked. Retry another graph instead of hanging.
                if (!placed) break;
            }

            foreach (var room in graph.Rooms.ToList())
            {
                foreach (var offset in DirectionUtility.Offsets)
                {
                    if (floor != 1)
                    {
                        if (random.NextDouble() <= settings.loopChance) graph.Connect(room, graph.At(room.Position + offset));
                        continue;
                    }
                    var other = graph.At(room.Position + offset);
                    if (other == null || room.Connections.Contains(other.RoomId)) continue;
                    if (floor == 1 && (!CanAddExit(graph, room, opening) || !CanAddExit(graph, other, opening)
                        || graph.Rooms.Count(r => r.Connections.Count >= 3) + (room.Connections.Count == 2 ? 1 : 0) + (other.Connections.Count == 2 ? 1 : 0) > settings.firstActMaximumJunctions)) continue;
                    if (random.NextDouble() <= (floor == 1 ? settings.firstActLoopChance : settings.loopChance)) graph.Connect(room, other);
                }
            }
            return graph;
        }

        private bool CanAddExit(DungeonGraph graph, DungeonRoom room, int opening)
        {
            if (opening > 0 && room.Position.X == 0 && room.Position.Y >= 0 && room.Position.Y < opening) return false;
            return room.Connections.Count < settings.firstActMaximumExits
                && (room.Connections.Count < 2 || graph.Rooms.Count(r => r.Connections.Count >= 3) < settings.firstActMaximumJunctions);
        }

        private bool TryAssignRooms(DungeonGraph graph, Random random, int floor)
        {
            var start = graph.StartRoom;
            if (graph.Rooms.Count < settings.minRooms) return false;
            var distances = graph.DistancesFrom(start);
            if (distances.Count != graph.Rooms.Count) return false;
            foreach (var room in graph.Rooms) { room.Type = RoomEncounterType.Empty; room.IsMajorRoom = false; room.IsRevealed = false; room.ChestContent = ChestContent.Relic; }
            start.Type = RoomEncounterType.Start; start.IsRevealed = true;

            var boss = SelectAtDistance(graph, distances, settings.minimumBossDistance, random);
            if (boss == null) return false;
            AssignMajor(boss, RoomEncounterType.Boss);
            var unassigned = graph.Rooms.Where(room => room != start && room != boss).ToList();
            if (floor == 1)
                foreach (var room in unassigned.Where(r => distances[r.RoomId] <= settings.firstActOpeningCombats).ToList())
                { room.Type = RoomEncounterType.Combat; unassigned.Remove(room); }

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
            // Elite routes become a meaningful map decision after B1: B1=1, B2=2, B3=3.
            for (var i = 0; i < Math.Max(1, floor); i++)
            {
                var candidate = unassigned.OrderByDescending(room => distances[room.RoomId]).ThenBy(_ => random.Next()).FirstOrDefault();
                if (candidate == null) return false;
                AssignMajor(candidate, RoomEncounterType.Elite); unassigned.Remove(candidate);
            }

            if (floor == 1)
            {
                if (!AssignDistant(RoomEncounterType.Camp, 1, settings.firstActCampDistance)
                    || !AssignDistant(RoomEncounterType.Reward, settings.minimumRewardRooms, settings.firstActServiceDistance)
                    || !AssignDistant(RoomEncounterType.Fountain, settings.minimumFountainRooms, settings.firstActServiceDistance)
                    || !AssignDistant(RoomEncounterType.Shop, 1, settings.firstActServiceDistance)
                    || !AssignDistant(RoomEncounterType.Goddess, settings.minimumGoddessRooms, settings.firstActServiceDistance)
                    || !AssignDistant(RoomEncounterType.Event, settings.eventRoomCount, settings.firstActServiceDistance)) return false;
            }
            else
            {
                AssignMinimum(unassigned, RoomEncounterType.Reward, settings.minimumRewardRooms, random);
                AssignMinimum(unassigned, RoomEncounterType.Camp, 1, random);
                AssignMinimum(unassigned, RoomEncounterType.Fountain, settings.minimumFountainRooms, random);
                AssignMinimum(unassigned, RoomEncounterType.Shop, floor, random);
                AssignMinimum(unassigned, RoomEncounterType.Goddess, settings.minimumGoddessRooms, random);
                AssignMinimum(unassigned, RoomEncounterType.Event, settings.eventRoomCount, random);
            }
            var combatCount = Math.Max(1, (int)Math.Ceiling(graph.Rooms.Count * (settings.combatRoomRatioMin + settings.combatRoomRatioMax) * .5f));
            AssignMinimum(unassigned, RoomEncounterType.Combat, Math.Min(Math.Max(0, combatCount - graph.Rooms.Count(r => r.Type == RoomEncounterType.Combat)), unassigned.Count), random);
            foreach (var room in unassigned) room.Type = random.NextDouble() < .55 ? RoomEncounterType.Combat : RoomEncounterType.Empty;
            AddFloorEventVariants(graph, random, floor);
            return true;

            bool AssignDistant(RoomEncounterType type, int count, int minimumDistance)
            {
                for (var i = 0; i < count; i++)
                {
                    var candidate = unassigned.Where(r => distances[r.RoomId] >= minimumDistance
                        && (type != RoomEncounterType.Event || !graph.Neighbors(r).Any(n => n.Type == RoomEncounterType.Event)))
                        .OrderBy(_ => random.Next()).FirstOrDefault();
                    if (candidate == null) return false;
                    candidate.Type = type; unassigned.Remove(candidate);
                }
                return true;
            }
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
