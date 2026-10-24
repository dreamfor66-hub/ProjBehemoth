using System.Collections;
using System.Linq;
using UnityEngine;

namespace DungeonSlash
{
    public sealed partial class CombatController
    {
        private void ApplySpellAbilities(SpellData spell, ActiveMonster target)
        {
            var context = new EquipmentTriggerContext(default, TriggerAttackType.None, TriggerAttackWay.None, false,
                target?.Runtime.IsStunned ?? false, target?.Runtime, spell: spell);
            foreach (var ability in spell.abilities)
                if (ability.Matches(run, context))
                    foreach (var effect in ability.effects)
                        run.ExecuteActivation(new EquipmentActivation(null, ability, effect, context, spell.GetInstanceID() + ":" + spell.abilities.IndexOf(ability)), false);
            ResolveQueuedAbilities(0);
            run.Buffs.UpdateState(0);
        }
        private IEnumerator SustainSpell(SpellData spell, Vector2 center, float multiplier)
        {
            var serial = encounterSerial;
            var interval = Mathf.Max(.05f, spell.pulseInterval);
            var count = spell.shape == SpellShape.Rain ? spell.pulses : Mathf.CeilToInt(spell.duration / interval);
            var context = new EquipmentTriggerContext(default, TriggerAttackType.None, TriggerAttackWay.None, false, false, spell: spell);
            var damage = spell.damage * multiplier * Player.AllDamageMultiplier * Player.ElementMultiplier(spell.element) * (1 + Player.Rule(AbilityRule.SpellDamageBonus, context));
            for (var i = 0; i < count && IsActive && serial == encounterSerial; i++)
            {
                attackLine?.ShowSpellArea(center, spell.radius, spell.Color);
                foreach (var target in activeMonsters.Where(m => m.Runtime.IsAlive && Vector2.Distance(m.Position, center) <= spell.radius + m.Runtime.Data.bodyHitRadius).ToArray())
                    HitSpell(target, spell, damage);
                if (activeMonsters.All(m => !m.Runtime.IsAlive)) { Finish(true); yield break; }
                yield return new WaitForSeconds(interval);
            }
        }
    }
}
