using UnityEngine;
namespace DungeonSlash
{
    [CreateAssetMenu(menuName = "Dungeon Slash/Resource")]
    public sealed class CombatResourceData : LocalizedData
    {
        public string displayName;
        [Min(1)] public float maximum = 100;
        public float initialValue;
        public bool showInHud;
    }
}
