using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace DungeonSlash
{
    public readonly struct CharacterAttack
    {
        public readonly float Multiplier; public readonly bool Charged; public readonly int Hits; public readonly bool Draw; public readonly MonkFormData Form;
        public CharacterAttack(float multiplier, bool charged, int hits, bool draw, MonkFormData form) { Multiplier = multiplier; Charged = charged; Hits = hits; Draw = draw; Form = form; }
    }
    public sealed partial class PlayerCombatRuntime
    {
        internal RunState Owner;
        public PlayerCombatData Data => data;
        public StartingMechanic Mechanic => data.startingRelic != null ? data.startingRelic.startingMechanic : StartingMechanic.None;
        public float CombatTime { get; private set; }
        public float Rage { get; private set; }
        public bool IsEnraged { get; private set; }
        public bool IsSheathed { get; private set; }
        public bool ParryDrawReady { get; private set; }
        public bool HoldingGuard { get; private set; }
        public float StoredGuardDamage { get; set; }
        public int GuardHitCount { get; private set; }
        public int FlowCharges => Owner?.BuffUses("flow") ?? 0;
        public float SheathElapsed => IsSheathed ? Mathf.Max(0, CombatTime - sheathStarted) : 0;
        public float DeferredDamage => debts.Sum(debt => debt.remaining);
        public IReadOnlyList<SwipeDirection> Combo => combo;
        private readonly List<SwipeDirection> combo = new();
        private readonly List<Debt> debts = new();
        private sealed class Debt { public float remaining, time; }
        private float guardStarted = -100f, sheathStarted;
        private int consecutiveHorizontal;
        public float Rule(AbilityRule kind, EquipmentTriggerContext? context = null) => Owner?.Rule(kind, context) ?? 0;
        // Ownership query retained for migration/test tooling only. Combat never dispatches by this enum.
        public bool HasPower(RelicPower power) => power != RelicPower.None && Owner != null && Owner.EquippedItems.Any(item => item.power == power);
        private float RageDamageMultiplier => IsEnraged ? data.mechanics.enragedDamageMultiplier : 1f;
        private float RageSpeedMultiplier => IsEnraged ? data.mechanics.enragedCooldownMultiplier : 1f;
        private float FlowSpeedMultiplier => Mathf.Max(.2f, 1f - Mathf.Min(Rule(AbilityRule.HorizontalCooldownCap), consecutiveHorizontal * Rule(AbilityRule.HorizontalCooldownPerHit)));
        public float EffectiveDamageReduction => Mathf.Clamp01(DamageReduction + (IsEnraged ? data.mechanics.enragedDamageReduction : 0) + Rule(AbilityRule.DamageReduction));
        public bool IsPerfectWindow => HoldingGuard && Rule(AbilityRule.PerfectGuardWindow) > 0 && CombatTime - guardStarted <= Rule(AbilityRule.PerfectGuardWindow);
        public bool IsSheathParryWindow => Mechanic == StartingMechanic.SamuraiDraw && IsSheathed && HoldingGuard && CombatTime - guardStarted <= data.mechanics.sheathParrySeconds + Rule(AbilityRule.SheathParryWindowBonus);
        public float SpellCooldownMultiplier => (Mechanic == StartingMechanic.WizardCasting ? data.mechanics.wizardCooldownMultiplier : 1) * Mathf.Max(.2f, 1 - Rule(AbilityRule.SpellCooldownReduction));
        public float SpellCastingMultiplier => Mathf.Max(.15f, 1 - Rule(AbilityRule.CastTimeReduction));
        public float SpellCooldownFor(SpellData spell) => (Mechanic == StartingMechanic.WizardCasting ? data.mechanics.wizardCooldownMultiplier : 1) * Mathf.Max(.2f, 1 - Rule(AbilityRule.SpellCooldownReduction, new EquipmentTriggerContext(default, TriggerAttackType.None, TriggerAttackWay.None, false, false, spell: spell)));
        public float SpellCastingFor(SpellData spell) => Mathf.Max(.15f, 1 - Rule(AbilityRule.CastTimeReduction, new EquipmentTriggerContext(default, TriggerAttackType.None, TriggerAttackWay.None, false, false, spell: spell)));
        private void ResetCharacter()
        { CombatTime = Rage = StoredGuardDamage = 0; GuardHitCount = consecutiveHorizontal = 0; IsEnraged = IsSheathed = ParryDrawReady = HoldingGuard = false; combo.Clear(); debts.Clear(); guardStarted = -100; }
        public void BeginCharacterCombat() { ResetCharacter(); TrySetGuard(false); SetKeyboardGuard(false); SetChargeGuard(false); }
        public void EndCharacterCombat() { debts.Clear(); IsEnraged = false; Rage = 0; combo.Clear(); TrySetGuard(false); SetKeyboardGuard(false); SetChargeGuard(false); Owner?.ClearAbilityBuffs(); }
        private void SetGuardIntent(bool enabled)
        {
            if (enabled == HoldingGuard) return; HoldingGuard = enabled; if (!enabled) return;
            guardStarted = CombatTime;
            if (Mechanic == StartingMechanic.SamuraiDraw)
            { if (!IsSheathed) { IsSheathed = true; sheathStarted = CombatTime; } else guardStarted = -100; }
        }
        private void OnPerfectGuard(EquipmentTriggerContext context) => Owner?.EmitEvent(TriggerType.OnPerfectGuard, context);
        private void OnSheathParry(EquipmentTriggerContext context) { ParryDrawReady = true; Owner?.EmitEvent(TriggerType.OnSheathParry, context); }
        private void TickCharacter(float dt)
        {
            CombatTime += Mathf.Max(0, dt); Owner?.TickAbilities(dt); if (!IsAlive) return;
            if (HoldingGuard && Mechanic == StartingMechanic.WizardCasting && Owner != null && Owner.Spellbook.Count > 0) RestoreShield(data.mechanics.castingGuardRegen * dt);
            if (Mechanic == StartingMechanic.BarbarianRage)
            {
                Rage = Mathf.Max(0, Rage - dt * (IsEnraged ? Mathf.Max(0, data.mechanics.rageDrainPerSecond - Rule(AbilityRule.RageDrainReduction)) : data.mechanics.rageIdleDecay));
                if (IsEnraged && Rage <= 0) { IsEnraged = false; CurrentHp = Mathf.Max(0, CurrentHp - DeferredDamage); debts.Clear(); }
            }
            for (var i = debts.Count - 1; i >= 0; i--)
            {
                var debt = debts[i]; var paid = debt.remaining * Mathf.Clamp01(dt / Mathf.Max(.001f, debt.time)); debt.remaining -= paid; debt.time -= dt;
                CurrentHp = Mathf.Max(0, CurrentHp - paid); if (debt.time <= 0) debts.RemoveAt(i);
            }
        }
        private void ApplyCharacterDamage(float damage)
        {
            if (damage <= 0) return; var delay = Rule(AbilityRule.DeferredDamageSeconds);
            if (delay > 0) debts.Add(new Debt { remaining = damage, time = delay }); else CurrentHp = Mathf.Max(0, CurrentHp - damage);
        }
        public void AddRage(float value) { if (Mechanic == StartingMechanic.BarbarianRage) Rage = Mathf.Clamp(Rage + value, 0, 100); }
        public MonkFormData PreparedForm => data.monkForms.Where(form => form != null && (Owner == null ? !form.requiresUnlock : Owner.IsFormUnlocked(form)) && form.pattern.Length <= combo.Count)
            .Where(form => form.pattern.SequenceEqual(combo.Skip(combo.Count - form.pattern.Length))).OrderByDescending(form => form.pattern.Length).FirstOrDefault();
        public CharacterAttack PrepareAttack(SwipeDirection direction, bool charged, int chargeLevel = 1)
        {
            var multiplier = charged && chargeLevel >= 2 ? 2f : 1;
            var draw = Mechanic == StartingMechanic.SamuraiDraw && IsSheathed; var actualCharge = charged || (draw && ParryDrawReady);
            var form = Mechanic == StartingMechanic.MonkForms && charged ? PreparedForm : null;
            var way = direction is SwipeDirection.Left or SwipeDirection.Right ? TriggerAttackWay.Horizontal : direction == SwipeDirection.Up ? TriggerAttackWay.Upward : TriggerAttackWay.Downward;
            var vector = direction == SwipeDirection.Left ? Vector2.left : direction == SwipeDirection.Right ? Vector2.right : direction == SwipeDirection.Up ? Vector2.up : Vector2.down;
            var context = new EquipmentTriggerContext(new AttackSegment(Vector2.zero, vector, 0), actualCharge ? TriggerAttackType.Charge : TriggerAttackType.Normal, way, actualCharge, false, form: form, draw: draw);
            Owner?.StartAction(context);
            if (Mechanic == StartingMechanic.BarbarianRage)
            {
                if (!charged && !IsEnraged) AddRage(data.mechanics.ragePerNormalAttack + Rule(AbilityRule.RageGenerationBonus, context));
                else if (charged && !IsEnraged && Rage >= data.mechanics.rageThreshold) { IsEnraged = true; Rage = 100; }
            }
            var hits = 1;
            if (draw) { multiplier *= data.mechanics.drawDamageMultiplier + Rule(AbilityRule.DrawDamageBonus, context); hits += Mathf.RoundToInt(Rule(AbilityRule.DrawExtraHits, context)); IsSheathed = ParryDrawReady = false; }
            if (Mechanic == StartingMechanic.MonkForms)
            {
                if (charged)
                {
                    if (form != null)
                    {
                        multiplier *= form.damageMultiplier; hits = Mathf.Max(1, form.hits + Mathf.RoundToInt(Rule(AbilityRule.FormExtraHits, context)));
                        foreach (var effect in form.effects) Owner?.ApplyAbilityEffect(effect, context, "form:" + form.name);
                        Owner?.EmitEvent(TriggerType.OnFormFinish, context);
                        RestoreShield(ShieldMax * Rule(AbilityRule.FormGuardFraction, context));
                    }
                    combo.Clear();
                }
                else { combo.Add(direction); while (combo.Count > 5) combo.RemoveAt(0); }
            }
            if (!charged) consecutiveHorizontal = direction is SwipeDirection.Left or SwipeDirection.Right ? consecutiveHorizontal + 1 : 0;
            return new CharacterAttack(multiplier, actualCharge, hits, draw, form);
        }
        public string StatusText
        {
            get
            {
                if (Mechanic == StartingMechanic.BarbarianRage) return GameText.Format("ui.charactercombatruntime.d6b28340", IsEnraged ? GameText.Get("ui.charactercombatruntime.19ed98b1") : GameText.Get("ui.charactercombatruntime.4676f734"), Rage) + (DeferredDamage > 0 ? GameText.Format("ui.charactercombatruntime.2cfc90f3", DeferredDamage) : "");
                if (Mechanic == StartingMechanic.SamuraiDraw) return ParryDrawReady ? GameText.Get("ui.charactercombatruntime.4c2f512d") : IsSheathed ? GameText.Get("ui.charactercombatruntime.b8ce3228") : GameText.Get("ui.charactercombatruntime.c088ba75");
                if (Mechanic == StartingMechanic.MonkForms) return GameText.Format("ui.charactercombatruntime.57448830", string.Join("", combo.Select(MonkFormData.DirectionLabel)), PreparedForm != null ? PreparedForm.displayName + GameText.Get("ui.charactercombatruntime.c7a3a29b") : GameText.Get("ui.charactercombatruntime.e3f2e38"));
                return HoldingGuard ? Owner != null && Owner.Spellbook.Count > 0 ? GameText.Get("ui.charactercombatruntime.b0f8c0f9") : GameText.Get("ui.charactercombatruntime.aabc49f9") : "";
            }
        }
    }
}
