using System;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
internal static class ExplorationVerification
{
    const string Request = "Temp/Exploration.request";
    const string PendingKey = "Behemoth.ExplorationVerification.Pending";
    const string ReadyKey = "Behemoth.ExplorationVerification.PlayReady";
    static double enterPlayAt = double.PositiveInfinity;
    static void Record(string value) => File.AppendAllText(ExplorationPreparation.Folder + "BackgroundRevision/VerificationLifecycle.txt", DateTime.UtcNow.ToString("O") + " " + value + "\n");
    static ExplorationVerification()
    {
        EditorApplication.update += Poll;
        EditorApplication.playModeStateChanged += state =>
        {
            SessionState.SetBool(ReadyKey, state == PlayModeStateChange.EnteredPlayMode);
            if (!string.IsNullOrEmpty(SessionState.GetString(PendingKey, ""))) Record(state.ToString());
        };
    }
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (File.Exists(Request))
        {
            var command = File.ReadAllText(Request).Trim(); File.Delete(Request);
            if (command == "stop") { SessionState.EraseString(PendingKey); enterPlayAt = double.PositiveInfinity; EditorApplication.isPlaying = false; return; }
            if ((command == "run" || command == "art") && File.Exists(ExplorationPreparation.Folder + "Installed.txt"))
            {
                // SessionState survives the domain reload on entering Play mode.
                SessionState.SetString(PendingKey, command);
                // Defer through editor update: Inspector delayCall can wait indefinitely while the editor is unfocused.
                enterPlayAt = EditorApplication.timeSinceStartup + .5;
                Record("Requested " + command);
            }
            else if (command == "audit") Audit();
            else if (command == "status") File.WriteAllText(ExplorationPreparation.Folder + "EditorStatus.txt",
                "playing=" + EditorApplication.isPlaying + "; flow=" + Object.FindFirstObjectByType<GameFlowController>()?.State
                + "; narration=" + Object.FindFirstObjectByType<ExplorationView>()?.narration.alpha
                + "; ready=" + SessionState.GetBool(ReadyKey, false) + "; pending=" + SessionState.GetString(PendingKey, "")
                + "; artProbe=" + (Object.FindFirstObjectByType<ExplorationArtLiveProbe>() != null)
                + "; liveProbe=" + (Object.FindFirstObjectByType<ExplorationLiveProbe>() != null) + "\n");
        }
        var pending = SessionState.GetString(PendingKey, "");
        if (!string.IsNullOrEmpty(pending) && EditorApplication.timeSinceStartup >= enterPlayAt)
        {
            enterPlayAt = double.PositiveInfinity; Record("Entering Play for " + pending);
            EditorApplication.isPlaying = true; EditorApplication.isPaused = false;
        }
        if (!string.IsNullOrEmpty(pending) && SessionState.GetBool(ReadyKey, false) && EditorApplication.isPlaying
            && EditorApplication.isPlayingOrWillChangePlaymode && Object.FindFirstObjectByType<GameFlowController>() != null)
        {
            SessionState.EraseString(PendingKey);
            Record("Starting " + pending + " probe");
            if (pending == "art") new GameObject("Scoped exploration visual review").AddComponent<ExplorationArtLiveProbe>().Run();
            else new GameObject("Scoped exploration runtime verification").AddComponent<ExplorationLiveProbe>().Run();
        }
    }
    static void Audit()
    {
        try
        {
            var settings = AssetDatabase.FindAssets("t:DungeonGenerationSettings").Select(g => AssetDatabase.LoadAssetAtPath<DungeonGenerationSettings>(AssetDatabase.GUIDToAssetPath(g))).First(s => s.exploration != null);
            var report = new System.Text.StringBuilder(); var failures = 0;
            var arrangements = new System.Collections.Generic.HashSet<string>();
            for (var floor = 1; floor <= 3; floor++)
            {
                var count = 0; var locks = 0; var hidden = 0;
                for (var index = 0; index < 64; index++)
                {
                    var state = new DungeonGenerator(settings).Generate(unchecked(197927 * index + 7817), floor);
                    var graph = state.Graph;
                    void Check(bool ok, string message) { if (!ok) { failures++; report.AppendLine("FAIL B" + floor + " seed " + index + ": " + message); } }
                    Check(DungeonGraphValidator.HasExpectedStructure(state, settings), "base structure");
                    foreach (var shrine in graph.Rooms.Where(r => r.Type == RoomEncounterType.Goddess))
                    {
                        var across = graph.At(shrine.Position + DirectionUtility.Offset(shrine.ObjectWall));
                        Check(shrine.Connections.Count < 4 && (across == null || !shrine.Connections.Contains(across.RoomId)), "shrine wall is intact");
                    }
                    foreach (var passage in graph.Passages)
                    {
                        count++; if (passage.Locked) locks++; if (passage.Concealed) hidden++;
                        Check(passage.Annex.IsOptionalAnnex && passage.Annex.Connections.Count == 1, "annex does not gate mandatory progression");
                        Check(!passage.Concealed || !state.CanSeePassage(passage.Origin, passage.Annex), "secret absent from navigation and map");
                        if (passage.Locked)
                        {
                            var key = graph.Rooms.SingleOrDefault(r => r.KeyTokens.Contains(passage.KeyToken));
                            Check(key != null && !key.IsOptionalAnnex && key.Type != RoomEncounterType.Boss, "key outside its lock");
                            var reachable = new System.Collections.Generic.HashSet<int> { graph.StartRoom.RoomId };
                            var queue = new System.Collections.Generic.Queue<DungeonRoom>();
                            // Traverse the base graph directly; all gated passages are optional leaves.
                            queue.Enqueue(graph.StartRoom);
                            while (queue.Count > 0) foreach (var next in graph.Neighbors(queue.Dequeue()))
                                if (!next.IsOptionalAnnex && next.Type != RoomEncounterType.Boss && reachable.Add(next.RoomId)) queue.Enqueue(next);
                            Check(key != null && reachable.Contains(key.RoomId) && reachable.Contains(passage.Origin.RoomId), "key and entrance reachable before boss");
                        }
                    }
                    if (floor == 1) Check(graph.Rooms.Where(r => !r.IsOptionalAnnex && r.Type == RoomEncounterType.Chest).All(r => r.ChestContent != ChestContent.Mimic), "B1 chest eligibility preserved");
                    arrangements.Add(floor + ":" + string.Join(";", graph.Passages.Select(p => p.Origin.Position + "/" + p.Concealed + "/" + p.Locked + "/" + p.Annex.Type)));
                }
                report.AppendLine($"B{floor}: 64 normal seeds, {count} optional annexes, {hidden} concealed entrances, {locks} locks. No production settings altered.");
            }
            report.AppendLine($"Distinct annex arrangements across acts: {arrangements.Count}; failures={failures}.");
            File.WriteAllText(ExplorationPreparation.Folder + "GenerationAudit.txt", report.ToString());
        }
        catch (Exception e) { File.WriteAllText(ExplorationPreparation.Folder + "GenerationAuditError.txt", e.ToString()); }
    }
}
