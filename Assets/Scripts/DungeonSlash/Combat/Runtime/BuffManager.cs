using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonSlash
{
    public sealed class BuffInstance
    {
        public BuffData Data { get; internal set; }
        public BuffManager Manager { get; internal set; }
        public RunState Root { get; internal set; }
        public EquipmentData Source { get; internal set; }
        public int StackCount { get; internal set; } = 1;
        public float ElapsedTime { get; internal set; }
        public float TimeLastStack { get; internal set; }
        public float Duration { get; internal set; }
        public float Potency { get; internal set; } = 1;
        public int MaximumStacks { get; internal set; }
        public bool IsEnd { get; internal set; }
        public float Value { get; internal set; }
        public EquipmentTriggerContext InitialContext { get; internal set; }
        internal bool Started, EndCast;
        internal int StageCount;
        internal readonly List<Unit> Units = new();
        internal readonly Queue<int> StackEvents = new();
        internal sealed class Unit
        {
            public AbilityTriggerState State;
            public readonly List<EquipmentAbility> Targets = new();
            public bool ConditionWasMet;
        }
    }

    // Smash-Dungeon execution model: FIFO commands in UpdateState, lifecycle/timers in Cast.
    // Commands emitted by Cast/events are never recursively applied in the same pass.
    public sealed class BuffManager
    {
        private readonly List<BuffInstance> buffs = new();
        private readonly Queue<Action> commands = new();
        private readonly HashSet<ModifierKind> evaluatingStats=new();
        public RunState Run { get; internal set; }
        public MonsterRuntime Monster { get; }
        public IReadOnlyList<BuffInstance> Active => buffs.Where(b => !b.IsEnd).ToArray();
        public BuffManager(RunState run = null, MonsterRuntime monster = null) { Run = run; Monster = monster; }
        public bool Has(BuffData data, BuffTagData tag = null) => Find(data, tag) != null;
        public static bool SameData(BuffData a,BuffData b)=>a!=null && b!=null && string.Equals(a.name,b.name,StringComparison.Ordinal);
        public BuffInstance Find(BuffData data, BuffTagData tag = null) => buffs.FirstOrDefault(b => !b.IsEnd && (SameData(b.Data,data) || tag != null && b.Data.Tags.Contains(tag)));
        public void Add(BuffData data, int stacks = 1, RunState root = null, EquipmentData source = null, float potency = 1, EquipmentTriggerContext context = default, float initialValue = 0, float duration = -1, int maximumStacks = 0)
        {
            if (data == null || stacks <= 0) return;
            commands.Enqueue(() => AddCore(data, stacks, root ?? Run, source, potency, context, initialValue,duration,maximumStacks));
        }
        private void AddCore(BuffData data, int stacks, RunState root, EquipmentData source, float potency, EquipmentTriggerContext context, float initialValue,float duration,int maximumStacks)
        {
            var previous = Find(data, data.DuplicationData.ConsiderTagIsDuplicate);
            if (previous != null)
            {
                switch (data.DuplicationData.Type)
                {
                    case BuffDuplicationType.UsePrev: return;
                    case BuffDuplicationType.StackCount:
                        if(duration>=0)previous.Duration=duration;
                        if(maximumStacks>0)previous.MaximumStacks=maximumStacks;
                        previous.Potency=potency;
                        if(data.ReleaseData.ResetOnReapplyAtCap)previous.TimeLastStack=previous.ElapsedTime;
                        ChangeStackCore(previous, stacks); return;
                    case BuffDuplicationType.UseNew: previous.IsEnd = true; break;
                }
            }
            var instance = new BuffInstance { Data = data, Manager = this, Root = root, Source = source, Potency = potency, Duration = duration>=0?duration:data.ReleaseData.Time, MaximumStacks=maximumStacks>0?maximumStacks:data.DuplicationData.MaxStackCount, InitialContext = context, Value = initialValue };
            if (root != null) instance.Duration += root.Rule(AbilityRule.BuffDurationBonus, new EquipmentTriggerContext(default, TriggerAttackType.None, TriggerAttackWay.None, false, false, buff: instance));
            BuffInstance.Unit host = null;
            for (var i = 0; i < data.Units.Count; i++)
            {
                var unit = data.Units[i];
                if (host == null || !unit.mergeConditionTrigger)
                { host = new BuffInstance.Unit { State = new AbilityTriggerState(source, unit, i) }; instance.Units.Add(host); }
                host.Targets.Add(unit);
            }
            buffs.Add(instance);
            if (stacks > 1) ChangeStackCore(instance, stacks - 1);
        }
        public void Remove(BuffData data, BuffTagData tag = null) => commands.Enqueue(() => { foreach (var buff in buffs.Where(b => SameData(b.Data,data) || tag != null && b.Data.Tags.Contains(tag))) buff.IsEnd = true; });
        public void Remove(BuffInstance instance) { if (instance != null) commands.Enqueue(() => instance.IsEnd = true); }
        public void ChangeStack(BuffInstance instance, int value, bool set = false) { if (instance != null) commands.Enqueue(() => ChangeStackCore(instance, set ? value - instance.StackCount : value)); }
        private static void ChangeStackCore(BuffInstance buff, int delta)
        {
            if (buff.IsEnd) return;
            var value = Mathf.Clamp(buff.StackCount + delta, 0, Mathf.Max(1, buff.MaximumStacks));
            if (value == buff.StackCount) return;
            if (value > buff.StackCount) buff.TimeLastStack = buff.ElapsedTime;
            buff.StackCount = value; buff.StackEvents.Enqueue(delta);
            if (value <= 0) buff.IsEnd = true;
        }
        public void UpdateState(float dt)
        {
            dt = Mathf.Max(0, dt);
            foreach (var buff in buffs)
            {
                if (buff.IsEnd) continue;
                buff.ElapsedTime += dt;
                if (buff.Data.ReleaseData.Type == BuffReleaseType.Time && buff.ElapsedTime - (buff.Data.ReleaseData.ResetOnStack ? buff.TimeLastStack : 0) > buff.Duration) buff.IsEnd = true;
                foreach (var unit in buff.Units)
                    if (unit.State.Ability.trigger.triggerType == TriggerType.OnTimer && unit.State.Ability.Matches(buff.Root ?? Run, Context(buff))) unit.State.AdvanceTimer(dt);
            }
            var count = commands.Count;
            while (count-- > 0) commands.Dequeue()();
        }
        private EquipmentTriggerContext Context(BuffInstance buff) => new(default, TriggerAttackType.None, TriggerAttackWay.None, false, Monster?.IsStunned ?? false, target: Monster, form: buff.Data.form, damage: buff.InitialContext.Damage, buff: buff);
        public void Cast()
        {
            foreach (var buff in buffs.ToArray())
            {
                if (!buff.Started) { buff.Started = true; DispatchOne(buff, TriggerType.OnBuffStart, Context(buff)); }
                if (!buff.IsEnd)
                {
                    while (buff.StackEvents.Count > 0) { buff.StackEvents.Dequeue(); DispatchOne(buff, TriggerType.OnBuffStack, Context(buff)); }
                    foreach (var unit in buff.Units)
                    {
                        var context = Context(buff); var run = buff.Root ?? Run;
                        var met = unit.State.Ability.Matches(run, context);
                        if (unit.State.Ability.trigger.triggerType == TriggerType.OnConditionMet && met && !unit.ConditionWasMet) Execute(buff, unit, TriggerType.OnConditionMet, context);
                        unit.ConditionWasMet = met;
                        if (unit.State.Ability.trigger.triggerType == TriggerType.OnTimer)
                            while (!buff.IsEnd && unit.State.ConsumeTimer()) Execute(buff, unit, TriggerType.OnTimer, context);
                    }
                    if (buff.Data.ReleaseData.Type == BuffReleaseType.Instant) buff.IsEnd = true;
                }
                CheckRelease(buff);
                if (buff.IsEnd && !buff.EndCast) { buff.EndCast = true; DispatchOne(buff, TriggerType.OnBuffEnd, Context(buff)); }
            }
            buffs.RemoveAll(b => b.EndCast);
        }
        public void Tick(float dt) { UpdateState(dt); Cast(); }
        public void Dispatch(TriggerType type, EquipmentTriggerContext context)
        {
            foreach (var buff in buffs.ToArray())
            {
                if (buff.IsEnd) continue;
                if (type == TriggerType.OnCombatWon) buff.StageCount++;
                DispatchOne(buff, type, context.WithBuff(buff)); CheckRelease(buff);
            }
        }
        public IEnumerable<EquipmentActivation> CollectHit(TriggerType type,EquipmentTriggerContext context)
        {
            foreach(var buff in buffs.ToArray())
                if(!buff.IsEnd)
                    foreach(var unit in buff.Units)
                        if(unit.State.TryTrigger(buff.Root??Run,type,context.WithBuff(buff)))
                            foreach(var target in unit.Targets)
                                foreach(var effect in target.effects) yield return new EquipmentActivation(buff.Source,target,effect,context.WithBuff(buff),"buff:"+buff.Data.GetInstanceID());
        }
        public void CheckReleases() {foreach(var buff in buffs)CheckRelease(buff);}
        private void DispatchOne(BuffInstance buff, TriggerType type, EquipmentTriggerContext context)
        { foreach (var unit in buff.Units) Execute(buff, unit, type, context); }
        private void Execute(BuffInstance buff, BuffInstance.Unit unit, TriggerType type, EquipmentTriggerContext context)
        {
            var run = buff.Root ?? Run;
            if (!unit.State.TryTrigger(run, type, context)) return;
            // A merged group shares the host's roll, cadence and conditions; targets remain ordered.
            foreach (var target in unit.Targets)
                foreach (var effect in target.effects)
                    ExecuteEffect(buff, target, effect, context);
        }
        private void ExecuteEffect(BuffInstance buff, BuffUnitData unit, EquipmentEffect effect, EquipmentTriggerContext context)
        {
            var run = buff.Root ?? Run;
            var value = run != null ? run.EffectValue(effect, context) : effect.effectMagnitude + (effect.valueVariables?.Sum(v => v.type == EffectScale.BuffStacks ? buff.StackCount * v.multiplier : 0) ?? 0);
            if (unit.target == TargetType.ThisBuff)
            {
                if (effect.effectType == EffectType.RemoveBuff) Remove(buff);
                else if (effect.effectType is EffectType.ChangeBuffStack or EffectType.SetBuffStack) ChangeStack(buff, Mathf.RoundToInt(value), effect.effectType == EffectType.SetBuffStack);
                else if (effect.effectType == EffectType.ChangeBuffValue) buff.Value = Mathf.Max(0,buff.Value+value);
                return;
            }
            if (Monster != null && unit.target is TargetType.Owner or TargetType.OwnerStat)
            {
                switch (effect.effectType)
                {
                    case EffectType.Heal: Monster.Heal(value); break;
                    case EffectType.Damage:
                        var damage = value * buff.Potency; Monster.TakeDamage(damage); Monster.RecordStatusDamage(buff.Data, damage); break;
                    case EffectType.AddBuff: Add(effect.buff, Mathf.Max(1, Mathf.RoundToInt(value)), buff.Root, buff.Source, buff.Potency); break;
                    case EffectType.RemoveBuff: Remove(effect.buff, effect.buffTag); break;
                    case EffectType.ChangeBuffStack: ChangeStack(buff, Mathf.RoundToInt(value)); break;
                }
                return;
            }
            run?.ExecuteActivation(new EquipmentActivation(buff.Source, unit, effect, context, "buff:" + buff.Data.GetInstanceID()), false);
        }
        private static void CheckRelease(BuffInstance buff)
        {
            var release = buff.Data.ReleaseData;
            if (release.Type == BuffReleaseType.TriggerCount && buff.Units.Sum(u => u.State.Activations) >= release.Count || release.Type == BuffReleaseType.Stage && buff.StageCount >= release.Count) buff.IsEnd = true;
        }
        public float Rule(AbilityRule rule, EquipmentTriggerContext context)
        {
            var total = 0f;
            foreach (var buff in buffs.Where(b => !b.IsEnd))
                foreach (var unit in buff.Units)
                    if (unit.State.Ability.trigger.triggerType == TriggerType.Passive && unit.State.Ability.Matches(buff.Root ?? Run, context.WithBuff(buff)))
                        foreach (var target in unit.Targets)
                            foreach (var effect in target.effects)
                                if (effect.effectType == EffectType.RuleModifier && effect.rule == rule && (rule is not AbilityRule.BuffDurationBonus and not AbilityRule.BlockBuff || effect.buff==null || effect.buff==context.Buff?.Data)) total += (buff.Root ?? Run)?.EffectValue(effect, context.WithBuff(buff)) ?? effect.effectMagnitude;
            return total;
        }
        public IEnumerable<(EquipmentAbility Unit,EquipmentTriggerContext Context)> PassiveUnits(EquipmentTriggerContext context,bool ignoreConditions=false)
        {
            foreach(var buff in buffs.Where(b=>!b.IsEnd))
                foreach(var unit in buff.Units)
                    if(unit.State.Ability.trigger.triggerType==TriggerType.Passive && (ignoreConditions || unit.State.Ability.Matches(buff.Root??Run,context.WithBuff(buff))))
                        foreach(var target in unit.Targets)yield return(target,context.WithBuff(buff));
        }
        public float Stat(ModifierKind kind)
        {
            if(!evaluatingStats.Add(kind))return 0;
            try{
            var total=0f;
            foreach(var buff in buffs.Where(b=>!b.IsEnd))
                foreach(var unit in buff.Units)
                    if(unit.State.Ability.trigger.triggerType==TriggerType.Passive && unit.State.Ability.Matches(buff.Root??Run,Context(buff)))
                        foreach(var target in unit.Targets)
                            foreach(var effect in target.effects)
                                if(effect.effectType==EffectType.StatIncrease && effect.effectStatType==kind) total+=(buff.Root??Run)?.EffectValue(effect,Context(buff))??effect.effectMagnitude;
            return total;
            }finally{evaluatingStats.Remove(kind);}
        }
        public void EndCombat()
        {
            commands.Clear();
            // Permanent and multi-stage effects survive transitions, just as in the source.
            buffs.RemoveAll(buff=>buff.Data.ReleaseData.Type is BuffReleaseType.Time or BuffReleaseType.TriggerCount or BuffReleaseType.Instant || buff.Data.ReleaseData.Type==BuffReleaseType.Stage && buff.Data.ReleaseData.Count<=1);
        }
        public void RemoveSource(EquipmentData source) { foreach (var buff in buffs.Where(b => b.Source == source)) buff.IsEnd = true; }
        public float PeriodicDamage(BuffInstance buff)=>buff.Data.Units.Where(u=>u.trigger.triggerType==TriggerType.OnTimer).SelectMany(u=>u.effects).Where(e=>e.effectType==EffectType.Damage).Sum(effect=>(buff.Root!=null?buff.Root.EffectValue(effect,Context(buff)):effect.effectMagnitude+(effect.valueVariables?.Sum(v=>v.type==EffectScale.BuffStacks?buff.StackCount*v.multiplier:0)??0))*buff.Potency);
        public void Clear() { commands.Clear(); buffs.Clear(); }
    }
}
