using System;
using System.Collections.Generic;
using System.Linq;
using DungeonSlash;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

// Same four-block editing rhythm as Smash-Dungeon's BuffUnitDataPropertyDrawer.
internal static class BuffInspectorLayout
{
    public static void Title(string text, Color color)
    {
        var rect=EditorGUILayout.GetControlRect(false,25); EditorGUI.DrawRect(rect,color);
        EditorGUI.LabelField(rect,new GUIContent(text),new GUIStyle(EditorStyles.boldLabel){fontSize=16,normal={textColor=new Color(.88f,.88f,.88f)}});
    }
    public static void Draw(InspectorProperty property,string name,string label=null) => property.Children[name]?.Draw(label==null?null:new GUIContent(label));
    public static T Choice<T>(T current,Func<T,bool> predicate) where T:Enum
    {
        var values=Enum.GetValues(typeof(T)).Cast<T>().Where(predicate).ToArray();
        var index=Array.IndexOf(values,current); var chosen=EditorGUILayout.Popup(Mathf.Max(0,index),values.Select(value=>ObjectNames.NicifyVariableName(value.ToString())).ToArray());
        return index<0 && !GUI.changed ? current : values[chosen];
    }
    public static bool AllowedRule(AbilityRule rule)=>rule is not AbilityRule.PerfectGuardWindow and not AbilityRule.HorizontalCooldownPerHit and not AbilityRule.HorizontalCooldownCap and not AbilityRule.SlowDurationBonus;
}
internal sealed class EquipmentAbilityOdinDrawer : OdinValueDrawer<EquipmentAbility>
{
    protected override void DrawPropertyLayout(GUIContent label)
    {
        var value=ValueEntry.SmartValue; if(value==null){CallNextDrawer(label);return;}
        using(new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            BuffInspectorLayout.Draw(Property,"mergeConditionTrigger","앞 Unit의 조건 / 트리거 공유");
            if(!value.mergeConditionTrigger)
            {
                BuffInspectorLayout.Title("조건",new Color(.1f,.1f,.2f)); BuffInspectorLayout.Draw(Property,"conditions","");
                BuffInspectorLayout.Title("트리거",new Color(.1f,.2f,.1f)); BuffInspectorLayout.Draw(Property,"trigger","");
            }
            BuffInspectorLayout.Title("대상",new Color(.15f,.15f,.1f));
            var target=BuffInspectorLayout.Choice(value.target,t=>AllowedTarget(t,value.trigger.triggerType));
            if(target!=value.target){value.target=target;ValueEntry.SmartValue=value;}
            BuffInspectorLayout.Title("효과",new Color(.2f,.1f,.1f)); BuffInspectorLayout.Draw(Property,"effects","");
        }
    }
    public static bool AllowedTarget(TargetType target,TriggerType type)=>target switch
    {
        TargetType.OwnerStat=>type==TriggerType.Passive,
        TargetType.Action=>type==TriggerType.OnActionPreparing,
        TargetType.Hit=>type==TriggerType.OnBeforeGuard || type==TriggerType.OnHurt || type==TriggerType.OnHit,
        _=>type!=TriggerType.Passive
    };
}
internal sealed class EquipmentTriggerOdinDrawer : OdinValueDrawer<EquipmentTrigger>
{
    protected override void DrawPropertyLayout(GUIContent label)
    {
        var value=ValueEntry.SmartValue;
        var type=BuffInspectorLayout.Choice(value.triggerType,t=>t is not TriggerType.OnSheathParry and not TriggerType.OnPerfectGuard);
        if(type!=value.triggerType){value.triggerType=type;ValueEntry.SmartValue=value;}
        if(value.UsesAttackFilters)
        { BuffInspectorLayout.Draw(Property,"triggerAttackTypeFilter","공격 종류");BuffInspectorLayout.Draw(Property,"triggerAttackWayFilter","공격 방향");BuffInspectorLayout.Draw(Property,"includeAdditionalAttacks","추가 공격 포함"); }
        if(type==TriggerType.OnTimer)BuffInspectorLayout.Draw(Property,"intervalSeconds","F초마다");
        if(type==TriggerType.OnSignal)BuffInspectorLayout.Draw(Property,"signal","신호");
        if(type!=TriggerType.Passive)
        {
            BuffInspectorLayout.Draw(Property,"triggerCount","N번 발생할 때마다");BuffInspectorLayout.Draw(Property,"chancePercent","확률 (%)");
            BuffInspectorLayout.Draw(Property,"triggerMaxCount","발동 상한 (0 = 없음)");BuffInspectorLayout.Draw(Property,"cooldownSeconds","재발동 대기 (초)");
            if(!Property.Tree.WeakTargets.Any(target=>target is BuffData))BuffInspectorLayout.Draw(Property,"resetCountEachCombat","전투마다 횟수 초기화");
        }
    }
}
internal sealed class AbilityConditionOdinDrawer : OdinValueDrawer<AbilityCondition>
{
    protected override void DrawPropertyLayout(GUIContent label)
    {
        var value=ValueEntry.SmartValue;if(value==null){CallNextDrawer(label);return;}
        var type=BuffInspectorLayout.Choice(value.kind,t=>t is not AbilityConditionKind.Enraged and not AbilityConditionKind.Sheathed);
        if(type!=value.kind){value.kind=type;ValueEntry.SmartValue=value;}
        if(type is AbilityConditionKind.OwnerHasBuff or AbilityConditionKind.TargetHasBuff){BuffInspectorLayout.Draw(Property,"buff");BuffInspectorLayout.Draw(Property,"buffTag");}
        if(type==AbilityConditionKind.ActionHasTag)BuffInspectorLayout.Draw(Property,"buffTag","행동 태그");
        if(type==AbilityConditionKind.Resource)BuffInspectorLayout.Draw(Property,"resource");
        if(type==AbilityConditionKind.Rule)BuffInspectorLayout.Draw(Property,"rule");
        if(type is AbilityConditionKind.Resource or AbilityConditionKind.Rule or AbilityConditionKind.BuffStack or AbilityConditionKind.EncounterActions)BuffInspectorLayout.Draw(Property,"comparator");
        if(type is AbilityConditionKind.Resource or AbilityConditionKind.Rule or AbilityConditionKind.BuffStack or AbilityConditionKind.EncounterActions or AbilityConditionKind.HpBelowFraction)BuffInspectorLayout.Draw(Property,"threshold","비교 값");
        if(type==AbilityConditionKind.FormIs)BuffInspectorLayout.Draw(Property,"form");
        if(type is AbilityConditionKind.DirectionIs or AbilityConditionKind.DirectionIsNot)BuffInspectorLayout.Draw(Property,"direction");
        if(type==AbilityConditionKind.ElementIs)BuffInspectorLayout.Draw(Property,"element");
        BuffInspectorLayout.Draw(Property,"invert","반전");
    }
}
internal sealed class EquipmentEffectOdinDrawer : OdinValueDrawer<EquipmentEffect>
{
    protected override void DrawPropertyLayout(GUIContent label)
    {
        var value=ValueEntry.SmartValue;
        BuffUnitData unit=null;for(var parent=Property.Parent;parent!=null;parent=parent.Parent)if(parent.ValueEntry?.WeakSmartValue is BuffUnitData found){unit=found;break;}
        var selected=BuffInspectorLayout.Choice(value.effectType,type=>Allowed(type,unit?.target??TargetType.Owner));
        if(selected!=value.effectType){value.effectType=selected;ValueEntry.SmartValue=value;}var type=value.effectType;
        if(type==EffectType.LearnSpell){BuffInspectorLayout.Draw(Property,"spell");return;}
        if(type is EffectType.UnlockForm or EffectType.ApplyForm){BuffInspectorLayout.Draw(Property,"form");return;}
        if(type==EffectType.SummonCompanion){BuffInspectorLayout.Draw(Property,"companion");return;}
        if(type is EffectType.AddBuff or EffectType.RemoveBuff or EffectType.ChangeBuffStack or EffectType.SetBuffStack)BuffInspectorLayout.Draw(Property,"buff");
        if(type is EffectType.RemoveBuff or EffectType.EmitSignal or EffectType.MarkAction)BuffInspectorLayout.Draw(Property,"buffTag",type==EffectType.EmitSignal?"신호":"태그");
        if(type is EffectType.AddResource or EffectType.SetResource)BuffInspectorLayout.Draw(Property,"resource");
        if(type is EffectType.ResetAttackCooldown or EffectType.RemoveBuff or EffectType.ActionCharge or EffectType.EmitSignal or EffectType.PayDeferredDamage or EffectType.ClearDeferredDamage or EffectType.MarkAction)return;
        BuffInspectorLayout.Draw(Property,"effectMagnitude","기본 값");BuffInspectorLayout.Draw(Property,"valueVariables","값 변수 (기본 값에 합산)");
        BuffInspectorLayout.Draw(Property,"maximumMagnitude","최종 상한 (0 = 없음)");
        if(type==EffectType.RuleModifier){var rule=BuffInspectorLayout.Choice(value.rule,BuffInspectorLayout.AllowedRule);if(rule!=value.rule){value.rule=rule;ValueEntry.SmartValue=value;}}
        if(type==EffectType.RuleModifier && value.rule is AbilityRule.BuffDurationBonus or AbilityRule.BlockBuff or AbilityRule.DeferredDamageSeconds)BuffInspectorLayout.Draw(Property,"buff","관련 Buff");
        if(type==EffectType.GuardCost)BuffInspectorLayout.Draw(Property,"guardFeedback","성공 피드백");
        if(type==EffectType.StatIncrease)BuffInspectorLayout.Draw(Property,"effectStatType");
        if(type==EffectType.ExtraAttack){BuffInspectorLayout.Draw(Property,"effectCount","횟수");BuffInspectorLayout.Draw(Property,"effectAngle","각도");}
        if(unit?.target==TargetType.Enemy && type is EffectType.Damage or EffectType.AddBuff or EffectType.SpreadStatus or EffectType.Interrupt)BuffInspectorLayout.Draw(Property,"targets","적 선택 규칙");
        if(unit?.target==TargetType.Enemy && type==EffectType.Damage)BuffInspectorLayout.Draw(Property,"element");
        if(type==EffectType.EchoSpell)BuffInspectorLayout.Draw(Property,"effectTime","지연 (초)");
        if(type==EffectType.ApplyStatus)EditorGUILayout.HelpBox("이전 형식입니다. AddBuff와 BuffData로 작성하세요.",MessageType.Warning);
    }
    private static bool Allowed(EffectType type,TargetType target)=>target switch{
        TargetType.ThisBuff=>type is EffectType.RemoveBuff or EffectType.ChangeBuffStack or EffectType.SetBuffStack or EffectType.ChangeBuffValue,
        TargetType.Action=>type is EffectType.ActionDamage or EffectType.ActionExtraHits or EffectType.ActionCharge or EffectType.MarkAction,
        TargetType.Hit=>type is EffectType.ActionDamage or EffectType.GuardCost,
        TargetType.OwnerStat=>type is EffectType.StatIncrease or EffectType.RuleModifier or EffectType.UnlockForm or EffectType.SummonCompanion,
        _=>type is not EffectType.ApplyStatus and not EffectType.ActionDamage and not EffectType.ActionCharge and not EffectType.ActionExtraHits and not EffectType.GuardCost and not EffectType.MarkAction and not EffectType.ChangeBuffValue and not EffectType.PayDeferredDamage and not EffectType.ClearDeferredDamage and not EffectType.SummonCompanion and not EffectType.UnlockForm};
}
internal sealed class EffectVariableOdinDrawer:OdinValueDrawer<EffectValueVariable>
{
    protected override void DrawPropertyLayout(GUIContent label)
    {
        var value=ValueEntry.SmartValue;if(value==null){CallNextDrawer(label);return;}
        var type=BuffInspectorLayout.Choice(value.type,t=>t is not EffectScale.Rage and not EffectScale.SheathTime and not EffectScale.Flat);
        if(type!=value.type){value.type=type;ValueEntry.SmartValue=value;}
        if(type is EffectScale.BuffElapsed or EffectScale.BuffStacks)BuffInspectorLayout.Draw(Property,"buff");
        if(type==EffectScale.Resource)BuffInspectorLayout.Draw(Property,"resource");
        if(type==EffectScale.Rule)BuffInspectorLayout.Draw(Property,"rule");
        BuffInspectorLayout.Draw(Property,"multiplier","계수");BuffInspectorLayout.Draw(Property,"limit","변수 상한 (0 = 없음)");
    }
}
