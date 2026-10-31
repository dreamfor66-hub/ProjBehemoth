using System;
using System.IO;
using System.Linq;
using System.Text;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class EventGroundingVerification
{
    const string Folder = EventGroundingPreparation.Folder;
    const string Pending = "Behemoth.EventGrounding.Verify";
    static double enter = double.PositiveInfinity;
    static bool ready;
    static EventGroundingVerification()
    {
        EditorApplication.update += Poll;
        EditorApplication.playModeStateChanged += s => ready = s == PlayModeStateChange.EnteredPlayMode;
    }
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        const string file = "Temp/EventGrounding.request";
        if (File.Exists(file))
        {
            var command = File.ReadAllText(file).Trim(); File.Delete(file);
            if (command == "audit" && !EditorApplication.isPlaying) Audit();
            if ((command == "run" || command == "placement") && File.Exists(Folder + "Installed.txt"))
            { SessionState.SetString(Pending+".mode",command); SessionState.SetBool(Pending, true); enter = EditorApplication.timeSinceStartup + .5; }
            if (command == "status") File.WriteAllText(Folder + "Status.txt", "playing=" + EditorApplication.isPlaying + "; scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
        }
        if (SessionState.GetBool(Pending, false) && EditorApplication.timeSinceStartup >= enter)
        { enter = double.PositiveInfinity; EditorApplication.isPlaying = true; }
        if (SessionState.GetBool(Pending, false) && ready && EditorApplication.isPlaying && UnityEngine.Object.FindFirstObjectByType<GameFlowController>() != null)
        {
            SessionState.SetBool(Pending, false);
            var probe = new GameObject("Scoped event grounding runtime review").AddComponent<EventGroundingLiveProbe>();
            if(SessionState.GetString(Pending+".mode","")=="placement")probe.RunPlacement();else probe.Run();
        }
    }
    static void Audit()
    {
        var text = new StringBuilder(); var fail = 0; var checks = 0;
        void Check(bool ok, string message) { checks++; if (!ok) { fail++; text.AppendLine("FAIL " + message); } }
        var settings = AssetDatabase.FindAssets("t:DungeonGenerationSettings").Select(g => AssetDatabase.LoadAssetAtPath<DungeonGenerationSettings>(AssetDatabase.GUIDToAssetPath(g))).First(s => s.exploration != null);
        for (var act = 1; act <= 3; act++) for (var seed = 0; seed < 384; seed++)
        {
            try
            {
                var state = new DungeonGenerator(settings).Generate(unchecked(seed * 197927 + 6871), act); var graph = state.Graph;
                Check(DungeonGraphValidator.HasExpectedStructure(state, settings), $"B{act}/{seed} connected base structure");
                Check(graph.Rooms.Count(r => r.Type == RoomEncounterType.Shop) == act, $"B{act}/{seed} shop opportunities preserved");
                Check(graph.Rooms.Count(r => r.Type == RoomEncounterType.Camp) == 1 && graph.Rooms.Single(r => r.Type == RoomEncounterType.Camp).Connections.Count == 1, $"B{act}/{seed} single dead-end camp");
                foreach (var a in graph.Rooms.Where(r => DungeonGenerator.FacilityFamily(r.Type) is RoomEncounterType.Shop or RoomEncounterType.Goddess or RoomEncounterType.Fountain))
                {
                    var distances = graph.DistancesFrom(a);
                    foreach (var b in graph.Rooms.Where(r => r.RoomId > a.RoomId && DungeonGenerator.FacilityFamily(r.Type) == DungeonGenerator.FacilityFamily(a.Type)))
                        Check(distances[b.RoomId] >= settings.minimumRepeatedFacilityDistance, $"B{act}/{seed} {a.Type} separation {distances[b.RoomId]}");
                }
            }
            catch (Exception e) { Check(false, $"B{act}/{seed} generation: {e.Message}"); }
        }
        text.AppendLine($"{checks} checks; {fail} failures; 384 seeds per act, one serial relevant generation audit. Minimum facility distance={settings.minimumRepeatedFacilityDistance}.");
        File.WriteAllText(Folder + "GenerationAudit.txt", text.ToString());
    }
}
