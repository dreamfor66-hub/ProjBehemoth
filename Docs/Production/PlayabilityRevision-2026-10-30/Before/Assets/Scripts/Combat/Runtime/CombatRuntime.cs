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
        public static bool IsPlayerDownwardGuard(in RectHitShape playerBody, Vector2 pressPosition, Vector2 releasePosition, float guardSwipeDistance, float bodyOuterMargin)
        {
            return RectHitResolver.Contains(playerBody, pressPosition, bodyOuterMargin)
                && IsDownwardStroke(pressPosition, releasePosition, guardSwipeDistance)
                && releasePosition.y < playerBody.Center.y - 12f;
        }

        /// <summary>
        /// A slow, held downward motion can also become a guard once it settles inside the player.
        /// Unlike the quick-flick guard, the press may have begun anywhere on the playfield.
        /// </summary>
        public static bool IsPlayerRestGuardCandidate(in RectHitShape playerBody, Vector2 pressPosition, Vector2 holdingPosition, float guardSwipeDistance)
        {
            return RectHitResolver.Contains(playerBody, holdingPosition)
                && IsDownwardStroke(pressPosition, holdingPosition, guardSwipeDistance);
        }

        /// <summary>Loose directional check used when an input that began outside enters the player's body.</summary>
        public static bool IsDownwardApproach(Vector2 startPosition, Vector2 endPosition)
        {
            var stroke = endPosition - startPosition;
            var downwardDistance = -stroke.y;
            return downwardDistance > 0f && Mathf.Abs(stroke.x) <= downwardDistance * 1.35f;
        }

        private static bool IsDownwardStroke(Vector2 startPosition, Vector2 endPosition, float guardSwipeDistance)
        {
            var downwardDistance = -(endPosition - startPosition).y;
            var minimumDownwardDistance = Mathf.Max(30f, guardSwipeDistance * .55f);
            return downwardDistance >= minimumDownwardDistance && IsDownwardApproach(startPosition, endPosition);
        }
    }

    public sealed class GuardRestConfirmation
    {
        private float stationarySince = -1f;
        public bool HasTouchedGuardZone { get; private set; }

        public void Reset()
        {
            HasTouchedGuardZone = false;
            stationarySince = -1f;
        }

        public bool Observe(bool pathTouchesGuardZone, bool isInsideBodyAndDownward, float pointerMovement, float time, float stationaryDistance, float confirmSeconds)
        {
            if (pathTouchesGuardZone) HasTouchedGuardZone = true;
            if (!HasTouchedGuardZone || !isInsideBodyAndDownward || pointerMovement > stationaryDistance)
            {
                stationarySince = -1f;
                return false;
            }

            if (stationarySince < 0f)
            {
                stationarySince = time;
                return false;
            }

            return time - stationarySince >= confirmSeconds;
        }
    }

    public sealed partial class PlayerCombatRuntime
    {
        private float baseMaxHp; public float MaxHp { get => baseMaxHp + BuffStat(ModifierKind.MaxHp); private set => baseMaxHp = value; }
        public float CurrentHp { get; private set; }
        public void DieFromExhaustion(){CurrentHp=0;}
        private float baseShieldMax; public float ShieldMax { get => baseShieldMax + BuffStat(ModifierKind.ShieldMax); private set => baseShieldMax = value; }
        public float ShieldCurrent { get; private set; }
        public ShieldState ShieldState { get; private set; }
        public float AttackCooldownRemaining { get; private set; }
        private float baseNormalDamageMultiplier = 1f; public float NormalDamageMultiplier { get => baseNormalDamageMultiplier + BuffStat(ModifierKind.NormalDamage); private set => baseNormalDamageMultiplier = value; }
        private float baseChargeDamageMultiplier = 1f; public float ChargeDamageMultiplier { get => baseChargeDamageMultiplier + BuffStat(ModifierKind.ChargeDamage); private set => baseChargeDamageMultiplier = value; }
        private float baseAttackCooldownMultiplier = 1f; public float AttackCooldownMultiplier { get => baseAttackCooldownMultiplier - BuffStat(ModifierKind.AttackCooldownMultiplier); private set => baseAttackCooldownMultiplier = value; }
        private float baseChargeDurationMultiplier = 1f; public float ChargeDurationMultiplier { get => baseChargeDurationMultiplier - BuffStat(ModifierKind.ChargeDurationMultiplier); private set => baseChargeDurationMultiplier = value; }
        private float baseStunDamageMultiplier = 1f; public float StunDamageMultiplier { get => baseStunDamageMultiplier + BuffStat(ModifierKind.StunDamageMultiplier); private set => baseStunDamageMultiplier = value; }
        private float baseShieldOnHit; public float ShieldOnHit { get => baseShieldOnHit + BuffStat(ModifierKind.ShieldOnHit); private set => baseShieldOnHit = value; }
        private float baseShieldOnWeakPoint; public float ShieldOnWeakPoint { get => baseShieldOnWeakPoint + BuffStat(ModifierKind.ShieldOnWeakPoint); private set => baseShieldOnWeakPoint = value; }
        private float baseAttackReach; public float AttackReach { get => baseAttackReach + BuffStat(ModifierKind.AttackReach); private set => baseAttackReach = value; }
        private float baseMonsterKillHeal; public float MonsterKillHeal { get => baseMonsterKillHeal + BuffStat(ModifierKind.MonsterKillHeal); private set => baseMonsterKillHeal = value; }
        private float allDamageMultiplier = 1f;
        public float AllDamageMultiplier { get => Mathf.Max(.1f,allDamageMultiplier + BuffStat(ModifierKind.AllDamageMultiplier) + Rule(AbilityRule.DamageMultiplier)); private set => allDamageMultiplier = value; }
        private float baseDamageReduction; public float DamageReduction { get => baseDamageReduction + BuffStat(ModifierKind.DamageReduction); private set => baseDamageReduction = value; }
        private float baseDamagePerAttackDistance; public float DamagePerAttackDistance { get => baseDamagePerAttackDistance + BuffStat(ModifierKind.DamagePerAttackDistance); private set => baseDamagePerAttackDistance = value; }
        public bool ChargeGuardEnabled => chargeGuardSources > 0;
        public bool DoubleChargeEnabled => doubleChargeSources > 0;
        public bool IsAlive => CurrentHp > 0f;
        public bool IsGuarding => IsAlive && ShieldState != ShieldState.Broken && ShieldCurrent > 0f && (ShieldState == ShieldState.Guarding || chargeGuardActive || keyboardGuardActive);

        private readonly PlayerCombatData data;
        private float normalShieldRegen;
        private float brokenShieldRegen;
        private bool chargeGuardActive;
        private bool keyboardGuardActive;
        private int chargeGuardSources;
        private int doubleChargeSources;
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
            keyboardGuardActive = false;
            chargeGuardSources = doubleChargeSources = 0;
            Buffs.Clear();
            extraPermanentStats.Clear();
            RefillMp();
            normalShieldRegen = data.normalShieldRegen;
            brokenShieldRegen = data.brokenShieldRegen;
            ResetCharacter();
        }

        public float NormalAttackDamage => (data.baseAttackDamage * NormalDamageMultiplier + BuffStat(ModifierKind.FlatNormalDamage)) * AllDamageMultiplier * (1+Rule(AbilityRule.WeaponBaseMultiplier));
        public float ChargeAttackDamage => (data.baseChargeDamage * ChargeDamageMultiplier + BuffStat(ModifierKind.FlatChargeDamage)) * AllDamageMultiplier * (1+Rule(AbilityRule.WeaponBaseMultiplier));
        public float CriticalChance => Mathf.Clamp01(data.criticalChance+BuffStat(ModifierKind.CriticalChance));
        public float CriticalDamageMultiplier => Mathf.Max(1,data.criticalDamageMultiplier+BuffStat(ModifierKind.CriticalDamageMultiplier));
        public float CurrentCooldown => Mathf.Max(data.minimumAttackCooldown, data.attackCooldown * AttackCooldownMultiplier * Mathf.Max(.2f, 1 + Rule(AbilityRule.CooldownMultiplier)) * Mathf.Max(.2f, 1 - Rule(AbilityRule.AttackCooldownReduction))) / (Owner?.Fatigue.AttackSpeedMultiplier??1);
        public float CurrentChargeDuration => data.chargeDuration * ChargeDurationMultiplier * Mathf.Max(.2f, 1 - Rule(AbilityRule.ChargeTimeReduction));
        public float ChargeTolerance => data.inputMoveTolerance;
        public float SegmentWidth => data.attackSegmentWidth;
        public float GuardSwipeDistance => data.guardSwipeDistance;
        public float GuardBodyOuterMargin => data.guardBodyOuterMargin;
        public float GuardRestConfirmSeconds => data.guardRestConfirmSeconds;
        public float GuardRestStationaryDistance => data.guardRestStationaryDistance;
        public float TraversalExitDistance => data.traversalExitDistance;
        public float TraversalStopConfirmSeconds => data.traversalStopConfirmSeconds;
        public float TraversalStationaryDistance => data.traversalStationaryDistance;
        public float SwipeSampleWindowSeconds => data.swipeSampleWindowSeconds;
        public float MinimumSwipeDistance => data.minimumSwipeDistance;
        public float MinimumSwipeSpeed => data.minimumSwipeSpeed;
        public float MinimumDirectionConsistency => data.minimumDirectionConsistency;

        public void Tick(float deltaTime)
        {
            AttackCooldownRemaining = Mathf.Max(0f, AttackCooldownRemaining - deltaTime);
            TickCharacter(deltaTime);
            CurrentHp = Mathf.Min(CurrentHp, MaxHp); ShieldCurrent = Mathf.Min(ShieldCurrent, ShieldMax);
            if (!IsAlive || IsGuarding) return;

            if (ShieldState == ShieldState.Broken)
            {
                ShieldCurrent = Mathf.Min(ShieldMax, ShieldCurrent + (brokenShieldRegen + BuffStat(ModifierKind.BrokenShieldRegen)) * deltaTime);
                if (ShieldCurrent >= ShieldMax)
                    ShieldState = ShieldState.Ready;
            }
            else
            {
                ShieldCurrent = Mathf.Min(ShieldMax, ShieldCurrent + (normalShieldRegen + BuffStat(ModifierKind.NormalShieldRegen)) * deltaTime);
                if (ShieldCurrent < ShieldMax)
                    ShieldState = ShieldState.Recharging;
                else if (ShieldState == ShieldState.Recharging)
                    ShieldState = ShieldState.Ready;
            }
        }

        public bool CanNormalAttack() => IsAlive && AttackCooldownRemaining <= 0f;
        // Charge preparation shares the same recovery window as a normal attack.
        public bool CanStartCharge() => IsAlive && AttackCooldownRemaining <= 0f;
        public void StartAttackCooldown() => AttackCooldownRemaining = CurrentCooldown;
        public void ResetAttackCooldown() => AttackCooldownRemaining = 0f;
        public bool TrySetGuard(bool enabled)
        {
            SetGuardIntent(enabled);
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

        public void SetKeyboardGuard(bool enabled)
        {
            keyboardGuardActive = enabled && IsAlive && ShieldState != ShieldState.Broken && ShieldCurrent > 0f;
        }

        public void RestoreShield(float amount) => ShieldCurrent = Mathf.Min(ShieldMax, ShieldCurrent + amount);
        public void RestoreShieldForNavigation()
        {
            ShieldCurrent = ShieldMax;
            ShieldState = ShieldState.Ready;
        }
        public void Heal(float amount) => CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        public void ClampResources()
        {
            CurrentHp=Mathf.Min(CurrentHp,MaxHp);ShieldCurrent=Mathf.Min(ShieldCurrent,ShieldMax);RestoreMp(0);
        }
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

        public void RemoveModifiers(IEnumerable<StatModifier> modifiers)
        {
            foreach (var modifier in modifiers)
                ApplyModifier(modifier, -1f);
        }

        /// <summary>Compatibility API. Timed effects use the common Buff manager.</summary>
        public void ApplyCombatModifier(StatModifier modifier, float duration)
        {
            var buff = ScriptableObject.CreateInstance<BuffData>(); buff.hideFlags = HideFlags.HideAndDontSave;
            buff.name="runtime-stat-"+System.Guid.NewGuid().ToString("N");
            buff.ReleaseData = new BuffReleaseData { Type = duration > 0 ? BuffReleaseType.Time : BuffReleaseType.Stage, Time = duration, Count = 1 };
            buff.Units.Add(new EquipmentAbility { trigger = new EquipmentTrigger { chancePercent = 100,  triggerType = TriggerType.Passive }, target = TargetType.OwnerStat,
                effects = new List<EquipmentEffect> { new EquipmentEffect { effectType = EffectType.StatIncrease, effectStatType = modifier.kind, effectMagnitude = modifier.value, effectCount = 1 } } });
            Buffs.Add(buff, root: Owner); Buffs.Tick(0);
        }
        public void ClearCombatModifiers() => Buffs.EndCombat();
        private void ApplyModifier(StatModifier modifier, float direction)
        {
            var value = modifier.value * direction;
            switch (modifier.kind)
            {
                case ModifierKind.NormalDamage: baseNormalDamageMultiplier += value; break;
                case ModifierKind.ChargeDamage: baseChargeDamageMultiplier += value; break;
                case ModifierKind.AttackCooldownMultiplier: baseAttackCooldownMultiplier = Mathf.Max(.1f, baseAttackCooldownMultiplier - value); break;
                case ModifierKind.ChargeDurationMultiplier: baseChargeDurationMultiplier = Mathf.Max(.2f, baseChargeDurationMultiplier - value); break;
                case ModifierKind.MaxHp: baseMaxHp = Mathf.Max(1f, baseMaxHp + value); CurrentHp = Mathf.Clamp(CurrentHp + (direction > 0f ? Mathf.Max(0, modifier.value) : 0f), 0f, baseMaxHp); break;
                case ModifierKind.Heal: if (direction > 0f) Heal(modifier.value); break;
                case ModifierKind.ShieldMax: baseShieldMax = Mathf.Max(0f, baseShieldMax + value); ShieldCurrent = Mathf.Clamp(ShieldCurrent + (direction > 0f ? Mathf.Max(0, modifier.value) : 0f), 0f, baseShieldMax); break;
                case ModifierKind.NormalShieldRegen: normalShieldRegen += value; break;
                case ModifierKind.BrokenShieldRegen: brokenShieldRegen += value; break;
                case ModifierKind.ShieldOnHit: baseShieldOnHit += value; break;
                case ModifierKind.ShieldOnWeakPoint: baseShieldOnWeakPoint += value; break;
                case ModifierKind.StunDamageMultiplier: baseStunDamageMultiplier += value; break;
                case ModifierKind.AttackReach: baseAttackReach += value; break;
                case ModifierKind.MonsterKillHeal: baseMonsterKillHeal += value; break;
                case ModifierKind.AllDamageMultiplier: allDamageMultiplier = Mathf.Max(.1f, allDamageMultiplier + value); break;
                case ModifierKind.DamageReduction: baseDamageReduction = Mathf.Clamp01(baseDamageReduction + value); break;
                case ModifierKind.ChargeGuardEnabled: chargeGuardSources += direction > 0f ? 1 : -1; break;
                case ModifierKind.DoubleChargeEnabled: doubleChargeSources += direction > 0f ? 1 : -1; break;
                case ModifierKind.DamagePerAttackDistance: baseDamagePerAttackDistance += value; break;
                default:
                    extraPermanentStats.TryGetValue(modifier.kind,out var previous);
                    var total=previous+value;
                    if(Mathf.Abs(total)<.000001f)extraPermanentStats.Remove(modifier.kind);
                    else extraPermanentStats[modifier.kind]=total;
                    break;
            }
        }

        internal DamageResult ReceiveDamage(DamageRequest request)
        {
            var context = new EquipmentTriggerContext(default, TriggerAttackType.None, TriggerAttackWay.None, false, request.Source?.IsStunned ?? false, request.Source);
            if (IsGuarding && request.IsGuardable)
            {
                var mutable = new ActionEffectContext();
                context = new EquipmentTriggerContext(default, TriggerAttackType.None, TriggerAttackWay.None, false, false, request.Source, guardDamage: request.ShieldDamage, mutable: mutable);
                Owner?.EmitEvent(TriggerType.OnBeforeGuard, context);
                var guardDamage = request.ShieldDamage * Mathf.Max(0, mutable.GuardCostMultiplier);
                ShieldCurrent = Mathf.Max(0f, ShieldCurrent - guardDamage);
                if (ShieldCurrent <= 0f)
                    ShieldState = ShieldState.Broken;
                const float chip = 0f;
                ApplyCharacterDamage(chip);
                GuardHitCount++;
                Owner?.EmitEvent(TriggerType.OnGuard, new EquipmentTriggerContext(default, TriggerAttackType.None, TriggerAttackWay.None, false, false, request.Source, guardDamage: request.ShieldDamage));
                return new DamageResult(chip, guardDamage, !IsAlive, true, mutable.GuardFeedback == GuardFeedback.Perfect, mutable.GuardFeedback == GuardFeedback.Parry);
            }

            var hitPacket=new ActionEffectContext();
            var incoming=new EquipmentTriggerContext(default,TriggerAttackType.None,TriggerAttackWay.None,false,false,request.Source,damage:request.RawDamage,mutable:hitPacket);
            var hitEffects=Owner?.BeginHit(TriggerType.OnHurt,incoming);
            var reducedDamage = request.RawDamage * Mathf.Max(0,1+hitPacket.DamageBonus) * (1f - EffectiveDamageReduction);
            ApplyCharacterDamage(reducedDamage);
            var hurt = new EquipmentTriggerContext(default, TriggerAttackType.None, TriggerAttackWay.None, false, false, request.Source, damage: reducedDamage);
            Owner?.CompleteAction(TriggerType.OnHurt, hurt);
            if(hitEffects!=null)Owner.FinishHit(hitEffects,reducedDamage);
            return new DamageResult(reducedDamage, 0f, !IsAlive, false);
        }
    }

    public sealed class WeakPointRuntime
    {
        public string Id { get; }
        public CircleHitShape HitShape { get; private set; }
        public int RequiredHits { get; }
        public bool IsOptional { get; }
        public int RemainingHits { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsDestroyed => IsActive && RemainingHits <= 0;

        public WeakPointRuntime(WeakPointDefinition definition, Vector2 center, float hitScale = 1f)
        {
            Id = definition.id;
            HitShape = new CircleHitShape(center, definition.hitRadius * Mathf.Max(.1f, hitScale));
            RequiredHits = Mathf.Max(1, definition.requiredChargeHits);
            IsOptional = definition.optional;
            RemainingHits = RequiredHits;
            IsActive = true;
        }

        public bool ApplyChargeHit()
        {
            if (!IsActive || RemainingHits <= 0) return false;
            RemainingHits--;
            return true;
        }

        public void Deactivate() => IsActive = false;
        public void Move(Vector2 delta) => HitShape = new CircleHitShape(HitShape.Center + delta, HitShape.Radius);
    }

    public sealed partial class MonsterRuntime
    {
        public MonsterData Data { get; }
        public float MaxHp { get; }
        public float DamageMultiplier { get; }
        public float VisualScale { get; }
        public float CurrentHp { get; private set; }
        public MonsterState State { get; private set; }
        public float StateTimer { get; private set; }
        public float NormalAttackTimer { get; private set; }
        public float ChargeTimer { get; private set; }
        public IReadOnlyList<WeakPointRuntime> WeakPoints => weakPoints;
        public bool IsAlive => State != MonsterState.Dead;
        public bool IsStunned => State == MonsterState.Stunned;
        public EncounterAwarenessRuntime Awareness { get; set; }
        public bool IsUnaware => Awareness?.IsConcealed == true;
        public void NotifyAttacked() => Awareness?.Reveal();
        private int normalActionIndex;
        public MonsterAttackData NormalAttack
        {
            get
            {
                var sequence = Data?.normalAttackSequence;
                return sequence != null && sequence.Count > 0
                    ? sequence[normalActionIndex % sequence.Count] ?? Data.GetAttack(MonsterAttackType.Normal)
                    : Data?.GetAttack(MonsterAttackType.Normal);
            }
        }
        public MonsterAttackData CurrentChargeAttack { get; private set; }
        public bool HasChargeAttack => AllowedChargeAttacks().Any();
        public int ChargeAttackIndex { get; private set; }
        public int RemainingNormalAttackHits { get; private set; }
        public int RemainingChargeHits { get; private set; }
        private MonsterCombatMechanics NormalCombatMechanics => NormalAttack?.combatMechanics;
        private DirectionalGuardData DirectionalGuard => NormalCombatMechanics?.directionalGuard;
        public bool HasDirectionalGuard => DirectionalGuard != null && DirectionalGuard.enabled;
        public bool IsDirectionalGuardActive => HasDirectionalGuard && directionalGuardActive;
        public float DirectionalGuardCurrent => HasDirectionalGuard ? directionalGuardCurrent : 0f;
        public float DirectionalGuardMax => HasDirectionalGuard ? Mathf.Max(1f, DirectionalGuard.maxGuard) : 0f;
        public string DirectionalGuardLabel
        {
            get
            {
                if (!HasDirectionalGuard) return string.Empty;
                var direction = DirectionalGuard.breakByAttackWay switch
                {
                    TriggerAttackWayFilter.Horizontal => GameText.Get("ui.combatruntime.2aa89c79"),
                    TriggerAttackWayFilter.Upward => GameText.Get("ui.combatruntime.e951ad3d"),
                    TriggerAttackWayFilter.Downward => GameText.Get("ui.combatruntime.668dc945"),
                    _ => GameText.Get("ui.combatruntime.29a11b1")
                };
                if (DirectionalGuard.breakByAttackWay == (TriggerAttackWayFilter.Upward | TriggerAttackWayFilter.Downward))
                    direction = GameText.Get("ui.combat.guard.vertical");
                return directionalGuardActive
                    ? GameText.Format("ui.combatruntime.96a6a71b", direction, directionalGuardCurrent, DirectionalGuardMax)
                    : GameText.Format("ui.combatruntime.ed3e470e", directionalGuardCurrent, DirectionalGuardMax);
            }
        }
        private readonly List<WeakPointRuntime> weakPoints = new();
        private float directionalGuardCurrent;
        private float directionalGuardRebuildDelay;
        private bool directionalGuardActive;

        public MonsterRuntime(MonsterData data, float healthMultiplier = 1f, float damageMultiplier = 1f, float visualScale = 1f)
        {
            Data = data;
            MaxHp = data.maxHp * Mathf.Max(.1f, healthMultiplier);
            DamageMultiplier = Mathf.Max(.1f, damageMultiplier);
            VisualScale = Mathf.Max(.1f, visualScale);
            CurrentHp = MaxHp;
            State = MonsterState.Idle;
            NormalAttackTimer = data.normalAttackInterval;
            ChargeTimer = data.chargeInterval;
            directionalGuardActive = HasDirectionalGuard;
            directionalGuardCurrent = DirectionalGuardMax;
        }

        /// <summary>Offsets an independently spawned unit so a group does not strike in the same frame.</summary>
        public void DelayActions(float seconds)
        {
            var delay = Mathf.Max(0f, seconds);
            NormalAttackTimer += delay;
            ChargeTimer += delay;
        }

        public void Tick(float deltaTime, Vector2 center)
        {
            if (!IsAlive) return;
            TickStatus(deltaTime);
            if (IsUnaware) return;
            deltaTime *= SlowRemaining > 0 ? .65f : 1f;
            StateTimer = Mathf.Max(0f, StateTimer - deltaTime);
            TickDirectionalGuard(deltaTime);
            switch (State)
            {
                case MonsterState.Idle:
                    NormalAttackTimer -= deltaTime;
                    if (HasChargeAttack) ChargeTimer -= deltaTime;
                    else ChargeTimer = float.PositiveInfinity;
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

        public bool ShouldNormalAttack => !IsUnaware && State == MonsterState.Idle && NormalAttackTimer <= 0f && ChargeTimer > 0f;
        public bool ShouldCharge => !IsUnaware && State == MonsterState.Idle && ChargeTimer <= 0f;
        public float NormalAttackPresentationDuration => (NormalAttack?.windupDuration ?? 0f) + Mathf.Max(0, (NormalCombatMechanics?.hitCount ?? 1) - 1) * (NormalCombatMechanics?.followupDelay ?? .01f);
        public void BeginNormalAttack()
        {
            State = MonsterState.Attacking;
            RemainingNormalAttackHits = Mathf.Max(1, NormalCombatMechanics?.hitCount ?? 1);
            StateTimer = NormalAttack?.windupDuration ?? .01f;
        }
        public bool IsNormalAttackReadyToHit => State == MonsterState.Attacking && StateTimer <= 0f;
        public bool AdvanceNormalAttack()
        {
            if (!IsAlive || State != MonsterState.Attacking) return false;
            RemainingNormalAttackHits = Mathf.Max(0, RemainingNormalAttackHits - 1);
            if (RemainingNormalAttackHits > 0)
            {
                StateTimer = Mathf.Max(.01f, NormalCombatMechanics?.followupDelay ?? .01f);
                return true;
            }
            CompleteNormalAttack();
            return false;
        }
        public void CompleteNormalAttack()
        {
            if (!IsAlive) return;
            normalActionIndex++;
            State = MonsterState.Idle;
            NormalAttackTimer = Data.normalAttackInterval;
        }
        public void StartCharge(Vector2 center)
        {
            var chargeAttacks = AllowedChargeAttacks().ToArray();
            if (chargeAttacks.Length == 0) return;
            State = MonsterState.Charging;
            CurrentChargeAttack = chargeAttacks[ChargeAttackIndex % chargeAttacks.Length];
            ChargeAttackIndex++;
            StateTimer = CurrentChargeAttack.chargeTimeLimit;
            RemainingChargeHits = Mathf.Max(1, CurrentChargeAttack.combatMechanics?.hitCount ?? 1);
            weakPoints.Clear();
            foreach (var point in CurrentChargeAttack.chargeWeakPoints)
                weakPoints.Add(new WeakPointRuntime(point, center + point.localPosition * VisualScale, VisualScale));
        }

        public bool ChargeExpired => State == MonsterState.Charging && StateTimer <= 0f;
        public bool AllWeakPointsDestroyed => weakPoints.Any(point => !point.IsOptional) && weakPoints.Where(point => !point.IsOptional).All(point => point.IsDestroyed);
        public bool AdvanceChargeAttack()
        {
            if (!IsAlive || State != MonsterState.Charging) return false;
            RemainingChargeHits = Mathf.Max(0, RemainingChargeHits - 1);
            if (RemainingChargeHits <= 0) return false;

            // The weak-point exposure ends when its charge resolves; follow-up hits are the same committed attack.
            ClearWeakPoints();
            StateTimer = Mathf.Max(.01f, CurrentChargeAttack?.combatMechanics?.followupDelay ?? .01f);
            return true;
        }

        public void EndCharge()
        {
            if (!IsAlive) return;
            ClearWeakPoints();
            State = MonsterState.Idle;
            CurrentChargeAttack = null;
            RemainingChargeHits = 0;
            ChargeTimer = Data.chargeInterval;
            NormalAttackTimer = Data.normalAttackInterval;
        }
        public void EnterStun() { var bonus = weakPoints.Count(point => point.IsOptional && point.IsDestroyed); ClearWeakPoints(); State = MonsterState.Stunned; StateTimer = Data.stunDuration * (1f + bonus * .5f); }
        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;
            if (damage > 0) NotifyAttacked();
            CurrentHp = Mathf.Max(0f, CurrentHp - damage);
            if (CurrentHp <= 0f) { State = MonsterState.Dead; ClearWeakPoints(); }
        }

        public float Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return 0f;
            var before = CurrentHp;
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
            return CurrentHp - before;
        }

        /// <summary>Returns true when an active directional guard intercepted this slash.</summary>
        public bool TryAbsorbDirectionalGuard(TriggerAttackWay attackWay, float damage, out float absorbedDamage, out bool matchedDirection, out bool broken)
        {
            absorbedDamage = 0f;
            matchedDirection = false;
            broken = false;
            if (!IsDirectionalGuardActive) return false;

            var attackFlag = attackWay switch
            {
                TriggerAttackWay.Horizontal => TriggerAttackWayFilter.Horizontal,
                TriggerAttackWay.Upward => TriggerAttackWayFilter.Upward,
                TriggerAttackWay.Downward => TriggerAttackWayFilter.Downward,
                _ => TriggerAttackWayFilter.All
            };
            matchedDirection = (DirectionalGuard.breakByAttackWay & attackFlag) != 0;
            if (!matchedDirection) return true;

            absorbedDamage = Mathf.Min(directionalGuardCurrent, Mathf.Max(0f, damage));
            directionalGuardCurrent = Mathf.Max(0f, directionalGuardCurrent - Mathf.Max(0f, damage));
            if (directionalGuardCurrent <= 0f)
            {
                directionalGuardActive = false;
                directionalGuardRebuildDelay = DirectionalGuard.rebuildDelay;
                broken = true;
            }
            return true;
        }

        private void TickDirectionalGuard(float deltaTime)
        {
            if (!HasDirectionalGuard || directionalGuardActive) return;
            if (directionalGuardRebuildDelay > 0f)
            {
                directionalGuardRebuildDelay = Mathf.Max(0f, directionalGuardRebuildDelay - deltaTime);
                return;
            }
            directionalGuardCurrent = Mathf.Min(DirectionalGuardMax, directionalGuardCurrent + DirectionalGuard.rebuildPerSecond * deltaTime);
            if (directionalGuardCurrent >= DirectionalGuardMax)
                directionalGuardActive = true;
        }

        private void ClearWeakPoints()
        {
            foreach (var point in weakPoints) point.Deactivate();
            weakPoints.Clear();
        }
    }

    public readonly struct DamageRequest
    {
        public DamageType Type { get; }
        public float RawDamage { get; }
        public float ShieldDamage { get; }
        public bool IsGuardable { get; }
        public MonsterRuntime Source { get; }
        public DamageRequest(DamageType type, float rawDamage, float shieldDamage, bool isGuardable, MonsterRuntime source = null)
        { Type = type; RawDamage = rawDamage; ShieldDamage = shieldDamage; IsGuardable = isGuardable; Source = source; }
    }

    public readonly struct DamageResult
    {
        public float HpDamage { get; }
        public float ShieldDamage { get; }
        public bool TargetDied { get; }
        public bool WasGuarded { get; }
        public bool WasPerfectGuard { get; }
        public bool WasSheathParry { get; }
        public DamageResult(float hpDamage, float shieldDamage, bool targetDied, bool wasGuarded, bool wasPerfectGuard = false, bool wasSheathParry = false)
        { HpDamage = hpDamage; ShieldDamage = shieldDamage; TargetDied = targetDied; WasGuarded = wasGuarded; WasPerfectGuard = wasPerfectGuard; WasSheathParry = wasSheathParry; }
    }

    public readonly struct MonsterDamageResult
    {
        public readonly float Damage;
        public readonly bool IsCritical;
        public readonly bool WasStunned;
        public MonsterDamageResult(float damage,bool critical,bool stunned){Damage=damage;IsCritical=critical;WasStunned=stunned;}
    }

    public sealed class DamageResolver
    {
        public DamageResult ApplyToPlayer(PlayerCombatRuntime player, DamageRequest request) => player.ReceiveDamage(request);
        public void ApplyToMonster(MonsterRuntime monster, float damage, float stunMultiplier) => monster.TakeDamage(monster.IsStunned ? damage * stunMultiplier : damage);
        public MonsterDamageResult ApplyToMonster(MonsterRuntime monster,float damage,PlayerCombatRuntime player,System.Random random,bool apply=true)
        {
            if(monster==null||!monster.IsAlive||damage<=0)return default;
            var stunned=monster.IsStunned;
            var chance=player.CriticalChance;
            var critical=stunned||chance>=1||chance>0&&random.NextDouble()<chance;
            var total=damage*(stunned?player.StunDamageMultiplier:1)*(critical?player.CriticalDamageMultiplier:1)*(player.Owner?.Fatigue.FinalDamageMultiplier??1);
            if(apply)monster.TakeDamage(total);
            return new MonsterDamageResult(total,critical,stunned);
        }
    }
}
