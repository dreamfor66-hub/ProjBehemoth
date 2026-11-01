using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DungeonSlash;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// A production preparation request installs authored data, never gameplay branches.
// No automatic migration runs over designer changes when the editor reopens.
[InitializeOnLoad]
internal static partial class ExplorationEncounterRevisionPreparation
{
    internal const string Folder = "Docs/Production/EncounterReconstruction-2026-11-01/";
    const string Request = "Temp/EncounterRevision.request";
    static double next;
    static ExplorationEncounterRevisionPreparation() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup < next || !File.Exists(Request)) return;
        next = EditorApplication.timeSinceStartup + 2;
        var request = File.ReadAllText(Request).Trim(); File.Delete(Request);
        if (request != "install") return;
        try { Install(); }
        catch (Exception error) { Directory.CreateDirectory(Folder); File.WriteAllText(Folder + "Error.txt", error.ToString()); Debug.LogException(error); }
    }

    [Serializable] class Plan { public Story[] events; }
    [Serializable] class Story
    {
        public string id, displayName, authoringLore, theme, narrative;
        public bool excludedFromRandomPool;
        public Page[] pages;
        public Route[] entryRoutes;
    }
    [Serializable] class Page { public string narrative; public Option[] options; public Route[] routes; }
    [Serializable] class Route { public Requirement[] requirements; public int page; }
    [Serializable] class Requirement
    {
        public EventRequirementKind kind;
        public float amount;
        public string spell, relic, flag;
        public bool invert;
    }
    [Serializable] class Effect
    {
        public EventEffectKind kind;
        public string relic, spell, encounter, flag;
        public string[] spellPool;
        public int spellChoiceCount = 3;
        public float amount;
        public ModifierKind stat;
    }
    [Serializable] class Option
    {
        public string title, description, requiredRelic, requiredPerk, requiredFlag, supply, resultText, resultingWorldObject, resultingObjectName, revisit;
        public int goldCost, supplyCost, nextPage = -1, failurePage = -1;
        public float hpCost, maxHpCost;
        public bool consumeHealingPotion, removeWorldObject, useWorldObject, leaveUnresolved;
        public Requirement[] requirements;
        public Effect[] effects;
    }
    static T Ref<T>(string guid) where T : Object => string.IsNullOrWhiteSpace(guid) ? null : AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
    static void Backup(string path)
    {
        var target = Folder + "BeforeAssets/" + path;
        if (!File.Exists(path) || File.Exists(target)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(target)); File.Copy(path, target);
    }
    static List<EventRequirement> Conditions(Requirement[] requirements) => (requirements ?? Array.Empty<Requirement>()).Select(r => new EventRequirement
    { kind = r.kind, amount = r.amount, spell = Ref<SpellData>(r.spell), relic = Ref<EquipmentData>(r.relic), flag = r.flag, invert = r.invert }).ToList();
    static List<EventEntryRoute> Routes(Route[] routes) => (routes ?? Array.Empty<Route>()).Select(r => new EventEntryRoute { requirements = Conditions(r.requirements), page = r.page }).ToList();
    static List<EventEffect> Effects(Effect[] effects) => (effects ?? Array.Empty<Effect>()).Select(e => new EventEffect
    {
        kind = e.kind, relic = Ref<EquipmentData>(e.relic), spell = Ref<SpellData>(e.spell), encounter = Ref<EncounterData>(e.encounter),
        flag = e.flag, amount = e.amount, stat = e.stat, spellChoiceCount = Math.Max(1, e.spellChoiceCount),
        spellPool = (e.spellPool ?? Array.Empty<string>()).Select(Ref<SpellData>).Where(s => s != null).ToList()
    }).ToList();

    public static void Install()
    {
        if (File.Exists(Folder + "ArtReady.json")) InstallArt();
        var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(Folder + "Plan.json"));
        var stories = AssetDatabase.FindAssets("t:RandomEventData").Select(g => Ref<RandomEventData>(g)).ToDictionary(e => e.id);
        var csv = ScriptableObject.CreateInstance<GameTextDatabase>(); csv.ImportCsv(File.ReadAllText(BehemothTextEditor.CsvPath));
        void Text(string key, string value) { csv.Set(key, value, "Encounter reconstruction"); BehemothTextEditor.Table.Set(key, value, "Encounter reconstruction"); }
        void Sync(LocalizedData data)
        {
            EditorUtility.SetDirty(data); BehemothTextEditor.SyncAsset(data);
            foreach (var binding in data.textKeys) csv.Set(binding.key, BehemothTextEditor.Table.Get(binding.key), "Encounter reconstruction");
        }
        Text("ui.encounter.pit.empty", "금고가 떨어진 빈 배수구다. 아래에는 손이 닿지 않는다.");
        var report = new StringBuilder();
        foreach (var patch in plan.events)
        {
            var story = stories[patch.id]; Backup(AssetDatabase.GetAssetPath(story));
            story.displayName = patch.displayName; story.authoringLore = patch.authoringLore;
            story.excludedFromRandomPool = patch.excludedFromRandomPool;
            story.pages = patch.pages.Select((p, pi) => new EventPage
            {
                narrative = p.narrative, illustration = story.worldObject != null ? story.worldObject.front : story.illustration,
                routes = Routes(p.routes), options = p.options.Select((o, oi) =>
                {
                    var key = "ui.encounter.revisit." + patch.id + "." + pi + "." + oi;
                    if (!string.IsNullOrWhiteSpace(o.revisit)) Text(key, o.revisit);
                    var option = new EventOption
                    {
                        title = o.title, description = o.description, goldCost = o.goldCost, hpCost = o.hpCost, maxHpCost = o.maxHpCost,
                        consumeHealingPotion = o.consumeHealingPotion, requiredRelic = Ref<EquipmentData>(o.requiredRelic), requiredPerk = o.requiredPerk, requiredFlag = o.requiredFlag,
                        supply = Ref<SupplyData>(o.supply), supplyCost = o.supplyCost, nextPage = o.nextPage, failurePage = o.failurePage, resultText = o.resultText,
                        resultingWorldObject = string.IsNullOrEmpty(o.resultingObjectName) ? Ref<RoomObjectData>(o.resultingWorldObject) : NamedAftermath(o.resultingObjectName), removeWorldObject = o.removeWorldObject, useWorldObject = o.useWorldObject,
                        leaveUnresolved = o.leaveUnresolved, revisitTextKey = string.IsNullOrWhiteSpace(o.revisit) ? null : key,
                        requirements = Conditions(o.requirements), effects = Effects(o.effects)
                    };
                    if (option.leaveUnresolved && !option.KeepsEncounterAvailable) throw new InvalidDataException(patch.id + ": postponement includes a payment or consequence");
                    if (option.nextPage >= patch.pages.Length || option.failurePage >= patch.pages.Length) throw new InvalidDataException(patch.id + ": invalid page route");
                    return option;
                }).ToList()
            }).ToList();
            story.options = story.pages[0].options.ToList(); story.narrative = story.pages[0].narrative;
            story.entryRoutes = Routes(patch.entryRoutes); story.illustration = story.worldObject != null ? story.worldObject.front : story.illustration;
            // Root production owns themes and assigns their raster/directional assets.
            // Resolve by authored asset name so this preparation remains independent of
            // theme implementation details and can be applied before or after it.
            var so = new SerializedObject(story); var field = so.FindProperty("roomTheme");
            if (field != null && !string.IsNullOrEmpty(patch.theme))
            {
                var theme = AssetDatabase.FindAssets("t:RoomThemeData").Select(g => AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault(t => t.name == "Data_RoomTheme_" + patch.theme || t.name == patch.theme);
                if (theme != null) { field.objectReferenceValue = theme; so.ApplyModifiedPropertiesWithoutUndo(); }
                else report.AppendLine("Theme pending: " + patch.id + " -> " + patch.theme);
            }
            var note = "2026-11-01 encounter reconstruction: observable subject, real occupation/purpose, authored physical ending or an unresolved NPC postponement. Original choices may be replaced; see EncounterReconstruction-2026-11-01/Review.md.";
            if (!(story.referenceExperience ?? "").Contains(note)) story.referenceExperience = (story.referenceExperience ?? "") + "\n" + note;
            if (story.worldObject != null)
            {
                Backup(AssetDatabase.GetAssetPath(story.worldObject));
                story.worldObject.labelKey = "ui.encounter.subject." + story.id; Text(story.worldObject.labelKey, story.displayName); EditorUtility.SetDirty(story.worldObject);
            }
            Sync(story); report.AppendLine((story.excludedFromRandomPool ? "Excluded " : "Active ") + story.id + " — " + story.displayName);
        }
        var bloodRoot = Ref<EquipmentData>("2900393ef75c6e542881f42f9ee141ec"); Backup(AssetDatabase.GetAssetPath(bloodRoot));
        bloodRoot.displayName = "혈근의 씨"; bloodRoot.description = "전투 3승마다 최대·현재 체력 +4. 벽의 포낭에서 떼어 낸 씨가 손목의 맥박을 함께 센다."; Sync(bloodRoot);
        File.WriteAllText(BehemothTextEditor.CsvPath, csv.ExportCsv(), new UTF8Encoding(true)); Object.DestroyImmediate(csv);
        EditorUtility.SetDirty(BehemothTextEditor.Table); AssetDatabase.SaveAssets();
        File.WriteAllText(Folder + "Installed.txt", report.ToString());
    }
    static RoomObjectData NamedAftermath(string name)
    {
        var path = "Assets/Data/Presentation/Exploration/Data_RoomObject_" + name + ".asset";
        var value = AssetDatabase.LoadAssetAtPath<RoomObjectData>(path);
        if (value == null) { value = ScriptableObject.CreateInstance<RoomObjectData>(); AssetDatabase.CreateAsset(value, path); }
        return value;
    }
}
