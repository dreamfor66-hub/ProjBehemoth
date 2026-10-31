using System.Collections;
using System.Linq;
using UnityEngine;

namespace DungeonSlash
{
    public sealed partial class GameFlowController
    {
        [SerializeField] private ExplorationView explorationView;
        private bool ExplorationEnabled => explorationView != null && dungeonController.Exploration != null;
        private bool IsWorldEncounter(DungeonRoom room) => ExplorationEnabled && room.Type is RoomEncounterType.Chest
            or RoomEncounterType.MajorReward or RoomEncounterType.Fountain or RoomEncounterType.PoisonFountain
            or RoomEncounterType.Goddess or RoomEncounterType.Shop or RoomEncounterType.Camp;

        private void PrepareRoomStory(DungeonRoom room)
        {
            if (!ExplorationEnabled || content == null || room.Type != RoomEncounterType.Event || room.StoryPrepared) return;
            room.StoryPrepared = true;
            room.Story = RandomEventSystem.Draw(content, runController.State, dungeonController.Floor, room.RoomId, room.Connections.Count);
            if (room.Story == null) room.Type = RoomEncounterType.Combat;
        }

        private void RefreshExploration()
        {
            if (!ExplorationEnabled) return;
            var state = dungeonController.State; var room = state.Player.CurrentRoom;
            var settings = dungeonController.Exploration;
            explorationView.SetBackdrop(FindFirstObjectByType<StageBackdropView>());
            explorationView.SetVisible(true); explorationView.Clear();
            explorationView.ShowKeys(state.Keys.Count);
            var art = room.Type == RoomEncounterType.Event ? room.Story?.worldObject : settings.For(room.Type);
            if (room.ObjectAftermath != null) art = room.ObjectAftermath;
            // An event must own its visible subject. A shared mystery-object fallback promises
            // unrelated encounters behind an identical image and is therefore never used here.
            if (art != null && !room.ObjectRemoved && (room.Type != RoomEncounterType.Event || room.Story != null))
            {
                if (!room.ObjectPlacementPrepared)
                {
                    room.ObjectPlacementPrepared = true;
                    if (art.facePlayerOnFirstEncounter)
                    {
                        var choice = (state.Seed ^ room.RoomId * 397) & 1;
                        choice = explorationView.FirstEncounterCorner(art, choice);
                        room.ObjectCorner = ((int)state.Player.Facing + choice) % 4;
                        room.ObjectFacing = (FacingDirection)(((int)state.Player.Facing + 2) % 4);
                    }
                }
                var used = room.Type == RoomEncounterType.Camp ? runController.State.Fatigue.CampUsed
                    : room.Type == RoomEncounterType.Shop ? GetRoomMemory(room).MerchantDeparted
                    : room.Type == RoomEncounterType.Event ? room.ObjectUsed : room.IsCleared;
                var corner = ExplorationPlacement.RelativeCorner(state.Player.Facing, room.ObjectCorner);
                var view = ((int)room.ObjectFacing - (int)state.Player.Facing + 6) % 4;
                var anchor = explorationView.Corner(corner); var perspective = ExplorationView.Perspective(anchor);
                if (art.roomInterior) { anchor = new Vector2(.30f, .24f); view = 0; perspective = .95f; }
                if (art.wallMounted)
                {
                    var relative = DirectionUtility.ToRelative(state.Player.Facing, room.ObjectWall);
                    // Entry direction is always an opening, so an intact attachment wall cannot be behind us.
                    if (relative != RelativeDirection.Back)
                    {
                        anchor = explorationView.Wall(relative); view = relative == RelativeDirection.Forward ? 0 : relative == RelativeDirection.Left ? 1 : 3;
                        perspective = relative == RelativeDirection.Forward ? .80f : .95f;
                    }
                }
                var concealed = room.Type == RoomEncounterType.Event && room.Story.discovery == EventDiscovery.Concealed && !used;
                explorationView.Add(art, view, used, anchor, perspective, concealed, () => InvestigateRoomObject(room, art));
            }
            foreach (var token in room.KeyTokens.ToArray())
            {
                var anchor = explorationView.KeyCorner((room.ObjectCorner + 1 - (int)state.Player.Facing + 4) % 4);
                explorationView.Add(settings.key, 0, false, anchor, ExplorationView.Perspective(anchor), false, () =>
                {
                    if (State != GameFlowState.DungeonNavigation || !room.KeyTokens.Remove(token)) return;
                    state.Keys.Add(token); RefreshExploration(); explorationView.Say("ui.explore.key.found");
                });
            }
            foreach (var passage in state.Graph.Passages.Where(p => p.Origin == room || p.Annex == room))
            {
                var other = passage.Other(room);
                var direction = DirectionUtility.FromDelta(new DungeonPosition(other.Position.X - room.Position.X, other.Position.Y - room.Position.Y));
                var relative = DirectionUtility.ToRelative(state.Player.Facing, direction);
                if (relative == RelativeDirection.Back || passage.Discovered && !passage.Locked) continue;
                var view = relative == RelativeDirection.Forward ? 0 : relative == RelativeDirection.Left ? 1 : 3;
                if (!passage.Discovered)
                    explorationView.AddHiddenWall(settings.hiddenWall ?? settings.lockedDoor, view, explorationView.Wall(relative), () => InvestigatePassage(passage));
                else
                    explorationView.Add(settings.lockedDoor, view, false, explorationView.Wall(relative), relative == RelativeDirection.Forward ? .80f : .95f,
                        false, () => InvestigatePassage(passage));
            }
            foreach (var target in state.Graph.Neighbors(room))
            {
                if (target.Type is not (RoomEncounterType.Elite or RoomEncounterType.Boss) || !state.CanTravel(room, target)) continue;
                var absolute = DirectionUtility.FromDelta(new DungeonPosition(target.Position.X-room.Position.X,target.Position.Y-room.Position.Y));
                var relative = DirectionUtility.ToRelative(state.Player.Facing, absolute);
                if (relative == RelativeDirection.Back) continue;
                var doorway = target.Type == RoomEncounterType.Boss ? settings.bossEntrance : settings.eliteEntrance;
                var facingView = relative == RelativeDirection.Forward ? 0 : relative == RelativeDirection.Left ? 1 : 3;
                explorationView.Add(doorway, facingView, false, explorationView.Wall(relative), 1, false,
                    () => { if(State==GameFlowState.DungeonNavigation) dungeonController.TravelThrough(absolute); });
            }
        }

        private void InvestigateRoomObject(DungeonRoom room, RoomObjectData art)
        {
            if (State != GameFlowState.DungeonNavigation || dungeonController.State.Player.CurrentRoom != room) return;
            var consumed = room.Type == RoomEncounterType.Shop ? GetRoomMemory(room).MerchantDeparted : room.IsCleared;
            if (room.Type == RoomEncounterType.Camp)
            {
                if (runController.State.Fatigue.CampUsed && runController.State.Fatigue.Value >= content.fatigue.shortRestCeiling)
                { explorationView.Say(art.resolvedTextKey); return; }
            }
            else if (consumed) { explorationView.Say(string.IsNullOrEmpty(room.RevisitTextKey) ? art.resolvedTextKey : room.RevisitTextKey); return; }
            BeginEventPhase();
            switch (room.Type)
            {
                case RoomEncounterType.Chest: case RoomEncounterType.MajorReward: ShowChestChoice(room); break;
                case RoomEncounterType.Fountain: ShowFountainChoice(room); break;
                case RoomEncounterType.PoisonFountain: ShowPoisonFountainChoice(room); break;
                case RoomEncounterType.Goddess: ShowGoddessChoice(room); break;
                case RoomEncounterType.Shop: HandleMerchantRoom(room); break;
                case RoomEncounterType.Camp: ShowCamp(room); break;
                case RoomEncounterType.Event: ShowRandomEvent(room); break;
                default: ShowNavigation(); break;
            }
        }

        private void ShowGoddessChoice(DungeonRoom room)
        {
            if (roomEventView == null) { ShowNavigation(); return; }
            State = GameFlowState.RoomEvent;
            roomEventView.ShowChoice(GameText.Get("ui.explore.goddess.ask"), goddessEventSprite,
                GameText.Get("ui.explore.goddess.pray"), GameText.Get("ui.event.leave"),
                () => StartCoroutine(UseGoddess(room)), ShowNavigation);
        }
        private IEnumerator UseGoddess(DungeonRoom room)
        {
            yield return ShowEventMessage(GameText.Get("ui.gameflowcontroller.79570a59"), goddessEventSprite);
            room.IsCleared = true; GrantAndShowReward(runController.GetGoddessRoomReward());
        }
        private void ApplyRoomObjectOutcome(DungeonRoom room, EventOption choice)
        {
            room.ObjectRemoved = choice.removeWorldObject;
            room.ObjectUsed = choice.useWorldObject;
            room.ObjectAftermath = choice.resultingWorldObject;
            room.RevisitTextKey = choice.revisitTextKey;
        }

        private void InvestigatePassage(DungeonPassage passage)
        {
            if (State != GameFlowState.DungeonNavigation) return;
            if (!passage.Discovered)
            {
                var opened = passage.Search(dungeonController.Exploration.secretWallSearches);
                ShowNavigation();
                explorationView.Say(opened ? "ui.explore.wall.opened" : passage.SearchProgress == 1 ? "ui.explore.wall.hollow" : "ui.explore.wall.loose");
                return;
            }
            if (passage.Locked)
            {
                if (!passage.Unlock(dungeonController.State.Keys))
                { ShowNavigation(); explorationView.Say("ui.explore.door.locked"); return; }
                ShowNavigation(); explorationView.Say("ui.explore.door.opened");
            }
            else { ShowNavigation(); explorationView.Say("ui.explore.passage.opened"); }
        }
    }
}
