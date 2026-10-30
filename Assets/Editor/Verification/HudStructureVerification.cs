using System.IO;
using DungeonSlash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
internal static class HudStructureVerification
{
    static bool pending,stopAfter,wasPaused;
    static string mode;
    static HudStructureVerification()=>EditorApplication.update+=Poll;
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        const string request="Temp/HudStructure.request";
        if(File.Exists(request))
        {
            mode=File.ReadAllText(request).Trim();File.Delete(request);
            if(mode.StartsWith("capture:")&&EditorApplication.isPlaying){ScreenCapture.CaptureScreenshot(HudStructurePreparation.Folder+mode.Substring(8)+".png");return;}
            stopAfter=!EditorApplication.isPlaying;wasPaused=EditorApplication.isPaused;
            if(!EditorApplication.isPlaying)
            {
                if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="MainGameScene")EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity");
                var structure=Object.FindFirstObjectByType<HudSceneStructure>();if(structure==null||structure.revision<1)return;
                EditorApplication.isPlaying=true;
            }
            EditorApplication.isPaused=false;pending=true;
        }
        if(!pending||!EditorApplication.isPlaying||Object.FindFirstObjectByType<CombatController>()==null)return;
        pending=false;
        if(mode=="manual")
        {
            new GameObject("Scoped pointer presentation practice").AddComponent<OctoberReviewLiveProbe>().Setup(CharacterKind.Warrior,false,"Assets/Data/Combat/Monsters/Data_Monster_B1_Minion_Skeleton.asset");return;
        }
        new GameObject("Scoped HUD structure verification").AddComponent<HudStructureLiveProbe>().Run();EditorApplication.update+=Finish;
    }
    static void Finish()
    {
        const string log=HudStructurePreparation.Folder+"Live.txt";
        if(!File.Exists(log)||!File.ReadAllText(log).Contains("COMPLETE"))return;
        EditorApplication.update-=Finish;
        if(stopAfter)EditorApplication.isPlaying=false;else EditorApplication.isPaused=wasPaused;
    }
}
