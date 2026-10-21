using TMPro;
using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal static partial class BehemothContentBuilder
{
    private static void SetObject(Object target, string field, Object value)
    { var serialized = new SerializedObject(target); serialized.FindProperty(field).objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target); }
    private static T GetObject<T>(Object target, string field) where T : Object => new SerializedObject(target).FindProperty(field).objectReferenceValue as T;
    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
        rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
    }
    private static Image Box(string name, Transform parent, Vector2 pos, Vector2 size, Color color, bool raycast = false)
    { var image = Rect(name, parent, pos, size).gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = raycast; return image; }
    private static TextMeshProUGUI Label(string name, Transform parent, Vector2 pos, Vector2 size, string text, int point = 22, TextAlignmentOptions align = TextAlignmentOptions.Left, Color? color = null)
    {
        var label = Rect(name, parent, pos, size).gameObject.AddComponent<TextMeshProUGUI>(); label.font = font; label.fontSize = point;
        label.text = text; label.color = color ?? new Color(.91f, .94f, .98f); label.alignment = align;
        label.textWrappingMode = TextWrappingModes.Normal; label.overflowMode = TextOverflowModes.Truncate; label.raycastTarget = false; return label;
    }
    private static Button Button(string name, Transform parent, Vector2 pos, Vector2 size, string text, int point = 22)
    {
        var box = Box(name, parent, pos, size, new Color(.13f, .22f, .29f), true); var button = box.gameObject.AddComponent<Button>(); button.targetGraphic = box;
        var colors = button.colors; colors.highlightedColor = new Color(.77f, .9f, 1); colors.pressedColor = new Color(.5f, .75f, .9f); colors.disabledColor = new Color(.38f, .4f, .43f); button.colors = colors;
        if (text != null) Label("Label", box.transform, Vector2.zero, size - new Vector2(20, 8), text, point, TextAlignmentOptions.Center);
        return button;
    }
    private static void BuildScenes()
    {
        const string mainPath = "Assets/Scenes/MainGameScene.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(mainPath) == null)
        {
            var error = AssetDatabase.MoveAsset("Assets/Scenes/GamePrototype.unity", mainPath);
            if (!string.IsNullOrEmpty(error)) throw new System.InvalidOperationException(error);
        }
        var main = EditorSceneManager.OpenScene(mainPath, OpenSceneMode.Single);
        var canvas = Object.FindFirstObjectByType<Canvas>();
        var old = canvas.transform.Find("RunExpansion"); if (old != null) Object.DestroyImmediate(old.gameObject);
        var root = Rect("RunExpansion", canvas.transform, Vector2.zero, new Vector2(1600, 900));
        var expansion = root.gameObject.AddComponent<RunExpansionView>();
        expansion.stageTitle = Label("StageTitle", root, new Vector2(0, 423), new Vector2(530, 32), "B1 · 탐험", 23, TextAlignmentOptions.Center, Gold);
        expansion.encounterHint = Label("EncounterHint", root, new Vector2(0, 341), new Vector2(500, 89), "", 18, TextAlignmentOptions.Center, new Color(.76f, .86f, .95f));
        expansion.characterStatus = Label("CharacterState", root, new Vector2(0, -187), new Vector2(510, 64), "직업 상태", 20, TextAlignmentOptions.Center, Gold);
        expansion.characterStatus.gameObject.AddComponent<Shadow>().effectColor = Ink;
        expansion.companionLabel = Label("Companion", root, new Vector2(0, -222), new Vector2(480, 28), "", 17, TextAlignmentOptions.Center, Gold);
        expansion.returnButton = Button("ReturnToSelection", root, new Vector2(655, -380), new Vector2(220, 38), "탐험 종료", 19);
        for (var i = 0; i < 4; i++)
        {
            var button = Button("Spell_" + (SwipeDirection)i, root, new Vector2(-187.5f + i * 125f, -419), new Vector2(119, 59), null);
            expansion.spellButtons[i] = button;
            var fill = Box("CastingProgress", button.transform, new Vector2(0, -25), new Vector2(111, 5), Color.cyan);
            fill.enabled = false; expansion.spellFills[i] = fill;
            expansion.spellLabels[i] = Label("SpellLabel", button.transform, new Vector2(0, 3), new Vector2(113, 48), "빈 슬롯", 17, TextAlignmentOptions.Center);
        }
        BuildModal(expansion, root);
        var run = Object.FindFirstObjectByType<RunController>();
        run.Configure(content.characters[0], GetObject<RunBalanceSettings>(run, "balanceSettings"), content.perks, content.equipment);
        var combat = Object.FindFirstObjectByType<CombatController>(); var flow = Object.FindFirstObjectByType<GameFlowController>();
        SetObject(combat, "content", content); SetObject(combat, "expansionView", expansion); SetObject(combat, "playerData", content.characters[0]);
        SetObject(flow, "content", content); SetObject(flow, "expansionView", expansion);
        var result = Object.FindFirstObjectByType<ResultView>();
        if (result != null) GetObject<Button>(result, "restart").GetComponentInChildren<TextMeshProUGUI>(true).text = "캐릭터 선택";
        EditorUtility.SetDirty(run); EditorUtility.SetDirty(expansion);
        EditorSceneManager.SaveScene(main);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera", typeof(Camera)); camera.tag = "MainCamera"; camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor; camera.GetComponent<Camera>().backgroundColor = Ink;
        var cg = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var outCanvas = cg.GetComponent<Canvas>(); outCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = cg.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        var view = cg.AddComponent<OutGameView>(); view.content = content;
        Box("Background", cg.transform, Vector2.zero, new Vector2(2000, 1300), Ink);
        Label("Title", cg.transform, new Vector2(0, 382), new Vector2(1300, 68), "BEHEMOTH", 49, TextAlignmentOptions.Center, Gold);
        Label("Subtitle", cg.transform, new Vector2(0, 328), new Vector2(1300, 36), "여섯 가지 전투 방식 · 하나의 탐험", 23, TextAlignmentOptions.Center);
        view.characterButtons = new Button[6]; view.characterLabels = new TextMeshProUGUI[6]; view.selectionMarks = new Image[6];
        for (var i = 0; i < 6; i++)
        {
            var data = content.characters[i];
            var button = Button(data.character.ToString(), cg.transform, new Vector2(-530 + i * 212, 220), new Vector2(192, 130), null);
            view.characterButtons[i] = button;
            view.selectionMarks[i] = Box("Selection", button.transform, new Vector2(0, -62), new Vector2(192, 6), data.characterColor);
            var portrait = Box("Portrait", button.transform, new Vector2(0, 22), new Vector2(48, 48), Color.white); portrait.sprite = data.portrait; portrait.preserveAspect = true;
            view.characterLabels[i] = Label("Name", button.transform, new Vector2(0, -31), new Vector2(184, 38), data.displayName, 23, TextAlignmentOptions.Center);
        }
        var detail = Box("CharacterDetails", cg.transform, new Vector2(0, -60), new Vector2(1260, 380), Panel).transform;
        view.characterName = Label("Name", detail, new Vector2(-355, 136), new Vector2(450, 50), "워리어", 35);
        view.portrait = Box("Portrait", detail, new Vector2(-537, 65), new Vector2(75, 75), Color.white);
        view.stats = Label("CombatStats", detail, new Vector2(-375, -52), new Vector2(420, 240), "", 22);
        view.characterDescription = Label("Description", detail, new Vector2(220, 106), new Vector2(665, 105), "", 25);
        view.startingRelic = Label("StartingRelic", detail, new Vector2(220, -66), new Vector2(665, 220), "", 23);
        view.startButton = Button("StartRun", cg.transform, new Vector2(0, -316), new Vector2(330, 69), "탐험 시작", 29);
        view.Select(0);
        Label("Controls", cg.transform, new Vector2(0, -389), new Vector2(1380, 64), "빠르게 긋기  일반공격    ·    전장 홀드 → 긋기  차지\n캐릭터 홀드  가드·캐스팅    ·    가드 중 방향으로 긋고 떼기  주문", 21, TextAlignmentOptions.Center, new Color(.7f, .78f, .87f));
        EditorUtility.SetDirty(view);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/OutGameScene.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/OutGameScene.unity", true), new EditorBuildSettingsScene(mainPath, true) };
    }
    private static void BuildModal(RunExpansionView view, Transform parent)
    {
        var backdrop = Box("EventAndChoiceOverlay", parent, Vector2.zero, new Vector2(2400, 1600), new Color(.01f, .02f, .035f, .97f), true);
        view.modal = backdrop.gameObject;
        var panel = Box("StoryPanel", backdrop.transform, Vector2.zero, new Vector2(1440, 790), Panel).transform;
        Box("Accent", panel, new Vector2(0, 391), new Vector2(1440, 5), Gold);
        view.modalTitle = Label("Title", panel, new Vector2(-405, 321), new Vector2(505, 77), "사건", 34, TextAlignmentOptions.Left, Gold);
        view.modalNarrative = Label("Narrative", panel, new Vector2(-405, 105), new Vector2(505, 340), "사건 설명", 23);
        view.modalIllustration = Box("Illustration", panel, new Vector2(-410, -219), new Vector2(235, 235), Color.white); view.modalIllustration.preserveAspect = true;
        view.choices = new ExpansionChoiceButton[4];
        for (var i = 0; i < 4; i++)
        {
            var button = Button("Choice_" + i, panel, new Vector2(280, 244 - i * 139), new Vector2(780, 127), null);
            var icon = Box("Icon", button.transform, new Vector2(-349, 19), new Vector2(48, 48), Color.white); icon.preserveAspect = true;
            var title = Label("ChoiceTitle", button.transform, new Vector2(31, 37), new Vector2(670, 34), "선택지", 23, color: Gold);
            var description = Label("ChoiceDescription", button.transform, new Vector2(31, -18), new Vector2(670, 75), "대가와 결과", 19);
            view.choices[i] = new ExpansionChoiceButton { button = button, title = title, description = description, icon = icon };
        }
        view.previousPage = Button("PreviousPage", panel, new Vector2(100, -329), new Vector2(160, 43), "이전", 20);
        view.nextPage = Button("NextPage", panel, new Vector2(460, -329), new Vector2(160, 43), "다음", 20);
        view.pageLabel = Label("Page", panel, new Vector2(280, -329), new Vector2(180, 43), "", 20, TextAlignmentOptions.Center);
        view.modal.SetActive(false);
    }
}
