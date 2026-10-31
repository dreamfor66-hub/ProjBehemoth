using System;
using System.Collections.Generic;
using UnityEngine;
namespace DungeonSlash
{
    public enum EventEncounterKind { Object, Person, Creature, Hostile }
    public enum EventRoomLayout { Any, Alcove }
    public enum EventEffectKind { Relic, SharedRelicChoice, ExclusiveChoice, PerkChoice, SpellChoice, Gold, Heal, Stat, SetFlag, RemoveFlag, Fight, SacrificeRelic, RemovePerk, UpgradePerk, Burden, GoldDebt, Potion, LearnSpell }
    [Serializable]
    public sealed class EventEffect
    {
        public EventEffectKind kind;
        public EquipmentData relic;
        public SpellData spell;
        [Tooltip("Optional authored candidates for SpellChoice; empty uses the shared low-tier spell pool.")]
        public List<SpellData> spellPool = new();
        [Min(1)] public int spellChoiceCount = 3;
        public EncounterData encounter;
        public string flag;
        public float amount;
        public ModifierKind stat;
    }
    [Serializable]
    public sealed class EventOption
    {
        public string title;
        [TextArea(5, 12)] public string description;
        [Min(0)] public int goldCost;
        [Min(0)] public float hpCost;
        [Min(0)] public float maxHpCost;
        public bool consumeHealingPotion;
        [HideInInspector] public RelicPower requiredPower; // Old assets only; new conditions use an asset reference.
        public EquipmentData requiredRelic;
        public string requiredPerk;
        public string requiredFlag;
        public SupplyData supply;
        [Min(0)] public int supplyCost;
        public int nextPage = -1;
        public int failurePage = -1;
        [TextArea] public string resultText;
        [Header("Persistent room aftermath (terminal choices)")]
        public RoomObjectData resultingWorldObject;
        public bool removeWorldObject;
        public bool useWorldObject;
        public string revisitTextKey;
        public List<EventRequirement> requirements = new();
        public List<EventEffect> effects = new();
    }
    public enum EventRequirementKind { MaxMp, SpellCasts, LearnedSpells, KnowsSpell, Flag, WonCombats, OwnsRelic, ChosenSpell }
    [Serializable] public sealed class EventRequirement
    {
        public EventRequirementKind kind;
        public float amount;
        public SpellData spell;
        public EquipmentData relic;
        public string flag;
        public bool invert;
        public bool Matches(RunState run)
        {
            var result = kind switch
            {
                EventRequirementKind.MaxMp => run.Player.MaxMp >= amount,
                EventRequirementKind.SpellCasts => run.SpellCasts >= amount,
                EventRequirementKind.LearnedSpells => run.Spellbook.Count >= amount,
                EventRequirementKind.KnowsSpell => spell != null && run.RuntimeFor(spell) != null,
                EventRequirementKind.Flag => run.EventFlags.Contains(flag),
                EventRequirementKind.WonCombats => run.WonCombats >= amount,
                EventRequirementKind.OwnsRelic => relic != null && run.EquippedItems.Contains(relic),
                EventRequirementKind.ChosenSpell => spell != null && !string.IsNullOrEmpty(flag) && run.EventSpellChoices.TryGetValue(flag, out var chosen) && chosen == spell,
                _ => false
            };
            return invert ? !result : result;
        }
    }
    [Serializable] public sealed class EventPage
    {
        [TextArea(3,8)] public string narrative;
        public Sprite illustration;
        public List<EventOption> options = new();
        public List<EventEntryRoute> routes = new();
    }
    [Serializable] public sealed class EventEntryRoute
    {
        public List<EventRequirement> requirements = new();
        public int page;
    }
    [CreateAssetMenu(menuName = "Dungeon Slash/Random Event")]
    public sealed class RandomEventData : LocalizedData
    {
        public string id;
        public string displayName;
        public EventDiscovery discovery;
        public RoomObjectData worldObject;
        public EventEncounterKind encounterKind;
        [TextArea(3, 7), Tooltip("Authoring context: reason for entering, objective and what the player can infer. Not shown as exposition.")]
        public string authoringLore;
        public EventRoomLayout roomLayout;
        [Tooltip("Optional complete room paintings, act 1 / 2 / 3, on each installed floor's camera.")]
        public Sprite[] roomInteriors = new Sprite[3];
        [Tooltip("Keep authored pages but exclude this event from random encounters.")]
        public bool excludedFromRandomPool;
        [TextArea(3, 8)] public string narrative;
        public Sprite illustration;
        public Color accent = new(.65f, .8f, .65f);
        [Tooltip("Portrait illustration on the left; complete page text and choices on the right.")]
        public bool illustratedPopup;
        [Range(1, 3)] public int minAct = 1;
        [Range(1, 3)] public int maxAct = 3;
        [Min(.01f)] public float weight = 1f;
        [Min(0)] public int minNormalCombatsInAct;
        public string referenceUrl;
        [TextArea] public string referenceExperience;
        public string requiredFlag;
        public string forbiddenFlag;
        public List<EventOption> options = new();
        public List<EventPage> pages = new();
        public List<EventEntryRoute> entryRoutes = new();
    }
}
