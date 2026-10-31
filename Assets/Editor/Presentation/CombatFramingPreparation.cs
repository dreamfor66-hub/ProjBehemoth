using System;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Scoped, saved presentation correction; no Play-mode assembly or manual baking.
[InitializeOnLoad]
internal static class CombatFramingPreparation
{
    const string Stamp = "Assets/Data/Presentation/Angular/CombatFramingInstalled.txt";
    const string Revision = "depth-and-face-crops-1";
    const string Crops = "Assets/Art/UI/Portraits/Angular/";
    const string Report = "Docs/Production/CombatFraming-2026-10-26/";
    static double next;
    static bool busy;
    static CombatFramingPreparation() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (busy || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + 4;
        if (File.Exists(Stamp) && File.ReadAllText(Stamp) == Revision) return;
        busy = true;
        try { Prepare(); File.WriteAllText(Stamp, Revision); AssetDatabase.ImportAsset(Stamp); }
        catch (Exception e) { Directory.CreateDirectory(Report); File.WriteAllText(Report + "Preparation-Error.txt", e.ToString()); next = EditorApplication.timeSinceStartup + 60; }
        finally { busy = false; }
    }
    static void Prepare()
    {
        const string path = "Assets/Scenes/MainGameScene.unity";
        var scene = SceneManager.GetSceneByPath(path);
        var loaded = scene.isLoaded;
        if (loaded && scene.isDirty) throw new InvalidOperationException("Unsaved designer scene: " + path);
        Directory.CreateDirectory(Report); Directory.CreateDirectory(Crops);
        if (!File.Exists(Report + "Before-MainGameScene.unity.txt")) File.Copy(path, Report + "Before-MainGameScene.unity.txt");
        if (!File.Exists(Report + "Before-HeroFaces.asset.txt")) File.Copy("Assets/Data/Presentation/Angular/HeroFaces.asset", Report + "Before-HeroFaces.asset.txt");
        // Top-left pixel coordinates in the existing 640px portraits. Original artwork stays intact.
        var names = new[] { "Warrior", "Rogue", "Barbarian", "Samurai", "Monk", "Wizard" };
        var boxes = new[] { new RectInt(155, 0, 420, 420), new RectInt(220, 10, 360, 360), new RectInt(195, 20, 375, 375), new RectInt(55, 20, 370, 370), new RectInt(95, 10, 370, 370), new RectInt(50, 0, 370, 370) };
        var faces = AssetDatabase.LoadAssetAtPath<FatigueFaces>("Assets/Data/Presentation/Angular/HeroFaces.asset");
        for (var i = 0; i < names.Length; i++)
        {
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            source.LoadImage(File.ReadAllBytes("Assets/Art/World/Angular/Heroes/" + names[i] + "-Portrait.png"));
            var b = boxes[i]; var crop = new Texture2D(b.width, b.height, TextureFormat.RGBA32, false);
            crop.SetPixels(source.GetPixels(b.x, source.height - b.yMax, b.width, b.height)); crop.Apply();
            var output = Crops + names[i] + ".png"; File.WriteAllBytes(output, crop.EncodeToPNG());
            Object.DestroyImmediate(source); Object.DestroyImmediate(crop);
            AssetDatabase.ImportAsset(output, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(output);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.npotScale = TextureImporterNPOTScale.None;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings); importer.SaveAndReimport();
            var kind = (CharacterKind)Enum.Parse(typeof(CharacterKind), names[i]);
            faces.heroes.Single(h => h.character == kind).expressions = Enumerable.Repeat(DungeonSlash.DesignerAssetPaths.Load<Sprite>(output), 4).ToArray();
        }
        EditorUtility.SetDirty(faces);
        // Imports can take several seconds; defer if Play was requested meanwhile.
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Waiting for Edit mode after portrait imports");
        if (!loaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            var roots = scene.GetRootGameObjects();
            var layout = roots.SelectMany(r => r.GetComponentsInChildren<CombatSceneView>(true)).Single();
            var serialized = new SerializedObject(layout);
            var monsters = (RectTransform)serialized.FindProperty("monsterAnchor").objectReferenceValue;
            var player = (RectTransform)serialized.FindProperty("playerAnchor").objectReferenceValue;
            // Move the anchor, so body, aim bounds, damage numbers and weak points agree.
            monsters.anchoredPosition = new Vector2(0f, -150f);
            if (monsters.parent != player.parent) throw new InvalidOperationException("Combat depth roots must share a parent");
            if (monsters.GetSiblingIndex() > player.GetSiblingIndex()) monsters.SetSiblingIndex(player.GetSiblingIndex());
            var hud = roots.SelectMany(r => r.GetComponentsInChildren<FatigueHudView>(true)).Single();
            var frame = (RectTransform)hud.transform;
            frame.anchorMin = Vector2.zero; frame.anchorMax = Vector2.one; frame.offsetMin = frame.offsetMax = Vector2.zero;
            hud.face.rectTransform.anchorMin = hud.face.rectTransform.anchorMax = new Vector2(0, 1);
            hud.face.rectTransform.pivot = new Vector2(0, 1); hud.face.rectTransform.anchoredPosition = Vector2.zero;
            hud.face.sprite = faces.Face(CharacterKind.Warrior, 0); hud.face.preserveAspect = true;
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        finally { if (!loaded) EditorSceneManager.CloseScene(scene, true); }
        AssetDatabase.SaveAssets();
        File.WriteAllText(Report + "Installed.txt", "Monster anchor (0,0), per latest combat reference; monsters draw before player. FatigueHUD stretches to canvas edges; six face-only crops preserve original portrait art. No balance, hero scale, background or selection changes.\n");
    }
}
