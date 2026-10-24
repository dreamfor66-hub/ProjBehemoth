using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class AshenVowGameplayPolish
{
    static AshenVowGameplayPolish()=>EditorApplication.delayCall+=Prepare;
    private static void Prepare()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Prepare;return;}
        var content=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/Data/Behemoth/GameContent.asset");
        if(content==null||content.gameplayRevision>=3)return;if(content.gameplayRevision<1){EditorApplication.delayCall+=Prepare;return;}
        var state=AssetDatabase.LoadAssetAtPath<BuffData>("Assets/Data/Behemoth/Buffs/FlowAcceleration.asset");
        var precision=AshenVowGameplayPreparation.Asset<BuffData>("Assets/Data/Behemoth/Buffs/AshenVow/FlowPrecision.asset");precision.displayName="연환 집중";precision.ReleaseData=new(){Type=BuffReleaseType.Stage,Count=1};precision.DuplicationData=new(){Type=BuffDuplicationType.UseNew};precision.Units.Clear();EditorUtility.SetDirty(precision);
        EquipmentEffect Add(){var e=AshenVowGameplayPreparation.E(EffectType.AddBuff,1);e.buff=precision;return e;}
        EquipmentEffect Remove(){var e=AshenVowGameplayPreparation.E(EffectType.RemoveBuff);e.buff=precision;return e;}
        EquipmentAbility Normal(EquipmentAbility u){var t=u.trigger;t.triggerAttackTypeFilter=TriggerAttackTypeFilter.NormalAttack;u.trigger=t;return u;}
        var source=content.perks.First(p=>p.id=="FlowFinish").abilitySource;
        source.abilities=new(){
            AshenVowGameplayPreparation.U(TriggerType.OnBuffStart,TargetType.Owner,Add(),new AbilityCondition{kind=AbilityConditionKind.ContextBuffIs,buff=state}),
            Normal(AshenVowGameplayPreparation.U(TriggerType.OnAttackResolved,TargetType.Owner,Remove(),new AbilityCondition{kind=AbilityConditionKind.ActionHitCount,threshold=0,comparator=Comparator.Equal})),
            AshenVowGameplayPreparation.U(TriggerType.OnActionPreparing,TargetType.Owner,Remove(),new AbilityCondition{kind=AbilityConditionKind.HasActionBuff,threshold=0}),
            Normal(AshenVowGameplayPreparation.U(TriggerType.OnHit,TargetType.Enemy,AshenVowGameplayPreparation.E(EffectType.Damage,10),new AbilityCondition{kind=AbilityConditionKind.BuffStack,buff=state,threshold=1,comparator=Comparator.Equal},new AbilityCondition{kind=AbilityConditionKind.OwnerHasBuff,buff=precision}))
        };
        // Starting another charged action ends the pending enhanced-normal sequence.
        var charge=source.abilities[2].trigger;charge.triggerAttackTypeFilter=TriggerAttackTypeFilter.ChargeAttack;source.abilities[2].trigger=charge;source.abilities[2].conditions.Clear();
        EditorUtility.SetDirty(source);
        var pressure=content.perks.First(p=>p.id=="Pressure").abilitySource;pressure.abilities[0].conditions.RemoveAll(c=>c.kind==AbilityConditionKind.Rule);EditorUtility.SetDirty(pressure);
        content.perks.First(p=>p.id=="ShortIncantation").maxStacks=12;EditorUtility.SetDirty(content.perks.First(p=>p.id=="ShortIncantation"));
        var blade=content.perks.First(p=>p.id=="LongBlade").abilitySource;var reach=blade.abilities[0].effects[0];reach.effectMagnitude=.08f;blade.abilities[0].effects[0]=reach;EditorUtility.SetDirty(blade);
        content.gameplayRevision=3;EditorUtility.SetDirty(content);AssetDatabase.SaveAssets();
    }
}
