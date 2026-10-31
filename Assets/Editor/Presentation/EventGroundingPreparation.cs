using System;
using System.IO;
using System.Linq;
using System.Text;
using DungeonSlash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Explicit production revision: saved data, editable PNGs and saved F1 references.
[InitializeOnLoad]
internal static class EventGroundingPreparation
{
    internal const string Folder = "Docs/Production/EventGrounding-2026-10-31/";
    const string Data = "Assets/Data/Presentation/Exploration/";
    static double next;
    [Serializable] class Plan { public Event[] events; }
    [Serializable] class Event
    {
        public string id, path, art, displayName; public bool npc, automatic;
        public Page[] pages; public Field[] fields; public Terminal[] terminals;
    }
    [Serializable] class Page { public int index; public string narrative; }
    [Serializable] class Field { public string path, value; }
    [Serializable] class Terminal { public int page, index; public string text, revisit; public bool remove, used; }
    [Serializable] class ArtSet { public Art[] art; public Camp[] camps; }
    [Serializable] class Camp { public int act; public string path; }
    [Serializable] class Art { public string name, front, side, rear, usedFront, usedSide, usedRear; public bool npc; public float height, aftermathHeight; }
    static EventGroundingPreparation() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode
            || EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + 3;
        if (File.Exists(Folder + "Installed.txt") || !File.Exists(Folder + "ArtReady.json")) return;
        if (Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)
            .Select(UnityEngine.SceneManagement.SceneManager.GetSceneAt).Any(s => s.isDirty)) return;
        try { Install(); }
        catch (Exception e) { File.WriteAllText(Folder + "PreparationError.txt", e.ToString()); Debug.LogException(e); next += 60; }
    }
    static void Install()
    {
        var art = JsonUtility.FromJson<ArtSet>(File.ReadAllText(Folder + "ArtReady.json"));
        foreach (var entry in art.art)
        {
            foreach (var path in new[] { entry.front, entry.side, entry.rear, entry.usedFront, entry.usedSide, entry.usedRear }.Where(p => !string.IsNullOrEmpty(p)).Distinct())
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.spritePixelsPerUnit = 100;
                importer.filterMode = FilterMode.Bilinear; importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            var pathData = Data + "Data_RoomObject_" + entry.name + ".asset";
            var obj = AssetDatabase.LoadAssetAtPath<RoomObjectData>(pathData);
            if (obj == null) { obj = ScriptableObject.CreateInstance<RoomObjectData>(); AssetDatabase.CreateAsset(obj, pathData); }
            obj.front = Sprite(entry.front); obj.side = Sprite(entry.side); obj.rear = Sprite(entry.rear);
            obj.usedFront = Sprite(entry.usedFront); obj.usedSide = Sprite(entry.usedSide) ?? obj.usedFront; obj.usedRear = Sprite(entry.usedRear) ?? obj.usedFront;
            obj.facePlayerOnFirstEncounter = entry.npc; obj.displayHeight = entry.height > 0 ? entry.height : entry.npc ? 350 : 260;
            if (entry.name != "상인") obj.usedForm = null;
            if (entry.aftermathHeight > 0 && obj.usedFront != null)
            {
                var aftermathPath = Data + "Data_RoomObject_" + entry.name + "_소진.asset";
                var aftermath = AssetDatabase.LoadAssetAtPath<RoomObjectData>(aftermathPath);
                if (aftermath == null) { aftermath = ScriptableObject.CreateInstance<RoomObjectData>(); AssetDatabase.CreateAsset(aftermath, aftermathPath); }
                aftermath.front = obj.usedFront; aftermath.side = obj.usedSide; aftermath.rear = obj.usedRear;
                aftermath.displayHeight = entry.aftermathHeight; aftermath.labelKey = obj.labelKey;
                aftermath.resolvedTextKey = obj.resolvedTextKey; obj.usedForm = aftermath;
                EditorUtility.SetDirty(aftermath);
            }
            EditorUtility.SetDirty(obj);
        }
        foreach (var camp in art.camps)
        {
            AssetDatabase.ImportAsset(camp.path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(camp.path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false; importer.filterMode = FilterMode.Bilinear; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            var floor = AssetDatabase.LoadAssetAtPath<FloorArtwork>("Assets/Data/Presentation/Angular/B" + camp.act + ".asset");
            floor.campInterior = floor.campUsedInterior = Sprite(camp.path); EditorUtility.SetDirty(floor);
        }
        var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(Folder + "Plan.json"));
        var csv = ScriptableObject.CreateInstance<GameTextDatabase>(); csv.ImportCsv(File.ReadAllText(BehemothTextEditor.CsvPath));
        foreach (var entry in plan.events)
        {
            var story = Story(entry);
            story.displayName = entry.displayName;
            foreach (var page in entry.pages) story.pages[page.index].narrative = page.narrative;
            foreach (var field in entry.fields)
            {
                var serialized = new SerializedObject(story); var value = serialized.FindProperty(field.path);
                if (value == null) throw new InvalidOperationException(entry.id + ": " + field.path);
                value.stringValue = field.value; serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            if (story.pages.Count > 0) story.narrative = story.pages[0].narrative;
            if (!story.excludedFromRandomPool)
            {
                story.worldObject = AssetDatabase.LoadAssetAtPath<RoomObjectData>(Data + "Data_RoomObject_" + entry.art + ".asset");
                if (story.worldObject == null) throw new FileNotFoundException(entry.art);
                story.worldObject.facePlayerOnFirstEncounter = entry.npc;
                story.worldObject.labelKey = "ui.eventgrounding.object." + entry.id;
                story.worldObject.resolvedTextKey = "ui.eventgrounding.used." + entry.id;
                Set(csv, story.worldObject.labelKey, entry.displayName);
                Set(csv, story.worldObject.resolvedTextKey, entry.npc ? "이야기를 마친 이다. 더 말을 걸지 않기로 했다." : "이미 한 차례 살펴본 물건이다.");
                story.discovery = entry.automatic ? EventDiscovery.Automatic : EventDiscovery.Visible;
                story.illustration = story.worldObject.front;
                foreach (var page in story.pages) page.illustration = story.worldObject.front;
                EditorUtility.SetDirty(story.worldObject);
            }
            foreach (var outcome in entry.terminals)
            {
                var option = outcome.page < 0 ? story.options[outcome.index] : story.pages[outcome.page].options[outcome.index];
                option.resultText = outcome.text; option.removeWorldObject = outcome.remove; option.useWorldObject = outcome.used;
                option.resultingWorldObject = null;
                option.revisitTextKey = "ui.eventgrounding.revisit." + entry.id + "." + outcome.page + "." + outcome.index;
                Set(csv, option.revisitTextKey, outcome.revisit);
            }
            EditorUtility.SetDirty(story); BehemothTextEditor.SyncAsset(story);
            foreach (var key in story.textKeys) Set(csv, key.key, BehemothTextEditor.Table.Get(key.key));
        }
        var settings = AssetDatabase.LoadAssetAtPath<ExplorationSettings>(ExplorationPreparation.SettingsPath);
        settings.For(RoomEncounterType.Camp).roomInterior = true;
        EditorUtility.SetDirty(settings.For(RoomEncounterType.Camp));
        settings.For(RoomEncounterType.Shop).facePlayerOnFirstEncounter = true;
        EditorUtility.SetDirty(settings.For(RoomEncounterType.Shop));
        foreach (var guid in AssetDatabase.FindAssets("t:DungeonGenerationSettings"))
        {
            var generation = AssetDatabase.LoadAssetAtPath<DungeonGenerationSettings>(AssetDatabase.GUIDToAssetPath(guid));
            if (generation.exploration != null) { generation.minimumRepeatedFacilityDistance = 4; EditorUtility.SetDirty(generation); }
        }
        // The catalog contains playable subjects; orphan legacy paintings cannot masquerade as live events.
        foreach (var path in new[] { "Assets/Scenes/MainGameScene.unity", "Assets/Scenes/Trials/Angular01_Main.unity" })
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path); var loaded = scene.isLoaded;
            if (!loaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var overlay = roots.SelectMany(r => r.GetComponentsInChildren<DebugCheatOverlay>(true)).Single();
                var stories = plan.events.Select(Story).Where(e => !e.excludedFromRandomPool);
                overlay.roomObjectCatalog = settings.roomObjects.Select(b => b.artwork).Concat(stories.Select(e => e.worldObject))
                    .Concat(settings.concealedEntranceClues).Concat(new[] { settings.key, settings.lockedDoor, settings.mechanism }).Where(a => a != null).Distinct().OrderBy(a => a.name).ToList();
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            finally { if (!loaded) EditorSceneManager.CloseScene(scene, true); }
        }
        File.WriteAllText(BehemothTextEditor.CsvPath, csv.ExportCsv(), new UTF8Encoding(true)); Object.DestroyImmediate(csv);
        EditorUtility.SetDirty(BehemothTextEditor.Table); AssetDatabase.SaveAssets();
        File.WriteAllText(Folder + "Installed.txt", "All 24 event texts reviewed; 19 active unique subjects, terminal/revisit choices; hooded merchant, first-facing NPCs, repeated-facility distance 4. Saved data and F1 scene references.\n");
    }
    static Sprite Sprite(string path) => string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(path);
    static RandomEventData Story(Event entry) => AssetDatabase.LoadAssetAtPath<RandomEventData>(entry.path)
        ?? AssetDatabase.FindAssets("t:RandomEventData", new[] { "Assets/Data/Dungeon/Events" })
            .Select(g => AssetDatabase.LoadAssetAtPath<RandomEventData>(AssetDatabase.GUIDToAssetPath(g))).Single(e => e.id == entry.id);
    static void Set(GameTextDatabase csv, string key, string value)
    { csv.Set(key, value, "Event grounding revision"); BehemothTextEditor.Table.Set(key, value, "Event grounding revision"); }
}
