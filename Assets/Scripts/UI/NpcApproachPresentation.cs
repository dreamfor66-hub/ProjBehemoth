using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonSlash
{
    public sealed partial class GameFlowController
    {
        private DungeonRunState npcVisitState;
        private DungeonRoom npcVisitRoom;

        private void ObserveNpcRoomChange(DungeonRoom room)
        {
            var state = dungeonController != null ? dungeonController.State : null;
            if (state == null || room == null) return;
            if (ReferenceEquals(npcVisitState, state) && npcVisitRoom != null && npcVisitRoom != room)
            {
                var memory = GetRoomMemory(npcVisitRoom);
                if (!npcVisitRoom.IsCleared) memory.IgnoreConversation();
                memory.LeaveRoom();
                if (npcVisitRoom.Type == RoomEncounterType.Shop && memory.MerchantDeparted)
                    npcVisitRoom.IsCleared = true;
            }
            npcVisitState = state; npcVisitRoom = room;
        }

        private bool TryShowRearApproach(DungeonRoom room, RoomObjectData art)
        {
            var response = art.rearApproach;
            if (response == null || expansionView == null || !GetRoomMemory(room).CanRearApproach) return false;
            var relativeView = ((int)room.ObjectFacing - (int)dungeonController.State.Player.Facing + 6) % 4;
            if (relativeView != 2) return false;
            EventOption hostile = null;
            var narrativeKey = response.narrativeKey;
            var combatIdentity = response.CombatIdentity(dungeonController.State.Seed, room.RoomId, dungeonController.Floor);
            if (response.standaloneResponse != null && combatIdentity == null)
            {
                hostile = response.standaloneResponse.options.FirstOrDefault();
                if (hostile == null || hostile.nextPage >= 0 || hostile.KeepsEncounterAvailable) return false;
            }
            else if (response.eventResponses.Count > 0)
            {
                var story = room.Story;
                if (story == null) return false;
                var route = response.eventResponses.FirstOrDefault(binding => binding.story == story);
                if (route == null) return false;
                if (!string.IsNullOrEmpty(route.narrativeKey)) narrativeKey = route.narrativeKey;
                var options = route.page < 0 ? story.options : route.page < story.pages.Count ? story.pages[route.page].options : null;
                if (options == null || route.option >= options.Count) return false;
                hostile = options[route.option];
                // A rear response must resolve an actual hostile action, never open an unrelated story page.
                if (hostile == null || hostile.nextPage >= 0 || hostile.KeepsEncounterAvailable) return false;
            }
            else if (combatIdentity == null || combatIdentity.members.All(member => member.monster == null)) return false;
            var unavailable = hostile != null ? RandomEventSystem.UnavailableReason(hostile, runController.State) : null;
            BeginEventPhase(); State = GameFlowState.RoomEvent;
            expansionView.ShowModal(room.Story != null ? room.Story.displayName : GameText.Get(response.titleKey), GameText.Get(narrativeKey), new[]
            {
                new PresentationChoice { Title = GameText.Get("ui.npc.approach.ambush"), Description = hostile?.description,
                    Enabled = unavailable == null, Requirements = unavailable,
                    Selected = () => ExecuteRearApproach(room, art, response, hostile) },
                new PresentationChoice { Title = GameText.Get("ui.npc.approach.talk"), Selected = () =>
                    {
                        GetRoomMemory(room).BeginConversation();
                        if (room.Type == RoomEncounterType.Shop) HandleMerchantRoom(room); else ShowRandomEvent(room);
                    } },
                new PresentationChoice { Title = GameText.Get("ui.event.leave"), Selected = () =>
                    { GetRoomMemory(room).IgnoreConversation(); ShowNavigation(); } }
            }, art.Painting(relativeView, false));
            return true;
        }

        private void ExecuteRearApproach(DungeonRoom room, RoomObjectData art, NpcApproachData response, EventOption hostile)
        {
            GetRoomMemory(room).BeginConversation();
            if (hostile != null)
            {
                if (!RandomEventSystem.Pay(hostile, runController.State)) { ShowNavigation(); return; }
                var story = room.Story != null ? room.Story : response.standaloneResponse;
                activeEvent = story; activeStoryIllustration = story.illustration;
                room.StoryInvestigated = true; room.IsCleared = true; ApplyRoomObjectOutcome(room, hostile);
                if (room.Type == RoomEncounterType.Shop) GetRoomMemory(room).MarkMerchantDeparted();
                var messages = new List<string>(); if (!string.IsNullOrWhiteSpace(hostile.resultText)) messages.Add(hostile.resultText);
                ApplyEventEffects(story, hostile.effects, 0, messages); return;
            }
            var encounter = response.CombatIdentity(dungeonController.State.Seed, room.RoomId, dungeonController.Floor);
            RecordRoomChoice(new EventOption { title = GameText.Get("ui.npc.approach.ambush") });
            BeginNpcIdentityCombat(room, response, encounter);
        }

        private void BeginNpcIdentityCombat(DungeonRoom room, NpcApproachData response, EncounterData encounter)
        {
            room.IsCleared = true; room.ObjectRemoved = response.removeAfterCombat;
            room.ObjectAftermath = response.aftermath; room.ObjectUsed = response.aftermath != null;
            if (room.Type == RoomEncounterType.Shop) GetRoomMemory(room).MarkMerchantDeparted();
            explorationView?.SetVisible(false);
            eventCombatCompleted = () => expansionView.ShowMessage(GameText.Get(response.titleKey), GameText.Get(response.victoryTextKey), AfterRoomResolution);
            combatController.PrepareFixedEncounter(runController.State, encounter, dungeonController.Floor);
            RecordRoomMonsters(combatController.Enemies.Select(enemy => enemy.Data));
            combatController.BeginCombatPresentation(); State = GameFlowState.Combat;
            expansionView.ShowMessage(encounter.displayName, GameText.Get(response.combatAnnouncementKey), () => combatController.BeginPreparedCombat());
        }
    }
}
