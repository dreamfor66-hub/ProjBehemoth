using System.IO;
using DungeonSlash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
internal static class WorldRenewalVerification
{
    static WorldRenewalVerification()
    {
        EditorApplication.update += Poll;
    }
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (!EditorApplication.isPlayingOrWillChangePlaymode && SessionState.GetBool("WorldRenewal.ReturnMain",false))
        {
            SessionState.SetBool("WorldRenewal.ReturnMain",false);
            EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity");
            return;
        }
        const string request = "Temp/WorldRenewal.review";
        if (File.Exists(request) && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            var command = File.ReadAllText(request).Trim(); File.Delete(request);
            if (command != "run" && command != "outgame" && command != "combat") return;
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.isDirty){File.WriteAllText(WorldRenewalPreparation.Folder+"RuntimePending.txt","Unsaved scene edits preserved; review not started.");return;}
            SessionState.SetString("WorldRenewal.ReviewMode",command);
            if(command=="outgame")
            {EditorSceneManager.OpenScene("Assets/Scenes/OutGameScene.unity");SessionState.SetBool("WorldRenewal.ReturnMain",true);}
            else if(scene.path!="Assets/Scenes/MainGameScene.unity")EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity");
            SessionState.SetBool("WorldRenewal.Review", true); EditorApplication.isPlaying = true;
        }
        var outgame=SessionState.GetString("WorldRenewal.ReviewMode","")=="outgame";
        if (EditorApplication.isPlaying && SessionState.GetBool("WorldRenewal.Review", false)
            &&(outgame?Object.FindFirstObjectByType<OutGameMenuView>()!=null:Object.FindFirstObjectByType<GameFlowController>()!=null))
        {
            SessionState.SetBool("WorldRenewal.Review", false);
            var probe=new GameObject("Scoped world renewal review").AddComponent<ExplorationDepthLiveProbe>();
            if(outgame)probe.RunOutgameRenewal();else probe.RunWorldRenewal(SessionState.GetString("WorldRenewal.ReviewMode","")=="combat");
        }
    }
}
