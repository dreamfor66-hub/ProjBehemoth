using System;
using System.Collections.Generic;
using UnityEngine;
namespace DungeonSlash
{
    // Reusable rule parameters, not item identities. Any item may compose any number of these.
    public enum AbilityRule
    {
        WeaponDamageBonus = 0, NormalDamageBonus = 1, ChargeDamageBonus = 2,
        AttackCooldownReduction = 5, ChargeTimeReduction = 6, SpellDamageBonus = 7, SpellCooldownReduction = 8, CastTimeReduction = 9,
        DrawDamageBonus = 10, DrawExtraHits = 11, SheathParryWindowBonus = 12, PerfectGuardWindow = 13,
        RageGenerationBonus = 14, RageDrainReduction = 15, DeferredDamageSeconds = 18,
        FormExtraHits = 19, FormGuardFraction = 20, BuffExtraUses = 21, BuffPotencyBonus = 22, IgnoreDirectionalGuard = 23,
        DisablePoison = 24, HorizontalCooldownPerHit = 25, HorizontalCooldownCap = 26, SpellChainTargets = 27, SlowDurationBonus = 28,
        FlatWeaponDamage = 30, DamageReduction = 31
    }
    public enum AbilityConditionKind { Always, Enraged, Sheathed, HpBelowFraction, TargetStunned, FirstWeaponHit, HasForm, FormIs, DirectionIs, DirectionIsNot, Horizontal, ElementIs, TargetPoisoned, HasSpell, GuardHeld, DrawAttack }
    public enum EffectScale { Flat, NormalWeapon, ChargeWeapon, DamageDealt, GuardMaximum, GuardCurrent, Rage, Level, PoisonStacks, TargetMaxHp, EnemyCount, GuardDamage, SheathTime }
    public enum AbilityTarget { ContextTarget, AllEnemies, LowestHp, RandomEnemy }
    public enum StatusKind { Poison, Slow, LightningRod, Burn }
    [Serializable] public sealed class AbilityCondition
    {
        public AbilityConditionKind kind;
        public bool invert;
        public float threshold = .5f;
        public SwipeDirection direction;
        public SpellElement element;
        public MonkFormData form;
        public bool Matches(RunState run, EquipmentTriggerContext context)
        {
            var player = run.Player;
            var result = kind switch {
                AbilityConditionKind.Always => true, AbilityConditionKind.Enraged => player.IsEnraged,
                AbilityConditionKind.Sheathed => player.IsSheathed, AbilityConditionKind.HpBelowFraction => player.CurrentHp < player.MaxHp * threshold,
                AbilityConditionKind.TargetStunned => context.Target != null ? context.Target.IsStunned : context.EnemyStunned,
                AbilityConditionKind.FirstWeaponHit => context.Target != null && !context.Target.HasTakenDirectHit,
                AbilityConditionKind.HasForm => context.Form != null, AbilityConditionKind.FormIs => context.Form == form && form != null,
                AbilityConditionKind.DirectionIs => context.HasAttack && context.Direction == direction,
                AbilityConditionKind.DirectionIsNot => context.HasAttack && context.Direction != direction,
                AbilityConditionKind.Horizontal => context.HasAttack && context.Direction is SwipeDirection.Left or SwipeDirection.Right,
                AbilityConditionKind.ElementIs => context.Spell != null && context.Spell.element == element,
                AbilityConditionKind.TargetPoisoned => context.Target != null && context.Target.PoisonStacks > 0,
                AbilityConditionKind.HasSpell => run.Spellbook.Count > 0, AbilityConditionKind.GuardHeld => player.HoldingGuard, AbilityConditionKind.DrawAttack => context.Draw, _ => false };
            return invert ? !result : result;
        }
    }
    [Serializable] public sealed class EquipmentAbility
    {
        [Tooltip("Inspector에서 기능을 구분하는 메모. 게임 표시 문구와 무관합니다.")] public string editorLabel;
        public EquipmentTrigger trigger = new() { triggerCount = 1, triggerAttackTypeFilter = TriggerAttackTypeFilter.All, triggerAttackWayFilter = TriggerAttackWayFilter.All };
        [Tooltip("조건은 모두 만족해야 합니다. 각 조건의 invert로 반전할 수 있습니다.")] public List<AbilityCondition> conditions = new();
        public TargetType target = TargetType.Owner;
        public List<EquipmentEffect> effects = new();
        public bool Matches(RunState run, EquipmentTriggerContext context) => conditions.TrueForAll(condition => condition.Matches(run, context));
    }
}
