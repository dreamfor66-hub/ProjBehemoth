using System.Collections.Generic;
using UnityEngine;

namespace DungeonSlash
{
    /// <summary>One shop/drop definition. Relics are permanent; potions occupy a limited consumable slot.</summary>
    [CreateAssetMenu(menuName = "Dungeon Slash/Equipment Data")]
    public sealed class EquipmentData : LocalizedData
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public RelicScope scope;
        public CharacterKind exclusiveCharacter;
        public StartingMechanic startingMechanic;
        [HideInInspector] public RelicPower power; // Migration metadata only; never used by gameplay dispatch.
        [UnityEngine.Serialization.FormerlySerializedAs("grantedSpell"), SerializeField, HideInInspector] private SpellData legacyGrantedSpell;
        public SpellData grantedSpell { get { foreach (var ability in abilities) foreach (var node in ability.effects) if (node.effectType == EffectType.LearnSpell) return node.spell; return legacyGrantedSpell; } set => legacyGrantedSpell = value; }
        [HideInInspector] public List<StatModifier> additionalModifiers = new();
        public List<EquipmentAbility> abilities = new();
        public ShopItemKind itemKind = ShopItemKind.Relic;
        [Min(0)] public int price = 20;
        [Min(1)] public int maxPurchaseCount = 1;
        [HideInInspector] public EquipmentTrigger trigger = new()
        {
            triggerType = TriggerType.OnAcquire,
            triggerAttackTypeFilter = TriggerAttackTypeFilter.All,
            triggerAttackWayFilter = TriggerAttackWayFilter.All,
            triggerCount = 1
        };
        [HideInInspector] public EquipmentCondition condition;
        [HideInInspector] public TargetType targetType = TargetType.Owner;
        [HideInInspector] public EquipmentEffect effect = new() { effectCount = 1 };

        public bool IsPotion => itemKind == ShopItemKind.Potion;
        public bool IsRelic => itemKind == ShopItemKind.Relic;
    }
}
