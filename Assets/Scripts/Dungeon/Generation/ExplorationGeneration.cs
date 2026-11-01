using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonSlash
{
    public static class ExplorationGeneration
    {
        public static void Populate(DungeonGraph graph, ExplorationSettings settings, int seed, int floor)
        {
            var random = new Random(unchecked(seed * 397 ^ floor * 104729 ^ 0x41935));
            foreach (var room in graph.Rooms)
            {
                room.ObjectCorner = random.Next(4);
                room.ObjectFacing = (FacingDirection)random.Next(4);
                room.ObjectWall = ExplorationPlacement.WallFor(graph, room, random.Next());
            }
            if (settings == null) return;
            // Two independent rolls: a floor can contain neither, either, or both.
            // Secret origins remain genuinely empty dead ends, never key/prop rooms.
            if (random.NextDouble() < settings.concealedEntranceChance) AddBranch(true);
            if (random.NextDouble() < settings.lockedEntranceChance) AddBranch(false);

            void AddBranch(bool hidden)
            {
                var distances = graph.DistancesFrom(graph.StartRoom);
                var accessible = BeforeBoss(graph);
                var candidates = graph.Rooms.Where(r => !r.IsOptionalAnnex && r != graph.StartRoom
                    && accessible.Contains(r.RoomId) && distances[r.RoomId] >= 2
                    && (hidden ? r.Type == RoomEncounterType.Empty && r.Connections.Count == 1 && r.KeyTokens.Count == 0
                        : r.Type != RoomEncounterType.Boss && r.Type != RoomEncounterType.Goddess && r.Type != RoomEncounterType.Camp && r.Connections.Count < 3)
                    && !graph.Passages.Any(p => p.Origin == r)).OrderBy(_ => random.Next()).ToArray();
                foreach (var origin in candidates)
                {
                    var ordinaryCount = random.Next(3);
                    var positions = new List<DungeonPosition>();
                    if (!FindPath(origin.Position, ordinaryCount + 1)) continue;
                    var nearby = graph.DistancesFrom(origin);
                    var keyRoom = hidden ? null : graph.Rooms.Where(r => !r.IsOptionalAnnex && r != origin && r.KeyTokens.Count == 0
                        && accessible.Contains(r.RoomId) && r.Type != RoomEncounterType.Boss
                        && !graph.Passages.Any(p => p.Concealed && p.Origin == r)
                        && nearby[r.RoomId] <= settings.maximumKeyDistance).OrderBy(_ => random.Next()).FirstOrDefault();
                    if (!hidden && keyRoom == null) continue;
                    var previous = origin; DungeonRoom entrance = null;
                    for (var i = 0; i < positions.Count; i++)
                    {
                        var annex = graph.AddRoom(positions[i]);
                        annex.IsOptionalAnnex = true; annex.ObjectCorner = random.Next(4); annex.ObjectFacing = (FacingDirection)random.Next(4);
                        var terminal = i == positions.Count - 1;
                        annex.Type = terminal ? (random.NextDouble() < settings.annexEventChance ? RoomEncounterType.Event : RoomEncounterType.Chest)
                            : random.NextDouble() < settings.annexCombatChance ? RoomEncounterType.Combat : RoomEncounterType.Empty;
                        annex.ChestContent = floor >= 2 && random.NextDouble() < settings.annexCombatChance ? ChestContent.Mimic
                            : random.NextDouble() < .55 ? ChestContent.Gold : ChestContent.Relic;
                        graph.Connect(previous, annex); previous = annex; entrance ??= annex;
                    }
                    graph.Passages.Add(new DungeonPassage(origin, entrance, hidden, !hidden, random.Next()));
                    if (!hidden) keyRoom.KeyTokens.Add(entrance.RoomId);
                    return;

                    bool FindPath(DungeonPosition parent, int remaining)
                    {
                        if (remaining == 0) return true;
                        foreach (var offset in DirectionUtility.Offsets.OrderBy(_ => random.Next()))
                        {
                            var p = parent + offset;
                            if (graph.HasRoom(p) || positions.Contains(p)) continue;
                            if (DirectionUtility.Offsets.Any(o =>
                            {
                                var adjacent = p + o;
                                return !adjacent.Equals(parent) && (graph.HasRoom(adjacent) || positions.Contains(adjacent));
                            })) continue;
                            positions.Add(p);
                            if (FindPath(p, remaining - 1)) return true;
                            positions.RemoveAt(positions.Count - 1);
                        }
                        return false;
                    }
                }
            }
            // Annex doors can occupy a previously intact wall of their origin room.
            foreach (var room in graph.Rooms) room.ObjectWall = ExplorationPlacement.WallFor(graph, room, random.Next());
        }
        private static HashSet<int> BeforeBoss(DungeonGraph graph)
        {
            var found = new HashSet<int> { graph.StartRoom.RoomId };
            var pending = new Queue<DungeonRoom>(); pending.Enqueue(graph.StartRoom);
            while (pending.Count > 0)
                foreach (var room in graph.Neighbors(pending.Dequeue()))
                    if (!room.IsOptionalAnnex && room.Type != RoomEncounterType.Boss && found.Add(room.RoomId)) pending.Enqueue(room);
            return found;
        }
    }
}
