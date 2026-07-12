using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonSlash
{
    public sealed class DungeonGenerator
    {
        private readonly DungeonGenerationSettings settings;
        public DungeonGenerator(DungeonGenerationSettings generationSettings) => settings = generationSettings;

        public DungeonRunState Generate(int seed)
        {
            for (var attempt = 0; attempt < settings.maxGenerationAttempts; attempt++)
            {
                var random = new Random(seed + attempt * 7919);
                var graph = CreateGraph(random);
                if (TryAssignRooms(graph, random)) return new DungeonRunState(seed, graph);
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

        private bool TryAssignRooms(DungeonGraph graph, Random random)
        {
            var start = graph.StartRoom;
            var distances = graph.DistancesFrom(start);
            if (distances.Count != graph.Rooms.Count) return false;
            foreach (var room in graph.Rooms) { room.Type = RoomEncounterType.Empty; room.IsMajorRoom = false; room.IsRevealed = false; }
            start.Type = RoomEncounterType.Start; start.IsRevealed = true;

            var boss = SelectAtDistance(graph, distances, settings.minimumBossDistance, random);
            if (boss == null) return false;
            AssignMajor(boss, RoomEncounterType.Boss);
            var unassigned = graph.Rooms.Where(room => room != start && room != boss).ToList();

            for (var i = 0; i < settings.majorRewardRoomCount; i++)
            {
                var candidate = unassigned.Where(room => distances[room.RoomId] >= settings.minimumMajorRewardDistance).OrderBy(_ => random.Next()).FirstOrDefault();
                if (candidate == null) return false;
                AssignMajor(candidate, RoomEncounterType.MajorReward); unassigned.Remove(candidate);
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
            return true;
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
                   graph.Rooms.Count(room => room.Type == RoomEncounterType.MajorReward) >= settings.majorRewardRoomCount;
        }
    }
}
