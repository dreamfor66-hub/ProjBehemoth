using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

// One image per authored gameplay state. Each actor installs only after its complete set is reviewed.
[InitializeOnLoad]
internal static class StateArtworkPreparation
{
    internal const string Folder = "Docs/Production/StateArtwork-2026-10-26/";
    [Serializable] internal sealed class Pose { public string state, source, output; }
    [Serializable] internal sealed class Actor { public string id, kind, dataPath, animationPath, idle, idleSource, idleHash; public Pose[] poses; }
    [Serializable] sealed class Manifest { public Actor[] actors; }
    static double next; static bool busy;
    static StateArtworkPreparation() => EditorApplication.update += Poll;
    internal static Actor[] Actors() => JsonUtility.FromJson<Manifest>(File.ReadAllText(Folder + "Manifest.json")).actors;
    static void Poll()
    {
        if (busy || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + 3;
        if (!File.Exists(Folder + "Manifest.json")) return;
        busy = true;
        try
        {
            foreach (var actor in Actors())
            {
                var ready = Folder + "Approved/" + actor.id + ".txt";
                if (!File.Exists(ready)) continue;
                var approvedRevision = File.ReadAllText(ready).Trim();
                var stamp = Folder + "Installed/" + actor.id + ".txt";
                // A completed approval is immutable. Revalidate only a newly approved revision,
                // rather than reading every production PNG on the Editor's main thread each poll.
                if (File.Exists(stamp) && File.ReadAllText(stamp).Trim() == approvedRevision) continue;
                var idleSource = string.IsNullOrEmpty(actor.idleSource) ? actor.idle : actor.idleSource;
                if (!File.Exists(idleSource) || actor.poses.Any(p => !File.Exists(p.source))) continue;
                using var sha = SHA256.Create();
                var revision = string.Join("|", actor.poses.Select(p => BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p.source)))));
                if (approvedRevision != revision) continue;
                var idleHash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(idleSource))).Replace("-", "");
                if (!string.IsNullOrEmpty(actor.idleHash) && idleHash != actor.idleHash)
                    throw new InvalidDataException("Authored Idle changed during state production; review required: " + actor.id);
                Apply(actor);
                Directory.CreateDirectory(Path.GetDirectoryName(stamp)); File.WriteAllText(stamp, revision);
            }
        }
        catch (Exception e) { File.WriteAllText(Folder + "Preparation-Error.txt", e.ToString()); next += 60; }
        finally { busy = false; }
    }
    static void Apply(Actor actor)
    {
        var animation = AssetDatabase.LoadAssetAtPath<CombatSpriteAnimation>(actor.animationPath);
        if (animation == null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(actor.animationPath));
            animation = ScriptableObject.CreateInstance<CombatSpriteAnimation>(); AssetDatabase.CreateAsset(animation, actor.animationPath);
        }
        var idle = string.IsNullOrEmpty(actor.idleSource) ? AssetDatabase.LoadAssetAtPath<Sprite>(actor.idle) : Import(actor.idleSource, actor.idle);
        if (idle == null) throw new InvalidDataException("Missing authored Idle: " + actor.id);
        var loaded = new Dictionary<string, Sprite>();
        foreach (var pose in actor.poses)
        {
            var sprite = Import(pose.source, pose.output);
            if (Mathf.Abs(sprite.rect.width / sprite.rect.height - idle.rect.width / idle.rect.height) > .005f)
                throw new InvalidDataException("State canvas aspect mismatch: " + actor.id + " " + pose.state);
            loaded.Add(pose.state, sprite);
        }
        var expected = actor.kind == "Player" ? 3 : actor.kind == "Monster" ? 4 : 2;
        var required = actor.kind == "Player" ? new[] { "Attack", "Guard" } : actor.kind == "Monster" ? new[] { "Ready", "Attack", "Dead" } : new[] { "Attack" };
        if (loaded.Count != required.Length || required.Any(s => !loaded.ContainsKey(s))) throw new InvalidDataException("Wrong state contract: " + actor.id);
        var states = new[] { idle }.Concat(loaded.Values).ToArray();
        if (states.Length != expected || states.Distinct().Count() != expected) throw new InvalidDataException("Incomplete or duplicated state images: " + actor.id);
        // Commit only after the complete imported set has passed validation.
        animation.idle = idle; animation.attack = loaded["Attack"];
        animation.guard = loaded.TryGetValue("Guard", out var guard) ? guard : null;
        animation.ready = loaded.TryGetValue("Ready", out var ready) ? ready : null;
        animation.dead = loaded.TryGetValue("Dead", out var dead) ? dead : null;
        animation.illustratedLayout = true; animation.illustratedMotion = false;
        EditorUtility.SetDirty(animation);
        if (actor.kind == "Player")
        {
            var data = AssetDatabase.LoadAssetAtPath<PlayerCombatData>(actor.dataPath);
            data.combatAnimation = animation; EditorUtility.SetDirty(data);
        }
        else if (actor.kind == "Monster")
        {
            var data = AssetDatabase.LoadAssetAtPath<MonsterData>(actor.dataPath);
            data.combatAnimation = animation; EditorUtility.SetDirty(data);
        }
        else
        {
            var data = AssetDatabase.LoadAssetAtPath<CompanionData>(actor.dataPath);
            data.combatAnimation = animation; data.combatSprite = idle; EditorUtility.SetDirty(data);
        }
        AssetDatabase.SaveAssets();
        File.AppendAllText(Folder + "Installation-Log.txt", actor.id + ": " + expected + " distinct state images; source Idle/layout retained, mesh-pose substitute disabled.\n");
    }
    static Sprite Import(string source, string output)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(output)); File.Copy(source, output, true);
        AssetDatabase.ImportAsset(output, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(output);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100; importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096; importer.npotScale = TextureImporterNPOTScale.None;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings); importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(output);
    }
}
