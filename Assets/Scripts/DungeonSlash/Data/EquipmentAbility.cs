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
        ResourceGainBonus = 14, ResourceDrainReduction = 15, DeferredDamageSeconds = 18,
        FormExtraHits = 19, FormGuardFraction = 20, BuffExtraUses = 21, BuffPotencyBonus = 22, IgnoreDirectionalGuard = 23,
        BlockBuff = 24, HorizontalCooldownPerHit = 25, HorizontalCooldownCap = 26, SpellChainTargets = 27, SlowDurationBonus = 28,
        FlatWeaponDamage = 30, DamageReduction = 31, GuardRecoveryPerSecond = 32, DamageMultiplier = 33, CooldownMultiplier = 34, WeaponBaseMultiplier = 35,
        EncounterConcealment = 36, DetectionChancePerAction = 37
    }
    public enum AbilityConditionKind { Always, Enraged, Sheathed, HpBelowFraction, TargetStunned, FirstWeaponHit, HasForm, FormIs, DirectionIs, DirectionIsNot, Horizontal, ElementIs, TargetPoisoned, HasSpell, GuardHeld, ActionHasTag, OwnerHasBuff, TargetHasBuff, Resource, BuffStack, Rule, OwnerConcealed, EncounterActions }
    public enum EffectScale { Flat, NormalWeapon, ChargeWeapon, DamageDealt, GuardMaximum, GuardCurrent, Rage, Level, PoisonStacks, TargetMaxHp, EnemyCount, GuardDamage, SheathTime, BuffStacks, BuffElapsed, Resource, Rule, BuffValue }
    public enum AbilityTarget { ContextTarget, AllEnemies, LowestHp, RandomEnemy }
    public enum StatusKind { Poison, Slow, LightningRod, Burn }
    [Serializable] public sealed class AbilityCondition
    {
        public AbilityConditionKind kind;
        public bool invert;
        public float threshold = .5f;
        public SwipeDirection direction;
        public SpellElement element;
        public FormData form;
        public BuffData buff;
        public BuffTagData buffTag;
        public CombatResourceData resource;
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
                AbilityConditionKind.HasForm => context.Form != null, AbilityConditionKind.FormIs => context.Form == form && form != null,
                AbilityConditionKind.DirectionIs => context.HasAttack && context.Direction == direction,
                AbilityConditionKind.DirectionIsNot => context.HasAttack && context.Direction != direction,
                AbilityConditionKind.Horizontal => context.HasAttack && context.Direction is SwipeDirection.Left or SwipeDirection.Right,
                AbilityConditionKind.ElementIs => context.Spell != null && context.Spell.element == element,
                AbilityConditionKind.TargetPoisoned => context.Target != null && context.Target.PoisonStacks > 0,
                AbilityConditionKind.HasSpell => run.Spellbook.Count > 0, AbilityConditionKind.GuardHeld => player.HoldingGuard, AbilityConditionKind.ActionHasTag => buffTag!=null && context.ActionTags!=null && context.ActionTags.Contains(buffTag),
                AbilityConditionKind.OwnerHasBuff => (context.Buff?.Manager ?? run.Buffs).Has(buff, buffTag),
                AbilityConditionKind.TargetHasBuff => context.Target != null && context.Target.Buffs.Has(buff, buffTag),
                AbilityConditionKind.Resource => Compare(player.Resource(resource), threshold, comparator),
                AbilityConditionKind.BuffStack => Compare(context.Buff?.StackCount ?? 0, threshold, comparator),
                AbilityConditionKind.Rule => Compare(run.Rule(rule, context), threshold, comparator),
                AbilityConditionKind.OwnerConcealed => run.Awareness.IsConcealed,
                AbilityConditionKind.EncounterActions => Compare(run.Awareness.CompletedActions, threshold, comparator), _ => false };
            return invert ? !result : result;
        }
        public static bool Compare(float a, float b, Comparator op) => op switch { Comparator.Equal => Mathf.Approximately(a,b), Comparator.NotEqual => !Mathf.Approximately(a,b), Comparator.LessOrEqual => a <= b, Comparator.GreaterOrEqual => a >= b, Comparator.Less => a < b, _ => a > b };
    }
    [Serializable] public sealed class EquipmentAbility : BuffUnitData { }
}
