using System.IO;
using System.Linq;
using DungeonSlash;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

[InitializeOnLoad]
internal static class BehemothTypography
{
    internal const string FontPath = "Assets/Art/Fonts/Resources/BehemothKorean.asset";
    static BehemothTypography() { EditorApplication.delayCall += Prepare; EditorApplication.delayCall += AutoFinalize; }
    private static void AutoFinalize()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.delayCall += AutoFinalize; return; }
        var content = AssetDatabase.LoadAssetAtPath<GameContent>(BehemothContentBuilder.ContentPath);
        if (Font == null || content == null || content.schemaVersion < 6) { EditorApplication.delayCall += AutoFinalize; return; }
        if (content.schemaVersion >= 7 || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty) return;
        FinalizeScenes(); content.schemaVersion = 7; EditorUtility.SetDirty(content); AssetDatabase.SaveAssets();
    }
    internal static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
    private static void Prepare()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.delayCall += Prepare; return; }
        if (Font != null) { ConfigureSettings(); return; }
        var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/NotoSansCJKkr-Regular.otf"); if (source == null) return;
        if (Shader.Find("TextMeshPro/Distance Field") == null)
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly);
            AssetDatabase.importPackageCompleted += Imported;
            AssetDatabase.ImportPackage(package.resolvedPath + "/Package Resources/TMP Essential Resources.unitypackage", false);
            return;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(FontPath)); AssetDatabase.Refresh();
        var font = TMP_FontAsset.CreateFontAsset(source, 42, 6, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
        font.name = "BehemothKorean"; AssetDatabase.CreateAsset(font, FontPath);
        var table = BehemothTextEditor.Table;
        var chars = new string((string.Concat(table.entries.Select(item => item.kr)) + "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 .,:;!?%+-×→←↑↓[]()/\n").Distinct().ToArray());
        font.TryAddCharacters(chars, out string missing);
        AssetDatabase.AddObjectToAsset(font.material, font);
        foreach (var texture in font.atlasTextures) if (texture != null && !AssetDatabase.Contains(texture)) AssetDatabase.AddObjectToAsset(texture, font);
        EditorUtility.SetDirty(font); AssetDatabase.SaveAssets();
        ConfigureSettings();
        File.WriteAllText("Logs/Behemoth-FontReady.txt", AssetDatabase.AssetPathToGUID(FontPath) + "\nMissing: " + missing);
    }
    private static void ConfigureSettings()
    {
        var settings = Resources.Load<TMP_Settings>("TMP Settings");
        if (settings != null) { var serialized = new SerializedObject(settings); serialized.FindProperty("m_defaultFontAsset").objectReferenceValue = Font; serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets(); }
    }
    internal static void FinalizeScenes()
    {
        foreach (var path in new[] { "Assets/Scenes/MainGameScene.unity", "Assets/Scenes/OutGameScene.unity" })
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
            foreach (var text in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            { text.font = Font; text.overflowMode = TextOverflowModes.Overflow; EditorUtility.SetDirty(text); }
            var selection = Object.FindFirstObjectByType<OutGameView>(); if (selection != null) selection.Select(0);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid); var root = PrefabUtility.LoadPrefabContents(path);
            var texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in texts) { text.font = Font; text.overflowMode = TextOverflowModes.Overflow; }
            if (texts.Length > 0) PrefabUtility.SaveAsPrefabAsset(root, path); PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();
    }
    private static void Imported(string name)
    {
        AssetDatabase.importPackageCompleted -= Imported;
        if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro") && !AssetDatabase.IsValidFolder("Assets/Art/TextMeshPro")) AssetDatabase.MoveAsset("Assets/TextMesh Pro", "Assets/Art/TextMeshPro");
        EditorApplication.delayCall += Prepare;
    }
}
