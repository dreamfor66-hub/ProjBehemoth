using System;
using System.Collections.Generic;
using UnityEngine;
namespace DungeonSlash
{
    public enum EventEffectKind { Relic, SharedRelicChoice, ExclusiveChoice, PerkChoice, SpellChoice, Gold, Heal, Stat, SetFlag, RemoveFlag, Fight, SacrificeRelic, RemovePerk, UpgradePerk, Burden, GoldDebt, Potion }
    [Serializable]
    public sealed class EventEffect
    {
        public EventEffectKind kind;
        public EquipmentData relic;
        public SpellData spell;
        public EncounterData encounter;
        public string flag;
        public float amount;
        public ModifierKind stat;
    }
    [Serializable]
    public sealed class EventOption
    {
        public string title;
        [TextArea] public string description;
        [Min(0)] public int goldCost;
        [Min(0)] public float hpCost;
        [Min(0)] public float maxHpCost;
        public bool consumeHealingPotion;
        [HideInInspector] public RelicPower requiredPower; // Old assets only; new conditions use an asset reference.
        public EquipmentData requiredRelic;
        public string requiredPerk;
        public string requiredFlag;
        public List<EventEffect> effects = new();
    }
    [CreateAssetMenu(menuName = "Dungeon Slash/Random Event")]
    public sealed class RandomEventData : LocalizedData
    {
        public string id;
        public string displayName;
        [TextArea(3, 8)] public string narrative;
        public Sprite illustration;
        public Color accent = new(.65f, .8f, .65f);
        [Range(1, 3)] public int minAct = 1;
        [Range(1, 3)] public int maxAct = 3;
        [Min(.01f)] public float weight = 1f;
        [Min(0)] public int minNormalCombatsInAct;
        public string referenceUrl;
        [TextArea] public string referenceExperience;
        public string requiredFlag;
        public string forbiddenFlag;
        public List<EventOption> options = new();
    }
}
