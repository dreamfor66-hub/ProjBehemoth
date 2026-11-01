using System.Collections.Generic;
using UnityEngine;

namespace DungeonSlash
{
    [CreateAssetMenu(menuName = "Dungeon Slash/Perk Data")]
    public sealed class PerkData : LocalizedData
    {
        public string id;
        public string displayName;
        [TextArea(5, 12)] public string description;
        public Sprite icon;
        public RelicScope scope;
        public CharacterKind exclusiveCharacter;
        [Tooltip("Optional shared reward bucket. One member per offer, with the group's combined weight.")] public PerkOfferGroup offerGroup;
        [Tooltip("Reusable ability data; this is not equipped as a unique relic.")] public EquipmentData abilitySource;
        [Min(1)] public int maxStacks = 1;
        [Header("Merchant")]
        public bool soldByMerchant = true;
        [Min(0)] public int shopPrice = 45;
        public List<StatModifier> modifiers = new();
    }
}
