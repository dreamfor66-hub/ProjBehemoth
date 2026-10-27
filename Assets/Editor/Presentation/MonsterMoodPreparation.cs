using System;
using System.IO;
using UnityEditor;

// Imports only the four reviewed sprite replacements; keeps authored data and importer GUIDs.
[InitializeOnLoad]
internal static class MonsterMoodPreparation
{
    const string Folder = "Docs/Production/MonsterMood-2026-10-26/";
    const string Stamp = "Assets/Data/Presentation/Angular/MonsterMoodInstalled.txt";
    static readonly string[] Ids = { "MirrorShell", "CoffinSlime", "BoneScorpion", "StringRevenant" };
    static bool busy;
    static double next;
    static MonsterMoodPreparation() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (busy || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + 3;
        if (File.Exists(Stamp) || !File.Exists(Folder + "Ready.txt")) return;
        foreach (var id in Ids) if (!File.Exists(Folder + "Generated/" + id + ".png")) return;
        busy = true;
        try
        {
            foreach (var id in Ids)
            {
                var path = "Assets/Art/World/Angular/Monsters/" + id + ".png";
                File.Copy(Folder + "Generated/" + id + ".png", path, true);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            File.WriteAllText(Folder + "Installed.txt", "Four reviewed mood redraws installed in place. Existing sprite GUIDs, texture importer settings, animation layout, combat data, names and encounter composition retained.\n");
            File.WriteAllText(Stamp, "four-monster-mood-1"); AssetDatabase.ImportAsset(Stamp);
        }
        catch (Exception e) { File.WriteAllText(Folder + "Preparation-Error.txt", e.ToString()); next += 60; }
        finally { busy = false; }
    }
}
