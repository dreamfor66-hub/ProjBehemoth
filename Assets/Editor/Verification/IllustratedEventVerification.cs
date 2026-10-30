using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class IllustratedEventVerification
{
    static IllustratedEventVerification()=>EditorApplication.update+=Poll;
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||!EditorApplication.isPlaying)return;
        const string request="Temp/IllustratedEvent.request";
        if(!File.Exists(request))return;File.Delete(request);
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="MainGameScene")return;
        if(Object.FindFirstObjectByType<IllustratedEventLiveProbe>()!=null)return;
        EditorApplication.ExecuteMenuItem("Window/General/Game");new GameObject("Scoped illustrated event review").AddComponent<IllustratedEventLiveProbe>().Run();
    }
}
