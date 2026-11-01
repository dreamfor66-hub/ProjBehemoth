using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class MerchantApproachVerification
{
    static bool ready;
    static MerchantApproachVerification()
    { EditorApplication.update += Poll; EditorApplication.playModeStateChanged += state => ready = state == PlayModeStateChange.EnteredPlayMode; }
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        const string request = "Temp/MerchantApproach.request";
        if (File.Exists(request))
        {
            string command; try { command = File.ReadAllText(request).Trim(); File.Delete(request); } catch (IOException) { return; }
            Directory.CreateDirectory(MerchantApproachPreparation.Folder);
            if (command == "status") File.WriteAllText(MerchantApproachPreparation.Folder + "Status.txt", "playing=" + EditorApplication.isPlaying);
            if (command == "run" && !EditorApplication.isPlaying && File.Exists(MerchantApproachPreparation.Folder + "Installed.txt"))
            {
                if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
                { File.WriteAllText(MerchantApproachPreparation.Folder + "RuntimePending.txt", "Unsaved scene edits; scoped runtime did not start."); return; }
                EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity"); SessionState.SetBool("MerchantApproach.Run", true); EditorApplication.isPlaying = true;
            }
        }
        if (ready && EditorApplication.isPlaying && SessionState.GetBool("MerchantApproach.Run", false) && Object.FindFirstObjectByType<GameFlowController>() != null)
        { SessionState.SetBool("MerchantApproach.Run", false); new GameObject("Merchant and rear-approach scoped review").AddComponent<MerchantApproachLiveProbe>().Run(); }
    }
}
