#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Opt-in visual verification in the existing Editor; never changes saved combat data.
public sealed class CombatFramingLiveProbe : MonoBehaviour
{
    const string Folder = "Docs/Production/CombatFraming-2026-10-26/";
    public void Run() => StartCoroutine(Review());
    void Check(bool ok, string message)
    {
        File.AppendAllText(Folder + "Live.txt", (ok ? "PASS " : "FAIL ") + message + "\n");
        if (!ok) Debug.LogError("Combat framing: " + message);
    }
    void Capture(string name)
    {
        // Capture the final output: TMP is composed after the artwork framebuffer.
        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Folder + "Screenshots/" + name + ".png", texture.EncodeToPNG());
        Object.Destroy(texture);
    }
    IEnumerator Review()
    {
        Directory.CreateDirectory(Folder + "Screenshots");
        File.WriteAllText(Folder + "Live.txt", "Scoped depth, target relocation and six HUD portrait checks. Existing Editor; runtime practice clones.\n");
        var practice = Object.FindFirstObjectByType<OctoberReviewLiveProbe>() ?? new GameObject("Framing practice").AddComponent<OctoberReviewLiveProbe>();
        foreach (CharacterKind kind in Enum.GetValues(typeof(CharacterKind)))
        {
            practice.Setup(kind, false); yield return null; yield return new WaitForEndOfFrame();
            var scene = Object.FindFirstObjectByType<CombatSceneView>();
            var hud = Object.FindFirstObjectByType<FatigueHudView>();
            var monster = Object.FindFirstObjectByType<MonsterView>();
            var player = Object.FindFirstObjectByType<PlayerView>();
            var enemyArt = monster.transform.Find("Art").GetComponent<Image>();
            var playerArt = player.GetComponentInChildren<CombatSpriteAnimator>().GetComponent<Image>();
            Check(enemyArt.canvasRenderer.absoluteDepth < playerArt.canvasRenderer.absoluteDepth, kind + " monster renders behind hero");
            Check(scene.MonsterPosition == new Vector2(0,-150), kind + " enemy target anchor matches lowered root (0,-150)");
            Check(hud.face.sprite != null && UnityEditor.AssetDatabase.GetAssetPath(hud.face.sprite).StartsWith("Assets/Art/UI/Portraits/Angular/"), kind + " face crop bound");
            var frame = (RectTransform)hud.transform;
            Check(frame.anchorMin == Vector2.zero && frame.anchorMax == Vector2.one && frame.offsetMin == Vector2.zero && frame.offsetMax == Vector2.zero && hud.face.rectTransform.anchoredPosition == Vector2.zero, kind + " portrait flush at canvas left/top");
            Capture("Combat-" + kind);
        }
        practice.Setup(CharacterKind.Barbarian, false, "Assets/Data/Combat/Monsters/TunnelRat.asset");
        yield return null; yield return new WaitForEndOfFrame(); Capture("Combat-Scorpion");
        // Charge/hit geometry must follow the relocated target, without changing tuning.
        practice.Setup(CharacterKind.Barbarian, false); yield return null;
        var combat = Object.FindFirstObjectByType<CombatController>();
        practice.Charge(); yield return null; yield return new WaitForEndOfFrame();
        var enemy = combat.Enemies[0]; var weak = enemy.WeakPoints.FirstOrDefault(p => p.IsActive && !p.IsOptional);
        Check(weak != null, "raised enemy exposes charge weak point");
        if (weak != null)
        {
            var remaining = weak.RemainingHits;
            var scene = Object.FindFirstObjectByType<CombatSceneView>();
            var screen = RectTransformUtility.WorldToScreenPoint(scene.UiCamera, scene.LogicalArea.TransformPoint(weak.HitShape.Center));
            Check(scene.TryScreenToLogical(screen, scene.UiCamera, out var logical) && Vector2.Distance(logical, weak.HitShape.Center) < 1, "raised weak point screen/logical input mapping");
            combat.Player.ResetAttackCooldown();
            combat.ExecuteAttack(new AttackSegment(weak.HitShape.Center + Vector2.left * 100, weak.HitShape.Center + Vector2.right * 100, 40), true);
            yield return null;
            Check(weak.RemainingHits < remaining, "charge stroke hits raised weak point");
        }
        yield return new WaitForEndOfFrame(); Capture("Combat-WeakPoint");
        practice.Setup(CharacterKind.Barbarian, false); yield return null; yield return new WaitForEndOfFrame();
        File.AppendAllText(Folder + "Live.txt", "COMPLETE. No balance suite or unrelated interaction tests.\n");
    }
}
#endif
