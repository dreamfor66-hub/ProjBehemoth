using UnityEngine;
namespace DungeonSlash
{
    [CreateAssetMenu(menuName = "Dungeon Slash/Companion")]
    public sealed class CompanionData : LocalizedData
    {
        public string displayName;
        public CompanionView prefab;
        public Vector2 playerOffset = new(-200, 70);
        public float idleBobHeight = 3;
        public float attackLungeDistance = 32;
    }
}
