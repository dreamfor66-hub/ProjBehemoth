using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonSlash
{
    public sealed partial class RunState
    {
        public int WeaponActionsThisCombat { get; private set; }
        public int ActionHits { get; internal set; }
        public BuffData PreviousAttackBuff { get; private set; }
        public SpellData PreviousSpell { get; private set; }
        private readonly HashSet<SpellElement> elementSequence = new();
        private readonly HashSet<BuffData> techniqueSequence = new();
        private readonly Dictionary<BuffTagData, float> tagLastUsed = new();
        private readonly Dictionary<BuffData, (float elapsed, float stacks)> actionStartBuffs = new();
        private void SnapshotActionBuffs() { ActionHits = 0; actionStartBuffs.Clear(); foreach (var buff in Buffs.Active) actionStartBuffs[buff.Data] = (buff.ElapsedTime, buff.StackCount); }
        public float ActionStartBuffElapsed(BuffData buff) => buff != null && actionStartBuffs.TryGetValue(buff, out var value) ? value.elapsed : 0;
        public float ActionStartBuffStacks(BuffData buff) => buff != null && actionStartBuffs.TryGetValue(buff, out var value) ? value.stacks : 0;
        public int NextDistinctElements(SpellData spell) => spell == null ? 0 : elementSequence.Contains(spell.element) ? 1 : elementSequence.Count + 1;
        public int NextDistinctTechniques(BuffData buff) => buff == null ? 0 : techniqueSequence.Contains(buff) ? 1 : techniqueSequence.Count + 1;
        public float SinceTag(BuffTagData tag) => tag != null && tagLastUsed.TryGetValue(tag, out var time) ? Player.CombatTime - time : float.PositiveInfinity;
        private void ResetActionHistory()
        {
            WeaponActionsThisCombat = 0; PreviousAttackBuff = null; PreviousSpell = null;
            elementSequence.Clear(); techniqueSequence.Clear(); tagLastUsed.Clear();
        }
        private void RecordAction(TriggerType type, EquipmentTriggerContext context)
        {
            if (type == TriggerType.OnAttack)
            {
                WeaponActionsThisCombat++;
                if (context.AttackBuff != null)
                {
                    if (techniqueSequence.Contains(context.AttackBuff) || techniqueSequence.Count >= 3) techniqueSequence.Clear();
                    techniqueSequence.Add(context.AttackBuff); PreviousAttackBuff = context.AttackBuff;
                }
                if (context.ActionTags != null) foreach (var tag in context.ActionTags) if (tag != null) tagLastUsed[tag] = Player.CombatTime;
            }
            if (type == TriggerType.OnSpellCast && context.Spell != null)
            {
                if (elementSequence.Contains(context.Spell.element) || elementSequence.Count >= 3) elementSequence.Clear();
                elementSequence.Add(context.Spell.element); PreviousSpell = context.Spell;
            }
        }
    }
}
