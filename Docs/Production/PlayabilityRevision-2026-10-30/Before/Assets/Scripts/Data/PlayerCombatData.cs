using System.Collections.Generic;
using UnityEngine;
namespace DungeonSlash
{
    public enum CharacterKind { Warrior, Rogue, Barbarian, Samurai, Monk, Wizard }
    [CreateAssetMenu(menuName = "Dungeon Slash/Player Combat Data")]
    public sealed class PlayerCombatData : LocalizedData
    {
        public CharacterKind character;
        public string displayName = "워리어";
        [TextArea(5, 12)] public string description;
        [Header("Character selection")]
        [TextArea] public string selectionStory;
        [TextArea] public string selectionAbility;
        public Color characterColor = new(.22f, .65f, .9f);
        public Sprite portrait;
        public Sprite selectionIllustration;
        public Material selectionMaterial;
        public Color selectionBackdrop = new(.05f,.08f,.12f);
        public Sprite combatSprite;
        public CombatSpriteAnimation combatAnimation;
        public GameVariables gameVariables;
        public EquipmentData startingRelic;
        public List<SpellData> startingSpells = new();
        public List<StartingSupply> startingSupplies = new();
        public List<BuffData> attackBuffs = new();
        [Header("Health and Shield")]
        [Min(1f)] public float maxHp = 100f;
        [Header("Mana")]
        [Min(0)] public float maxMp = 30;
        [Min(0)] public float mpRegen = .1f;
        [Min(0f)] public float shieldMax = 100f;
        [Range(0, 1)] public float guardDamageReduction = 1f;
        [Min(0f)] public float normalShieldRegen = 8f;
        [Min(0f)] public float brokenShieldRegen = 22f;

        [Header("Attacks")]
        [Range(0,1)] public float criticalChance;
        [Min(1)] public float criticalDamageMultiplier = 1.5f;
        [Min(0f)] public float baseAttackDamage = 12f;
        [Min(0f)] public float baseChargeDamage = 28f;
        [Min(.01f)] public float attackCooldown = .5f;
        [Min(.01f)] public float chargeDuration = .75f;
        public GameVariables Variables => gameVariables != null ? gameVariables : GameVariables.Default;
        // Compatibility accessors for tools and existing tests; input settings are serialized in GameVariables only.
        public float minimumAttackCooldown { get => Variables.minimumAttackCooldown; set => Variables.minimumAttackCooldown = value; }
        public float inputMoveTolerance { get => Variables.inputMoveTolerance; set => Variables.inputMoveTolerance = value; }
        public float attackSegmentWidth { get => Variables.attackSegmentWidth; set => Variables.attackSegmentWidth = value; }
        public float guardSwipeDistance { get => Variables.guardSwipeDistance; set => Variables.guardSwipeDistance = value; }
        public float guardBodyOuterMargin { get => Variables.guardBodyOuterMargin; set => Variables.guardBodyOuterMargin = value; }
        public float guardRestConfirmSeconds { get => Variables.guardRestConfirmSeconds; set => Variables.guardRestConfirmSeconds = value; }
        public float guardRestStationaryDistance { get => Variables.guardRestStationaryDistance; set => Variables.guardRestStationaryDistance = value; }
        public float traversalExitDistance { get => Variables.traversalExitDistance; set => Variables.traversalExitDistance = value; }
        public float traversalStopConfirmSeconds { get => Variables.traversalStopConfirmSeconds; set => Variables.traversalStopConfirmSeconds = value; }
        public float traversalStationaryDistance { get => Variables.traversalStationaryDistance; set => Variables.traversalStationaryDistance = value; }
        public float swipeSampleWindowSeconds { get => Variables.swipeSampleWindowSeconds; set => Variables.swipeSampleWindowSeconds = value; }
        public float minimumSwipeDistance { get => Variables.minimumSwipeDistance; set => Variables.minimumSwipeDistance = value; }
        public float minimumSwipeSpeed { get => Variables.minimumSwipeSpeed; set => Variables.minimumSwipeSpeed = value; }
        public float minimumDirectionConsistency { get => Variables.minimumDirectionConsistency; set => Variables.minimumDirectionConsistency = value; }
    }
}
