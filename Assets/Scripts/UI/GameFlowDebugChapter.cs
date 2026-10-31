using System.Linq;
using UnityEngine;

namespace DungeonSlash
{
    public sealed partial class GameFlowController
    {
        public RoomObjectData DebugInspectionArt { get; private set; }

        private void ResetDebugChapterFlow()
        {
            StopAllCoroutines(); combatController.ResetEncounter(); roomTransitionView?.Cancel();
            roomEvents.Clear(); eventCombatCompleted = null; activeEvent = null; activeStoryIllustration = null;
            isCheatCombat = false; HideTransientViews(); resultView?.Hide();
            FindFirstObjectByType<ArcadeHudController>()?.Close();
        }
        public bool DebugJumpToChapter(int act)
        {
            if (act < 1 || act > 3 || dungeonController == null || runController == null) return false;
            if (runController.State == null) StartNewRun();
            ResetDebugChapterFlow(); DebugInspectionArt = null;
            seed = CreateRandomSeed(); var run = runController.State;
            run.Seed = seed; run.CurrentAct = act; run.NormalCombatsInAct = 0;
            run.SeenEvents.Clear(); runController.ClearChapterShopCache();
            dungeonController.BeginFloor(seed, act);
            run.Fatigue.BeginFloor(content.fatigue, dungeonController.State.Graph, act);
            run.Player.Revive(1); run.Player.RestoreShieldForNavigation();
            dungeonController.State.Player.CurrentRoom.IsCleared = true;
            combatController.ShowPlayer(run); combatController.pressureFeedback?.ResetFeedback(); ShowNavigation(); return true;
        }

        public bool DebugIsPassageArtwork(RoomObjectData art)
        {
            var settings = dungeonController?.Exploration;
            return art != null && settings != null && (art == settings.lockedDoor || art == settings.mechanism
                || art == settings.hiddenWall
                || settings.concealedEntranceClues.Contains(art) && !content.events.Any(e => e != null && e.worldObject == art));
        }
        public bool DebugClearInspectionRoom() => DebugCreateInspection(null, 0, false);
        public bool DebugPlaceRoomObject(RoomObjectData art, int view, bool used) => art != null && DebugCreateInspection(art, view, used);

        private bool DebugCreateInspection(RoomObjectData art, int view, bool used)
        {
            if (!ExplorationEnabled || runController?.State == null) return false;
            ResetDebugChapterFlow(); DebugInspectionArt = art;
            var settings = dungeonController.Exploration; view = (view % 4 + 4) % 4;
            var gate = DebugIsPassageArtwork(art);
            var entrance = art != null && (art == settings.bossEntrance || art == settings.eliteEntrance);
            var camping = art != null && art == settings.For(RoomEncounterType.Camp);
            var story = art == null ? null : content.events.FirstOrDefault(e => e != null && e.worldObject == art);
            if (art != null && (art.wallMounted || art.fitsDoorway || gate) && view == 2) view = 0;
            var mount = view == 0 ? RelativeDirection.Forward : view == 1 ? RelativeDirection.Left : RelativeDirection.Right;
            var graph = new DungeonGraph(); var center = graph.AddRoom(new DungeonPosition(0, 0));
            DungeonRoom annex = null;
            for (var i = 0; i < 4; i++)
            {
                var relative = (RelativeDirection)i;
                if ((camping || story?.roomLayout == EventRoomLayout.Alcove) && relative != RelativeDirection.Back) continue;
                var neighbor = graph.AddRoom(DirectionUtility.Offset(DirectionUtility.ToAbsolute(FacingDirection.North, relative)));
                neighbor.Type = RoomEncounterType.Empty; neighbor.IsCleared = true;
                if (art != null && art.wallMounted && !gate && !entrance && relative == mount) continue;
                graph.Connect(center, neighbor);
                if (relative == mount) annex = neighbor;
            }
            center.Type = RoomEncounterType.Empty; center.IsCleared = true;
            center.ObjectCorner = view; center.ObjectFacing = (FacingDirection)((view + 2) % 4);
            center.ObjectWall = DirectionUtility.ToAbsolute(FacingDirection.North, mount);
            center.ObjectPlacementPrepared = true; // F1 explicitly authors the requested view.
            var binding = settings.roomObjects.FirstOrDefault(b => b.artwork == art);
            if (gate)
            {
                annex.Type = RoomEncounterType.Chest; annex.IsCleared = false; annex.IsOptionalAnnex = true;
                var passage = new DungeonPassage(center, annex, art != settings.lockedDoor, true, Mathf.Max(0, settings.concealedEntranceClues.IndexOf(art)));
                graph.Passages.Add(passage);
                if (used) { passage.Discover(); var key = new System.Collections.Generic.HashSet<int> { passage.KeyToken }; passage.Unlock(key); }
                else if (passage.Locked) center.KeyTokens.Add(passage.KeyToken);
            }
            else if (entrance)
            { annex.Type = art == settings.bossEntrance ? RoomEncounterType.Boss : RoomEncounterType.Elite; annex.IsCleared = false; }
            else if (art == settings.key)
            {
                center.ObjectCorner = (view + 3) % 4;
                if (!used) center.KeyTokens.Add(annex.RoomId);
            }
            else if (art != null)
            {
                center.Type = binding != null ? binding.encounter : story != null ? RoomEncounterType.Event : RoomEncounterType.Empty;
                center.Story = story; center.StoryPrepared = story != null;
                center.ObjectAftermath = art; center.IsCleared = used || center.Type == RoomEncounterType.Empty; center.ObjectUsed = used;
                if (used && story != null)
                {
                    var outcomes = story.pages.Count > 0 ? story.pages.SelectMany(p => p.options) : story.options;
                    var outcome = outcomes.FirstOrDefault(o => o.nextPage < 0 && (o.removeWorldObject || o.useWorldObject));
                    if (outcome != null) ApplyRoomObjectOutcome(center, outcome);
                }
                if (used && center.Type == RoomEncounterType.Shop) GetRoomMemory(center).MarkMerchantDeparted();
            }
            dungeonController.BeginDebugInspection(graph);
            runController.State.Fatigue.BeginFloor(content.fatigue, graph, runController.State.CurrentAct);
            if (used && center.Type == RoomEncounterType.Camp) runController.State.Fatigue.Camp();
            combatController.ShowPlayer(runController.State); ShowNavigation(); return true;
        }
    }
}
