using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace DungeonSlash
{
    public sealed partial class CombatController
    {
        private readonly Dictionary<CompanionData, CompanionView> companionViews = new();
        private void RefreshCompanions()
        {
            if (run == null || playerSpawnRoot == null) return;
            foreach (var data in companionViews.Keys.ToArray()) if (!run.HasCompanion(data)) { if (companionViews[data] != null) Destroy(companionViews[data].gameObject); companionViews.Remove(data); }
            foreach (var data in run.Companions)
            {
                if (data.prefab == null || companionViews.TryGetValue(data, out var existing) && existing != null) continue;
                // The player framing anchor moves between navigation/combat. Companions
                // stand on the room floor, in its stable coordinate system instead.
                var view = Instantiate(data.prefab, playerSpawnRoot.parent);
                view.transform.SetSiblingIndex(playerSpawnRoot.GetSiblingIndex());
                view.Configure(data); companionViews[data] = view;
            }
        }
        private void ResolveQueuedAbilities(int depth)
        {
            if (run == null || depth > 8) return;
            var budget = 128;
            while (run.PendingAbilityEffects.Count > 0 && budget-- > 0)
            {
                var activation = run.PendingAbilityEffects.Dequeue(); var effect = activation.Effect; var context = activation.Context;
                var value = run.EffectValue(effect, context);
                var living = activeMonsters.Where(item => item.Runtime.IsAlive).ToArray();
                var original = activeMonsters.FirstOrDefault(item => item.Runtime == context.Target);
                var targets = effect.targets switch {
                    AbilityTarget.OtherEnemies => living.Where(item => item != original).ToArray(),
                    AbilityTarget.AllEnemies => living,
                    AbilityTarget.LowestHp => living.OrderBy(item => item.Runtime.CurrentHp).Take(1).ToArray(),
                    AbilityTarget.RandomEnemy => living.OrderBy(_ => spellRandom.Next()).Take(1).ToArray(),
                    _ => original != null ? new[] { original } : living.Take(1).ToArray() };
                if (effect.effectType == EffectType.EchoSpell && context.Spell != null) { StartCoroutine(EchoSpell(context.Spell, value, effect.effectTime,context.Segment.End!=context.Segment.Start?context.Segment:null)); continue; }
                if (effect.effectType == EffectType.ExtraAttack && context.HasAttack)
                {
                    for (var i = 0; i < Mathf.Max(1, effect.effectCount); i++)
                    {
                        var segment = RotateAtMidpoint(context.Segment, effect.effectAngle); attackLine?.ShowAdditional(segment, context.AttackType == TriggerAttackType.Charge);
                        ExecutePlayerAttack(segment, context.AttackType == TriggerAttackType.Charge, Mathf.Max(0, effect.effectMagnitude), TriggerAttackType.Additional, false, false, depth + 1);
                    }
                    continue;
                }
                if (effect.effectType == EffectType.SpreadStatus)
                {
                    foreach (var target in living.Where(item => item != original)) if(run.CanApplyBuff(effect.buff,context)){target.Runtime.NotifyAttacked();target.Runtime.Buffs.Add(effect.buff,Mathf.Max(1,Mathf.RoundToInt(value)),run,activation.Source,Player.AllDamageMultiplier);}
                    continue;
                }
                foreach (var target in targets)
                {
                    switch (effect.effectType)
                    {
                        case EffectType.AddBuff: if(effect.buff!=null && run.CanApplyBuff(effect.buff,context)){target.Runtime.NotifyAttacked();target.Runtime.Buffs.Add(effect.buff, Mathf.Max(1, Mathf.RoundToInt(value)), run, activation.Source, Player.AllDamageMultiplier);} break;
                        case EffectType.RemoveBuff: target.Runtime.Buffs.Remove(effect.buff, effect.buffTag); break;
                        case EffectType.Damage:
                            if (activation.Source != null && activation.Source.abilities.Count == 0) ApplyEquipmentDamage(target, effect);
                            else DealMagic(target, value * (effect.scale is EffectScale.NormalWeapon or EffectScale.ChargeWeapon or EffectScale.DamageDealt ? 1 : Player.AllDamageMultiplier), AbilityColor(effect.element));
                            break;
                        case EffectType.ElementDamage: DealMagic(target, value * Player.AllDamageMultiplier * Player.ElementMultiplier(effect.element), AbilityColor(effect.element)); break;
                        case EffectType.ApplyStatus:
                            target.Runtime.NotifyAttacked();
                            if (effect.status == StatusKind.Poison && Player.Rule(AbilityRule.BlockBuff) <= 0) target.Runtime.AddPoison(Mathf.RoundToInt(value), effect.statusDamage * Player.AllDamageMultiplier, effect.effectTime, Mathf.Max(1, effect.statusCap),run,activation.Source);
                            else if (effect.status == StatusKind.Slow) target.Runtime.Slow(effect.effectTime);
                            else if (effect.status == StatusKind.LightningRod) target.Runtime.LightningRodUntil = Player.CombatTime + effect.effectTime;
                            else if (effect.status == StatusKind.Burn) target.Runtime.AddBurn(effect.statusDamage * Player.AllDamageMultiplier, effect.effectTime,run,activation.Source);
                            break;
                        case EffectType.DetonatePoison: DealMagic(target, target.Runtime.DetonatePoison(value), AbilityColor(effect.element)); break;
                        case EffectType.Interrupt: target.Runtime.NotifyAttacked(); target.Runtime.Interrupt(value); break;
                    }
                    if (activation.Source != null)
                        foreach (var data in activation.Source.abilities.SelectMany(ability => ability.effects).Where(node => node.effectType == EffectType.SummonCompanion).Select(node => node.companion))
                            if (data != null && companionViews.TryGetValue(data, out var companion) && companion != null) companion.Attack(target.Position);
                }
            }
            if (budget <= 0) { run.PendingAbilityEffects.Clear(); Debug.LogWarning("Ability recursion budget reached; check trigger filters."); }
        }
        private static Color AbilityColor(SpellElement element) => element == SpellElement.Fire ? new Color(1, .5f, .2f) : element == SpellElement.Ice ? new Color(.4f, .9f, 1) : new Color(.9f, .8f, 1);
    }
}
