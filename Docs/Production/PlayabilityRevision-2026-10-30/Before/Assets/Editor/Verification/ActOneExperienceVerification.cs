using System;
using System.IO;
using System.Linq;
using System.Text;
using DungeonSlash;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// A single scoped suite in the user's existing Editor. Never starts another Editor.
[InitializeOnLoad]
internal static class ActOneExperienceVerification
{
    const string Request = "Temp/ActOneExperience.request";
    const string Folder = "Docs/Production/ActOneExperience-2026-10-28/";
    static ActOneExperienceVerification() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (SessionState.GetBool("ActOneExperiencePending", false) && EditorApplication.isPlaying)
        {
            SessionState.SetBool("ActOneExperiencePending", false);
            var pendingMode = SessionState.GetString("ActOneExperienceMode", "run");
            if(pendingMode=="growth"||pendingMode=="growth-pressure") { var growth=new GameObject("Scoped act-one actual growth verification").AddComponent<ActOneGrowthLiveProbe>();if(pendingMode=="growth-pressure")growth.RunPressure();else growth.Run(); return; }
            var probe = new GameObject("Scoped act-one experience verification").AddComponent<ActOneExperienceLiveProbe>();
            if (pendingMode == "warrior-poses") probe.RunWarriorPoses(); else if (pendingMode == "monster-poses") probe.RunMonsterPoses(); else if (pendingMode == "poses-before") probe.RunPoses(true); else if (pendingMode == "poses") probe.RunPoses(false); else if (pendingMode == "sphinx") probe.RunSphinx(); else if (pendingMode == "pressure") probe.RunPressure(); else if (pendingMode == "encounters") probe.RunEncounters(); else probe.Run();
            return;
        }
        if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        var mode = File.ReadAllText(Request).Trim(); File.Delete(Request);
        if (mode != "growth-pressure" && mode != "growth" && mode != "run" && mode != "encounters" && mode != "sphinx" && mode != "pressure" && mode != "poses" && mode != "poses-before" && mode != "monster-poses" && mode != "warrior-poses") return;
        var scene = SceneManager.GetActiveScene();
        if (scene.isDirty || !scene.path.EndsWith("MainGameScene.unity"))
        { File.WriteAllText(Folder + "Verification-Pending.txt", "Saved MainGameScene is required; designer scene preserved."); return; }
        try
        {
            if (mode == "run" || mode == "encounters") VerifyGeneration();
            SessionState.SetString("ActOneExperienceMode", mode);
            SessionState.SetBool("ActOneExperiencePending", true); EditorApplication.isPlaying = true;
        }
        catch (Exception e) { File.WriteAllText(Folder + "Verification-Error.txt", e.ToString()); Debug.LogException(e); }
    }
    static void VerifyGeneration()
    {
        var settings = AssetDatabase.LoadAssetAtPath<DungeonGenerationSettings>("Assets/Data/Settings/DungeonGenerationSettings.asset");
        var fatigue = AssetDatabase.LoadAssetAtPath<FatigueSettings>("Assets/Data/Settings/Fatigue.asset");
        var log = new StringBuilder("64 deterministic seeds: changed act-one generator and fatigue only.\n");
        for (var seed = 0; seed < 64; seed++)
        {
            var state = new DungeonGenerator(settings).Generate(seed, 1); var graph = state.Graph; var distances = graph.DistancesFrom(graph.StartRoom);
            bool ok = DungeonGraphValidator.HasExpectedStructure(state, settings)
                && graph.Rooms.Where(r => distances[r.RoomId] > 0 && distances[r.RoomId] <= settings.firstActOpeningCombats).All(r => r.Type == RoomEncounterType.Combat)
                && graph.Rooms.Where(r => r.Type == RoomEncounterType.Camp).All(r => distances[r.RoomId] >= settings.firstActCampDistance)
                && graph.Rooms.Where(r => r.Type is RoomEncounterType.Event or RoomEncounterType.Shop or RoomEncounterType.Goddess or RoomEncounterType.Fountain).All(r => distances[r.RoomId] >= settings.firstActServiceDistance)
                && graph.Rooms.Where(r => r.Type == RoomEncounterType.Event).All(r => !graph.Neighbors(r).Any(n => n.Type == RoomEncounterType.Event))
                && graph.Rooms.Max(r => r.Connections.Count) <= settings.firstActMaximumExits
                && graph.Rooms.Count(r => r.Connections.Count >= 3) <= settings.firstActMaximumJunctions;
            if (!ok) throw new InvalidOperationException("Generation invariants failed: " + seed);
            var f = new FatigueRuntime(); f.BeginFloor(fatigue, graph, 1); var grace = f.GraceRemaining;
            for (var i = 0; i <= grace; i++) f.Step();
            if (f.Value >= 100 || !f.Camp() || f.Value != 100 || f.Camp()) throw new InvalidOperationException("Fatigue/camp invariant: " + seed);
            log.AppendLine($"PASS seed={seed}, rooms={graph.Rooms.Count}, camp={distances[graph.Rooms.Single(r => r.Type == RoomEncounterType.Camp).RoomId]}, junctions={graph.Rooms.Count(r => r.Connections.Count >= 3)}, grace={grace}");
        }
        File.WriteAllText(Folder + "Generation.txt", log + "COMPLETE\n");
    }
}
