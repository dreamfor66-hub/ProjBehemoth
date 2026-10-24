using System.Collections.Generic;
using UnityEngine;
namespace DungeonSlash
{
    [CreateAssetMenu(menuName = "Dungeon Slash/Game Content")]
    public sealed class GameContent : ScriptableObject
    {
        public int schemaVersion;
        public int gameplayRevision, artRevision, narrativeRevision;
        public int spellbookPoolRevision;
        public int fatigueRevision;
        public FatigueSettings fatigue;
        public GameVariables variables;
        public List<PlayerCombatData> characters = new();
        public List<EquipmentData> equipment = new();
        public List<PerkData> perks = new();
        public List<SpellData> spells = new();
        public List<StageSetData> stages = new();
        public List<RandomEventData> events = new();
    }
}
