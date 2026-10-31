using System.IO;
using UnityEditor;
using UnityEngine;

// Explicit scoped opt-in in the already-open Editor. No extra Unity process.
[InitializeOnLoad]
internal static class DirectionalSwingVerification
{
    const string Request="Temp/DirectionalSwing.request";
    const string Folder="Docs/Production/DirectionalSwing-2026-10-28/";
    static double deadline;
    static bool running;
    static DirectionalSwingVerification()=>EditorApplication.update+=Poll;
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(running&&EditorApplication.timeSinceStartup>deadline)
        {running=false;File.AppendAllText(Folder+"Runtime.txt","TIMEOUT: runtime verification remains pending.\n");EditorApplication.isPlaying=false;return;}
        if(SessionState.GetBool("DirectionalSwingPending",false)&&EditorApplication.isPlaying)
        {
            SessionState.SetBool("DirectionalSwingPending",false);running=true;deadline=EditorApplication.timeSinceStartup+100;
            new GameObject("Scoped directional sword input verification").AddComponent<DirectionalSwingLiveProbe>().Run();return;
        }
        if(running&&!EditorApplication.isPlaying){running=false;return;}
        if(!File.Exists(Request)||EditorApplication.isPlayingOrWillChangePlaymode)return;
        try{File.Delete(Request);}catch(IOException){return;}
        Directory.CreateDirectory(Folder);
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.isDirty||!scene.path.EndsWith("MainGameScene.unity"))
        {File.WriteAllText(Folder+"Runtime.txt","PENDING: saved MainGameScene required; designer scene preserved.");return;}
        SessionState.SetBool("DirectionalSwingPending",true);EditorApplication.isPlaying=true;
    }
}
