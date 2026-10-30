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
using UnityEngine.UI;
using Object = UnityEngine.Object;

// One-time, saved asset migration. Subsequent designer edits are never reapplied over.
[InitializeOnLoad]
internal static class ActOneExperiencePreparation
{
    internal const string Folder = "Docs/Production/ActOneExperience-2026-10-28/";
    const string Stamp = Folder + "Installed.txt";
    const string MapStamp = Folder + "MapPolished.txt";
    const string RoleStamp = Folder + "GuardRoleInstalled.txt";
    const string MapTypeStamp = Folder + "MapTypeInstalled.txt";
    static bool busy;
    static double next;
    [Serializable] sealed class Plan { public Tune[] monsters; }
    [Serializable] sealed class Tune
    {
        public string id, monster;
        public float hp, normal, charge, damage, guard, chargeDamage, chargeGuard;
        public string[] names;
    }
    static ActOneExperiencePreparation() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (busy || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode
            || EditorApplication.timeSinceStartup < next || File.Exists(Stamp) && File.Exists(MapStamp) && File.Exists(RoleStamp) && File.Exists(MapTypeStamp) || !File.Exists(Folder + "Tuning.json")) return;
        next = EditorApplication.timeSinceStartup + 5;
        busy = true;
        try
        {
            if (!File.Exists(Stamp)) { Prepare(); File.WriteAllText(Stamp, "act-one-experience-1; designer edits remain authoritative\n"); }
            if (!File.Exists(MapStamp)) { PolishMap(); File.WriteAllText(MapStamp, "Map typography and compact buttons checked at runtime.\n"); }
            if (!File.Exists(RoleStamp)) { PrepareGuardRole(); File.WriteAllText(RoleStamp, "Guard budget is tuned to role and act-one encounter pressure, not an old number.\n"); }
            if (!File.Exists(MapTypeStamp)) { PolishMapType(); File.WriteAllText(MapTypeStamp, "Serialized typography role sizes.\n"); }
        }
        catch (Exception e) { File.WriteAllText(Folder + "Preparation-Error.txt", e.ToString()); Debug.LogException(e); }
        finally { busy = false; }
    }
    static void PrepareGuardRole()
    {
        var item = Asset<EquipmentData>("Assets/Data/Items/Relics/Starter_Warrior.asset");
        Backup(AssetDatabase.GetAssetPath(item));
        var old = item.description;
        foreach (var ability in item.abilities)
            for (var i = 0; i < ability.effects.Count; i++)
            {
                var effect = ability.effects[i];
                if (effect.effectType != EffectType.RuleModifier || effect.rule != AbilityRule.GuardRecoveryPerSecond) continue;
                effect.effectMagnitude = 2; ability.effects[i] = effect;
            }
        var passive = item.abilities.First(a => a.trigger.triggerType == TriggerType.OnAcquire);
        if (!passive.effects.Any(e => e.effectType == EffectType.StatIncrease && e.effectStatType == ModifierKind.BrokenShieldRegen))
            passive.effects.Add(new EquipmentEffect { effectType = EffectType.StatIncrease, effectStatType = ModifierKind.BrokenShieldRegen, effectMagnitude = 5, effectCount = 1 });
        item.description = "전투 중 가드게이지가 초당 2 회복됩니다. 가드 붕괴 중 회복 속도가 추가로 5 증가합니다.";
        var binding = item.textKeys.First(b => b.propertyPath == "description");
        var csv = BehemothLocaleEditing.PatchCsv(File.ReadAllText(BehemothTextEditor.CsvPath), binding.key, old, item.description, AssetDatabase.GetAssetPath(item) + ":description");
        BehemothTextEditor.SyncAsset(item); Save(item);
        BehemothTextEditor.Table.Set("ui.combat.guard.vertical", "세로 베기");
        csv = BehemothLocaleEditing.PatchCsv(csv, "ui.combat.guard.vertical", "", "세로 베기", "Assets/Scripts/Combat/Runtime/CombatRuntime.cs");
        Save(BehemothTextEditor.Table); File.WriteAllText(BehemothTextEditor.CsvPath, csv, new UTF8Encoding(true)); AssetDatabase.ImportAsset(BehemothTextEditor.CsvPath);
    }
    static void PolishMapType()
    {
        const string path = "Assets/Scenes/MainGameScene.unity";
        var scene = SceneManager.GetSceneByPath(path); var opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        var hud = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<ArcadeHudController>(true)).Single();
        foreach (var text in hud.mapPanel.GetComponentsInChildren<TextMeshProUGUI>(true).Where(t => t.name == "Legend")) SetTypeSize(text, 18);
        var heading = hud.mapPanel.transform.Find("MapHeading")?.GetComponent<TextMeshProUGUI>();
        if (heading == null)
        {
            var root = new GameObject("MapHeading", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); root.transform.SetParent(hud.mapPanel.transform, false);
            heading = root.GetComponent<TextMeshProUGUI>();
            var sample = hud.mapButton.GetComponentInChildren<TextMeshProUGUI>(); heading.font = sample.font; heading.fontSharedMaterial = sample.fontSharedMaterial;
            heading.text = GameText.Get("ui.arcade.map"); root.AddComponent<GameTextBinding>().key = "ui.arcade.map";
        }
        SetTypeSize(heading, 30); heading.color = new Color(.9f, .85f, .73f); heading.alignment = TextAlignmentOptions.MidlineLeft; heading.raycastTarget = false;
        heading.rectTransform.anchorMin = heading.rectTransform.anchorMax = new Vector2(0, 1); heading.rectTransform.pivot = new Vector2(0, .5f);
        heading.rectTransform.anchoredPosition = new Vector2(24, -36); heading.rectTransform.sizeDelta = new Vector2(330, 44);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); if (opened) EditorSceneManager.CloseScene(scene, true);
    }
    static void SetTypeSize(TextMeshProUGUI text, float size)
    {
        text.enableAutoSizing = false; text.fontSize = size;
        if (text.TryGetComponent<TypographyRoleBinding>(out var role))
        { role.designSize = size / Mathf.Max(.01f, role.profile?.Rule(role.role)?.sizeMultiplier ?? 1); role.snapSize = false; role.Apply(); }
    }
    static void PolishMap()
    {
        const string path = "Assets/Scenes/MainGameScene.unity";
        var scene = SceneManager.GetSceneByPath(path);
        var opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        var hud = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<ArcadeHudController>(true)).Single();
        var texts = hud.mapPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var text in texts)
        {
            if (text.GetComponentInParent<MapRoomIcon>() != null || text.GetComponentInParent<Button>() != null) continue;
            if (text.name == "Legend")
            {
                text.fontSize = 18; text.enableAutoSizing = false; text.alignment = TextAlignmentOptions.Center;
                text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(.5f, 0);
                text.rectTransform.anchoredPosition = new Vector2(0, 66); text.rectTransform.sizeDelta = new Vector2(468, 28);
            }
            else if (text.name == "Title")
            {
                text.fontSize = 30; text.enableAutoSizing = false; text.alignment = TextAlignmentOptions.MidlineLeft;
                text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0, 1);
                text.rectTransform.pivot = new Vector2(0, .5f); text.rectTransform.anchoredPosition = new Vector2(24, -36);
                text.rectTransform.sizeDelta = new Vector2(330, 44);
            }
        }
        var map = hud.mapPanel.GetComponentInChildren<DungeonMapView>(true);
        ((RectTransform)map.transform).anchoredPosition = new Vector2(0, 14);
        ((RectTransform)map.transform).sizeDelta = new Vector2(464, 310);
        var inventory = (RectTransform)hud.inventoryButton.transform; var button = (RectTransform)hud.mapButton.transform;
        inventory.anchoredPosition = new Vector2(button.anchoredPosition.x + 170, inventory.anchoredPosition.y);
        inventory.sizeDelta = new Vector2(168, inventory.sizeDelta.y);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        if (opened) EditorSceneManager.CloseScene(scene, true);
    }
    static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
    static void Save(Object asset) { EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset); }
    static void Backup(string path)
    {
        var destination = Folder + "Before/" + path;
        if (File.Exists(destination)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(destination)); File.Copy(path, destination);
    }
    static void Pose(CombatSpriteAnimation animation, bool hero, int index)
    {
        if (animation == null) return;
        Backup(AssetDatabase.GetAssetPath(animation));
        // Latest direction: each pose is painted; no keyed mesh motion.
        animation.illustratedMotion = animation.emphasizePoses = false;
        animation.readyTilt = animation.attackTilt = 0;
        animation.readyOffset = animation.attackOffset = Vector2.zero;
        animation.readyScale = animation.strikeScale = Vector2.one;
        animation.strikeSeconds = .3f;
        Save(animation);
    }
    static void Prepare()
    {
        var log = new StringBuilder("Act-one scoped migration. Warrior and act-two/three numeric baselines preserved.\n");
        var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(Folder + "Tuning.json"));
        Backup(BehemothTextEditor.CsvPath); Backup(AssetDatabase.GetAssetPath(BehemothTextEditor.Table));
        var csv = File.ReadAllText(BehemothTextEditor.CsvPath);
        // Patch only the requested Korean rows, preserving unrelated external CSV edits.
        foreach (var tune in plan.monsters)
        {
            var monster = Asset<MonsterData>(tune.monster);
            for (var i = 0; i < monster.attacks.Count; i++)
            {
                var attack = monster.attacks[i];
                var binding = attack.textKeys.First(b => b.propertyPath == "displayName");
                csv = BehemothLocaleEditing.PatchCsv(csv, binding.key, attack.displayName, tune.names[i], AssetDatabase.GetAssetPath(attack) + ":displayName");
            }
        }
        const string travelHint = "현재 위치와 연결된 칸을 눌러 이동 · Tab 닫기";
        csv = BehemothLocaleEditing.PatchCsv(csv, "ui.map.navigate", "", travelHint, "Assets/Scenes/MainGameScene.unity:MapTravelHint");
        var index = 0;
        foreach (var tune in plan.monsters)
        {
            var monster = Asset<MonsterData>(tune.monster);
            if (monster == null) throw new InvalidDataException(tune.monster);
            Backup(tune.monster);
            log.AppendLine($"{monster.displayName}: HP {monster.maxHp}->{tune.hp}; normal {monster.normalAttackInterval}->{tune.normal}; charge {monster.chargeInterval}->{tune.charge}");
            monster.maxHp = tune.hp; monster.normalAttackInterval = tune.normal; monster.chargeInterval = tune.charge;
            for (var i = 0; i < monster.attacks.Count; i++)
            {
                var attack = monster.attacks[i]; Backup(AssetDatabase.GetAssetPath(attack));
                attack.displayName = tune.names[i];
                if (attack.type == MonsterAttackType.Normal) { attack.damage = tune.damage; attack.shieldDamage = tune.guard; }
                if (attack.type == MonsterAttackType.Charge && tune.chargeDamage > 0) { attack.damage = tune.chargeDamage; attack.shieldDamage = tune.chargeGuard; }
                BehemothTextEditor.SyncAsset(attack); Save(attack);
                log.AppendLine($"  {attack.displayName}: {attack.combatMechanics.hitCount} hit(s), HP {attack.damage}, guard {attack.shieldDamage}, telegraph {(attack.type == MonsterAttackType.Charge ? attack.chargeTimeLimit : attack.windupDuration)}");
            }
            Save(monster); Pose(monster.combatAnimation, false, index++);
        }
        foreach (var guid in AssetDatabase.FindAssets("t:StageSetData", new[] { "Assets/Data/Dungeon/Stages" }))
        {
            var stage = Asset<StageSetData>(AssetDatabase.GUIDToAssetPath(guid));
            if (stage.act != 1) continue;
            Backup(AssetDatabase.GetAssetPath(stage)); stage.easyCombatCount = 2; stage.packChance = .6f; Save(stage);
        }
        var generation = Asset<DungeonGenerationSettings>("Assets/Data/Settings/DungeonGenerationSettings.asset");
        Backup(AssetDatabase.GetAssetPath(generation)); Save(generation);
        var fatigue = Asset<FatigueSettings>("Assets/Data/Settings/Fatigue.asset");
        Backup(AssetDatabase.GetAssetPath(fatigue)); Save(fatigue);
        foreach (var guid in AssetDatabase.FindAssets("t:PlayerCombatData", new[] { "Assets/Data/Characters" }))
        {
            var hero = Asset<PlayerCombatData>(AssetDatabase.GUIDToAssetPath(guid));
            Pose(hero.combatAnimation, true, 0);
        }
        BehemothTextEditor.Table.Set("ui.map.navigate", travelHint);
        Save(BehemothTextEditor.Table);
        File.WriteAllText(BehemothTextEditor.CsvPath, csv, new UTF8Encoding(true)); AssetDatabase.ImportAsset(BehemothTextEditor.CsvPath);
        PrepareScene(log);
        File.WriteAllText(Folder + "Changes.txt", log.ToString());
    }
    static void PrepareScene(StringBuilder log)
    {
        const string path = "Assets/Scenes/MainGameScene.unity";
        var scene = SceneManager.GetSceneByPath(path);
        var opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        Backup(path);
        var objects = scene.GetRootGameObjects();
        var hud = objects.SelectMany(o => o.GetComponentsInChildren<ArcadeHudController>(true)).Single();
        var map = objects.SelectMany(o => o.GetComponentsInChildren<DungeonMapView>(true)).Single();
        foreach (var button in new[] { hud.mapButton, hud.inventoryButton })
            log.AppendLine(button.name + ": " + ((RectTransform)button.transform).anchoredPosition);
        // Leave the doors and navigation clickable. The map is a compact side panel below the top bar.
        var panel = (RectTransform)hud.mapPanel.transform;
        panel.anchorMin = panel.anchorMax = new Vector2(1, 1); panel.pivot = new Vector2(1, 1);
        panel.anchoredPosition = new Vector2(-28, -130); panel.sizeDelta = new Vector2(520, 500);
        if (hud.mapPanel.TryGetComponent<Image>(out var surface)) surface.raycastTarget = true;
        var mapRect = (RectTransform)map.transform;
        mapRect.anchorMin = mapRect.anchorMax = new Vector2(.5f, .5f); mapRect.pivot = new Vector2(.5f, .5f);
        mapRect.anchoredPosition = new Vector2(0, -8); mapRect.sizeDelta = new Vector2(464, 338);
        var serialized = new SerializedObject(map); serialized.FindProperty("spacing").floatValue = 58; serialized.ApplyModifiedPropertiesWithoutUndo();
        // Existing title and close remain in the same panel; position them inside its new dimensions.
        foreach (var text in hud.mapPanel.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (text.GetComponentInParent<MapRoomIcon>() != null || text.GetComponentInParent<Button>() != null) continue;
            if (text.transform.IsChildOf(map.transform)) continue;
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1); rect.anchoredPosition = new Vector2(0, -38);
            rect.sizeDelta = new Vector2(360, 42);
        }
        var close = (RectTransform)hud.closeMap.transform;
        close.anchorMin = close.anchorMax = new Vector2(1, 1); close.anchoredPosition = new Vector2(-36, -36); close.sizeDelta = new Vector2(52, 48);
        var hintObject = new GameObject("MapTravelHint", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        hintObject.transform.SetParent(panel, false);
        var hint = hintObject.GetComponent<TextMeshProUGUI>();
        var sample = hud.mapButton.GetComponentInChildren<TextMeshProUGUI>(true);
        hint.font = sample.font; hint.fontSharedMaterial = sample.fontSharedMaterial;
        hint.fontSize = 18; hint.color = new Color(.78f, .74f, .65f); hint.alignment = TextAlignmentOptions.Center;
        hint.text = GameText.Get("ui.map.navigate"); hint.raycastTarget = false;
        hintObject.AddComponent<GameTextBinding>().key = "ui.map.navigate";
        hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(.5f, 0);
        hint.rectTransform.anchoredPosition = new Vector2(0, 32); hint.rectTransform.sizeDelta = new Vector2(468, 48);
        // Keep the top menu compact without changing the surrounding portrait/resource layout.
        var inventory = (RectTransform)hud.inventoryButton.transform; var mapButton = (RectTransform)hud.mapButton.transform;
        inventory.anchoredPosition = new Vector2(mapButton.anchoredPosition.x + 210, inventory.anchoredPosition.y);
        foreach (var line in objects.SelectMany(o => o.GetComponentsInChildren<AttackLineView>(true)))
        {
            var so = new SerializedObject(line); so.FindProperty("visibleSeconds").floatValue = .24f;
            var image = so.FindProperty("image").objectReferenceValue as Image;
            if (image != null)
            {
                image.sprite = null;
                var ribbon = image.GetComponent<SlashRibbonEffect>() ?? image.gameObject.AddComponent<SlashRibbonEffect>();
                so.FindProperty("ribbon").objectReferenceValue = ribbon;
            }
            so.FindProperty("showCollisionPreview").boolValue = false; so.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        if (opened) EditorSceneManager.CloseScene(scene, true);
    }
}
