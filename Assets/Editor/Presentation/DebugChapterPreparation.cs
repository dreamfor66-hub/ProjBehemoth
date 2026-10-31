using System;
using System.IO;
using System.Linq;
using DungeonSlash;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
internal static class DebugChapterPreparation
{
    internal const string Folder = "Docs/Production/DebugChapter-2026-10-31/";
    const string Revision = "chapter-object-tools-1";
    static double next;
    static TypographyProfile typography;
    static DebugChapterPreparation() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + 4;
        if (File.Exists(Folder + "Installed.txt") && File.ReadAllText(Folder + "Installed.txt").StartsWith(Revision)) return;
        if (!File.Exists("Assets/Data/Presentation/DebugCatalogInstalled.txt")) return;
        var paths = new[] { "Assets/Scenes/MainGameScene.unity", "Assets/Scenes/Trials/Angular01_Main.unity" };
        if (paths.Any(p => { var scene = SceneManager.GetSceneByPath(p); return scene.isLoaded && scene.isDirty; })) return;
        try
        {
            Directory.CreateDirectory(Folder); InstallText();
            typography = AssetDatabase.LoadAssetAtPath<TypographyProfile>(TypographyPreparation.ProfilePath);
            var source = SceneManager.GetSceneByPath(paths[0]); var sourceLoaded = source.isLoaded;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!sourceLoaded) source = EditorSceneManager.OpenScene(paths[0], OpenSceneMode.Additive);
            try
            {
                var main = Find<DebugCheatOverlay>(source); if (main.catalogScroll == null) return;
                Install(main, Find<GameFlowController>(source)); Save(source);
                var trial = SceneManager.GetSceneByPath(paths[1]); var trialLoaded = trial.isLoaded;
                if (!trialLoaded) trial = EditorSceneManager.OpenScene(paths[1], OpenSceneMode.Additive);
                try
                {
                    var overlay = Find<DebugCheatOverlay>(trial);
                    if (overlay.catalogScroll == null) CopySavedCatalog(main, overlay);
                    Install(overlay, Find<GameFlowController>(trial)); Save(trial);
                }
                finally { if (!trialLoaded && trial.isLoaded) EditorSceneManager.CloseScene(trial, true); }
            }
            finally { if (!sourceLoaded && source.isLoaded) EditorSceneManager.CloseScene(source, true); }
            AssetDatabase.SaveAssets();
            File.WriteAllText(Folder + "Installed.txt", Revision + "\nSaved chapter tab, 3 travel buttons, all world-object paintings, direction/state/place/clear controls in MainGameScene and Angular01_Main. No manual menu step.\n");
        }
        catch (Exception e) { File.WriteAllText(Folder + "PreparationError.txt", e.ToString()); Debug.LogException(e); }
    }
    static T Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).Single();
    static void Save(Scene scene) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
    static void CopySavedCatalog(DebugCheatOverlay source, DebugCheatOverlay destination)
    {
        var sourcePanel = (GameObject)new SerializedObject(source).FindProperty("panel").objectReferenceValue;
        var old = (GameObject)new SerializedObject(destination).FindProperty("panel").objectReferenceValue;
        var copy = Object.Instantiate(sourcePanel, old.transform.parent, false); copy.name = old.name;
        EditorUtility.CopySerialized(source, destination);
        var serialized = new SerializedObject(destination); var property = serialized.GetIterator();
        while (property.Next(true))
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
            var value = property.objectReferenceValue;
            var transform = value is GameObject go ? go.transform : value is Component c ? c.transform : null;
            if (transform == null || transform != sourcePanel.transform && !transform.IsChildOf(sourcePanel.transform)) continue;
            var path = AnimationUtility.CalculateTransformPath(transform, sourcePanel.transform);
            var target = string.IsNullOrEmpty(path) ? copy.transform : copy.transform.Find(path);
            property.objectReferenceValue = value is GameObject ? target.gameObject : target.GetComponent(value.GetType());
        }
        serialized.ApplyModifiedPropertiesWithoutUndo(); Object.DestroyImmediate(old);
    }
    static void Install(DebugCheatOverlay overlay, GameFlowController flow)
    {
        var panel = (GameObject)new SerializedObject(overlay).FindProperty("panel").objectReferenceValue;
        var root = panel.transform;
        var existing = root.Find("ChapterTools"); if (existing != null) Object.DestroyImmediate(existing.gameObject);
        var buttons = overlay.categoryButtons.Take(4).ToList();
        var tab = root.Find("Category4");
        buttons.Add(tab != null ? tab.GetComponent<Button>() : Button("Category4", root, new(351, 302), new(212, 52), "ui.debugcatalog.tab4", out _));
        overlay.categoryButtons = buttons.ToArray();
        var tools = Node("ChapterTools", root, Vector2.zero, new(1380, 860));
        overlay.chapterTools = tools.gameObject; overlay.chapterFlow = flow;
        overlay.catalogHoverHint = root.Find("HoverHint")?.gameObject;
        Label("ChapterHeading", tools, new(466, 255), new(350, 36), "ui.debugchapter.heading", 24);
        overlay.chapterButtons = new Button[3];
        for (var i = 0; i < 3; i++) overlay.chapterButtons[i] = Button("JumpToAct" + (i + 1), tools, new(466, 201 - i * 56), new(350, 46), "ui.debugchapter.act" + (i + 1), out _);
        overlay.objectStateButton = Button("ObjectState", tools, new(466, 20), new(350, 44), null, out var state);
        overlay.objectStateLabel = state; state.text = GameText.Get("ui.debugchapter.available");
        overlay.objectDirectionButtons = new Button[4];
        for (var i = 0; i < 4; i++) overlay.objectDirectionButtons[i] = Button("ObjectDirection" + i, tools, new(377 + i % 2 * 178, -37 - i / 2 * 52), new(170, 44), "ui.debugchapter.view" + i, out _);
        overlay.clearObjectButton = Button("ClearInspection", tools, new(466, -151), new(350, 44), "ui.debugchapter.clear", out _);
        Label("InspectionHelp", tools, new(466, -239), new(350, 108), "ui.debugchapter.help", 20);
        overlay.placeObjectButton = Button("PlaceObject", tools, new(-486, -342), new(340, 56), "ui.debugchapter.place", out _);
        overlay.roomObjectCatalog = AssetDatabase.FindAssets("t:RoomObjectData", new[] { "Assets/Data/Presentation/Exploration" })
            .Select(g => AssetDatabase.LoadAssetAtPath<RoomObjectData>(AssetDatabase.GUIDToAssetPath(g))).Where(a => a != null).OrderBy(a => a.name, StringComparer.Ordinal).ToList();
        tools.gameObject.SetActive(false); panel.SetActive(false); EditorUtility.SetDirty(overlay);
    }
    static RectTransform Node(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
    }
    static TextMeshProUGUI Label(string name, Transform parent, Vector2 position, Vector2 size, string key, float fontSize)
    {
        var text = Node(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = typography.Font(Typeface.Base); text.fontSize = fontSize; text.color = new Color32(237, 230, 209, 255);
        text.alignment = TextAlignmentOptions.Center; text.textWrappingMode = TextWrappingModes.Normal; text.raycastTarget = false;
        if (!string.IsNullOrEmpty(key)) { text.text = GameText.Get(key); text.gameObject.AddComponent<GameTextBinding>().key = key; }
        return text;
    }
    static Button Button(string name, Transform parent, Vector2 position, Vector2 size, string key, out TextMeshProUGUI label)
    {
        var image = Node(name, parent, position, size).gameObject.AddComponent<Image>(); image.color = new Color32(39, 41, 47, 255);
        var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var nav = button.navigation; nav.mode = Navigation.Mode.None; button.navigation = nav;
        label = Label("Label", image.transform, Vector2.zero, size - new Vector2(16, 4), key, 25); return button;
    }
    static void InstallText()
    {
        var rows = new[] {
            ("ui.debugcatalog.tab4", "챕터"), ("ui.debugchapter.heading", "막 시작으로 이동"),
            ("ui.debugchapter.act1", "1막 시작으로"), ("ui.debugchapter.act2", "2막 시작으로"), ("ui.debugchapter.act3", "3막 시작으로"),
            ("ui.debugchapter.count", "배치 가능한 오브젝트 · {0}개"),
            ("ui.debugchapter.hint", "오브젝트 선택 → 방향·상태 선택 → 확인방에 배치 · 막 이동 시 지도 초기화"),
            ("ui.debugchapter.help", "장비·성장 유지 / 지도 초기화\n배치하면 별도 확인방으로 이동\n오브젝트를 눌러 반응 확인"),
            ("ui.debugchapter.objecthelp", "현재 막의 배경에서 확인합니다.\n방향과 사용 전후 상태를 고른 뒤\n‘확인방에 배치’를 누르세요.\n배치한 오브젝트는 직접 클릭할 수 있습니다."),
            ("ui.debugchapter.available", "상태: 미사용"), ("ui.debugchapter.used", "상태: 사용 후"),
            ("ui.debugchapter.view0", "정면"), ("ui.debugchapter.view1", "측면"), ("ui.debugchapter.view2", "뒷면"), ("ui.debugchapter.view3", "반대 측면"),
            ("ui.debugchapter.wall0", "정면 벽"), ("ui.debugchapter.wall1", "왼쪽 벽"), ("ui.debugchapter.wall2", "뒤쪽 벽 불가"), ("ui.debugchapter.wall3", "오른쪽 벽"),
            ("ui.debugchapter.place", "확인방에 배치"), ("ui.debugchapter.clear", "확인방 비우기"),
            ("ui.debugchapter.unavailable", "현재 장면에서는 배치할 수 없습니다.")
        };
        var csv = ScriptableObject.CreateInstance<GameTextDatabase>(); csv.ImportCsv(File.ReadAllText(BehemothTextEditor.CsvPath));
        foreach (var row in rows) { csv.Set(row.Item1, row.Item2, "F1 chapter tools"); BehemothTextEditor.Table.Set(row.Item1, row.Item2, "F1 chapter tools"); }
        File.WriteAllText(BehemothTextEditor.CsvPath, csv.ExportCsv(), new System.Text.UTF8Encoding(true)); Object.DestroyImmediate(csv);
        EditorUtility.SetDirty(BehemothTextEditor.Table); AssetDatabase.ImportAsset(BehemothTextEditor.CsvPath);
    }
}
