using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace DungeonSlash
{
    // Reusable rule parameters, not item identities. Any item may compose any number of these.
    public enum AbilityRule
    {
        WeaponDamageBonus = 0, NormalDamageBonus = 1, ChargeDamageBonus = 2,
        AttackCooldownReduction = 5, ChargeTimeReduction = 6, SpellDamageBonus = 7, SpellCooldownReduction = 8, CastTimeReduction = 9,
        ActionDamageBonus = 10, ActionExtraHits = 11, BuffDurationBonus = 12, PerfectGuardWindow = 13,
        BuffStackGainBonus = 14, BuffStackDrainReduction = 15, DeferredDamageSeconds = 18,
        AttackBuffExtraHits = 19, AttackBuffGuardFraction = 20, BuffExtraUses = 21, BuffPotencyBonus = 22, IgnoreDirectionalGuard = 23,
        BlockBuff = 24, HorizontalCooldownPerHit = 25, HorizontalCooldownCap = 26, SpellChainTargets = 27, SlowDurationBonus = 28,
        FlatWeaponDamage = 30, DamageReduction = 31, GuardRecoveryPerSecond = 32, DamageMultiplier = 33, CooldownMultiplier = 34, WeaponBaseMultiplier = 35,
        EncounterConcealment = 36, DetectionChancePerAction = 37, DrawReachBonus = 38
    }
    public enum AbilityConditionKind { Always = 0, Enraged = 1, Sheathed = 2, HpBelowFraction = 3, TargetStunned = 4, FirstWeaponHit = 5, HasActionBuff = 6, ActionBuffIs = 7, DirectionIs = 8, DirectionIsNot = 9, Horizontal = 10, ElementIs = 11, TargetPoisoned = 12, HasSpell = 13, GuardHeld = 14, ActionHasTag = 15, OwnerHasBuff = 16, TargetHasBuff = 17, BuffStack = 19, Rule = 20, OwnerConcealed = 21, EncounterActions = 22, FirstWeaponAction = 23, TargetPreparing = 24, MpFraction = 25, PreviousSpellElementSame = 26, NextDistinctElements = 27, PreviousAttackBuffSame = 28, NextDistinctTechniques = 29, SinceActionTag = 30, ContextBuffIs = 31, OwnerBuffElapsed = 32, OwnerBuffRemainingUses = 33, ActionHitCount = 34 }
    public enum EffectScale { Flat = 0, NormalWeapon = 1, ChargeWeapon = 2, DamageDealt = 3, GuardMaximum = 4, GuardCurrent = 5, Level = 7, PoisonStacks = 8, TargetMaxHp = 9, EnemyCount = 10, GuardDamage = 11, SheathTime = 12, BuffStacks = 13, BuffElapsed = 14, Rule = 16, BuffValue = 17, ActionStartBuffElapsed = 18, ActionStartBuffStacks = 19 }
    public enum AbilityTarget { ContextTarget, AllEnemies, LowestHp, RandomEnemy, OtherEnemies }
    public enum StatusKind { Poison, Slow, LightningRod, Burn }
    [Serializable] public sealed class AbilityCondition
    {
        public AbilityConditionKind kind;
        public bool invert;
        public float threshold = .5f;
        public SwipeDirection direction;
        public SpellElement element;
        public BuffData buff;
        public BuffTagData buffTag;
        public Comparator comparator = Comparator.GreaterOrEqual;
        public AbilityRule rule;
        public bool Matches(RunState run, EquipmentTriggerContext context)
        {
            var player = run?.Player;
            var result = kind switch {
                AbilityConditionKind.Always => true,
                AbilityConditionKind.HpBelowFraction => context.Buff?.Manager.Monster != null ? context.Buff.Manager.Monster.CurrentHp < context.Buff.Manager.Monster.MaxHp * threshold : player != null && player.CurrentHp < player.MaxHp * threshold,
                AbilityConditionKind.TargetStunned => context.Target != null ? context.Target.IsStunned : context.EnemyStunned,
                AbilityConditionKind.FirstWeaponHit => context.Target != null && !context.Target.HasTakenDirectHit,
                AbilityConditionKind.HasActionBuff => context.AttackBuff != null && (threshold < 1 || (int)context.AttackBuff.attackRole == (int)threshold), AbilityConditionKind.ActionBuffIs => BuffManager.SameData(context.AttackBuff, buff),
                AbilityConditionKind.DirectionIs => context.HasAttack && context.Direction == direction,
                AbilityConditionKind.DirectionIsNot => context.HasAttack && context.Direction != direction,
                AbilityConditionKind.Horizontal => context.HasAttack && context.Direction is SwipeDirection.Left or SwipeDirection.Right,
                AbilityConditionKind.ElementIs => context.Spell != null && context.Spell.element == element,
                AbilityConditionKind.TargetPoisoned => context.Target != null && context.Target.PoisonStacks > 0,
                AbilityConditionKind.HasSpell => run.Spellbook.Count > 0, AbilityConditionKind.GuardHeld => player.HoldingGuard, AbilityConditionKind.ActionHasTag => buffTag!=null && context.ActionTags!=null && context.ActionTags.Contains(buffTag),
                AbilityConditionKind.OwnerHasBuff => (context.Buff?.Manager ?? run.Buffs).Has(buff, buffTag),
                AbilityConditionKind.TargetHasBuff => context.Target != null && context.Target.Buffs.Has(buff, buffTag),
                AbilityConditionKind.BuffStack => Compare((buff != null || buffTag != null ? (context.Buff?.Manager ?? run.Buffs).Find(buff, buffTag) : context.Buff)?.StackCount ?? 0, threshold, comparator),
                AbilityConditionKind.Rule => Compare(run.Rule(rule, context), threshold, comparator),
                AbilityConditionKind.OwnerConcealed => run.Awareness.IsConcealed,
                AbilityConditionKind.EncounterActions => Compare(run.Awareness.CompletedActions, threshold, comparator),
                AbilityConditionKind.FirstWeaponAction => run.WeaponActionsThisCombat == 0,
                AbilityConditionKind.TargetPreparing => context.Target != null && context.Target.State is MonsterState.Charging or MonsterState.Attacking,
                AbilityConditionKind.MpFraction => player != null && Compare(player.MaxMp > 0 ? player.CurrentMp / player.MaxMp : 0, threshold, comparator),
                AbilityConditionKind.PreviousSpellElementSame => context.Spell != null && run.PreviousSpell != null && context.Spell.element == run.PreviousSpell.element,
                AbilityConditionKind.NextDistinctElements => Compare(run.NextDistinctElements(context.Spell), threshold, comparator),
                AbilityConditionKind.PreviousAttackBuffSame => BuffManager.SameData(context.AttackBuff, run.PreviousAttackBuff),
                AbilityConditionKind.NextDistinctTechniques => Compare(run.NextDistinctTechniques(context.AttackBuff), threshold, comparator),
                AbilityConditionKind.SinceActionTag => Compare(run.SinceTag(buffTag), threshold, comparator),
                AbilityConditionKind.ContextBuffIs => BuffManager.SameData(context.Buff?.Data, buff),
                AbilityConditionKind.ActionHitCount => Compare(run.ActionHits, threshold, comparator),
                AbilityConditionKind.OwnerBuffElapsed => run.Buffs.Find(buff) != null && Compare(run.Buffs.Find(buff).ElapsedTime, threshold, comparator),
                AbilityConditionKind.OwnerBuffRemainingUses => run.Buffs.Find(buff) != null && Compare(run.Buffs.RemainingUses(buff), threshold, comparator),
                _ => false };
            return invert ? !result : result;
        }
        public static bool Compare(float a, float b, Comparator op) => op switch { Comparator.Equal => Mathf.Approximately(a,b), Comparator.NotEqual => !Mathf.Approximately(a,b), Comparator.LessOrEqual => a <= b, Comparator.GreaterOrEqual => a >= b, Comparator.Less => a < b, _ => a > b };
    }
    [Serializable] public sealed class EquipmentAbility : BuffUnitData { }
}
