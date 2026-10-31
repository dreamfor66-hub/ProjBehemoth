using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Request-driven, GUID-preserving maintenance. It never removes assets on project startup.
[InitializeOnLoad]
internal static class ArtResourceMaintenance
{
    internal const string Folder = "Docs/Production/ArtResourceRules-2026-10-31/";
    [Serializable] internal sealed class Row
    {
        public string path, guid, type;
        public string[] dependencies;
        public SpriteRow[] sprites;
    }
    [Serializable] internal sealed class SpriteRow
    {
        public long fileId;
        public string texture;
        public Rect rect;
        public Vector2 pivot;
        public Vector4 border;
        public float ppu;
    }
    [Serializable] internal sealed class Inventory { public Row[] rows; public bool playing; }
    [Serializable] internal sealed class Move { public string oldPath, path, guid; }
    [Serializable] internal sealed class Plan { public Move[] moves; public string[] delete; }
    static bool waitingSmoke, runningSmoke, stopAfterSmoke;
    static ArtResourceMaintenance() => EditorApplication.update += Poll;
    static void Poll()
    {
        const string request = "Temp/ArtResourceMaintenance.request";
        if (waitingSmoke && EditorApplication.isPlaying && Object.FindFirstObjectByType<CombatController>() != null)
        {
            waitingSmoke = false; runningSmoke = true;
            new GameObject("Art resource scoped verification").AddComponent<ArtResourceNamingLiveProbe>().Run();
        }
        if (runningSmoke && File.Exists(Folder + "LiveComplete.txt"))
        { runningSmoke = false; if (stopAfterSmoke) EditorApplication.isPlaying = false; }
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(request)) return;
        var command = File.ReadAllText(request).Trim(); File.Delete(request);
        Directory.CreateDirectory(Folder);
        try
        {
            if (command == "inventory") Export("Inventory.json");
            if (command == "apply") Apply();
            if (command == "canonical-placeholder") ReplaceLegacyPlaceholder();
            if (command == "cleanup") Apply(Folder + "Correction/");
            if (command == "smoke")
            {
                if (File.Exists(Folder + "LiveComplete.txt")) File.Delete(Folder + "LiveComplete.txt");
                stopAfterSmoke = !EditorApplication.isPlaying; waitingSmoke = true;
                EditorApplication.isPlaying = true; EditorApplication.isPaused = false;
            }
        }
        catch (Exception e) { File.WriteAllText(Folder + "Error.txt", e.ToString()); Debug.LogException(e); }
    }
    static Inventory Snapshot()
    {
        var paths = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") && !AssetDatabase.IsValidFolder(p)).ToArray();
        return new Inventory { playing = EditorApplication.isPlaying, rows = paths.Select(path => new Row {
            path = path, guid = AssetDatabase.AssetPathToGUID(path), type = AssetDatabase.GetMainAssetTypeAtPath(path)?.Name ?? "",
            dependencies = AssetDatabase.GetDependencies(path, false).Where(p => p != path).Select(AssetDatabase.AssetPathToGUID).Where(g => !string.IsNullOrEmpty(g)).Distinct().OrderBy(g => g).ToArray(),
            sprites = path.StartsWith("Assets/Art/") ? AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Select(s => {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(s, out string _, out long id);
                return new SpriteRow { fileId = id, texture = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(s.texture)), rect = s.rect, pivot = s.pivot, ppu = s.pixelsPerUnit, border = s.border };
            }).OrderBy(s => s.fileId).ToArray() : Array.Empty<SpriteRow>()
        }).ToArray() };
    }
    static void Export(string name) => File.WriteAllText(Folder + name, JsonUtility.ToJson(Snapshot(), true));
    static void ReplaceLegacyPlaceholder()
    {
        const string path = "Assets/Prefabs/Combat/MonsterView.prefab";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Combat/NativeMonsterFrames/T_Sprite_Monster_해골전사_Idle.asset");
        if (sprite == null) throw new InvalidDataException("Missing current skeleton Idle sprite.");
        var root = PrefabUtility.LoadPrefabContents(path);
        var count = 0;
        try
        {
            foreach (var component in root.GetComponentsInChildren<Component>(true).Where(c => c != null))
            {
                var serialized = new SerializedObject(component); var property = serialized.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference || !(property.objectReferenceValue is Sprite old)) continue;
                    if (!AssetDatabase.GetAssetPath(old).StartsWith("Assets/Art/Enemies/Combat/")) continue;
                    property.objectReferenceValue = sprite; count++;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        Directory.CreateDirectory(Folder + "Correction/");
        File.WriteAllText(Folder + "Correction/Placeholder.txt", count + " obsolete prefab preview sprite references replaced with the current skeleton Idle painting.\n");
        Export("Inventory.json");
    }
    static void EnsureArtFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        if (!path.StartsWith("Assets/Art/")) throw new InvalidDataException("Invalid destination folder: " + path);
        var parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureArtFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
    static void Apply(string folder = Folder)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Leave Play mode before asset maintenance.");
        Directory.CreateDirectory(folder);
        var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(folder + "Plan.json"));
        var before = Snapshot();
        File.WriteAllText(folder + "Before.json", JsonUtility.ToJson(before, true));
        var deletion = new HashSet<string>(plan.delete, StringComparer.Ordinal);
        var deleteGuids = new HashSet<string>(before.rows.Where(r => deletion.Contains(r.path)).Select(r => r.guid));
        var dangling = before.rows.Where(r => !deletion.Contains(r.path) && r.dependencies.Any(deleteGuids.Contains)).ToArray();
        if (dangling.Length != 0) throw new InvalidDataException("Deletion still referenced: " + string.Join(", ", dangling.Select(r => r.path)));
        foreach (var path in plan.delete)
            if (!(path.StartsWith("Assets/Art/", StringComparison.Ordinal) ||
                  folder == Folder + "Correction/" && path.StartsWith("Assets/Data/Presentation/", StringComparison.Ordinal) && path.EndsWith(".asset")) ||
                AssetDatabase.IsValidFolder(path) || !File.Exists(path))
                throw new InvalidDataException("Invalid deletion: " + path);
        var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var move in plan.moves)
        {
            if (!move.oldPath.StartsWith("Assets/Art/") || !move.path.StartsWith("Assets/Art/") ||
                AssetDatabase.AssetPathToGUID(move.oldPath) != move.guid || File.Exists(move.path) || !targets.Add(move.path))
                throw new InvalidDataException("Invalid move: " + move.oldPath + " -> " + move.path);
        }
        var index = File.Exists(DesignerAssetPaths.IndexPath) ? JsonUtility.FromJson<DesignerAssetPaths.Index>(File.ReadAllText(DesignerAssetPaths.IndexPath)) : new DesignerAssetPaths.Index();
        // Folder imports must finish before pausing imports for the batch move.
        foreach (var move in plan.moves) EnsureArtFolder(Path.GetDirectoryName(move.path).Replace('\\', '/'));
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var move in plan.moves)
            {
                var error = AssetDatabase.MoveAsset(move.oldPath, move.path);
                if (!string.IsNullOrEmpty(error)) throw new IOException(error);
                var row = index.entries.FirstOrDefault(e => e.guid == move.guid);
                if (row == null) { row = new DesignerAssetPaths.Entry { oldPath = move.oldPath, guid = move.guid, type = "Art" }; index.entries.Add(row); }
                row.aliases ??= new List<string>();
                if (!row.aliases.Contains(move.oldPath)) row.aliases.Add(move.oldPath);
                row.path = move.path; row.displayName = Path.GetFileNameWithoutExtension(move.path);
            }
            foreach (var path in plan.delete)
                if (!AssetDatabase.DeleteAsset(path)) throw new IOException("Could not delete " + path);
        }
        finally { AssetDatabase.StopAssetEditing(); }
        foreach (var move in plan.moves)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(move.path);
            if (move.path.EndsWith(".asset") && (asset is Sprite || asset is Texture2D))
            { asset.name = Path.GetFileNameWithoutExtension(move.path); EditorUtility.SetDirty(asset); }
        }
        index.entries.RemoveAll(e => deleteGuids.Contains(e.guid));
        DesignerAssetPaths.SaveIndex(index);
        AssetDatabase.SaveAssets();
        // Delete only verified empty Art directories, deepest first.
        foreach (var directory in AssetDatabase.GetAllAssetPaths().Where(p => (p.StartsWith("Assets/Art/") ||
                     folder == Folder + "Correction/" && p.StartsWith("Assets/Data/Presentation/")) && AssetDatabase.IsValidFolder(p)).OrderByDescending(p => p.Length).ToArray())
            if (!Directory.EnumerateFileSystemEntries(directory).Any()) AssetDatabase.DeleteAsset(directory);
        AssetDatabase.Refresh();
        var after = Snapshot();
        File.WriteAllText(folder + "After.json", JsonUtility.ToJson(after, true));
        var current = after.rows.ToDictionary(r => r.guid);
        var errors = new List<string>();
        foreach (var row in before.rows.Where(r => !deleteGuids.Contains(r.guid)))
        {
            if (!current.TryGetValue(row.guid, out var updated)) { errors.Add("Missing GUID: " + row.path); continue; }
            if (!row.dependencies.SequenceEqual(updated.dependencies)) errors.Add("Changed dependencies: " + row.path);
            if (JsonUtility.ToJson(new SpriteSet { sprites = row.sprites }) != JsonUtility.ToJson(new SpriteSet { sprites = updated.sprites })) errors.Add("Changed sprite geometry or file IDs: " + row.path);
        }
        foreach (var move in plan.moves)
            if (AssetDatabase.AssetPathToGUID(move.path) != move.guid) errors.Add("Moved GUID mismatch: " + move.path);
        if (errors.Count != 0) throw new InvalidDataException(string.Join("\n", errors));
        File.WriteAllText(folder + "Complete.txt", $"Moved {plan.moves.Length} active art assets. Deleted {plan.delete.Length} unused art/presentation assets.\nAll retained asset GUIDs, direct dependencies, sprite file IDs, textures, rects, pivots, borders and PPU preserved.\n");
    }
    [Serializable] sealed class SpriteSet { public SpriteRow[] sprites; }
}
