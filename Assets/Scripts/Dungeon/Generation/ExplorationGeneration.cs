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
            if (settings == null || random.NextDouble() >= settings.annexChance) return;
            var count = random.NextDouble() < settings.secondAnnexChance ? 2 : 1;
            for (var index = 0; index < count; index++)
            {
                var distances = graph.DistancesFrom(graph.StartRoom);
                var accessible = BeforeBoss(graph);
                var candidates = graph.Rooms.Where(r => !r.IsOptionalAnnex && r != graph.StartRoom
                    && accessible.Contains(r.RoomId)
                    && r.Type != RoomEncounterType.Boss && r.Type != RoomEncounterType.Goddess && r.Type != RoomEncounterType.Camp
                    && distances[r.RoomId] >= 4 && r.Connections.Count < 3
                    && !graph.Passages.Any(p => p.Origin == r)).OrderBy(_ => random.Next()).ToArray();
                foreach (var origin in candidates)
                {
                    var free = DirectionUtility.Offsets.Select(o => origin.Position + o).Where(p => !graph.HasRoom(p)
                        && DirectionUtility.Offsets.All(o => { var adjacent = graph.At(p + o); return adjacent == null || adjacent == origin; })).ToArray();
                    if (free.Length == 0) continue;
                    var hidden = random.NextDouble() < settings.concealedEntranceChance;
                    var locked = random.NextDouble() < settings.lockedEntranceChance;
                    if (graph.Passages.Any(p => p.Locked)) locked = false;
                    if (!hidden && !locked) hidden = true;
                    var nearby = graph.DistancesFrom(origin);
                    var keyRoom = graph.Rooms.Where(r => !r.IsOptionalAnnex && r != origin && r.KeyTokens.Count == 0
                        && accessible.Contains(r.RoomId)
                        && r.Type != RoomEncounterType.Boss && nearby[r.RoomId] <= settings.maximumKeyDistance)
                        .OrderBy(_ => random.Next()).FirstOrDefault();
                    if (locked && keyRoom == null) continue;
                    var annex = graph.AddRoom(free[random.Next(free.Length)]);
                    annex.IsOptionalAnnex = true; annex.ObjectCorner = random.Next(4);
                    annex.ObjectFacing = (FacingDirection)random.Next(4);
                    var roll = random.NextDouble();
                    annex.Type = roll < settings.annexCombatChance ? RoomEncounterType.Combat
                        : roll < settings.annexCombatChance + settings.annexEventChance ? RoomEncounterType.Event : RoomEncounterType.Chest;
                    // Hidden chests have their own low ambush roll; ordinary B2/B3 chests retain existing tuning.
                    annex.ChestContent = floor >= 2 && random.NextDouble() < settings.annexCombatChance ? ChestContent.Mimic
                        : random.NextDouble() < .55 ? ChestContent.Gold : ChestContent.Relic;
                    graph.Connect(origin, annex);
                    annex.ObjectWall = ExplorationPlacement.WallFor(graph, annex, random.Next());
                    graph.Passages.Add(new DungeonPassage(origin, annex, hidden, locked, random.Next()));
                    if (locked) keyRoom.KeyTokens.Add(annex.RoomId);
                    break;
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
