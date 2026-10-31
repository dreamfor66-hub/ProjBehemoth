using System.IO;
using UnityEditor;
using UnityEngine;

// Explicit opt-in only, in the existing Editor. Never launches another Unity instance.
[InitializeOnLoad]
internal static class CombatFocusVerification
{
    const string Request = "Temp/CombatFocus.request";
    static CombatFocusVerification() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (File.Exists(Request))
        {
            string command;
            try { command = File.ReadAllText(Request).Trim(); File.Delete(Request); }
            catch (IOException) { return; }
            if (command == "run" || command == "contacts" || command == "monsters" || command == "backgrounds" || command == "announcement" || command == "grounding" || command == "spawn-height")
            {
                SessionState.SetBool("ManualBackgroundStopAfter", !EditorApplication.isPlaying);
                SessionState.SetBool("ManualBackgroundWasPaused", EditorApplication.isPaused);
                SessionState.SetString("CombatFocusMode", command);
                SessionState.SetBool("CombatFocusPending", true);
                EditorApplication.isPaused = false;
                if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;
            }
        }
        if (!EditorApplication.isPlaying || !SessionState.GetBool("CombatFocusPending", false)) return;
        SessionState.SetBool("CombatFocusPending", false);
        if (SessionState.GetString("CombatFocusMode", "run") == "spawn-height")
        {
            new GameObject("Scoped monster spawn height verification").AddComponent<MonsterSpawnHeightLiveProbe>().Run();
            return;
        }
        if (SessionState.GetString("CombatFocusMode", "run") == "grounding")
        {
            new GameObject("Scoped monster grounding verification").AddComponent<MonsterGroundingLiveProbe>().Run();
            return;
        }
        if (SessionState.GetString("CombatFocusMode", "run") == "announcement")
        {
            new GameObject("Scoped announcement framing verification").AddComponent<EncounterFramingLiveProbe>().Run();
            return;
        }
        if (SessionState.GetString("CombatFocusMode", "run") == "backgrounds")
        {
            foreach (var previous in Object.FindObjectsByType<ManualBackgroundLiveProbe>(FindObjectsSortMode.None)) Object.DestroyImmediate(previous.gameObject);
            new GameObject("Scoped manual background verification").AddComponent<ManualBackgroundLiveProbe>().Run(
                SessionState.GetBool("ManualBackgroundStopAfter", true), SessionState.GetBool("ManualBackgroundWasPaused", false));
            return;
        }
        var probe = new GameObject("Scoped combat framing verification").AddComponent<CombatFocusLiveProbe>();
        if (SessionState.GetString("CombatFocusMode", "run") == "monsters")
        {
            Object.DestroyImmediate(probe.gameObject);
            new GameObject("Scoped monster placement verification").AddComponent<MonsterPlacementLiveProbe>().Run();
        }
        else if (SessionState.GetString("CombatFocusMode", "run") == "contacts") probe.RunContacts();
        else probe.Run();
    }
}
