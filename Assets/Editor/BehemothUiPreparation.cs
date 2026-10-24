using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DungeonSlash;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>One-time, asset-first layout upgrade. Subsequent designer edits are not rebuilt.</summary>
[InitializeOnLoad]
internal static class BehemothUiPreparation
{
    private const int Version = 21;
    private static readonly Color Surface = new(.045f, .065f, .095f, 1);
    private static readonly Color Card = new(.10f, .15f, .21f, 1);
    private static readonly Color Gold = new(1f, .79f, .26f, 1);
    static BehemothUiPreparation() => EditorApplication.delayCall += Prepare;

    internal static void Prepare()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        { EditorApplication.delayCall += Prepare; return; }
        var content = AssetDatabase.LoadAssetAtPath<GameContent>(BehemothContentBuilder.ContentPath);
        if (content == null || content.schemaVersion < 19 || BehemothTypography.Font == null)
        { EditorApplication.delayCall += Prepare; return; }
        if (content.schemaVersion >= Version) return;
        // Never silently save or discard a designer's unsaved scene work.
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
        { EditorApplication.delayCall += Prepare; return; }
        var previous = SceneManager.GetActiveScene();
        SeedText();
        UpgradeScene("Assets/Scenes/MainGameScene.unity", UpgradeMain);
        UpgradeScene("Assets/Scenes/OutGameScene.unity", UpgradeSelection);
        if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        content.schemaVersion = Version; EditorUtility.SetDirty(content); AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/Behemoth-UiPreparation.txt", "Schema 21: central modal layer, character summary, central selection/event layouts, six fixed merchant slots.");
    }

    private static void UpgradeScene(string path, Action<Scene> upgrade)
    {
        var scene = SceneManager.GetSceneByPath(path);
        var alreadyOpen = scene.IsValid() && scene.isLoaded;
        if (!alreadyOpen) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        upgrade(scene);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        if (!alreadyOpen) EditorSceneManager.CloseScene(scene, true);
    }

    private static T Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<T>(true)).First();
    private static T Ref<T>(Object target, string name) where T : Object =>
        new SerializedObject(target).FindProperty(name).objectReferenceValue as T;
    private static T[] Refs<T>(Object target, string name) where T : Object
    {
        var array = new SerializedObject(target).FindProperty(name);
        return Enumerable.Range(0, array.arraySize).Select(i => array.GetArrayElementAtIndex(i).objectReferenceValue as T).ToArray();
    }
    private static RectTransform Place(Transform target, Vector2 position, Vector2 size)
    {
        var rect = (RectTransform)target;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
        rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }
    private static RectTransform NewRect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var found = parent.Find(name);
        if (found != null) return Place(found, position, size);
        var root = new GameObject(name, typeof(RectTransform)); root.transform.SetParent(parent, false);
        return Place(root.transform, position, size);
    }
    private static Image Box(string name, Transform parent, Vector2 position, Vector2 size, Color color, bool raycast = false)
    {
        var rect = NewRect(name, parent, position, size);
        var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
        image.color = color; image.raycastTarget = raycast; return image;
    }
    private static TextMeshProUGUI Text(string name, Transform parent, Vector2 position, Vector2 size, int fontSize, string key = null)
    {
        var rect = NewRect(name, parent, position, size);
        var label = rect.GetComponent<TextMeshProUGUI>() ?? rect.gameObject.AddComponent<TextMeshProUGUI>();
        Style(label, fontSize);
        if (key != null)
        {
            var binding = label.GetComponent<GameTextBinding>() ?? label.gameObject.AddComponent<GameTextBinding>();
            binding.key = key; binding.Refresh();
        }
        return label;
    }
    private static void Style(TextMeshProUGUI label, int size, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        label.font = BehemothTypography.Font; label.fontSize = size; label.enableAutoSizing = false;
        label.textWrappingMode = TextWrappingModes.Normal; label.overflowMode = TextOverflowModes.Ellipsis;
        label.alignment = alignment; label.color = new Color(.91f, .94f, .98f); label.raycastTarget = false;
    }
    private static void ButtonLayout(Button button, Vector2 position, Vector2 size, int fontSize = 22)
    {
        Place(button.transform, position, size);
        var image = button.GetComponent<Image>(); if (image != null) { image.color = Card; image.raycastTarget = true; }
        var navigation = button.navigation; navigation.mode = Navigation.Mode.None; button.navigation = navigation;
        var label = button.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (label != null) { Place(label.transform, Vector2.zero, size - new Vector2(16, 6)); Style(label, fontSize, TextAlignmentOptions.Center); }
    }
    private static ScrollRect ScrollText(TextMeshProUGUI text, Transform parent, Vector2 position, Vector2 size, int fontSize)
    {
        var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(text.gameObject);
        if (prefabRoot != null) PrefabUtility.UnpackPrefabInstance(prefabRoot, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        var host = NewRect(text.name + "Scroll", parent, position, size);
        var hit = host.GetComponent<Image>() ?? host.gameObject.AddComponent<Image>();
        hit.color = new Color(1, 1, 1, .015f); hit.raycastTarget = true;
        var scroll = host.GetComponent<ScrollRect>() ?? host.gameObject.AddComponent<ScrollRect>();
        var viewport = NewRect("Viewport", host, new Vector2(-6, 0), size - new Vector2(12, 0));
        if (viewport.GetComponent<RectMask2D>() == null) viewport.gameObject.AddComponent<RectMask2D>();
        text.transform.SetParent(viewport, false); Style(text, fontSize, TextAlignmentOptions.TopLeft);
        text.overflowMode = TextOverflowModes.Overflow;
        var body = text.rectTransform;
        body.anchorMin = new Vector2(0, 1); body.anchorMax = new Vector2(1, 1); body.pivot = new Vector2(.5f, 1);
        body.anchoredPosition = Vector2.zero; body.sizeDelta = Vector2.zero;
        var fitter = text.GetComponent<ContentSizeFitter>() ?? text.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var track = Box("Scrollbar", host, new Vector2(size.x * .5f - 4, 0), new Vector2(6, size.y), new Color(.15f, .2f, .27f), true);
        var bar = track.GetComponent<Scrollbar>() ?? track.gameObject.AddComponent<Scrollbar>();
        var handle = Box("Handle", track.transform, Vector2.zero, new Vector2(6, size.y), new Color(.4f, .55f, .65f), true);
        handle.rectTransform.sizeDelta = Vector2.zero;
        bar.handleRect = handle.rectTransform; bar.targetGraphic = handle; bar.direction = Scrollbar.Direction.BottomToTop;
        scroll.viewport = viewport; scroll.content = body; scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 28;
        scroll.verticalScrollbar = bar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        return scroll;
    }
    private static void CentralPanel(GameObject panel, Transform layer)
    {
        panel.transform.SetParent(layer, false); Place(panel.transform, Vector2.zero, new Vector2(510, 820));
        var image = panel.GetComponent<Image>(); if (image != null) { image.color = Surface; image.raycastTarget = true; }
        Box("TopAccent", panel.transform, new Vector2(0, 407), new Vector2(510, 4), Gold);
    }

    private static void UpgradeMain(Scene scene)
    {
        var canvas = Find<Canvas>(scene);
        var layer = NewRect("CenterModalLayer", canvas.transform, Vector2.zero, new Vector2(510, 820));
        layer.SetAsLastSibling();
        var expansion = Find<RunExpansionView>(scene);
        UpgradeSummary(scene, canvas);
        CentralPanel(expansion.modal, layer);
        var story = expansion.modal.transform.Find("StoryPanel");
        Place(story, Vector2.zero, new Vector2(478, 788));
        var oldAccent = story.Find("Accent"); if (oldAccent != null) Place(oldAccent, new Vector2(0, 391), new Vector2(478, 3));
        Place(expansion.modalTitle.transform, new Vector2(0, 351), new Vector2(440, 58));
        Style(expansion.modalTitle, 29); expansion.modalTitle.color = Gold;
        Place(expansion.modalIllustration.transform, new Vector2(0, 264), new Vector2(96, 96));
        expansion.narrativeScroll = ScrollText(expansion.modalNarrative, story, new Vector2(0, 151), new Vector2(438, 128), 19);
        for (var i = 0; i < expansion.choices.Length; i++)
        {
            var choice = expansion.choices[i];
            ButtonLayout(choice.button, new Vector2(0, 30 - i * 104), new Vector2(440, 96));
            Place(choice.icon.transform, new Vector2(-192, 26), new Vector2(30, 30));
            Place(choice.title.transform, new Vector2(23, 27), new Vector2(358, 30));
            Style(choice.title, 20); choice.title.color = Gold;
            Place(choice.description.transform, new Vector2(0, -16), new Vector2(408, 57));
            Style(choice.description, 18);
        }
        ButtonLayout(expansion.previousPage, new Vector2(-139, -362), new Vector2(112, 36), 18);
        ButtonLayout(expansion.nextPage, new Vector2(139, -362), new Vector2(112, 36), 18);
        Place(expansion.pageLabel.transform, new Vector2(0, -362), new Vector2(146, 36)); Style(expansion.pageLabel, 18, TextAlignmentOptions.Center);
        EditorUtility.SetDirty(expansion);
        UpgradeRoomEvent(Find<RoomEventView>(scene), layer);
        UpgradeReward(Find<RewardPopupView>(scene), layer);
        UpgradeShop(Find<ShopView>(scene), layer);
        UpgradePerks(Find<PerkChoiceView>(scene), layer);
        var result = Find<ResultView>(scene); CentralPanel(Ref<GameObject>(result, "panel"), layer);
        Place(Ref<TextMeshProUGUI>(result, "title").transform, new Vector2(0, 170), new Vector2(450, 65));
        Place(Ref<TextMeshProUGUI>(result, "details").transform, Vector2.zero, new Vector2(440, 210));
        ButtonLayout(Ref<Button>(result, "restart"), new Vector2(0, -220), new Vector2(400, 56));
    }

    private static void UpgradeSummary(Scene scene, Canvas canvas)
    {
        var hud = Find<RunHudView>(scene);
        var left = canvas.transform.Find("LeftPanel");
        var root = Box("CharacterSummary", left, Vector2.zero, new Vector2(282, 208), Surface).rectTransform;
        root.anchorMin = root.anchorMax = new Vector2(.5f, 0); root.anchoredPosition = new Vector2(0, 124);
        Box("Divider", root, new Vector2(0, 103), new Vector2(282, 2), new Color(.25f, .35f, .43f));
        var portrait = Box("Portrait", root, new Vector2(-99, 56), new Vector2(48, 48), Color.white);
        var name = Text("CharacterName", root, new Vector2(29, 56), new Vector2(184, 40), 24);
        var hpTrack = Box("HealthTrack", root, new Vector2(0, 9), new Vector2(248, 23), new Color(.12f, .035f, .05f));
        var hpFill = Box("Fill", hpTrack.transform, Vector2.zero, new Vector2(248, 23), new Color(.83f, .16f, .23f));
        var hpText = Text("Health", hpTrack.transform, Vector2.zero, new Vector2(240, 23), 15); hpText.alignment = TextAlignmentOptions.Center;
        var xpText = Text("Experience", root, new Vector2(0, -25), new Vector2(248, 26), 17);
        var xpTrack = Box("ExperienceTrack", root, new Vector2(0, -47), new Vector2(248, 10), new Color(.08f, .15f, .21f));
        var xpFill = Box("Fill", xpTrack.transform, Vector2.zero, new Vector2(248, 10), new Color(.23f, .7f, .92f));
        var gold = Text("Gold", root, new Vector2(0, -78), new Vector2(248, 33), 21); gold.color = Gold;
        hud.ConfigureCharacterSummary(portrait, name, hpText, hpFill, xpText, xpFill, gold);
        var old = Ref<TextMeshProUGUI>(hud, "runInfo"); if (old != null) { old.text = string.Empty; old.enabled = false; }
        EditorUtility.SetDirty(hud);
    }

    private static void UpgradeReward(RewardPopupView view, Transform layer)
    {
        var panel = Ref<GameObject>(view, "panel"); CentralPanel(panel, layer);
        var heading = panel.transform.Find("Title"); if (heading != null) { Place(heading, new Vector2(0, 222), new Vector2(440, 60)); Style(heading.GetComponent<TextMeshProUGUI>(), 32, TextAlignmentOptions.Center); }
        var xp = Ref<TextMeshProUGUI>(view, "experienceText"); Place(xp.transform, new Vector2(0, 98), new Vector2(440, 42)); Style(xp, 22, TextAlignmentOptions.Center);
        var fill = Ref<Image>(view, "experienceFill"); Place(fill.transform.parent, new Vector2(0, 48), new Vector2(400, 18));
        var gold = Ref<TextMeshProUGUI>(view, "goldText"); Place(gold.transform, new Vector2(0, -43), new Vector2(440, 48)); Style(gold, 25, TextAlignmentOptions.Center); gold.color = Gold;
        ButtonLayout(Ref<Button>(view, "confirmButton"), new Vector2(0, -185), new Vector2(400, 56));
    }
    private static void UpgradeRoomEvent(RoomEventView view, Transform layer)
    {
        var panel = Ref<GameObject>(view, "panel"); CentralPanel(panel, layer);
        var heading = Text("ContextTitle", panel.transform, new Vector2(0, 351), new Vector2(440, 58), 29, "ui.event.heading"); heading.color = Gold;
        Place(Ref<Image>(view, "illustration").transform, new Vector2(0, 179), new Vector2(208, 208));
        var message = Ref<TextMeshProUGUI>(view, "message"); Place(message.transform, new Vector2(0, -28), new Vector2(440, 160)); Style(message, 24, TextAlignmentOptions.Center);
        message.enableAutoSizing = true; message.fontSizeMin = 19; message.fontSizeMax = 24;
        ButtonLayout(Ref<Button>(view, "yesButton"), new Vector2(-115, -250), new Vector2(216, 54));
        ButtonLayout(Ref<Button>(view, "noButton"), new Vector2(115, -250), new Vector2(216, 54));
    }
    private static void UpgradeShop(ShopView view, Transform layer)
    {
        var panel = Ref<GameObject>(view, "panel"); CentralPanel(panel, layer);
        var heading = panel.transform.Find("Title");
        if (heading != null) { Place(heading, new Vector2(-100, 355), new Vector2(240, 52)); Style(heading.GetComponent<TextMeshProUGUI>(), 29); }
        var wallet = Text("Wallet", panel.transform, new Vector2(131, 354), new Vector2(182, 48), 20); wallet.color = Gold; wallet.alignment = TextAlignmentOptions.Right;
        var items = Refs<ShopItemView>(view, "items").ToList();
        while (items.Count < 6) items.Add(NewRect("ShopItem_" + items.Count, panel.transform, Vector2.zero, Vector2.one).gameObject.AddComponent<ShopItemView>());
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var image = item.GetComponent<Image>() ?? item.gameObject.AddComponent<Image>(); image.color = Card; image.raycastTarget = true;
            var button = item.GetComponent<Button>() ?? item.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            ButtonLayout(button, new Vector2(i % 2 == 0 ? -119 : 119, 210 - i / 2 * 201), new Vector2(226, 190));
            var description = Ref<TextMeshProUGUI>(item, "label") ?? Text("Description", item.transform, Vector2.zero, Vector2.one, 17);
            ScrollText(description, item.transform, new Vector2(0, -3), new Vector2(204, 91), 17);
            var icon = Box("ItemIcon", item.transform, new Vector2(-86, 64), new Vector2(30, 30), Color.white);
            var title = Text("ItemTitle", item.transform, new Vector2(19, 64), new Vector2(158, 44), 20);
            title.enableAutoSizing = true; title.fontSizeMin = 17; title.fontSizeMax = 20;
            var status = Text("PurchaseState", item.transform, new Vector2(0, -70), new Vector2(204, 36), 18); status.alignment = TextAlignmentOptions.Center;
            item.Configure(button, description, icon, title, status); EditorUtility.SetDirty(item);
        }
        var leave = Ref<Button>(view, "leaveButton"); ButtonLayout(leave, new Vector2(0, -359), new Vector2(442, 48));
        Text("StockHint", panel.transform, new Vector2(0, -316), new Vector2(442, 28), 16, "ui.shop.stockhint").alignment = TextAlignmentOptions.Center;
        view.Configure(panel, items, leave, wallet); EditorUtility.SetDirty(view);
    }
    private static void UpgradePerks(PerkChoiceView view, Transform layer)
    {
        var panel = Ref<GameObject>(view, "panel"); CentralPanel(panel, layer);
        var title = Ref<TextMeshProUGUI>(view, "title"); Place(title.transform, new Vector2(0, 355), new Vector2(440, 55)); Style(title, 29, TextAlignmentOptions.Center); title.color = Gold;
        var items = Refs<PerkChoiceItem>(view, "items");
        for (var i = 0; i < items.Length; i++)
        {
            ButtonLayout(Ref<Button>(items[i], "button"), new Vector2(0, 180 - i * 177), new Vector2(450, 157));
            var label = Ref<TextMeshProUGUI>(items[i], "label"); Place(label.transform, new Vector2(24, 0), new Vector2(370, 141)); Style(label, 21);
            label.enableAutoSizing = true; label.fontSizeMin = 18; label.fontSizeMax = 21;
            var icon = Ref<Image>(items[i], "icon"); if (icon != null) Place(icon.transform, new Vector2(-195, 44), new Vector2(34, 34));
        }
    }

    private static void UpgradeSelection(Scene scene)
    {
        var canvas = Find<Canvas>(scene); var view = Find<OutGameView>(scene);
        var center = Box("SelectionCenter", canvas.transform, Vector2.zero, new Vector2(510, 820), Surface).rectTransform;
        Box("TopAccent", center, new Vector2(0, 407), new Vector2(510, 4), Gold);
        foreach (var name in new[] { "Title", "Subtitle", "CharacterDetails", "StartRun", "Controls" })
        { var child = canvas.transform.Find(name); if (child != null) child.SetParent(center, false); }
        var title = center.Find("Title"); Place(title, new Vector2(0, 370), new Vector2(474, 61)); Style(title.GetComponent<TextMeshProUGUI>(), 32, TextAlignmentOptions.Center); title.GetComponent<TextMeshProUGUI>().color = Gold;
        var subtitle = center.Find("Subtitle"); Place(subtitle, new Vector2(0, 332), new Vector2(474, 30)); Style(subtitle.GetComponent<TextMeshProUGUI>(), 18, TextAlignmentOptions.Center);
        var binding = subtitle.GetComponent<GameTextBinding>(); binding.key = "ui.selection.subtitle"; binding.Refresh();
        for (var i = 0; i < view.characterButtons.Length; i++)
        {
            var button = view.characterButtons[i]; button.transform.SetParent(center, false);
            ButtonLayout(button, new Vector2(-154 + i % 3 * 154, 272 - i / 3 * 75), new Vector2(144, 65));
            var portrait = button.transform.Find("Portrait"); Place(portrait, new Vector2(-46, 0), new Vector2(30, 30));
            Place(view.characterLabels[i].transform, new Vector2(17, 0), new Vector2(105, 45)); Style(view.characterLabels[i], 20, TextAlignmentOptions.Center);
            Place(view.selectionMarks[i].transform, new Vector2(0, -31), new Vector2(144, 3));
        }
        var detail = center.Find("CharacterDetails"); Place(detail, new Vector2(0, -62), new Vector2(478, 414));
        var detailImage = detail.GetComponent<Image>(); if (detailImage != null) detailImage.color = Card;
        Place(view.portrait.transform, new Vector2(-196, 171), new Vector2(46, 46));
        Place(view.characterName.transform, new Vector2(36, 170), new Vector2(340, 43)); Style(view.characterName, 28);
        Place(view.characterDescription.transform, new Vector2(0, 112), new Vector2(438, 66)); Style(view.characterDescription, 19);
        view.characterDescription.enableAutoSizing = true; view.characterDescription.fontSizeMin = 17; view.characterDescription.fontSizeMax = 19;
        Place(view.stats.transform, new Vector2(0, 0), new Vector2(438, 143)); Style(view.stats, 18);
        view.startingRelicScroll = ScrollText(view.startingRelic, detail, new Vector2(0, -137), new Vector2(438, 127), 19);
        ButtonLayout(view.startButton, new Vector2(0, -316), new Vector2(478, 54), 25);
        var controls = center.Find("Controls"); Place(controls, new Vector2(0, -378), new Vector2(478, 61)); Style(controls.GetComponent<TextMeshProUGUI>(), 16, TextAlignmentOptions.Center);
        var controlsBinding = controls.GetComponent<GameTextBinding>(); controlsBinding.key = "ui.selection.controls"; controlsBinding.Refresh();
        view.Select(0); EditorUtility.SetDirty(view);
    }

    private static void SeedText()
    {
        var table = BehemothTextEditor.Table;
        var csv = ScriptableObject.CreateInstance<GameTextDatabase>(); csv.ImportCsv(File.ReadAllText(BehemothTextEditor.CsvPath));
        var entries = new Dictionary<string, string> {
            ["ui.hud.experience"] = "LV {0}   EXP {1}/{2}", ["ui.hud.gold"] = "소지 골드  {0:N0}",
            ["ui.shop.buy"] = "{0}G · 구매", ["ui.shop.unavailable"] = "{0}G · {1}",
            ["ui.shop.soldout"] = "품절", ["ui.shop.owned"] = "보유 중", ["ui.shop.known"] = "습득한 주문",
            ["ui.shop.potionfull"] = "포션 슬롯 부족", ["ui.shop.notenoughgold"] = "골드 부족", ["ui.shop.unavailablelabel"] = "구매 불가",
            ["ui.shop.stockhint"] = "상품은 구매 후에도 교체·갱신되지 않습니다.",
            ["ui.event.heading"] = "탐험 중 발견", ["ui.selection.subtitle"] = "함께 탐험할 캐릭터를 선택하세요",
            ["ui.selection.stats"] = "체력 {0:0}  ·  가드 {5:0}\n일반 공격 {1:0.#}  ·  공격 간격 {2:0.##}초\n차지 공격 {3:0.#}  ·  준비 {4:0.##}초\n가드 회복 {6:0.#}/초  ·  가드 피해 감소 {7:P0}",
            ["ui.selection.controls"] = "빠르게 긋기: 일반 공격 · 전장 홀드 후 긋기: 차지\n캐릭터 홀드: 가드 · 가드 중 방향으로 긋고 떼기: 주문"
        };
        foreach (var entry in entries)
        {
            if (!table.Contains(entry.Key)) table.Set(entry.Key, csv.Contains(entry.Key) ? csv.Get(entry.Key) : entry.Value, "Assets/Editor/BehemothUiPreparation.cs");
            if (!csv.Contains(entry.Key)) csv.Set(entry.Key, table.Get(entry.Key), "Assets/Editor/BehemothUiPreparation.cs");
        }
        EditorUtility.SetDirty(table);
        File.WriteAllText(BehemothTextEditor.CsvPath, csv.ExportCsv(), new UTF8Encoding(true)); Object.DestroyImmediate(csv);
    }
}
