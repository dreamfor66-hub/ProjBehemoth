using UnityEngine;

namespace DungeonSlash
{
    /// <summary>A stable slot in one merchant's stock, backed by the authored item or perk.</summary>
    public sealed class ShopOffer
    {
        public EquipmentData Item { get; }
        public PerkData Perk { get; }
        public ShopOffer(EquipmentData item) => Item = item;
        public ShopOffer(PerkData perk) => Perk = perk;
        public string DisplayName => Perk != null ? Perk.displayName : Item != null ? Item.displayName : string.Empty;
        public string Description => Perk != null ? Perk.description : Item != null ? Item.description : string.Empty;
        public Sprite Icon => Perk != null ? Perk.icon : Item != null ? Item.icon : null;
        public int Price => Perk != null ? Perk.shopPrice : Item != null ? Item.price : 0;
        public bool Purchased { get; internal set; }
    }
}
