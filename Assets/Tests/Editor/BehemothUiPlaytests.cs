using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DungeonSlash.Tests
{
    public sealed partial class BehemothPlaytests
    {
        [UnityTest] public IEnumerator UI_CentralSelectionAndSummary_ModalPriority_FixedMerchant()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/OutGameScene.unity"); yield return new EnterPlayMode();
            Application.runInBackground = true;
            var selection = Object.FindFirstObjectByType<OutGameView>();
            foreach (var index in Enumerable.Range(0, selection.characterButtons.Length))
            {
                selection.characterButtons[index].onClick.Invoke(); Canvas.ForceUpdateCanvases();
                BehemothUiTests.Inside(selection.characterName.rectTransform, selection.startButton.transform.parent as RectTransform);
                Assert.That(selection.startingRelicScroll.content, Is.EqualTo(selection.startingRelic.rectTransform));
            }
            selection.Select((int)CharacterKind.Wizard); yield return null;
            ScreenCapture.CaptureScreenshot("Logs/Behemoth-UI-Selection.png"); yield return null;
            Matrix("중앙 캐릭터 선택: 여섯 캐릭터 전환, 능력치·시작 유물, 스크롤, 탐험 시작");
            selection.startButton.onClick.Invoke(); yield return null; yield return null;
            var flow = Object.FindFirstObjectByType<GameFlowController>();
            var expansion = Object.FindFirstObjectByType<RunExpansionView>();
            Assert.That(expansion.IsModalOpen, Is.True);
            ScreenCapture.CaptureScreenshot("Logs/Behemoth-UI-StartEvent.png"); yield return null;
            expansion.choices[0].button.onClick.Invoke(); yield return null;
            var controller = Object.FindFirstObjectByType<RunController>(); var run = controller.State;
            var hud = Object.FindFirstObjectByType<RunHudView>();
            run.Player.TakeEnvironmentalDamage(7); run.GainExperience(5); run.GainGold(13); yield return null;
            Assert.That(Field<TextMeshProUGUI>(hud, "characterName").text, Is.EqualTo(run.Player.Data.displayName));
            Assert.That(Field<TextMeshProUGUI>(hud, "healthText").text, Is.EqualTo(GameText.Format("ui.combathudview.dfe9f229", run.Player.CurrentHp, run.Player.MaxHp)));
            Assert.That(Field<Image>(hud, "experienceFill").rectTransform.anchorMax.x, Is.EqualTo((float)run.Experience / run.RequiredExperience).Within(.001));
            Assert.That(Field<TextMeshProUGUI>(hud, "goldText").text, Is.EqualTo(GameText.Format("ui.hud.gold", run.Gold)));
            var battleHud = Object.FindFirstObjectByType<CombatHudView>();
            Assert.That(Field<Image>(battleHud, "playerHp").transform.IsChildOf(hud.transform), Is.False, "Original combat HP stays in place.");
            run.Player.Heal(3); yield return null;
            Assert.That(Field<TextMeshProUGUI>(hud, "healthText").text, Is.EqualTo(GameText.Format("ui.combathudview.dfe9f229", run.Player.CurrentHp, run.Player.MaxHp)));
            Matrix("좌측 하단 추가 요약: 초상화·이름·실시간 피해/회복·EXP 게이지·골드, 기존 전투 체력 보존");

            Call(flow, "GrantAndShowReward", new CombatReward(17, 9), null);
            yield return new WaitForSecondsRealtime(1);
            var reward = Object.FindFirstObjectByType<RewardPopupView>();
            var rewardPanel = Field<GameObject>(reward, "panel");
            var point = RectTransformUtility.WorldToScreenPoint(null, expansion.spellButtons.First(button => button.gameObject.activeSelf).transform.position);
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
            Assert.That(hits.Count, Is.GreaterThan(0));
            Assert.That(hits[0].gameObject.transform.IsChildOf(rewardPanel.transform), Is.True, "Reward owns raycasts over the spell slots.");
            ScreenCapture.CaptureScreenshot("Logs/Behemoth-UI-Reward.png"); yield return null;
            Field<Button>(reward, "confirmButton").onClick.Invoke(); yield return null;
            Assert.That(rewardPanel.activeSelf, Is.False);
            Matrix("보상: 주문 슬롯보다 앞에 렌더링·입력 차단, EXP 애니메이션 후 확인·복귀");

            var roomEvent = Object.FindFirstObjectByType<RoomEventView>(FindObjectsInactive.Include);
            var accepted = false; var declined = false;
            roomEvent.ShowChoice(GameText.Get("ui.gameflowcontroller.543192e4"), Field<Sprite>(flow, "fountainEventSprite"), "마신다", "떠난다", () => accepted = true, () => declined = true);
            yield return null; ScreenCapture.CaptureScreenshot("Logs/Behemoth-UI-Fountain.png"); yield return null;
            Field<Button>(roomEvent, "yesButton").onClick.Invoke(); Assert.That(accepted, Is.True);
            roomEvent.ShowChoice(GameText.Get("ui.gameflowcontroller.f792ceab"), Field<Sprite>(flow, "goldEventSprite"), "연다", "떠난다", () => accepted = true, () => declined = true);
            Field<Button>(roomEvent, "noButton").onClick.Invoke(); Assert.That(declined, Is.True);
            expansion.ShowModal("사건", string.Concat(Enumerable.Repeat("대가와 결과를 읽고 선택합니다. ", 60)), Enumerable.Range(0, 11).Select(i => new PresentationChoice { Title = "선택 " + i, Description = "대가와 보상을 확인합니다." }));
            yield return null; Canvas.ForceUpdateCanvases();
            Assert.That(expansion.narrativeScroll.content.rect.height, Is.GreaterThan(expansion.narrativeScroll.viewport.rect.height));
            expansion.nextPage.onClick.Invoke(); Assert.That(expansion.choices[0].title.text, Is.EqualTo("선택 4"));
            ScreenCapture.CaptureScreenshot("Logs/Behemoth-UI-Story.png"); yield return null; expansion.HideModal();
            Matrix("중앙 이벤트: 분수 수락·상자 거절, 장문 스크롤·다중 선택 페이지");

            run.CurrentAct = 2; run.GainGold(2000);
            var dungeon = Object.FindFirstObjectByType<DungeonController>(); var room = dungeon.State.Player.CurrentRoom;
            Call(flow, "ShowShop", room); yield return null;
            var shop = Object.FindFirstObjectByType<ShopView>(); var stock = controller.GetShopInventory(dungeon.State.Seed, room.RoomId).ToArray();
            Assert.That(stock.Length, Is.EqualTo(6));
            var chosen = System.Array.FindIndex(stock, item => item.grantedSpell == null && item.IsRelic);
            var cards = Field<List<ShopItemView>>(shop, "items");
            var goldBefore = run.Gold; Field<Button>(cards[chosen], "button").onClick.Invoke(); yield return null;
            Assert.That(run.Gold, Is.EqualTo(goldBefore - stock[chosen].price));
            CollectionAssert.AreEqual(stock, controller.GetShopInventory(dungeon.State.Seed, room.RoomId));
            Assert.That(Field<Button>(cards[chosen], "button").interactable, Is.False);
            ScreenCapture.CaptureScreenshot("Logs/Behemoth-UI-Shop.png"); yield return null;
            var potion = stock.First(item => item.IsPotion);
            while (run.HasPotionSpace) Assert.That(run.TryStorePotion(potion), Is.True);
            Call(flow, "ShowShop", room);
            foreach (var index in Enumerable.Range(0, stock.Length).Where(i => stock[i].IsPotion))
                Assert.That(Field<TextMeshProUGUI>(cards[index], "status").text, Does.Contain("슬롯 부족"));

            // Buying a spell replacement must charge only after accepting the choice.
            foreach (var spell in Content.spells)
                if (run.Slot(spell.defaultSlot) == null) run.LearnSpell(spell);
            DungeonRoom bookRoom = null; EquipmentData[] bookStock = null; var bookIndex = -1;
            for (var attempt = 0; attempt < 100 && bookIndex < 0; attempt++)
            {
                bookRoom = new DungeonRoom(500 + attempt, default);
                bookStock = controller.GetShopInventory(dungeon.State.Seed, bookRoom.RoomId).ToArray();
                bookIndex = System.Array.FindIndex(bookStock, item => item.grantedSpell != null);
            }
            Assert.That(bookIndex, Is.GreaterThanOrEqualTo(0));
            Call(flow, "ShowShop", bookRoom);
            var book = bookStock[bookIndex]; var oldSpell = run.Slot(book.grantedSpell.defaultSlot); var beforeBook = run.Gold;
            Field<Button>(cards[bookIndex], "button").onClick.Invoke(); Assert.That(expansion.IsModalOpen, Is.True);
            expansion.choices[1].button.onClick.Invoke();
            Assert.That(run.Gold, Is.EqualTo(beforeBook)); Assert.That(run.Slot(book.grantedSpell.defaultSlot), Is.EqualTo(oldSpell));
            Field<Button>(cards[bookIndex], "button").onClick.Invoke(); expansion.choices[0].button.onClick.Invoke();
            Assert.That(run.Gold, Is.EqualTo(beforeBook - book.price)); Assert.That(run.Slot(book.grantedSpell.defaultSlot), Is.EqualTo(book.grantedSpell));
            Assert.That(Field<Button>(cards[bookIndex], "button").interactable, Is.False);
            Matrix("상점 추가 확인: 포션 슬롯 부족, 주문 교체 취소 무차감·수락 시 1회 차감·품절");
            run.SpendGold(run.Gold); Call(flow, "ShowShop", room); yield return null;
            Assert.That(Field<GameObject>(shop, "panel").activeSelf, Is.True, "A broke or sold-out merchant must not auto-close.");
            Assert.That(cards.All(card => !Field<Button>(card, "button").interactable), Is.True);
            Field<Button>(shop, "leaveButton").onClick.Invoke(); yield return null;
            Assert.That(flow.State, Is.EqualTo(GameFlowState.DungeonNavigation));
            Matrix("상점: 고정 6개, 구매·정확한 골드 차감·품절, 목록 불변, 골드 부족·수동 퇴장");
            yield return new ExitPlayMode();
        }
    }
}
