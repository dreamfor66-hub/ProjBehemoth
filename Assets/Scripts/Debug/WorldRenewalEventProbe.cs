#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed partial class ExplorationDepthLiveProbe
    {
        // F1 fixtures control encounter identity/teacher eligibility only. Every
        // investigation and choice below still goes through the actual EventSystem.
        IEnumerator WorldRenewalEvents()
        {
            var content = Ref<GameContent>(flow, "content");
            var stories = content.events.Where(e => e != null && !e.excludedFromRandomPool).OrderBy(e => e.id).ToArray();
            Require(stories.Length == 18, "event review sees exactly18 active encounters");
            Check(stories.All(s => s.worldObject != null) && stories.Select(s => s.worldObject).Distinct().Count() == stories.Length,
                "all18 encounters own a unique visible subject");
            Require(run.State.PendingPerkChoices == 0, "event fixture starts without unrelated pending level-up choices");
            var originalCasts = run.State.SpellCasts;
            run.State.GainGold(500);
            foreach (var story in stories)
            {
                yield return WrEventFixture(story);
                var room = dungeon.State.Player.CurrentRoom;
                Check(room.Story == story && !room.IsCleared && !room.ObjectUsed && !room.ObjectRemoved,
                    story.id + " actual room starts unresolved with its own subject");
                yield return Shot("Event-" + story.id + "-Entry");
                Click(Slot(story.worldObject).button);
                yield return Settle();
                Require(expansion.IsModalOpen && expansion.modalTitle.text == story.displayName,
                    story.id + " world-object pointer opens the actual encounter");
                yield return Shot("Event-" + story.id + "-FirstPage");
                yield return WrEventAdvanceToChoices(story);
                WrEventCheckChoices(story);

                if (story.encounterKind is EventEncounterKind.Person or EventEncounterKind.Creature || story.id == "Banner")
                {
                    var presented = Ref<PresentationChoice[]>(expansion, "currentChoices");
                    var held = story.pages.SelectMany(p => p.options).Concat(story.options)
                        .FirstOrDefault(o => o.KeepsEncounterAvailable && presented.Any(c => c.Enabled && c.Title == o.title));
                    Require(held != null, story.id + " has an available, free authored postponement on this entry page");
                    var gold = run.State.Gold; var owned = run.State.EquippedItems.Count;
                    yield return WrEventSelect(held.title);
                    yield return WrEventConclusions();
                    Check(!room.IsCleared && !room.ObjectUsed && !room.ObjectRemoved,
                        story.id + " declining preserves the actual person/creature and unresolved encounter");
                    Check(run.State.Gold == gold && run.State.EquippedItems.Count == owned,
                        story.id + " postponement grants and charges nothing");
                    yield return WrEventLeaveAndReturn(room);
                    Click(Slot(story.worldObject).button);
                    yield return Settle();
                    // On the opposite entry the same actor may now show its authored
                    // rear-approach menu. Speaking there must reach this same event.
                    var reopened = Ref<PresentationChoice[]>(expansion, "currentChoices");
                    if (reopened.Any(c => c.Title == GameText.Get("ui.npc.approach.talk")))
                    {
                        Check(reopened.Any(c => c.Title == GameText.Get("ui.npc.approach.ambush")),
                            story.id + " real rear revisit offers both speak and ambush");
                        yield return WrEventSelect(GameText.Get("ui.npc.approach.talk"));
                    }
                    Require(expansion.IsModalOpen && expansion.modalTitle.text == story.displayName,
                        story.id + " can naturally be approached again after a real room revisit");
                    yield return WrEventAdvanceToChoices(story);
                    WrEventCheckChoices(story);
                }
            }

            foreach (var id in new[] { "Well", "AmberSeed", "Scholar", "Delivery", "Training" })
            {
                var story = stories.Single(s => s.id == id);
                yield return WrEventFixture(story);
                var room = dungeon.State.Player.CurrentRoom;
                Check(story.roomTheme != null && Enumerable.Range(1, 3).All(act =>
                    Enum.GetValues(typeof(FacingDirection)).Cast<FacingDirection>().All(facing => story.roomTheme.Resolve(act, facing) != null)),
                    id + " has authored environment paintings in all3 acts and all4 views");
                Check(backdrop.VisibleTheme == story.roomTheme, id + " actual event room uses its purpose-built theme");
                Click(Slot(story.worldObject).button); yield return Settle();
                yield return WrEventAdvanceToChoices(story);
                var options = story.pages[0].options;
                var terminal = id == "Delivery"
                    ? options.Single(o => o.effects.Any(e => e.kind == EventEffectKind.RandomSharedRelic))
                    : options.Single(o => o.nextPage < 0 && !o.KeepsEncounterAvailable && o.effects.Count == 0);
                var beforeGold = run.State.Gold; var beforeOwned = run.State.EquippedItems.Count;
                yield return WrEventSelect(terminal.title);
                yield return WrEventConclusions();
                Check(room.IsCleared && room.ObjectRemoved == terminal.removeWorldObject && room.ObjectUsed == terminal.useWorldObject
                    && room.ObjectAftermath == terminal.resultingWorldObject,
                    id + " real choice applies the authored terminal physical state");
                if (id == "Delivery")
                {
                    Check(run.State.EquippedItems.Count == beforeOwned + 1 && run.State.Gold == beforeGold,
                        "Delivery receives one real relic without an invented cash payment or selection of unclaimed goods");
                    Check(!run.State.EventFlags.Contains("DeliveryWaiting"), "Delivery consumes the actual reservation flag");
                }
                else Check(run.State.Gold == beforeGold && run.State.EquippedItems.Count == beforeOwned,
                    id + " refusal/emergency stop gives no hidden reward");
                yield return Shot("Event-" + id + "-Terminal");
                yield return WrEventLeaveAndReturn(room);
                Check(room.IsCleared && room.ObjectRemoved == terminal.removeWorldObject && room.ObjectUsed == terminal.useWorldObject,
                    id + " physical ending persists after leaving and returning");
                if (room.ObjectRemoved)
                {
                    Check(world.slots.All(s => !s.gameObject.activeInHierarchy || s.BoundData != story.worldObject),
                        id + " destroyed/removed object has no remaining clickable painting");
                    Check(!expansion.IsModalOpen && flow.State == GameFlowState.DungeonNavigation,
                        id + " destroyed book does not reopen an encounter on room entry");
                }
                else
                {
                    var aftermath = room.ObjectAftermath != null ? room.ObjectAftermath : story.worldObject;
                    var slot = Slot(aftermath);
                    Check(slot.painting.sprite == aftermath.Painting(slot.BoundView, room.ObjectUsed),
                        id + " actual painting matches its used/aftermath state");
                    var gold = run.State.Gold; var owned = run.State.EquippedItems.Count;
                    Click(slot.button); yield return Settle();
                    Check(!expansion.IsModalOpen && flow.State == GameFlowState.DungeonNavigation,
                        id + " used subject speaks a short revisit line instead of reopening the choice popup");
                    Check(!string.IsNullOrWhiteSpace(room.RevisitTextKey) && world.narrationText.text == GameText.Get(room.RevisitTextKey),
                        id + " actual toast describes this particular physical ending");
                    Check(run.State.Gold == gold && run.State.EquippedItems.Count == owned,
                        id + " revisit cannot duplicate a reward");
                }
                yield return Shot("Event-" + id + "-Revisited");
            }
            run.State.SpellCasts = originalCasts;
            log.AppendLine("Event scope: all18 first-page pointer/modal reviews; authored person/creature postponements; one actual terminal path in each of five object events. No combat/DPS or whole-run replay suite.");
        }

        IEnumerator WrEventFixture(RandomEventData story)
        {
            Require(flow.DebugJumpToChapter(Mathf.Clamp(story.minAct, 1, 3)), "event fixture chapter " + story.id);
            if (!string.IsNullOrEmpty(story.requiredFlag)) run.State.EventFlags.Add(story.requiredFlag);
            if (!string.IsNullOrEmpty(story.forbiddenFlag)) run.State.EventFlags.Remove(story.forbiddenFlag);
            // Use the actual authored under-qualified entry pages for follow-up
            // teachers; no core requirements are weakened for the test.
            if (story.id is "MagicTeacherReturn" or "MagicTeacherAdvanced") run.State.SpellCasts = 0;
            if (story.id == "LostBeliever")
            {
                var proof = story.pages.SelectMany(p => p.options).Select(o => o.requiredRelic).FirstOrDefault(r => r != null);
                if (proof != null && !run.State.EquippedItems.Contains(proof)) run.TryGrantRelic(proof);
            }
            Require(flow.DebugPlaceRoomObject(story.worldObject, 0, false), "F1 authors the actual event subject " + story.id);
            yield return Settle(.18f);
        }

        void WrEventCheckChoices(RandomEventData story)
        {
            var presented = Ref<PresentationChoice[]>(expansion, "currentChoices");
            Check(!string.IsNullOrWhiteSpace(expansion.modalNarrative.text) && !expansion.modalNarrative.text.StartsWith("data."),
                story.id + " first page has localized visible narrative");
            Check(presented.Length > 0 && presented.All(c => !string.IsNullOrWhiteSpace(c.Title) && !c.Title.StartsWith("ui.") && !c.Title.Contains("{0}")),
                story.id + " choices have no blank titles or unresolved text keys");
            Check(presented.All(c => c.Enabled || !string.IsNullOrWhiteSpace(c.Requirements)),
                story.id + " every unavailable choice explains its authored requirement");
            foreach (var option in story.pages.SelectMany(p => p.options).Concat(story.options))
            {
                var choice = presented.FirstOrDefault(c => c.Title == option.title);
                if (choice == null) continue;
                if (option.goldCost > 0 || option.hpCost > 0 || option.maxHpCost > 0 || option.supplyCost > 0 || option.consumeHealingPotion)
                    Check(!string.IsNullOrWhiteSpace(choice.Requirements), story.id + " visible cost is displayed: " + option.title);
            }
        }

        IEnumerator WrEventAdvanceToChoices(RandomEventData story)
        {
            // Non-illustrated encounters reveal one paragraph at a time. Follow
            // their real Continue buttons before looking for the authored choice.
            var continued = 0;
            for (var i = 0; i < 16; i++)
            {
                var presented = Ref<PresentationChoice[]>(expansion, "currentChoices");
                var title = GameText.Get("ui.event.continue");
                if (presented.Length != 1 || presented[0].Title != title)
                {
                    if (continued > 0) yield return Shot("Event-" + story.id + "-Choices");
                    yield break;
                }
                Require(expansion.IsModalOpen && expansion.modalTitle.text == story.displayName,
                    story.id + " paragraph continuation belongs to the same event");
                yield return WrEventSelect(title);
                continued++;
            }
            Require(false, story.id + " paragraph progression reaches authored options within16 clicks");
        }

        IEnumerator WrEventSelect(string title)
        {
            for (var page = 0; page < 8; page++)
            {
                var row = expansion.ActiveChoices.FirstOrDefault(c => c.button.gameObject.activeInHierarchy && c.title.text == "> " + title);
                if (row != null) { Click(row.button); yield return Settle(); yield break; }
                Require(expansion.nextPage.gameObject.activeInHierarchy && expansion.nextPage.IsInteractable(), "reachable event choice: " + title);
                Click(expansion.nextPage); yield return Settle();
            }
            Require(false, "event choice pagination stays bounded: " + title);
        }

        IEnumerator WrEventConclusions()
        {
            for (var i = 0; i < 12; i++)
            {
                yield return Settle();
                if (flow.State == GameFlowState.DungeonNavigation && !expansion.IsModalOpen) yield break;
                Require(expansion.IsModalOpen, "event conclusion has its actual visible continuation");
                if (expansion.modalTitle.text == GameText.Get("ui.spell.learn.title"))
                { yield return WrEventSelect(GameText.Get("ui.spell.learn.decline")); continue; }
                var enabled = expansion.ActiveChoices.Where(c => c.button.gameObject.activeInHierarchy && c.button.IsInteractable()).ToArray();
                Require(enabled.Length == 1, "terminal event conclusion has one reachable Continue");
                Click(enabled[0].button);
            }
            Require(false, "event conclusion reaches navigation within its authored pages");
        }

        IEnumerator WrEventLeaveAndReturn(DungeonRoom origin)
        {
            var neighbor = dungeon.State.Graph.Neighbors(origin).First();
            yield return WrEventWalk(neighbor);
            yield return WrEventWalk(origin);
            Check(dungeon.State.Player.CurrentRoom == origin, "actual navigation returns to event room " + origin.RoomId);
        }
        IEnumerator WrEventWalk(DungeonRoom destination)
        {
            var from = dungeon.State.Player.CurrentRoom;
            var absolute = DirectionUtility.FromDelta(new DungeonPosition(destination.Position.X - from.Position.X, destination.Position.Y - from.Position.Y));
            var nav = Ref<NavigationChoiceView>(dungeon, "navigation");
            var choices = Ref<List<RoomChoiceButton>>(nav, "choices");
            var row = choices.Single(c => c.gameObject.activeInHierarchy && Ref<FacingDirection>(c, "absoluteDirection") == absolute);
            Click(Ref<Button>(row, "button"));
            var until = Time.realtimeSinceStartup + 5;
            while (Time.realtimeSinceStartup < until && (flow.State != GameFlowState.DungeonNavigation || dungeon.State.Player.CurrentRoom != destination)) yield return null;
            Require(flow.State == GameFlowState.DungeonNavigation && dungeon.State.Player.CurrentRoom == destination, "actual room movement completes");
            yield return Settle();
        }
    }
}
#endif
