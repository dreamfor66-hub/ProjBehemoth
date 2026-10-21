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
        private int seen, activations;
        private float elapsed, nextAllowed;
        public bool HasReachedMaximum => Ability.trigger.triggerMaxCount > 0 && activations >= Ability.trigger.triggerMaxCount;
        public AbilityTriggerState(EquipmentData source, EquipmentAbility ability, int index) { Source = source; Ability = ability; Index = index; }
        public void BeginCombat() { elapsed = nextAllowed = 0; if (Ability.trigger.resetCountEachCombat) seen = activations = 0; }
        public bool TimerReady(float dt) { elapsed += dt; if (elapsed < Mathf.Max(.05f, Ability.trigger.intervalSeconds)) return false; elapsed %= Mathf.Max(.05f, Ability.trigger.intervalSeconds); return true; }
        public IEnumerable<EquipmentActivation> Trigger(RunState run, TriggerType type, EquipmentTriggerContext context)
        {
            var trigger = Ability.trigger;
            if (trigger.triggerType != type || !Ability.Matches(run, context) || run.Player.CombatTime < nextAllowed) yield break;
            if (trigger.triggerMaxCount > 0 && activations >= trigger.triggerMaxCount) yield break;
            if (context.AttackType == TriggerAttackType.Additional && !trigger.includeAdditionalAttacks) yield break;
            if (context.HasAttack)
            {
                var attack = context.AttackType == TriggerAttackType.Charge ? TriggerAttackTypeFilter.ChargeAttack : context.AttackType == TriggerAttackType.Normal ? TriggerAttackTypeFilter.NormalAttack : TriggerAttackTypeFilter.AdditionalAttack;
                var way = context.AttackWay == TriggerAttackWay.Upward ? TriggerAttackWayFilter.Upward : context.AttackWay == TriggerAttackWay.Downward ? TriggerAttackWayFilter.Downward : TriggerAttackWayFilter.Horizontal;
                if (trigger.triggerAttackTypeFilter != 0 && (trigger.triggerAttackTypeFilter & attack) == 0) yield break;
                if (trigger.triggerAttackWayFilter != 0 && (trigger.triggerAttackWayFilter & way) == 0) yield break;
            }
            if (++seen % Mathf.Max(1, trigger.triggerCount) != 0) yield break;
            activations++; nextAllowed = run.Player.CombatTime + trigger.cooldownSeconds;
            for (var i = 0; i < Ability.effects.Count; i++)
                yield return new EquipmentActivation(Source, Ability, Ability.effects[i], context, Source.GetInstanceID() + ":" + Index + ":" + i);
        }
    }
    public sealed partial class RunState
    {
        private readonly List<AbilityTriggerState> abilityStates = new();
        public Queue<EquipmentActivation> PendingAbilityEffects { get; } = new();
        public EquipmentTriggerContext ActionContext { get; private set; }
        public int ActionSerial { get; private set; }
        private readonly List<AbilityBuff> abilityBuffs = new();
        private sealed class AbilityBuff { public EquipmentEffect effect; public string key; public float expires; public int uses, created, sourceId; }
        public void StartAction(EquipmentTriggerContext context) { ActionSerial++; ActionContext = context; }
        public float Rule(AbilityRule kind, EquipmentTriggerContext? supplied = null)
        {
            var context = supplied ?? ActionContext; var value = 0f;
            foreach (var source in EquippedItems)
                foreach (var ability in source.abilities)
                    if (ability.trigger.triggerType == TriggerType.Passive && ability.Matches(this, context))
                        foreach (var effect in ability.effects) if (effect.effectType == EffectType.RuleModifier && effect.rule == kind) value += EffectValue(effect, context);
            foreach (var buff in abilityBuffs) if (buff.effect.effectType == EffectType.RuleModifier && buff.effect.rule == kind) value += buff.effect.effectMagnitude;
            return value;
        }
        public bool HasCompanion(CompanionData data) => EquippedItems.SelectMany(item => item.abilities).SelectMany(ability => ability.effects).Any(effect => effect.effectType == EffectType.SummonCompanion && effect.companion == data);
        public IEnumerable<CompanionData> Companions => EquippedItems.SelectMany(item => item.abilities).SelectMany(ability => ability.effects).Where(effect => effect.effectType == EffectType.SummonCompanion && effect.companion != null).Select(effect => effect.companion).Distinct();
        public bool IsFormUnlocked(MonkFormData form) => !form.requiresUnlock || EquippedItems.SelectMany(item => item.abilities).SelectMany(ability => ability.effects).Any(effect => effect.effectType == EffectType.UnlockForm && effect.form == form);
        public float EffectValue(EquipmentEffect effect, EquipmentTriggerContext context)
        {
            var scale = effect.scale switch {
                EffectScale.NormalWeapon => Player.NormalAttackDamage, EffectScale.ChargeWeapon => Player.ChargeAttackDamage,
                EffectScale.DamageDealt => context.Damage, EffectScale.GuardMaximum => Player.ShieldMax, EffectScale.GuardCurrent => Player.ShieldCurrent,
                EffectScale.Rage => Player.Rage, EffectScale.Level => Level, EffectScale.PoisonStacks => context.Target?.PoisonStacks ?? 0,
                EffectScale.TargetMaxHp => context.Target?.MaxHp ?? 0, EffectScale.EnemyCount => context.EnemyCount, EffectScale.GuardDamage => context.GuardDamage, EffectScale.SheathTime => Player.SheathElapsed, _ => 0 };
            var value = (effect.effectMagnitude + effect.scaleRatio * scale) * Mathf.Max(1, effect.effectCount);
            return effect.maximumMagnitude > 0 ? Mathf.Min(value, effect.maximumMagnitude) : value;
        }
        private void RegisterAbilities(EquipmentData item)
        { for (var i = 0; i < item.abilities.Count; i++) abilityStates.Add(new AbilityTriggerState(item, item.abilities[i], i)); }
        public void EmitEvent(TriggerType type, EquipmentTriggerContext context)
        {
            foreach (var activation in TriggerEquipment(type, context))
                if (activation.Target == TargetType.Owner) ApplyOwnerActivation(activation, type == TriggerType.OnAcquire || type == TriggerType.OnCombatWon);
                else PendingAbilityEffects.Enqueue(activation);
        }
        public void TickAbilities(float dt)
        {
            TickAbilityBuffs();
            foreach (var state in abilityStates.Where(state => state.Ability.trigger.triggerType == TriggerType.OnTimer))
                if (state.TimerReady(dt)) foreach (var activation in state.Trigger(this, TriggerType.OnTimer, EquipmentTriggerContext.Simple()))
                    if (activation.Target == TargetType.Owner) ApplyOwnerActivation(activation, false); else PendingAbilityEffects.Enqueue(activation);
        }
        public void ApplyAbilityEffect(EquipmentEffect effect, EquipmentTriggerContext context, string key = "form")
            => ApplyOwnerActivation(new EquipmentActivation(null, new EquipmentAbility { target = TargetType.Owner }, effect, context, key + ":" + effect.effectStatType + ":" + effect.rule), false);
        private bool ApplyExtendedOwnerEffect(EquipmentActivation activation, bool permanent)
        {
            var effect = activation.Effect; var value = EffectValue(effect, activation.Context);
            switch (effect.effectType)
            {
                case EffectType.RestoreGuard: Player.RestoreShield(value); return true;
                case EffectType.ReduceSpellCooldown: ReduceSpellCooldowns(value); return true;
                case EffectType.ResetAttackCooldown: Player.ResetAttackCooldown(); return true;
                case EffectType.AddRage: Player.AddRage(value); return true;
                case EffectType.StoreGuardDamage: Player.StoredGuardDamage += value; return true;
                case EffectType.LearnSpell:
                    if (effect.spell != null && RuntimeFor(effect.spell) == null && !LearnSpell(effect.spell)) PendingSpellOffers.Enqueue(effect.spell); return true;
                case EffectType.UnlockForm: case EffectType.SummonCompanion: return true;
                case EffectType.RuleModifier: AddAbilityBuff(effect, value, activation); return true;
                case EffectType.Heal: Player.Heal(value); return true;
                case EffectType.StatIncrease when !permanent && (effect.buffUses > 0 || !string.IsNullOrEmpty(effect.buffGroup)):
                    AddAbilityBuff(effect, value, activation); return true;
                default: return false;
            }
        }
        private void AddAbilityBuff(EquipmentEffect effect, float value, EquipmentActivation activation)
        {
            var key = string.IsNullOrEmpty(effect.buffGroup) ? activation.BuffKey : effect.buffGroup;
            for (var i = abilityBuffs.Count - 1; i >= 0; i--) if (abilityBuffs[i].key == key) RemoveBuffAt(i);
            var uses = effect.buffUses;
            if (uses > 0) { value += Rule(AbilityRule.BuffPotencyBonus, activation.Context); uses += Mathf.RoundToInt(Rule(AbilityRule.BuffExtraUses, activation.Context)); }
            effect.effectMagnitude = value; effect.scaleRatio = 0; effect.effectCount = 1;
            abilityBuffs.Add(new AbilityBuff { key = key, effect = effect, uses = uses, expires = effect.effectTime > 0 ? Player.CombatTime + effect.effectTime : float.PositiveInfinity, created = ActionSerial, sourceId = activation.Source != null ? activation.Source.GetInstanceID() : 0 });
            if (effect.effectType == EffectType.StatIncrease) Player.ApplyModifiers(new[] { new StatModifier { kind = effect.effectStatType, value = value } });
        }
        private void RemoveBuffAt(int index)
        {
            var effect = abilityBuffs[index].effect;
            if (effect.effectType == EffectType.StatIncrease) Player.RemoveModifiers(new[] { new StatModifier { kind = effect.effectStatType, value = effect.effectMagnitude } });
            abilityBuffs.RemoveAt(index);
        }
        public void CompleteAction(TriggerType type, EquipmentTriggerContext context)
        {
            for (var i = abilityBuffs.Count - 1; i >= 0; i--)
            {
                var buff = abilityBuffs[i]; if (buff.uses <= 0 || buff.effect.consumeOn != type) continue;
                if ((type is TriggerType.OnAttack or TriggerType.OnSpellCast) && buff.created >= ActionSerial) continue;
                var filter = buff.effect.consumeAttackTypes;
                if (type == TriggerType.OnAttack && filter != 0 && (filter & (context.AttackType == TriggerAttackType.Charge ? TriggerAttackTypeFilter.ChargeAttack : TriggerAttackTypeFilter.NormalAttack)) == 0) continue;
                if (--buff.uses == 0) RemoveBuffAt(i);
            }
        }
        private void TickAbilityBuffs() { for (var i = abilityBuffs.Count - 1; i >= 0; i--) if (Player.CombatTime >= abilityBuffs[i].expires) RemoveBuffAt(i); }
        public void ClearAbilityCombatState()
        { ClearAbilityBuffs(); PendingAbilityEffects.Clear(); ActionContext = default; foreach (var state in abilityStates) state.BeginCombat(); }
        public void ClearAbilityBuffs() { for (var i = abilityBuffs.Count - 1; i >= 0; i--) RemoveBuffAt(i); }
        public int BuffUses(string key) => abilityBuffs.FirstOrDefault(buff => buff.key == key)?.uses ?? 0;
    }
}
