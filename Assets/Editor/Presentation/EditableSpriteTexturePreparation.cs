using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using DungeonSlash;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Explicit migration only. PNG is the editable painting; native Sprite GUIDs remain stable.
[InitializeOnLoad]
internal static class EditableSpriteTexturePreparation
{
    const string Folder = "Docs/Production/EditableSpriteTextures-2026-10-31/";
    [Serializable] sealed class Entry
    {
        public string sprite, guid, source, png;
        public Rect rect, localRect;
        public Vector2 pivot;
        public Vector4 border;
        public float ppu;
        public Vector2[] vertices;
        public ushort[] triangles;
        public int left, bottom, width, height;
    }
    [Serializable] sealed class Plan { public Entry[] entries; public string[] removedSources; public string[] resizedSources; }
    static EditableSpriteTexturePreparation() => EditorApplication.update += Poll;
    static void Poll()
    {
        const string request = "Temp/EditableSpriteTextures.request";
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(request)) return;
        var command = File.ReadAllText(request).Trim(); File.Delete(request);
        Directory.CreateDirectory(Folder);
        try
        {
            if (command == "install") Install(); if (command == "resume") Install(true); if (command == "audit") Audit();
            if (command == "rebind")
            {
                if (File.Exists(Folder + "Complete.txt")) { Audit(); return; }
                var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(Folder + "Plan.json"));
                plan.resizedSources = plan.entries.Select(e => e.source).Distinct().Where(path =>
                {
                    var bytes = File.ReadAllBytes(path); var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    var width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
                    var height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
                    return width != texture.width || height != texture.height;
                }).ToArray();
                File.WriteAllText(Folder + "Plan.json", JsonUtility.ToJson(plan, true));
                foreach (var entry in plan.entries) BindOriginalGeometry(entry);
                Finish(plan);
            }
            if (command == "edit-probe") VerifyPaintingEdits();
        }
        catch (Exception e) { File.WriteAllText(Folder + "Error.txt", e.ToString()); Debug.LogException(e); }
    }
    static Sprite[] NativeSprites() => AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Art", "Assets/Data/Presentation" })
        .Select(AssetDatabase.GUIDToAssetPath).Where(p => p.EndsWith(".asset"))
        .Select(AssetDatabase.LoadMainAssetAtPath).OfType<Sprite>().ToArray();
    static Entry Describe(Sprite sprite)
    {
        var path = AssetDatabase.GetAssetPath(sprite); var r = sprite.rect; var texture = sprite.texture;
        // A one-pixel gutter preserves bilinear sampling at fractional portrait crop boundaries.
        var left = Mathf.Max(0, Mathf.FloorToInt(r.x) - 1); var bottom = Mathf.Max(0, Mathf.FloorToInt(r.y) - 1);
        var right = Mathf.Min(texture.width, Mathf.CeilToInt(r.xMax) + 1); var top = Mathf.Min(texture.height, Mathf.CeilToInt(r.yMax) + 1);
        return new Entry { sprite = path, guid = AssetDatabase.AssetPathToGUID(path), source = AssetDatabase.GetAssetPath(texture),
            png = Path.ChangeExtension(path, ".png").Replace('\\', '/'), rect = r, localRect = new Rect(r.x - left, r.y - bottom, r.width, r.height),
            pivot = sprite.pivot / r.size, border = sprite.border, ppu = sprite.pixelsPerUnit, vertices = sprite.vertices, triangles = sprite.triangles,
            left = left, bottom = bottom, width = right - left, height = top - bottom };
    }
    static Texture2D ReadImported(Texture2D texture)
    {
        var previous = RenderTexture.active; var previousWrite = GL.sRGBWrite;
        var target = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        try
        {
            GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
            Graphics.Blit(texture, target); RenderTexture.active = target;
            var copy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false, false);
            copy.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0); copy.Apply(false, false); return copy;
        }
        finally { RenderTexture.active = previous; GL.sRGBWrite = previousWrite; RenderTexture.ReleaseTemporary(target); }
    }
    static void Install(bool resume = false)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Leave Play mode before texture migration.");
        if (File.Exists(Folder + "Complete.txt")) { Audit(); return; }
        if (File.Exists(Folder + "Plan.json") && !resume) throw new InvalidOperationException("Migration already planned; resume rather than overwriting its baseline.");
        var plan = resume ? JsonUtility.FromJson<Plan>(File.ReadAllText(Folder + "Plan.json")) :
            new Plan { entries = NativeSprites().Select(Describe).Where(e => !File.Exists(e.png)).ToArray(), removedSources = Array.Empty<string>(), resizedSources = Array.Empty<string>() };
        var entries = plan.entries;
        foreach (var entry in entries)
        {
            if (!entry.source.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || !File.Exists(entry.source))
                throw new InvalidDataException("Expected ordinary PNG source: " + entry.source);
            var before = Folder + "Before/" + entry.sprite; Directory.CreateDirectory(Path.GetDirectoryName(before)); if (!File.Exists(before)) File.Copy(entry.sprite, before);
        }
        File.WriteAllText(Folder + "Plan.json", JsonUtility.ToJson(plan, true));
        var resizedSources = new HashSet<string>(plan.resizedSources ?? Array.Empty<string>());
        // Decode source PNG bytes directly. No color grading, resizing, generation or GPU color conversion.
        foreach (var group in entries.GroupBy(e => e.source))
        {
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(source, File.ReadAllBytes(group.Key))) throw new InvalidDataException(group.Key);
                var imported = AssetDatabase.LoadAssetAtPath<Texture2D>(group.Key);
                if (source.width != imported.width || source.height != imported.height)
                {
                    // Legacy training images intentionally render at a smaller import resolution.
                    // Preserve their current rendering and keep their larger original as authoring source.
                    Object.DestroyImmediate(source); source = ReadImported(imported); resizedSources.Add(group.Key);
                }
                var pixels = source.GetPixels32();
                foreach (var entry in group)
                {
                    var output = new Texture2D(entry.width, entry.height, TextureFormat.RGBA32, false);
                    try
                    {
                        var crop = new Color32[entry.width * entry.height];
                        for (var y = 0; y < entry.height; y++) Array.Copy(pixels, (entry.bottom + y) * source.width + entry.left, crop, y * entry.width, entry.width);
                        output.SetPixels32(crop); output.Apply(false, false); File.WriteAllBytes(entry.png, output.EncodeToPNG());
                    }
                    finally { Object.DestroyImmediate(output); }
                }
            }
            finally { Object.DestroyImmediate(source); }
        }
        plan.resizedSources = resizedSources.ToArray();
        File.WriteAllText(Folder + "Plan.json", JsonUtility.ToJson(plan, true));
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (var entry in entries)
        {
            var oldImporter = AssetImporter.GetAtPath(entry.source) as TextureImporter;
            var importer = (TextureImporter)AssetImporter.GetAtPath(entry.png);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = entry.ppu; importer.spriteBorder = entry.border;
            importer.spritePivot = new Vector2((entry.localRect.x + entry.pivot.x * entry.rect.width) / entry.width,
                (entry.localRect.y + entry.pivot.y * entry.rect.height) / entry.height);
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.mipmapEnabled = false; importer.alphaIsTransparency = oldImporter == null || oldImporter.alphaIsTransparency;
            importer.sRGBTexture = oldImporter == null || oldImporter.sRGBTexture;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = Mathf.Max(4096, Mathf.NextPowerOfTwo(Mathf.Max(entry.width, entry.height)));
            importer.filterMode = oldImporter != null ? oldImporter.filterMode : FilterMode.Bilinear;
            importer.wrapMode = oldImporter != null ? oldImporter.wrapMode : TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            BindOriginalGeometry(entry);
        }
        Finish(plan);
    }
    static void BindOriginalGeometry(Entry entry)
    {
        // Sprite.Create/OverrideGeometry rebuilds some native meshes. Preserve the reviewed
        // serialized mesh verbatim and translate only texture pixel coordinates instead.
        var text = File.ReadAllText(Folder + "Before/" + entry.sprite);
        var oldGuid = AssetDatabase.AssetPathToGUID(entry.source); var pngGuid = AssetDatabase.AssetPathToGUID(entry.png);
        if (string.IsNullOrEmpty(pngGuid)) throw new InvalidDataException("Missing editable PNG: " + entry.png);
        text = text.Replace("texture: {fileID: 2800000, guid: " + oldGuid + ",", "texture: {fileID: 2800000, guid: " + pngGuid + ",");
        text = Regex.Replace(text, @"(?m)^(\s*(?:m_Rect|textureRect):\r?\n\s*serializedVersion: 2\r?\n\s*x: )([^\r\n]+)(\r?\n\s*y: )([^\r\n]+)", match =>
            match.Groups[1].Value + (float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) - entry.left).ToString("R", CultureInfo.InvariantCulture) +
            match.Groups[3].Value + (float.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture) - entry.bottom).ToString("R", CultureInfo.InvariantCulture));
        text = Regex.Replace(text, @"uvTransform: \{x: ([^,]+), y: ([^,]+), z: ([^,]+), w: ([^}]+)\}", match =>
            "uvTransform: {x: " + match.Groups[1].Value + ", y: " + (float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) - entry.left).ToString("R", CultureInfo.InvariantCulture) +
            ", z: " + match.Groups[3].Value + ", w: " + (float.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture) - entry.bottom).ToString("R", CultureInfo.InvariantCulture) + "}");
        var temporary = Folder + "Working.asset"; File.WriteAllText(temporary, text); File.Replace(temporary, entry.sprite, null);
        AssetDatabase.ImportAsset(entry.sprite, ImportAssetOptions.ForceSynchronousImport);
    }
    static void Finish(Plan plan)
    {
        var entries = plan.entries;
        var resizedSources = new HashSet<string>(plan.resizedSources ?? Array.Empty<string>());
        AssetDatabase.SaveAssets();
        ValidateGeometry(plan);
        // Remove only former source PNGs which now have no remaining asset references.
        var sources = new HashSet<string>(entries.Select(e => e.source));
        var referenced = new HashSet<string>(AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") && !AssetDatabase.IsValidFolder(p))
            .SelectMany(p => AssetDatabase.GetDependencies(p, false).Where(d => d != p)));
        var removed = sources.Where(p => !referenced.Contains(p) && !resizedSources.Contains(p) && !p.StartsWith("Assets/Art/References/") && !p.Contains("/Sources/")).ToArray();
        var index = JsonUtility.FromJson<DesignerAssetPaths.Index>(File.ReadAllText(DesignerAssetPaths.IndexPath));
        var deleted = new HashSet<string>(removed.Select(AssetDatabase.AssetPathToGUID));
        foreach (var path in removed) if (!AssetDatabase.DeleteAsset(path)) throw new IOException("Failed deleting unused former source " + path);
        foreach (var path in resizedSources.Where(p => !referenced.Contains(p)))
        {
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent + "/Sources")) AssetDatabase.CreateFolder(parent, "Sources");
            var destination = parent + "/Sources/" + Path.GetFileNameWithoutExtension(path) + "Source.png";
            var guid = AssetDatabase.AssetPathToGUID(path); var error = AssetDatabase.MoveAsset(path, destination);
            if (!string.IsNullOrEmpty(error)) throw new IOException(error);
            var row = index.entries.FirstOrDefault(e => e.guid == guid);
            if (row != null) { row.aliases ??= new(); row.aliases.Add(path); row.path = destination; }
        }
        index.entries.RemoveAll(e => deleted.Contains(e.guid)); DesignerAssetPaths.SaveIndex(index);
        plan.removedSources = removed; plan.resizedSources = resizedSources.ToArray(); File.WriteAllText(Folder + "Plan.json", JsonUtility.ToJson(plan, true));
        foreach (var directory in AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/Art/") && AssetDatabase.IsValidFolder(p)).OrderByDescending(p => p.Length).ToArray())
            if (!Directory.EnumerateFileSystemEntries(directory).Any()) AssetDatabase.DeleteAsset(directory);
        Audit();
    }
    static void VerifyPaintingEdits()
    {
        var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(Folder + "Plan.json"));
        var cases = new[] { plan.entries.First(e => e.sprite.Contains("/Portraits/")), plan.entries.First(e => e.sprite.Contains("/NativeMonsterFrames/")) };
        var report = new List<string>();
        foreach (var entry in cases)
        {
            var original = File.ReadAllBytes(entry.png); var painting = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                ImageConversion.LoadImage(painting, original); var colors = painting.GetPixels32();
                var pixel = -1;
                for (var y = Mathf.CeilToInt(entry.localRect.y); y < Mathf.FloorToInt(entry.localRect.yMax) && pixel < 0; y++)
                    for (var x = Mathf.CeilToInt(entry.localRect.x); x < Mathf.FloorToInt(entry.localRect.xMax); x++)
                        if (colors[y * painting.width + x].a > 200) { pixel = y * painting.width + x; break; }
                if (pixel < 0) throw new InvalidDataException("No visible paint: " + entry.png);
                var expected = new Color32(colors[pixel].r == 255 ? (byte)0 : (byte)255, 0, 255, 255);
                colors[pixel] = expected; painting.SetPixels32(colors); painting.Apply(false, false);
                File.WriteAllBytes(entry.png, painting.EncodeToPNG()); AssetDatabase.ImportAsset(entry.png, ImportAssetOptions.ForceSynchronousImport);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(entry.sprite); var readback = ReadImported(sprite.texture);
                try
                {
                    var actual = readback.GetPixels32()[pixel];
                    if (Mathf.Abs(actual.r - expected.r) > 1 || Mathf.Abs(actual.g - expected.g) > 1 || Mathf.Abs(actual.b - expected.b) > 1 || actual.a != expected.a)
                        throw new InvalidDataException("Painting edits did not propagate: " + entry.png);
                }
                finally { Object.DestroyImmediate(readback); }
                report.Add("PASS PNG pixel edit reimports into the existing Sprite: " + entry.png);
            }
            finally
            {
                File.WriteAllBytes(entry.png, original); AssetDatabase.ImportAsset(entry.png, ImportAssetOptions.ForceSynchronousImport); Object.DestroyImmediate(painting);
            }
            if (!File.ReadAllBytes(entry.png).SequenceEqual(original)) throw new InvalidDataException("Original painting was not restored: " + entry.png);
            report.Add("PASS original PNG bytes restored exactly");
        }
        ValidateGeometry(plan); File.WriteAllLines(Folder + "EditProbe.txt", report);
    }
    static void ValidateGeometry(Plan plan)
    {
        foreach (var entry in plan.entries)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(entry.sprite);
            if (sprite == null || AssetDatabase.AssetPathToGUID(entry.sprite) != entry.guid || AssetDatabase.GetAssetPath(sprite.texture) != entry.png ||
                sprite.rect.size != entry.rect.size || Mathf.Abs(sprite.pixelsPerUnit - entry.ppu) > .0001f ||
                Vector2.Distance(sprite.pivot / sprite.rect.size, entry.pivot) > .00001f || sprite.border != entry.border ||
                !sprite.vertices.SequenceEqual(entry.vertices) || !sprite.triangles.SequenceEqual(entry.triangles))
                throw new InvalidDataException("Sprite geometry/reference mismatch: " + entry.sprite);
        }
    }
    static void Audit()
    {
        var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(Folder + "Plan.json")); ValidateGeometry(plan);
        var missing = NativeSprites().Where(s => !File.Exists(Path.ChangeExtension(AssetDatabase.GetAssetPath(s), ".png")) ||
            AssetDatabase.GetAssetPath(s.texture) != Path.ChangeExtension(AssetDatabase.GetAssetPath(s), ".png").Replace('\\', '/')).ToArray();
        File.WriteAllText(Folder + "Audit.txt", $"Created {plan.entries.Length} directly editable PNG textures.\nRemoved {plan.removedSources.Length} unreferenced former source PNGs.\nStandalone native Sprites without their own adjacent PNG: {missing.Length}.\nAll migrated Sprite GUIDs, physical dimensions, pivots, PPU, borders and mesh geometry preserved.\n");
        if (missing.Length != 0) throw new InvalidDataException("Sprite-only resources remain: " + string.Join(", ", missing.Select(s => s.name)));
        File.WriteAllText(Folder + "Complete.txt", "PASS " + plan.entries.Length + " editable textures; zero Sprite-only paintings.\n");
    }
}
