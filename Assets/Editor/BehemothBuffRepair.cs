using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEngine;
internal static class BehemothBuffRepair
{
    [InitializeOnLoadMethod] private static void Schedule()=>EditorApplication.delayCall+=Run;
    private static void Run()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Run;return;}
        var content=AssetDatabase.LoadAssetAtPath<GameContent>(BehemothContentBuilder.ContentPath);if(content==null||content.schemaVersion!=9)return;
        BuffData Buff(string name)=>AssetDatabase.LoadAssetAtPath<BuffData>("Assets/Data/Behemoth/Buffs/"+name+".asset");
        foreach(var name in new[]{"SheathParryWindow","PerfectGuardWindow"})
        {var data=Buff(name);for(var u=0;u<data.Units.Count;u++)for(var i=0;i<data.Units[u].effects.Count;i++){var fx=data.Units[u].effects[i];if(fx.effectType==EffectType.GuardCost){fx.guardFeedback=name=="SheathParryWindow"?GuardFeedback.Parry:GuardFeedback.Perfect;data.Units[u].effects[i]=fx;}}EditorUtility.SetDirty(data);}
        var delayed=ScriptableObject.CreateInstance<BuffData>();delayed.displayName="지연 피해";delayed.ReleaseData=new(){Type=BuffReleaseType.Time,Time=5};delayed.DuplicationData.Type=BuffDuplicationType.None;
        var rage=Buff("Rage");foreach(var unit in rage.Units)for(var i=0;i<unit.effects.Count;i++){var fx=unit.effects[i];if(fx.effectType==EffectType.RuleModifier && fx.rule==AbilityRule.DamageMultiplier){fx.rule=AbilityRule.WeaponBaseMultiplier;unit.effects[i]=fx;}}EditorUtility.SetDirty(rage);
        EquipmentEffect Amount(EffectType type,EffectScale scale,float multiplier)=>new(){effectType=type,effectCount=1,valueVariables=new(){new(){type=scale,multiplier=multiplier}}};
        var tick=new EquipmentAbility{trigger=new(){triggerType=TriggerType.OnTimer,intervalSeconds=.05f,triggerCount=1,chancePercent=100},target=TargetType.Owner,effects=new(){Amount(EffectType.Damage,EffectScale.DamageDealt,.01f)}};delayed.Units.Add(tick);
        var reduce=new EquipmentAbility{mergeConditionTrigger=true,trigger=tick.trigger,target=TargetType.ThisBuff,effects=new(){Amount(EffectType.ChangeBuffValue,EffectScale.DamageDealt,-.01f)}};delayed.Units.Add(reduce);
        delayed.Units.Add(new(){trigger=new(){triggerType=TriggerType.OnBuffEnd,triggerCount=1,chancePercent=100},target=TargetType.Owner,effects=new(){Amount(EffectType.Damage,EffectScale.BuffValue,1)}});
        AssetDatabase.CreateAsset(delayed,"Assets/Data/Behemoth/Buffs/DelayedDamage.asset");
        foreach(var item in content.equipment)
        {
            foreach(var unit in item.abilities)
            {
                if(unit.trigger.triggerType==TriggerType.Passive)unit.target=TargetType.OwnerStat;
                for(var i=0;i<unit.effects.Count;i++){var fx=unit.effects[i];if(fx.effectType==EffectType.RuleModifier && fx.rule==AbilityRule.DeferredDamageSeconds)fx.buff=delayed;if(fx.effectType==EffectType.RuleModifier && fx.rule==AbilityRule.BuffDurationBonus)fx.buff=Buff("SheathParryWindow");unit.effects[i]=fx;}
            }
            EditorUtility.SetDirty(item);
        }
        // Strip removed class fields from every serialized character, preserving its values.
        foreach(var character in content.characters)EditorUtility.SetDirty(character);
        content.schemaVersion=10;EditorUtility.SetDirty(content);AssetDatabase.SaveAssets();BehemothTextEditor.PrepareForVerification();
    }
}
