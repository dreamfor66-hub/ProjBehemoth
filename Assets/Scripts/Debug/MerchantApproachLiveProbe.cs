#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonSlash
{
    /// <summary>Scoped, explicit request only. Uses the saved Canvas and real pointer targets.</summary>
    public sealed class MerchantApproachLiveProbe : MonoBehaviour
    {
        const string Folder = "Docs/Production/WorldRenewal-2026-11-01/Merchant/";
        readonly StringBuilder log = new(); int checks, failures;
        GameFlowController flow; DungeonController dungeon; RunController run; ExplorationView world; RunExpansionView expansion; ShopView shop; CombatController combat;
        BaseInputModule[] inputs; bool background; string history; int? forcedSeed;
        public void Run() => StartCoroutine(Guarded(Review()));
        void Awake()
        {
            background = Application.runInBackground; Application.runInBackground = true;
            history = PlayerPrefs.GetString("Behemoth.EventHistory.v1", ""); forcedSeed = RunSession.ForcedSeed;
            inputs = FindObjectsByType<BaseInputModule>(FindObjectsSortMode.None).Where(i => i.enabled).ToArray(); foreach (var input in inputs) input.enabled = false;
        }
        void OnDestroy()
        {
            Application.runInBackground = background; RunSession.ForcedSeed = forcedSeed;
            if (history == "") PlayerPrefs.DeleteKey("Behemoth.EventHistory.v1"); else PlayerPrefs.SetString("Behemoth.EventHistory.v1", history);
            foreach (var input in inputs ?? Array.Empty<BaseInputModule>()) if (input != null) input.enabled = true;
        }
        static T Ref<T>(object target, string name) => (T)target.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
        static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, args);
        void Check(bool ok, string message) { checks++; log.AppendLine((ok ? "PASS " : "FAIL ") + message); if (!ok) failures++; Directory.CreateDirectory(Folder); File.WriteAllText(Folder + "RuntimeProgress.txt", log.ToString()); }
        void Require(bool ok, string message) { Check(ok, message); if (!ok) throw new InvalidOperationException(message); }
        IEnumerator Settle(float seconds = .1f) { yield return new WaitForSecondsRealtime(seconds); yield return new WaitForEndOfFrame(); }
        IEnumerator Guarded(IEnumerator routine)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(routine);
            while (stack.Count > 0)
            {
                object value = null; bool moved;
                try { moved = stack.Peek().MoveNext(); if (moved) value = stack.Peek().Current; }
                catch (Exception e) { Check(false, e.ToString()); break; }
                if (!moved) { stack.Pop(); continue; } if (value is IEnumerator child) { stack.Push(child); continue; } yield return value;
            }
            if (failures > 0) yield return Shot("Failure");
            log.AppendLine($"{checks} checks; {failures} failures. Scoped merchant/rear-approach runtime only; no balance or full regression suite.");
            File.WriteAllText(Folder + "RuntimeReview.txt", log.ToString()); EditorApplication.isPlaying = false;
        }
        void Click(Button button)
        {
            Require(button != null && button.gameObject.activeInHierarchy && button.interactable, "reachable " + button?.name);
            Canvas.ForceUpdateCanvases(); var canvas = button.GetComponentInParent<Canvas>().rootCanvas; var rect = (RectTransform)button.transform;
            var point = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, rect.TransformPoint(rect.rect.center));
            var pointer = new PointerEventData(EventSystem.current) { position = point, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            var top = hits.Count > 0 ? ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) : null;
            Require(top == button.gameObject, "pointer reaches " + button.name + "; actual " + top?.name);
            ExecuteEvents.Execute(top, pointer, ExecuteEvents.pointerDownHandler); ExecuteEvents.Execute(top, pointer, ExecuteEvents.pointerUpHandler); ExecuteEvents.Execute(top, pointer, ExecuteEvents.pointerClickHandler);
        }
        IEnumerator Shot(string name)
        {
            yield return Settle(); Directory.CreateDirectory(Folder + "Screens"); var texture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Folder + "Screens/" + name + ".jpg", texture.EncodeToJPG(90)); Destroy(texture); log.AppendLine("SCREEN " + name);
        }
        RoomObjectSlot Slot(RoomObjectData art) => world.slots.First(s => s.gameObject.activeInHierarchy && s.BoundData == art);
        RoomEventProgress Memory(DungeonRoom room) => (RoomEventProgress)Call(flow, "GetRoomMemory", room);
        static string ChoiceTitle(string key) => "> " + GameText.Get(key);
        bool HasChoice(string key) => expansion.ActiveChoices.Any(c => c.button.gameObject.activeInHierarchy && c.title.text == ChoiceTitle(key));
        void Choice(string key)
        {
            for(var page=0;page<8;page++)
            {
                var row=expansion.ActiveChoices.FirstOrDefault(c=>c.button.gameObject.activeInHierarchy&&c.title.text==ChoiceTitle(key));
                if(row!=null){Click(row.button);return;}
                Require(expansion.nextPage.gameObject.activeInHierarchy&&expansion.nextPage.IsInteractable(),"reachable paged choice "+key);
                Click(expansion.nextPage);
            }
            Require(false,"choice pagination remains bounded "+key);
        }
        void Offer(bool accept)
        { var view = Ref<RoomEventView>(flow, "roomEventView"); Click(Ref<Button>(view, accept ? "yesButton" : "noButton")); }
        IEnumerator NavigationWait()
        {
            var end = Time.realtimeSinceStartup + 8;
            while (flow.State != GameFlowState.DungeonNavigation && Time.realtimeSinceStartup < end) yield return null;
            Require(flow.State == GameFlowState.DungeonNavigation, "room walking beat returns to navigation"); yield return Settle();
        }
        IEnumerator Walk(RelativeDirection direction)
        {
            var navigation = Ref<NavigationChoiceView>(dungeon, "navigation"); var choices = Ref<List<RoomChoiceButton>>(navigation, "choices");
            var old = dungeon.State.Player.CurrentRoom;
            Click(Ref<Button>(choices[(int)direction], "button")); yield return NavigationWait();
            Require(dungeon.State.Player.CurrentRoom != old, "actual room movement " + direction);
        }
        IEnumerator ReturnFromBehind()
        { yield return Walk(RelativeDirection.Forward); yield return Walk(RelativeDirection.Back); }
        IEnumerator CloseSingleMessages()
        {
            for (var i = 0; i < 10; i++)
            {
                yield return Settle();
                var view = Ref<RoomEventView>(flow, "roomEventView");
                if (Ref<GameObject>(view, "panel").activeInHierarchy)
                {
                    var yes = Ref<Button>(view, "yesButton"); var no = Ref<Button>(view, "noButton");
                    if (yes.gameObject.activeInHierarchy && !no.gameObject.activeInHierarchy) { Click(yes); continue; }
                }
                if (!expansion.IsModalOpen) break;
                var choices = expansion.ActiveChoices.Where(c => c.button.gameObject.activeInHierarchy && c.button.interactable).ToArray();
                if (choices.Length != 1) break; Click(choices[0].button);
            }
        }
        IEnumerator Buy(ShopOffer offer)
        {
            var cards = Ref<List<ShopItemView>>(shop, "items");
            for (var i = 0; i < 4 && !cards.Any(c => c.gameObject.activeInHierarchy && c.Offer == offer); i++) { Click(shop.nextPage); yield return Settle(); }
            var card = cards.Single(c => c.gameObject.activeInHierarchy && c.Offer == offer);
            if (shop.compact) { Click(Ref<Button>(card, "button")); yield return Settle(); Click(shop.purchase); }
            else Click(Ref<Button>(card, "button"));
            yield return Settle(); Require(offer.Purchased, "real purchase marks the original offer sold out: " + offer.DisplayName);
        }
        IEnumerator Review()
        {
            flow = FindFirstObjectByType<GameFlowController>(); dungeon = FindFirstObjectByType<DungeonController>(); run = FindFirstObjectByType<RunController>(); combat = FindFirstObjectByType<CombatController>();
            world = FindFirstObjectByType<ExplorationView>(); expansion = FindFirstObjectByType<RunExpansionView>(); shop = Ref<ShopView>(flow, "shopView");
            yield return Settle(.7f); yield return CloseSingleMessages();
            var art = dungeon.Exploration.For(RoomEncounterType.Shop); Require(art.rearApproach != null, "merchant has an authored approach response");
            var content = Ref<GameContent>(flow, "content");
            foreach (var act in new[] { 1, 2, 3 })
            {
                RunSession.ForcedSeed = 7117; flow.DebugJumpToChapter(act); flow.DebugPlaceRoomObject(art, 0, false); yield return Settle();
                var room = dungeon.State.Player.CurrentRoom; var profile = art.rearApproach;
                Require(profile.CombatIdentity(dungeon.State.Seed, room.RoomId, act) == null, "ordinary merchant fixture B" + act);
                var stock = run.GetShopStock(dungeon.State.Seed, room.RoomId);
                Check(stock.Count == 10 && stock.Count(o => o.Item != null && o.Item.IsPotion) == 3 && stock.Count(o => o.Perk != null) == 3 && stock.Count(o => o.Item != null && o.Item.IsRelic) == 4, "B" + act + " authored stock is 3 potions, 3 perks, 4 relics");
                Check(ReferenceEquals(stock, run.GetShopStock(dungeon.State.Seed, room.RoomId)), "stock is stable before any purchase");
                Click(Slot(art).button); yield return Settle(); Offer(false); yield return Settle();
                Check(!Memory(room).MerchantDeparted && !room.IsCleared, "canceling conversation does not dismiss the merchant");
                Click(Slot(art).button); yield return Settle(); Offer(true); yield return Settle();
                Require(flow.State == GameFlowState.Shop, "front approach opens the real shop"); yield return Shot("B" + act + "-Shop-Page1");
                Click(Ref<Button>(shop, "leaveButton")); yield return Settle();
                Check(!Memory(room).MerchantDeparted, "closing without purchase keeps merchant");
                yield return ReturnFromBehind();
                Check(!Memory(room).MerchantDeparted && Slot(art).BoundView == 2, "unbought merchant remains on actual rear re-entry");
                Click(Slot(art).button); yield return Settle();
                Check(HasChoice("ui.npc.approach.ambush"), "rear re-entry presents actual ambush and talk choices");
                yield return Shot("B" + act + "-Rear-Approach"); Choice("ui.npc.approach.talk"); yield return Settle(); Offer(true); yield return Settle();
                run.State.GainGold(2000);
                Call(flow, "ShowShop", room); yield return Settle();
                var perkOffer = stock.First(o => o.Perk != null && run.Shop.CanPurchase(run.State, o));
                var previousStacks = run.State.ActivePerks.Where(p => p.Data == perkOffer.Perk).Sum(p => p.StackCount);
                var pending = run.State.PendingPerkChoices; var gold = run.State.Gold;
                yield return Buy(perkOffer);
                Check(run.State.ActivePerks.Where(p => p.Data == perkOffer.Perk).Sum(p => p.StackCount) == previousStacks + 1 && run.State.Gold == gold - perkOffer.Price, "perk purchase applies exactly one stack for its authored price");
                Check(run.State.PendingPerkChoices == pending, "buying a perk preserves pending level-up choices");
                Check(ReferenceEquals(stock, run.GetShopStock(dungeon.State.Seed, room.RoomId)) && stock.Count == 10, "buying preserves all ten slots without reroll/refill");
                Click(Ref<Button>(shop, "leaveButton")); yield return Settle();
                Check(Memory(room).MerchantPurchased && !Memory(room).MerchantDeparted, "purchase plus close keeps merchant for the current visit");
                Click(Slot(art).button); yield return Settle(); Offer(true); yield return Settle();
                Require(flow.State == GameFlowState.Shop, "merchant can reopen during purchased visit");
                Click(shop.nextPage); yield return Shot("B" + act + "-Shop-Page2");
                Click(Ref<Button>(shop, "leaveButton")); yield return Settle();
                yield return Walk(RelativeDirection.Back); yield return Walk(RelativeDirection.Back);
                Check(Memory(room).MerchantDeparted && room.IsCleared && Slot(art).painting.sprite != art.front, "only purchased room departure removes merchant on re-entry");
                Click(Slot(art).button); yield return Settle(); Check(flow.State == GameFlowState.DungeonNavigation && !expansion.IsModalOpen, "departed merchant is narration, never a reopened shop");
                yield return Shot("B" + act + "-Merchant-Departed");
            }
            // An ignored, ordinary merchant has a real data-driven counterstroke and one-time aftermath.
            flow.DebugJumpToChapter(1); flow.DebugPlaceRoomObject(art, 0, false); yield return Settle(); var ordinary = dungeon.State.Player.CurrentRoom;
            yield return ReturnFromBehind(); Click(Slot(art).button); yield return Settle(); var hp = run.State.Player.CurrentHp; var money = run.State.Gold;
            Choice("ui.npc.approach.ambush"); yield return Settle();
            Check(Mathf.Approximately(run.State.Player.CurrentHp, hp - 8) && run.State.Gold == money + 30, "ordinary merchant counterstroke pays exact authored cost/reward");
            yield return CloseSingleMessages(); Require(flow.State == GameFlowState.DungeonNavigation, "counterstroke conclusion returns to exploration");
            Check(ordinary.IsCleared && ordinary.ObjectRemoved && Memory(ordinary).MerchantDeparted && !world.slots.Any(s => s.gameObject.activeInHierarchy && s.BoundData == art), "merchant escape is persistent and cannot be farmed");
            yield return Shot("Merchant-Ambush-Aftermath");
            foreach (var story in content.events.Where(e => e != null && !e.excludedFromRandomPool && e.encounterKind == EventEncounterKind.Person && e.worldObject?.rearApproach != null))
            {
                flow.DebugJumpToChapter(story.minAct); flow.DebugPlaceRoomObject(story.worldObject, 0, false); yield return Settle();
                var room = dungeon.State.Player.CurrentRoom; room.Story = story; room.StoryPrepared = true;
                yield return ReturnFromBehind(); Click(Slot(story.worldObject).button); yield return Settle();
                Check(HasChoice("ui.npc.approach.ambush"), story.id + " rear response routes to its own terminal hostile choice");
                Choice("ui.event.leave"); yield return Settle(); Check(!room.IsCleared, story.id + " ignoring rear approach keeps encounter available");
            }
            // Pick an actual concealed identity seed; ordinary merchants above never transform.
            var hiddenSeed = Enumerable.Range(1, 10000).First(s => art.rearApproach.CombatIdentity(s, 0, 2) != null);
            RunSession.ForcedSeed = hiddenSeed; flow.DebugJumpToChapter(2); flow.DebugPlaceRoomObject(art, 0, false); yield return Settle();
            yield return ReturnFromBehind(); Click(Slot(art).button); yield return Settle(); Choice("ui.npc.approach.ambush"); yield return Settle();
            yield return CloseSingleMessages(); yield return Settle(.3f);
            Check(combat.IsActive && combat.Enemies.Any(e => e.Data == combat.MerchantMutant), "only authored concealed identity starts the real merchant mutant encounter");
            yield return Shot("Concealed-Merchant-Combat");
        }
    }
}
#endif
