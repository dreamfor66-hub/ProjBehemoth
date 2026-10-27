using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class StateArtworkVerification
{
    const string Folder = "Docs/Production/StateArtwork-2026-10-26/";
    const string Request = "Temp/StateArtwork.request";
    static bool running;
    static double deadline;
    static StateArtworkVerification() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (running && (File.Exists(Folder + "Live-Complete.txt") || EditorApplication.timeSinceStartup > deadline))
        {
            if (!File.Exists(Folder + "Live-Complete.txt")) File.WriteAllText(Folder + "Verification-Pending.txt", "Scoped state verification timed out; inspect live log.");
            running = false; EditorApplication.isPlaying = false; return;
        }
        if (SessionState.GetBool("StateArtworkVerify", false) && EditorApplication.isPlaying)
        {
            SessionState.SetBool("StateArtworkVerify", false); running = true; deadline = EditorApplication.timeSinceStartup + 300;
            if (File.Exists(Folder + "Live-Complete.txt")) File.Delete(Folder + "Live-Complete.txt");
            new GameObject("Scoped regular state-image verification").AddComponent<StateArtworkLiveProbe>().Run(SessionState.GetString("StateArtworkMode", "heroes"), SessionState.GetString("StateArtworkActors", ""));
            return;
        }
        if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        var mode = File.ReadAllText(Request).Trim(); File.Delete(Request);
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.isDirty || !scene.path.EndsWith("MainGameScene.unity"))
        { File.WriteAllText(Folder + "Verification-Pending.txt", "Saved MainGameScene required; existing designer scene was left untouched."); return; }
        var kind = mode == "heroes" ? "Player" : mode == "dog" ? "Companion" : "Monster";
        var actors = StateArtworkPreparation.Actors().Where(a => a.kind == kind && File.Exists(Folder + "Installed/" + a.id + ".txt"))
            .Where(a => !File.Exists(Folder + "Verified/" + a.id + ".txt") || File.ReadAllText(Folder + "Verified/" + a.id + ".txt") != File.ReadAllText(Folder + "Installed/" + a.id + ".txt")).Select(a => a.id).ToArray();
        if (actors.Length == 0) { File.AppendAllText(Folder + "Verification-Requests.txt", mode + ": no newly installed actors pending verification.\n"); return; }
        SessionState.SetString("StateArtworkMode", mode); SessionState.SetString("StateArtworkActors", string.Join(",", actors));
        SessionState.SetBool("StateArtworkVerify", true); EditorApplication.isPlaying = true;
    }
}
