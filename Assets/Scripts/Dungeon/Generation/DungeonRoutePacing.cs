using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonSlash
{
    // Allocate an existing floor budget across actual walkable paths. Counts and rewards do
    // not change. The search uses a seeded random start; it does not stamp a fixed itinerary.
    public static class DungeonRoutePacing
    {
        public static bool IsThreat(RoomEncounterType type) => type is RoomEncounterType.Combat or RoomEncounterType.Elite or RoomEncounterType.Boss;
        public static bool IsOpportunity(RoomEncounterType type) => type is RoomEncounterType.Camp or RoomEncounterType.Chest
            or RoomEncounterType.Reward or RoomEncounterType.MajorReward or RoomEncounterType.Fountain
            or RoomEncounterType.PoisonFountain or RoomEncounterType.Shop or RoomEncounterType.Goddess or RoomEncounterType.Event;

        public static List<int[]> Windows(DungeonGraph graph)
        {
            var result = new List<int[]>(); var seen = new HashSet<string>(); var path = new List<int>();
            foreach (var room in graph.Rooms.Where(r => !r.IsOptionalAnnex)) Visit(room);
            return result;
            void Visit(DungeonRoom room)
            {
                path.Add(room.RoomId);
                if (path.Count >= 3)
                {
                    var key = string.Join(",", path[0] < path[path.Count - 1] ? path : path.AsEnumerable().Reverse());
                    if (seen.Add(key)) result.Add(path.ToArray());
                }
                if (path.Count < 6) foreach (var next in graph.Neighbors(room))
                    if (!next.IsOptionalAnnex && !path.Contains(next.RoomId)) Visit(next);
                path.RemoveAt(path.Count - 1);
            }
        }

        public static int HardViolations(DungeonGraph graph)
        {
            return Windows(graph).Count(p => p.Length == 5 && p.All(id => IsThreat(graph.ById(id).Type))
                || p.Length == 4 && p.All(id => IsOpportunity(graph.ById(id).Type))
                || p.Length == 6 && !p.Any(id => IsOpportunity(graph.ById(id).Type))
                || p.Length == 6 && !p.Any(id => IsThreat(graph.ById(id).Type) || graph.ById(id).Type == RoomEncounterType.Start));
        }

        public static bool Distribute(DungeonGraph graph, DungeonGenerationSettings settings, Random random, int floor)
        {
            var rooms = graph.Rooms.OrderBy(r => r.RoomId).ToArray();
            var distances = graph.DistancesFrom(graph.StartRoom);
            var between = rooms.Select(graph.DistancesFrom).ToArray();
            var windows = Windows(graph);
            var mutable = rooms.Where(r => r != graph.StartRoom && r.Type != RoomEncounterType.Boss && !r.IsExplorationAlcove
                && !(r.Type==RoomEncounterType.Empty&&r.Connections.Count==1)
                && (floor != 1 || distances[r.RoomId] > settings.firstActOpeningCombats)).ToArray();
            var best = rooms.Select(r => (r.Type, r.ChestContent, r.IsMajorRoom, r.IsRevealed)).ToArray();
            var score = Score(); var bestScore = score;
            // Small graphs: a bounded annealing pass avoids clustering without ordering
            // every second room into the same predictable combat/reward alternation.
            for (var step = 0; step < 1600 && mutable.Length > 1; step++)
            {
                var a = mutable[random.Next(mutable.Length)]; var b = mutable[random.Next(mutable.Length)];
                if (a == b || a.Type == b.Type || !Fits(a, b.Type) || !Fits(b, a.Type)) continue;
                Swap(a, b); var next = Score();
                var temperature = 9d * (1d - step / 1600d) + .15d;
                if (next <= score || random.NextDouble() < Math.Exp((score - next) / temperature)) score = next;
                else Swap(a, b);
                if (score < bestScore)
                {
                    bestScore = score;
                    for (var i = 0; i < rooms.Length; i++) best[i] = (rooms[i].Type, rooms[i].ChestContent, rooms[i].IsMajorRoom, rooms[i].IsRevealed);
                }
            }
            for (var i = 0; i < rooms.Length; i++) (rooms[i].Type, rooms[i].ChestContent, rooms[i].IsMajorRoom, rooms[i].IsRevealed) = best[i];
            return HardViolations(graph) == 0 && FacilitiesValid();

            bool Fits(DungeonRoom room, RoomEncounterType type)
            {
                var depth = distances[room.RoomId];
                if (type == RoomEncounterType.Camp) return room.Connections.Count == 1 && depth >= (floor == 1 ? settings.firstActCampDistance : 1);
                if (type == RoomEncounterType.Chest) return depth >= settings.minimumChestDistance;
                if (type == RoomEncounterType.Elite) return depth >= settings.minimumChestDistance;
                if (type == RoomEncounterType.Goddess && room.Connections.Count == 4) return false;
                return !IsOpportunity(type) || floor != 1 || depth >= settings.firstActServiceDistance;
            }
            bool FacilitiesValid()
            {
                foreach (var a in rooms) if (DungeonGenerator.FacilityFamily(a.Type) is RoomEncounterType.Shop or RoomEncounterType.Goddess or RoomEncounterType.Fountain)
                    foreach (var b in rooms) if (b.RoomId > a.RoomId && DungeonGenerator.FacilityFamily(a.Type) == DungeonGenerator.FacilityFamily(b.Type)
                        && between[a.RoomId][b.RoomId] < settings.minimumRepeatedFacilityDistance) return false;
                return true;
            }
            double Score()
            {
                double value = 0;
                foreach (var p in windows)
                {
                    int threats = 0, opportunities = 0;
                    foreach (var id in p) { if (IsThreat(rooms[id].Type)) threats++; if (IsOpportunity(rooms[id].Type)) opportunities++; }
                    if (p.Length == 5 && threats == 5) value += 120;
                    if (p.Length == 4 && opportunities == 4) value += 120;
                    if (p.Length == 6 && threats == 0 && !p.Contains(graph.StartRoom.RoomId)) value += 120;
                    if (p.Length == 3 && opportunities == 3) value += 10;
                    if (p.Length == 4 && threats == 4) value += 3;
                    if (p.Length == 6 && opportunities == 0) value += 120;
                }
                foreach (var a in rooms) if (IsOpportunity(a.Type)) foreach (var b in rooms)
                {
                    if (b.RoomId <= a.RoomId || !IsOpportunity(b.Type)) continue;
                    var distance = between[a.RoomId][b.RoomId];
                    if (distance == 1) value += 2;
                    if (DungeonGenerator.FacilityFamily(a.Type) == DungeonGenerator.FacilityFamily(b.Type)
                        && DungeonGenerator.FacilityFamily(a.Type) is RoomEncounterType.Shop or RoomEncounterType.Goddess or RoomEncounterType.Fountain
                        && distance < settings.minimumRepeatedFacilityDistance) value += 200;
                }
                return value;
            }
        }
        private static void Swap(DungeonRoom a, DungeonRoom b)
        {
            (a.Type, b.Type) = (b.Type, a.Type); (a.ChestContent, b.ChestContent) = (b.ChestContent, a.ChestContent);
            (a.IsMajorRoom, b.IsMajorRoom) = (b.IsMajorRoom, a.IsMajorRoom); (a.IsRevealed, b.IsRevealed) = (b.IsRevealed, a.IsRevealed);
        }
    }
}
