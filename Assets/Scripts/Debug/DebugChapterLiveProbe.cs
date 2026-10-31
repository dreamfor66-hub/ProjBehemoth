#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonSlash;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class DebugChapterLiveProbe : MonoBehaviour
{
    const string Root = "Docs/Production/DebugChapter-2026-10-31/";
    public bool smoke;
    string Folder => Root + (smoke ? "Trial/" : "");
    BaseInputModule[] physicalModules;
    int checks, failures;
    bool previousBackground, hadHistory;
    string history;
    void Awake()
    {
        previousBackground = Application.runInBackground; Application.runInBackground = true;
        hadHistory = PlayerPrefs.HasKey("Behemoth.EventHistory.v1"); history = PlayerPrefs.GetString("Behemoth.EventHistory.v1", "");
        // The user may be typing in other apps. Exercise real UI raycasts/handlers without
        // unrelated hardware submit/navigation events racing these explicit test clicks.
        physicalModules = EventSystem.current.GetComponents<BaseInputModule>().Where(m => m.enabled).ToArray();
        foreach (var module in physicalModules) module.enabled = false;
    }
    void OnDestroy()
    {
        Application.runInBackground = previousBackground;
        if (hadHistory) PlayerPrefs.SetString("Behemoth.EventHistory.v1", history); else PlayerPrefs.DeleteKey("Behemoth.EventHistory.v1");
        foreach (var module in physicalModules) if (module != null) module.enabled = true;
    }
    void Check(bool ok, string message)
    { checks++; if (!ok) failures++; File.AppendAllText(Folder + "Live.txt", (ok ? "PASS " : "FAIL ") + message + "\n"); }
    bool Click(Button button)
    {
        Canvas.ForceUpdateCanvases();
        var canvas = button.GetComponentInParent<Canvas>().rootCanvas;
        var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left,
            position = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                button.transform.TransformPoint(((RectTransform)button.transform).rect.center)) };
        var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
        var reachable = hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) == button.gameObject;
        Check(reachable && button.IsInteractable(), "button reachable: " + button.name + "; top=" + (hits.Count == 0 ? "none" : hits[0].gameObject.name));
        if (!reachable || !button.IsInteractable()) return false;
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler); return true;
    }
    static T Ref<T>(object value, string name) => (T)value.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(value);
    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame(); var texture = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Folder + name + ".jpg", texture.EncodeToJPG(92)); Destroy(texture);
    }
    public void Run() => StartCoroutine(Review());
    IEnumerator Review()
    {
        Directory.CreateDirectory(Folder); File.WriteAllText(Folder + "Live.txt", "Scoped F1 chapter / object placement runtime matrix.\n");
        if (File.Exists(Folder + "Complete.txt")) File.Delete(Folder + "Complete.txt");
        var flow = FindFirstObjectByType<GameFlowController>(); var run = FindFirstObjectByType<RunController>();
        var dungeon = FindFirstObjectByType<DungeonController>(); var combat = FindFirstObjectByType<CombatController>();
        var overlay = FindFirstObjectByType<DebugCheatOverlay>(); var view = FindFirstObjectByType<ExplorationView>();
        var backdrop = FindFirstObjectByType<StageBackdropView>();
        flow.StartNewRun(); yield return null;
        overlay.TogglePanel(); Check(overlay.IsOpen, "F1 toggle opens saved panel");
        yield return new WaitForEndOfFrame();
        Check(overlay.categoryButtons.Length == 5 && overlay.roomObjectCatalog.Count == 17, "five tabs and all 17 authored objects");
        for (var tab = 0; tab < 4; tab++)
        { Click(overlay.categoryButtons[tab]); yield return null; Check(!overlay.chapterTools.activeSelf && overlay.catalogScroll.content.GetComponentsInChildren<DebugCatalogTile>().Any(), "existing catalog " + tab + " still usable"); }
        Click(overlay.categoryButtons[4]); yield return null;
        Check(overlay.chapterTools.activeSelf && !overlay.grantButton.gameObject.activeSelf, "chapter controls replace grant action");
        var textFits = overlay.chapterTools.GetComponentsInChildren<TMP_Text>().All(t => { t.ForceMeshUpdate(); return !t.isTextOverflowing; });
        Check(textFits, "all chapter labels fit at actual Game view size"); yield return Capture("ChapterTab");
        var player = run.State.Player; var level = run.State.Level; var gold = run.State.Gold;
        foreach (var act in new[] { 3, 1, 2, 2, 1 })
        {
            var old = dungeon.State;
            foreach (var room in old.Graph.Rooms) { room.IsVisited = room.IsCleared = true; }
            old.Keys.Add(937); run.State.NormalCombatsInAct = 9; run.State.Fatigue.Step();
            overlay.SetOpen(true); overlay.SelectCategory(4); yield return new WaitForEndOfFrame(); Click(overlay.chapterButtons[act - 1]); yield return new WaitForEndOfFrame();
            Check(dungeon.State != old && dungeon.Floor == act && dungeon.State.Floor == act && run.State.CurrentAct == act, "act " + act + " fresh graph and synchronized chapter");
            Check(dungeon.State.Player.CurrentRoom == dungeon.State.Graph.StartRoom && dungeon.State.Graph.Rooms.Count(r => r.IsVisited) == 1
                && dungeon.State.Player.CurrentRoom.IsCleared && dungeon.State.Keys.Count == 0, "act " + act + " reset visits/clear/keys and starts at entrance");
            Check(run.State.NormalCombatsInAct == 0 && run.State.Fatigue.Steps == 0 && run.State.Fatigue.Value == 100 && !run.State.Fatigue.CampUsed, "act " + act + " fresh floor counters and fatigue");
            Check(player == run.State.Player && level == run.State.Level && gold == run.State.Gold, "act " + act + " preserves player progression");
            var expectedBackground = backdrop.artwork != null && backdrop.artwork.Length > 0
                ? backdrop.artwork[Mathf.Clamp(act - 1, 0, backdrop.artwork.Length - 1)].walls
                : backdrop.stages[Mathf.Clamp(act - 1, 0, backdrop.stages.Length - 1)];
            Check(!overlay.IsOpen && flow.State == GameFlowState.DungeonNavigation && !combat.IsActive && !combat.HasPreparedMonsters
                && backdrop.background.sprite == expectedBackground, "act " + act + " live navigation, configured background, no battle residue");
            yield return Capture("Act" + act + "-Start");
        }
        foreach (var act in new[] { 1, 2, 3 })
        {
            overlay.SetOpen(true); overlay.SelectCategory(4); yield return new WaitForEndOfFrame(); Click(overlay.chapterButtons[act - 1]); yield return new WaitForEndOfFrame();
            foreach (var art in smoke ? overlay.roomObjectCatalog.Take(1) : overlay.roomObjectCatalog) foreach (var used in new[] { false, true })
            {
                overlay.SetOpen(true); overlay.SelectCategory(4); yield return new WaitForEndOfFrame();
                var cells = overlay.catalogScroll.content.GetComponentsInChildren<DebugCatalogTile>();
                var index = overlay.roomObjectCatalog.IndexOf(art); Click(cells[index].button);
                if (Ref<bool>(overlay, "objectUsed") != used) Click(overlay.objectStateButton);
                Click(overlay.objectDirectionButtons[0]); Click(overlay.placeObjectButton); yield return new WaitForEndOfFrame();
                var passage = dungeon.State.Graph.Passages.FirstOrDefault();
                var shouldShow = !used || art != dungeon.Exploration.key && !flow.DebugIsPassageArtwork(art);
                var slot = view.slots.FirstOrDefault(s => s.gameObject.activeInHierarchy && s.BoundData == art);
                Check(flow.DebugInspectionArt == art && !overlay.IsOpen && dungeon.State.Graph.Rooms.Count == 5
                    && flow.State == GameFlowState.DungeonNavigation, $"B{act} {art.name} {used}: actual isolated placement");
                Check(shouldShow ? slot != null && (art.fitsDoorway || slot.painting.sprite == art.Painting(0, used)) : slot == null,
                    $"B{act} {art.name} {used}: actual chosen painting/state");
                if (passage != null) Check(used ? passage.Discovered && !passage.Locked : passage.Locked, "gate reflects selected state");
                if (act == 3 && !used) yield return Capture("Object-" + art.name);
            }
        }
        // Side-wall placement must suppress a doorway and reject the invisible rear wall.
        var goddess = dungeon.Exploration.For(RoomEncounterType.Goddess);
        overlay.SetOpen(true); overlay.SelectCategory(4); yield return new WaitForEndOfFrame();
        Click(overlay.catalogScroll.content.GetComponentsInChildren<DebugCatalogTile>()[overlay.roomObjectCatalog.IndexOf(goddess)].button);
        Check(!overlay.objectDirectionButtons[2].interactable, "rear wall unavailable because it is the entry");
        foreach (var direction in new[] { 1, 3 })
        {
            overlay.SetOpen(true); yield return new WaitForEndOfFrame(); if (Ref<bool>(overlay, "objectUsed")) Click(overlay.objectStateButton);
            Click(overlay.objectDirectionButtons[direction]); Click(overlay.placeObjectButton); yield return new WaitForEndOfFrame();
            var slot = view.slots.FirstOrDefault(s => s.gameObject.activeInHierarchy && s.BoundData == goddess);
            Check(slot != null && slot.painting.sprite == goddess.Painting(direction, false), "wall side painting " + direction);
            yield return Capture("Goddess-D" + direction);
        }
        // An ordinary service really opens its normal popup; jumping cancels that flow.
        overlay.SetOpen(false); flow.DebugPlaceRoomObject(dungeon.Exploration.For(RoomEncounterType.Fountain), 1, false); yield return new WaitForEndOfFrame();
        var fountain = view.slots.First(s => s.gameObject.activeInHierarchy && s.BoundData == dungeon.Exploration.For(RoomEncounterType.Fountain));
        Click(fountain.button); yield return null; Check(flow.State == GameFlowState.RoomEvent, "placed fountain opens actual choices");
        overlay.SetOpen(true); overlay.SelectCategory(4); yield return new WaitForEndOfFrame(); Click(overlay.chapterButtons[1]); yield return new WaitForEndOfFrame();
        Check(flow.State == GameFlowState.DungeonNavigation && !Ref<GameObject>(Ref<RoomEventView>(flow, "roomEventView"), "panel").activeSelf, "chapter jump cancels service popup");
        combat.ResetEncounter(); flow.StartCheatBattle(overlay.monsterCatalog[0]); yield return null;
        overlay.SetOpen(true); yield return new WaitForEndOfFrame(); Click(overlay.chapterButtons[2]); yield return new WaitForEndOfFrame();
        Check(flow.State == GameFlowState.DungeonNavigation && !combat.HasPreparedMonsters, "chapter jump cancels pending battle");
        run.State.Player.TakeEnvironmentalDamage(run.State.Player.MaxHp * 10);
        overlay.SetOpen(true); yield return new WaitForEndOfFrame(); Click(overlay.chapterButtons[0]); yield return new WaitForEndOfFrame();
        Check(run.State.Player.IsAlive && run.State.Player.CurrentHp == run.State.Player.MaxHp, "jump restores health even from dead player");
        Check(combat.pressureFeedback == null || combat.pressureFeedback.danger.intensity == 0, "chapter jump clears previous damage wash");
        overlay.SetOpen(true); overlay.SelectCategory(4); yield return new WaitForEndOfFrame(); Click(overlay.clearObjectButton); yield return new WaitForEndOfFrame();
        Check(view.VisibleObjects == 0 && flow.DebugInspectionArt == null, "clear inspection removes objects");
        var drawers = FindFirstObjectByType<ArcadeHudController>(); drawers.mapPanel.SetActive(true);
        overlay.SetOpen(true); yield return new WaitForEndOfFrame(); Click(overlay.chapterButtons[0]); yield return new WaitForEndOfFrame();
        Check(!drawers.mapPanel.activeSelf && !drawers.inventoryPanel.activeSelf, "chapter jump closes map/inventory drawers");
        Check(dungeon.State.Graph.Rooms.Count > 5 && flow.DebugInspectionArt == null, "chapter jump leaves inspection and regenerates ordinary map");
        overlay.SetOpen(true); overlay.SelectCategory(4); yield return Capture("Final-ChapterTab"); overlay.SetOpen(false);
        File.WriteAllText(Folder + "Complete.txt", checks + " checks; " + failures + " failures. Actual Unity UI raycasts/click handlers, chapter resets; "
            + (smoke ? "saved Trial scene bindings and representative object placement." : "all 17 objects in both states across acts 1–3.") + "\n");
        UnityEditor.EditorApplication.isPlaying = false;
    }
}
#endif
