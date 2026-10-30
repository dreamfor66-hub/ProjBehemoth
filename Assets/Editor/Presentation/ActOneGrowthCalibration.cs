using System;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

// Applies a saved production specification once. Later designer edits are not regenerated.
[InitializeOnLoad]
internal static class ActOneGrowthCalibration
{
    const string Folder = "Docs/Production/WarriorActOne-2026-10-28/";
    const string Request = "Temp/ActOneGrowth.prepare";
    [Serializable] public sealed class Spec { public Build[] builds; public Value[] values; }
    [Serializable] public sealed class Build
    {
        public string label, acquisitionBasis; public int checkpoint, scenario, experience, shopGold;
        public float fatigue; public Grant[] perks; public string[] relics;
    }
    [Serializable] public sealed class Grant { public string path; public int stacks; public bool fromEvent; }
    [Serializable] public sealed class Value
    {
        public string path, basis; public float offense, health, guard, utility, uptime = 1;
    }
    static ActOneGrowthCalibration() => EditorApplication.update += Poll;
    static T Load<T>(string path) where T:UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>("Assets/"+path);
    static void Save(UnityEngine.Object item) { EditorUtility.SetDirty(item); AssetDatabase.SaveAssetIfDirty(item); }
    static void Poll()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Request);
        try { Prepare(); File.WriteAllText(Folder+"GrowthPrepared.txt",DateTime.UtcNow.ToString("O")); }
        catch(Exception e) { File.WriteAllText(Folder+"GrowthPreparationError.txt",e.ToString()); Debug.LogException(e); }
    }
    static EquipmentAbility Acquisition(params StatModifier[] stats) => new EquipmentAbility {
        trigger = new EquipmentTrigger { triggerType=TriggerType.OnAcquire,triggerCount=1,chancePercent=100,
            triggerAttackTypeFilter=TriggerAttackTypeFilter.All,triggerAttackWayFilter=TriggerAttackWayFilter.All },
        effects = stats.Select(s=>new EquipmentEffect {effectType=EffectType.StatIncrease,effectStatType=s.kind,effectMagnitude=s.value,effectCount=1}).ToList()
    };
    static void Prepare()
    {
        var spec=JsonUtility.FromJson<Spec>(File.ReadAllText(Folder+"GrowthSpecification.json"));
        var data=Load<ActGrowthBudget>("Data/Balance/Act1GrowthBudget.asset");
        data.requiredUnits=4.55f;data.requiredOffenseUnits=3.55f;data.requiredHealthUnits=1;
        data.designIntent="1막 보스 전 기준 4.55U: 실제 4회 레벨업(날 선 무기 2/묵직한 무기 1/활력 1)+날 선 부적. 체력/가드/공격 가치를 따로 기록. 미달 빌드와 피로 누적은 더 어려워야 하며 자동 보너스는 지급하지 않음.";
        data.referenceBuilds.Clear();
        foreach(var row in spec.builds)
            data.referenceBuilds.Add(new GrowthReferenceBuild {label=row.label,checkpoint=(GrowthCheckpoint)row.checkpoint,scenario=(GrowthScenario)row.scenario,
                acquisitionBasis=row.acquisitionBasis,experience=row.experience,shopGold=row.shopGold,fatigue=row.fatigue,
                perks=row.perks.Select(p=>new GrowthPerkGrant {perk=Load<PerkData>(p.path),stacks=p.stacks,fromEvent=p.fromEvent}).ToList(),
                relics=row.relics.Select(Load<EquipmentData>).ToList()});
        data.valuations.Clear();
        foreach(var row in spec.values)
            data.valuations.Add(new GrowthValuation {content=Load<LocalizedData>(row.path),offenseUnits=row.offense,healthUnits=row.health,
                guardUnits=row.guard,utilityUnits=row.utility,estimatedUnits=row.offense+row.health+row.guard,assumedUptime=row.uptime,
                needsDesignerReview=false,basis=row.basis});
        // Broken reward definitions must contribute their stated effect before growth can be measured.
        var hunter=Load<EquipmentData>("Data/Items/Relics/HunterLantern.asset");
        hunter.abilities.Clear();hunter.abilities.Add(new EquipmentAbility {
            trigger=new EquipmentTrigger {triggerType=TriggerType.OnKill,triggerCount=1,chancePercent=100,triggerAttackTypeFilter=TriggerAttackTypeFilter.All,triggerAttackWayFilter=TriggerAttackWayFilter.All},
            effects=new(){new EquipmentEffect {effectType=EffectType.Heal,effectMagnitude=7,effectCount=1}}});Save(hunter);
        var wire=Load<EquipmentData>("Data/Items/Relics/ThunderWire.asset");
        wire.abilities.Clear();wire.abilities.Add(Acquisition(new StatModifier {kind=ModifierKind.NormalDamage,value=.2f},new StatModifier {kind=ModifierKind.AttackCooldownMultiplier,value=.12f}));Save(wire);
        // OnAcquire RuleModifier becomes a stage buff and is cleared at BeginCombat.
        // Permanent passive rules belong in a Passive unit; permanent stats remain acquisition effects.
        var starter=Load<EquipmentData>("Data/Items/Relics/Starter_Warrior.asset");
        starter.abilities.Clear();starter.abilities.Add(Acquisition(new StatModifier {kind=ModifierKind.BrokenShieldRegen,value=3}));
        starter.abilities.Add(new EquipmentAbility {target=TargetType.OwnerStat,
            trigger=new EquipmentTrigger {triggerType=TriggerType.Passive,triggerCount=1,chancePercent=100},
            effects=new(){new EquipmentEffect {effectType=EffectType.RuleModifier,rule=AbilityRule.GuardRecoveryPerSecond,effectMagnitude=2,effectCount=1}}});
        starter.description="전투 중 가드게이지가 초당 2 회복됩니다. 가드 붕괴 중 회복 속도가 추가로 3 증가합니다.";
        BehemothTextEditor.SyncAsset(starter);Save(starter);
        // Comparable elite/boss phase counts at the declared middle/pre-boss progression.
        // Preserve authored damage and species size; do not touch B2/B3.
        foreach(var row in new[]{("MirrorKnight",420f),("ArmoredExecutioner",420f),("BellKeeper",820f),("BroodKeeper",760f)})
        {var monster=Load<MonsterData>("Data/Combat/Monsters/"+row.Item1+".asset");monster.maxHp=row.Item2;Save(monster);}
        // These committed attacks resolve twice if not broken. A single guard cannot erase
        // the entire failed weak-point window through the shield's existing no-overflow rule.
        foreach(var name in new[]{"IronMantis-1","OssuaryBell-1","WebDuchess-1"})
        {var attack=Load<MonsterAttackData>("Data/Presentation/Angular/Attacks/"+name+".asset");attack.combatMechanics.hitCount=2;attack.combatMechanics.followupDelay=.45f;Save(attack);}
        var distance=Load<EquipmentData>("Data/Items/Relics/DistanceMomentum.asset");
        foreach(var ability in distance.abilities)for(var i=0;i<ability.effects.Count;i++)
        {
            var effect=ability.effects[i];
            if(effect.effectType==EffectType.StatIncrease && effect.effectStatType==ModifierKind.DamagePerAttackDistance){effect.effectMagnitude=.0006f;ability.effects[i]=effect;}
        }
        distance.description="베기 길이 100마다 피해 +6%";BehemothTextEditor.SyncAsset(distance);Save(distance);Save(BehemothTextEditor.Table);
        var formation=Load<EncounterData>("Data/Dungeon/Encounters/MazeLeeches.asset");
        if(formation.members.Count==3) formation.members.RemoveAt(1);
        formation.members[0].offset=new Vector2(-280,45);formation.members[0].actionDelay=0;
        formation.members[1].offset=new Vector2(280,-45);formation.members[1].actionDelay=1.2f;Save(formation);
        // Count only formations the actual planner can draw (act-one normal maximum is two).
        var stage=data.stage;var settings=Load<RunBalanceSettings>("Data/Settings/RunBalanceSettings.asset");
        float Xp(System.Collections.Generic.IEnumerable<EncounterData> list) => (float)list.Average(e=>e.members.Sum(m=>m.monster.experienceReward));
        var early=Xp(stage.earlyCombats.Where(e=>e.members.Count==1));var packs=Xp(stage.hardCombats.Where(e=>e.members.Count<=2));
        var count=data.normalCombatOpportunities;var opening=Mathf.Min(stage.easyCombatCount,count);
        data.expectedExperience=opening*early+(count-opening)*Mathf.Lerp(early,packs,stage.packChance)+data.eliteCombatOpportunities*Xp(stage.elites)*2+settings.goddessExperience*data.goddessOpportunities;
        var remaining=Mathf.FloorToInt(data.expectedExperience);var requirement=settings.initialExperienceRequirement;var levels=0;
        while(remaining>=requirement){remaining-=requirement;levels++;requirement+=settings.experienceRequirementGrowth;}
        data.cumulativeLevelUpPerks=levels;
        var perkMean=BestOfThree(data.valuations.Where(v=>v.content is PerkData).ToArray());
        var sharedMean=data.valuations.Where(v=>v.content is EquipmentData e&&e.scope==RelicScope.Shared).Average(v=>v.estimatedUnits);
        var shopMean=BestOfThree(data.valuations.Where(v=>v.content is EquipmentData e&&e.scope==RelicScope.Shared).Select(v=>v.estimatedUnits).ToArray());
        data.opportunities.Clear();
        data.opportunities.Add(new GrowthOpportunity {source="레벨업: 실제 워리어 풀에서 3택1",opportunities=levels,unitsPerAcquisition=perkMean,assumption="15개 동등 가중치 버킷(MP 2종은 한 버킷)의 모든 비복원 3택 조합을 열거한 현재 유효 가치 최대값. 주문 없는 워리어 기준. 조건부 가정은 개별 valuation 참조."});
        data.opportunities.Add(new GrowthOpportunity {source="상자 방문: 무작위 공유 유물 1개",opportunities=1,unitsPerAcquisition=sharedMean,assumption="실제 29개 공유 유물/마도서의 조건부 환산 평균. 상자는 방문 가정이며 보장 경로 보상이 아님. 골드 방은 유물로 중복 계산하지 않음."});
        data.opportunities.Add(new GrowthOpportunity {source="성장 이벤트 선택",opportunities=2,acquisitionRate=.5f,unitsPerAcquisition=.75f,assumption="표준 탐색에서 이벤트 2회 중 성장 선택 1회라는 명시적 시나리오. 확률 측정치 아님. 피/금/소모품 대가, 다음 막 이연 보상은 1막 즉시 성장으로 미산입."});
        data.opportunities.Add(new GrowthOpportunity {source="상점 선택 구매",opportunities=1,acquisitionRate=1,unitsPerAcquisition=shopMean,assumption="현재 공유 유물29종의 모든 비복원 3택 조합에서 유효가치 최대값 열거. 가장 비싼 재고86G+회복/이벤트 예비비60G를 표준 탐색 예상수입 약224G로 충당하는 시나리오. 방문/구매를 강제하거나 실측 구매율로 주장하지 않음."});
        Save(data);
        File.WriteAllText(Folder+"GrowthComputed.json",JsonUtility.ToJson(data,true));
    }
    static float BestOfThree(GrowthValuation[] rows)
    {
        var buckets=rows.GroupBy(v=>((PerkData)v.content).offerGroup!=null?(UnityEngine.Object)((PerkData)v.content).offerGroup:v.content).Select(g=>g.Average(v=>v.estimatedUnits)).ToArray();
        return BestOfThree(buckets);
    }
    static float BestOfThree(float[] buckets)
    {
        double sum=0;var count=0;
        for(var i=0;i<buckets.Length;i++)for(var j=i+1;j<buckets.Length;j++)for(var k=j+1;k<buckets.Length;k++) {sum+=Math.Max(buckets[i],Math.Max(buckets[j],buckets[k]));count++;}
        return count==0?0:(float)(sum/count);
    }
}
