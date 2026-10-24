using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace DungeonSlash
{
    internal sealed class AbilityTriggerState
    {
        public readonly EquipmentData Source;
        public readonly EquipmentAbility Ability;
        public readonly int Index;
        private readonly bool isBuffUnit;
        private int seen, activations;
        private float elapsed, nextAllowed;
        public int Activations => activations;
        public readonly List<EquipmentAbility> Targets = new();
        public bool HasReachedMaximum => Ability.trigger.triggerMaxCount > 0 && activations >= Ability.trigger.triggerMaxCount;
        public AbilityTriggerState(EquipmentData source, EquipmentAbility ability, int index, bool fromBuff = false) { Source = source; Ability = ability; Index = index; isBuffUnit = fromBuff; }
        public void BeginCombat() { elapsed = nextAllowed = 0; if (Ability.trigger.resetCountEachCombat) seen = activations = 0; }
        public void AdvanceTimer(float dt) => elapsed += dt;
        public bool ConsumeTimer() { var period = Mathf.Max(.001f, Ability.trigger.intervalSeconds); if (elapsed + .00001f < period) return false; elapsed -= period; return true; }
        public bool TimerReady(float dt) { AdvanceTimer(dt); return ConsumeTimer(); }
        public bool TryTrigger(RunState run, TriggerType type, EquipmentTriggerContext context)
        {
            var trigger = Ability.trigger;
            if (trigger.triggerType != type || !Ability.Matches(run, context) || (run?.Player.CombatTime ?? context.Buff?.ElapsedTime ?? 0) < nextAllowed) return false;
            if (HasReachedMaximum || type == TriggerType.OnSignal && trigger.signal != null && trigger.signal != context.Signal) return false;
            if(trigger.UsesAttackFilters && !context.HasAttack && (trigger.triggerAttackTypeFilter is not 0 and not TriggerAttackTypeFilter.All || trigger.triggerAttackWayFilter is not 0 and not TriggerAttackWayFilter.All))return false;
            if (trigger.UsesAttackFilters && context.HasAttack)
            {
                if (context.AttackType == TriggerAttackType.Additional && !trigger.includeAdditionalAttacks) return false;
                var attack = context.AttackType == TriggerAttackType.Charge ? TriggerAttackTypeFilter.ChargeAttack : context.AttackType == TriggerAttackType.Normal ? TriggerAttackTypeFilter.NormalAttack : TriggerAttackTypeFilter.AdditionalAttack;
                var way = context.AttackWay == TriggerAttackWay.Upward ? TriggerAttackWayFilter.Upward : context.AttackWay == TriggerAttackWay.Downward ? TriggerAttackWayFilter.Downward : TriggerAttackWayFilter.Horizontal;
                if (trigger.triggerAttackTypeFilter != 0 && (trigger.triggerAttackTypeFilter & attack) == 0 || trigger.triggerAttackWayFilter != 0 && (trigger.triggerAttackWayFilter & way) == 0) return false;
            }
            if (++seen % Mathf.Max(1, trigger.triggerCount) != 0) return false;
            if (trigger.chancePercent <= 0 || trigger.chancePercent < 100 && UnityEngine.Random.value * 100 >= trigger.chancePercent) return false;
            activations++; nextAllowed = (run?.Player.CombatTime ?? context.Buff?.ElapsedTime ?? 0) + trigger.cooldownSeconds;
            return true;
        }
        public IEnumerable<EquipmentActivation> Trigger(RunState run, TriggerType type, EquipmentTriggerContext context)
        {
            if (!TryTrigger(run, type, context)) yield break;
            foreach (var target in Targets.Count > 0 ? Targets : new List<EquipmentAbility> { Ability })
                for (var i = 0; i < target.effects.Count; i++)
                    yield return new EquipmentActivation(Source, target, target.effects[i], !isBuffUnit ? context.WithPotency(run.AbilityCopies(Source)) : context, (Source != null ? Source.GetInstanceID() : 0) + ":" + Index + ":" + i);
        }
    }
    public sealed partial class RunState
    {
        private readonly List<AbilityTriggerState> abilityStates = new();
        private readonly HashSet<AbilityRule> evaluatingRules=new();
        private readonly HashSet<ModifierKind> evaluatingStats=new();
        public Queue<EquipmentActivation> PendingAbilityEffects { get; } = new();
        public EquipmentTriggerContext ActionContext { get; private set; }
        public int ActionSerial { get; private set; }
        private BuffManager buffs;
        public BuffManager Buffs => buffs ??= new BuffManager(this);
        private EncounterAwarenessRuntime awareness;
        public EncounterAwarenessRuntime Awareness
        {
            get
            {
                if (awareness != null) return awareness;
                awareness = new EncounterAwarenessRuntime();
                awareness.Revealed += () => EmitEvent(TriggerType.OnEncounterRevealed, ActionContext);
                return awareness;
            }
        }
        public void StartAction(EquipmentTriggerContext context) { ActionSerial++; ActionContext = context; SnapshotActionBuffs(); }
        public void SetPreparedAction(EquipmentTriggerContext context)=>ActionContext=context;
        public float Rule(AbilityRule kind, EquipmentTriggerContext? supplied = null)
        {
            if(!evaluatingRules.Add(kind))return 0;
            try
            {
            var context = supplied ?? ActionContext; var value = 0f;
            foreach (var node in PassiveUnits(context))
                foreach (var effect in node.Unit.effects) if (effect.effectType == EffectType.RuleModifier && effect.rule == kind && (kind is not AbilityRule.BuffDurationBonus and not AbilityRule.BlockBuff || effect.buff==null || BuffManager.SameData(effect.buff,context.Buff?.Data))) value += EffectValue(effect, node.Context);
            return value;
            }
            finally{evaluatingRules.Remove(kind);}
        }
        private IEnumerable<(EquipmentAbility Unit,EquipmentTriggerContext Context)> PassiveUnits(EquipmentTriggerContext context,bool ignoreConditions=false)
        {
            foreach(var state in abilityStates)
                if(state.Ability.trigger.triggerType==TriggerType.Passive && (ignoreConditions || state.Ability.Matches(this,context)))
                    foreach(var target in state.Targets)yield return(target,context.WithPotency(AbilityCopies(state.Source)));
            foreach(var node in Buffs.PassiveUnits(context,ignoreConditions))yield return node;
        }
        public float Stat(ModifierKind kind)
        {
            if(!evaluatingStats.Add(kind))return 0;
            try{return PassiveUnits(ActionContext).SelectMany(node=>node.Unit.effects.Where(effect=>effect.effectType==EffectType.StatIncrease && effect.effectStatType==kind).Select(effect=>EffectValue(effect,node.Context))).Sum();}
            finally{evaluatingStats.Remove(kind);}
        }
        public BuffData RuleBuff(AbilityRule rule,bool ignoreConditions=false) => PassiveUnits(ActionContext,ignoreConditions)
            .SelectMany(node=>node.Unit.effects).Where(effect=>effect.effectType==EffectType.RuleModifier && effect.rule==rule).Select(effect=>effect.buff).FirstOrDefault(buff=>buff!=null);
        public bool CanApplyBuff(BuffData data,EquipmentTriggerContext context)=>data!=null && Rule(AbilityRule.BlockBuff,context.WithBuff(new BuffInstance{Data=data,Manager=Buffs}))<=0;
        public bool HasCompanion(CompanionData data) => Companions.Contains(data);
        public IEnumerable<CompanionData> Companions => PassiveUnits(default).SelectMany(node=>node.Unit.effects).Where(effect=>effect.effectType==EffectType.SummonCompanion && effect.companion!=null).Select(effect=>effect.companion).Distinct();
        public bool IsBuffUnlocked(BuffData buff) => buff != null && (!buff.requiresUnlock || PassiveUnits(default).SelectMany(node=>node.Unit.effects).Any(effect=>effect.effectType==EffectType.UnlockBuff && BuffManager.SameData(effect.buff,buff)));
        public float EffectValue(EquipmentEffect effect, EquipmentTriggerContext context)
        {
            if (effect.valueVariables != null && effect.valueVariables.Count > 0)
            {
                var result = effect.effectMagnitude;
                foreach (var variable in effect.valueVariables)
                {
                    var contribution = (variable.type == EffectScale.Rule ? Rule(variable.rule, context) : ReferenceValue(variable.type, context, variable.buff)) * variable.multiplier;
                    result += variable.limit > 0 ? Mathf.Min(contribution, variable.limit) : contribution;
                }
                result *= Mathf.Max(1, effect.effectCount);
                return (effect.maximumMagnitude > 0 ? Mathf.Min(result, effect.maximumMagnitude) : result) * Mathf.Max(1, context.Potency) * (effect.multiplyByBuffStacks ? context.Buff?.StackCount ?? 1 : 1);
            }
            var scale = effect.scale switch {
                EffectScale.NormalWeapon => Player.NormalAttackDamage, EffectScale.ChargeWeapon => Player.ChargeAttackDamage,
                EffectScale.DamageDealt => context.Damage, EffectScale.GuardMaximum => Player.ShieldMax, EffectScale.GuardCurrent => Player.ShieldCurrent,
                EffectScale.Level => Level, EffectScale.PoisonStacks => context.Target?.PoisonStacks ?? 0,
                EffectScale.TargetMaxHp => context.Target?.MaxHp ?? 0, EffectScale.EnemyCount => context.EnemyCount, EffectScale.GuardDamage => context.GuardDamage, _ => 0 };
            var value = (effect.effectMagnitude + effect.scaleRatio * scale) * Mathf.Max(1, effect.effectCount);
            return (effect.maximumMagnitude > 0 ? Mathf.Min(value, effect.maximumMagnitude) : value) * Mathf.Max(1, context.Potency) * (effect.multiplyByBuffStacks ? context.Buff?.StackCount ?? 1 : 1);
        }
        private float ReferenceValue(EffectScale scale, EquipmentTriggerContext context, BuffData buff) => scale switch {
            EffectScale.ActionStartBuffElapsed => ActionStartBuffElapsed(buff), EffectScale.ActionStartBuffStacks => ActionStartBuffStacks(buff),
            EffectScale.NormalWeapon => Player.NormalAttackDamage, EffectScale.ChargeWeapon => Player.ChargeAttackDamage,
            EffectScale.DamageDealt => context.Damage, EffectScale.GuardMaximum => Player.ShieldMax, EffectScale.GuardCurrent => Player.ShieldCurrent,
            EffectScale.Level => Level, EffectScale.PoisonStacks => context.Target?.PoisonStacks ?? 0, EffectScale.TargetMaxHp => context.Target?.MaxHp ?? 0,
            EffectScale.EnemyCount => context.EnemyCount, EffectScale.GuardDamage => context.GuardDamage,
            EffectScale.BuffStacks => (buff != null ? (context.Buff?.Manager ?? Buffs).Find(buff) : context.Buff)?.StackCount ?? 0,
            EffectScale.BuffElapsed => (buff != null ? (context.Buff?.Manager ?? Buffs).Find(buff) : context.Buff)?.ElapsedTime ?? 0,
            EffectScale.BuffValue => (buff != null ? (context.Buff?.Manager ?? Buffs).Find(buff) : context.Buff)?.Value ?? 0, _ => 0 };
        private void RegisterAbilities(EquipmentData item)
        {
            AbilityTriggerState host = null;
            for (var i = 0; i < item.abilities.Count; i++)
            { var unit = item.abilities[i]; if (host == null || !unit.mergeConditionTrigger) { host = new AbilityTriggerState(item, unit, i); abilityStates.Add(host); } host.Targets.Add(unit); }
        }
        internal int AbilityCopies(EquipmentData source) => source == null ? 1 : ActivePerks.Where(p => p.Data.abilitySource == source).Select(p => p.StackCount).DefaultIfEmpty(1).Max();
        internal void RegisterPerkAbilities(PerkData perk) { if (perk.abilitySource != null) RegisterAbilities(perk.abilitySource); }
        internal void RemovePerkAbilities(PerkData perk)
        {
            if (perk.abilitySource == null) return;
            abilityStates.RemoveAll(state => state.Source == perk.abilitySource);
            Buffs.RemoveSource(perk.abilitySource);
        }
        public void ExecuteActivation(EquipmentActivation activation, bool permanent)
        {
            if (activation.Target == TargetType.Enemy) PendingAbilityEffects.Enqueue(activation);
            else if (activation.Target == TargetType.ThisBuff)
            {
                var instance = activation.Context.Buff;
                if (activation.Effect.effectType == EffectType.RemoveBuff) instance?.Manager.Remove(instance);
                else instance?.Manager.ChangeStack(instance, EffectValue(activation.Effect, activation.Context), activation.Effect.effectType == EffectType.SetBuffStack);
            }
            else ApplyOwnerActivation(activation, permanent);
        }
        public List<EquipmentActivation> BeginHit(TriggerType type,EquipmentTriggerContext context)
        {
            var effects=Buffs.CollectHit(type,context).ToList();
            if (context.Target != null) effects.AddRange(context.Target.Buffs.CollectHit(type, context));
            foreach(var state in abilityStates)effects.AddRange(state.Trigger(this,type,context));
            foreach(var state in equippedEquipment)if(state.Data.abilities.Count==0 && state.TryActivate(type,context,out var effect))effects.Add(effect);
            foreach(var effect in effects.Where(e=>e.Target==TargetType.Hit))ExecuteActivation(effect,false);
            return effects;
        }
        public void FinishHit(List<EquipmentActivation> effects,float damage)
        {
            ActionHits++;
            foreach(var effect in effects.Where(e=>e.Target!=TargetType.Hit))
                ExecuteActivation(new EquipmentActivation(effect.Source,new EquipmentAbility{target=effect.Target},effect.Effect,effect.Context.WithDamage(damage),effect.BuffKey),false);
            Buffs.CheckReleases();
        }
        public void EmitEvent(TriggerType type, EquipmentTriggerContext context)
        {
            foreach (var activation in TriggerEquipment(type, context))
                ExecuteActivation(activation, type == TriggerType.OnAcquire || type == TriggerType.OnCombatWon);
        }
        public void TickAbilities(float dt)
        {
            Buffs.Tick(dt);
            foreach (var state in abilityStates.Where(state => state.Ability.trigger.triggerType == TriggerType.OnTimer))
            {
                var context=EquipmentTriggerContext.Simple();
                if(!state.Ability.Matches(this,context))continue;
                state.AdvanceTimer(dt);
                while(state.ConsumeTimer())foreach(var activation in state.Trigger(this,TriggerType.OnTimer,context))ExecuteActivation(activation,false);
            }
        }
        public void ApplyAbilityEffect(EquipmentEffect effect, EquipmentTriggerContext context, string key = "buff")
            => ApplyOwnerActivation(new EquipmentActivation(null, new EquipmentAbility { target = TargetType.Owner }, effect, context, key + ":" + effect.effectStatType + ":" + effect.rule), false);
        private bool ApplyExtendedOwnerEffect(EquipmentActivation activation, bool permanent)
        {
            var effect = activation.Effect; var value = EffectValue(effect, activation.Context);
            switch (effect.effectType)
            {
                case EffectType.RestoreGuard: Player.RestoreShield(value); return true;
                case EffectType.RestoreMp: Player.RestoreMp(value); return true;
                case EffectType.RecoverFatigue: Fatigue.Recover(value,effect.travelGraceSteps);return true;
                case EffectType.Cleanse: Cleanse(); return true;
                case EffectType.Damage: Player.TakeEnvironmentalDamage(value); return true;
                case EffectType.ApplyStatus:
                    var status = CombatStatusCatalog.Instance?.Get(effect.status);
                    if (status != null) Buffs.Add(status, Mathf.Max(1, Mathf.RoundToInt(value)), this, activation.Source,
                        effect.status is StatusKind.Poison or StatusKind.Burn ? effect.statusDamage : 1,
                        duration: effect.effectTime > 0 ? effect.effectTime : -1, maximumStacks: effect.statusCap);
                    return true;
                case EffectType.ReduceSpellCooldown: ReduceSpellCooldowns(value); return true;
                case EffectType.ResetAttackCooldown: Player.ResetAttackCooldown(); return true;
                case EffectType.AddBuff: if(CanApplyBuff(effect.buff,activation.Context))Buffs.Add(effect.buff, Mathf.Max(0, value), this, activation.Source, context:activation.Context); return true;
                case EffectType.RemoveBuff: Buffs.Remove(effect.buff, effect.buffTag); return true;
                case EffectType.ChangeBuffStack: case EffectType.SetBuffStack:
                    if (effect.buff != null) Buffs.ChangeStack(effect.buff, value, effect.effectType == EffectType.SetBuffStack, this, activation.Source);
                    else Buffs.ChangeStack(activation.Context.Buff, value, effect.effectType == EffectType.SetBuffStack); return true;
                case EffectType.EmitSignal: EmitEvent(TriggerType.OnSignal, activation.Context.WithSignal(effect.buffTag)); return true;
                case EffectType.ActionDamage: if (activation.Context.Mutable != null) activation.Context.Mutable.DamageBonus += value; return true;
                case EffectType.ScaleActionDamage: if (activation.Context.Mutable != null) activation.Context.Mutable.DamageMultiplier *= value; return true;
                case EffectType.ActionExtraHits:
                    if (activation.Context.Mutable != null)
                    {
                        activation.Context.Mutable.ExtraHits += Mathf.RoundToInt(value);
                        if (effect.effectTime > 0) activation.Context.Mutable.HitInterval = effect.effectTime;
                    }
                    return true;
                case EffectType.ActionCharge: if (activation.Context.Mutable != null) activation.Context.Mutable.ForceCharge = true; return true;
                case EffectType.MarkAction: if (activation.Context.Mutable != null && effect.buffTag!=null) activation.Context.Mutable.Tags.Add(effect.buffTag); return true;
                case EffectType.GuardCost: if (activation.Context.Mutable != null) { activation.Context.Mutable.GuardCostMultiplier *= value; activation.Context.Mutable.GuardFeedback = effect.guardFeedback; } return true;
                case EffectType.PayDeferredDamage: Player.PayDeferredDamage(); return true;
                case EffectType.ClearDeferredDamage: Player.ClearDeferredDamage(); return true;
                case EffectType.StoreGuardDamage: Player.StoredGuardDamage += value; return true;
                case EffectType.LearnSpell:
                    if (effect.spell != null && RuntimeFor(effect.spell) == null && !LearnSpell(effect.spell)) PendingSpellOffers.Enqueue(effect.spell); return true;
                case EffectType.UnlockBuff: case EffectType.SummonCompanion: return true;
                case EffectType.RuleModifier: AddTransientBuff(effect, value, activation); return true;
                case EffectType.Heal: Player.Heal(value); return true;
                case EffectType.StatIncrease when !permanent && (effect.buffUses > 0 || !string.IsNullOrEmpty(effect.buffGroup)):
                    AddTransientBuff(effect, value, activation); return true;
                default: return false;
            }
        }
        private void AddTransientBuff(EquipmentEffect effect, float value, EquipmentActivation activation)
        {
            // Legacy API compatibility only. Production assets are migrated to named BuffData.
            var data = ScriptableObject.CreateInstance<BuffData>(); data.hideFlags = HideFlags.HideAndDontSave;
            data.name="runtime-effect-"+System.Guid.NewGuid().ToString("N");
            data.ReleaseData.Type = effect.effectTime > 0 ? BuffReleaseType.Time : BuffReleaseType.Stage; data.ReleaseData.Time = effect.effectTime;
            effect.effectMagnitude = value; effect.scaleRatio = 0; effect.effectCount = 1;
            data.Units.Add(new EquipmentAbility { trigger = new EquipmentTrigger { chancePercent = 100,  triggerType = TriggerType.Passive }, target = TargetType.OwnerStat, effects = new() { effect } });
            Buffs.Add(data, source: activation.Source);
        }
        public void CompleteAction(TriggerType type, EquipmentTriggerContext context)
        {
            if (type is not TriggerType.OnAttack and not TriggerType.OnSpellCast and not TriggerType.OnConsume) return;
            EmitEvent(TriggerType.OnActionCompleted, context);
            RecordAction(type, context);
            Awareness.CompleteAction(Awareness.IsConcealed ? UnityEngine.Random.value : 1f);
        }
        public void ClearAbilityCombatState()
        { Buffs.EndCombat(); PendingAbilityEffects.Clear(); ActionContext = default; ResetActionHistory(); foreach (var state in abilityStates) state.BeginCombat(); }
        internal void NotifyAbilityLifecycle(TriggerType type, EquipmentTriggerContext context)
        { foreach (var state in abilityStates.ToArray()) foreach (var activation in state.Trigger(this, type, context)) ExecuteActivation(activation, false); }
        public void ClearAbilityBuffs() => Buffs.Clear();
        public float BuffUses(string key) => Buffs.Active.FirstOrDefault(buff => buff.Data.name == key)?.StackCount ?? 0;
    }
}
