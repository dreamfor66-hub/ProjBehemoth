#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Scoped to the changed state-image behavior in the existing live Editor.
public sealed class MonsterStunLiveProbe : MonoBehaviour
{
    const string Folder = "Docs/Production/MonsterStun-2026-10-30/";
    [Serializable] sealed class Plan { public Entry[] actors; }
    [Serializable] sealed class Entry { public string monster; }
    int checks, failures;
    bool background;
    void Awake() { background = Application.runInBackground; Application.runInBackground = true; }
    void OnDestroy() => Application.runInBackground = background;
    public void Run() => StartCoroutine(Review());
    void Check(bool ok, string message)
    {
        checks++; if (!ok) failures++;
        File.AppendAllText(Folder + "Live.txt", (ok ? "PASS " : "FAIL ") + message + "\n");
    }
    static bool Shows(MonsterView view, CombatVisualState state, Sprite sprite)
    {
        var animator = view.GetComponentInChildren<CombatSpriteAnimator>();
        var image = animator != null ? animator.GetComponent<Image>() : null;
        return animator != null && animator.State == state && image != null && image.enabled && image.sprite == sprite;
    }
    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        var frame = FindFirstObjectByType<ArcadeRasterCamera>()?.Frame;
        if (frame == null) yield break;
        var previous = RenderTexture.active; RenderTexture.active = frame;
        var image = new Texture2D(frame.width, frame.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0); image.Apply();
        File.WriteAllBytes(Folder + name + ".png", image.EncodeToPNG());
        Destroy(image); RenderTexture.active = previous;
    }
    IEnumerator Review()
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder + "Live.txt", "Existing live Unity Editor; 12 act-one state transitions, no balance suite. Runtime-only clones.\n");
        var practice = FindFirstObjectByType<OctoberReviewLiveProbe>() ?? new GameObject("Stun artwork practice").AddComponent<OctoberReviewLiveProbe>();
        practice.Setup(CharacterKind.Warrior, false); yield return null;
        var combat = FindFirstObjectByType<CombatController>();
        var scene = FindFirstObjectByType<CombatSceneView>();
        var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(Folder + "Install.json"));
        foreach (var entry in plan.actors)
        {
            var original = DungeonSlash.DesignerAssetPaths.Load<MonsterData>(entry.monster);
            var data = Instantiate(original);
            data.maxHp = 50000; data.normalAttackInterval = data.chargeInterval = 999;
            // The linked minion is checked alone without triggering leader-death cleanup.
            data.diesWithLeader = false;
            combat.ResetEncounter(); combat.PrepareEncounter(combat.Run, new[] { data }, 1); combat.BeginPreparedCombat();
            yield return null; yield return null;
            var view = FindFirstObjectByType<MonsterView>(); var enemy = combat.Enemies[0];
            var library = data.combatAnimation; var id = original.name;
            Check(library.stunned != null && library.stunned != library.idle && library.stunned != library.dead, id + " distinct stun asset");
            Check(library.stunned != null && library.stunned.rect.width > 0 && library.stunned.rect.height > 0
                && library.stunned.vertices.Length == 4 && !float.IsNaN(library.stunned.pivot.y) && !float.IsInfinity(library.stunned.pivot.y),
                id + " saved frame has valid native geometry and pivot");
            Check(Shows(view, CombatVisualState.Idle, library.idle), id + " initial Idle");
            var scale = view.Root.localScale;
            enemy.BeginNormalAttack(); view.PlayAttack();
            enemy.EnterStun(); view.Bind(enemy);
            Check(Shows(view, CombatVisualState.Stunned, library.stunned), id + " stun immediately interrupts active Attack painting");
            yield return new WaitForSecondsRealtime(.4f);
            Check(enemy.IsStunned && Shows(view, CombatVisualState.Stunned, library.stunned), id + " stun persists across production update frames");
            Check(view.Root.localScale == scale, id + " species scale preserved");
            Check(Mathf.Abs(-library.stunned.pivot.y * MonsterData.ArtworkUnitsPerWorldUnit / library.stunned.pixelsPerUnit - data.FootprintY) < .1f,
                id + " static stun frame shares authored hitbox ground baseline");
            Check(view.GetComponentInChildren<CombatSpriteAnimator>().GetComponent<Image>().color == Color.white, id + " authored colors preserved");
            yield return Capture(id + "-Stunned-Live");
            enemy.Tick(data.stunDuration + .01f, Vector2.zero); view.Bind(enemy);
            Check(Shows(view, CombatVisualState.Idle, library.idle), id + " recovery restores Idle without stale Attack");
            enemy.StartCharge(Vector2.zero); view.Bind(enemy);
            Check(Shows(view, CombatVisualState.Ready, library.ready), id + " Ready still works after recovery");
            enemy.EnterStun(); enemy.TakeDamage(float.MaxValue); view.Bind(enemy);
            Check(Shows(view, CombatVisualState.Dead, library.dead), id + " Dead takes priority over Stunned");
            combat.ResetEncounter(); Destroy(data);
        }
        // Null-safe compatibility for monsters outside the authored act-one scope.
        var fallback = ScriptableObject.CreateInstance<CombatSpriteAnimation>();
        var sample = DungeonSlash.DesignerAssetPaths.Load<MonsterData>(plan.actors[0].monster);
        fallback.idle = sample.combatAnimation.idle;
        Check(fallback.For(CombatVisualState.Stunned) == fallback.idle, "unassigned stun falls back to Idle");
        Destroy(fallback);
        File.AppendAllText(Folder + "Live.txt", "COMPLETE checks=" + checks + " failures=" + failures + "\n");
        File.WriteAllText(Folder + "LiveComplete.txt", "checks=" + checks + " failures=" + failures + "\n");
    }
}
#endif
