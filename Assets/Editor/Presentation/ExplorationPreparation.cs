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

[InitializeOnLoad]
internal static class ExplorationPreparation
{
    internal const string Folder = "Docs/Production/Exploration-2026-10-31/";
    internal const string SettingsPath = "Assets/Data/Presentation/Data_Exploration_탐험.asset";
    const string ArtFolder = "Assets/Art/World/Exploration/";
    const string DataFolder = "Assets/Data/Presentation/Exploration/";
    static bool busy, done;
    static ExplorationPreparation() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (busy || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (File.Exists(Folder + "Installed.txt"))
        {
            if (File.Exists(Folder + "ArtComplete.txt") && !File.Exists(Folder + "ArtConnectedV2.txt")) ConnectCompletedArt();
            if (!File.Exists(Folder + "PresentationRevision2.txt")) RepairPresentation();
            if (!File.Exists(Folder + "PresentationRevision4.txt")) RepairInputSurface();
            if (!File.Exists(Folder + "AftermathAuthoredV1.txt")) AuthorAftermath();
            if (!File.Exists(Folder + "IntegrationReview/ScenePresentation.txt")) SaveArtPresentation();
            done = true; return;
        }
        if (done || !File.Exists(Folder + "ArtReady.txt")) return;
        busy = true;
        try { Install(); }
        catch (Exception e) { File.WriteAllText(Folder + "PreparationError.txt", e.ToString()); Debug.LogException(e); done = true; }
        finally { busy = false; }
    }
    static void SaveArtPresentation()
    {
        busy = true;
        try
        {
            var complete = true;
            foreach (var path in new[] { "Assets/Scenes/MainGameScene.unity", "Assets/Scenes/Trials/Angular01_Main.unity" })
            {
                var scene = SceneManager.GetSceneByPath(path); var loaded = scene.isLoaded;
                if (loaded && scene.isDirty) { complete = false; continue; }
                try
                {
                    if (!loaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    foreach (var slot in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<RoomObjectSlot>(true)))
                    { slot.PreparePresentation(); EditorUtility.SetDirty(slot); }
                    EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                }
                finally { if (!loaded && scene.isLoaded) EditorSceneManager.CloseScene(scene, true); }
            }
            if (complete) File.WriteAllText(Folder + "IntegrationReview/ScenePresentation.txt", "Saved editor-visible contact and doorway painting layers in both production scenes.\n");
        }
        catch (Exception e) { File.WriteAllText(Folder + "IntegrationReview/SceneError.txt", e.ToString()); Debug.LogException(e); }
        finally { busy = false; }
    }
    static void AuthorAftermath()
    {
        busy = true;
        try
        {
            InstallText();
            var merchant = AssetDatabase.LoadAssetAtPath<RoomObjectData>(DataFolder + "Data_RoomObject_상인.asset");
            var hourglass = AssetDatabase.LoadAssetAtPath<RoomObjectData>(DataFolder + "Data_RoomObject_모래시계.asset");
            if (string.IsNullOrEmpty(merchant.usedLabelKey)) { merchant.usedLabelKey = "ui.explore.object.merchant.remains"; EditorUtility.SetDirty(merchant); }
            if (string.IsNullOrEmpty(hourglass.usedLabelKey)) { hourglass.usedLabelKey = "ui.explore.object.table.remains"; EditorUtility.SetDirty(hourglass); }
            foreach (var guid in AssetDatabase.FindAssets("t:RandomEventData", new[] { "Assets/Data/Dungeon/Events" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid); var story = AssetDatabase.LoadAssetAtPath<RandomEventData>(path);
                if (story.worldObject != merchant && story.worldObject != hourglass) continue;
                foreach (var option in story.options.Concat(story.pages.SelectMany(p => p.options)).Where(o => o.nextPage < 0))
                {
                    if (story.worldObject == merchant && string.IsNullOrEmpty(option.revisitTextKey))
                    { option.revisitTextKey = "ui.explore.used.scholar"; EditorUtility.SetDirty(story); }
                    if (story.worldObject == hourglass && option.effects.Any(e => e.kind == EventEffectKind.Relic && e.relic != null))
                    {
                        option.useWorldObject = true;
                        if (string.IsNullOrEmpty(option.revisitTextKey)) option.revisitTextKey = "ui.explore.used.hourglass.taken";
                        EditorUtility.SetDirty(story);
                    }
                }
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText(Folder + "AftermathAuthoredV1.txt", "Authored terminal choice aftermath for retained NPCs and collected hourglass; localized consumed-object hover labels.\n");
        }
        catch (Exception e) { File.WriteAllText(Folder + "AftermathError.txt", e.ToString()); Debug.LogException(e); }
        finally { busy = false; }
    }
    static void RepairInputSurface()
    {
        busy = true;
        try
        {
            var complete = true;
            foreach (var path in new[] { "Assets/Scenes/MainGameScene.unity", "Assets/Scenes/Trials/Angular01_Main.unity" })
            {
                var scene = SceneManager.GetSceneByPath(path); var loaded = scene.isLoaded;
                if (loaded && scene.isDirty) { complete = false; continue; }
                try
                {
                    if (!loaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    var roots = scene.GetRootGameObjects();
                    var view = roots.SelectMany(r => r.GetComponentsInChildren<ExplorationView>(true)).Single();
                    var combat = roots.SelectMany(r => r.GetComponentsInChildren<CombatSceneView>(true)).Single();
                    var navigation = roots.SelectMany(r => r.GetComponentsInChildren<NavigationChoiceView>(true)).Single();
                    view.combatInputSurface = combat.LogicalArea.GetComponent<Graphic>();
                    view.interactionLayer.SetParent(combat.LogicalArea, false); Stretch(view.interactionLayer);
                    view.interactionLayer.SetAsLastSibling();
                    var branch = navigation.transform;
                    while (branch.parent != null && branch.parent != combat.LogicalArea) branch = branch.parent;
                    if (branch.parent == combat.LogicalArea) branch.SetAsLastSibling();
                    EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                }
                finally { if (!loaded && scene.isLoaded) EditorSceneManager.CloseScene(scene, true); }
            }
            if (complete) File.WriteAllText(Folder + "PresentationRevision4.txt", "World pointer targets are above actor graphics, below navigation; coordinate frame derived from painted rectangles.\n");
        }
        catch (Exception e) { File.WriteAllText(Folder + "PresentationRepairError.txt", e.ToString()); Debug.LogException(e); }
        finally { busy = false; }
    }
    static void RepairPresentation()
    {
        busy = true;
        try
        {
            var complete = true;
            foreach (var path in new[] { "Assets/Scenes/MainGameScene.unity", "Assets/Scenes/Trials/Angular01_Main.unity" })
            {
                var scene = SceneManager.GetSceneByPath(path); var loaded = scene.isLoaded;
                if (loaded && scene.isDirty) { complete = false; continue; }
                try
                {
                    if (!loaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    var roots = scene.GetRootGameObjects();
                    var view = roots.SelectMany(r => r.GetComponentsInChildren<ExplorationView>(true)).Single();
                    var backdrop = roots.SelectMany(r => r.GetComponentsInChildren<StageBackdropView>(true)).Single();
                    var flow = roots.SelectMany(r => r.GetComponentsInChildren<GameFlowController>(true)).Single();
                    var navigation = roots.SelectMany(r => r.GetComponentsInChildren<NavigationChoiceView>(true)).Single();
                    if (view.interactionLayer == null)
                    {
                        var bg = backdrop.background.rectTransform;
                        var hitLayer = Rect(bg.parent, "Exploration Interaction");
                        hitLayer.anchorMin = bg.anchorMin; hitLayer.anchorMax = bg.anchorMax; hitLayer.pivot = bg.pivot;
                        hitLayer.sizeDelta = bg.sizeDelta; hitLayer.anchoredPosition = bg.anchoredPosition; hitLayer.localScale = bg.localScale;
                        // Interaction is above the full-screen combat input surface, while art remains below actors.
                        hitLayer.SetAsLastSibling();
                        var navigationBranch = navigation.transform;
                        while (navigationBranch.parent != null && navigationBranch.parent != hitLayer.parent) navigationBranch = navigationBranch.parent;
                        if (navigationBranch.parent == hitLayer.parent) hitLayer.SetSiblingIndex(navigationBranch.GetSiblingIndex());
                        view.interactionLayer = hitLayer;
                        foreach (var slot in view.slots) { slot.hitArea.SetParent(hitLayer, false); slot.hitArea.gameObject.SetActive(false); }
                        hitLayer.gameObject.SetActive(false);
                    }
                    var settings = AssetDatabase.LoadAssetAtPath<ExplorationSettings>(SettingsPath);
                    flow.SetEventArt(new[] { settings.For(RoomEncounterType.Chest).front, settings.For(RoomEncounterType.Fountain).front,
                        settings.For(RoomEncounterType.Goddess).front, settings.For(RoomEncounterType.Shop).front });
                    EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                }
                finally { if (!loaded && scene.isLoaded) EditorSceneManager.CloseScene(scene, true); }
            }
            if (complete) File.WriteAllText(Folder + "PresentationRevision2.txt", "Separated art and interaction layers; primary event popups use matching production paintings.\n");
        }
        catch (Exception e) { File.WriteAllText(Folder + "PresentationRepairError.txt", e.ToString()); Debug.LogException(e); }
        finally { busy = false; }
    }
    static void ConnectCompletedArt()
    {
        busy = true;
        try
        {
            PrepareTextures();
            foreach (var guid in AssetDatabase.FindAssets("t:RoomObjectData", new[] { DataFolder.TrimEnd('/') }))
            {
                var art = AssetDatabase.LoadAssetAtPath<RoomObjectData>(AssetDatabase.GUIDToAssetPath(guid));
                if (art.front == null) continue;
                var frontPath = AssetDatabase.GetAssetPath(art.front);
                var prefix = frontPath.Substring(0, frontPath.Length - "Front.png".Length);
                if (art.side == null) art.side = AssetDatabase.LoadAssetAtPath<Sprite>(prefix + "Side.png");
                if (art.rear == null) art.rear = AssetDatabase.LoadAssetAtPath<Sprite>(prefix + "Rear.png");
                if (art.usedFront == null) art.usedFront = AssetDatabase.LoadAssetAtPath<Sprite>(prefix + "UsedFront.png");
                if (art.usedSide == null) art.usedSide = AssetDatabase.LoadAssetAtPath<Sprite>(prefix + "UsedSide.png");
                if (art.usedRear == null) art.usedRear = AssetDatabase.LoadAssetAtPath<Sprite>(prefix + "UsedRear.png");
                EditorUtility.SetDirty(art);
            }
            AssetDatabase.SaveAssets(); File.WriteAllText(Folder + "ArtConnectedV2.txt", "All completed PNG direction/aftermath references connected after synchronous import; existing non-null assignments preserved.\n");
        }
        catch (Exception e) { File.WriteAllText(Folder + "ArtConnectionError.txt", e.ToString()); Debug.LogException(e); }
        finally { busy = false; }
    }
    internal static void Backup(string path)
    {
        if (!File.Exists(path)) return;
        var target = Folder + "Before/" + path; Directory.CreateDirectory(Path.GetDirectoryName(target));
        if (!File.Exists(target)) File.Copy(path, target);
    }
    static T Asset<T>(string path) where T : ScriptableObject
    {
        var data = AssetDatabase.LoadAssetAtPath<T>(path);
        if (data != null) return data;
        Directory.CreateDirectory(Path.GetDirectoryName(path)); AssetDatabase.Refresh();
        data = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(data, path); return data;
    }
    static Sprite Painting(string stem, string state)
    {
        var path = ArtFolder + stem + state + ".png";
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static RoomObjectData ObjectArt(string name, string stem, float height, bool wall, string label, string resolved)
    {
        var path = DataFolder + "Data_RoomObject_" + name + ".asset";
        var existed = File.Exists(path); var art = Asset<RoomObjectData>(path);
        if (existed) return art;
        art.front = Painting(stem, "Front"); art.side = Painting(stem, "Side"); art.rear = Painting(stem, "Rear");
        art.usedFront = Painting(stem, "UsedFront"); art.usedSide = Painting(stem, "UsedSide"); art.usedRear = Painting(stem, "UsedRear");
        art.wallMounted = wall; art.displayHeight = height; art.labelKey = label; art.resolvedTextKey = resolved;
        if (art.front == null) throw new InvalidOperationException("Missing production painting: " + stem);
        EditorUtility.SetDirty(art); return art;
    }
    static void PrepareTextures()
    {
        foreach (var path in Directory.GetFiles(ArtFolder, "*.png"))
        {
            var assetPath = path.Replace('\\', '/');
            if (AssetImporter.GetAtPath(assetPath) == null) AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            if (importer == null) throw new IOException("Texture has not imported: " + assetPath);
            if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single) continue;
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        }
    }
    static void Install()
    {
        PrepareTextures();
        InstallText();
        var chest = ObjectArt("상자", "T_UI_World_Chest_", 250, false, "ui.explore.object.chest", "ui.explore.used.chest");
        var fountain = ObjectArt("분수", "T_UI_World_Fountain_", 390, true, "ui.explore.object.fountain", "ui.explore.used.fountain");
        var goddess = ObjectArt("여신상", "T_UI_World_Goddess_", 580, true, "ui.explore.object.goddess", "ui.explore.used.goddess");
        var merchant = ObjectArt("상인", "T_Sprite_Character_행상_Seated", 310, false, "ui.explore.object.merchant", "ui.explore.used.merchant");
        var cleric = ObjectArt("성직자", "T_Sprite_Character_절망한성직자_Seated", 280, false, "ui.explore.object.cleric", "ui.explore.used.cleric");
        var scholar = ObjectArt("학자", "T_Sprite_Character_노학자_Seated", 300, false, "ui.explore.object.scholar", "ui.explore.used.scholar");
        var camp = ObjectArt("야영지", "T_UI_World_Camp_", 190, false, "ui.explore.object.camp", "ui.explore.used.camp");
        var crate = ObjectArt("나무상자", "T_UI_World_Crate_", 185, false, "ui.explore.object.crate", "ui.explore.used.crate");
        var mirror = ObjectArt("거울", "T_UI_World_Mirror_", 390, false, "ui.explore.object.mirror", "ui.explore.used.mirror");
        var hourglass = ObjectArt("모래시계", "T_UI_World_Hourglass_", 230, false, "ui.explore.object.hourglass", "ui.explore.used.hourglass");
        var seed = ObjectArt("씨앗", "T_UI_World_Seed_", 210, false, "ui.explore.object.seed", "ui.explore.used.seed");
        var fruit = ObjectArt("유리덮개", "T_UI_World_Fruit_", 245, false, "ui.explore.object.fruit", "ui.explore.used.fruit");
        var book = ObjectArt("떠있는책", "T_UI_World_Book_", 225, false, "ui.explore.object.book", "ui.explore.used.book");
        var mechanism = ObjectArt("벽장치", "T_UI_World_Mechanism_", 220, true, "ui.explore.object.mechanism", "ui.explore.used.mechanism");
        var door = ObjectArt("잠긴문", "T_UI_World_Door_", 580, true, "ui.explore.object.door", "ui.explore.door.locked");
        var key = ObjectArt("열쇠", "T_UI_World_Key_", 83, false, "ui.explore.object.key", "ui.explore.key.found");
        var settings = Asset<ExplorationSettings>(SettingsPath);
        if (settings.roomObjects.Count == 0)
        {
            settings.roomObjects = new List<RoomObjectBinding> {
                new() { encounter = RoomEncounterType.Chest, artwork = chest }, new() { encounter = RoomEncounterType.MajorReward, artwork = chest },
                new() { encounter = RoomEncounterType.Goddess, artwork = goddess }, new() { encounter = RoomEncounterType.Fountain, artwork = fountain },
                new() { encounter = RoomEncounterType.PoisonFountain, artwork = fountain }, new() { encounter = RoomEncounterType.Shop, artwork = merchant },
                new() { encounter = RoomEncounterType.Camp, artwork = camp } };
            settings.key = key; settings.mechanism = mechanism; settings.lockedDoor = door; settings.fallbackStory = crate;
            settings.concealedEntranceClues = new List<RoomObjectData> { mechanism, crate };
            EditorUtility.SetDirty(settings);
        }
        var generation = AssetDatabase.FindAssets("t:DungeonGenerationSettings").Select(g => AssetDatabase.LoadAssetAtPath<DungeonGenerationSettings>(AssetDatabase.GUIDToAssetPath(g)));
        foreach (var data in generation) { Backup(AssetDatabase.GetAssetPath(data)); if (data.exploration == null) { data.exploration = settings; EditorUtility.SetDirty(data); } }
        // Initial content authoring only: runtime discovery/presentation reads these assets, never event IDs.
        var authored = new Dictionary<string, (EventDiscovery mode, RoomObjectData prop)> {
            ["Mirror"] = (EventDiscovery.Visible, mirror),
            ["Scholar"] = (EventDiscovery.Visible, book), ["Hourglass"] = (EventDiscovery.Concealed, hourglass),
            ["Symbiosis"] = (EventDiscovery.Concealed, seed), ["AmberSeed"] = (EventDiscovery.Concealed, fruit),
            ["Investment"] = (EventDiscovery.Visible, merchant), ["StormBottle"] = (EventDiscovery.Visible, merchant),
            ["Delivery"] = (EventDiscovery.Visible, crate),
            ["MagicTeacherReturn"] = (EventDiscovery.Visible, scholar), ["MagicTeacherFirst"] = (EventDiscovery.Visible, scholar),
            ["Pilgrim"] = (EventDiscovery.Visible, merchant) };
        foreach (var guid in AssetDatabase.FindAssets("t:RandomEventData", new[] { "Assets/Data/Dungeon/Events" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid); var story = AssetDatabase.LoadAssetAtPath<RandomEventData>(path);
            if (story.worldObject != null || !authored.TryGetValue(story.id, out var entry)) continue;
            Backup(path); story.discovery = entry.mode; story.worldObject = entry.prop; EditorUtility.SetDirty(story);
            if (story.id is "Symbiosis" or "AmberSeed")
                foreach (var option in story.options.Concat(story.pages.SelectMany(p => p.options)))
                    if (option.nextPage < 0 && option.effects.Count > 0) option.useWorldObject = true;
        }
        InstallCleric(cleric);
        AssetDatabase.SaveAssets();
        var complete = true;
        foreach (var path in new[] { "Assets/Scenes/MainGameScene.unity", "Assets/Scenes/Trials/Angular01_Main.unity" })
        {
            var scene = SceneManager.GetSceneByPath(path); var loaded = scene.isLoaded;
            if (loaded && scene.isDirty) { complete = false; continue; }
            try
            {
                if (!loaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                var flow = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameFlowController>(true)).Single();
                var serialized = new SerializedObject(flow);
                if (serialized.FindProperty("explorationView").objectReferenceValue != null) continue;
                Backup(path); var view = PrepareScene(scene);
                serialized.FindProperty("explorationView").objectReferenceValue = view; serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene); if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed: " + path);
            }
            finally { if (!loaded && scene.isLoaded) EditorSceneManager.CloseScene(scene, true); }
        }
        if (complete) { done = true; File.WriteAllText(Folder + "Installed.txt", "Saved world object slots, directional PNGs, localized narration, all-act generation and event discovery data.\n"); }
    }
    static void InstallCleric(RoomObjectData cleric)
    {
        const string path = "Assets/Data/Dungeon/Events/Data_Event_꺼진_기도.asset";
        var exists = File.Exists(path); var story = Asset<RandomEventData>(path);
        if (!exists)
        {
            story.id = "QuietPrayer"; story.displayName = "꺼진 기도";
            story.narrative = "기둥 뒤에 누군가 웅크리고 앉아 있다. 낡은 성표를 쥔 손이 무릎 위에 멈춰 있다.\n\n\"여기서는 기도가 멀리 가지 못하네요.\"\n\n그녀가 천천히 고개를 든다.\n\n\"그래도 상처는 볼 수 있어요. 약초가 조금 남았거든요.\"";
            story.discovery = EventDiscovery.Concealed; story.worldObject = cleric; story.illustration = cleric.front;
            story.minAct = 1; story.maxAct = 3; story.weight = .8f;
            story.referenceExperience = "2026-10-31 사용자 탐험 제안의 구석에 앉은 성직자. 기존 Slay the Spire 참조 사건을 교체하지 않는 Behemoth 고유의 선택적 회복/대화 사건.";
            story.options = new List<EventOption> {
                new() { title = "상처를 맡긴다", description = "금화 14 지불 · 체력 25 회복", goldCost = 14,
                    resultText = "그녀는 약초를 갈아 상처를 감싼다.\n\n\"돌아가는 길을 찾으면, 저 바깥에도 아직 아침이 오는지 알려 주세요.\"",
                    revisitTextKey = "ui.explore.cleric.treated", effects = new() { new() { kind = EventEffectKind.Heal, amount = 25 } } },
                new() { title = "말없이 곁에 앉는다", description = "체력 6 회복",
                    resultText = "잠시 아무 말도 하지 않는다. 그녀의 짧은 기도가 돌벽 사이로 잦아든다.\n\n다시 일어설 때에는 숨이 조금 고르게 돌아온다.",
                    revisitTextKey = "ui.explore.cleric.quiet", effects = new() { new() { kind = EventEffectKind.Heal, amount = 6 } } },
                new() { title = "길을 계속 간다", description = "그녀를 방해하지 않는다.", resultText = "그녀는 작은 고갯짓으로 배웅한다.", revisitTextKey = "ui.explore.cleric.left" }
            };
            EditorUtility.SetDirty(story); BehemothTextEditor.SyncAsset(story);
            var csv = File.ReadAllText(BehemothTextEditor.CsvPath);
            foreach (var binding in story.textKeys)
                csv = BehemothLocaleEditing.PatchCsv(csv, binding.key, "", BehemothTextEditor.Table.Get(binding.key), path + ":" + binding.propertyPath);
            File.WriteAllText(BehemothTextEditor.CsvPath, csv, new UTF8Encoding(true));
        }
        foreach (var guid in AssetDatabase.FindAssets("t:GameContent", new[] { "Assets/Data" }))
        {
            var contentPath = AssetDatabase.GUIDToAssetPath(guid); var content = AssetDatabase.LoadAssetAtPath<GameContent>(contentPath);
            if (content.events.Contains(story)) continue;
            Backup(contentPath); content.events.Add(story); EditorUtility.SetDirty(content);
        }
    }
    static RectTransform Rect(Transform parent, string name)
    {
        var root = new GameObject(name, typeof(RectTransform)); root.transform.SetParent(parent, false); return (RectTransform)root.transform;
    }
    static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    static Image Image(Transform parent, string name)
    {
        var rect = Rect(parent, name); var image = rect.gameObject.AddComponent<Image>(); image.raycastTarget = false; return image;
    }
    static TextMeshProUGUI Text(Transform parent, string name, TextMeshProUGUI sample, float size)
    {
        var rect = Rect(parent, name); var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = sample.font; text.fontSharedMaterial = sample.fontSharedMaterial; text.fontSize = size;
        text.color = new Color(.95f, .88f, .72f); text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
        text.text = ""; return text;
    }
    static ExplorationView PrepareScene(Scene scene)
    {
        var roots = scene.GetRootGameObjects();
        var backdrop = roots.SelectMany(r => r.GetComponentsInChildren<StageBackdropView>(true)).Single();
        var hud = roots.SelectMany(r => r.GetComponentsInChildren<ArcadeHudController>(true)).Single();
        var sample = hud.mapButton.GetComponentInChildren<TextMeshProUGUI>(true);
        // Same coordinate space and clipping as the authored background, under actors and HUD.
        var layer = Rect(backdrop.background.transform, "Exploration Objects"); Stretch(layer);
        var view = backdrop.gameObject.AddComponent<ExplorationView>(); view.objectLayer = layer;
        view.slots = new RoomObjectSlot[5];
        for (var index = 0; index < view.slots.Length; index++)
        {
            var rect = Rect(layer, "World Object " + (index + 1)); var slot = rect.gameObject.AddComponent<RoomObjectSlot>();
            slot.painting = Image(rect, "Painting"); Stretch(slot.painting.rectTransform);
            var hit = Image(rect, "Interaction Area"); hit.color = Color.clear; hit.raycastTarget = true;
            hit.rectTransform.anchorMin = hit.rectTransform.anchorMax = new Vector2(.5f, .5f); slot.hitArea = hit.rectTransform;
            slot.button = hit.gameObject.AddComponent<Button>(); slot.button.targetGraphic = hit; slot.button.transition = Selectable.Transition.None;
            hit.gameObject.AddComponent<RoomObjectPointer>().slot = slot;
            view.slots[index] = slot;
            rect.gameObject.SetActive(false);
        }
        var canvas = hud.transform.GetComponentInParent<Canvas>().transform;
        var narration = Image(canvas, "Exploration Narration"); narration.color = new Color(.025f, .025f, .02f, .94f);
        narration.rectTransform.anchorMin = narration.rectTransform.anchorMax = new Vector2(.5f, 0);
        narration.rectTransform.anchoredPosition = new Vector2(0, 170); narration.rectTransform.sizeDelta = new Vector2(1020, 70);
        view.narration = narration.gameObject.AddComponent<CanvasGroup>(); view.narration.alpha = 0; view.narration.blocksRaycasts = false; view.narration.interactable = false;
        view.narrationText = Text(narration.transform, "Narration", sample, 27); Stretch(view.narrationText.rectTransform);
        view.hoverText = Text(canvas, "Exploration Interaction Hint", sample, 25);
        view.hoverText.rectTransform.anchorMin = view.hoverText.rectTransform.anchorMax = new Vector2(.5f, 0);
        view.hoverText.rectTransform.anchoredPosition = new Vector2(0, 123); view.hoverText.rectTransform.sizeDelta = new Vector2(650, 45);
        view.hoverText.gameObject.SetActive(false);
        view.inventoryKeys = Text(hud.inventoryPanel.transform, "Exploration Keys", sample, 23);
        view.inventoryKeys.alignment = TextAlignmentOptions.TopRight;
        view.inventoryKeys.rectTransform.anchorMin = view.inventoryKeys.rectTransform.anchorMax = Vector2.one;
        view.inventoryKeys.rectTransform.pivot = Vector2.one;
        view.inventoryKeys.rectTransform.anchoredPosition = new Vector2(-42, -70);
        view.inventoryKeys.rectTransform.sizeDelta = new Vector2(450, 38);
        return view;
    }
    static void InstallText()
    {
        Backup(BehemothTextEditor.TablePath); Backup(BehemothTextEditor.CsvPath);
        var texts = new Dictionary<string, string> {
            ["object.chest"] = "보물상자 조사", ["object.fountain"] = "분수 살펴보기", ["object.goddess"] = "여신상에 다가가기",
            ["object.merchant"] = "말 걸기", ["object.cleric"] = "웅크린 사람에게 말 걸기", ["object.scholar"] = "노인에게 말 걸기",
            ["object.camp"] = "야영지 살펴보기", ["object.crate"] = "상자 안 살펴보기", ["object.mirror"] = "거울 들여다보기",
            ["object.mechanism"] = "어긋난 장치 살펴보기", ["object.door"] = "잠긴 문 조사", ["object.key"] = "열쇠 줍기",
            ["object.hourglass"] = "모래시계 살펴보기", ["object.seed"] = "실험대의 씨앗 살펴보기", ["object.fruit"] = "유리 덮개 안 살펴보기", ["object.book"] = "떠 있는 책 조사",
            ["object.table.remains"] = "남겨진 탁자 살펴보기", ["object.merchant.remains"] = "상인이 떠난 자리 살펴보기",
            ["used.hourglass.taken"] = "모래시계가 놓여 있던 자리다. 탁자 위에는 먼지 자국만 남았다.",
            ["used.hourglass"] = "모래는 여전히 제멋대로 흐른다. 다시 손대지는 말자.", ["used.seed"] = "실험대에서 살펴볼 것은 모두 살펴봤다.",
            ["used.fruit"] = "한 번 살펴본 화분이다. 더 손댈 것은 없다.", ["used.book"] = "책에서 더 알아낼 것은 없다.",
            ["cleric.treated"] = "그녀는 남은 붕대를 접고 있다. \"다시 다치지 않게 조심하세요.\"",
            ["cleric.quiet"] = "그녀가 작은 목소리로 기도를 되뇐다. 이번에는 조금 덜 외롭게 들린다.",
            ["cleric.left"] = "그녀는 여전히 기둥 곁에 앉아 있다. \"……\"",
            ["used.chest"] = "이미 열려 있는 상자다. 안에는 아무것도 남지 않았다.",
            ["used.fountain"] = "물이 말랐다. 다시 마실 수는 없겠지.", ["used.goddess"] = "차가운 돌의 감촉만 남았다.",
            ["used.merchant"] = "상인은 떠났다. 내려놓은 포장 천만 남아 있다.", ["used.cleric"] = "그녀는 고개를 숙인 채 조용히 앉아 있다. \"……\"",
            ["used.scholar"] = "노인은 생각에 잠겨 있다. 지금은 말을 아끼는 편이 좋겠다.", ["used.camp"] = "쉬었던 자리다. 지금은 다시 출발할 때다.",
            ["used.crate"] = "한 번 살펴본 상자다. 더 나올 것은 없다.", ["used.mirror"] = "거울 속에는 익숙한 모습만 비친다.",
            ["used.mechanism"] = "장치는 이미 움직였다.", ["key.found"] = "작은 열쇠를 주웠다. 머리에는 태양 문양이 새겨져 있다.",
            ["door.locked"] = "잠겨 있다. 열쇠 구멍 위에 태양 문양이 보인다.", ["passage.locked"] = "벽이 밀려나며 문이 드러났다. 태양 문양의 자물쇠가 걸려 있다.",
            ["door.opened"] = "열쇠가 맞는다. 잠금이 풀렸다.", ["passage.opened"] = "돌이 움직인다. 벽 뒤에 통로가 있었다.", ["map.locked"] = "잠",
            ["inventory.key"] = "태양 문양 열쇠 × {0}" };
        var table = BehemothTextEditor.Table; var csv = File.ReadAllText(BehemothTextEditor.CsvPath);
        var csvTable = ScriptableObject.CreateInstance<GameTextDatabase>(); csvTable.ImportCsv(csv);
        foreach (var text in texts)
        {
            var key = "ui.explore." + text.Key;
            if (!table.Contains(key)) table.Set(key, text.Value, "Exploration");
            if (!csvTable.Contains(key)) csv = BehemothLocaleEditing.PatchCsv(csv, key, "", table.Get(key), "Exploration");
        }
        const string hintKey = "ui.tutorial.exploration.body";
        var old = csvTable.Contains(hintKey) ? csvTable.Get(hintKey) : table.Get(hintKey);
        if (!old.Contains("방 안"))
        {
            var hint = old + "\n\n방 안의 사물이나 사람을 눌러 조사할 수 있습니다. 지나쳤던 곳에도 작은 단서가 남아 있을지 모릅니다.";
            csv = BehemothLocaleEditing.PatchCsv(csv, hintKey, old, hint, "Exploration"); table.Set(hintKey, hint, "Exploration");
        }
        Object.DestroyImmediate(csvTable);
        EditorUtility.SetDirty(table); File.WriteAllText(BehemothTextEditor.CsvPath, csv, new UTF8Encoding(true));
    }
}
