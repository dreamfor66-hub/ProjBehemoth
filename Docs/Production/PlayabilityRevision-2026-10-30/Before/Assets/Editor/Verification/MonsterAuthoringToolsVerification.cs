using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonSlash;
using DungeonSlash.EditorTools;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Explicit requests only; never rewrites designer hitboxes on project open.
[InitializeOnLoad]
internal static class MonsterAuthoringToolsVerification
{
    internal const string Folder = "Docs/Production/MonsterAuthoringTools-2026-10-30/";
    static int checks, failures;
    static MonsterAuthoringToolsVerification() => EditorApplication.update += Poll;
    static void Poll()
    {
        const string request = "Temp/MonsterAuthoringTools.request";
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(request)) return;
        string command; try { command = File.ReadAllText(request).Trim(); File.Delete(request); } catch (IOException) { return; }
        Directory.CreateDirectory(Folder);
        try
        {
            if (command == "deduplicate") Deduplicate();
            if (command == "checks") Checks();
        }
        catch (Exception e) { File.WriteAllText(Folder + "Error.txt", e.ToString()); Debug.LogException(e); }
    }
    static IEnumerable<MonsterData> Monsters() => AssetDatabase.FindAssets("t:MonsterData", new[] { "Assets/Data/Combat/Monsters" })
        .Select(g => AssetDatabase.LoadAssetAtPath<MonsterData>(AssetDatabase.GUIDToAssetPath(g)));
    static bool Same(MonsterHitBox a, MonsterHitBox b) => a != null && b != null && a.center.Equals(b.center) && a.radius.Equals(b.radius);
    static void Deduplicate()
    {
        var report = new List<string>(); var removed = 0; var changed = 0;
        Undo.IncrementCurrentGroup(); var group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Remove identical monster hitboxes");
        foreach (var data in Monsters())
        {
            var unique = new List<MonsterHitBox>();
            foreach (var box in data.hitBoxes) if (box == null || !unique.Any(other => Same(box, other))) unique.Add(box);
            if (unique.Count == data.hitBoxes.Count) continue;
            var originalCount = data.hitBoxes.Count; var originalBounds = data.HitBoxBounds;
            if (!data.hitBoxes.All(box => unique.Any(other => Same(box, other)))) throw new InvalidOperationException("Collision union would change: " + data.name);
            var path = AssetDatabase.GetAssetPath(data); var backup = Folder + "Before/" + path;
            Directory.CreateDirectory(Path.GetDirectoryName(backup)); if (!File.Exists(backup)) File.Copy(path, backup);
            Undo.RegisterCompleteObjectUndo(data, "Remove identical monster hitboxes"); data.hitBoxes = unique;
            if (data.HitBoxBounds != originalBounds) throw new InvalidOperationException("Bounds changed: " + data.name);
            EditorUtility.SetDirty(data); AssetDatabase.SaveAssetIfDirty(data);
            removed += originalCount - unique.Count; changed++;
            report.Add($"PASS {path}: {originalCount} -> {unique.Count}; exact circle union / bounds / footprint unchanged");
        }
        Undo.CollapseUndoOperations(group);
        report.Add($"RESULT {changed} monsters, {removed} exact duplicate circles removed; 0 collision changes.");
        File.WriteAllLines(Folder + "Deduplication.txt", report);
    }
    static void Check(bool ok, string message)
    { checks++; if (!ok) failures++; File.AppendAllText(Folder + "Checks.txt", (ok ? "PASS " : "FAIL ") + message + "\n"); }
    static void Checks()
    {
        checks = failures = 0; File.WriteAllText(Folder + "Checks.txt", "Scoped editor preview / clipboard checks. Existing assets are read only.\n");
        foreach (var data in Monsters())
        {
            var before = EditorJsonUtility.ToJson(data); var attack = data.GetAttack(MonsterAttackType.Charge);
            using var preview = new MonsterWeakPointPreview();
            preview.Refresh(data, attack, CombatVisualState.Ready); var centers = preview.Runtime.WeakPoints.Select(p => p.HitShape.Center).ToArray();
            preview.Refresh(data, attack, CombatVisualState.Idle);
            var library = data.combatAnimation;
            var idle = library != null && library.idle != null ? library.idle : data.portrait;
            Check(preview.Artwork.sprite == idle, data.name + " actual Idle painting");
            Check(centers.SequenceEqual(preview.Runtime.WeakPoints.Select(p => p.HitShape.Center)), data.name + " pose toggle retains authored weak-point coordinates");
            Check(before == EditorJsonUtility.ToJson(data), data.name + " preview never changes authored monster data");
            Check(!data.hitBoxes.Where(b => b != null).Select((b, i) => data.hitBoxes.Take(i).Any(a => Same(a, b))).Any(b => b), data.name + " no exact duplicate hitboxes");
        }
        var previousClipboard = EditorGUIUtility.systemCopyBuffer;
        var source = ScriptableObject.CreateInstance<MonsterAttackData>(); var destination = ScriptableObject.CreateInstance<MonsterAttackData>();
        try
        {
            source.chargeWeakPoints = new() { new() { id = "head", localPosition = new Vector2(-31, 120), hitRadius = 23, requiredChargeHits = 3, optional = true }, new() { id = "leg", localPosition = new Vector2(60, -80) } };
            destination.chargeWeakPoints = new() { new() { id = "replace-me" } };
            MonsterWeakPointPatternClipboard.Copy(source);
            Check(MonsterWeakPointPatternClipboard.Paste(destination) && JsonUtility.ToJson(source) == JsonUtility.ToJson(destination), "pattern replaces full list and preserves coordinates / radius / hits / optional / IDs");
            destination.chargeWeakPoints[0].localPosition = Vector2.zero;
            Check(source.chargeWeakPoints[0].localPosition == new Vector2(-31, 120), "pasted points are independent copies");
            EditorGUIUtility.systemCopyBuffer = "ordinary text";
            var before = JsonUtility.ToJson(destination);
            Check(!MonsterWeakPointPatternClipboard.Paste(destination) && before == JsonUtility.ToJson(destination), "unrelated clipboard content cannot overwrite pattern");
            source.chargeWeakPoints.Clear(); MonsterWeakPointPatternClipboard.Copy(source);
            Check(MonsterWeakPointPatternClipboard.Paste(destination) && destination.chargeWeakPoints.Count == 0, "empty pattern copies and pastes consistently");
        }
        finally { EditorGUIUtility.systemCopyBuffer = previousClipboard; Undo.ClearUndo(source); Undo.ClearUndo(destination); Object.DestroyImmediate(source); Object.DestroyImmediate(destination); }
        File.AppendAllText(Folder + "Checks.txt", $"RESULT {checks} checks, {failures} failures\n");
    }
}
