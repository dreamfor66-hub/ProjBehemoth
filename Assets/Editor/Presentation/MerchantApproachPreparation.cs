using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DungeonSlash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
internal static class MerchantApproachPreparation
{
    internal const string Folder = "Docs/Production/WorldRenewal-2026-11-01/Merchant/";
    const string Revision = "v1";
    static double next;
    static MerchantApproachPreparation() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + 4;
        if (!File.Exists(Folder + "Ready.txt") || File.Exists(Folder + "Installed.txt") && File.ReadAllText(Folder + "Installed.txt").StartsWith(Revision)) return;
        if (!File.Exists("Docs/Production/EncounterReconstruction-2026-11-01/Installed.txt")) return;
        // Wait for the author's new event pages; installing an old page index would route to a different action.
        if (!File.Exists(Folder + "RearResponses.json")) return;
        var scenes = new[] { "Assets/Scenes/MainGameScene.unity", "Assets/Scenes/Trials/Angular01_Main.unity" };
        if (scenes.Any(path => { var scene = SceneManager.GetSceneByPath(path); return scene.isLoaded && scene.isDirty; })) return;
        try { Install(scenes); }
        catch (Exception e) { Directory.CreateDirectory(Folder); File.WriteAllText(Folder + "PreparationError.txt", e.ToString()); Debug.LogException(e); next += 60; }
    }
    [Serializable] sealed class Map { public List<Binding> bindings; }
    [Serializable] sealed class Binding { public string id; public int page; public int option; public string narrative; }
    static T Asset<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        Directory.CreateDirectory(Path.GetDirectoryName(path)); AssetDatabase.Refresh();
        asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset;
    }
    static void Install(string[] scenes)
    {
        var table = BehemothTextEditor.Table;
        var csv = Object.Instantiate(table); if (File.Exists(BehemothTextEditor.CsvPath)) csv.ImportCsv(File.ReadAllText(BehemothTextEditor.CsvPath));
        void Text(string key, string text) { table.Set(key, text, "Merchant and NPC approach"); csv.Set(key, text, "Merchant and NPC approach"); }
        void Sync(LocalizedData data)
        {
            EditorUtility.SetDirty(data); BehemothTextEditor.SyncAsset(data);
            foreach (var binding in data.textKeys) csv.Set(binding.key, table.Get(binding.key), "Merchant and NPC approach");
        }
        var content = AssetDatabase.FindAssets("t:GameContent", new[] { "Assets/Data" }).Select(g => AssetDatabase.LoadAssetAtPath<GameContent>(AssetDatabase.GUIDToAssetPath(g))).First(c => c.events.Count > 0);
        var settings = AssetDatabase.FindAssets("t:ExplorationSettings", new[] { "Assets/Data" }).Select(g => AssetDatabase.LoadAssetAtPath<ExplorationSettings>(AssetDatabase.GUIDToAssetPath(g))).First();
        var policy = Asset<ShopInventorySettings>("Assets/Data/Dungeon/Data_ShopInventory_행상재고.asset");
        EditorUtility.SetDirty(policy);
        // New price fields are saved on the real perk assets, so every price is inspectable before Play.
        foreach (var perk in content.perks.Where(p => p != null)) EditorUtility.SetDirty(perk);
        Text("ui.npc.approach.ambush", "기습한다"); Text("ui.npc.approach.talk", "말을 건다");
        Text("ui.npc.approach.merchant.title", "후드를 쓴 행상");
        Text("ui.npc.approach.merchant.rear", "행상은 내 쪽을 등진 채 자루의 끈을 조여 묶고 있다. 손이 닿을 거리다.");
        Text("ui.npc.approach.merchant.reveal", "후드 아래로 누런 턱이 벌어진다. 부풀어 오른 몸이 옷솔기를 터뜨리고, 혀끝에 걸린 금화가 돌바닥에 쏟아진다.");
        Text("ui.npc.approach.merchant.victory", "찢어진 후드 아래로 더는 움직임이 없다. 흩어진 금화를 거둔다.");
        var merchant = settings.For(RoomEncounterType.Shop);
        var profile = Asset<NpcApproachData>("Assets/Data/Dungeon/NpcApproaches/Data_NpcApproach_행상.asset");
        profile.titleKey = "ui.npc.approach.merchant.title"; profile.narrativeKey = "ui.npc.approach.merchant.rear";
        var reaction = Asset<RandomEventData>("Assets/Data/Dungeon/NpcApproaches/Data_EventResponse_행상기습.asset");
        reaction.id = "MerchantRearAmbush"; reaction.displayName = "끊어진 거래"; reaction.excludedFromRandomPool = true;
        reaction.encounterKind = EventEncounterKind.Person; reaction.worldObject = merchant; reaction.illustration = merchant.front; reaction.illustratedPopup = true;
        reaction.authoringLore = "통로에서 생계를 잇는 행상은 짐을 빼앗기기 전에 도주한다. 모든 행상이 괴물인 것은 아니다. 희귀 위장 개체만 별도 실제 전투로 연결된다.";
        reaction.narrative = "행상의 뒤에서 짐자루를 낚아챈다.";
        reaction.options = new List<EventOption> { new()
        {
            title = "기습한다", description = "짐자루를 노린다. 체력 −8 · 골드 +30 · 이 행상과 거래할 수 없게 된다.", hpCost = 8,
            resultText = "자루를 잡아당기자 행상이 몸을 틀어 손목 안쪽으로 짧은 칼을 긋는다. 손을 놓친 사이, 그는 짐을 끌고 통로 너머로 달아난다.\n\n바닥에 쏟아진 동전 몇 닢만 건진다.",
            removeWorldObject = true, revisitTextKey = "ui.explore.used.merchant",
            effects = new List<EventEffect> { new() { kind = EventEffectKind.Gold, amount = 30 } }
        } };
        reaction.pages.Clear(); Sync(reaction); profile.standaloneResponse = reaction;
        var mutant = AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/Combat/Monsters/Data_Monster_Special_금먹는두꺼비.asset");
        if (mutant == null) throw new InvalidOperationException("The merchant's installed concealed combat identity is missing.");
        var revealed = Asset<EncounterData>("Assets/Data/Dungeon/NpcApproaches/Data_Encounter_행상의본색.asset");
        revealed.id = "MerchantConcealedIdentity"; revealed.displayName = mutant.displayName; revealed.hint = table.Get("ui.npc.approach.merchant.reveal");
        revealed.category = RoomEncounterType.Combat; revealed.members = new List<EncounterMember> { new() { monster = mutant, actionDelay = 1.8f } }; Sync(revealed);
        profile.encounter = null; profile.concealedEncounter = revealed; profile.concealedChance = .05f; profile.concealedMinAct = 2;
        profile.combatAnnouncementKey = "ui.npc.approach.merchant.reveal"; profile.victoryTextKey = "ui.npc.approach.merchant.victory";
        profile.eventResponses.Clear(); profile.removeAfterCombat = true; profile.aftermath = null; merchant.rearApproach = profile;
        EditorUtility.SetDirty(profile); EditorUtility.SetDirty(merchant);

        var bindings = JsonUtility.FromJson<Map>(File.ReadAllText(Folder + "RearResponses.json")).bindings;
        var touched = new HashSet<RoomObjectData>();
        foreach (var binding in bindings)
        {
            var story = content.events.Single(e => e != null && e.id == binding.id && !e.excludedFromRandomPool);
            var choices = binding.page < 0 ? story.options : story.pages[binding.page].options;
            var option = choices[binding.option];
            if (story.encounterKind != EventEncounterKind.Person || option.nextPage >= 0 || option.KeepsEncounterAvailable)
                throw new InvalidOperationException("Invalid hostile response route: " + binding.id);
            var art = story.worldObject; if (art == null) throw new InvalidOperationException("NPC has no visible subject: " + story.id);
            var target = Asset<NpcApproachData>("Assets/Data/Dungeon/NpcApproaches/Data_NpcApproach_" + art.name.Replace("Data_RoomObject_", "") + ".asset");
            if (touched.Add(art)) { target.eventResponses.Clear(); target.standaloneResponse = null; target.encounter = target.concealedEncounter = null; target.concealedChance = 0; }
            var key = "ui.npc.approach.rear." + story.id;
            Text(key, binding.narrative);
            // All stories sharing this person's painting keep their own exact hostile choice.
            target.eventResponses.Add(new NpcApproachData.EventResponse { story = story, narrativeKey = key, page = binding.page, option = binding.option });
            target.titleKey = art.labelKey; target.narrativeKey = key;
            art.rearApproach = target; EditorUtility.SetDirty(art); EditorUtility.SetDirty(target);
        }
        foreach (var path in scenes)
        {
            var scene = SceneManager.GetSceneByPath(path); var loaded = scene.isLoaded;
            if (!loaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var run = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<RunController>(true)).Single();
                var serialized = new SerializedObject(run); serialized.FindProperty("shopInventory").objectReferenceValue = policy; serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(run); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            finally { if (!loaded) EditorSceneManager.CloseScene(scene, true); }
        }
        File.WriteAllText(BehemothTextEditor.CsvPath, csv.ExportCsv(), new UTF8Encoding(true)); Object.DestroyImmediate(csv); EditorUtility.SetDirty(table); AssetDatabase.SaveAssets();
        Directory.CreateDirectory(Folder); File.WriteAllText(Folder + "Installed.txt", Revision + "\nSaved 3 potion / 3 perk / 4 relic stock, per-perk prices and " + bindings.Count + " NPC event response routes plus merchant rear approach.\n");
        if (File.Exists(Folder + "PreparationError.txt")) File.Delete(Folder + "PreparationError.txt");
    }
}
