using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace DungeonSlash
{
    public readonly struct CharacterAttack
    {
        public readonly float Multiplier; public readonly bool Charged; public readonly int Hits; public readonly FormData Form;
        public readonly IReadOnlyCollection<BuffTagData> Tags;
        public bool Draw => Tags!=null && Tags.Count>0; // Presentation compatibility; mechanics use data tags.
        public CharacterAttack(float multiplier, bool charged, int hits, IReadOnlyCollection<BuffTagData> tags, FormData form) { Multiplier = multiplier; Charged = charged; Hits = hits; Tags=tags; Form = form; }
    }
    public sealed partial class PlayerCombatRuntime
    {
        internal RunState Owner;
        public PlayerCombatData Data => data;
        private BuffManager standaloneBuffs;
        public BuffManager Buffs => Owner?.Buffs ?? (standaloneBuffs ??= new BuffManager());
        public float CombatTime { get; private set; }
        public bool HoldingGuard { get; private set; }
        public float StoredGuardDamage { get; set; }
        public int GuardHitCount { get; private set; }
        public float DeferredDamage => Buffs.Active.Where(buff=>buff.Data==Owner?.RuleBuff(AbilityRule.DeferredDamageSeconds,true)).Sum(buff=>buff.Value);
        public FormData ActiveForm => Buffs?.Active.Select(b => b.Data.form).FirstOrDefault(form => form != null);
        public IReadOnlyList<SwipeDirection> Combo => combo;
        private readonly List<SwipeDirection> combo = new();
        private readonly Dictionary<CombatResourceData, float> resources = new();
        public float Rule(AbilityRule kind, EquipmentTriggerContext? context = null) => Owner?.Rule(kind, context) ?? 0;
        private float BuffStat(ModifierKind kind)=>Owner?.Stat(kind) ?? Buffs.Stat(kind);
        public bool HasPower(RelicPower power) => power != RelicPower.None && Owner != null && Owner.EquippedItems.Any(item => item.power == power);
        public float Resource(CombatResourceData resource) => resource != null && resources.TryGetValue(resource, out var value) ? value : resource != null ? resource.initialValue : 0;
        public void SetResource(CombatResourceData resource, float value) { if (resource != null) resources[resource] = Mathf.Clamp(value, 0, resource.maximum); }
        public void AddResource(CombatResourceData resource, float value) => SetResource(resource, Resource(resource) + value);
        public float EffectiveDamageReduction => Mathf.Clamp01(DamageReduction + Rule(AbilityRule.DamageReduction));
        public float SpellCooldownMultiplier => Mathf.Max(.2f, 1 - Rule(AbilityRule.SpellCooldownReduction));
        public float SpellCastingMultiplier => Mathf.Max(.15f, 1 - Rule(AbilityRule.CastTimeReduction));
        public float SpellCooldownFor(SpellData spell) => Mathf.Max(.2f, 1 - Rule(AbilityRule.SpellCooldownReduction, new EquipmentTriggerContext(default, TriggerAttackType.None, TriggerAttackWay.None, false, false, spell: spell)));
        public float SpellCastingFor(SpellData spell) => Mathf.Max(.15f, 1 - Rule(AbilityRule.CastTimeReduction, new EquipmentTriggerContext(default, TriggerAttackType.None, TriggerAttackWay.None, false, false, spell: spell)));
        private void ResetCharacter()
        {
            CombatTime = StoredGuardDamage = 0; GuardHitCount = 0; HoldingGuard = false; combo.Clear(); resources.Clear();
            foreach (var resource in data.resources) SetResource(resource, resource.initialValue);
        }
        public void BeginCharacterCombat() { ResetCharacter(); TrySetGuard(false); SetKeyboardGuard(false); SetChargeGuard(false); }
        public void EndCharacterCombat() { Owner?.Awareness.End(); resources.Clear(); combo.Clear(); TrySetGuard(false); SetKeyboardGuard(false); SetChargeGuard(false); Buffs.EndCombat(); }
        private void SetGuardIntent(bool enabled)
        {
            if (enabled == HoldingGuard) return; HoldingGuard = enabled;
            Owner?.EmitEvent(enabled ? TriggerType.OnGuardStarted : TriggerType.OnGuardEnded, default);
        }
        private void TickCharacter(float dt)
        {
            CombatTime += Mathf.Max(0, dt); if(Owner!=null)Owner.TickAbilities(dt);else Buffs.Tick(dt); if (!IsAlive) return;
            RestoreShield(Mathf.Max(0, Rule(AbilityRule.GuardRecoveryPerSecond)) * dt);
        }
        private void ApplyCharacterDamage(float damage)
        {
            if (damage <= 0) return; var definition=Owner?.RuleBuff(AbilityRule.DeferredDamageSeconds);
            if (definition!=null && Rule(AbilityRule.DeferredDamageSeconds)>0) Buffs.Add(definition,root:Owner,context:new EquipmentTriggerContext(default,TriggerAttackType.None,TriggerAttackWay.None,false,false,damage:damage),initialValue:damage);
            else CurrentHp = Mathf.Max(0, CurrentHp - damage);
        }
        public void PayDeferredDamage() => Buffs.Remove(Owner?.RuleBuff(AbilityRule.DeferredDamageSeconds,true));
        public void ClearDeferredDamage() => Buffs.EndCombat();
        public FormData PreparedForm => data.forms.Where(form => form != null && form.pattern.Length > 0 && (Owner == null ? !form.requiresUnlock : Owner.IsFormUnlocked(form)) && form.pattern.Length <= combo.Count)
            .Where(form => form.pattern.SequenceEqual(combo.Skip(combo.Count - form.pattern.Length))).OrderByDescending(form => form.pattern.Length).FirstOrDefault();
        public CharacterAttack PrepareAttack(SwipeDirection direction, bool charged, int chargeLevel = 1)
        {
            var form = charged ? PreparedForm : null;
            var way = direction is SwipeDirection.Left or SwipeDirection.Right ? TriggerAttackWay.Horizontal : direction == SwipeDirection.Up ? TriggerAttackWay.Upward : TriggerAttackWay.Downward;
            var vector = direction == SwipeDirection.Left ? Vector2.left : direction == SwipeDirection.Right ? Vector2.right : direction == SwipeDirection.Up ? Vector2.up : Vector2.down;
            var mutable = new ActionEffectContext();
            var context = new EquipmentTriggerContext(new AttackSegment(Vector2.zero, vector, 0), charged ? TriggerAttackType.Charge : TriggerAttackType.Normal, way, charged, false, form: form, mutable: mutable);
            Owner?.StartAction(context); Owner?.EmitEvent(TriggerType.OnActionPreparing, context);
            var resolvedCharge=charged||mutable.ForceCharge;
            context=new EquipmentTriggerContext(context.Segment,resolvedCharge?TriggerAttackType.Charge:TriggerAttackType.Normal,way,resolvedCharge,false,form:form,mutable:mutable);
            Owner?.SetPreparedAction(context);
            var multiplier = (charged && chargeLevel >= 2 ? 2f : 1) * mutable.DamageMultiplier * (1 + mutable.DamageBonus);
            var hits = 1 + mutable.ExtraHits;
            if (charged)
            {
                if (form != null)
                {
                    multiplier *= form.damageMultiplier; hits = Mathf.Max(1, form.hits + mutable.ExtraHits + Mathf.RoundToInt(Rule(AbilityRule.FormExtraHits, context)));
                    if (form.buff != null) Buffs.Add(form.buff);
                    foreach (var effect in form.effects) Owner?.ApplyAbilityEffect(effect, context, "form:" + form.name);
                    Owner?.EmitEvent(TriggerType.OnFormFinish, context);
                    RestoreShield(ShieldMax * Rule(AbilityRule.FormGuardFraction, context));
                }
                combo.Clear();
            }
            else if (data.forms.Any(f => f != null && f.pattern.Length > 0)) { combo.Add(direction); var length = Mathf.Max(2, data.forms.Max(f => f.pattern.Length)); while (combo.Count > length) combo.RemoveAt(0); }
            return new CharacterAttack(multiplier, resolvedCharge, hits, mutable.Tags.ToArray(), form);
        }
        public string StatusText
        {
            get
            {
                var labels = new List<string>();
                if (Owner?.Awareness.IsConcealed == true) labels.Add(GameText.Format("ui.stealth.status", Owner.Awareness.NextDetectionChance * 100));
                if (Buffs != null) labels.AddRange(Buffs.Active.Where(b => b.Data.showInHud).Select(b => b.Data.displayName));
                labels.AddRange(data.resources.Where(r => r.showInHud).Select(r => r.displayName + " " + Resource(r).ToString("0")));
                if (data.forms.Any(f => f != null && f.pattern.Length > 0)) labels.Add(GameText.Format("ui.charactercombatruntime.57448830", string.Join("", combo.Select(FormData.DirectionLabel)), PreparedForm != null ? PreparedForm.displayName + GameText.Get("ui.charactercombatruntime.c7a3a29b") : GameText.Get("ui.charactercombatruntime.e3f2e38")));
                if (labels.Count == 0 && HoldingGuard) labels.Add(Owner != null && Owner.Spellbook.Count > 0 ? GameText.Get("ui.charactercombatruntime.b0f8c0f9") : GameText.Get("ui.charactercombatruntime.aabc49f9"));
                return string.Join(" · ", labels);
            }
        }
    }
}
