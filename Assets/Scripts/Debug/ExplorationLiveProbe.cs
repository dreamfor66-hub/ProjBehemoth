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
using UnityEngine.UI;

public sealed class ExplorationLiveProbe : MonoBehaviour
{
    const string Folder = "Docs/Production/Exploration-2026-10-31/";
    GameFlowController flow;
    DungeonController dungeon;
    RunController run;
    CombatController combat;
    ExplorationView view;
    RunExpansionView expansion;
    GameContent content;
    RoomEventView roomEvent;
    int checks, failures;
    bool previousBackground;
    bool hadHistory, completed;
    string previousHistory;
    static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Flags).GetValue(target);
    object Call(string name, params object[] args) => typeof(GameFlowController).GetMethod(name, Flags).Invoke(flow, args);
    void Check(bool ok, string message)
    { checks++; if (!ok) failures++; File.AppendAllText(Folder + "Live.txt", (ok ? "PASS " : "FAIL ") + message + "\n"); }
    public void Run() => StartCoroutine(Matrix());
    void Awake() { previousBackground = Application.runInBackground; Application.runInBackground = true; hadHistory = PlayerPrefs.HasKey("Behemoth.EventHistory.v1"); previousHistory = PlayerPrefs.GetString("Behemoth.EventHistory.v1", ""); }
    void OnDestroy()
    {
        Application.runInBackground = previousBackground;
        if (hadHistory) PlayerPrefs.SetString("Behemoth.EventHistory.v1", previousHistory); else PlayerPrefs.DeleteKey("Behemoth.EventHistory.v1");
        if (!completed) File.WriteAllText(Folder + "LiveIncomplete.txt", $"Interrupted after {checks} checks; {failures} failures. Final verification is pending.\n");
    }
    RoomObjectSlot Slot(RoomObjectData art) => view.slots.FirstOrDefault(s => s.gameObject.activeInHierarchy && s.BoundData == art);
    void Place(DungeonRoom room)
    {
        flow.StopAllCoroutines(); combat.ResetEncounter(); expansion.HideModal();
        var neighbor = dungeon.State.Graph.Neighbors(room).First();
        dungeon.State.Player.MoveTo(neighbor); dungeon.State.Player.MoveTo(room);
        Call("ShowNavigation"); Canvas.ForceUpdateCanvases();
    }
    void Press(bool yes) => Field<Button>(roomEvent, yes ? "yesButton" : "noButton").onClick.Invoke();
    IEnumerator Capture(string name)
    {
        yield return new WaitForSecondsRealtime(.55f);
        Canvas.ForceUpdateCanvases(); yield return new WaitForEndOfFrame();
        var texture = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Folder + name + ".png", texture.EncodeToPNG()); Destroy(texture);
    }
    IEnumerator Matrix()
    {
        Directory.CreateDirectory(Folder);
        if (File.Exists(Folder + "LiveComplete.txt")) File.Delete(Folder + "LiveComplete.txt");
        File.WriteAllText(Folder + "Live.txt", "Scoped live Unity exploration matrix. Synthetic interaction fixtures are isolated to this probe; production generation has no test routes or forced encounters. No DPS/balance suite.\n");
        yield return null; yield return null;
        flow = FindFirstObjectByType<GameFlowController>(); dungeon = FindFirstObjectByType<DungeonController>();
        run = FindFirstObjectByType<RunController>(); combat = FindFirstObjectByType<CombatController>();
        view = FindFirstObjectByType<ExplorationView>(); expansion = FindFirstObjectByType<RunExpansionView>();
        roomEvent = Field<RoomEventView>(flow, "roomEventView"); content = Field<GameContent>(flow, "content");
        flow.StartNewRun(); yield return null; expansion.HideModal();
        for (var floor = 1; floor <= 3; floor++)
        {
            if (floor > 1) dungeon.BeginNextFloor();
            run.State.CurrentAct = floor; run.State.Fatigue.BeginFloor(content.fatigue, dungeon.State.Graph, floor);
            Field<Dictionary<int, RoomEventProgress>>(flow, "roomEvents").Clear();
            var settings = dungeon.Exploration;
            Check(settings != null && view.slots.Length >= 4, $"B{floor}: saved exploration settings and object slots loaded");
            var room = dungeon.State.Graph.Rooms.First(r => r.Type == RoomEncounterType.Fountain);
            Place(room); var art = settings.For(room.Type); var slot = Slot(art);
            yield return null;
            Check(slot != null && !expansion.IsDecisionOpen && flow.State == GameFlowState.DungeonNavigation, $"B{floor}: fountain is a world interaction before a popup");
            var rays = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(FindFirstObjectByType<CombatSceneView>().UiCamera, slot.hitArea.position) }, rays);
            Check(rays.Count > 0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(rays[0].gameObject) == slot.button.gameObject,
                $"B{floor}: world hit area receives the top UI click (hits: {string.Join(",", rays.Take(3).Select(r => r.gameObject.name))})");
            slot.button.onClick.Invoke(); yield return null;
            Check(flow.State == GameFlowState.RoomEvent, $"B{floor}: world fountain opens its actual choice");
            Press(false); yield return null;
            Check(!room.IsCleared && Slot(art) != null, $"B{floor}: leaving the fountain preserves unused state");
            Slot(art).button.onClick.Invoke(); yield return null; Press(true); yield return null;
            Check(room.IsCleared, $"B{floor}: only drinking consumes fountain");
            Press(true); yield return null;
            Check(Slot(art) != null && Slot(art).painting.sprite == art.usedFront || Slot(art) != null && Slot(art).painting.sprite == art.usedSide, $"B{floor}: dry painting survives return to navigation");
            var hp = run.State.Player.CurrentHp;
            Slot(art).button.onClick.Invoke(); yield return null;
            Check(flow.State == GameFlowState.DungeonNavigation && view.narration.alpha > 0 && run.State.Player.CurrentHp == hp, $"B{floor}: spent fountain gives narration without modal or repeated healing");
            yield return Capture($"B{floor}-Fountain-Used");

            room = dungeon.State.Graph.Rooms.First(r => r.Type == RoomEncounterType.Chest && !r.IsOptionalAnnex);
            room.ChestContent = ChestContent.Gold; Place(room); art = settings.For(room.Type);
            Slot(art).button.onClick.Invoke(); yield return null; Press(false); yield return null;
            Check(!room.IsCleared, $"B{floor}: declined chest stays closed and reusable");
            Slot(art).button.onClick.Invoke(); yield return null; Press(true); yield return null;
            Check(room.IsCleared, $"B{floor}: accepting chest consumes it once");
            Field<Button>(Field<RewardPopupView>(flow, "rewardPopupView"), "confirmButton").onClick.Invoke(); yield return null;
            var gold = run.State.Gold; Slot(art).button.onClick.Invoke(); yield return null;
            Check(run.State.Gold == gold && flow.State == GameFlowState.DungeonNavigation && view.narration.alpha > 0, $"B{floor}: reopened chest gives narration without duplicating rewards");
            yield return Capture($"B{floor}-Chest-Open");

            room = dungeon.State.Graph.Rooms.First(r => r.Type == RoomEncounterType.Shop); Place(room); art = settings.For(room.Type);
            Slot(art).button.onClick.Invoke(); yield return null; Press(false); yield return null;
            Check(!Field<Dictionary<int, RoomEventProgress>>(flow, "roomEvents")[room.RoomId].MerchantDeparted && Slot(art) != null,
                $"B{floor}: declining merchant conversation does not make him leave");

            room = dungeon.State.Graph.Rooms.First(r => r.Type == RoomEncounterType.Goddess); art = settings.For(room.Type);
            foreach (var from in dungeon.State.Graph.Neighbors(room).ToArray())
            {
                Place(room); dungeon.State.Player.MoveTo(from); dungeon.State.Player.MoveTo(room); Call("ShowNavigation");
                var relative = DirectionUtility.ToRelative(dungeon.State.Player.Facing, room.ObjectWall);
                Check(relative != RelativeDirection.Back && Slot(art) != null, $"B{floor}: shrine intact wall visible from entry {from.Position}");
                Check(Slot(art).painting.sprite == (relative == RelativeDirection.Forward ? art.front : art.side), $"B{floor}: shrine uses authored painting for {relative}");
            }
            yield return Capture($"B{floor}-Goddess");

            room = dungeon.State.Graph.Rooms.First(r => r.Type == RoomEncounterType.Camp); Place(room); art = settings.For(room.Type);
            Check(Slot(art) != null, $"B{floor}: camp is present without forced popup");
            Slot(art).button.onClick.Invoke(); yield return null;
            expansion.ActiveChoices[0].button.onClick.Invoke(); yield return null;
            expansion.ActiveChoices[0].button.onClick.Invoke(); yield return null;
            Check(run.State.Fatigue.CampUsed && Slot(art) != null, $"B{floor}: resting persists camp's used state");

            var eventRoom = dungeon.State.Graph.Rooms.First(r => r.Type == RoomEncounterType.Event);
            var story = content.events.First(e => e != null && e.id == "QuietPrayer");
            eventRoom.Story = story; eventRoom.StoryPrepared = true; eventRoom.IsCleared = false;
            Place(eventRoom); Slot(story.worldObject).button.onClick.Invoke(); yield return null;
            Check(eventRoom.Story == story && eventRoom.StoryInvestigated && expansion.IsModalOpen, $"B{floor}: authored story opens from its scene object without reroll");
            yield return Capture($"B{floor}-Cleric-Conversation");
            expansion.ActiveChoices[1].button.onClick.Invoke(); yield return null;
            for (var paragraph = 0; paragraph < 12 && expansion.IsModalOpen; paragraph++)
            { expansion.ActiveChoices[0].button.onClick.Invoke(); yield return null; }
            Check(eventRoom.IsCleared && eventRoom.RevisitTextKey == "ui.explore.cleric.quiet", $"B{floor}: actual dialogue choice persists its specific aftermath");
            yield return Capture($"B{floor}-Cleric-World");
            Slot(story.worldObject).button.onClick.Invoke(); yield return null;
            Check(flow.State == GameFlowState.DungeonNavigation && view.narration.alpha > 0, $"B{floor}: resolved story answers in-world without reopening choices");
        }
        yield return PassageMatrix();
        yield return AftermathMatrix();
        yield return VariantMatrix();
        completed = true;
        if (File.Exists(Folder + "LiveIncomplete.txt")) File.Delete(Folder + "LiveIncomplete.txt");
        File.WriteAllText(Folder + "LiveComplete.txt", $"{checks} checks; {failures} failures. Live editor, scoped changed interactions. Experience/balance assessment reserved for user's normal full runs.\n");
        if (File.Exists("Temp/Exploration.manual"))
        {
            File.Delete("Temp/Exploration.manual");
            flow.StartNewRun(); yield return null; expansion.HideModal();
            Place(dungeon.State.Graph.Rooms.First(r => r.Type == RoomEncounterType.Fountain));
            yield return Capture("Manual-Ready");
            File.WriteAllText(Folder + "ManualReady.txt", "Live fixture ready for ordinary mouse interaction. Stop Play after scoped interaction check.\n");
            yield break;
        }
        UnityEditor.EditorApplication.isPlaying = false;
    }
    IEnumerator AftermathMatrix()
    {
        var room = dungeon.State.Graph.Rooms.First(r => r.Type == RoomEncounterType.Event);
        var story = content.events.First(e => e != null && e.id == "Hourglass");
        room.Story = story; room.StoryPrepared = true; room.IsCleared = false; room.ObjectUsed = false;
        Place(room); Slot(story.worldObject).button.onClick.Invoke(); yield return null;
        for (var step = 0; step < 18 && flow.State != GameFlowState.DungeonNavigation; step++)
        {
            if (expansion.IsModalOpen) expansion.ActiveChoices[0].button.onClick.Invoke();
            yield return null;
        }
        Check(room.ObjectUsed && room.IsCleared && room.RevisitTextKey == "ui.explore.used.hourglass.taken",
            "collecting actual hourglass persists an empty-table aftermath");
        Check(story.worldObject.usedFront != null && story.worldObject.usedSide != null && story.worldObject.usedRear != null,
            "collected hourglass has three authored aftermath directions");
        yield return Capture("Hourglass-Collected");
        var merchant = dungeon.Exploration.For(RoomEncounterType.Shop);
        story = content.events.First(e => e != null && e.worldObject == merchant);
        Check(story.options.Concat(story.pages.SelectMany(p => p.options)).Where(o => o.nextPage < 0)
            .All(o => o.revisitTextKey != "ui.explore.used.merchant" && !string.IsNullOrEmpty(o.revisitTextKey)),
            "retained story NPCs never claim that the seated person has departed");
    }
    IEnumerator PassageMatrix()
    {
        var generation = Field<DungeonGenerationSettings>(dungeon, "generationSettings");
        DungeonRunState chosen = null;
        for (var seed = 20; seed < 200; seed++)
        {
            var candidate = new DungeonGenerator(generation).Generate(seed, 3);
            if (candidate.Graph.Passages.Any(p => p.Concealed && p.Locked)) { chosen = candidate; break; }
        }
        Check(chosen != null, "natural generation produces a concealed locked passage");
        if (chosen == null) yield break;
        typeof(DungeonController).GetField("<State>k__BackingField", Flags).SetValue(dungeon, chosen);
        var passage = chosen.Graph.Passages.First(p => p.Concealed && p.Locked); var settings = dungeon.Exploration;
        Place(passage.Origin);
        Check(!chosen.CanSeePassage(passage.Origin, passage.Annex) && !chosen.CanTravel(passage.Origin, passage.Annex), "undiscovered entrance cannot be traversed");
        var map = FindFirstObjectByType<ArcadeHudController>().mapPanel.GetComponentInChildren<DungeonMapView>(true);
        map.Render(chosen);
        Check(!map.GetComponentsInChildren<MapRoomIcon>(true).Any(i => i.Room == passage.Annex), "hidden room and connecting route absent from map");
        yield return Capture("Hidden-Entrance");
        var clue = settings.concealedEntranceClues.Count > 0 ? settings.concealedEntranceClues[passage.ClueVariant % settings.concealedEntranceClues.Count] : settings.mechanism;
        view.slots.Last(s => s.gameObject.activeInHierarchy && s.BoundData == clue).button.onClick.Invoke(); yield return null;
        Check(passage.Discovered && passage.Locked && !chosen.CanTravel(passage.Origin, passage.Annex) && Slot(settings.lockedDoor) != null,
            "finding mechanism reveals locked door without granting access");
        yield return Capture("Locked-Discovered");
        var keyRoom = chosen.Graph.Rooms.First(r => r.KeyTokens.Contains(passage.KeyToken));
        Place(keyRoom); Slot(settings.key).button.onClick.Invoke(); yield return null;
        Check(chosen.Keys.Contains(passage.KeyToken) && !keyRoom.KeyTokens.Contains(passage.KeyToken), "key pickup is persistent and cannot be duplicated");
        Place(passage.Origin); Slot(settings.lockedDoor).button.onClick.Invoke(); yield return null;
        Check(!passage.Locked && !chosen.Keys.Contains(passage.KeyToken) && chosen.CanTravel(passage.Origin, passage.Annex), "matching key consumed exactly once and enables navigation");
        var destination = passage.Annex;
        var relative = DirectionUtility.ToRelative(chosen.Player.Facing, DirectionUtility.FromDelta(new DungeonPosition(destination.Position.X - passage.Origin.Position.X, destination.Position.Y - passage.Origin.Position.Y)));
        Call("ShowNavigation");
        var navigation = FindFirstObjectByType<NavigationChoiceView>();
        Field<Button>(Field<List<RoomChoiceButton>>(navigation, "choices")[(int)relative], "button").onClick.Invoke();
        yield return new WaitForSecondsRealtime(1.1f);
        Check(chosen.Player.CurrentRoom == destination && destination.IsVisited, "actual saved navigation button enters newly opened annex");
        flow.StopAllCoroutines(); combat.ResetEncounter(); expansion.HideModal(); Call("ShowNavigation");
        yield return Capture("Annex-Entered");
    }
    IEnumerator VariantMatrix()
    {
        var settings = dungeon.Exploration; var room = dungeon.State.Graph.Rooms.First(r => !r.IsOptionalAnnex && r.Connections.Count < 4);
        room.Type = RoomEncounterType.PoisonFountain; room.IsCleared = false;
        room.ObjectWall = ExplorationPlacement.WallFor(dungeon.State.Graph, room, 42);
        Place(room); var slot = Slot(settings.For(RoomEncounterType.Fountain));
        Check(slot != null, "toxic fountain shares ordinary fountain's exact pre-use artwork");
        slot.button.onClick.Invoke(); yield return null; Press(false); yield return null;
        Check(!room.IsCleared, "declining toxic fountain preserves unused state");
        Slot(settings.For(RoomEncounterType.Fountain)).button.onClick.Invoke(); yield return null; Press(true); yield return null;
        Check(room.IsCleared && run.State.PoisonStacks > 0, "toxic fountain actual drink applies poison and consumes the object");
        Press(true); yield return null;
        room.Type = RoomEncounterType.Chest; room.ChestContent = ChestContent.Mimic; room.IsCleared = false;
        Place(room); Slot(settings.For(RoomEncounterType.Chest)).button.onClick.Invoke(); yield return null; Press(true);
        var deadline = Time.realtimeSinceStartup + 8;
        while (!combat.IsActive && Time.realtimeSinceStartup < deadline)
        {
            if (expansion.IsModalOpen && flow.State == GameFlowState.Combat) expansion.ActiveChoices[0].button.onClick.Invoke();
            yield return null;
        }
        Check(combat.IsActive || flow.State == GameFlowState.Combat, "B3 mimic opens actual combat after chest interaction");
        yield return Capture("B3-Mimic-Combat");
        flow.StopAllCoroutines(); combat.ResetEncounter();
    }
}
#endif
