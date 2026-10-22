using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

// Only the starting relic is migrated. Designer-tuned character/monster values stay intact.
internal static class BehemothStealthRevision
{
    internal const string Description = "조우 시 은신한다. 차지·캐스팅 준비 중에는 적의 행동 시간이 멈춘다. 행동 완료마다 25% → 50% → 75% → 100% 확률로 발각된다. 적 하나라도 공격받으면 무리 전체가 즉시 알아차린다.";
    [InitializeOnLoadMethod] private static void Schedule() => EditorApplication.delayCall += Apply;
    private static void Apply()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        { EditorApplication.delayCall += Apply; return; }
        var content = AssetDatabase.LoadAssetAtPath<GameContent>(BehemothContentBuilder.ContentPath);
        if (content == null || content.schemaVersion >= 14) return;
        if (content.schemaVersion < 13) { EditorApplication.delayCall += Apply; return; }
        var starter = content.characters.Single(character => character.character == CharacterKind.Rogue).startingRelic;
        Configure(starter);
        BehemothTextEditor.SyncAsset(starter);
        var table = BehemothTextEditor.Table;
        table.Set("ui.stealth.status", "은신 · 행동 후 발각 {0:0}%\n차지·주문 준비 가능 · 적 적중 시 전체 발각", "Assets/Scripts/DungeonSlash/Combat/Runtime/CharacterCombatRuntime.cs");
        table.Set("ui.stealth.unaware", "미인지", "Assets/Scripts/DungeonSlash/Combat/Presentation/MonsterView.cs");
        table.Set("ui.stealth.revealed", "발각!", "Assets/Scripts/DungeonSlash/Combat/Systems/CombatController.cs");
        EditorUtility.SetDirty(table);
        content.schemaVersion = 14; EditorUtility.SetDirty(content);
        AssetDatabase.SaveAssets();
    }

    internal static void Configure(EquipmentData starter)
    {
        starter.displayName = "그림자 망토";
        starter.description = Description;
        starter.abilities.Clear();
        starter.abilities.Add(new EquipmentAbility {
            trigger = new EquipmentTrigger { triggerType = TriggerType.Passive, chancePercent = 100, triggerCount = 1 },
            target = TargetType.OwnerStat,
            effects = new() {
                new EquipmentEffect { effectType = EffectType.RuleModifier, rule = AbilityRule.EncounterConcealment, effectMagnitude = 1, effectCount = 1 },
                new EquipmentEffect { effectType = EffectType.RuleModifier, rule = AbilityRule.DetectionChancePerAction, effectMagnitude = .25f, effectCount = 1 }
            }
        });
        EditorUtility.SetDirty(starter);
    }
}
