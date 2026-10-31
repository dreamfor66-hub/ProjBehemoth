using System;
using System.IO;
using System.Linq;
using System.Text;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class ActGrowthBudgetPreparation
{
    const string Folder = "Docs/Production/ActOneExperience-2026-10-28/";
    const string Root = "Assets/Data/Balance/";
    static bool busy; static double next;
    static ActGrowthBudgetPreparation() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (busy || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup < next
            || File.Exists(Folder + "GrowthBudgetsInstalled.txt") || !File.Exists(Folder + "FootprintsInstalled.txt")) return;
        // Saved, calibrated production data supersedes this legacy draft, even if a Docs stamp is removed.
        var calibrated=DungeonSlash.DesignerAssetPaths.Load<ActGrowthBudget>(Root+"Act1GrowthBudget.asset");
        if(calibrated!=null && calibrated.referenceBuilds.Count>0)return;
        next = EditorApplication.timeSinceStartup + 5; busy = true;
        try { Prepare(); File.WriteAllText(Folder + "GrowthBudgetsInstalled.txt", "Design budgets and deterministic reward-opportunity accounting, not repeated random-run averages.\n"); }
        catch (Exception e) { File.WriteAllText(Folder + "GrowthBudget-Error.txt", e.ToString()); Debug.LogException(e); }
        finally { busy = false; }
    }
    static void Prepare()
    {
        Directory.CreateDirectory(Root); AssetDatabase.Refresh();
        var balance = DungeonSlash.DesignerAssetPaths.Load<RunBalanceSettings>("Assets/Data/Settings/RunBalanceSettings.asset");
        var stages = AssetDatabase.FindAssets("t:StageSetData", new[] { "Assets/Data/Dungeon/Stages" }).Select(g => DungeonSlash.DesignerAssetPaths.Load<StageSetData>(AssetDatabase.GUIDToAssetPath(g))).OrderBy(s => s.act).ToArray();
        var report = new StringBuilder("# 막별 성장 요구량과 획득 기회\n\n이 수치는 첫 설계 가정이다. 랜덤 런 평균이나 검증된 최적 밸런스가 아니다. 막 보스 **진입 전**의 누적 예산을 사용하며 해당 보스 처치 보상은 다음 막에만 더한다.\n\n1 성장 단위(U)는 기준 빌드의 실효 공격 기여 약 10% 또는 방어 기여 최대 체력 10에 해당하는 설계 교환 단위다. 조건부 효과의 발동률·소모 자원·방어 및 유틸 가치는 분리 검토해야 한다.\n\n|막|보스 전 누적 XP|레벨업 퍽 누적|공급 기대 U|요구 U|부족 / 기준 / 상향 U|\n|---|---:|---:|---:|---:|---|\n");
        float inheritedXp = 0, inheritedNonLevelUnits = 0;
        foreach (var stage in stages)
        {
            var path = Root + "Act" + stage.act + "GrowthBudget.asset";
            var data = DungeonSlash.DesignerAssetPaths.Load<ActGrowthBudget>(path);
            if (data == null) { data = ScriptableObject.CreateInstance<ActGrowthBudget>(); AssetDatabase.CreateAsset(data, path); }
            data.act = stage.act; data.stage = stage; data.normalCombatOpportunities = 8; data.eliteCombatOpportunities = stage.act;
            data.requiredUnits = stage.act == 1 ? 5 : stage.act == 2 ? 12 : 20;
            data.designIntent = "보스 진입 전 요구 성장량. 획득 기회, 선택 수락률, 효과 가치는 조정 가능한 설계 가정이며 랜덤 런 평균으로 숨기지 않는다.";
            var earlyXp = AverageXp(stage.earlyCombats); var packXp = AverageXp(stage.hardCombats);
            var early = Mathf.Min(stage.easyCombatCount, data.normalCombatOpportunities);
            var normalXp = early * earlyXp + (data.normalCombatOpportunities - early) * Mathf.Lerp(earlyXp, packXp, stage.packChance);
            var floorXp = normalXp + data.eliteCombatOpportunities * AverageXp(stage.elites) * 2 + balance.goddessExperience * data.goddessOpportunities;
            data.inheritedExperience = inheritedXp; data.expectedExperience = inheritedXp + floorXp;
            var xp = Mathf.FloorToInt(data.expectedExperience); var requirement = balance.initialExperienceRequirement; var perks = 0;
            while (xp >= requirement) { xp -= requirement; perks++; requirement += balance.experienceRequirementGrowth; }
            data.cumulativeLevelUpPerks = perks;
            data.opportunities.Clear();
            data.opportunities.Add(new GrowthOpportunity { source = "지금까지 레벨업 퍽", opportunities = perks, unitsPerAcquisition = .8f, assumption = "최소 3개 선택지 중 빌드에 쓸 수 있는 선택의 실효 평균을 0.8U로 가정. 전체 퍽을 같은 값으로 확정한 것이 아님." });
            data.opportunities.Add(new GrowthOpportunity { source = "이전 막의 비레벨업 성장", opportunities = 1, unitsPerAcquisition = inheritedNonLevelUnits, assumption = "이전 막 상자·이벤트·상점의 기대치와 그 막 보스 처치 후 유물. 시작 유물은 클래스 기본 성능으로 제외." });
            data.opportunities.Add(new GrowthOpportunity { source = "유물 상자", opportunities = 1, unitsPerAcquisition = 2, assumption = "1막의 다른 상자는 골드이며 유물로 중복 계산하지 않음. 상자 방문을 가정한 기회." });
            data.opportunities.Add(new GrowthOpportunity { source = "이벤트 성장 선택", opportunities = 3, acquisitionRate = .35f, unitsPerAcquisition = 1.5f, assumption = "이벤트 수 자체를 유물 획득 수로 보지 않음. 선택 조건·대가를 감수하는 실효 성장 수락률 35%를 임시 가정. 이벤트 데이터 선별 후 재산정." });
            data.opportunities.Add(new GrowthOpportunity { source = "상점 성장 구매", opportunities = stage.act, acquisitionRate = .3f, unitsPerAcquisition = 2, assumption = "상점은 무료 유물이 아님. 보유 골드·포션 지출·가격을 고려한 구매율 가정. 골드 상자는 이 구매 기회와 중복 성장으로 더하지 않음." });
            data.valuations.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:PerkData", new[] { "Assets/Data/Items/Perks" }))
            {
                var perk = DungeonSlash.DesignerAssetPaths.Load<PerkData>(AssetDatabase.GUIDToAssetPath(guid));
                var value = new GrowthValuation { content = perk, basis = "기본 워리어 평타 14 / 차지 24, 평타 비중 70%, 브레이크 후 비중 25% 가정. ", estimatedUnits = 0 };
                foreach (var modifier in perk.modifiers)
                {
                    switch (modifier.kind)
                    {
                        case ModifierKind.FlatNormalDamage: value.estimatedUnits += modifier.value / 14 * data.normalDamageShare / .1f; break;
                        case ModifierKind.FlatChargeDamage: value.estimatedUnits += modifier.value / 24 * (1 - data.normalDamageShare) / .1f; break;
                        case ModifierKind.NormalDamage: value.estimatedUnits += modifier.value * data.normalDamageShare / .1f; break;
                        case ModifierKind.ChargeDamage: value.estimatedUnits += modifier.value * (1 - data.normalDamageShare) / .1f; break;
                        case ModifierKind.StunDamageMultiplier: value.estimatedUnits += modifier.value * data.breakFollowupShare / .1f; break;
                        case ModifierKind.MaxHp: value.estimatedUnits += modifier.value / 10; break;
                        case ModifierKind.AttackCooldownMultiplier: value.estimatedUnits += (1 / Mathf.Max(.1f, 1 - modifier.value) - 1) / .1f; break;
                        default: value.needsDesignerReview = true; break;
                    }
                }
                if (perk.modifiers.Count == 0) value.needsDesignerReview = true;
                value.basis += value.needsDesignerReview ? "조건부 능력·범위·마나 등 수동 평가 필요. 0은 무가치 판정이 아니라 미산정." : "명시된 단순 능력치만 환산. 능력 소스와 실제 발동·중복 효과는 별도 확인.";
                data.valuations.Add(value);
            }
            foreach (var guid in AssetDatabase.FindAssets("t:EquipmentData", new[] { "Assets/Data/Items/Relics" }))
            {
                var relic = DungeonSlash.DesignerAssetPaths.Load<EquipmentData>(AssetDatabase.GUIDToAssetPath(guid));
                if (relic.scope == RelicScope.Starting) continue;
                data.valuations.Add(new GrowthValuation { content = relic, estimatedUnits = 2, needsDesignerReview = true, basis = "공통 유물 목표 예산 2U. 개별 유물 확정 가치가 아님. 발동당 효과 × 예상 발동 횟수 / 전투 시간, 조건 충족률, 소모 자원과 빌드 시너지를 검토하여 덮어쓸 것." });
            }
            EditorUtility.SetDirty(data); AssetDatabase.SaveAssetIfDirty(data);
            report.AppendLine($"|{stage.act}|{data.expectedExperience:F1}|{perks}|{data.AvailableExpectedUnits:F2}|{data.requiredUnits:F1}|{data.UnitsFor(GrowthScenario.UnderBudget):F2} / {data.requiredUnits:F2} / {data.UnitsFor(GrowthScenario.Strong):F2}|");
            inheritedXp = data.expectedExperience + AverageXp(stage.bosses);
            inheritedNonLevelUnits = data.opportunities.Skip(1).Sum(o => o.ExpectedUnits) + 2;
        }
        foreach (var stage in stages)
        {
            var data = DungeonSlash.DesignerAssetPaths.Load<ActGrowthBudget>(Root + "Act" + stage.act + "GrowthBudget.asset");
            report.AppendLine($"\n## {stage.act}막 기회 계산\n");
            foreach (var row in data.opportunities) report.AppendLine($"- {row.source}: {row.opportunities:F2}회 × 수락/구매율 {row.acquisitionRate:P0} × {row.unitsPerAcquisition:F2}U = {row.ExpectedUnits:F2}U. {row.assumption}");
            report.AppendLine("\n### 개별 퍽의 첫 환산\n");
            foreach (var value in data.valuations.Where(v => v.content is PerkData)) report.AppendLine($"- {((PerkData)value.content).displayName}: {(value.needsDesignerReview ? "검토 필요; 산정된 부분 " : "단순 능력치 ")}{value.estimatedUnits:F2}U. {value.basis}");
        }
        report.AppendLine("\n검증용 고정 성장 빌드는 총 U의 80%를 공격(1U당 평타/차지 피해 +10%), 20%를 방어(1U당 최대 HP +10)로 배분한다. 실제 랜덤 퍽/유물과 완전히 같은 빌드가 아니며, 공격 비중·방어 비중이 다른 빌드와 조건부·유틸 유물은 별도 한계로 남긴다. 성장 예산을 검증한다고 플레이어 실제 런에 이 보너스를 자동 지급하지 않는다.\n");
        File.WriteAllText(Folder + "GrowthBudget.md", report.ToString());
    }
    static float AverageXp(System.Collections.Generic.IEnumerable<EncounterData> encounters)
    {
        var valid = encounters.Where(e => e != null).ToArray();
        return valid.Length == 0 ? 0 : (float)valid.Average(e => e.members.Where(m => m.monster != null).Sum(m => m.monster.experienceReward));
    }
}
