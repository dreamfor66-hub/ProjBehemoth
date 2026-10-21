using System.Collections.Generic;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

// Only show fields that the selected primitive actually evaluates.
[CustomPropertyDrawer(typeof(EquipmentEffect))]
internal sealed class EquipmentEffectDrawer : PropertyDrawer
{
    private static List<string> Fields(SerializedProperty property)
    {
        var result = new List<string> { "effectType" }; var type = (EffectType)property.FindPropertyRelative("effectType").enumValueIndex;
        if (type == EffectType.LearnSpell) { result.Add("spell"); return result; }
        if (type == EffectType.UnlockForm) { result.Add("form"); return result; }
        if (type == EffectType.SummonCompanion) { result.Add("companion"); return result; }
        if (type == EffectType.ResetAttackCooldown) return result;
        result.AddRange(new[] { "effectMagnitude", "effectCount" });
        if (type == EffectType.ExtraAttack) { result.Add("effectAngle"); return result; }
        if (type != EffectType.EchoSpell && type != EffectType.DetonatePoison && type != EffectType.Interrupt)
            result.AddRange(new[] { "scale", "scaleRatio", "maximumMagnitude" });
        if (type is EffectType.Damage or EffectType.ApplyStatus or EffectType.DetonatePoison or EffectType.Interrupt or EffectType.SpreadStatus) result.Add("targets");
        if (type == EffectType.Damage) result.Add("element");
        if (type is EffectType.ApplyStatus or EffectType.SpreadStatus) result.AddRange(new[] { "status", "statusDamage", "statusCap", "effectTime" });
        if (type == EffectType.EchoSpell) result.Add("effectTime");
        if (type is EffectType.StatIncrease or EffectType.RuleModifier)
        { result.Add(type == EffectType.StatIncrease ? "effectStatType" : "rule"); result.AddRange(new[] { "effectTime", "buffUses", "consumeOn", "consumeAttackTypes", "buffGroup" }); }
        return result;
    }
    private static GUIContent Label(string name) => new(name switch {
        "effectType" => "효과 종류", "effectMagnitude" => "기본 효과량", "effectCount" => "반복 / 배율", "scale" => "참조 기준", "scaleRatio" => "참조 계수", "maximumMagnitude" => "효과량 상한 (0 = 없음)", "effectTime" => "지속 / 지연 시간 (초)", "buffUses" => "사용 횟수 (0 = 시간만)", "consumeOn" => "횟수 차감 시점", "consumeAttackTypes" => "차감할 무기 공격", "buffGroup" => "중첩 방지 그룹", _ => ObjectNames.NicifyVariableName(name) });
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var height = EditorGUIUtility.singleLineHeight;
        if (property.isExpanded) foreach (var name in Fields(property)) height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative(name), true) + 2;
        return height;
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property); position.height = EditorGUIUtility.singleLineHeight;
        property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, true);
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            foreach (var name in Fields(property))
            { var field = property.FindPropertyRelative(name); position.y += position.height + 2; position.height = EditorGUI.GetPropertyHeight(field, true); EditorGUI.PropertyField(position, field, Label(name), true); }
            EditorGUI.indentLevel--;
        }
        EditorGUI.EndProperty();
    }
}
