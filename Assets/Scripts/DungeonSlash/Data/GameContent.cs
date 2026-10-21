using System.Collections.Generic;
using UnityEngine;
namespace DungeonSlash
{
    [CreateAssetMenu(menuName = "Dungeon Slash/Game Content")]
    public sealed class GameContent : ScriptableObject
    {
        public int schemaVersion;
        public GameVariables variables;
        public List<PlayerCombatData> characters = new();
        public List<EquipmentData> equipment = new();
        public List<PerkData> perks = new();
        public List<SpellData> spells = new();
        public List<StageSetData> stages = new();
        public List<RandomEventData> events = new();
    }
}
