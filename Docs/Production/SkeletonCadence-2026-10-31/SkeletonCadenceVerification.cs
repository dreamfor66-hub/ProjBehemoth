using System;
using System.IO;
using DungeonSlash;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Temporary scoped check in the existing live Editor; archived after verification.
[InitializeOnLoad]
internal static class SkeletonCadenceVerification
{
    const string Request = "Temp/SkeletonCadence.request";
    const string Folder = "Docs/Production/SkeletonCadence-2026-10-31/";
    const string Pending = "SkeletonCadence.pending";
    static CombatController combat;
    static MonsterRuntime enemy;
    static MonsterState previous;
    static int count;
    static bool running, afterStun;
    static double started;
    static SkeletonCadenceVerification() => EditorApplication.update += Poll;
    static void Log(string text) => File.AppendAllText(Folder + "Live.txt", text + "\n");
    static void Finish(string result)
    {
        Log(result); running = false; SessionState.SetBool(Pending, false);
        EditorApplication.isPlaying = false;
    }
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        try
        {
            if (File.Exists(Request))
            {
                File.Delete(Request); Directory.CreateDirectory(Folder);
                File.WriteAllText(Folder + "Live.txt", "Existing Unity Editor, production CombatController and saved Skeleton data. Player healed between hits; attack timing/data unchanged.\n");
                SessionState.SetBool(Pending, true);
                EditorApplication.isPlaying = true;
                return;
            }
            if (!running && SessionState.GetBool(Pending, false) && EditorApplication.isPlaying)
            {
                combat = Object.FindFirstObjectByType<CombatController>();
                if (combat == null) return;
                var practice = new GameObject("Scoped Skeleton cadence check").AddComponent<OctoberReviewLiveProbe>();
                practice.Setup(CharacterKind.Warrior, false);
                var source = AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/Combat/Monsters/Data_Monster_B1_Normal_해골_전사.asset");
                if (source.normalAttackInterval != 2f || source.chargeInterval != 5f) throw new Exception("Unexpected saved timing");
                combat.ResetEncounter(); combat.PrepareEncounter(combat.Run, new[] { source }, 1); combat.BeginPreparedCombat();
                enemy = combat.Enemies[0]; previous = enemy.State;
                count = 0; afterStun = false; running = true; started = EditorApplication.timeSinceStartup;
                Log("PASS imported asset: normal=2, charge=5; initial state=" + previous);
            }
            if (!running) return;
            if (!EditorApplication.isPlaying) { running = false; SessionState.SetBool(Pending, false); return; }
            if (EditorApplication.timeSinceStartup - started > 65) { Finish("FAIL timeout"); return; }
            combat.Player.Heal(combat.Player.MaxHp);
            var state = enemy.State;
            if (state == previous) return;
            previous = state;
            if (state != MonsterState.Attacking && state != MonsterState.Charging) return;
            var expected = count % 3 == 2 ? MonsterState.Charging : MonsterState.Attacking;
            Log($"{(state == expected ? "PASS" : "FAIL")} {(afterStun ? "post-stun" : "normal-repeat")} action {count + 1}: {state} at {Time.time:F3}s");
            if (state != expected) { Finish("FAIL unexpected attack order"); return; }
            count++;
            if (!afterStun && count == 6)
            {
                enemy.EnterStun(); previous = enemy.State; afterStun = true; count = 0;
                Log("PASS two consecutive weak/weak/strong cycles; charge interrupted with stun to verify recovery cadence");
            }
            else if (afterStun && count == 3) Finish("PASS complete: opening, repeat, and post-stun weak/weak/strong");
        }
        catch (Exception e)
        {
            Directory.CreateDirectory(Folder); Log(e.ToString()); Finish("FAIL exception");
        }
    }
}
