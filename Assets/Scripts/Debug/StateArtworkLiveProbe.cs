#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonSlash;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class StateArtworkLiveProbe : MonoBehaviour
{
    const string Folder = "Docs/Production/StateArtwork-2026-10-26/";
    [Serializable] sealed class Row { public string id, kind, dataPath, animationPath, idle; }
    [Serializable] sealed class Manifest { public Row[] actors; }
    string log; bool passed, background; string[] wanted;
    void Awake() { background = Application.runInBackground; Application.runInBackground = true; }
    void OnDestroy()
    {
        Application.runInBackground = background;
        var input = FindFirstObjectByType<CombatInputController>(); if (input != null) input.SpellModifierForVerification = null;
    }
    public void Run(string mode, string ids)
    {
        wanted = ids.Split(','); Directory.CreateDirectory(Folder + "Screenshots"); Directory.CreateDirectory(Folder + "Verified");
        log = Folder + "Live-" + mode + "-" + DateTime.Now.ToString("HHmmss") + ".txt";
        File.WriteAllText(log, "Scoped original 3/4/2 state-image contract; production actions and existing Unity Editor.\n");
        StartCoroutine(Review(mode));
    }
    void Check(bool ok, string message) { passed &= ok; File.AppendAllText(log, (ok ? "PASS " : "FAIL ") + message + "\n"); }
    void Verified(Row row)
    {
        if (passed) File.Copy(Folder + "Installed/" + row.id + ".txt", Folder + "Verified/" + row.id + ".txt", true);
    }
    static CombatSpriteAnimator Pose(Component owner) => owner.GetComponentInChildren<CombatSpriteAnimator>();
    static bool Shows(CombatSpriteAnimator pose, CombatVisualState state, CombatSpriteAnimation source) => pose != null && pose.State == state && pose.GetComponent<Image>().sprite == source.For(state);
    void Contract(CombatSpriteAnimation source, int count, string id)
    {
        var sprites = new[] { source.idle, source.attack, source.guard, source.ready, source.dead }.Where(s => s != null).ToArray();
        Check(sprites.Length == count && sprites.Distinct().Count() == count && !source.illustratedMotion && source.UsesIllustratedLayout, id + " exact distinct state images; no mesh-motion substitute");
    }
    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        var shot = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Folder + "Screenshots/" + name + ".png", shot.EncodeToPNG()); Destroy(shot);
    }
    IEnumerator Review(string mode)
    {
        var rows = JsonUtility.FromJson<Manifest>(File.ReadAllText(Folder + "Manifest.json")).actors.Where(r => wanted.Contains(r.id)).ToArray();
        var practice = FindFirstObjectByType<OctoberReviewLiveProbe>() ?? new GameObject("State artwork practice").AddComponent<OctoberReviewLiveProbe>();
        foreach (var row in rows)
        {
            passed = true;
            if (mode == "heroes") yield return Hero(row, practice);
            else if (mode == "dog") yield return Dog(row, practice);
            else yield return Monster(row, practice);
            Verified(row);
        }
        File.AppendAllText(log, "COMPLETE\n"); File.WriteAllText(Folder + "Live-Complete.txt", log);
    }
    IEnumerator Hero(Row row, OctoberReviewLiveProbe practice)
    {
        var kind = (CharacterKind)Enum.Parse(typeof(CharacterKind), row.id.Substring("Hero-".Length));
        practice.Setup(kind, kind == CharacterKind.Wizard); yield return null; yield return null;
        var combat = FindFirstObjectByType<CombatController>(); var scene = FindFirstObjectByType<CombatSceneView>();
        var input = FindFirstObjectByType<CombatInputController>(); var view = FindFirstObjectByType<PlayerView>(); var pose = Pose(view); var source = combat.Player.Data.combatAnimation;
        input.SpellModifierForVerification = false;
        Contract(source, 3, row.id); Check(Shows(pose, CombatVisualState.Idle, source), row.id + " Idle");
        var center = scene.MonsterPosition + combat.Enemies[0].Data.combatAnimation.combatPositionOffset;
        var stroke = new AttackSegment(center + Vector2.left * 120, center + Vector2.right * 120, 40);
        combat.Player.ResetAttackCooldown(); combat.ExecuteAttack(stroke, false);
        var timer = typeof(PlayerView).GetField("attackUntil", BindingFlags.Instance | BindingFlags.NonPublic); var before = (float)timer.GetValue(view);
        combat.ExecuteAttack(stroke, false); Check(Mathf.Approximately(before, (float)timer.GetValue(view)), row.id + " cooldown-rejected attack does not restart pose");
        yield return null;
        Check(Shows(pose, CombatVisualState.Attack, source), row.id + " accepted attack switches Attack");
        yield return Capture(row.id + "-Attack"); yield return new WaitForSeconds(.35f);
        Check(Shows(pose, CombatVisualState.Idle, source), row.id + " attack returns to Idle");
        Vector2 ScreenAt(Vector2 p) => RectTransformUtility.WorldToScreenPoint(scene.UiCamera, scene.LogicalArea.TransformPoint(p));
        void Contact(string method, params object[] args) => typeof(CombatInputController).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(input, args);
        var start = ScreenAt(scene.PlayerPosition + Vector2.up * 90); var end = ScreenAt(scene.PlayerPosition + Vector2.down * 55);
        Contact("Begin", 851, start); Contact("Move", 851, end); yield return null; yield return null;
        Check(combat.Player.IsGuarding && Shows(pose, CombatVisualState.Guard, source), row.id + " production drag-hold switches Guard");
        yield return Capture(row.id + "-Guard"); Contact("End", 851, end, false); yield return null;
        Check(Shows(pose, CombatVisualState.Idle, source), row.id + " guard release restores Idle");
        if (kind == CharacterKind.Wizard)
        {
            combat.Player.ResetAttackCooldown(); var accepted = combat.TryCastSpell(SwipeDirection.Up, 3f); yield return null;
            Check(accepted && Shows(pose, CombatVisualState.Attack, source), row.id + " accepted spell also switches Attack");
        }
    }
    IEnumerator Monster(Row row, OctoberReviewLiveProbe practice)
    {
        var definition = AssetDatabase.LoadAssetAtPath<MonsterData>(row.dataPath);
        practice.Setup(CharacterKind.Warrior, false, row.dataPath); yield return null;
        var combat = FindFirstObjectByType<CombatController>(); var scene = FindFirstObjectByType<CombatSceneView>();
        if (definition.diesWithLeader)
        {
            // The single-enemy practice setup can already have queued its victory coroutine.
            // Cancel that fixture before replacing it with the authored leader group.
            combat.ResetEncounter();
            var encounter = AssetDatabase.FindAssets("t:EncounterData", new[] { "Assets/Data/Dungeon/Encounters" }).Select(g => AssetDatabase.LoadAssetAtPath<EncounterData>(AssetDatabase.GUIDToAssetPath(g)))
                .FirstOrDefault(e => e.members.Any(m => m.monster == definition) && e.members.Any(m => m.monster != null && !m.monster.diesWithLeader));
            if (encounter == null) { Check(false, row.id + " requires authored leader encounter"); yield break; }
            combat.PrepareFixedEncounter(combat.Run, encounter, 1); combat.BeginPreparedCombat(); yield return null;
        }
        var enemy = definition.diesWithLeader ? combat.Enemies.First(m => m.Data == definition) : combat.Enemies[0];
        var source = enemy.Data.combatAnimation;
        var entries = (IEnumerable)typeof(CombatController).GetField("activeMonsters", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(combat);
        var active = entries.Cast<object>().First(e => ReferenceEquals(e.GetType().GetProperty("Runtime").GetValue(e), enemy));
        var view = (MonsterView)active.GetType().GetProperty("View").GetValue(active); var pose = Pose(view);
        Contract(source, 4, row.id); Check(Shows(pose, CombatVisualState.Idle, source), row.id + " Idle");
        if (enemy.NormalAttack != null)
        {
            enemy.BeginNormalAttack(); yield return null;
            Check(Shows(pose, CombatVisualState.Ready, source), row.id + " real normal windup switches Ready");
            var deadline = Time.realtimeSinceStartup + enemy.NormalAttack.windupDuration + 1f;
            while (Time.realtimeSinceStartup < deadline && pose.State != CombatVisualState.Attack) yield return null;
            Check(Shows(pose, CombatVisualState.Attack, source), row.id + " real normal hit switches Attack");
        }
        else Check(source.ready != null && source.attack != null, row.id + " passive support: Ready/Attack slots valid; role does not attack");
        enemy.TakeDamage(enemy.CurrentHp); view.Bind(enemy);
        Check(Shows(pose, CombatVisualState.Dead, source), row.id + " lethal damage switches Dead before fade");
        yield return Capture(row.id + "-Dead");
    }
    IEnumerator Dog(Row row, OctoberReviewLiveProbe practice)
    {
        practice.Setup(CharacterKind.Warrior, false); yield return null;
        var run = FindFirstObjectByType<RunController>(); var combat = FindFirstObjectByType<CombatController>();
        run.TryGrantEquipment(combat.Content.equipment.First(e => e.id == "StrayDog")); yield return null;
        var view = FindFirstObjectByType<CompanionView>(); var data = run.State.Companions.First(); var source = data.combatAnimation;
        Contract(source, 2, row.id); Check(view != null && Shows(Pose(view), CombatVisualState.Idle, source), row.id + " rear Idle");
        var scene = FindFirstObjectByType<CombatSceneView>(); var input = FindFirstObjectByType<CombatInputController>(); input.SpellModifierForVerification = false;
        Vector2 ScreenAt(Vector2 p) => RectTransformUtility.WorldToScreenPoint(scene.UiCamera, scene.LogicalArea.TransformPoint(p));
        void Contact(string method, params object[] args) => typeof(CombatInputController).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(input, args);
        var start = ScreenAt(scene.PlayerPosition + Vector2.up * 90); var end = ScreenAt(scene.PlayerPosition + Vector2.down * 55);
        Contact("Begin", 852, start); Contact("Move", 852, end); yield return null;
        var deadline = Time.realtimeSinceStartup + 8f;
        while (Time.realtimeSinceStartup < deadline && view.State != CombatVisualState.Attack) yield return null;
        Check(combat.Player.IsGuarding && view.State == CombatVisualState.Attack && Shows(Pose(view), CombatVisualState.Attack, source), row.id + " companion attack switches independently while player guards");
        yield return Capture(row.id + "-Attack");
        Contact("End", 852, end, false);
    }
}
#endif
