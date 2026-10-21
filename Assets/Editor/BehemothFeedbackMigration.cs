using TMPro;
using System;
using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

internal static partial class BehemothContentBuilder
{
    [InitializeOnLoadMethod] private static void ScheduleFeedback() => EditorApplication.delayCall += AutoFeedback;
    private static void AutoFeedback()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += AutoFeedback; return; }
        var existing = AssetDatabase.LoadAssetAtPath<GameContent>(ContentPath);
        if (existing == null || existing.schemaVersion < 1 || existing.schemaVersion >= 2) return;
        // Never replace a designer's unsaved scene with a generated scene.
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty) return;
        try { ApplyFeedback(); } catch (Exception exception) { Debug.LogException(exception); }
    }
    internal static void ApplyFeedback()
    {
        content = AssetDatabase.LoadAssetAtPath<GameContent>(ContentPath);
        relics.Clear(); encounters.Clear();
        foreach (var relic in content.equipment.Where(item => item != null && item.power != RelicPower.None)) relics[relic.power] = relic;
        foreach (var guid in AssetDatabase.FindAssets("t:EncounterData", new[] { Root }))
        { var encounter = AssetDatabase.LoadAssetAtPath<EncounterData>(AssetDatabase.GUIDToAssetPath(guid)); encounters[encounter.id] = encounter; }
        foreach (var character in content.characters)
        {
            character.guardDamageReduction = 1f;
            if (character.character == CharacterKind.Monk) character.attackCooldown = .55f;
            if (character.character == CharacterKind.Samurai) character.description = "가드로 납도하고 다음 공격을 발도로 바꾼다. 납도 직후 0.1초 패리는 가드게이지를 쓰지 않고 다음 발도를 차지로 강화한다.";
            if (character.character == CharacterKind.Barbarian) character.startingRelic.description = "일반공격 격노 +12. 40 이상에서 차지 시 분노 100. 분노 중 피해 +50%, 공격 쿨타임 -38%, 받는 피해 -30%. 초당 격노 20 소모. 분노 중 일반공격은 격노를 채우지 않는다.";
            if (character.character == CharacterKind.Monk) character.startingRelic.description = "마지막 일반공격 두 방향 → 방향 무관 차지.\n↑↓ 붕권: 차지 피해 ×2.5.\n↑↑ 백렬권: 차지 피해 ×0.65로 3회 타격, 약점도 3회.\n↓→ 호신격: 차지 피해 ×1, 다음 체력 피해 50% 감소.\n→↑ 연환: 차지 피해 ×1.15, 다음 일반공격 2회 공격 쿨타임 40% 감소.\n가드해도 연계 유지. 차지 시 기록 초기화.";
            EditorUtility.SetDirty(character); EditorUtility.SetDirty(character.startingRelic);
        }
        Describe(RelicPower.PerfectMending, "완벽한 수호", "가드 시작 0.1초에 퍼펙트가드 가능. 성공 시 가드게이지를 소모하지 않고 10초 동안 모든 피해 +30%. 다시 성공하면 지속시간 갱신.");
        Describe(RelicPower.LongRage, "끝나지 않는 격노", "분노 중 일반공격으로 격노 +6. 초당 격노 소모량이 20에서 12로 감소.");
        Describe(RelicPower.ParryRecovery, "틈을 베다", "납도패리 성공 시 공격 쿨타임 즉시 초기화.");
        Describe(RelicPower.ShadowCounter, "그림자 반격", "가드 성공 후 다음 무기 공격 피해 +150%. 중첩되지 않는다.");
        Describe(RelicPower.Paladin, "성기사의 길", "가드 성공마다 주문의 남은 쿨타임 -1초. 잿불 화살 습득.");
        Describe(RelicPower.PilgrimOath, "순례자의 서약", "가드 3회마다 가드게이지 3 회복. 길 잃은 신도에게 인연을 증명할 수 있다.");
        Describe(RelicPower.LongTiger, "맹호의 두루마리", "긴 초식 ↓↑↑ 해금. 방향 무관 차지로 맹호붕권 발동: 차지 피해 ×4. 마지막 세 일반공격이 일치하면 기본 초식보다 우선한다.");
        Describe(RelicPower.LongDragon, "유룡의 두루마리", "긴 초식 ↑↓→ 해금. 방향 무관 차지로 유룡연타 발동: 차지 피해 ×0.75로 4회 타격. 약점에도 4회 적용.");
        foreach (var relic in content.equipment)
        { relic.description = relic.description.Replace("공격 대기시간", "공격 쿨타임").Replace("공격 대기", "공격 쿨타임").Replace("대기시간이 점점 감소", "공격 쿨타임이 점점 감소"); EditorUtility.SetDirty(relic); }
        foreach (var id in new[] { "MazeRat", "ForgeMites" })
        {
            var encounter = encounters[id]; encounter.members = encounter.members.Take(1).ToList(); encounter.members[0].offset = Vector2.zero; encounter.members[0].scale = 1f;
            encounter.displayName = id == "MazeRat" ? "굴쥐" : "용광로 진드기"; EditorUtility.SetDirty(encounter);
        }
        foreach (var stage in content.stages)
        {
            stage.easyCombatCount = 6; stage.tripleCombatCount = 9; stage.packChance = .35f;
            stage.earlyCombats.RemoveAll(encounter => encounter.members.Count != 1 || encounter.id == "MazeCaller");
            foreach (var encounter in stage.hardCombats)
            { for (var i = 0; i < encounter.members.Count; i++) encounter.members[i].actionDelay = i * 1.6f; EditorUtility.SetDirty(encounter); }
            EditorUtility.SetDirty(stage);
        }
        content.perks.RemoveAll(perk => !PerkSystem.IsAvailable(perk));
        ReviseEvents();
        // Patch only the requested UI objects. Preserve the combat scene and all designer tuning.
        var main = EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity");
        var expansion = UnityEngine.Object.FindFirstObjectByType<RunExpansionView>();
        var positions = new[] { new Vector2(-175, -275), new Vector2(175, -275), new Vector2(0, -133), new Vector2(0, -410) };
        for (var i = 0; i < 4; i++)
        {
            var button = expansion.spellButtons[i]; var rect = (RectTransform)button.transform; rect.anchoredPosition = positions[i];
            var colors = button.colors; colors.disabledColor = Color.white; button.colors = colors; button.interactable = false;
            foreach (var graphic in button.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            button.gameObject.SetActive(false);
        }
        expansion.characterStatus.rectTransform.anchoredPosition = new Vector2(0, -74);
        expansion.companionLabel.rectTransform.anchoredPosition = new Vector2(0, -30); expansion.companionLabel.rectTransform.sizeDelta = new Vector2(510, 52); expansion.companionLabel.fontSize = 16;
        EditorUtility.SetDirty(expansion);
        EditorSceneManager.SaveScene(main);
        var selection = EditorSceneManager.OpenScene("Assets/Scenes/OutGameScene.unity");
        UnityEngine.Object.FindFirstObjectByType<OutGameView>().Select(0);
        var controls = UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(text => text.name == "Controls");
        if (controls != null) controls.text = "빠르게 긋기  일반공격    ·    전장 홀드 → 긋기  차지    ·    캐릭터 홀드  가드";
        EditorSceneManager.SaveScene(selection);
        content.schemaVersion = 2; EditorUtility.SetDirty(content); AssetDatabase.SaveAssets();
    }
    private static void Describe(RelicPower power, string title, string text)
    { var relic = relics[power]; relic.displayName = title; relic.description = text; EditorUtility.SetDirty(relic); }
}
