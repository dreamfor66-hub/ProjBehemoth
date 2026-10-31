using System.IO;
using DungeonSlash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
internal static class CombatUxVerification
{
    static bool awaitingStop,awaitingPlay,cardsOnly,eventsOnly;
    static CombatUxVerification()=>EditorApplication.update+=Poll;
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        const string request="Temp/CombatUx.request";
        if(File.Exists(request))
        {
            var scope=File.ReadAllText(request).Trim();cardsOnly=scope=="cards";eventsOnly=scope=="events";File.Delete(request);
            if(Object.FindFirstObjectByType<CombatUxLiveProbe>()!=null)return;
            if(EditorApplication.isPlaying){awaitingStop=true;EditorApplication.isPlaying=false;return;}
            Start();return;
        }
        if(awaitingStop&&!EditorApplication.isPlayingOrWillChangePlaymode){awaitingStop=false;Start();return;}
        if(awaitingPlay&&EditorApplication.isPlaying)
        {
            awaitingPlay=false;EditorApplication.isPaused=false;EditorApplication.ExecuteMenuItem("Window/General/Game");
            var probe=new GameObject("Scoped combat UX verification").AddComponent<CombatUxLiveProbe>();probe.CardsOnly=cardsOnly;probe.EventsOnly=eventsOnly;probe.Run();
        }
    }
    static void Start()
    {
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty){File.WriteAllText(CombatUxPreparation.Folder+"Runtime.txt","PENDING: unsaved designer scene preserved.\n");return;}
        EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity");awaitingPlay=true;EditorApplication.isPlaying=true;
    }
}
