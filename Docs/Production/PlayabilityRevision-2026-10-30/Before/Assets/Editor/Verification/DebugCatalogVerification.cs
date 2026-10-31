using System.IO;
using UnityEditor;
using UnityEngine;

// Explicit local F1-only check. Never launches another Editor or a balance suite.
[InitializeOnLoad]
internal static class DebugCatalogVerification
{
    static DebugCatalogVerification() => EditorApplication.update += Poll;
    static void Poll()
    {
        const string request = "Temp/DebugCatalog.request";
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(request)) return;
        string command;
        try { command = File.ReadAllText(request).Trim(); }
        catch (IOException) { return; } // The external request writer may still be closing its file.
        if (command == "play")
        {
            try { File.Delete(request); } catch (IOException) { return; }
            if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;
            return;
        }
        if (!EditorApplication.isPlaying) return;
        try { File.Delete(request); } catch (IOException) { return; }
        new GameObject("Scoped F1 catalog verification").AddComponent<DungeonSlash.DebugCatalogLiveProbe>();
    }
}
