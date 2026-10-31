using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonSlash
{
    [Serializable]
    public sealed class WeakPointDefinition
    {
        public string id = "weak-point";
        [UnityEngine.Serialization.FormerlySerializedAs("normalizedPosition")]
        public Vector2 localPosition;
        [Min(1f)] public float hitRadius = 30f;
        [Min(1)] public int requiredChargeHits = 1;
        public bool optional;
    }

    public enum ModifierKind
    {
        NormalDamage, ChargeDamage, AttackCooldownMultiplier, ChargeDurationMultiplier, MaxHp, Heal, ShieldMax,
        NormalShieldRegen, BrokenShieldRegen, ShieldOnHit, ShieldOnWeakPoint, StunDamageMultiplier, AttackReach,
        MonsterKillHeal, Gold,
        AllDamageMultiplier, DamageReduction, ChargeGuardEnabled, DoubleChargeEnabled, DamagePerAttackDistance,
        RevivalHealthFraction, FlatNormalDamage, FlatChargeDamage, MaxMp, MpRegen, FireDamage, IceDamage, LightningDamage, PoisonDamage,
        CriticalChance, CriticalDamageMultiplier
    }

    public enum ShopItemKind { Relic, Potion }
    public enum TriggerType
    {
        OnAttack,
        OnHit,
        OnHurt,
        OnGuard,
        OnAcquire,
        OnConsume,
        OnCombatStartAfterConsume,
        Passive, OnCombatStart, OnTimer, OnKill, OnPoisonTick, OnSpellCast, OnSheathParry, OnPerfectGuard, OnAttackBuffUsed, OnWeakPointBreak, OnCombatWon, OnAttackResolved,
        OnBuffStart, OnBuffEnd, OnBuffStack, OnConditionMet, OnGuardStarted, OnGuardEnded, OnBeforeGuard, OnActionPreparing, OnSignal,
        OnActionCompleted, OnEncounterRevealed, OnThrow
    }

    [Flags]
    public enum TriggerAttackTypeFilter { NormalAttack = 1, ChargeAttack = 2, AdditionalAttack = 4, All = NormalAttack | ChargeAttack | AdditionalAttack }
    [Flags]
    public enum TriggerAttackWayFilter { Horizontal = 1, Upward = 2, Downward = 4, All = Horizontal | Upward | Downward }
    public enum ConditionType { None, OwnerCharge, EnemyStun }
    public enum TargetType { Owner, Enemy, OwnerStat, ThisBuff, Action, Hit }
    public enum EffectType { ExtraAttack = 0, Damage = 1, StatIncrease = 2, Heal = 3, RestoreGuard = 4, ApplyStatus = 5, DetonatePoison = 6, ReduceSpellCooldown = 7, ResetAttackCooldown = 8, StoreGuardDamage = 10, RuleModifier = 11, LearnSpell = 12, UnlockBuff = 13, SummonCompanion = 14, EchoSpell = 15, SpreadStatus = 16, Interrupt = 17,
        AddBuff = 18, RemoveBuff = 19, ChangeBuffStack = 20, SetBuffStack = 21, ActionDamage = 24, ActionExtraHits = 25, ActionCharge = 26, GuardCost = 27, EmitSignal = 28, PayDeferredDamage = 29, ClearDeferredDamage = 30, MarkAction = 31, ChangeBuffValue = 32, ScaleActionDamage = 33, RestoreMp = 34, ElementDamage = 35, Cleanse = 36, RecoverFatigue = 37 }

    [Serializable]
    public struct EquipmentTrigger
    {
        public TriggerType triggerType;
        public TriggerAttackTypeFilter triggerAttackTypeFilter;
        public TriggerAttackWayFilter triggerAttackWayFilter;
        [Min(1)] public int triggerCount;
        [Min(0)] public int triggerMaxCount;
        [Min(0)] public float intervalSeconds;
        [Min(0)] public float cooldownSeconds;
        public bool resetCountEachCombat;
        public bool includeAdditionalAttacks;
        [Range(0,100)] public float chancePercent;
        public BuffTagData signal;
        public bool UsesAttackFilters => triggerType is TriggerType.OnAttack or TriggerType.OnHit or TriggerType.OnKill or TriggerType.OnAttackResolved or TriggerType.OnActionPreparing or TriggerType.OnWeakPointBreak;
    }

    [Serializable]
    public struct EquipmentCondition { public ConditionType conditionType; }

    [Serializable]
    public struct EquipmentEffect
    {
        public EffectType effectType;
        public float effectMagnitude;
        [Min(1)] public int effectCount;
        public float effectAngle;
        [Min(0f)] public float effectTime;
        [Min(0)] public int travelGraceSteps;
        public ModifierKind effectStatType;
        public EffectScale scale;
        [Tooltip("실제 효과량 = (effectMagnitude + 참조값 × scaleRatio) × effectCount")] public float scaleRatio;
        public float maximumMagnitude;
        public SpellElement element;
        public AbilityTarget targets;
        public StatusKind status;
        public float statusDamage;
        [Min(1)] public int statusCap;
        public AbilityRule rule;
        public SpellData spell;
        public BuffData buff;
        public BuffTagData buffTag;
        public List<EffectValueVariable> valueVariables;
        public GuardFeedback guardFeedback;
        public bool multiplyByBuffStacks;
        public CompanionData companion;
        [Min(0)] public int buffUses;
        public TriggerType consumeOn;
        public TriggerAttackTypeFilter consumeAttackTypes;
        [Tooltip("같은 그룹의 버프는 중첩하지 않고 갱신합니다. 비어 있으면 유물의 개별 효과별로 갱신합니다.")] public string buffGroup;
    }

    [Serializable]
    public struct StatModifier { public ModifierKind kind; public float value; }

    // MajorReward is retained only to read older saved/generated data. New runs use Chest for every chest outcome.
    public enum RoomEncounterType { Start, Combat, Reward, Fountain, PoisonFountain, Chest, Goddess, Shop, Elite, MajorReward, Boss, Empty, Event, Camp }
    public enum ChestContent { Gold, Relic, Mimic }
}
