using UnityEngine;

namespace DungeonSlash
{
    [CreateAssetMenu(menuName = "Dungeon Slash/Shop Inventory")]
    public sealed class ShopInventorySettings : ScriptableObject
    {
        [Min(0)] public int potionSlots = 3;
        [Min(0)] public int perkSlots = 3;
        [Min(0)] public int relicSlots = 4;
        public int TotalSlots => potionSlots + perkSlots + relicSlots;
    }
}
