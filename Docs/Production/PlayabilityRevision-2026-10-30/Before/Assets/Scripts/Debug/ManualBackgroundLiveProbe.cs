#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DungeonSlash;
using UnityEngine;

// Explicit local verification only. All inspector test values are runtime-only and restored.
public sealed class ManualBackgroundLiveProbe : MonoBehaviour
{
    const string Folder = "Docs/Production/ManualBackground-2026-10-27/";
    StageBackdropView backdrop;
    RectTransform rect;
    Vector2 originalMin, originalMax, originalSize, originalPosition;
    Vector3 originalScale;
    int originalFloor, failures;
    bool captured, previousBackground;
    static T Ref<T>(object owner, string field) => (T)owner.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
    void Awake() { previousBackground = Application.runInBackground; Application.runInBackground = true; }
    void Restore()
    {
        if (!captured || rect == null) return;
        rect.anchorMin = originalMin; rect.anchorMax = originalMax; rect.sizeDelta = originalSize;
        rect.anchoredPosition = originalPosition; rect.localScale = originalScale;
        if (backdrop != null && backdrop.run?.State != null)
        {
            backdrop.run.State.CurrentAct = originalFloor;
            backdrop.Refresh(originalFloor, backdrop.dungeon?.State);
        }
        captured = false;
    }
    void OnDestroy() { Restore(); Application.runInBackground = previousBackground; }
    void Check(bool ok, string message)
    {
        File.AppendAllText(Folder + "Live.txt", (ok ? "PASS " : "FAIL ") + message + "\n");
        if (!ok) { failures++; Debug.LogError("Manual background: " + message); }
    }
    void CheckLayout(string context)
    {
        Check(rect.anchorMin == new Vector2(.5f, .5f) && rect.anchorMax == rect.anchorMin, context + " manual anchors retained");
        Check(rect.sizeDelta == new Vector2(1234, 730), context + " manual width/height retained");
        Check(rect.localScale == new Vector3(1.12f, .94f, 1), context + " manual scale retained");
        Check(rect.anchoredPosition == new Vector2(37, -41), context + " manual position retained");
    }
    public void Run(bool stopAfter, bool wasPaused) => StartCoroutine(Review(stopAfter, wasPaused));
    IEnumerator Review(bool stopAfter, bool wasPaused)
    {
        Directory.CreateDirectory(Folder); File.WriteAllText(Folder + "Live.txt", "Existing Unity Editor only. Manual RectTransform through frames, floors, walking and reset.\n");
        yield return null; yield return null;
        backdrop = FindFirstObjectByType<StageBackdropView>(); rect = backdrop.background.rectTransform;
        if (backdrop.run.State == null) FindFirstObjectByType<GameFlowController>().StartNewRun();
        originalMin = rect.anchorMin; originalMax = rect.anchorMax; originalSize = rect.sizeDelta;
        originalPosition = rect.anchoredPosition; originalScale = rect.localScale; originalFloor = backdrop.run.State.CurrentAct; captured = true;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(1234, 730);
        rect.anchoredPosition = new Vector2(37, -41); rect.localScale = new Vector3(1.12f, .94f, 1);
        for (var frame = 0; frame < 12; frame++) yield return null;
        CheckLayout("12 LateUpdate frames");
        for (var floor = 1; floor <= backdrop.artwork.Length; floor++)
        {
            backdrop.run.State.CurrentAct = floor; yield return null; yield return null;
            CheckLayout("floor " + floor);
            Check(backdrop.background.sprite == backdrop.artwork[floor - 1].walls, "floor " + floor + " artwork swapping still works");
        }
        var transition = FindFirstObjectByType<RoomTransitionView>();
        transition.Configure(rect, Ref<RectTransform>(transition, "walkBackdrop"), Ref<List<RectTransform>>(transition, "footfallMarkers"));
        CheckLayout("transition configure/reset");
        var walk = StartCoroutine(transition.Play()); yield return new WaitForSecondsRealtime(.2f);
        CheckLayout("during walking transition"); yield return walk;
        CheckLayout("after walking transition/reset");
        Restore(); Check(!captured, "test inspector layout and floor restored without saving assets");
        File.AppendAllText(Folder + "Live.txt", "COMPLETE failures=" + failures + ". No combat/balance suite.\n");
        if (stopAfter) UnityEditor.EditorApplication.isPlaying = false;
        else { UnityEditor.EditorApplication.isPaused = wasPaused; Destroy(gameObject); }
    }
}
#endif
