using System;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

// Saved sprite contact metadata, prepared once. No Play-mode assembly and no menu action.
[InitializeOnLoad]
internal static class MonsterGroundingPreparation
{
    const string Folder = "Docs/Production/MonsterGrounding-2026-10-27/";
    const string Stamp = "Assets/Data/Presentation/Angular/MonsterGroundingInstalled.txt";
    const string Revision = "body-bottom-and-facing-art-1";
    static readonly string[] Names = { "Skeleton", "Spider" };
    static readonly string[] Libraries = { "StringRevenant", "BoneScorpion" };
    static readonly string[] States = { "Idle", "Ready", "Attack", "Dead" };
    static double next;
    static bool busy;
    static MonsterGroundingPreparation() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (busy || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + 4;
        if (File.Exists(Stamp) && File.ReadAllText(Stamp) == Revision) return;
        if (!File.Exists(Folder + "Ready.txt") || Names.Any(n => States.Any(s => !File.Exists(Folder + "Generated/" + n + "-" + s + ".png")))) return;
        busy = true;
        try
        {
            Directory.CreateDirectory(Folder + "Before/States");
            for (var i = 0; i < Names.Length; i++)
            {
                foreach (var state in States)
                {
                    var output = state == "Idle" ? "Assets/Art/World/Angular/Monsters/" + Libraries[i] + ".png" : "Assets/Art/World/Angular/States/Monsters/" + Libraries[i] + "-" + state + ".png";
                    var before = Folder + "Before/States/" + Names[i] + "-" + state + ".png";
                    if (!File.Exists(before)) File.Copy(output, before);
                    File.Copy(Folder + "Generated/" + Names[i] + "-" + state + ".png", output, true);
                    AssetDatabase.ImportAsset(output, ImportAssetOptions.ForceSynchronousImport);
                }
            }
            // Register existing and newly redrawn species from each Idle alpha silhouette.
            // This avoids using PNG transparent padding as the floor-contact height.
            var animations = AssetDatabase.FindAssets("t:MonsterData", new[] { "Assets/Data" })
                .Select(g => AssetDatabase.LoadAssetAtPath<MonsterData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(m => m != null && m.combatAnimation != null).Select(m => m.combatAnimation).Distinct().ToArray();
            foreach (var animation in animations)
            {
                if (animation.idle == null) continue;
                var path = AssetDatabase.GetAssetPath(animation);
                var backup = Folder + "Before/" + path.Replace('/', '_') + ".txt";
                if (!File.Exists(backup)) File.Copy(path, backup);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(animation.idle)));
                    var pixels = texture.GetPixels32(); var bottom = texture.height; var top = -1;
                    for (var y = 0; y < texture.height; y++)
                        for (var x = 0; x < texture.width; x++)
                            if (pixels[y * texture.width + x].a > 24) { bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y); }
                    if (top < 0) throw new InvalidDataException("Empty sprite: " + path);
                    animation.groundContactY = bottom / (float)texture.height;
                    animation.artworkTopY = (top + 1f) / texture.height;
                    EditorUtility.SetDirty(animation);
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
            AssetDatabase.SaveAssets(); File.WriteAllText(Stamp, Revision); AssetDatabase.ImportAsset(Stamp);
            File.WriteAllText(Folder + "Installed.txt", "Saved Idle contact metadata for " + animations.Length + " monster libraries; eight distinct skeleton/spider state images imported. Grounding uses scaled body hitbox radius. No stat values changed.\n");
        }
        catch (Exception error) { File.WriteAllText(Folder + "Preparation-Error.txt", error.ToString()); next += 30; }
        finally { busy = false; }
    }
}
