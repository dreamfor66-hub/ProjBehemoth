using System.IO;
using System.Text;
using System.Collections.Generic;
using DungeonSlash;
using UnityEditor;

// Finalizes already-migrated saved assets once; no scene baking or manual menu is required.
internal static class BehemothBuffPreparation
{
    [InitializeOnLoadMethod] private static void Schedule() => EditorApplication.delayCall += Prepare;
    internal static void Prepare()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        { EditorApplication.delayCall += Prepare; return; }
        var content = AssetDatabase.LoadAssetAtPath<GameContent>(BehemothContentBuilder.ContentPath);
        if (content == null || content.schemaVersion < 15 || content.schemaVersion >= 19) return;
        // Serialization drops the removed fields only after their references have been migrated.
        foreach (var type in new[] { "EquipmentData", "BuffData", "PlayerCombatData" })
            foreach (var guid in AssetDatabase.FindAssets("t:" + type, new[] { "Assets/Data" }))
                EditorUtility.SetDirty(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)));
        var table = BehemothTextEditor.Table;
        table.Set("ui.buff.stacks", "{0} {1:0.#}/{2:0.#}", "Assets/Scripts/DungeonSlash/Combat/Runtime/CharacterCombatRuntime.cs");
        EditorUtility.SetDirty(table);
        var csv = UnityEngine.ScriptableObject.CreateInstance<GameTextDatabase>(); csv.ImportCsv(File.ReadAllText(BehemothTextEditor.CsvPath));
        if (content.schemaVersion < 17) foreach (var name in new[] { "LongRage", "ExecuteRage", "RageCharge", "BloodFrenzy" })
        {
            var item = AssetDatabase.LoadAssetAtPath<EquipmentData>("Assets/Data/Behemoth/Relics/" + name + ".asset");
            item.description = item.description.Replace("격노", name == "BloodFrenzy" ? "격노/분노" : "분노");
            EditorUtility.SetDirty(item); BehemothTextEditor.SyncAsset(item);
            foreach (var binding in item.textKeys) csv.Set(binding.key, table.Get(binding.key), AssetDatabase.GetAssetPath(item) + ":" + binding.propertyPath);
        }
        var renamedKeys = new HashSet<string>();
        foreach (var state in new[] { (Old: "Form_Flow_0_flow", Name: "FlowAcceleration", Title: "연환 가속"), (Old: "Form_Shelter_0_shelter", Name: "ShelterWard", Title: "호신 보호") })
        {
            var path = "Assets/Data/Behemoth/Buffs/" + state.Old + ".asset";
            var data = AssetDatabase.LoadAssetAtPath<BuffData>(path); if (data == null) continue;
            var error = AssetDatabase.RenameAsset(path, state.Name); if (!string.IsNullOrEmpty(error)) throw new System.InvalidOperationException(error);
            if (data.displayName.StartsWith("Form_")) data.displayName = state.Title;
            EditorUtility.SetDirty(data); BehemothTextEditor.SyncAsset(data);
            foreach (var binding in data.textKeys) renamedKeys.Add(binding.key);
        }
        // Stable text keys survive asset renames; their source links follow the new paths.
        foreach (var guid in AssetDatabase.FindAssets("t:BuffData", new[] { "Assets/Data/Behemoth/Buffs" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid); var data = AssetDatabase.LoadAssetAtPath<BuffData>(path);
            foreach (var binding in data.textKeys)
            {
                var source = path + ":" + binding.propertyPath;
                table.Set(binding.key, table.Get(binding.key), source);
                csv.Set(binding.key, renamedKeys.Contains(binding.key) || !csv.Contains(binding.key) ? table.Get(binding.key) : csv.Get(binding.key), source);
                // Set deliberately preserves existing attribution; migration must relocate it explicitly.
                foreach (var entry in table.entries.FindAll(entry => entry.key == binding.key)) entry.source = source;
                foreach (var entry in csv.entries.FindAll(entry => entry.key == binding.key)) entry.source = source;
            }
        }
        EditorUtility.SetDirty(table);
        content.schemaVersion = 19; EditorUtility.SetDirty(content); AssetDatabase.SaveAssets();
        File.WriteAllText(BehemothTextEditor.CsvPath, csv.ExportCsv(), new UTF8Encoding(true)); UnityEngine.Object.DestroyImmediate(csv);
        Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/Behemoth-BuffUnificationMigration.txt", "Schema 19 complete: state, stacks and attack chains use BuffData. Legacy types/assets archived outside Assets; text source links updated.");
    }
}
