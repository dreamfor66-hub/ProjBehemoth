using System;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// One saved migration to the user's supplied composition; later designer edits stay intact.
[InitializeOnLoad]
internal static class MonsterPlacementPreparation
{
    const string Stamp = "Assets/Data/Presentation/Angular/MonsterPlacementInstalled.txt";
    const string Revision = "hitbox-bottom-root-minus150-2";
    const string Report = "Docs/Production/MonsterSpawnHeight-2026-10-27/";
    static bool busy;
    static double next;
    static MonsterPlacementPreparation() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (busy || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + 3;
        if (File.Exists(Stamp) && File.ReadAllText(Stamp) == Revision) return;
        busy = true;
        try
        {
            const string path = "Assets/Scenes/MainGameScene.unity";
            var scene = SceneManager.GetSceneByPath(path); var loaded = scene.isLoaded;
            Directory.CreateDirectory(Report);
            if (!File.Exists(Report + "Before-MainGameScene.unity.txt")) File.Copy(path, Report + "Before-MainGameScene.unity.txt");
            // Preserve the open scene's designer edits as well as the saved file before the scoped change.
            if (loaded && scene.isDirty && !File.Exists(Report + "Before-OpenScene.unity"))
                EditorSceneManager.SaveScene(scene, Report + "Before-OpenScene.unity", true);
            if (!loaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var layout = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CombatSceneView>(true)).Single();
                var serialized = new SerializedObject(layout);
                var anchor = (RectTransform)serialized.FindProperty("monsterAnchor").objectReferenceValue;
                anchor.anchoredPosition = new Vector2(0f, -150f);
                serialized.FindProperty("multiMonsterSpacing").floatValue = 370f;
                serialized.FindProperty("multiMonsterDepth").floatValue = 65f;
                serialized.FindProperty("twoMonsterOffset").vector2Value = new Vector2(270f, -35f);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            finally { if (!loaded) EditorSceneManager.CloseScene(scene, true); }
            // This animation is the existing goblin hunter artwork. Its old padding compensation
            // shifted the target a second time; the supplied reference uses local center (0,0).
            var goblin = AssetDatabase.LoadAssetAtPath<CombatSpriteAnimation>("Assets/Data/Presentation/Angular/Animations/LanternImp.asset");
            goblin.combatPositionOffset = Vector2.zero; EditorUtility.SetDirty(goblin);
            AssetDatabase.SaveAssets();
            File.WriteAllText(Stamp, Revision); AssetDatabase.ImportAsset(Stamp);
            Directory.CreateDirectory(Report);
            File.WriteAllText(Report + "Installed.txt", "Saved MonsterSpawnRoot=(0,-150). Hitbox-bottom placement and authored formation/member offsets retained. Default trio bottoms=(-370,-215),(0,-150),(370,-215); default pair bottoms=(-270,-185),(270,-185). Existing open-scene designer edits preserved.\n");
        }
        catch (Exception error) { Directory.CreateDirectory(Report); File.WriteAllText(Report + "Preparation-Pending.txt", error.ToString()); next += 10; }
        finally { busy = false; }
    }
}
