#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEngine;

// Scoped collision, placement, scaling and orbit checks in the user's existing Editor.
public sealed class MonsterAuthoringLiveProbe : MonoBehaviour
{
    const string Folder = "Docs/Production/MonsterAuthoring-2026-10-30/";
    int failures;
    bool previousBackground;
    readonly List<MonsterData> copies = new();
    void Awake() { previousBackground = Application.runInBackground; Application.runInBackground = true; }
    void OnDestroy() { Application.runInBackground = previousBackground; foreach (var copy in copies) if (copy != null) Destroy(copy); }
    void Check(bool ok, string name) { if (!ok) failures++; File.AppendAllText(Folder + "Live.txt", (ok ? "PASS " : "FAIL ") + name + "\n"); }
    MonsterData Copy(string name)
    {
        var source = DungeonSlash.DesignerAssetPaths.Load<MonsterData>("Assets/Data/Combat/Monsters/" + name + ".asset");
        var copy = Instantiate(source); copy.maxHp = 10000; copy.normalAttackInterval = copy.chargeInterval = 999; copies.Add(copy); return copy;
    }
    public void Run() => StartCoroutine(Review());
    IEnumerator Review()
    {
        File.WriteAllText(Folder + "Live.txt", "Scoped existing-Editor runtime matrix; runtime-only clones, no balance suite.\n");
        var practice = FindFirstObjectByType<OctoberReviewLiveProbe>() ?? new GameObject("Monster authoring practice").AddComponent<OctoberReviewLiveProbe>();
        practice.Setup(CharacterKind.Warrior, false);
        var combat = FindFirstObjectByType<CombatController>(); var scene = FindFirstObjectByType<CombatSceneView>();
        var skeleton = Copy("Data_Monster_B1_Minion_Skeleton"); skeleton.useOrbit = false;
        foreach (var count in new[] { 1, 3 })
        {
            combat.ResetEncounter(); combat.PrepareEncounter(combat.Run, Enumerable.Repeat(skeleton, count).ToArray(), 1); combat.BeginPreparedCombat(); yield return null;
            var views = FindObjectsByType<MonsterView>(FindObjectsSortMode.None).OrderBy(v => v.Root.anchoredPosition.x).ToArray();
            Check(views.Length == count && views.All(v => Mathf.Abs(v.Root.localScale.x - skeleton.combatScale) < .001f), count + " actors retain species CombatScale");
            for (var i = 0; i < views.Length; i++)
                Check(Vector2.Distance(views[i].Root.anchoredPosition + scene.MonsterPosition + Vector2.up * skeleton.FootprintY * skeleton.combatScale, scene.GetMonsterFormationPosition(i, count)) < .01f, count + " formation footprint " + i);
        }
        combat.ResetEncounter(); combat.PrepareEncounter(combat.Run, new[] { skeleton }, 1); combat.BeginPreparedCombat(); yield return null;
        var view = FindFirstObjectByType<MonsterView>(); var center = view.Root.anchoredPosition + scene.MonsterPosition;
        var runtime = combat.Enemies[0]; var hp = runtime.CurrentHp; combat.Player.ResetAttackCooldown();
        combat.ExecuteAttack(new AttackSegment(center + Vector2.left * 80, center + Vector2.right * 80, 30), false); yield return null;
        Check(runtime.CurrentHp < hp, "actual normal slash damages unified hitboxes");
        runtime.StartCharge(center); yield return null;
        var point = runtime.WeakPoints.First(p => p.IsActive && !p.IsOptional); var beforeHits = point.RemainingHits;
        combat.Player.ResetAttackCooldown(); combat.ExecuteAttack(new AttackSegment(point.HitShape.Center + Vector2.left * 40, point.HitShape.Center + Vector2.right * 40, 30), true); yield return null;
        Check(point.RemainingHits < beforeHits, "actual charged slash hits authored weak point");
        var scaled = Copy("Data_Monster_B1_Minion_Skeleton"); scaled.combatScale = 1.6f;
        combat.ResetEncounter(); combat.PrepareEncounter(combat.Run, new[] { scaled }, 1); combat.BeginPreparedCombat(); yield return null;
        view = FindFirstObjectByType<MonsterView>(); center = view.Root.anchoredPosition + scene.MonsterPosition; runtime = combat.Enemies[0]; runtime.StartCharge(center);
        var definition = scaled.GetAttack(MonsterAttackType.Charge).chargeWeakPoints[0];
        Check(Mathf.Abs(runtime.VisualScale - 1.6f) < .001f && Vector2.Distance(runtime.WeakPoints[0].HitShape.Center - center, definition.localPosition * 1.6f) < .001f, "CombatScale consistently scales artwork, body and weak points");
        var orbit = Copy("Data_Monster_B1_Minion_Skeleton"); orbit.useOrbit = true;
        combat.ResetEncounter(); combat.PrepareEncounter(combat.Run, new[] { orbit }, 1); combat.BeginPreparedCombat();
        yield return new WaitForSecondsRealtime(.15f);
        view = FindFirstObjectByType<MonsterView>();
        var origin = orbit.BodyCenterFromPlacement(scene.GetMonsterFormationPosition(0, 1), orbit.combatScale);
        var angle = combat.Player.CombatTime * orbit.orbitSpeed;
        var expected = origin + new Vector2(Mathf.Cos(angle) * orbit.orbitRadius, Mathf.Sin(angle) * 65);
        Check(Vector2.Distance(view.Root.anchoredPosition + scene.MonsterPosition, expected) < .01f, "useOrbit preserves original radius/speed/65-unit vertical orbit");
        orbit.useOrbit = false; var stopped = view.Root.anchoredPosition; yield return new WaitForSecondsRealtime(.1f);
        Check(Vector2.Distance(stopped, view.Root.anchoredPosition) < .001f, "useOrbit=false stops orbit movement");
        combat.ResetEncounter();
        File.AppendAllText(Folder + "Live.txt", "COMPLETE failures=" + failures + "\n");
    }
}
#endif
