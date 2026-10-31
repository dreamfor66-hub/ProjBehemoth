using System.IO;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
static class HudItemLayoutVerification
{
    static bool pending,running;
    static HudItemLayoutVerification(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        const string request="Temp/HudItems.request";
        if(File.Exists(request))
        {
            var command=File.ReadAllText(request).Trim();File.Delete(request);
            if(command=="verify")
            {
                var quick=Object.FindFirstObjectByType<QuickInventoryView>();
                if(quick==null||quick.layoutRevision<1){Debug.LogError("Saved HUD item layout is not prepared.");return;}
                var completion=HudItemLayoutPreparation.Folder+"Complete.txt";if(File.Exists(completion))File.Delete(completion);
                pending=true;EditorApplication.isPlaying=true;
            }
        }
        if(pending&&EditorApplication.isPlaying&&Object.FindFirstObjectByType<CombatController>()!=null)
        {pending=false;running=true;new GameObject("Scoped HUD item verification").AddComponent<HudItemLayoutLiveProbe>().Run();}
        if(running&&File.Exists(HudItemLayoutPreparation.Folder+"Complete.txt")){running=false;EditorApplication.isPlaying=false;}
    }
}
