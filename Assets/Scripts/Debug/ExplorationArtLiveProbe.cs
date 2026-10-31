#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonSlash;
using UnityEngine;
using UnityEngine.EventSystems;

// Isolated visual fixtures only. No changes to production generation, event weighting, or run saves.
public sealed class ExplorationArtLiveProbe : MonoBehaviour
{
    const string Folder = "Docs/Production/Exploration-2026-10-31/BackgroundRevision/Runtime/";
    bool previousBackground, hadHistory;
    string previousHistory;
    public void Run() => StartCoroutine(Review());
    void Awake()
    {
        previousBackground = Application.runInBackground; Application.runInBackground = true;
        hadHistory = PlayerPrefs.HasKey("Behemoth.EventHistory.v1"); previousHistory = PlayerPrefs.GetString("Behemoth.EventHistory.v1", "");
    }
    void OnDestroy()
    {
        Application.runInBackground = previousBackground;
        if (hadHistory) PlayerPrefs.SetString("Behemoth.EventHistory.v1", previousHistory); else PlayerPrefs.DeleteKey("Behemoth.EventHistory.v1");
    }
    IEnumerator Review()
    {
        Directory.CreateDirectory(Folder); File.WriteAllText(Folder + "Progress.txt", "Capturing actual Unity rendered frames.\n");
        if (File.Exists(Folder + "Complete.txt")) File.Delete(Folder + "Complete.txt");
        var flow = FindFirstObjectByType<GameFlowController>(); var run = FindFirstObjectByType<RunController>();
        var view = FindFirstObjectByType<ExplorationView>(); var backdrop = FindFirstObjectByType<StageBackdropView>();
        var expansion = FindFirstObjectByType<RunExpansionView>();
        var dungeon = FindFirstObjectByType<DungeonController>();
        var navigation = FindFirstObjectByType<NavigationChoiceView>();
        flow.StartNewRun(); yield return null; expansion.HideModal();
        typeof(GameFlowController).GetMethod("ShowNavigation", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(flow, null);
        backdrop.enabled = false; view.SetBackdrop(backdrop); view.SetVisible(true);
        if (view.narration != null) view.narration.alpha = 0;
        var objects = UnityEditor.AssetDatabase.FindAssets("t:RoomObjectData", new[] { "Assets/Data/Presentation/Exploration" })
            .Select(g => UnityEditor.AssetDatabase.LoadAssetAtPath<RoomObjectData>(UnityEditor.AssetDatabase.GUIDToAssetPath(g))).OrderBy(a => a.name).ToArray();
        var count = 0;
        var inputFailures = 0;
        File.WriteAllText(Folder + "Input.txt", "Actual live UI raycast at the center of every resized object; hover preserves environment color.\n");
        for (var act = 1; act <= 3; act++)
        {
            if (act > 1) dungeon.BeginNextFloor();
            run.State.CurrentAct = act; backdrop.Refresh(act, null);
            typeof(GameFlowController).GetMethod("ShowNavigation", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(flow, null);
            // Every closed/open wall combination: inspect seams and floor projection independently of props.
            for (var mask = 0; mask < 8; mask++)
            {
                view.Clear();
                var graph = new DungeonGraph(); var center = graph.AddRoom(new DungeonPosition(0, 0));
                for (var side = 0; side < 4; side++)
                {
                    if (side < 3 && (mask & (1 << side)) == 0) continue;
                    var absolute = DirectionUtility.ToAbsolute(FacingDirection.North, (RelativeDirection)side);
                    graph.Connect(center, graph.AddRoom(DirectionUtility.Offset(absolute)));
                }
                var room = new DungeonRunState(0, graph, act);
                navigation.Bind(room, _ => { }); backdrop.Refresh(act, room);
                Canvas.ForceUpdateCanvases(); yield return null; yield return new WaitForEndOfFrame();
                var capture = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Folder + $"B{act}-Room-Mask{mask}.jpg", capture.EncodeToJPG(95)); Destroy(capture);
            }
            for (var d = 0; d < backdrop.doorLayers.Length; d++) backdrop.doorLayers[d].enabled = false;
            foreach (var art in objects) foreach (var direction in art.wallMounted ? new[] { 0, 1, 3 } : new[] { 0, 1, 2, 3 })
            {
                foreach (var used in new[] { false, true })
                {
                    if (used && art.Painting(direction, true) == art.Painting(direction, false)) continue;
                    view.Clear();
                    // The fixture uses actual wall/passage rules: no navigation button can exist on the attachment wall.
                    var fixture = new DungeonGraph(); var center = fixture.AddRoom(new DungeonPosition(0, 0));
                    var attachment = direction == 0 ? RelativeDirection.Forward : direction == 1 ? RelativeDirection.Left : RelativeDirection.Right;
                    foreach (RelativeDirection relative in Enum.GetValues(typeof(RelativeDirection)))
                    {
                        var absolute = DirectionUtility.ToAbsolute(FacingDirection.North, relative);
                        var neighbor = fixture.AddRoom(DirectionUtility.Offset(absolute));
                        if (art.wallMounted && relative == attachment && !art.fitsDoorway) continue;
                        fixture.Connect(center, neighbor);
                        if (art.fitsDoorway && relative == attachment) fixture.Passages.Add(new DungeonPassage(center, neighbor, false, true));
                    }
                    var fixtureState = new DungeonRunState(0, fixture, act);
                    navigation.Bind(fixtureState, _ => { }); backdrop.Refresh(act, fixtureState);
                    var anchor = art.wallMounted ? view.Wall(direction == 0 ? RelativeDirection.Forward : direction == 1 ? RelativeDirection.Left : RelativeDirection.Right) : view.Corner(direction);
                    var scale = art.wallMounted ? direction == 0 ? .80f : .95f : ExplorationView.Perspective(anchor);
                    if (art == FindFirstObjectByType<DungeonController>().Exploration.mechanism)
                        anchor = view.Switch(direction == 0 ? RelativeDirection.Forward : direction == 1 ? RelativeDirection.Left : RelativeDirection.Right);
                    if (art == dungeon.Exploration.key) { anchor = view.KeyCorner(direction); scale = ExplorationView.Perspective(anchor); }
                    view.Add(art, direction, used, anchor, scale, false, () => { });
                    Canvas.ForceUpdateCanvases(); yield return null; yield return new WaitForEndOfFrame();
                    var name = $"B{act}-{art.name}-D{direction}-{(used ? "Used" : "Available")}";
                    var slot = view.slots.First(s => s.gameObject.activeInHierarchy && s.BoundData == art);
                    var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(FindFirstObjectByType<CombatSceneView>().UiCamera,
                        slot.hitArea.TransformPoint(slot.hitArea.rect.center)) };
                    var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
                    var hit = hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) == slot.button.gameObject;
                    var tint = slot.painting.color; slot.OnPointerEnter(pointer); var steady = slot.painting.color == tint; slot.OnPointerExit(pointer);
                    if (!hit || !steady) inputFailures++;
                    File.AppendAllText(Folder + "Input.txt", (hit && steady ? "PASS " : "FAIL ") + name + "; top=" + (hits.Count == 0 ? "none" : hits[0].gameObject.name) + "; steadyTint=" + steady + "\n");
                    var capture = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Folder + name + ".jpg", capture.EncodeToJPG(90)); Destroy(capture);
                    File.AppendAllText(Folder + "Progress.txt", name + "\n"); count++;
                }
            }
        }
        File.WriteAllText(Folder + "Complete.txt", count + " actual Unity frames; all room objects, visible directions and available/used states, B1–B3. Input/color failures=" + inputFailures + ".\n");
        backdrop.enabled = true; UnityEditor.EditorApplication.isPlaying = false;
    }
}
#endif
