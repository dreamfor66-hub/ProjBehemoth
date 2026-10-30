using System;
using System.IO;
using DungeonSlash;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Explicit, short check in the existing Editor; never runs a balance suite.
[InitializeOnLoad]
internal static class WarriorPoseTimingVerification
{
    const string Request = "Temp/WarriorPoseTiming.request";
    const string Log = "Docs/Production/WarriorFrontTiming-2026-10-29/Runtime.txt";
    static GameObject fixture;
    static PlayerView view;
    static PlayerCombatRuntime runtime;
    static CombatSpriteAnimator pose;
    static ArcadePresentation presentation;
    static int step, direction, failures;
    static float started;
    static double deadline;
    static bool running, stopAfter, oldBackground;
    static WarriorPoseTimingVerification() => EditorApplication.update += Poll;
    static void Check(bool ok, string message)
    {
        File.AppendAllText(Log, (ok ? "PASS " : "FAIL ") + message + "\n");
        if (!ok) failures++;
    }
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        try
        {
            if (!running && File.Exists(Request))
            {
                File.Delete(Request);
                File.WriteAllText(Log, "Scoped live frame check: production PlayerView and saved Warrior images. No combat/DPS suite.\n");
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (!EditorApplication.isPlaying && scene.isDirty)
                { File.AppendAllText(Log, "PENDING: designer's unsaved scene preserved.\n"); return; }
                running = true; stopAfter = !EditorApplication.isPlaying; failures = step = direction = 0;
                oldBackground = Application.runInBackground; Application.runInBackground = true;
                deadline = EditorApplication.timeSinceStartup + 40;
                if (stopAfter) EditorApplication.isPlaying = true;
            }
            if (!running) return;
            if (EditorApplication.timeSinceStartup > deadline) { Check(false, "timeout"); Finish(); return; }
            if (!EditorApplication.isPlaying || EditorApplication.isPaused) return;
            if (fixture == null)
            {
                var data = AssetDatabase.LoadAssetAtPath<PlayerCombatData>("Assets/Data/Characters/Warrior.asset");
                fixture = new GameObject("Scoped Warrior painting timing", typeof(RectTransform));
                fixture.hideFlags = HideFlags.DontSave;
                var child = new GameObject("Painting", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                child.transform.SetParent(fixture.transform, false);
                presentation = ScriptableObject.CreateInstance<ArcadePresentation>();
                view = fixture.AddComponent<PlayerView>(); view.presentation = presentation;
                view.Configure((RectTransform)fixture.transform, child.GetComponent<Image>(), null, null, null);
                runtime = new PlayerCombatRuntime(data); view.Bind(runtime); pose = child.GetComponent<CombatSpriteAnimator>();
                Check(Mathf.Abs(data.combatAnimation.attackPoseSeconds - .495f) < .00001f, "Warrior painting duration = .495 seconds (1.65 x .3)");
                Check(Mathf.Abs(runtime.CurrentCooldown - .85f) < .00001f, "attack cooldown remains .85 seconds");
                foreach (var name in new[] { "Rogue", "Monk", "Wizard", "Samurai", "Barbarian" })
                {
                    var other = AssetDatabase.LoadAssetAtPath<PlayerCombatData>("Assets/Data/Characters/" + name + ".asset");
                    Check(other != null && Mathf.Approximately(other.combatAnimation.attackPoseSeconds, .3f), name + " duration remains .3 seconds");
                }
            }
            view.Bind(runtime);
            if (step == 0)
            {
                view.PlayAttack((SwordSwingDirection)direction); started = Time.time; step = 1;
                Check(pose.State == CombatVisualState.Attack, "immediate Attack " + direction);
            }
            else if (step == 1 && Time.time - started >= .35f)
            {
                Check(pose.State == CombatVisualState.Attack, "still Attack after old .3s deadline " + direction);
                step = 2;
            }
            else if (step == 2 && Time.time - started >= .495f)
            {
                Check(pose.State == CombatVisualState.Idle, "Idle at elapsed " + (Time.time - started).ToString("F4") + " direction " + direction);
                Check(pose.GetComponent<Image>().sprite == runtime.Data.combatAnimation.idle, "valid Idle painting restored");
                if (++direction < 2) step = 0; else Finish();
            }
        }
        catch (Exception e) { File.AppendAllText(Log, "FAIL " + e + "\n"); failures++; Finish(); }
    }
    static void Finish()
    {
        running = false;
        if (fixture != null) Object.DestroyImmediate(fixture);
        if (presentation != null) Object.DestroyImmediate(presentation);
        Application.runInBackground = oldBackground;
        File.AppendAllText(Log, "COMPLETE failures=" + failures + "\n");
        if (stopAfter) EditorApplication.isPlaying = false;
    }
}
