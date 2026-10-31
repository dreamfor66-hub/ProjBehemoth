#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEngine;
using UnityEngine.UI;

// Explicitly scoped visual review of the replaced monster sprites in the existing Editor.
public sealed class MonsterArtPolishLiveProbe : MonoBehaviour
{
    const string Folder = "Docs/Production/MonsterPolish-2026-10-26/";
    [Serializable] sealed class Row { public string id, baseline, name, hint; }
    [Serializable] sealed class Manifest { public Row[] items; }
    bool wasBackground;
    void Awake() { wasBackground = Application.runInBackground; Application.runInBackground = true; }
    void OnDestroy() => Application.runInBackground = wasBackground;
    public void Run(string target = "") => StartCoroutine(Review(target));
    IEnumerator Review(string target)
    {
        Directory.CreateDirectory(Folder + "Screenshots");
        var log = Folder + (string.IsNullOrEmpty(target) ? "Live" : "Live-" + target) + ".txt";
        File.WriteAllText(log, "Scoped live visual review of monster artwork; no balance suite.\n");
        var rows = JsonUtility.FromJson<Manifest>(File.ReadAllText(Folder + "Manifest.json")).items;
        var practice = FindFirstObjectByType<OctoberReviewLiveProbe>() ?? new GameObject("Monster art practice").AddComponent<OctoberReviewLiveProbe>();
        var failures = 0;
        var targets = target.Split(',');
        foreach (var row in rows.Where(r => string.IsNullOrEmpty(target) || targets.Contains(r.id)))
        {
            practice.Setup(CharacterKind.Warrior, false, "Assets/Data/Combat/Monsters/" + row.baseline + ".asset");
            yield return null;
            var combat = FindFirstObjectByType<CombatController>(); combat.InputPaused = true;
            var data = combat.Enemies[0].Data;
            var views = FindObjectsByType<MonsterView>(FindObjectsSortMode.None);
            var arts = views.SelectMany(v => v.GetComponentsInChildren<Image>()).Where(i => i.sprite != null && i.sprite == data.portrait).ToArray();
            var ok = data.displayName == row.name && data.portrait != null && data.combatAnimation.idle == data.portrait && arts.Length > 0;
            if (!ok) failures++;
            File.AppendAllText(log, (ok ? "PASS " : "FAIL ") + row.id + ": localized name, production sprite and visible combat art.\n");
            yield return new WaitForEndOfFrame();
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Folder + "Screenshots/" + row.id + ".png", shot.EncodeToPNG()); Destroy(shot);
        }
        File.AppendAllText(log, failures == 0 ? "COMPLETE\n" : "FAILED\n");
        File.WriteAllText(Folder + "Live-Complete.txt", failures.ToString());
    }
}
#endif
