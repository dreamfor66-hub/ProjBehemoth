using System;
using System.IO;
using System.Linq;
using System.Text;
using DungeonSlash;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

// One-time migration; saved font, profile, scenes and prefabs remain designer editable.
[InitializeOnLoad]
internal static class YiSunShinPreparation
{
    internal const string FontPath = "Assets/Art/Fonts/Typography/YiSunShin.asset";
    const string Report = "Docs/Production/Typography/YiSunShin/";
    static bool finished;
    static YiSunShinPreparation() => EditorApplication.update += Prepare;

    static void Prepare()
    {
        if (finished || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        var profile = AssetDatabase.LoadAssetAtPath<TypographyProfile>(TypographyPreparation.ProfilePath);
        var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/YiSunShin/YiSunShinRegular.ttf");
        if (profile == null || source == null) return;
        // Subsequent domain reloads must preserve any later font choices by the designer.
        if (profile.yiSunShin != null) { finished = true; return; }
        finished = true;
        Directory.CreateDirectory(Report);
        try
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
            {
                font = TMP_FontAsset.CreateFontAsset(source, 64, 8, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
                font.name = "YiSunShin";
                var symbols = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/TextMeshPro/Resources/Fonts & Materials/LiberationSans SDF.asset");
                if (symbols != null) font.fallbackFontAssetTable.Add(symbols);
                AssetDatabase.CreateAsset(font, FontPath);
                AssetDatabase.AddObjectToAsset(font.material, font);
                var characters = File.ReadAllText("Assets/Data/Localization/kr.csv") + "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 ·×%+-/:()[]<>!?.,궤";
                characters = new string(characters.Where(c => !char.IsControl(c)).Distinct().OrderBy(c => c).ToArray());
                font.TryAddCharacters(characters, out string missing);
                foreach (var atlas in font.atlasTextures)
                {
                    atlas.filterMode = FilterMode.Bilinear;
                    if (!AssetDatabase.Contains(atlas)) AssetDatabase.AddObjectToAsset(atlas, font);
                    EditorUtility.SetDirty(atlas);
                }
                File.WriteAllText(Report + "Glyphs.txt", "Prewarmed characters: " + characters.Length + "\nSource-missing characters: " + missing + "\n");
                EditorUtility.SetDirty(font.material);
                EditorUtility.SetDirty(font);
            }
            profile.yiSunShin = font;
            profile.baseFace = Typeface.YiSunShin;
            profile.snapToNativePixels = false;
            foreach (var rule in profile.rules)
            {
                rule.face = Typeface.Base;
                rule.narrowFace = Typeface.Base;
                rule.useNarrowFaceWhenNeeded = false;
                rule.numericOnly = false;
            }
            EditorUtility.SetDirty(profile);
            var settings = new SerializedObject(TMP_Settings.instance);
            settings.FindProperty("m_defaultFontAsset").objectReferenceValue = font;
            settings.ApplyModifiedPropertiesWithoutUndo();
            SyncSavedReferences(font);
        }
        catch (Exception e) { Debug.LogException(e); File.WriteAllText(Report + "Error.txt", e.ToString()); }
    }

    internal static void SyncSavedReferences(TMP_FontAsset font)
    {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                try { if (Apply(root, font, ref count)) PrefabUtility.SaveAsPrefabAsset(root, path); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            var pending = new StringBuilder();
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = SceneManager.GetSceneByPath(path);
                var loaded = scene.isLoaded;
                if (loaded && scene.isDirty) { pending.AppendLine(path); continue; }
                if (!loaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    var changed = false;
                    foreach (var root in scene.GetRootGameObjects()) changed |= Apply(root, font, ref count);
                    if (changed) EditorSceneManager.SaveScene(scene);
                }
                finally { if (!loaded) EditorSceneManager.CloseScene(scene, true); }
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText(Report + "Migration.txt", "YiSunShin Regular / dynamic SDFAA 64px / smooth sampling / native-pixel snapping disabled\nSaved TMP labels: " + count + "\nSkipped dirty scenes (pending):\n" + pending);
    }

    static bool Apply(GameObject root, TMP_FontAsset font, ref int count)
    {
        var changed = false;
        // The old 30px text boxes clipped the new 44px pager labels completely.
        foreach (var view in root.GetComponentsInChildren<RunExpansionView>(true))
            foreach (var button in new[] { view.previousPage, view.nextPage })
            {
                if (button == null) continue;
                var pager = button.GetComponentInChildren<TMP_Text>(true);
                if (pager == null) continue;
                var rect = pager.rectTransform;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(8, 4); rect.offsetMax = new Vector2(-8, -4);
                EditorUtility.SetDirty(rect); changed = true;
            }
        foreach (var number in root.GetComponentsInChildren<DamageNumberView>(true))
            foreach (var text in number.GetComponentsInChildren<TMP_Text>(true))
            {
                text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 240);
                text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 80);
                EditorUtility.SetDirty(text.rectTransform); changed = true;
            }
        foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
        {
            // ExecuteAlways bindings may already have changed the in-memory font on load.
            // Persist that value even when the live component already matches the target.
            label.font = font;
            label.fontSharedMaterial = font.material;
            label.GetComponent<TypographyRoleBinding>()?.Apply();
            EditorUtility.SetDirty(label);
            changed = true; count++;
        }
        return changed;
    }
}
