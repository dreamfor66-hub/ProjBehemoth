using System.Collections.Generic;
using UnityEngine;
namespace DungeonSlash
{
    [CreateAssetMenu(menuName = "Dungeon Slash/Stage Set")]
    public sealed class StageSetData : LocalizedData
    {
        public string id;
        public string displayName;
        [Range(1, 3)] public int act = 1;
        [Min(0)] public int easyCombatCount = 6;
        [Min(1), Tooltip("Minimum player level before normal multi-monster encounters can be assigned.")] public int minimumPackLevel = 4;
        [Min(0)] public int tripleCombatCount = 9;
        [Range(0, 1)] public float packChance = .35f;
        public List<EncounterData> earlyCombats = new();
        public List<EncounterData> hardCombats = new();
        public List<EncounterData> elites = new();
        public List<EncounterData> bosses = new();
    }
}
