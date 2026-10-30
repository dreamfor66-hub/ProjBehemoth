using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class IllustratedOutgameVerification
{
    const string Request="Temp/IllustratedOutgame.request";
    const string Log="Docs/Production/OutgameIllustrated-2026-10-29/Runtime.txt";
    static string ActiveLog=>SessionState.GetString("IllustratedOutgameMode","")=="scale-review"?"Docs/Production/OutgameIllustrated-2026-10-29/SelectionScaleRevision/Runtime.txt":SessionState.GetString("IllustratedOutgameMode","")=="barbarian-grip"?"Docs/Production/OutgameIllustrated-2026-10-29/BarbarianGripRevision/Runtime.txt":SessionState.GetString("IllustratedOutgameMode","")=="artwork"?"Docs/Production/OutgameIllustrated-2026-10-29/CharacterCharmRevision/Runtime.txt":Log;
    static bool running,ownsPlay;static double deadline;
    static IllustratedOutgameVerification()=>EditorApplication.update+=Poll;
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(SessionState.GetBool("IllustratedOutgameFinished",false)){SessionState.SetBool("IllustratedOutgameFinished",false);running=false;ownsPlay=false;return;}
        if(running&&EditorApplication.timeSinceStartup>deadline){running=false;File.AppendAllText(ActiveLog,"TIMEOUT: runtime verification pending.\n");if(ownsPlay)EditorApplication.isPlaying=false;return;}
        if(running&&!EditorApplication.isPlaying){running=false;return;}
        if(SessionState.GetBool("IllustratedOutgamePending",false)&&EditorApplication.isPlaying)
        {
            SessionState.SetBool("IllustratedOutgamePending",false);running=true;ownsPlay=true;deadline=EditorApplication.timeSinceStartup+90;
            EditorApplication.ExecuteMenuItem("Window/General/Game");var probe=new GameObject("Scoped illustrated outgame verification").AddComponent<IllustratedOutgameLiveProbe>();var activeMode=SessionState.GetString("IllustratedOutgameMode","");if(activeMode=="scale-review")probe.RunScaleArtwork(false);else if(activeMode=="barbarian-grip")probe.RunGripArtwork();else if(activeMode=="artwork")probe.RunArtwork();else if(activeMode=="details")probe.RunDetails();else probe.Run();return;
        }
        if(!running&&File.Exists(Request)&&EditorApplication.isPlaying&&File.ReadAllText(Request).Trim()=="scale-review")
        {
            File.Delete(Request);SessionState.SetString("IllustratedOutgameMode","scale-review");
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="OutGameScene")
            {File.WriteAllText(ActiveLog,"PENDING: existing Play scene preserved; six-page artwork capture not run.\n");return;}
            running=true;ownsPlay=false;deadline=EditorApplication.timeSinceStartup+90;
            EditorApplication.ExecuteMenuItem("Window/General/Game");new GameObject("Scoped selection artwork review").AddComponent<IllustratedOutgameLiveProbe>().RunScaleArtwork(true);return;
        }
        if(!File.Exists(Request)||EditorApplication.isPlayingOrWillChangePlaymode)return;
        var mode=File.ReadAllText(Request).Trim();File.Delete(Request);SessionState.SetString("IllustratedOutgameMode",mode);
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.isDirty){File.WriteAllText(ActiveLog,"PENDING: unsaved designer scene preserved.\n");return;}
        SessionState.SetBool("IllustratedOutgamePending",true);EditorApplication.isPlaying=true;
    }
}
