using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonSlash
{
    // A finite, legal exploration walk, not the shortest route or an allowance for
    // unlimited revisits. Side passages are returned from and the boss is entered last.
    public sealed class FatigueFloorBudget
    {
        public int ReachableRooms { get; }
        public int DirectBossSteps { get; }
        public int CoverageSteps => CoverageRoute.Count - 1;
        public int KeyReturnSteps { get; }
        public int Capacity { get; }
        public IReadOnlyList<int> CoverageRoute { get; }

        private FatigueFloorBudget(int rooms, int direct, List<int> route, float costPerStep)
        {
            ReachableRooms = rooms;
            DirectBossSteps = direct;
            CoverageRoute = route.AsReadOnly();
            KeyReturnSteps = Math.Max(0, CoverageSteps - (2 * Math.Max(0, rooms - 1) - direct));
            // Zero is death. Keep one real movement's reserve after this entire walk.
            Capacity = 10 * (int)Math.Ceiling(Math.Max(100d, (CoverageSteps + 1) * (double)Math.Max(.1f, costPerStep)) / 10d);
        }

        public static FatigueFloorBudget For(DungeonGraph graph, float costPerStep)
        {
            if (graph?.StartRoom == null) return new FatigueFloorBudget(0, 0, new List<int> { 0 }, costPerStep);
            var start = graph.StartRoom;
            var reached = graph.DistancesFrom(start);
            var boss = graph.Rooms.Where(r => reached.ContainsKey(r.RoomId) && r.Type == RoomEncounterType.Boss)
                .OrderBy(r => r.RoomId).FirstOrDefault();
            var spine = boss == null ? new List<DungeonRoom> { start } : Path(start, boss, false);
            var spineIds = new HashSet<int>(spine.Select(r => r.RoomId));
            var children = graph.Rooms.ToDictionary(r => r.RoomId, _ => new List<DungeonRoom>());
            var inTree = new HashSet<int>(spineIds);
            var queue = new Queue<DungeonRoom>(spine.Where(r => r != boss));
            // Pin the shortest boss route; attach side rooms to that route. This
            // produces a spanning tree even when the generated floor contains loops.
            while (queue.Count > 0)
            {
                var room = queue.Dequeue();
                foreach (var next in graph.Neighbors(room).OrderBy(r => r.RoomId))
                    if (next != boss && inTree.Add(next.RoomId))
                    { children[room.RoomId].Add(next); queue.Enqueue(next); }
            }
            var route = new List<int> { start.RoomId };
            var keys = new HashSet<int>(start.KeyTokens);
            var deferred = new List<(DungeonRoom origin, DungeonRoom entrance)>();
            var current = start;
            for (var i = 0; i < spine.Count; i++)
            {
                var room = spine[i];
                if (room == boss) break;
                foreach (var child in children[room.RoomId].OrderBy(r => r.IsOptionalAnnex).ThenBy(r => r.RoomId)) Visit(room, child);
                if (i + 1 < spine.Count && spine[i + 1] != boss) Walk(spine[i + 1]);
            }
            // A locked branch may be encountered before its key. Visit it after all
            // base rooms, including an actual trip back to its entrance, before boss.
            while (deferred.Count > 0)
            {
                var pending = deferred.OrderBy(p => Path(current, p.origin, true).Count).ThenBy(p => p.origin.RoomId).First();
                deferred.Remove(pending);
                var gate = graph.Passage(pending.origin, pending.entrance);
                if (gate is { Locked: true } && !keys.Contains(gate.KeyToken)) continue;
                foreach (var step in Path(current, pending.origin, true).Skip(1)) Walk(step);
                Visit(pending.origin, pending.entrance);
            }
            if (boss != null) foreach (var step in Path(current, boss, false).Skip(1)) Walk(step);
            return new FatigueFloorBudget(inTree.Count, boss == null ? 0 : spine.Count - 1, route, costPerStep);

            void Walk(DungeonRoom room)
            { route.Add(room.RoomId); current = room; keys.UnionWith(room.KeyTokens); }
            void Visit(DungeonRoom origin, DungeonRoom room)
            {
                var passage = graph.Passage(origin, room);
                if (passage is { Locked: true } && !keys.Contains(passage.KeyToken))
                {
                    // Invalid authored data must not hang the runtime budget planner.
                    if (!graph.Rooms.Any(r => !r.IsOptionalAnnex && r.KeyTokens.Contains(passage.KeyToken))) return;
                    deferred.Add((origin, room)); return;
                }
                Walk(room);
                foreach (var child in children[room.RoomId].OrderBy(r => r.IsOptionalAnnex).ThenBy(r => r.RoomId)) Visit(room, child);
                Walk(origin);
            }
            List<DungeonRoom> Path(DungeonRoom from, DungeonRoom to, bool baseOnly)
            {
                var parents = new Dictionary<int, int> { [from.RoomId] = -1 };
                var pending = new Queue<DungeonRoom>(); pending.Enqueue(from);
                while (pending.Count > 0 && !parents.ContainsKey(to.RoomId))
                {
                    var room = pending.Dequeue();
                    foreach (var next in graph.Neighbors(room).OrderBy(r => r.RoomId))
                    {
                        if ((baseOnly && (next.IsOptionalAnnex || next == boss)) || parents.ContainsKey(next.RoomId)) continue;
                        parents[next.RoomId] = room.RoomId; pending.Enqueue(next);
                    }
                }
                var result = new List<DungeonRoom>();
                if (!parents.ContainsKey(to.RoomId)) return result;
                for (var id = to.RoomId; id >= 0; id = parents[id]) result.Add(graph.ById(id));
                result.Reverse(); return result;
            }
        }
    }
}
