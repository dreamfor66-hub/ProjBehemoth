using System;
using System.Collections.Generic;
using System.Linq;
namespace DungeonSlash
{
    public sealed class EncounterPlanner
    {
        private readonly GameContent content;
        private readonly int seed;
        private readonly Dictionary<string, EncounterData> assigned = new();
        private readonly Dictionary<int, HashSet<string>> elitesSeen = new();
        public EncounterPlanner(GameContent content, int seed) { this.content = content; this.seed = seed; }
        public StageSetData Stage(int act)
        {
            var candidates = content.stages.Where(stage => stage != null && stage.act == act).ToArray();
            return candidates.Length == 0 ? null : candidates[new Random(unchecked(seed + act * 7919)).Next(candidates.Length)];
        }
        public EncounterData Get(int act, RoomEncounterType type, int roomId, int normalCount)
        {
            var key = $"{act}:{type}:{roomId}";
            if (assigned.TryGetValue(key, out var existing)) return existing;
            var stage = Stage(act); if (stage == null) return null;
            var random = new Random(unchecked(seed + act * 1619 + roomId * 577 + (int)type * 919));
            var normal = type != RoomEncounterType.Elite && type != RoomEncounterType.Boss;
            var packsEnabled = normalCount >= stage.easyCombatCount && random.NextDouble() < stage.packChance;
            var pool = type == RoomEncounterType.Elite ? stage.elites : type == RoomEncounterType.Boss ? stage.bosses : packsEnabled ? stage.hardCombats : stage.earlyCombats;
            if (!elitesSeen.TryGetValue(act, out var used)) elitesSeen[act] = used = new HashSet<string>();
            var maxCount = !packsEnabled ? 1 : act == 1 || normalCount < stage.tripleCombatCount ? 2 : 3;
            var candidates = pool.Where(item => item != null && item.members.Any(member => member.monster != null) && (!normal || item.members.Count <= maxCount) && (type != RoomEncounterType.Elite || !used.Contains(item.id))).ToArray();
            if (normal && candidates.Length == 0) candidates = stage.earlyCombats.Where(item => item != null && item.members.Count == 1).ToArray();
            if (candidates.Length == 0) return null;
            var selected = candidates[random.Next(candidates.Length)]; assigned[key] = selected;
            if (type == RoomEncounterType.Elite) used.Add(selected.id);
            return selected;
        }
    }
}
