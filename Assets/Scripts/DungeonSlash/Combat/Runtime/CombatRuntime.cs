using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonSlash
{
    public enum PointerGestureState { None, Pressed, Charging, Charged, SecondCharging, SecondCharged, Attacking, Guarding }
    public enum ShieldState { Ready, Guarding, Broken, Recharging }
    public enum MonsterState { Idle, Attacking, Charging, Stunned, Dead }
    public enum DamageType { NormalAttack, ChargeAttack, MonsterAttack, Special }

    public static class GuardGestureResolver
    {
        private const float PlayerHalfExtent = 150f;

        public static bool IsPlayerDownwardGuard(Vector2 playerCenter, Vector2 pressPosition, Vector2 releasePosition, float guardSwipeDistance)
        {
            var localPress = pressPosition - playerCenter;
            if (Mathf.Abs(localPress.x) > PlayerHalfExtent || Mathf.Abs(localPress.y) > PlayerHalfExtent)
                return false;

            var stroke = releasePosition - pressPosition;
            var downwardDistance = -stroke.y;
            var minimumDownwardDistance = Mathf.Max(30f, guardSwipeDistance * .55f);
            if (downwardDistance < minimumDownwardDistance)
                return false;

            if (Mathf.Abs(stroke.x) > downwardDistance * 1.35f)
                return false;

            return releasePosition.y < playerCenter.y - 12f;
        }
    }

    public sealed class PlayerCombatRuntime
    {
        public float MaxHp { get; private set; }
        public float CurrentHp { get; private set; }
        public float ShieldMax { get; private set; }
        public float ShieldCurrent { get; private set; }
        public ShieldState ShieldState { get; private set; }
        public float AttackCooldownRemaining { get; private set; }
        public float NormalDamageMultiplier { get; private set; } = 1f;
        public float ChargeDamageMultiplier { get; private set; } = 1f;
        public float AttackCooldownMultiplier { get; private set; } = 1f;
        public float ChargeDurationMultiplier { get; private set; } = 1f;
        public float StunDamageMultiplier { get; private set; } = 1f;
        public float ShieldOnHit { get; private set; }
        public float ShieldOnWeakPoint { get; private set; }
        public float AttackReach { get; private set; }
        public float MonsterKillHeal { get; private set; }
        public float AllDamageMultiplier { get; private set; } = 1f;
        public float DamageReduction { get; private set; }
        public float DamagePerAttackDistance { get; private set; }
        public bool ChargeGuardEnabled => chargeGuardSources > 0;
        public bool DoubleChargeEnabled => doubleChargeSources > 0;
        public bool IsAlive => CurrentHp > 0f;
        public bool IsGuarding => ShieldState == ShieldState.Guarding || chargeGuardActive;

        private readonly PlayerCombatData data;
        private float normalShieldRegen;
        private float brokenShieldRegen;
        private bool chargeGuardActive;
        private int chargeGuardSources;
        private int doubleChargeSources;
        private readonly List<TemporaryModifier> temporaryModifiers = new();

        private readonly struct TemporaryModifier
        {
            public StatModifier Modifier { get; }
            public float ExpiresAt { get; }
            public TemporaryModifier(StatModifier modifier, float expiresAt) { Modifier = modifier; ExpiresAt = expiresAt; }
        }

        public PlayerCombatRuntime(PlayerCombatData source)
        {
            data = source;
            Reset();
        }

        public void Reset()
        {
            MaxHp = data.maxHp;
            CurrentHp = MaxHp;
            ShieldMax = data.shieldMax;
            ShieldCurrent = ShieldMax;
            ShieldState = ShieldState.Ready;
            AttackCooldownRemaining = 0f;
            NormalDamageMultiplier = ChargeDamageMultiplier = AttackCooldownMultiplier = ChargeDurationMultiplier = StunDamageMultiplier = 1f;
            ShieldOnHit = ShieldOnWeakPoint = 0f;
            AttackReach = MonsterKillHeal = 0f;
            AllDamageMultiplier = 1f;
            DamageReduction = 0f;
            DamagePerAttackDistance = 0f;
            chargeGuardActive = false;
            chargeGuardSources = doubleChargeSources = 0;
            temporaryModifiers.Clear();
            normalShieldRegen = data.normalShieldRegen;
            brokenShieldRegen = data.brokenShieldRegen;
        }

        public float NormalAttackDamage => data.baseAttackDamage * NormalDamageMultiplier * AllDamageMultiplier;
        public float ChargeAttackDamage => data.baseChargeDamage * ChargeDamageMultiplier * AllDamageMultiplier;
        public float CurrentCooldown => Mathf.Max(data.minimumAttackCooldown, data.attackCooldown * AttackCooldownMultiplier);
        public float CurrentChargeDuration => data.chargeDuration * ChargeDurationMultiplier;
        public float ChargeTolerance => data.inputMoveTolerance;
        public float SegmentWidth => data.attackSegmentWidth;
        public float GuardSwipeDistance => data.guardSwipeDistance;
        public float SwipeSampleWindowSeconds => data.swipeSampleWindowSeconds;
        public float MinimumSwipeDistance => data.minimumSwipeDistance;
        public float MinimumSwipeSpeed => data.minimumSwipeSpeed;
        public float MinimumDirectionConsistency => data.minimumDirectionConsistency;

        public void Tick(float deltaTime)
        {
            AttackCooldownRemaining = Mathf.Max(0f, AttackCooldownRemaining - deltaTime);
            ExpireTemporaryModifiers();
            if (!IsAlive || IsGuarding) return;

            if (ShieldState == ShieldState.Broken)
            {
                ShieldCurrent = Mathf.Min(ShieldMax, ShieldCurrent + brokenShieldRegen * deltaTime);
                if (ShieldCurrent >= ShieldMax)
                    ShieldState = ShieldState.Ready;
            }
            else
            {
                ShieldCurrent = Mathf.Min(ShieldMax, ShieldCurrent + normalShieldRegen * deltaTime);
                if (ShieldCurrent < ShieldMax)
                    ShieldState = ShieldState.Recharging;
                else if (ShieldState == ShieldState.Recharging)
                    ShieldState = ShieldState.Ready;
            }
        }

        public bool CanNormalAttack() => IsAlive && AttackCooldownRemaining <= 0f;
        public void StartAttackCooldown() => AttackCooldownRemaining = CurrentCooldown;
        public void ResetAttackCooldown() => AttackCooldownRemaining = 0f;
        public bool TrySetGuard(bool enabled)
        {
            if (!enabled)
            {
                if (ShieldState == ShieldState.Guarding) ShieldState = ShieldCurrent >= ShieldMax ? ShieldState.Ready : ShieldState.Recharging;
                return true;
            }

            // A partially recovered shield is still usable. Only a fully broken shield locks guarding.
            if (!IsAlive || ShieldState == ShieldState.Broken || ShieldCurrent <= 0f) return false;
            ShieldState = ShieldState.Guarding;
            return true;
        }

        public void SetChargeGuard(bool enabled)
        {
            chargeGuardActive = enabled && IsAlive && ShieldState != ShieldState.Broken && ShieldCurrent > 0f;
        }

        public void RestoreShield(float amount) => ShieldCurrent = Mathf.Min(ShieldMax, ShieldCurrent + amount);
        public void RestoreShieldForNavigation()
        {
            ShieldCurrent = ShieldMax;
            ShieldState = ShieldState.Ready;
        }
        public void Heal(float amount) => CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        public void Revive(float healthFraction)
        {
            CurrentHp = Mathf.Max(1f, MaxHp * Mathf.Clamp01(healthFraction));
            RestoreShieldForNavigation();
        }
        public DamageResult TakeEnvironmentalDamage(float damage)
        {
            var appliedDamage = Mathf.Max(0f, damage);
            CurrentHp = Mathf.Max(0f, CurrentHp - appliedDamage);
            return new DamageResult(appliedDamage, 0f, !IsAlive, false);
        }

        public void ApplyModifiers(IEnumerable<StatModifier> modifiers)
        {
            foreach (var modifier in modifiers)
                ApplyModifier(modifier, 1f);
        }

        /// <summary>Applies a stat effect for one combat. A zero duration lasts until the combat ends.</summary>
        public void ApplyCombatModifier(StatModifier modifier, float duration)
        {
            ApplyModifier(modifier, 1f);
            temporaryModifiers.Add(new TemporaryModifier(modifier, duration > 0f ? Time.unscaledTime + duration : float.PositiveInfinity));
        }

        public void ClearCombatModifiers()
        {
            foreach (var modifier in temporaryModifiers) ApplyModifier(modifier.Modifier, -1f);
            temporaryModifiers.Clear();
        }

        private void ExpireTemporaryModifiers()
        {
            var now = Time.unscaledTime;
            for (var index = temporaryModifiers.Count - 1; index >= 0; index--)
            {
                if (temporaryModifiers[index].ExpiresAt > now) continue;
                ApplyModifier(temporaryModifiers[index].Modifier, -1f);
                temporaryModifiers.RemoveAt(index);
            }
        }

        private void ApplyModifier(StatModifier modifier, float direction)
        {
            var value = modifier.value * direction;
            switch (modifier.kind)
            {
                case ModifierKind.NormalDamage: NormalDamageMultiplier += value; break;
                case ModifierKind.ChargeDamage: ChargeDamageMultiplier += value; break;
                case ModifierKind.AttackCooldownMultiplier: AttackCooldownMultiplier = Mathf.Max(.1f, AttackCooldownMultiplier - value); break;
                case ModifierKind.ChargeDurationMultiplier: ChargeDurationMultiplier = Mathf.Max(.2f, ChargeDurationMultiplier - value); break;
                case ModifierKind.MaxHp: MaxHp = Mathf.Max(1f, MaxHp + value); CurrentHp = Mathf.Clamp(CurrentHp + (direction > 0f ? modifier.value : 0f), 0f, MaxHp); break;
                case ModifierKind.Heal: if (direction > 0f) Heal(modifier.value); break;
                case ModifierKind.ShieldMax: ShieldMax = Mathf.Max(0f, ShieldMax + value); ShieldCurrent = Mathf.Clamp(ShieldCurrent + (direction > 0f ? modifier.value : 0f), 0f, ShieldMax); break;
                case ModifierKind.NormalShieldRegen: normalShieldRegen += value; break;
                case ModifierKind.BrokenShieldRegen: brokenShieldRegen += value; break;
                case ModifierKind.ShieldOnHit: ShieldOnHit += value; break;
                case ModifierKind.ShieldOnWeakPoint: ShieldOnWeakPoint += value; break;
                case ModifierKind.StunDamageMultiplier: StunDamageMultiplier += value; break;
                case ModifierKind.AttackReach: AttackReach += value; break;
                case ModifierKind.MonsterKillHeal: MonsterKillHeal += value; break;
                case ModifierKind.AllDamageMultiplier: AllDamageMultiplier = Mathf.Max(.1f, AllDamageMultiplier + value); break;
                case ModifierKind.DamageReduction: DamageReduction = Mathf.Clamp01(DamageReduction + value); break;
                case ModifierKind.ChargeGuardEnabled: chargeGuardSources += direction > 0f ? 1 : -1; break;
                case ModifierKind.DoubleChargeEnabled: doubleChargeSources += direction > 0f ? 1 : -1; break;
                case ModifierKind.DamagePerAttackDistance: DamagePerAttackDistance += value; break;
            }
        }

        internal DamageResult ReceiveDamage(DamageRequest request)
        {
            if (IsGuarding && request.IsGuardable)
            {
                ShieldCurrent = Mathf.Max(0f, ShieldCurrent - request.ShieldDamage);
                if (ShieldCurrent <= 0f)
                    ShieldState = ShieldState.Broken;
                return new DamageResult(0f, request.ShieldDamage, false, true);
            }

            var reducedDamage = request.RawDamage * (1f - DamageReduction);
            CurrentHp = Mathf.Max(0f, CurrentHp - reducedDamage);
            return new DamageResult(reducedDamage, 0f, !IsAlive, false);
        }
    }

    public sealed class WeakPointRuntime
    {
        public string Id { get; }
        public CircleHitShape HitShape { get; private set; }
        public int RemainingHits { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsDestroyed => IsActive && RemainingHits <= 0;

        public WeakPointRuntime(WeakPointDefinition definition, Vector2 center)
        {
            Id = definition.id;
            HitShape = new CircleHitShape(center, definition.hitRadius);
            RemainingHits = definition.requiredChargeHits;
            IsActive = true;
        }

        public bool ApplyChargeHit()
        {
            if (!IsActive || RemainingHits <= 0) return false;
            RemainingHits--;
            return true;
        }

        public void Deactivate() => IsActive = false;
    }

    public sealed class MonsterRuntime
    {
        public MonsterData Data { get; }
        public float MaxHp { get; }
        public float DamageMultiplier { get; }
        public float CurrentHp { get; private set; }
        public MonsterState State { get; private set; }
        public float StateTimer { get; private set; }
        public float NormalAttackTimer { get; private set; }
        public float ChargeTimer { get; private set; }
        public IReadOnlyList<WeakPointRuntime> WeakPoints => weakPoints;
        public bool IsAlive => State != MonsterState.Dead;
        public bool IsStunned => State == MonsterState.Stunned;
        public int ChargePatternIndex { get; private set; }
        private readonly List<WeakPointRuntime> weakPoints = new();

        public MonsterRuntime(MonsterData data, float healthMultiplier = 1f, float damageMultiplier = 1f)
        {
            Data = data;
            MaxHp = data.maxHp * Mathf.Max(.1f, healthMultiplier);
            DamageMultiplier = Mathf.Max(.1f, damageMultiplier);
            CurrentHp = MaxHp;
            State = MonsterState.Idle;
            NormalAttackTimer = data.normalAttackInterval;
            ChargeTimer = data.chargeInterval;
        }

        public void Tick(float deltaTime, Vector2 center)
        {
            if (!IsAlive) return;
            StateTimer = Mathf.Max(0f, StateTimer - deltaTime);
            switch (State)
            {
                case MonsterState.Idle:
                    NormalAttackTimer -= deltaTime;
                    ChargeTimer -= deltaTime;
                    break;
                case MonsterState.Attacking:
                    break;
                case MonsterState.Charging:
                    break;
                case MonsterState.Stunned:
                    if (StateTimer <= 0f)
                    {
                        State = MonsterState.Idle;
                        NormalAttackTimer = Data.normalAttackInterval;
                        ChargeTimer = Data.chargeInterval;
                    }
                    break;
            }
        }

        public bool ShouldNormalAttack => State == MonsterState.Idle && NormalAttackTimer <= 0f && ChargeTimer > 0f;
        public bool ShouldCharge => State == MonsterState.Idle && ChargeTimer <= 0f;
        public void BeginNormalAttack() { State = MonsterState.Attacking; StateTimer = Data.normalAttack.windupDuration; }
        public bool IsNormalAttackReadyToHit => State == MonsterState.Attacking && StateTimer <= 0f;
        public void CompleteNormalAttack()
        {
            if (!IsAlive) return;
            State = MonsterState.Idle;
            NormalAttackTimer = Data.normalAttackInterval;
        }
        public void StartCharge(Vector2 center)
        {
            State = MonsterState.Charging;
            var pattern = Data.chargePatterns[ChargePatternIndex % Data.chargePatterns.Count];
            ChargePatternIndex++;
            StateTimer = pattern.timeLimit;
            weakPoints.Clear();
            foreach (var point in pattern.weakPoints)
                weakPoints.Add(new WeakPointRuntime(point, center + point.normalizedPosition));
        }

        public bool ChargeExpired => State == MonsterState.Charging && StateTimer <= 0f;
        public bool AllWeakPointsDestroyed => weakPoints.Count > 0 && weakPoints.All(point => point.IsDestroyed);
        public void EndCharge()
        {
            if (!IsAlive) return;
            foreach (var point in weakPoints) point.Deactivate();
            weakPoints.Clear();
            State = MonsterState.Idle;
            ChargeTimer = Data.chargeInterval;
            NormalAttackTimer = Data.normalAttackInterval;
        }
        public void EnterStun() { foreach (var point in weakPoints) point.Deactivate(); weakPoints.Clear(); State = MonsterState.Stunned; StateTimer = Data.stunDuration; }
        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;
            CurrentHp = Mathf.Max(0f, CurrentHp - damage);
            if (CurrentHp <= 0f) { State = MonsterState.Dead; weakPoints.Clear(); }
        }
    }

    public readonly struct DamageRequest
    {
        public DamageType Type { get; }
        public float RawDamage { get; }
        public float ShieldDamage { get; }
        public bool IsGuardable { get; }
        public DamageRequest(DamageType type, float rawDamage, float shieldDamage, bool isGuardable)
        { Type = type; RawDamage = rawDamage; ShieldDamage = shieldDamage; IsGuardable = isGuardable; }
    }

    public readonly struct DamageResult
    {
        public float HpDamage { get; }
        public float ShieldDamage { get; }
        public bool TargetDied { get; }
        public bool WasGuarded { get; }
        public DamageResult(float hpDamage, float shieldDamage, bool targetDied, bool wasGuarded)
        { HpDamage = hpDamage; ShieldDamage = shieldDamage; TargetDied = targetDied; WasGuarded = wasGuarded; }
    }

    public sealed class DamageResolver
    {
        public DamageResult ApplyToPlayer(PlayerCombatRuntime player, DamageRequest request) => player.ReceiveDamage(request);
        public void ApplyToMonster(MonsterRuntime monster, float damage, float stunMultiplier) => monster.TakeDamage(monster.IsStunned ? damage * stunMultiplier : damage);
    }
}
