using System;
using System.IO;
using DungeonSlash;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

[InitializeOnLoad]
internal static class PlayabilityRevisionVerification
{
    static bool waiting,started,stopAfter;
    static string mode;
    static PlayabilityRevisionVerification()=>EditorApplication.update+=Poll;
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        const string request="Temp/PlayabilityRevision.request";
        if(File.Exists(request))
        {
            mode=File.ReadAllText(request).Trim();File.Delete(request);
            if(mode=="browser"){DesignerDataBrowser.Open();return;}
            if(mode=="stop"){EditorApplication.isPlaying=false;waiting=started=false;return;}
            if(mode.StartsWith("capture:")&&EditorApplication.isPlaying){ScreenCapture.CaptureScreenshot(PlayabilityRevisionPreparation.Folder+mode.Substring(8)+".png");return;}
            if(!File.Exists(PlayabilityRevisionPreparation.Folder+"Installed.txt"))return;
            stopAfter=!EditorApplication.isPlaying;waiting=true;started=false;
            if(!EditorApplication.isPlaying)EditorApplication.isPlaying=true;
            EditorApplication.isPaused=false;
        }
        if(waiting&&!started&&EditorApplication.isPlaying&&Object.FindFirstObjectByType<CombatController>()!=null)
        {
            waiting=false;started=true;
            try
            {
                var probe=new GameObject("Scoped playability verification").AddComponent<PlayabilityRevisionLiveProbe>();
                if(mode.StartsWith("show:"))probe.Show(mode.Substring(5));else if(mode=="result")probe.RunResult();else if(mode=="visuals")probe.RunVisuals();else probe.Run();
            }
            catch(Exception e){File.WriteAllText(PlayabilityRevisionPreparation.Folder+"VerificationError.txt",e.ToString());Debug.LogException(e);}
        }
        var complete=mode=="result"?"ResultComplete.txt":mode=="visuals"?"VisualsComplete.txt":"LiveComplete.txt";
        if(started&&!mode.StartsWith("show:")&&File.Exists(PlayabilityRevisionPreparation.Folder+complete))
        {started=false;if(stopAfter)EditorApplication.isPlaying=false;}
    }
}
