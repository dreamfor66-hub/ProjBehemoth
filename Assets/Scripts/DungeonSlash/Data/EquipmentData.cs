using System.Collections.Generic;
using UnityEngine;

namespace DungeonSlash
{
    /// <summary>One shop/drop definition. Relics are permanent; potions occupy a limited consumable slot.</summary>
    [CreateAssetMenu(menuName = "Dungeon Slash/Equipment Data")]
    public sealed class EquipmentData : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public ShopItemKind itemKind = ShopItemKind.Relic;
        [Min(0)] public int price = 20;
        [Min(1)] public int maxPurchaseCount = 1;
        public EquipmentTrigger trigger = new()
        {
            triggerType = TriggerType.OnAcquire,
            triggerAttackTypeFilter = TriggerAttackTypeFilter.All,
            triggerAttackWayFilter = TriggerAttackWayFilter.All,
            triggerCount = 1
        };
        public EquipmentCondition condition;
        public TargetType targetType = TargetType.Owner;
        public EquipmentEffect effect = new() { effectCount = 1 };

        public bool IsPotion => itemKind == ShopItemKind.Potion;
        public bool IsRelic => itemKind == ShopItemKind.Relic;
    }
}
