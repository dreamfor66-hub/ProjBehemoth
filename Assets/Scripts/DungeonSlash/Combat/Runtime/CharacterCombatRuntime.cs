using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace DungeonSlash
{
    public readonly struct CharacterAttack
    {
        public readonly float Multiplier; public readonly bool Charged; public readonly int Hits; public readonly BuffData AttackBuff;
        public readonly IReadOnlyCollection<BuffTagData> Tags;
        public readonly string FeedbackKey;
        public readonly float HitInterval;
        public bool Draw => Tags != null && Tags.Count > 0;
        public CharacterAttack(float multiplier, bool charged, int hits, IReadOnlyCollection<BuffTagData> tags, BuffData attackBuff,string feedbackKey=null,float hitInterval=.12f)
        { FeedbackKey=feedbackKey; HitInterval=Mathf.Max(.04f,hitInterval); Multiplier = multiplier; Charged = charged; Hits = hits; Tags = tags; AttackBuff = attackBuff; }
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
        public float DeferredDamage => Buffs.Active.Where(buff => buff.Data == Owner?.RuleBuff(AbilityRule.DeferredDamageSeconds, true)).Sum(buff => buff.Value);
        public IReadOnlyList<SwipeDirection> Combo => combo;
        private readonly List<SwipeDirection> combo = new();
        public float Rule(AbilityRule kind, EquipmentTriggerContext? context = null) => Owner?.Rule(kind, context) ?? 0;
        private readonly Dictionary<ModifierKind, float> extraPermanentStats = new();
        private float BuffStat(ModifierKind kind) => (extraPermanentStats.TryGetValue(kind, out var extra) ? extra : 0) + (Owner?.Stat(kind) ?? Buffs.Stat(kind));
        public bool HasPower(RelicPower power) => power != RelicPower.None && Owner != null && Owner.EquippedItems.Any(item => item.power == power);
        public float EffectiveDamageReduction => Mathf.Clamp01(DamageReduction + Rule(AbilityRule.DamageReduction));
        public float SpellCooldownMultiplier => Mathf.Max(.2f, 1 - Rule(AbilityRule.SpellCooldownReduction));
        public float SpellCastingMultiplier => Mathf.Max(.15f, 1 - Rule(AbilityRule.CastTimeReduction));
        public float SpellCooldownFor(SpellData spell) => Mathf.Max(.2f, 1 - Rule(AbilityRule.SpellCooldownReduction, new EquipmentTriggerContext(default, TriggerAttackType.None, TriggerAttackWay.None, false, false, spell: spell)));
        public float SpellCastingFor(SpellData spell) => Mathf.Max(.15f, 1 - Rule(AbilityRule.CastTimeReduction, new EquipmentTriggerContext(default, TriggerAttackType.None, TriggerAttackWay.None, false, false, spell: spell)));
        private void ResetCharacter()
        { CombatTime = StoredGuardDamage = 0; GuardHitCount = 0; HoldingGuard = false; combo.Clear(); }
        public void BeginCharacterCombat() { ResetCharacter(); RefillMp(); TrySetGuard(false); SetKeyboardGuard(false); SetChargeGuard(false); }
        public void EndCharacterCombat() { Owner?.Awareness.End(); combo.Clear(); TrySetGuard(false); SetKeyboardGuard(false); SetChargeGuard(false); Buffs.EndCombat(); }
        private void SetGuardIntent(bool enabled)
        {
            if (enabled == HoldingGuard) return; HoldingGuard = enabled;
            Owner?.EmitEvent(enabled ? TriggerType.OnGuardStarted : TriggerType.OnGuardEnded, default);
        }
        private void TickCharacter(float dt)
        {
            CombatTime += Mathf.Max(0, dt); if (Owner != null) Owner.TickAbilities(dt); else Buffs.Tick(dt); if (!IsAlive) return;
            RestoreMp((data.mpRegen + BuffStat(ModifierKind.MpRegen)) * dt);
            RestoreShield(Mathf.Max(0, Rule(AbilityRule.GuardRecoveryPerSecond)) * dt);
        }
        private void ApplyCharacterDamage(float damage)
        {
            if (damage <= 0) return; var definition = Owner?.RuleBuff(AbilityRule.DeferredDamageSeconds);
            if (definition != null && Rule(AbilityRule.DeferredDamageSeconds) > 0) Buffs.Add(definition, root: Owner, context: new EquipmentTriggerContext(default, TriggerAttackType.None, TriggerAttackWay.None, false, false, damage: damage), initialValue: damage);
            else CurrentHp = Mathf.Max(0, CurrentHp - damage);
        }
        public void PayDeferredDamage() => Buffs.Remove(Owner?.RuleBuff(AbilityRule.DeferredDamageSeconds, true));
        public void ClearDeferredDamage() => Buffs.EndCombat();
        public BuffData PreparedAttackBuff => data.attackBuffs
            .Where(buff => buff != null && buff.attackPattern.Length > 0 && (Owner == null ? !buff.requiresUnlock : Owner.IsBuffUnlocked(buff)) && buff.attackPattern.Length <= combo.Count)
            .Where(buff => buff.attackPattern.SequenceEqual(combo.Skip(combo.Count - buff.attackPattern.Length)))
            .OrderByDescending(buff => buff.attackPattern.Length).FirstOrDefault();
        public CharacterAttack PrepareAttack(SwipeDirection direction, bool charged, int chargeLevel = 1)
        {
            // Commit the previous action's queued changes before evaluating the next action.
            Buffs.UpdateState(0);
            var feedbackKey=Buffs.Active.Where(b=>!string.IsNullOrEmpty(b.Data.attackFeedbackKey)).OrderByDescending(b=>b.Data.attackFeedbackPriority).Select(b=>b.Data.attackFeedbackKey).FirstOrDefault();
            var attackBuff = charged ? PreparedAttackBuff : null;
            var way = direction is SwipeDirection.Left or SwipeDirection.Right ? TriggerAttackWay.Horizontal : direction == SwipeDirection.Up ? TriggerAttackWay.Upward : TriggerAttackWay.Downward;
            var vector = direction == SwipeDirection.Left ? Vector2.left : direction == SwipeDirection.Right ? Vector2.right : direction == SwipeDirection.Up ? Vector2.up : Vector2.down;
            var mutable = new ActionEffectContext();
            var context = new EquipmentTriggerContext(new AttackSegment(Vector2.zero, vector, 0), charged ? TriggerAttackType.Charge : TriggerAttackType.Normal, way, charged, false, attackBuff: attackBuff, mutable: mutable);
            Owner?.StartAction(context);
            if (attackBuff != null) { Buffs.Add(attackBuff, root: Owner, context: context); Buffs.UpdateState(0); }
            Owner?.EmitEvent(TriggerType.OnActionPreparing, context);
            // Ordered AddBuff(Rage = Fury stacks), RemoveBuff(Fury) is visible to this very attack.
            Buffs.UpdateState(0);
            var resolvedCharge = charged || mutable.ForceCharge;
            context = new EquipmentTriggerContext(context.Segment, resolvedCharge ? TriggerAttackType.Charge : TriggerAttackType.Normal, way, resolvedCharge, false, attackBuff: attackBuff, mutable: mutable);
            Owner?.SetPreparedAction(context);
            var multiplier = (charged && chargeLevel >= 2 ? 2f : 1) * mutable.DamageMultiplier * (1 + mutable.DamageBonus);
            if (charged)
            {
                if (attackBuff != null)
                {
                    Owner?.EmitEvent(TriggerType.OnAttackBuffUsed, context);
                    RestoreShield(ShieldMax * Rule(AbilityRule.AttackBuffGuardFraction, context));
                }
                combo.Clear();
            }
            else if (data.attackBuffs.Any(buff => buff != null && buff.attackPattern.Length > 0))
            {
                combo.Add(direction); var length = Mathf.Max(2, data.attackBuffs.Max(buff => buff.attackPattern.Length));
                while (combo.Count > length) combo.RemoveAt(0);
            }
            return new CharacterAttack(multiplier, resolvedCharge, Mathf.Max(1, 1 + mutable.ExtraHits), mutable.Tags.ToArray(), attackBuff,feedbackKey,mutable.HitInterval);
        }
        public string StatusText
        {
            get
            {
                var labels = new List<string>();
                if (Owner?.Awareness.IsConcealed == true) labels.Add(GameText.Format("ui.stealth.status", Owner.Awareness.NextDetectionChance * 100));
                labels.AddRange(Buffs.Active.Where(buff => buff.Data.showInHud || buff.Data.harmful).Select(buff => buff.MaximumStacks > 1
                    ? GameText.Format("ui.buff.stacks", buff.Data.displayName, buff.StackCount, buff.MaximumStacks) : buff.Data.displayName));
                if (data.attackBuffs.Any(buff => buff != null && buff.attackPattern.Length > 0))
                    labels.Add(GameText.Format("ui.charactercombatruntime.57448830", string.Join("", combo.Select(SwipeDirectionUtility.Label)), PreparedAttackBuff != null ? "<color=#7CA7A3>["+PreparedAttackBuff.displayName+"]</color>" : ""));
                if (labels.Count == 0 && HoldingGuard) labels.Add(GameText.Get("ui.charactercombatruntime.aabc49f9"));
                return string.Join(" · ", labels);
            }
        }
    }
}
