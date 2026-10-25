using TMPro;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DungeonSlash;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class BehemothTextEditor
{
    public const string TablePath = "Assets/Data/Localization/Resources/BehemothKr.asset";
    public const string CsvPath = "Assets/Data/Localization/kr.csv";
    private static readonly HashSet<string> Fields = new() { "displayName", "description", "narrative", "title", "hint", "encounterHint", "resultText" };
    private static bool synchronizing;
    static BehemothTextEditor() { EditorApplication.delayCall += Prepare; Undo.undoRedoPerformed += () => EditorApplication.delayCall += SyncAll; }
    public static GameTextDatabase Table
    {
        get
        {
            var table = AssetDatabase.LoadAssetAtPath<GameTextDatabase>(TablePath);
            if (table != null) return table;
            Directory.CreateDirectory(Path.GetDirectoryName(TablePath)); AssetDatabase.Refresh();
            table = ScriptableObject.CreateInstance<GameTextDatabase>(); AssetDatabase.CreateAsset(table, TablePath); return table;
        }
    }
    private static void Prepare()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.delayCall += Prepare; return; }
        // Production data is serialized. Opening the project only synchronizes authored text.
        SyncAll();
        EditorUtility.SetDirty(Table); AssetDatabase.SaveAssets();
        // Recompilation must never overwrite a CSV the designer is editing externally.
        if (!File.Exists(CsvPath)) ExportDefault();
    }
    public static void PrepareForVerification() => Prepare();
    public static void SyncAsset(LocalizedData data, bool tableWins = false)
    {
        if (data == null || synchronizing) return;
        synchronizing = true;
        try
        {
            var table = Table; var assetPath = AssetDatabase.GetAssetPath(data); var guid = AssetDatabase.AssetPathToGUID(assetPath); var changed = false;
            if (!string.IsNullOrEmpty(guid) && guid != data.textOwnerGuid)
            { if (!string.IsNullOrEmpty(data.textOwnerGuid)) data.textKeys.Clear(); data.textOwnerGuid = guid; changed = true; }
            var serialized = new SerializedObject(data); var property = serialized.GetIterator(); var valid = new HashSet<string>();
            while (property.Next(true))
            {
                if (property.propertyType != SerializedPropertyType.String || !Fields.Contains(property.name) || property.propertyPath.StartsWith("textKeys.")) continue;
                var path = property.propertyPath; valid.Add(path);
                var binding = data.textKeys.FirstOrDefault(item => item.propertyPath == path);
                if (binding == null)
                {
                    var slug = data.name.ToLowerInvariant().Replace(" ", "_");
                    if (table.entries.Any(entry => entry.key.StartsWith("data." + data.GetType().Name.ToLowerInvariant() + "." + slug + ".", StringComparison.Ordinal) && !string.IsNullOrEmpty(entry.source) && !entry.source.StartsWith(assetPath + ":", StringComparison.Ordinal))) slug += "_" + guid.Substring(0, Math.Min(8, guid.Length));
                    binding = new DataTextBinding { propertyPath = path, key = "data." + data.GetType().Name.ToLowerInvariant() + "." + slug + "." + path.Replace(".Array.data[", ".").Replace("]", "").ToLowerInvariant(), lastText = property.stringValue };
                    data.textKeys.Add(binding); changed = true;
                }
                var raw = property.stringValue;
                if (!table.Contains(binding.key) || (!tableWins && raw != binding.lastText))
                { if (table.Set(binding.key, raw, AssetDatabase.GetAssetPath(data) + ":" + path)) EditorUtility.SetDirty(table); }
                else if (raw != table.Get(binding.key)) { property.stringValue = table.Get(binding.key); changed = true; }
            }
            // Applying serialized string edits may replace managed lists with the old snapshot.
            // Preserve newly generated bindings, then capture the actual resulting field values.
            var bindings = data.textKeys.ToList();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            data.textKeys = bindings;
            var applied = new SerializedObject(data);
            foreach (var binding in data.textKeys)
            {
                var value = applied.FindProperty(binding.propertyPath);
                if (value == null || value.propertyType != SerializedPropertyType.String || binding.lastText == value.stringValue) continue;
                binding.lastText = value.stringValue; changed = true;
            }
            changed |= data.textKeys.RemoveAll(item => !valid.Contains(item.propertyPath)) > 0;
            if (changed || EditorUtility.IsDirty(data)) EditorUtility.SetDirty(data);
        }
        finally { synchronizing = false; }
    }
    public static void SyncAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        foreach (var guid in AssetDatabase.FindAssets("t:LocalizedData", new[] { "Assets/Data" })) SyncAsset(AssetDatabase.LoadAssetAtPath<LocalizedData>(AssetDatabase.GUIDToAssetPath(guid)));
    }
    [MenuItem("Behemoth/텍스트/한국어 CSV 내보내기")]
    public static void ExportDefault()
    {
        SyncAll(); EditorUtility.SetDirty(Table); AssetDatabase.SaveAssets();
        File.WriteAllText(CsvPath, Table.ExportCsv(), new UTF8Encoding(true)); AssetDatabase.ImportAsset(CsvPath);
    }
    [MenuItem("Behemoth/텍스트/한국어 CSV 가져오기")]
    public static void ImportDefault() => ImportFile(CsvPath);
    public static void ImportFile(string path)
    {
        var table = Table; Undo.RecordObject(table, "Import Korean CSV"); table.ImportCsv(File.ReadAllText(path)); EditorUtility.SetDirty(table);
        foreach (var guid in AssetDatabase.FindAssets("t:LocalizedData", new[] { "Assets/Data" }))
        { var data = AssetDatabase.LoadAssetAtPath<LocalizedData>(AssetDatabase.GUIDToAssetPath(guid)); Undo.RecordObject(data, "Import Korean Text"); SyncAsset(data, true); }
        AssetDatabase.SaveAssets();
        foreach (var binding in UnityEngine.Object.FindObjectsByType<GameTextBinding>(FindObjectsInactive.Include, FindObjectsSortMode.None)) binding.Refresh();
    }
}
[CustomEditor(typeof(LocalizedData), true)]
internal sealed class LocalizedDataInspector : Sirenix.OdinInspector.Editor.OdinEditor
{
    private bool showKeys;
    public override void OnInspectorGUI()
    {
        var data = (LocalizedData)target;
        if (data.textKeys.Count == 0) BehemothTextEditor.SyncAsset(data);
        EditorGUI.BeginChangeCheck(); base.OnInspectorGUI();
        if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(BehemothTextEditor.Table, "Edit Korean Text"); BehemothTextEditor.SyncAsset(data); EditorApplication.delayCall += () => AssetDatabase.SaveAssets(); }
        showKeys = EditorGUILayout.Foldout(showKeys, "텍스트 키 (자동 생성)");
        if (showKeys) foreach (var binding in data.textKeys) { EditorGUILayout.LabelField(binding.propertyPath); EditorGUILayout.SelectableLabel(binding.key, EditorStyles.textField, GUILayout.Height(19)); }
        if (GUILayout.Button("로케일 검색·편집 열기")) BehemothLocaleWindow.OpenSearch(data.textKeys.FirstOrDefault()?.key ?? "");
    }
}
internal sealed class BehemothTextAssetImport : AssetPostprocessor
{
    private static readonly HashSet<string> Pending = new();
    private static bool scheduled;
    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] previous)
    {
        foreach (var path in imported.Concat(moved))
            if (path.StartsWith("Assets/Data/", StringComparison.Ordinal) && path.EndsWith(".asset", StringComparison.Ordinal) && path != BehemothTextEditor.TablePath) Pending.Add(path);
        if (Pending.Count == 0 || scheduled) return;
        scheduled = true; EditorApplication.delayCall += Flush;
    }
    private static void Flush()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += Flush; return; }
        scheduled = false; var paths = Pending.ToArray(); Pending.Clear();
        foreach (var path in paths) BehemothTextEditor.SyncAsset(AssetDatabase.LoadAssetAtPath<LocalizedData>(path));
    }
}
[CustomEditor(typeof(GameTextDatabase))]
internal sealed class GameTextDatabaseInspector : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("한국어 원문 테이블. 데이터의 이름·설명을 직접 고쳐도 이 테이블에 반영됩니다. CSV의 key 열은 유지하고 kr 열을 편집하세요.", MessageType.Info);
        if (GUILayout.Button("로케일 검색·편집 열기")) BehemothLocaleWindow.Open();
        if (GUILayout.Button("kr.csv 내보내기")) BehemothTextEditor.ExportDefault();
        if (GUILayout.Button("kr.csv 가져오기")) BehemothTextEditor.ImportDefault();
        EditorGUI.BeginChangeCheck(); DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck())
        { ((GameTextDatabase)target).Invalidate(); BehemothTextEditor.SyncAll(); EditorApplication.delayCall += () => AssetDatabase.SaveAssets(); }
    }
}
