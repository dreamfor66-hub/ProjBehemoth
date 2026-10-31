using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

// One saved, idempotent presentation revision. Combat data is never rebuilt.
[InitializeOnLoad]
internal static class MonsterArtPolishPreparation
{
    const string Folder = "Docs/Production/MonsterPolish-2026-10-26/";
    const string Stamp = "Assets/Data/Presentation/Angular/MonsterArtPolishInstalled.txt";
    const string Revision = "monster-art-polish-2";
    const string Request = "Temp/MonsterArtPolish.request";
    [Serializable] internal sealed class Row { public string id, baseline, name, hint; public bool setOffset; public float offsetY; }
    [Serializable] sealed class Manifest { public Row[] items; }
    static bool busy, verifying;
    static double next;
    static MonsterArtPolishPreparation() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (busy || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (verifying && File.Exists(Folder + "Live-Complete.txt"))
        { verifying = false; EditorApplication.isPlaying = false; return; }
        if (File.Exists(Request) && File.ReadAllText(Request).Trim().StartsWith("verify") && File.Exists(Stamp) && File.ReadAllText(Stamp) == Revision)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.isDirty || !scene.path.EndsWith("MainGameScene.unity"))
            { File.WriteAllText(Folder + "Verification-Pending.txt", "Live verification requires saved MainGameScene."); File.Delete(Request); return; }
            var command = File.ReadAllText(Request).Trim().Split(':');
            SessionState.SetString("MonsterPolishVerifyTarget", command.Length > 1 ? command[1] : "");
            File.Delete(Request);
            SessionState.SetBool("MonsterPolishVerify", true);
            EditorApplication.isPlaying = true;
            return;
        }
        if (SessionState.GetBool("MonsterPolishVerify", false) && EditorApplication.isPlaying)
        {
            SessionState.SetBool("MonsterPolishVerify", false); verifying = true;
            if (File.Exists(Folder + "Live-Complete.txt")) File.Delete(Folder + "Live-Complete.txt");
            new GameObject("Scoped monster artwork review").AddComponent<MonsterArtPolishLiveProbe>().Run(SessionState.GetString("MonsterPolishVerifyTarget", ""));
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + 4;
        if ((File.Exists(Stamp) && File.ReadAllText(Stamp) == Revision) || !File.Exists(Folder + "Ready.txt")) return;
        busy = true;
        try { Apply(); File.WriteAllText(Stamp, Revision); AssetDatabase.ImportAsset(Stamp); }
        catch (Exception e) { File.WriteAllText(Folder + "Preparation-Error.txt", e.ToString()); next += 60; }
        finally { busy = false; }
    }
    static void Apply()
    {
        var rows = JsonUtility.FromJson<Manifest>(File.ReadAllText(Folder + "Manifest.json")).items;
        if (rows.Length != 41 || rows.Select(r => r.id).Distinct().Count() != 41) throw new InvalidDataException("Expected 41 unique artworks");
        var csv = ScriptableObject.CreateInstance<GameTextDatabase>();
        csv.ImportCsv(File.ReadAllText(BehemothTextEditor.CsvPath));
        var touched = new HashSet<LocalizedData>();
        void Sync(LocalizedData data)
        {
            EditorUtility.SetDirty(data); BehemothTextEditor.SyncAsset(data); touched.Add(data);
        }
        foreach (var row in rows)
        {
            var output = "Assets/Art/World/Angular/Monsters/" + row.id + ".png";
            File.Copy(Folder + "Generated/" + row.id + ".png", output, true);
            AssetDatabase.ImportAsset(output, ImportAssetOptions.ForceSynchronousImport);
            var monster = AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/Combat/Monsters/" + row.baseline + ".asset");
            if (monster == null) throw new InvalidDataException(row.baseline);
            if (row.setOffset)
            {
                monster.combatAnimation.combatPositionOffset = new Vector2(monster.combatAnimation.combatPositionOffset.x, row.offsetY);
                EditorUtility.SetDirty(monster.combatAnimation);
            }
            // Existing sprite GUID, importer settings, animation layout, attacks and timing stay authored.
            if (monster.displayName == row.name && monster.encounterHint == row.hint) continue;
            var oldName = monster.displayName;
            monster.displayName = row.name; monster.encounterHint = row.hint; Sync(monster);
            foreach (var attack in monster.attacks.Where(a => a != null))
            { attack.displayName = attack.displayName.Replace(oldName, row.name); Sync(attack); }
        }
        foreach (var guid in AssetDatabase.FindAssets("t:EncounterData", new[] { "Assets/Data/Dungeon/Encounters" }))
        {
            var encounter = AssetDatabase.LoadAssetAtPath<EncounterData>(AssetDatabase.GUIDToAssetPath(guid));
            var occupants = encounter.members.Where(m => m.monster != null).Select(m => m.monster).Distinct().ToArray();
            if (!occupants.Any(m => touched.Contains(m))) continue;
            encounter.displayName = string.Join(" · ", occupants.Select(m => m.displayName));
            encounter.hint = string.Join("\n", occupants.Select(m => m.encounterHint).Where(h => !string.IsNullOrEmpty(h))); Sync(encounter);
        }
        foreach (var data in touched) foreach (var binding in data.textKeys)
            csv.Set(binding.key, BehemothTextEditor.Table.Get(binding.key), AssetDatabase.GetAssetPath(data) + ":" + binding.propertyPath);
        File.WriteAllText(BehemothTextEditor.CsvPath, csv.ExportCsv(), new UTF8Encoding(true));
        UnityEngine.Object.DestroyImmediate(csv); AssetDatabase.ImportAsset(BehemothTextEditor.CsvPath);
        EditorUtility.SetDirty(BehemothTextEditor.Table); AssetDatabase.SaveAssets();
        File.WriteAllText(Folder + "Installed.txt", "40 sprite replacements; CrystalUrchin retained because image generation was blocked. Five concept/name revisions and Korean text synchronized. Skeleton warrior lowered from the former hanging marionette position to the standing enemies' height (-67), keeping its name below the HUD. No attack, timing, stat, encounter composition, global layout or hero changes.\n");
    }
}
