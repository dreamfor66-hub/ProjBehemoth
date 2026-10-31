using System.IO;
using DungeonSlash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class DebugChapterVerification
{
    const string Pending = "Behemoth.DebugChapter.Pending";
    static double enter = double.PositiveInfinity;
    static bool ready;
    static DebugChapterVerification()
    {
        EditorApplication.update += Poll;
        EditorApplication.playModeStateChanged += state => ready = state == PlayModeStateChange.EnteredPlayMode;
    }
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        const string request = "Temp/DebugChapter.request";
        if (File.Exists(request))
        {
            var command = File.ReadAllText(request).Trim(); File.Delete(request);
            if ((command == "main" || command == "trial") && SceneManager.GetActiveScene().isDirty)
            { File.WriteAllText(DebugChapterPreparation.Folder + "ScenePending.txt", "Scene switch skipped: unsaved designer scene.\n"); return; }
            if (command == "main" && !EditorApplication.isPlaying) EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity");
            if ((command == "run" || command == "trial") && File.Exists(DebugChapterPreparation.Folder + "Installed.txt"))
            {
                if (command == "trial" && !EditorApplication.isPlaying) EditorSceneManager.OpenScene("Assets/Scenes/Trials/Angular01_Main.unity");
                SessionState.SetBool(Pending + ".Smoke", command == "trial");
                SessionState.SetBool(Pending, true); enter = EditorApplication.timeSinceStartup + .5;
            }
            if (command == "status") File.WriteAllText(DebugChapterPreparation.Folder + "EditorStatus.txt", "playing=" + EditorApplication.isPlaying + "; flow=" + Object.FindFirstObjectByType<GameFlowController>()?.State + "; pending=" + SessionState.GetBool(Pending, false));
        }
        if (SessionState.GetBool(Pending, false) && EditorApplication.timeSinceStartup >= enter)
        { enter = double.PositiveInfinity; EditorApplication.isPlaying = true; EditorApplication.isPaused = false; }
        if (SessionState.GetBool(Pending, false) && ready && EditorApplication.isPlaying && Object.FindFirstObjectByType<GameFlowController>() != null)
        { SessionState.SetBool(Pending, false); var probe = new GameObject("Scoped F1 chapter verification").AddComponent<DebugChapterLiveProbe>(); probe.smoke = SessionState.GetBool(Pending + ".Smoke", false); probe.Run(); }
    }
}
