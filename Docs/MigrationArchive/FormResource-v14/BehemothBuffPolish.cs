using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEngine;
internal static class BehemothBuffPolish
{
    [InitializeOnLoadMethod] private static void Schedule()=>EditorApplication.delayCall+=Run;
    private static void Run()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Run;return;}
        var content=AssetDatabase.LoadAssetAtPath<GameContent>(BehemothContentBuilder.ContentPath);if(content==null)return;
        if(content.schemaVersion==12)
        {
            foreach(var item in content.equipment)
            {
                foreach(var unit in item.abilities)
                    if(unit.effects.Count>0 && unit.effects.All(e=>e.effectType is EffectType.UnlockForm or EffectType.SummonCompanion)) {unit.trigger.triggerType=TriggerType.Passive;unit.target=TargetType.OwnerStat;}
                EditorUtility.SetDirty(item);
            }
            EditorUtility.SetDirty(content.variables);content.schemaVersion=13;EditorUtility.SetDirty(content);AssetDatabase.SaveAssets();return;
        }
        if(content.schemaVersion==11)
        {
            var rage=AssetDatabase.LoadAssetAtPath<BuffData>("Assets/Data/Behemoth/Buffs/Rage.asset");var delayed=AssetDatabase.LoadAssetAtPath<BuffData>("Assets/Data/Behemoth/Buffs/DelayedDamage.asset");
            foreach(var unit in rage.Units)for(var i=0;i<unit.effects.Count;i++){var fx=unit.effects[i];if(fx.effectType==EffectType.PayDeferredDamage){fx.effectType=EffectType.RemoveBuff;fx.buff=delayed;unit.effects[i]=fx;}}
            EditorUtility.SetDirty(rage);content.schemaVersion=12;EditorUtility.SetDirty(content);AssetDatabase.SaveAssets();EditorApplication.delayCall+=Run;return;
        }
        if(content.schemaVersion!=10)return;
        var tag=ScriptableObject.CreateInstance<BuffTagData>();AssetDatabase.CreateAsset(tag,"Assets/Data/Behemoth/BuffTags/DrawAttack.asset");
        CombatStatusCatalog.Instance.poison.ReleaseData.ResetOnReapplyAtCap=true;EditorUtility.SetDirty(CombatStatusCatalog.Instance.poison);
        var sheath=AssetDatabase.LoadAssetAtPath<BuffData>("Assets/Data/Behemoth/Buffs/Sheathed.asset");
        for(var u=0;u<sheath.Units.Count;u++)for(var i=0;i<sheath.Units[u].effects.Count;i++){var fx=sheath.Units[u].effects[i];if(fx.effectType==EffectType.MarkAction){fx.buffTag=tag;sheath.Units[u].effects[i]=fx;}}
        EditorUtility.SetDirty(sheath);
        foreach(var item in content.equipment)
        {
            foreach(var unit in item.abilities)
            {
                if(unit.effects.Any(e=>e.effectType==EffectType.AddBuff && e.buff==CombatStatusCatalog.Instance.poison))unit.conditions.RemoveAll(c=>c.kind==AbilityConditionKind.Rule && c.rule==AbilityRule.BlockBuff);
                foreach(var condition in unit.conditions.Where(c=>c.kind==AbilityConditionKind.ActionHasTag))condition.buffTag=tag;
                for(var i=0;i<unit.effects.Count;i++)
                {
                    var fx=unit.effects[i];
                    if(fx.effectType==EffectType.RuleModifier && fx.rule==AbilityRule.BlockBuff)fx.buff=CombatStatusCatalog.Instance.poison;
                    if(fx.effectType==EffectType.RuleModifier && fx.rule==AbilityRule.SlowDurationBonus){fx.rule=AbilityRule.BuffDurationBonus;fx.buff=CombatStatusCatalog.Instance.slow;}
                    unit.effects[i]=fx;
                }
            }
            EditorUtility.SetDirty(item);
        }
        content.schemaVersion=11;EditorUtility.SetDirty(content);AssetDatabase.SaveAssets();
        EditorApplication.delayCall+=Run;
    }
}
