using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DungeonSlash;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
internal static class MonsterTimingRollback
{
    const string Folder = "Docs/Production/MonsterComparison-2026-10-26/";
    const string Stamp = "Assets/Data/Presentation/Angular/NormalTimingRestored.txt";
    static readonly string[] Changed = { "BoneScorpion", "EttinGatekeeper", "IronMantis", "TwinAntlerStag", "TempestRoc", "SerpentWarden", "AshDragon", "TarrasqueWhelp" };
    static double next;
    static bool busy;
    static MonsterTimingRollback() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (busy || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < next) return;
        const string request = "Temp/MonsterComparison.request";
        var command = File.Exists(request) ? File.ReadAllText(request).Trim() : "";
        if (command == "verify" && EditorApplication.isPlaying) { File.Delete(request); VerifyLive(); return; }
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        next = EditorApplication.timeSinceStartup + 4;
        if (File.Exists(Stamp) && command != "export") return;
        busy = true;
        try
        {
            Directory.CreateDirectory(Folder);
            if (!File.Exists(Stamp)) Restore();
            Export();
            if (File.Exists(request)) File.Delete(request);
        }
        catch (Exception e) { File.WriteAllText(Folder + "Preparation-Error.txt", e.ToString()); }
        finally { busy = false; }
    }
    static MonsterData Monster(string baseline) => AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/Combat/Monsters/" + baseline + ".asset");
    static float Before(string baseline, string field)
    {
        var text = File.ReadAllText("Docs/Production/AngularWorld-2026-10-26/Before/Monsters/" + baseline + ".asset");
        return float.Parse(Regex.Match(text, "^  " + field + @": (\S+)", RegexOptions.Multiline).Groups[1].Value, CultureInfo.InvariantCulture);
    }
    static void Backup(string path)
    {
        var output = Folder + "BeforeRollback/" + path;
        if (File.Exists(output)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(output)); File.Copy(path, output);
    }
    static void Restore()
    {
        var log = "Requested rollback: retain original normal timing/action; no concept-specific timing redesign.\n";
        foreach (var e in AngularWorldPreparation.Entries().Where(e => Changed.Contains(e.id)))
        {
            var monster = Monster(e.baseline); var normal = monster.GetAttack(MonsterAttackType.Normal);
            Backup(AssetDatabase.GetAssetPath(monster));
            foreach (var attack in monster.normalAttackSequence.Where(a => a != null))
            {
                Backup(AssetDatabase.GetAssetPath(attack)); attack.windupDuration = normal.windupDuration; EditorUtility.SetDirty(attack);
            }
            monster.normalAttackSequence.Clear(); EditorUtility.SetDirty(monster);
            log += e.id + ": sequence removed; normal windup=" + normal.windupDuration.ToString("R", CultureInfo.InvariantCulture) + " sec; interval=" + monster.normalAttackInterval.ToString("R", CultureInfo.InvariantCulture) + " sec\n";
        }
        AssetDatabase.SaveAssets();
        File.WriteAllText(Folder + "Rollback.txt", log);
        File.WriteAllText(Stamp, "designer-normal-timing-restored-1"); AssetDatabase.ImportAsset(Stamp);
    }
    [Serializable] sealed class AttackRow { public string path, json, onFailure; public string[] opening; }
    [Serializable] sealed class MonsterRow { public string id, baseline, sprite, json; public int floor; public AttackRow[] attacks; public string[] normalSequence; }
    [Serializable] sealed class MemberRow { public string monster; public Vector2 offset; public float scale, actionDelay; }
    [Serializable] sealed class EncounterRow { public string id, category; public MemberRow[] members; }
    [Serializable] sealed class Report { public MonsterRow[] monsters; public EncounterRow[] encounters; }
    static void Export()
    {
        var entries = AngularWorldPreparation.Entries();
        var ids = entries.ToDictionary(e => Monster(e.baseline), e => e.id);
        var report = new Report
        {
            monsters = entries.Select(e =>
            {
                var m = Monster(e.baseline);
                return new MonsterRow { id=e.id, baseline=e.baseline, floor=e.floor, sprite=AssetDatabase.GetAssetPath(m.portrait.texture), json=JsonUtility.ToJson(m),
                    attacks=m.attacks.Select(a=>new AttackRow{path=AssetDatabase.GetAssetPath(a),json=JsonUtility.ToJson(a),
                        onFailure=a.chargeSummonMechanics.summonOnFailedCharge!=null&&ids.TryGetValue(a.chargeSummonMechanics.summonOnFailedCharge,out var summon)?summon:"",
                        opening=a.chargeSummonMechanics.openingSummons.Where(s=>s!=null&&ids.ContainsKey(s)).Select(s=>ids[s]).ToArray()}).ToArray(), normalSequence=m.normalAttackSequence.Select(a=>AssetDatabase.GetAssetPath(a)).ToArray() };
            }).ToArray(),
            encounters = AssetDatabase.FindAssets("t:EncounterData", new[] { "Assets/Data/Dungeon/Encounters" }).Select(g => AssetDatabase.LoadAssetAtPath<EncounterData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(e => e.members.Any(m => m.monster != null && ids.ContainsKey(m.monster)))
                .Select(e => new EncounterRow { id=e.id, category=e.category.ToString(), members=e.members.Where(m=>m.monster!=null&&ids.ContainsKey(m.monster)).Select(m=>new MemberRow{monster=ids[m.monster],offset=m.offset,scale=m.scale,actionDelay=m.actionDelay}).ToArray() }).ToArray()
        };
        File.WriteAllText(Folder + "CurrentData.json", JsonUtility.ToJson(report, true));
    }
    static void VerifyLive()
    {
        var log = "Scoped live Unity runtime verification: two original normal actions for each of the eight restored monsters.\n";
        var failures = 0;
        foreach (var e in AngularWorldPreparation.Entries().Where(e => Changed.Contains(e.id)))
        {
            var data = Monster(e.baseline); var normal = data.GetAttack(MonsterAttackType.Normal);
            var beforeMonster = File.ReadAllText("Docs/Production/AngularWorld-2026-10-26/Before/Monsters/" + e.baseline + ".asset");
            var attackGuids = Regex.Matches(beforeMonster.Substring(beforeMonster.IndexOf("  attacks:", StringComparison.Ordinal)), @"guid: (\w+)").Cast<Match>().Select(m=>m.Groups[1].Value);
            string original = null;
            foreach (var guid in attackGuids)
            {
                var meta = Directory.GetFiles("Docs/Production/AngularWorld-2026-10-26/Before/Attacks", "*.asset.meta").Single(p=>File.ReadAllText(p).Contains("guid: " + guid));
                var text = File.ReadAllText(meta.Substring(0, meta.Length-5));
                if (Regex.IsMatch(text, "^  type: 0\\r?$", RegexOptions.Multiline)) { original = text; break; }
            }
            float Old(string field) => float.Parse(Regex.Match(original, "^  " + field + @": (\S+)", RegexOptions.Multiline).Groups[1].Value, CultureInfo.InvariantCulture);
            var ok = data.normalAttackSequence.Count == 0 && Mathf.Approximately(data.normalAttackInterval, Before(e.baseline,"normalAttackInterval"))
                && Mathf.Approximately(normal.windupDuration,Old("windupDuration")) && Mathf.Approximately(normal.damage,Old("damage")) && Mathf.Approximately(normal.shieldDamage,Old("shieldDamage"));
            var runtime = new MonsterRuntime(data);
            for (var action=0;action<2;action++)
            {
                runtime.BeginNormalAttack(); ok &= runtime.NormalAttack==normal && Mathf.Approximately(runtime.StateTimer,Old("windupDuration"));
                var steps=0;
                while(runtime.State==MonsterState.Attacking&&steps++<20){runtime.Tick(runtime.StateTimer+.01f,Vector2.zero);runtime.AdvanceNormalAttack();}
                ok &= runtime.State==MonsterState.Idle && Mathf.Approximately(runtime.NormalAttackTimer,Before(e.baseline,"normalAttackInterval"));
            }
            if(!ok)failures++;
            log += (ok?"PASS ":"FAIL ") + e.id + ": original normal action, first/second windup " + normal.windupDuration + ", reset interval " + data.normalAttackInterval + ", original HP/guard damage.\n";
        }
        File.WriteAllText(Folder+"Rollback-Live.txt",log+(failures==0?"COMPLETE\n":"FAILED\n"));
    }
}
