#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonSlash;
using UnityEngine;
using UnityEngine.UI;

// Explicit local checks only: authored scene placement and the target geometry that follows it.
public sealed class MonsterPlacementLiveProbe : MonoBehaviour
{
    const string Folder = "Docs/Production/MonsterPlacement-2026-10-27/";
    CombatController combat;
    CombatSceneView scene;
    int failures;
    bool previousBackground;
    void Awake() { previousBackground = Application.runInBackground; Application.runInBackground = true; }
    void OnDestroy() => Application.runInBackground = previousBackground;
    static T Ref<T>(object owner, string field) => (T)owner.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
    void Check(bool ok, string name)
    {
        File.AppendAllText(Folder + "Live.txt", (ok ? "PASS " : "FAIL ") + name + "\n");
        if (!ok) { failures++; Debug.LogError("Monster placement: " + name); }
    }
    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame(); var frame = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Folder + name + ".png", frame.EncodeToPNG()); Destroy(frame);
    }
    public void Run() => StartCoroutine(Review());
    IEnumerator Review()
    {
        Directory.CreateDirectory(Folder); File.WriteAllText(Folder + "Live.txt", "Existing Editor only. Saved anchor/default formation and real target geometry. No balance suite.\n");
        yield return null; yield return null;
        var practice = FindFirstObjectByType<OctoberReviewLiveProbe>() ?? new GameObject("Monster placement practice").AddComponent<OctoberReviewLiveProbe>();
        practice.Setup(CharacterKind.Warrior, false);
        combat = FindFirstObjectByType<CombatController>(); scene = FindFirstObjectByType<CombatSceneView>();
        var goblin = Instantiate(combat.Enemies[0].Data);
        Check(scene.MonsterPosition == new Vector2(0,-150), "saved MonsterSpawnRoot=(0,-150)");
        Check(goblin.combatAnimation.combatPositionOffset == Vector2.zero, "goblin has no extra -66.5 vertical offset");
        foreach (var count in new[] { 1, 2, 3 })
        {
            combat.ResetEncounter(); combat.PrepareEncounter(combat.Run, Enumerable.Repeat(goblin, count).ToArray(), 1);
            combat.BeginPreparedCombat(); yield return new WaitForSecondsRealtime(.55f);
            var views = FindObjectsByType<MonsterView>(FindObjectsSortMode.None).OrderBy(v => v.Root.anchoredPosition.x).ToArray();
            var positions = count == 1 ? new[] { Vector2.zero } : count == 2 ? new[] { new Vector2(-270, -35), new Vector2(270, -35) } : new[] { new Vector2(-370, -65), Vector2.zero, new Vector2(370, -65) };
            Check(views.Length == count && views.Select(v => v.Root.anchoredPosition - Vector2.up * (-goblin.FootprintY) * v.Root.localScale.x).SequenceEqual(positions), count + " default hitbox bottoms use saved formation: " + string.Join(", ", views.Select(v => v.Root.anchoredPosition)));
            Check(scene.PlayerPosition == new Vector2(0, -532), count + " foreground player remains at -532");
            var playerImage = Ref<Image>(FindFirstObjectByType<PlayerView>(), "body");
            var enemyImage = views[0].transform.Find("Art").GetComponent<Image>();
            Check(enemyImage.canvasRenderer.absoluteDepth < playerImage.canvasRenderer.absoluteDepth, count + " enemies remain behind foreground player");
            yield return Capture("Formation-" + count);
        }
        // The lowered left target must expose weak points and accept the real slash there.
        var enemy = combat.Enemies[0]; enemy.StartCharge(enemy.Data.BodyCenterFromPlacement(scene.GetMonsterFormationPosition(0, 3), enemy.VisualScale)); yield return null;
        var weak = enemy.WeakPoints.FirstOrDefault(p => p.IsActive && !p.IsOptional);
        Check(weak != null, "left lowered target exposes its charge weak point");
        if (weak != null)
        {
            var position = weak.HitShape.Center;
            var screen = RectTransformUtility.WorldToScreenPoint(scene.UiCamera, scene.LogicalArea.TransformPoint(position));
            Check(scene.TryScreenToLogical(screen, scene.UiCamera, out var logical) && Vector2.Distance(position, logical) < 1f, "lowered weak point screen/input mapping follows actual target");
            var remaining = weak.RemainingHits; combat.Player.ResetAttackCooldown();
            combat.ExecuteAttack(new AttackSegment(position + Vector2.left * 100, position + Vector2.right * 100, 40), true); yield return null;
            Check(weak.RemainingHits < remaining, "charged slash hits lowered left weak point");
        }
        yield return Capture("WeakPoint");
        // Existing encounter data is deliberately not normalized to the three-goblin example.
        combat.ResetEncounter();
        var pair = UnityEditor.AssetDatabase.LoadAssetAtPath<EncounterData>("Assets/Data/Dungeon/Encounters/MazePair.asset");
        combat.PrepareFixedEncounter(combat.Run, pair, 1); yield return null;
        var pairViews = FindObjectsByType<MonsterView>(FindObjectsSortMode.None).OrderBy(v => v.Root.anchoredPosition.x).ToArray();
        Check(pairViews.Length == 2 && pairViews.Select(v => v.Root.anchoredPosition).SequenceEqual(pair.members.Select(m => m.monster.BodyCenterFromPlacement(m.offset, m.monster.combatScale)).OrderBy(p => p.x)), "authored two-species encounter offsets place the hitbox bottoms");
        combat.BeginPreparedCombat(); yield return new WaitForSecondsRealtime(.55f); yield return Capture("Authored-Pair");
        combat.ResetEncounter();
        var flying = UnityEditor.AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/Combat/Monsters/Data_Monster_B2_Minion_FrostBat.asset");
        combat.PrepareEncounter(combat.Run, new[] { flying }, 2); combat.BeginPreparedCombat(); yield return new WaitForSecondsRealtime(.55f);
        var flyingView = FindFirstObjectByType<MonsterView>();
        Check(flyingView.Root.anchoredPosition == flying.BodyCenterFromPlacement(Vector2.zero, flyingView.Root.localScale.x), "flying species uses authored hitbox-bottom placement: " + flyingView.Root.anchoredPosition);
        yield return Capture("Flying");
        combat.ResetEncounter();
        File.AppendAllText(Folder + "Live.txt", "COMPLETE failures=" + failures + ". Runtime-only practice clones; no saved combat/balance data changed. Real device pending.\n");
        UnityEditor.EditorApplication.isPlaying = false;
    }
}
#endif
