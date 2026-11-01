using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DungeonSlash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Explicit production installation; reopening the project never overwrites authored changes.
[InitializeOnLoad]
internal static class WorldRenewalPreparation
{
    internal const string Folder = "Docs/Production/WorldRenewal-2026-11-01/";
    const string Request = "Temp/WorldRenewal.request";
    const string Art = "Assets/Art/World/Angular/RoomThemes/T_UI_Room_";
    const string Data = "Assets/Data/Presentation/RoomThemes/";
    static double next; static bool busy;
    [Serializable] sealed class Manifest { public Entry[] files; }
    [Serializable] sealed class Entry { public string key, path; }
    static WorldRenewalPreparation() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (busy || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup < next || !File.Exists(Request)) return;
        next = EditorApplication.timeSinceStartup + 2;
        var command = File.ReadAllText(Request).Trim();
        if (command != "install") { File.Delete(Request); return; }
        busy=true;
        try { if(Install()) File.Delete(Request); }
        catch (Exception e) { File.Delete(Request); File.WriteAllText(Folder + "InstallError.txt", e.ToString()); Debug.LogException(e); }
        finally { busy=false; }
    }
    static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
    static void Backup(string path)
    {
        var target = Folder + "Before/" + path;
        if (!File.Exists(path) || File.Exists(target)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(target)); File.Copy(path, target);
    }
    static void Import(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.spritePixelsPerUnit = 100;
        importer.maxTextureSize = 4096; importer.npotScale = TextureImporterNPOTScale.None;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings); importer.SaveAndReimport();
    }
    static RoomThemeData Theme(string name, string[] paintings, bool commonLight = false, params RoomThemeData.WallMark[] marks)
    {
        var path = Data + "Data_RoomTheme_" + name + ".asset"; Backup(path);
        var data = Load<RoomThemeData>(path);
        if (data == null) { data = ScriptableObject.CreateInstance<RoomThemeData>(); AssetDatabase.CreateAsset(data, path); }
        data.acts = paintings.Select(p => new RoomThemeData.ActPainting { walls = Load<Sprite>(p) }).ToArray();
        if (data.acts.Any(p => p.walls == null)) throw new InvalidDataException("Missing room painting: " + name);
        data.useFloorLighting = commonLight; data.wallMarks = marks; EditorUtility.SetDirty(data); return data;
    }
    static string[] Three(string name) => Enumerable.Range(1, 3).Select(i => Art + "B" + i + "_" + name + ".png").ToArray();
    public static bool Install()
    {
        var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Folder + "ArtReady.json"));
        foreach (var file in manifest.files) Import(file.path);
        Directory.CreateDirectory(Data); AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        // Imports can execute other editor callbacks. Defer scene work if a play
        // transition was requested while those callbacks were running.
        if (EditorApplication.isPlayingOrWillChangePlaymode) return false;
        var cloth = Load<Sprite>(Art + "장식_경비천.png"); var relief = Load<Sprite>(Art + "장식_돌문장.png");
        var watch = Theme("옛경비실", Three("옛경비실"), false,
            new RoomThemeData.WallMark { wall = FacingDirection.North, painting = cloth },
            new RoomThemeData.WallMark { wall = FacingDirection.East, painting = relief });
        // Each painting already carries its act's scene lighting. Multiplying
        // the object-light tint again made the B1 room yellow beside its doors.
        var oath = Theme("잊힌서약실", new[] { Art + "공용_서약실.png", Art + "B2_서약실.png", Art + "B3_서약실.png" }, false,
            new RoomThemeData.WallMark { wall = FacingDirection.West, painting = relief });
        var payroll = Theme("폐성급료고", Three("폐성급료고"));
        var root = Theme("혈근번식실", Three("혈근번식실"));
        var supplies = Theme("물자보관칸", Three("물자보관칸"));
        var library = Theme("봉인기록고", Enumerable.Range(1, 3).Select(i => "Assets/Art/World/Exploration/T_UI_Room_B" + i + "기록더미내부.png").ToArray());
        var training = Theme("훈련보관실", Enumerable.Range(1, 3).Select(i => "Assets/Art/World/Exploration/T_UI_Room_B" + i + "운반틀내부.png").ToArray());
        foreach (var guid in AssetDatabase.FindAssets("t:FloorArtwork"))
        { var floor = Load<FloorArtwork>(AssetDatabase.GUIDToAssetPath(guid)); Backup(AssetDatabase.GetAssetPath(floor)); floor.roomVariations = new[] { watch, oath }; EditorUtility.SetDirty(floor); }
        foreach (var guid in AssetDatabase.FindAssets("t:RandomEventData"))
        {
            var story = Load<RandomEventData>(AssetDatabase.GUIDToAssetPath(guid));
            var theme = story.id switch { "Well" => payroll, "AmberSeed" => root, "Scholar" => library, "Delivery" => supplies, "Training" => training, _ => null };
            if (theme == null) continue; Backup(AssetDatabase.GetAssetPath(story)); story.roomTheme = theme; EditorUtility.SetDirty(story);
        }
        foreach (var guid in AssetDatabase.FindAssets("t:RoomObjectData"))
        {
            var obj = Load<RoomObjectData>(AssetDatabase.GUIDToAssetPath(guid));
            obj.SetDerivedGeometry(new[] { obj.front, obj.side, obj.rear, obj.usedFront, obj.usedSide, obj.usedRear }.Where(s => s != null).Distinct().Select(ExplorationArtPreparation.Geometry).ToArray());
            EditorUtility.SetDirty(obj);
        }
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode) return false;
        if (scene.path != "Assets/Scenes/MainGameScene.unity") throw new InvalidOperationException("Install in the existing MainGameScene.");
        var backdrop = Object.FindFirstObjectByType<StageBackdropView>();
        if (backdrop == null || backdrop.background == null) throw new InvalidDataException("Missing saved backdrop.");
        Backup(scene.path);
        var layer = backdrop.background.GetComponentInChildren<RoomWallMarkLayer>(true);
        if (layer == null)
        {
            var go = new GameObject("Fixed room architecture marks", typeof(RectTransform), typeof(RoomWallMarkLayer));
            go.transform.SetParent(backdrop.background.transform, false); layer = go.GetComponent<RoomWallMarkLayer>();
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        layer.Prepare(); EditorUtility.SetDirty(layer);
        var serialized = new SerializedObject(backdrop); serialized.FindProperty("wallMarks").objectReferenceValue = layer; serialized.ApplyModifiedPropertiesWithoutUndo();
        var overlay=Object.FindFirstObjectByType<DebugCheatOverlay>();
        var flow=Object.FindFirstObjectByType<GameFlowController>();
        var content=(GameContent)new SerializedObject(flow).FindProperty("content").objectReferenceValue;
        var exploration=AssetDatabase.FindAssets("t:ExplorationSettings").Select(g=>Load<ExplorationSettings>(AssetDatabase.GUIDToAssetPath(g))).First();
        var active=content.events.Where(e=>e!=null&&!e.excludedFromRandomPool);
        var usable=exploration.roomObjects.Select(b=>b.artwork).Concat(active.Select(e=>e.worldObject))
            .Concat(active.SelectMany(e=>e.options.Concat(e.pages.SelectMany(p=>p.options))).Select(o=>o.resultingWorldObject))
            .Concat(new[]{exploration.key,exploration.hiddenWall,exploration.lockedDoor,exploration.mechanism,exploration.eliteEntrance,exploration.bossEntrance})
            .Concat(exploration.concealedEntranceClues).Where(a=>a!=null).SelectMany(a=>new[]{a,a.usedForm}).Where(a=>a!=null).Distinct().OrderBy(a=>a.name).ToList();
        if(overlay!=null){overlay.roomObjectCatalog=usable;EditorUtility.SetDirty(overlay);}
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        InstallOutgameBackground();
        UpdateArtRevision(); AssetDatabase.SaveAssets();
        File.WriteAllText(Folder + "Installed.txt", "Saved 7 room themes, 3-act paintings, fixed directional wall marks, scene binding, image-derived object geometry and current art revision.\n" + DateTime.UtcNow.ToString("O"));
        return true;
    }
    static void InstallOutgameBackground()
    {
        const string path = "Assets/Scenes/OutGameScene.unity";
        var backdrop = Load<Sprite>("Assets/Art/UI/Outgame/T_UI_Title_Backdrop.png");
        if (backdrop == null) throw new InvalidDataException("Missing illustrated title background.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path); var alreadyLoaded = scene.isLoaded;
        if (alreadyLoaded && scene.isDirty) throw new InvalidOperationException("Unsaved OutGameScene edits; preserve before background replacement.");
        Backup(path);
        if (!alreadyLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            foreach (var picture in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UnityEngine.UI.Image>(true)))
                if (picture.sprite != null && AssetDatabase.GetAssetPath(picture.sprite).EndsWith("/TitleLabyrinth.png",StringComparison.OrdinalIgnoreCase))
                { picture.sprite = backdrop; EditorUtility.SetDirty(picture); }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        finally { if (!alreadyLoaded) EditorSceneManager.CloseScene(scene,true); }
    }
    internal static void UpdateArtRevision()
    {
        var paths = Directory.GetFiles("Assets/Art/World", "*.png", SearchOption.AllDirectories).ToList();
        // Companions may live outside World alongside their editable pose PNGs.
        // Follow their authored dependencies rather than a one-dog filename list.
        foreach(var guid in AssetDatabase.FindAssets("t:CompanionData"))
            paths.AddRange(AssetDatabase.GetDependencies(AssetDatabase.GUIDToAssetPath(guid),true)
                .Where(p=>p.EndsWith(".png",StringComparison.OrdinalIgnoreCase)));
        // Combat assets remain ordinary PNGs beside native Sprite geometry wrappers.
        foreach (var list in Directory.GetFiles(Folder + "CombatMood", "*AssetFiles.json"))
        {
            var wrapped = JsonUtility.FromJson<PathList>("{\"paths\":" + File.ReadAllText(list) + "}");
            paths.AddRange(wrapped.paths.Where(File.Exists));
        }
        using var sha = SHA256.Create();
        var projectRoot = Path.GetFullPath(".").TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var normalized = paths.Select(Path.GetFullPath).Distinct().OrderBy(p => p).ToArray();
        var signature = "world-renewal-1|" + string.Join("|", normalized.Select(p => p.Substring(projectRoot.Length).Replace('\\','/') + ":" + BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p)))));
        var revision = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(signature))).Replace("-", "");
        foreach (var guid in AssetDatabase.FindAssets("t:ExplorationSettings"))
        { var settings = Load<ExplorationSettings>(AssetDatabase.GUIDToAssetPath(guid)); Backup(AssetDatabase.GetAssetPath(settings)); settings.presentationRevision = revision; EditorUtility.SetDirty(settings); }
    }
    [Serializable] sealed class PathList { public string[] paths; }
}
