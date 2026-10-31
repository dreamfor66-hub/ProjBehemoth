using System;
using System.IO;
using System.Linq;
using System.Text;
using DungeonSlash;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// One-time, saved preparation. Later designer edits remain authoritative.
[InitializeOnLoad]
internal static class RoomCorridorMapPreparation
{
    internal const string Folder = "Docs/Production/RoomCorridorMap-2026-10-31/";
    internal const string StylePath = "Assets/Data/Presentation/Data_DungeonMapStyle_방과_통로.asset";
    static bool done, busy;
    static RoomCorridorMapPreparation() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (done || busy || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        busy = true;
        try
        {
            Directory.CreateDirectory(Folder);
            if (!BehemothTextEditor.Table.Contains("ui.map.marker.camp"))
            {
                Backup(BehemothTextEditor.CsvPath); Backup(BehemothTextEditor.TablePath);
                BehemothTextEditor.Table.Set("ui.map.marker.camp", "야영", "RoomCorridorMap");
                EditorUtility.SetDirty(BehemothTextEditor.Table); AssetDatabase.SaveAssetIfDirty(BehemothTextEditor.Table);
                var csv = BehemothLocaleEditing.PatchCsv(File.ReadAllText(BehemothTextEditor.CsvPath), "ui.map.marker.camp", "", "야영", "RoomCorridorMap");
                File.WriteAllText(BehemothTextEditor.CsvPath, csv, new UTF8Encoding(true));
            }
            var style = AssetDatabase.LoadAssetAtPath<DungeonMapStyle>(StylePath);
            if (style == null) { style = ScriptableObject.CreateInstance<DungeonMapStyle>(); AssetDatabase.CreateAsset(style, StylePath); }
            PrepareIcon();
            var complete = true;
            foreach (var path in new[] { "Assets/Scenes/MainGameScene.unity", "Assets/Scenes/Trials/Angular01_Main.unity" })
            {
                var scene = SceneManager.GetSceneByPath(path); var loaded = scene.isLoaded;
                if (loaded && scene.isDirty) { complete = false; continue; }
                try
                {
                    if (!loaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    var hud = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ArcadeHudController>(true)).Single();
                    var map = hud.mapPanel.GetComponentInChildren<DungeonMapView>(true);
                    if (map.layoutRevision >= 1) continue;
                    Backup(path); PreparePanel(hud, map, style);
                    if (EditorApplication.isPlayingOrWillChangePlaymode) { complete = false; continue; }
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Map save failed: " + path);
                }
                finally { if (!loaded && scene.isLoaded && !EditorApplication.isPlayingOrWillChangePlaymode) EditorSceneManager.CloseScene(scene, true); }
            }
            done = complete;
            if (done) File.WriteAllText(Folder + "Installed.txt", "Saved mixed-size map, corridor tiles, navigation hit areas, drag/zoom/recenter.\n");
        }
        catch (InvalidOperationException) when (EditorApplication.isPlayingOrWillChangePlaymode) { done = false; }
        catch (Exception e) { done = true; File.WriteAllText(Folder + "PreparationError.txt", e.ToString()); Debug.LogException(e); }
        finally { busy = false; }
    }
    static void Backup(string path)
    {
        var target = Folder + "Before/" + path;
        Directory.CreateDirectory(Path.GetDirectoryName(target)); if (!File.Exists(target)) File.Copy(path, target);
    }
    static Image ImageChild(Transform parent, string name)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.transform.SetParent(parent, false); var image = root.GetComponent<Image>(); image.raycastTarget = false; return image;
    }
    static void PrepareIcon()
    {
        const string path = "Assets/Prefabs/UI/MapRoomIcon.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var icon = root.GetComponent<MapRoomIcon>(); var so = new SerializedObject(icon);
            if (so.FindProperty("interior").objectReferenceValue != null) return;
            Backup(path);
            var interior = ImageChild(root.transform, "Room Interior"); interior.transform.SetAsFirstSibling();
            interior.rectTransform.anchorMin = Vector2.zero; interior.rectTransform.anchorMax = Vector2.one;
            interior.rectTransform.offsetMin = new Vector2(2, 2); interior.rectTransform.offsetMax = new Vector2(-2, -2);
            interior.color = new Color(.11f, .115f, .11f);
            var hit = ImageChild(root.transform, "Navigation Hit Area"); hit.color = Color.clear;
            hit.rectTransform.sizeDelta = Vector2.one * 44;
            so.FindProperty("interior").objectReferenceValue = interior; so.FindProperty("hitTarget").objectReferenceValue = hit;
            so.ApplyModifiedPropertiesWithoutUndo();
            var label = so.FindProperty("label").objectReferenceValue as TextMeshProUGUI;
            SetType(label, 20); label.raycastTarget = false;
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.anchoredPosition = Vector2.zero; label.rectTransform.sizeDelta = new Vector2(-4, -4);
            root.GetComponent<Image>().color = new Color(.56f, .54f, .46f); root.GetComponent<Image>().raycastTarget = false;
            foreach (var route in new[] { "northRoute", "eastRoute", "southRoute", "westRoute" })
                ((Image)so.FindProperty(route).objectReferenceValue).enabled = false;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    static void SetType(TextMeshProUGUI text, float size)
    {
        text.enableAutoSizing = false; text.fontSize = size;
        if (text.TryGetComponent<TypographyRoleBinding>(out var role))
        { role.designSize = size / Mathf.Max(.01f, role.profile?.Rule(role.role)?.sizeMultiplier ?? 1); role.snapSize = false; role.Apply(); }
    }
    static void TextKey(TextMeshProUGUI text, string key, string value)
    {
        Backup(BehemothTextEditor.CsvPath); Backup(BehemothTextEditor.TablePath);
        var old = BehemothTextEditor.Table.Get(key);
        var csv = BehemothLocaleEditing.PatchCsv(File.ReadAllText(BehemothTextEditor.CsvPath), key, old, value, "RoomCorridorMap");
        BehemothTextEditor.Table.Set(key, value, "RoomCorridorMap"); EditorUtility.SetDirty(BehemothTextEditor.Table);
        AssetDatabase.SaveAssetIfDirty(BehemothTextEditor.Table);
        File.WriteAllText(BehemothTextEditor.CsvPath, csv, new UTF8Encoding(true));
        text.text = value;
        var binding = text.GetComponent<GameTextBinding>() ?? text.gameObject.AddComponent<GameTextBinding>(); binding.key = key;
    }
    static void PreparePanel(ArcadeHudController hud, DungeonMapView map, DungeonMapStyle style)
    {
        var panel = (RectTransform)hud.mapPanel.transform; panel.sizeDelta = new Vector2(660, 620);
        var viewport = (RectTransform)map.transform; viewport.anchoredPosition = new Vector2(0, 10); viewport.sizeDelta = new Vector2(604, 426);
        map.GetComponent<Image>().raycastTarget = true;
        var so = new SerializedObject(map); so.FindProperty("layout").objectReferenceValue = style;
        so.FindProperty("spacing").floatValue = style.cellSpacing; so.ApplyModifiedPropertiesWithoutUndo();
        var legend = hud.mapPanel.GetComponentsInChildren<TextMeshProUGUI>(true).First(t => t.name == "Legend");
        TextKey(legend, "ui.map.roomcorridor.legend", "큰 방: 주요 장소  ·  작은 칸: 통로 사건"); SetType(legend, 18);
        legend.rectTransform.anchorMin = legend.rectTransform.anchorMax = new Vector2(.5f, 0);
        legend.alignment = TextAlignmentOptions.Center;
        legend.rectTransform.sizeDelta = new Vector2(610, 30); legend.rectTransform.anchoredPosition = new Vector2(0, 82);
        var hint = hud.mapPanel.transform.Find("MapTravelHint")?.GetComponent<TextMeshProUGUI>();
        if (hint == null) { hint = Object.Instantiate(legend, panel); hint.name = "MapTravelHint"; }
        TextKey(hint, "ui.map.roomcorridor.controls", "연결된 칸 선택 · 드래그 이동 · 휠 확대/축소\nTab 닫기"); SetType(hint, 17);
        hint.rectTransform.sizeDelta = new Vector2(610, 56); hint.rectTransform.anchoredPosition = new Vector2(0, 37);
        var surface = ImageChild(panel, "MapCenter"); surface.raycastTarget = true; surface.color = new Color(.18f, .18f, .15f);
        surface.rectTransform.anchorMin = surface.rectTransform.anchorMax = Vector2.one;
        surface.rectTransform.anchoredPosition = new Vector2(-132, -36); surface.rectTransform.sizeDelta = new Vector2(106, 42);
        var button = surface.gameObject.AddComponent<Button>(); button.targetGraphic = surface;
        UnityEventTools.AddPersistentListener(button.onClick, map.FocusCurrent);
        var labelRoot = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); labelRoot.transform.SetParent(surface.transform, false);
        var label = labelRoot.GetComponent<TextMeshProUGUI>(); var sample = hud.mapButton.GetComponentInChildren<TextMeshProUGUI>(true);
        label.font = sample.font; label.fontSharedMaterial = sample.fontSharedMaterial; label.fontSize = 20; label.color = new Color(.90f, .84f, .69f);
        label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one; label.rectTransform.sizeDelta = Vector2.zero;
        TextKey(label, "ui.map.roomcorridor.center", "내 위치");
        var table = BehemothTextEditor.Table; table.Set("ui.map.marker.combat", "전", "RoomCorridorMap"); EditorUtility.SetDirty(table); AssetDatabase.SaveAssetIfDirty(table);
        var csv = BehemothLocaleEditing.PatchCsv(File.ReadAllText(BehemothTextEditor.CsvPath), "ui.map.marker.combat", "", "전", "RoomCorridorMap");
        File.WriteAllText(BehemothTextEditor.CsvPath, csv, new UTF8Encoding(true));
        map.layoutRevision = 1;
    }
}
