using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

internal static class BehemothAbilityRepair
{
    [InitializeOnLoadMethod] private static void Schedule() => EditorApplication.delayCall += Repair;
    private static void Repair()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.delayCall += Repair; return; }
        var content = AssetDatabase.LoadAssetAtPath<GameContent>(BehemothContentBuilder.ContentPath);
        if (content == null || content.schemaVersion < 5 || content.schemaVersion >= 8) return;
        if (content.schemaVersion == 5)
        {
        foreach (var item in content.equipment.Where(item => item.IsPotion))
        {
            foreach (var ability in item.abilities)
                for (var i = 0; i < ability.effects.Count; i++)
                {
                    var effect = ability.effects[i];
                    if (effect.effectType == EffectType.Damage) { effect.scale = EffectScale.Flat; effect.scaleRatio = 0; effect.effectMagnitude = item.effect.effectMagnitude; ability.effects[i] = effect; }
                }
            EditorUtility.SetDirty(item);
        }
        content.schemaVersion = 6; EditorUtility.SetDirty(content); AssetDatabase.SaveAssets();
        }
        if (content.schemaVersion != 7) return;
        // Calibrated from the 30-second live core-loop comparison, not paper burst damage.
        var wizard = content.characters.Single(item => item.character == CharacterKind.Wizard); wizard.baseAttackDamage = 13.5f;
        var monk = content.characters.Single(item => item.character == CharacterKind.Monk); monk.baseChargeDamage = 13;
        var bolt = content.spells.Single(item => item.id == "EnergyBolt"); bolt.damage = 17;
        var rageCharge = content.equipment.Single(item => item.power == RelicPower.RageCharge);
        foreach (var ability in rageCharge.abilities.Where(ability => ability.effects.Any(effect => effect.effectType == EffectType.AddResource))) ability.trigger.triggerType = TriggerType.OnAttackResolved;
        EditorUtility.SetDirty(rageCharge);
        EditorUtility.SetDirty(wizard); EditorUtility.SetDirty(monk); EditorUtility.SetDirty(bolt);
        content.schemaVersion = 8; EditorUtility.SetDirty(content); AssetDatabase.SaveAssets();
    }
}
