using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class AngularWorldVerification
{
    static AngularWorldVerification(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        const string path="Temp/AngularWorld.request";
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(path))return;
        var command=File.ReadAllText(path).Trim();File.Delete(path);
        if(command=="refresh"){AssetDatabase.Refresh();return;}
        if(command=="status"){File.WriteAllText(AngularWorldPreparation.Report+"Status.txt","playing="+EditorApplication.isPlaying+"\nscene="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);return;}
        if(!EditorApplication.isPlaying)return;
        if(command=="combat-framing")
        {
            var review=Object.FindFirstObjectByType<CombatFramingLiveProbe>()??new GameObject("Combat depth and HUD portrait review").AddComponent<CombatFramingLiveProbe>();review.Run();return;
        }
        if(command=="restoration")
        {
            var review=Object.FindFirstObjectByType<AngularRestorationLiveProbe>()??new GameObject("Restored original composition review").AddComponent<AngularRestorationLiveProbe>();review.Run();return;
        }
        var probe=Object.FindFirstObjectByType<AngularWorldLiveProbe>()??new GameObject("Angular world scoped verification").AddComponent<AngularWorldLiveProbe>();
        probe.Run(command);
    }
}
