using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonSlash
{
    [Serializable]
    public sealed class WeakPointDefinition
    {
        public string id = "weak-point";
        public Vector2 normalizedPosition;
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
        RevivalHealthFraction
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
        Passive, OnCombatStart, OnTimer, OnKill, OnPoisonTick, OnSpellCast, OnSheathParry, OnPerfectGuard, OnFormFinish, OnWeakPointBreak, OnCombatWon, OnAttackResolved
    }

    [Flags]
    public enum TriggerAttackTypeFilter { NormalAttack = 1, ChargeAttack = 2, AdditionalAttack = 4, All = NormalAttack | ChargeAttack | AdditionalAttack }
    [Flags]
    public enum TriggerAttackWayFilter { Horizontal = 1, Upward = 2, Downward = 4, All = Horizontal | Upward | Downward }
    public enum ConditionType { None, OwnerCharge, EnemyStun }
    public enum TargetType { Owner, Enemy }
    public enum EffectType { ExtraAttack, Damage, StatIncrease, Heal, RestoreGuard, ApplyStatus, DetonatePoison, ReduceSpellCooldown, ResetAttackCooldown, AddRage, StoreGuardDamage, RuleModifier, LearnSpell, UnlockForm, SummonCompanion, EchoSpell, SpreadStatus, Interrupt }

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
        public MonkFormData form;
        public CompanionData companion;
        [Min(0)] public int buffUses;
        public TriggerType consumeOn;
        public TriggerAttackTypeFilter consumeAttackTypes;
        [Tooltip("같은 그룹의 버프는 중첩하지 않고 갱신합니다. 비어 있으면 유물의 개별 효과별로 갱신합니다.")] public string buffGroup;
    }

    [Serializable]
    public struct StatModifier { public ModifierKind kind; public float value; }

    // MajorReward is retained only to read older saved/generated data. New runs use Chest for every chest outcome.
    public enum RoomEncounterType { Start, Combat, Reward, Fountain, PoisonFountain, Chest, Goddess, Shop, Elite, MajorReward, Boss, Empty, Event }
    public enum ChestContent { Gold, Relic, Mimic }
}
