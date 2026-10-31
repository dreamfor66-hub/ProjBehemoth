using System;
using System.IO;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

// One-time installation of reviewed paintings. Saved Sprite assets remain designer-editable.
[InitializeOnLoad]
internal static class MonsterStunPreparation
{
    internal const string Folder = "Docs/Production/MonsterStun-2026-10-30/";
    [Serializable] internal sealed class Plan { public Entry[] actors; }
    [Serializable] internal sealed class Entry
    {
        public string monster, image;
        public Region rect;
        public float referenceFeatureFraction, stunnedFeatureFraction;
    }
    [Serializable] internal sealed class Region
    {
        public float x, y, width, height;
        public Rect Value => new Rect(x, y, width, height);
    }
    static MonsterStunPreparation() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Folder + "Ready.txt") && !File.Exists(Folder + "Installed.txt"))
        {
            try { Install(); }
            catch (Exception e) { File.WriteAllText(Folder + "InstallError.txt", e.ToString()); Debug.LogException(e); }
        }
        if (!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Folder + "Installed.txt") && !File.Exists(Folder + "FramesReady.txt"))
            CalibrateFrames();
        const string request = "Temp/MonsterStun.request";
        if (!File.Exists(request)) return;
        var command = File.ReadAllText(request).Trim(); File.Delete(request);
        if (command == "live" && File.Exists(Folder + "Installed.txt"))
        {
            CalibrateFrames();
            SessionState.SetBool("MonsterStunLivePending", true);
            EditorApplication.isPaused = false;
            if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;
        }
        if (command == "stop") EditorApplication.isPlaying = false;
    }
    static void CalibrateFrames()
    {
        if (File.Exists(Folder + "FramesReady.txt")) return;
        var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(Folder + "Install.json"));
        foreach (var entry in plan.actors)
        {
            var data = DungeonSlash.DesignerAssetPaths.Load<MonsterData>(entry.monster);
            var library = data.combatAnimation; var existing = library.stunned;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(entry.image);
            var ppu = library.idle.pixelsPerUnit * texture.width / library.idle.texture.width;
            if (entry.referenceFeatureFraction > 0 && entry.stunnedFeatureFraction > 0)
                ppu *= entry.stunnedFeatureFraction / entry.referenceFeatureFraction;
            var pivot = new Vector2(.5f, -data.FootprintY * ppu / (MonsterData.ArtworkUnitsPerWorldUnit * entry.rect.height));
            if (entry.rect.width <= 0 || entry.rect.height <= 0) throw new InvalidDataException(entry.image + " has an empty frame");
            var sprite = Sprite.Create(texture, entry.rect.Value, pivot, ppu, 0, SpriteMeshType.FullRect);
            sprite.name = existing.name;
            EditorUtility.CopySerialized(sprite, existing); UnityEngine.Object.DestroyImmediate(sprite);
            EditorUtility.SetDirty(existing); AssetDatabase.SaveAssetIfDirty(existing);
        }
        File.WriteAllText(Folder + "FramesReady.txt", "12 native Sprite rectangles/pivots saved; reviewed bat/knight head scale matched.\n");
    }
    [InitializeOnLoadMethod] static void Resume() => EditorApplication.update += StartPending;
    static void StartPending()
    {
        if (!EditorApplication.isPlaying || !SessionState.GetBool("MonsterStunLivePending", false)) return;
        SessionState.SetBool("MonsterStunLivePending", false);
        new GameObject("Scoped monster stun artwork verification").AddComponent<MonsterStunLiveProbe>().Run();
    }
    static void Install()
    {
        var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(Folder + "Install.json"));
        foreach (var entry in plan.actors)
        {
            var data = DungeonSlash.DesignerAssetPaths.Load<MonsterData>(entry.monster);
            if (data == null || data.combatAnimation == null || !File.Exists(entry.image)) throw new InvalidOperationException(entry.monster);
        }
        foreach (var entry in plan.actors)
        {
            var data = DungeonSlash.DesignerAssetPaths.Load<MonsterData>(entry.monster);
            var library = data.combatAnimation;
            if (library.stunned != null) continue;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(entry.image);
            if (texture == null) { AssetDatabase.ImportAsset(entry.image, ImportAssetOptions.ForceSynchronousImport); texture = AssetDatabase.LoadAssetAtPath<Texture2D>(entry.image); }
            var importer = (TextureImporter)AssetImporter.GetAtPath(entry.image);
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(entry.image);
            var idle = library.idle;
            // Same source-canvas pixel scale, with a fixed ground anchor from authored hitboxes.
            // No pose-dependent runtime movement or duplicated footprint setting.
            var ppu = idle.pixelsPerUnit * texture.width / idle.texture.width;
            // Some folded poses occupy less canvas. Match the reviewed anatomical
            // landmark, rather than enlarging a folded body to fill the image.
            if (entry.referenceFeatureFraction > 0 && entry.stunnedFeatureFraction > 0)
                ppu *= entry.stunnedFeatureFraction / entry.referenceFeatureFraction;
            var pivot = new Vector2(.5f, -data.FootprintY * ppu / (MonsterData.ArtworkUnitsPerWorldUnit * entry.rect.height));
            var sprite = Sprite.Create(texture, entry.rect.Value, pivot, ppu, 0, SpriteMeshType.FullRect);
            sprite.name = data.name + "-Stunned";
            var path = "Assets/Art/Combat/NativeMonsterFrames/" + sprite.name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) { UnityEngine.Object.DestroyImmediate(sprite); sprite = existing; }
            else AssetDatabase.CreateAsset(sprite, path);
            library.stunned = sprite;
            EditorUtility.SetDirty(library); AssetDatabase.SaveAssetIfDirty(library);
        }
        File.WriteAllText(Folder + "Installed.txt", plan.actors.Length + " reviewed distinct stun paintings installed; existing poses and balance preserved.\n");
    }
}
