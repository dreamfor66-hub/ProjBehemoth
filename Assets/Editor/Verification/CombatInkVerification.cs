#if UNITY_EDITOR
using System.Collections;
using System.IO;
using DungeonSlash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Explicit scoped check in the existing editor. No additional Unity process.
[InitializeOnLoad]
internal static class CombatInkVerification
{
    static bool pending,stopAfter,wasPaused;
    static string previousScene;
    static CombatInkVerification()=>EditorApplication.update+=Poll;
    static void Poll()
    {
        const string request="Temp/CombatInk.request";
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists(request))
        {
            var command=File.ReadAllText(request).Trim();File.Delete(request);
            if(command=="check"||command=="preview")
            {
                EditorApplication.update-=Finish;
                stopAfter=!EditorApplication.isPlaying;wasPaused=EditorApplication.isPaused;
                previousScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
                SessionState.SetString("CombatInkCommand",command);pending=true;
                EditorApplication.isPaused=false;
                if(!EditorApplication.isPlaying)
                {if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="MainGameScene")EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity");EditorApplication.isPlaying=true;}
                else if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="MainGameScene")UnityEngine.SceneManagement.SceneManager.LoadScene("MainGameScene");
            }
        }
        if(!pending||!EditorApplication.isPlaying||Object.FindFirstObjectByType<CombatController>()==null)return;
        pending=false;
        var probe=new GameObject("Scoped combat ink verification").AddComponent<WeakPointVisualLiveProbe>();
        probe.Run(SessionState.GetString("CombatInkCommand","check"));
        if(SessionState.GetString("CombatInkCommand","check")=="check")EditorApplication.update+=Finish;
    }
    static void Finish()
    {
        const string log=WeakPointVisualLiveProbe.Folder+"Live.txt";
        if(!File.Exists(log)||!File.ReadAllText(log).Contains("COMPLETE"))return;
        EditorApplication.update-=Finish;
        if(stopAfter){EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>{if(!EditorApplication.isPlaying&&!string.IsNullOrEmpty(previousScene))EditorSceneManager.OpenScene(previousScene);};}else EditorApplication.isPaused=wasPaused;
    }
}
#endif
