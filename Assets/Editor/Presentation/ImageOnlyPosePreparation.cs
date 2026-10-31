using System;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

// Saved, one-time correction. Subsequent designer edits remain authoritative.
[InitializeOnLoad]
internal static class ImageOnlyPosePreparation
{
    const string Folder = "Docs/Production/ImageOnlyPoses-2026-10-28/";
    const string Images = "Assets/Art/World/Angular/States/Heroes/";
    static readonly string[] Heroes = { "Warrior", "Rogue", "Barbarian", "Samurai", "Monk", "Wizard" };
    static bool busy; static double next;
    static ImageOnlyPosePreparation() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (busy || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode
            || EditorApplication.timeSinceStartup < next || !Directory.Exists(Folder)) return;
        if (File.Exists(Folder + "KeyMotionRemoved.txt") && File.Exists(Folder + "PlayerReadyInstalled.txt")) return;
        next = EditorApplication.timeSinceStartup + 5; busy = true;
        try
        {
            if (!File.Exists(Folder + "KeyMotionRemoved.txt"))
            {
                foreach (var guid in AssetDatabase.FindAssets("t:CombatSpriteAnimation"))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid); var animation = AssetDatabase.LoadAssetAtPath<CombatSpriteAnimation>(path);
                    Backup(path);
                    animation.illustratedLayout |= animation.illustratedMotion;
                    animation.illustratedMotion = animation.emphasizePoses = false;
                    animation.readyTilt = animation.attackTilt = 0;
                    animation.readyOffset = animation.attackOffset = Vector2.zero;
                    animation.readyScale = animation.strikeScale = Vector2.one;
                    Save(animation);
                }
                File.WriteAllText(Folder + "KeyMotionRemoved.txt", "Image swaps only. Static transparent-canvas compensation retained; no state/time-dependent mesh transforms.\n");
            }
            if (!File.Exists(Folder + "PlayerReadyInstalled.txt") && Heroes.All(id => File.Exists(Images + id + "-Ready.png")))
            {
                foreach (var id in Heroes)
                {
                    var path = Images + id + "-Ready.png"; AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 100; importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
                    importer.filterMode = FilterMode.Bilinear; importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.maxTextureSize = 4096; importer.npotScale = TextureImporterNPOTScale.None;
                    var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings); importer.SaveAndReimport();
                    var sprite = DungeonSlash.DesignerAssetPaths.Load<Sprite>(path);
                    if (sprite == null) throw new InvalidDataException("Missing Ready sprite: " + id);
                    var animation = AssetDatabase.LoadAssetAtPath<CombatSpriteAnimation>("Assets/Data/Presentation/Angular/Animations/Hero-" + id + ".asset");
                    animation.ready = sprite; Save(animation);
                }
                File.WriteAllText(Folder + "PlayerReadyInstalled.txt", "Six distinct authored Ready paintings installed. No empty pose images.\n");
            }
        }
        catch (Exception error) { next = EditorApplication.timeSinceStartup + 60; File.WriteAllText(Folder + "Preparation-Error.txt", error.ToString()); Debug.LogException(error); }
        finally { busy = false; }
    }
    static void Save(UnityEngine.Object item) { EditorUtility.SetDirty(item); AssetDatabase.SaveAssetIfDirty(item); }
    static void Backup(string path)
    {
        var target = Folder + "Before/" + path;
        if (File.Exists(target)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(target)); File.Copy(path, target);
    }
}
