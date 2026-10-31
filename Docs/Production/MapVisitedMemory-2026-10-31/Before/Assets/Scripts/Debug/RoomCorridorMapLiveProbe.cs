#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonSlash;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class RoomCorridorMapLiveProbe : MonoBehaviour
{
    const string Folder = "Docs/Production/RoomCorridorMap-2026-10-31/";
    DungeonController dungeon;
    DungeonMapView map;
    ArcadeHudController hud;
    bool background;
    int checks, failures;
    void Awake() { background = Application.runInBackground; Application.runInBackground = true; }
    void OnDestroy() => Application.runInBackground = background;
    void Check(bool ok, string text)
    { checks++; if (!ok) failures++; File.AppendAllText(Folder + "Live.txt", (ok ? "PASS " : "FAIL ") + text + "\n"); }
    public void Run() => StartCoroutine(Matrix());
    MapRoomIcon[] Icons() => map.GetComponentsInChildren<MapRoomIcon>().Where(i => i.gameObject.activeInHierarchy).ToArray();
    RectTransform Content => (RectTransform)map.transform.Find("MapContent");
    public void Status()
    {
        var current = dungeon.State.Player.CurrentRoom;
        File.WriteAllText(Folder + "ManualStatus.txt", $"room={current.RoomId}, position={current.Position}, facing={dungeon.State.Player.Facing}\npan={Content.anchoredPosition}, zoom={Content.localScale.x}, mapOpen={hud.mapPanel.activeSelf}\n");
    }
    public void CaptureNow(string name) => StartCoroutine(Capture(name));
    IEnumerator Capture(string name)
    {
        Canvas.ForceUpdateCanvases(); yield return new WaitForEndOfFrame();
        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Folder + name + ".png", texture.EncodeToPNG()); Destroy(texture);
    }
    IEnumerator Matrix()
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder + "Live.txt", "Existing Unity Editor. Scoped map layout, generation and navigation checks only. No combat/balance suite.\n");
        var flow = FindFirstObjectByType<GameFlowController>(); flow.StartNewRun(); yield return null;
        flow.StopAllCoroutines(); flow.enabled = false;
        FindFirstObjectByType<CombatController>().ResetEncounter();
        FindFirstObjectByType<RunExpansionView>().HideModal();
        dungeon = FindFirstObjectByType<DungeonController>(); hud = FindFirstObjectByType<ArcadeHudController>();
        // Suppress room encounter side effects while testing the production navigation callbacks.
        typeof(DungeonController).GetField("RoomEntered", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(dungeon, null);
        dungeon.BeginRun(20261031); dungeon.State.Graph.StartRoom.IsCleared = true;
        hud.mapButton.onClick.Invoke(); yield return null;
        map = hud.mapPanel.GetComponentInChildren<DungeonMapView>(true);
        Check(map.Layout != null && map.layoutRevision == 1, "saved map style assigned");
        var settings = UnityEditor.AssetDatabase.LoadAssetAtPath<DungeonGenerationSettings>("Assets/Data/Settings/Data_DungeonGenerationSettings_미궁_생성_규칙.asset");
        foreach (var floor in new[] { 1, 2, 3 })
        {
            var valid = true;
            foreach (var seed in new[] { 333, 5581, 8812, 9217, 20261031 })
            {
                var generated = new DungeonGenerator(settings).Generate(seed, floor);
                valid &= DungeonGraphValidator.HasExpectedStructure(generated, settings)
                    && generated.Graph.Rooms.Count(r => r.Type == RoomEncounterType.Event) == settings.eventRoomCount;
                foreach (var room in generated.Graph.Rooms)
                {
                    var p = room.Position;
                    valid &= !(generated.Graph.HasRoom(p + new DungeonPosition(1, 0))
                        && generated.Graph.HasRoom(p + new DungeonPosition(0, 1)) && generated.Graph.HasRoom(p + new DungeonPosition(1, 1)));
                    valid &= DirectionUtility.Offsets.All(offset => generated.Graph.At(p + offset) is not { } adjacent || room.Connections.Contains(adjacent.RoomId));
                }
                if (floor == 1) valid &= generated.Graph.Rooms.Count(r => r.Connections.Count >= 3) <= settings.firstActMaximumJunctions
                    && Enumerable.Range(1, settings.firstActOpeningCombats).All(i => generated.Graph.At(new DungeonPosition(0, i)).Type == RoomEncounterType.Combat);
            }
            Check(valid, "floor " + floor + ": 5 deterministic maps preserve room/event budgets, connectivity and opening; no packed blocks or touching unconnected routes");
        }
        var state = dungeon.State;
        var neighbours = state.Graph.Neighbors(state.Player.CurrentRoom).ToArray();
        Check(Icons().All(icon => icon.Room.IsVisited || icon.Room.IsRevealed || neighbours.Contains(icon.Room)), "fog exposes only known landmarks and adjacent destinations");
        Check(Icons().Where(i => i.Room.IsMajorRoom).All(i => ((RectTransform)i.transform).rect.width == map.Layout.roomSize), "revealed major destinations use large rooms");
        var next = Icons().First(i => neighbours.Contains(i.Room)); var target = next.Room;
        Check(next.transform.Find("Navigation Hit Area").GetComponent<Image>().raycastTarget
            && ((RectTransform)next.transform.Find("Navigation Hit Area")).rect.width >= 44, "small selectable cell has enlarged hit area");
        var canvas = map.GetComponentInParent<Canvas>(); var raycaster = canvas.GetComponent<GraphicRaycaster>();
        var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, next.transform.position) };
        var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
        Check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<MapRoomIcon>() == next, "real EventSystem raycast reaches small cell through its transparent hit area");
        yield return Capture("01-Start-Fog");
        next.OnPointerClick(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
        Check(state.Player.CurrentRoom == target && state.Player.Facing == FacingDirection.North, "cell selection uses actual connected-room navigation and facing");
        target.IsCleared = true; dungeon.RefreshNavigation(); yield return null;
        var beforeRoom = state.Player.CurrentRoom;
        var far = Icons().First(i => i.Room.IsMajorRoom && !state.Graph.Neighbors(beforeRoom).Contains(i.Room));
        far.OnPointerClick(new PointerEventData(EventSystem.current));
        Check(state.Player.CurrentRoom == beforeRoom, "distant known destination cannot teleport");
        var center = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, map.transform.position);
        var drag = new PointerEventData(EventSystem.current) { pointerId = 701, button = PointerEventData.InputButton.Left, position = center + new Vector2(80, 30), delta = new Vector2(80, 30), dragging = true,
            pointerPressRaycast = new RaycastResult { module = raycaster } };
        var oldPan = Content.anchoredPosition; map.OnBeginDrag(drag); map.OnDrag(drag);
        Check(Content.anchoredPosition != oldPan && state.Player.CurrentRoom == beforeRoom, "drag pans without moving player");
        var ownedPan = Content.anchoredPosition;
        var other = new PointerEventData(EventSystem.current) { pointerId = 702, position = center, delta = new Vector2(30, 30) };
        map.OnBeginDrag(other); map.OnDrag(other); map.OnEndDrag(other);
        Check(Content.anchoredPosition == ownedPan, "unrelated contact cannot move or end the owned drag");
        map.OnDrag(drag); Check(Content.anchoredPosition != ownedPan, "original contact continues after second contact lift");
        map.OnEndDrag(drag);
        var scroll = new PointerEventData(EventSystem.current) { position = center, scrollDelta = new Vector2(0, -100), pointerCurrentRaycast = new RaycastResult { module = raycaster } };
        map.OnScroll(scroll); Check(Mathf.Approximately(Content.localScale.x, .65f), "zoom-out is bounded and never auto-fits the map into tiny cells");
        scroll.scrollDelta = new Vector2(0, 100); map.OnScroll(scroll); Check(Mathf.Approximately(Content.localScale.x, 1.5f), "zoom-in is bounded");
        hud.mapPanel.transform.Find("MapCenter").GetComponent<Button>().onClick.Invoke();
        Check(Content.anchoredPosition == Vector2.zero && Content.localScale == Vector3.one, "saved recenter button restores current room and default size");
        dungeon.HideNavigation(); yield return null;
        Check(Icons().All(i => !i.transform.Find("Navigation Hit Area").GetComponent<Image>().raycastTarget), "hidden navigation disables all cell selection");
        dungeon.RefreshNavigation();
        // Reveal this generated graph only for a legible layout review, not as production fog behavior.
        foreach (var room in state.Graph.Rooms) { room.IsRevealed = true; room.IsVisited = true; room.IsCleared = true; }
        var middleX = state.Graph.Rooms.Average(r => r.Position.X); var middleY = state.Graph.Rooms.Average(r => r.Position.Y);
        var middle = state.Graph.Rooms.OrderBy(r => Mathf.Abs(r.Position.X - (float)middleX) + Mathf.Abs(r.Position.Y - (float)middleY)).First();
        state.Player.MoveTo(middle); dungeon.RefreshNavigation(); yield return null;
        Check(Icons().Any(i => ((RectTransform)i.transform).rect.width == map.Layout.roomSize)
            && Icons().Any(i => ((RectTransform)i.transform).rect.width == map.Layout.corridorEventSize), "large rooms and small corridor events coexist at authored sizes");
        Check(map.GetComponentsInChildren<Image>().Where(i => i.name == "Corridor").All(i => !i.raycastTarget), "decorative corridor tiles never intercept navigation input");
        // Leave several uncompleted icons visible for the reference comparison.
        foreach (var room in state.Graph.Rooms.Where(r => map.Layout.IsLargeRoom(r))) room.IsCleared = false;
        foreach (var room in state.Graph.Rooms.Where(r => r.Type is RoomEncounterType.Fountain or RoomEncounterType.Reward)) room.IsCleared = false;
        dungeon.RefreshNavigation(); yield return null;
        yield return Capture("02-Rooms-And-Corridors");
        scroll.scrollDelta = new Vector2(0, -3); map.OnScroll(scroll); yield return Capture("03-Overview");
        hud.closeMap.onClick.Invoke(); Check(!hud.mapPanel.activeSelf, "saved close button closes map");
        hud.mapButton.onClick.Invoke(); Check(hud.mapPanel.activeSelf, "HUD map button reopens map");
        map.FocusCurrent(); Status();
        File.WriteAllText(Folder + "Complete.txt", $"{checks} checks, {failures} failures. Manual UI check pending.\n");
    }
}
#endif
