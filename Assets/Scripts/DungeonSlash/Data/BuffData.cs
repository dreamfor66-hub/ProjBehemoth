using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DungeonSlash
{
    public enum BuffReleaseType { Permanent = 0, Instant = 1, Time = 10, Stage = 20, TriggerCount = 30 }
    public enum BuffDuplicationType { None, UsePrev, UseNew, StackCount }
    public enum Comparator { Equal, NotEqual, LessOrEqual, GreaterOrEqual, Less, Greater }
    [Serializable] public sealed class BuffReleaseData
    {
        public BuffReleaseType Type;
        [ShowIf(nameof(IsTime)), Min(0)] public float Time;
        [ShowIf(nameof(IsTime))] public bool ResetOnStack;
        [ShowIf(nameof(IsTime))] public bool ResetOnReapplyAtCap;
        [ShowIf(nameof(HasCount)), Min(1)] public int Count = 1;
        private bool IsTime => Type == BuffReleaseType.Time;
        private bool HasCount => Type is BuffReleaseType.Stage or BuffReleaseType.TriggerCount;
    }
    [Serializable] public sealed class BuffDuplicationData
    {
        public BuffDuplicationType Type = BuffDuplicationType.UsePrev;
        [ShowIf(nameof(IsStack)), Min(1)] public int MaxStackCount = 1;
        public BuffTagData ConsiderTagIsDuplicate;
        private bool IsStack => Type == BuffDuplicationType.StackCount;
    }
    // Equipment and every combat state share the same Unit representation.
    [Serializable] public class BuffUnitData
    {
        public bool mergeConditionTrigger;
        [ListDrawerSettings(ShowFoldout=false,ShowIndexLabels=false,ShowItemCount=false)] public List<AbilityCondition> conditions = new();
        public EquipmentTrigger trigger = new() { triggerCount = 1, triggerAttackTypeFilter = TriggerAttackTypeFilter.All, triggerAttackWayFilter = TriggerAttackWayFilter.All, chancePercent = 100 };
        public TargetType target = TargetType.Owner;
        [ListDrawerSettings(ShowFoldout=false,ShowIndexLabels=false,ShowItemCount=false)] public List<EquipmentEffect> effects = new();
        public bool Matches(RunState run, EquipmentTriggerContext context)
        {
            if (trigger.triggerType == TriggerType.Passive && context.HasAttack)
            {
                var type = context.AttackType == TriggerAttackType.Normal ? TriggerAttackTypeFilter.NormalAttack : context.AttackType == TriggerAttackType.Charge ? TriggerAttackTypeFilter.ChargeAttack : TriggerAttackTypeFilter.AdditionalAttack;
                if (trigger.triggerAttackTypeFilter != 0 && (trigger.triggerAttackTypeFilter & type) == 0) return false;
            }
            return conditions.TrueForAll(condition => condition.Matches(run, context));
        }
        public static void ValidateGroups(List<EquipmentAbility> units)
        {
            TriggerType? previous=null;
            foreach(var unit in units)
            {
                if(unit==null)continue;
                if(unit.mergeConditionTrigger && previous.HasValue){var trigger=unit.trigger;trigger.triggerType=previous.Value;unit.trigger=trigger;}
                else unit.mergeConditionTrigger=false;
                previous=unit.trigger.triggerType;
            }
        }
    }
    [CreateAssetMenu(menuName = "Dungeon Slash/Buff")]
    public sealed class BuffData : LocalizedData
    {
        public string displayName;
        [TextArea(5, 12)] public string description;
        public List<BuffTagData> Tags = new();
        public List<EquipmentAbility> Units = new();
        [Title("해제 조건"), HideLabel] public BuffReleaseData ReleaseData = new();
        [Title("중복 버프 처리"), HideLabel] public BuffDuplicationData DuplicationData = new();
        public bool showInHud;
        public string attackFeedbackKey;
        public int attackFeedbackPriority;
        public bool harmful;
        public Color presentationColor = Color.white;
        public bool tintOwner;
        [Tooltip("일반공격 방향 연계 후 차지로 이 버프를 사용합니다. 빈 목록은 일반 상태 버프입니다.")] public SwipeDirection[] attackPattern = Array.Empty<SwipeDirection>();
        public bool requiresUnlock;
        public AttackBuffRole attackRole;
        public string PatternLabel => string.Join("", Array.ConvertAll(attackPattern, SwipeDirectionUtility.Label));
        private void OnValidate()
        {
            BuffUnitData.ValidateGroups(Units);
        }
    }
    [Serializable] public sealed class EffectValueVariable
    {
        public EffectScale type;
        [ShowIf(nameof(UsesBuff))] public BuffData buff;
        [ShowIf(nameof(UsesRule))] public AbilityRule rule;
        public float multiplier = 1;
        [Tooltip("0 = 상한 없음")] public float limit;
        private bool UsesBuff => type is EffectScale.BuffStacks or EffectScale.BuffElapsed or EffectScale.BuffValue;
        private bool UsesRule => type == EffectScale.Rule;
    }
    // Mutable pre-action/pre-hit packet. A Unit edits the packet before combat resolves it.
    public sealed class ActionEffectContext
    {
        public float DamageMultiplier = 1, DamageBonus, GuardCostMultiplier = 1;
        public int ExtraHits;
        public float HitInterval = .12f;
        public bool ForceCharge;
        public readonly HashSet<BuffTagData> Tags = new();
        public GuardFeedback GuardFeedback;
    }
    public enum GuardFeedback { Default, Parry, Perfect }
    public enum AttackBuffRole { None, Offense, Defense, Utility }
}
