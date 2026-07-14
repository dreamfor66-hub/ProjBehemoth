using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonSlash
{
    public enum GameFlowState { RunStart, DungeonNavigation, RoomEntering, RoomEvent, Combat, RoomReward, PerkChoice, Shop, RunVictory, RunDefeat }

    public sealed class GameFlowController : MonoBehaviour
    {
        [SerializeField] private RunController runController;
        [SerializeField] private DungeonController dungeonController;
        [SerializeField] private CombatController combatController;
        [SerializeField] private PerkChoiceView perkChoiceView;
        [SerializeField] private ShopView shopView;
        [SerializeField] private ResultView resultView;
        [SerializeField] private RunHudView runHud;
        [SerializeField] private RoomTransitionView roomTransitionView;
        [SerializeField] private EncounterMessageView encounterMessageView;
        [SerializeField] private RoomEventView roomEventView;
        [SerializeField] private RewardPopupView rewardPopupView;
        [SerializeField] private Sprite goldEventSprite;
        [SerializeField] private Sprite fountainEventSprite;
        [SerializeField] private Sprite goddessEventSprite;
        [SerializeField] private Sprite merchantEventSprite;
        [SerializeField] private int seed = 12345;

        private readonly Dictionary<int, RoomEventProgress> roomEvents = new();
        private int previousSeed;
        public GameFlowState State { get; private set; }

        public void Configure(RunController run, DungeonController dungeon, CombatController combat, PerkChoiceView perks, ShopView shop, ResultView result, RunHudView hud, RoomTransitionView transition, EncounterMessageView encounterMessage, RoomEventView roomEvent, RewardPopupView rewardPopup, Sprite goldSprite, Sprite fountainSprite, Sprite goddessSprite, Sprite merchantSprite)
        {
            runController = run;
            dungeonController = dungeon;
            combatController = combat;
            perkChoiceView = perks;
            shopView = shop;
            resultView = result;
            runHud = hud;
            roomTransitionView = transition;
            encounterMessageView = encounterMessage;
            roomEventView = roomEvent;
            rewardPopupView = rewardPopup;
            goldEventSprite = goldSprite;
            fountainEventSprite = fountainSprite;
            goddessEventSprite = goddessSprite;
            merchantEventSprite = merchantSprite;
        }

        private void Awake()
        {
            dungeonController.RoomEntered += EnterRoom;
            combatController.Completed += CombatCompleted;
        }

        private void Start() => StartNewRun();

        public void StartNewRun()
        {
            StopAllCoroutines();
            seed = CreateRandomSeed();
            roomEvents.Clear();
            State = GameFlowState.RunStart;
            HideTransientViews();
            resultView?.Hide();
            runController.BeginRun();
            combatController.ShowPlayer(runController.State);
            dungeonController.BeginRun(seed);
            dungeonController.State.Player.CurrentRoom.IsCleared = true;
            ShowNavigation();
        }

        private int CreateRandomSeed()
        {
            var next = Guid.NewGuid().GetHashCode();
            while (next == 0 || next == previousSeed) next = Guid.NewGuid().GetHashCode();
            previousSeed = next;
            return next;
        }

        private void EnterRoom(DungeonRoom room)
        {
            StopAllCoroutines();
            StartCoroutine(EnterRoomRoutine(room));
        }

        private IEnumerator EnterRoomRoutine(DungeonRoom room)
        {
            var travelDamage = runController.ApplyTravelHazard();
            if (travelDamage.HpDamage > 0f)
            {
                RefreshHud();
                if (travelDamage.TargetDied) { ShowResult(false); yield break; }
            }
            BeginEventPhase();
            if (roomTransitionView != null) yield return roomTransitionView.Play();
            if (room.IsCleared) { ShowNavigation(); yield break; }

            switch (room.Type)
            {
                case RoomEncounterType.Combat:
                case RoomEncounterType.Elite:
                case RoomEncounterType.Boss:
                    yield return BeginCombatEvent(room);
                    yield break;
                case RoomEncounterType.Reward:
                    yield return ShowEventMessage("\uBC14\uB2E5\uC5D0 \uAE08\uD654\uAC00 \uB5A8\uC5B4\uC838 \uC788\uB2E4.", goldEventSprite);
                    room.IsCleared = true;
                    GrantAndShowReward(runController.GetGoldRoomReward(false));
                    yield break;
                case RoomEncounterType.MajorReward:
                    // Compatibility for existing generated runs: old major rooms now use the chest flow.
                    room.ChestContent = ChestContent.Gold;
                    ShowChestChoice(room);
                    yield break;
                case RoomEncounterType.Fountain:
                    ShowFountainChoice(room);
                    yield break;
                case RoomEncounterType.PoisonFountain:
                    ShowPoisonFountainChoice(room);
                    yield break;
                case RoomEncounterType.Chest:
                    ShowChestChoice(room);
                    yield break;
                case RoomEncounterType.Goddess:
                    yield return ShowEventMessage("\uC5EC\uC2E0\uC0C1\uC774\uB2E4. \uAE30\uC6B4\uC774 \uCDA9\uB9CC\uD574\uC84C\uB2E4.", goddessEventSprite);
                    room.IsCleared = true;
                    GrantAndShowReward(runController.GetGoddessRoomReward());
                    yield break;
                case RoomEncounterType.Shop:
                    HandleMerchantRoom(room);
                    yield break;
                case RoomEncounterType.Empty:
                    yield return ShowEventMessage("\uC544\uBB34\uAC83\uB3C4 \uC5C6\uB2E4.", null);
                    room.IsCleared = true;
                    ShowNavigation();
                    yield break;
                default:
                    room.IsCleared = true;
                    ShowNavigation();
                    yield break;
            }
        }

        private IEnumerator BeginCombatEvent(DungeonRoom room, IReadOnlyList<MonsterData> forcedMonsters = null, string announcement = null)
        {
            State = GameFlowState.RoomEvent;
            var monster = forcedMonsters == null ? combatController.GetMonsterData(room.Type, room.RoomId, dungeonController.Floor) : forcedMonsters.Count > 0 ? forcedMonsters[0] : null;
            if (forcedMonsters == null) combatController.PrepareEncounter(runController.State, room.Type, room.RoomId, dungeonController.Floor);
            else combatController.PrepareEncounter(runController.State, forcedMonsters, dungeonController.Floor);
            if (!combatController.HasPreparedMonsters)
            {
                // A malformed encounter must never leave the flow parked behind an empty announcement.
                room.IsCleared = true;
                yield return ShowEventMessage("\uBAAC\uC2A4\uD130\uC758 \uD754\uC801\uC740 \uC5C6\uC5B4\uC84C\uB2E4.", null);
                ShowNavigation();
                yield break;
            }
            if (encounterMessageView != null)
            {
                var monsterName = monster == null || string.IsNullOrWhiteSpace(monster.displayName) ? "\uC54C \uC218 \uC5C6\uB294 \uBAAC\uC2A4\uD130" : monster.displayName;
                yield return encounterMessageView.Show(string.IsNullOrWhiteSpace(announcement) ? $"{monsterName}\uC774(\uAC00) \uB098\uD0C0\uB0AC\uB2E4!" : announcement);
            }
            State = GameFlowState.Combat;
            combatController.BeginPreparedCombat();
        }

        private void ShowFountainChoice(DungeonRoom room)
        {
            State = GameFlowState.RoomEvent;
            var memory = GetRoomMemory(room);
            switch (memory.ConsumeFountainVisit())
            {
                case FountainRoomEvent.DriedMessage:
                    StartCoroutine(ShowDriedFountain(room));
                    return;
                case FountainRoomEvent.Finished:
                    room.IsCleared = true;
                    ShowNavigation();
                    return;
            }
            if (roomEventView == null) { ShowNavigation(); return; }
            roomEventView.ShowChoice("\uBD84\uC218\uB300\uB2E4. \uBB3C\uC744 \uB9C8\uC2E4\uAE4C?", fountainEventSprite, "\uC608", "\uC544\uB2C8\uC624", () => StartCoroutine(DrinkFountain(memory)), ShowNavigation);
        }

        private IEnumerator DrinkFountain(RoomEventProgress memory)
        {
            var before = runController.State.Player.CurrentHp;
            runController.State.Player.Heal(runController.FountainHealAmount);
            var healed = runController.State.Player.CurrentHp - before;
            memory.DrinkFromFountain();
            RefreshHud();
            yield return ShowEventMessage($"\uAE30\uC6B4\uC774 \uCDA9\uB9CC\uD574\uC84C\uB2E4. HP\uAC00 {healed:0}\uB9CC\uD07C \uD68C\uBCF5\uB410\uB2E4.", fountainEventSprite);
            ShowNavigation();
        }

        private IEnumerator ShowDriedFountain(DungeonRoom room)
        {
            State = GameFlowState.RoomEvent;
            yield return ShowEventMessage("\uB9C8\uB978 \uBD84\uC218\uB300\uB9CC \uB369\uADF8\uB7EC\uB2C8 \uB193\uC5EC\uC838 \uC788\uB2E4.", fountainEventSprite);
            room.IsCleared = true;
            ShowNavigation();
        }

        private void ShowPoisonFountainChoice(DungeonRoom room)
        {
            State = GameFlowState.RoomEvent;
            if (roomEventView == null) { room.IsCleared = true; ShowNavigation(); return; }
            // Poison is intentionally a gamble: it must present exactly like an ordinary fountain.
            roomEventView.ShowChoice("\uBD84\uC218\uB300\uB2E4. \uBB3C\uC744 \uB9C8\uC2E4\uAE4C?", fountainEventSprite, "\uC608", "\uC544\uB2C8\uC624", () => StartCoroutine(DrinkPoisonFountain(room)), () => { room.IsCleared = true; ShowNavigation(); });
        }

        private IEnumerator DrinkPoisonFountain(DungeonRoom room)
        {
            var healed = runController.DrinkToxicFountain();
            RefreshHud();
            yield return ShowEventMessage($"\uC77C\uC2DC\uC801\uC73C\uB85C HP\uAC00 {healed:0}\uB9CC\uD07C \uD68C\uBCF5\uB410\uB2E4. \uB300\uC2E0 \uB3C5\uC5D0 \uC911\uB3C5\uB410\uB2E4.", fountainEventSprite);
            room.IsCleared = true;
            ShowNavigation();
        }

        private void ShowChestChoice(DungeonRoom room)
        {
            State = GameFlowState.RoomEvent;
            if (roomEventView == null) { room.IsCleared = true; ShowNavigation(); return; }
            roomEventView.ShowChoice("\uBCF4\uBB3C\uC0C1\uC790\uB2E4. \uC5F4\uC5B4\uBCFC\uAE4C?", goldEventSprite, "\uC608", "\uC544\uB2C8\uC624", () => StartCoroutine(OpenChest(room)), () => { room.IsCleared = true; ShowNavigation(); });
        }

        private IEnumerator OpenChest(DungeonRoom room)
        {
            var mimic = room.ChestContent == ChestContent.Mimic && combatController.MimicMonster != null;
            if (mimic)
            {
                var trapDamage = runController.State.Player.TakeEnvironmentalDamage(8f);
                RefreshHud();
                if (trapDamage.TargetDied) { ShowResult(false); yield break; }
                yield return BeginCombatEvent(room, new[] { combatController.MimicMonster }, "\uBCF4\uBB3C\uC0C1\uC790\uAC00 \uC774\uBE68\uC744 \uB4DC\uB7EC\uB0C8\uB2E4! \uBBF8\uBBF9\uC774 \uC2B5\uACA9\uD55C\uB2E4!");
                yield break;
            }

            room.IsCleared = true;
            if (room.ChestContent == ChestContent.Gold)
            {
                GrantAndShowReward(runController.GetGoldRoomReward(true));
                yield break;
            }
            var relic = runController.GetChestRelic(dungeonController.State.Seed, room.RoomId * 37);
            if (relic != null && runController.TryGrantRelic(relic))
                yield return ShowEventMessage($"\uC0C1\uC790 \uC548\uC5D0\uC11C {relic.displayName}\uC744(\uB97C) \uBC1C\uACAC\uD588\uB2E4.", goldEventSprite);
            else
                yield return ShowEventMessage("\uC0C1\uC790 \uC548\uC740 \uBE44\uC5B4 \uC788\uB2E4.", goldEventSprite);
            AfterRoomResolution();
        }

        private void HandleMerchantRoom(DungeonRoom room)
        {
            var memory = GetRoomMemory(room);
            switch (memory.ConsumeMerchantVisit())
            {
                case MerchantRoomEvent.GoneMessage:
                    StartCoroutine(ShowMerchantGone(room));
                    return;
                case MerchantRoomEvent.Finished:
                    room.IsCleared = true;
                    ShowNavigation();
                    return;
            }

            State = GameFlowState.RoomEvent;
            if (roomEventView == null) { ShowNavigation(); return; }
            roomEventView.ShowChoice("\uC774\uB7F0 \uACF3\uC5D0 \uC0C1\uC778...? \uC218\uC0C1\uD558\uC9C0\uB9CC \uB9D0\uC744 \uAC78\uC5B4\uBCFC\uAE4C?", merchantEventSprite, "\uC608", "\uC544\uB2C8\uC624", () =>
            {
                memory.MarkMerchantDeparted();
                State = GameFlowState.Shop;
                ShowShop(room);
            }, () =>
            {
                memory.MarkMerchantDeparted();
                ShowNavigation();
            });
        }

        private IEnumerator ShowMerchantGone(DungeonRoom room)
        {
            State = GameFlowState.RoomEvent;
            yield return ShowEventMessage("\uC0C1\uC778\uC774 \uC788\uC5C8\uB358 \uC790\uB9AC. \uADF8\uC0C8 \uC5C6\uC5B4\uC84C\uB2E4.", null);
            room.IsCleared = true;
            ShowNavigation();
        }

        private RoomEventProgress GetRoomMemory(DungeonRoom room)
        {
            if (!roomEvents.TryGetValue(room.RoomId, out var memory))
            {
                memory = new RoomEventProgress();
                roomEvents.Add(room.RoomId, memory);
            }
            return memory;
        }

        private IEnumerator ShowEventMessage(string message, Sprite illustration)
        {
            if (roomEventView != null) yield return roomEventView.ShowTimed(message, illustration);
        }

        private void CombatCompleted(CombatResult result)
        {
            var room = dungeonController.State.Player.CurrentRoom;
            if (result.PlayerDied) { ShowResult(false); return; }
            room.IsCleared = true;
            Action afterReward = room.Type switch
            {
                RoomEncounterType.Boss => () => ShowRelicChoice(room.RoomId, true),
                _ => null
            };
            GrantAndShowReward(runController.GetCombatReward(result.Monsters, room.Type == RoomEncounterType.Elite), afterReward);
        }

        private void GrantAndShowReward(CombatReward reward, Action afterReward = null)
        {
            var startingLevel = runController.State.Level;
            var startingExperience = runController.State.Experience;
            var startingRequirement = runController.State.RequiredExperience;
            runController.GrantCombatReward(reward);
            RefreshHud();
            ShowReward(reward, startingLevel, startingExperience, startingRequirement, afterReward);
        }

        private void ShowReward(CombatReward reward, int startingLevel, int startingExperience, int startingRequirement, Action afterReward)
        {
            State = GameFlowState.RoomReward;
            if (rewardPopupView == null)
            {
                if (runController.State.PendingPerkChoices > 0) ShowPerkChoice(() => CompleteReward(afterReward));
                else CompleteReward(afterReward);
                return;
            }
            rewardPopupView.Show(reward, startingLevel, startingExperience, startingRequirement, runController.State, () => PauseRewardForPerkChoice(reward, afterReward), () => CompleteReward(afterReward));
        }

        private void PauseRewardForPerkChoice(CombatReward reward, Action afterReward)
        {
            rewardPopupView?.Hide();
            ShowPerkChoice(() =>
            {
                rewardPopupView?.ResumeAfterPerk(reward, runController.State, () => CompleteReward(afterReward));
                State = GameFlowState.RoomReward;
                RefreshHud();
            });
        }

        private void CompleteReward(Action afterReward)
        {
            rewardPopupView?.Hide();
            if (afterReward != null) { afterReward(); return; }
            AfterRoomResolution();
        }

        private void ShowRelicChoice(int roomId, bool bossRoom)
        {
            var choices = runController.GetRelicChoices(dungeonController.State.Seed, roomId + dungeonController.Floor * 1000);
            if (choices.Count == 0)
            {
                if (bossRoom) ResolveBossFloor();
                else AfterRoomResolution();
                return;
            }
            State = GameFlowState.PerkChoice;
            perkChoiceView.ShowRelics(choices, relic =>
            {
                runController.TryGrantRelic(relic);
                perkChoiceView.Hide();
                RefreshHud();
                if (bossRoom) ResolveBossFloor();
                else AfterRoomResolution();
            });
        }

        private void ResolveBossFloor()
        {
            if (dungeonController.Floor >= 3) { ShowResult(true); return; }
            StartCoroutine(EnterNextFloorRoutine());
        }

        private IEnumerator EnterNextFloorRoutine()
        {
            var player = runController.State.Player;
            player.Heal(player.MaxHp);
            player.RestoreShieldForNavigation();
            RefreshHud();
            var nextFloor = dungeonController.Floor + 1;
            yield return ShowEventMessage($"\uBCF4\uC2A4\uB97C \uC4F0\uB7EC\uB728\uB838\uB2E4. \uC81C {nextFloor}\uCE35\uC73C\uB85C \uB0B4\uB824\uAC04\uB2E4.", null);
            roomEvents.Clear();
            dungeonController.BeginNextFloor();
            dungeonController.State.Player.CurrentRoom.IsCleared = true;
            ShowNavigation();
        }

        private void ShowShop(DungeonRoom room)
        {
            var inventory = runController.GetShopInventory(dungeonController.State.Seed, room.RoomId);
            shopView.Show(inventory, runController.State, runController.Shop, item =>
            {
                runController.TryBuy(item);
                ShowShop(room);
                RefreshHud();
            }, () =>
            {
                shopView.Hide();
                ShowNavigation();
            });
        }

        private void AfterRoomResolution()
        {
            RefreshHud();
            if (runController.State.PendingPerkChoices > 0) { ShowPerkChoice(); return; }
            ShowNavigation();
        }

        private void ShowPerkChoice(Action completed = null)
        {
            State = GameFlowState.PerkChoice;
            var perks = runController.Perks.GetChoices(runController.State, dungeonController.State.Seed);
            perkChoiceView.Show(perks, perk =>
            {
                runController.Perks.Apply(runController.State, perk);
                perkChoiceView.Hide();
                if (runController.State.PendingPerkChoices > 0) { ShowPerkChoice(completed); return; }
                if (completed != null) completed();
                else AfterRoomResolution();
            });
        }

        private void BeginEventPhase()
        {
            State = GameFlowState.RoomEntering;
            dungeonController.HideNavigation();
            HideTransientViews();
            RefreshHud();
        }

        private void ShowNavigation()
        {
            HideTransientViews();
            runController?.State?.Player?.RestoreShieldForNavigation();
            State = GameFlowState.DungeonNavigation;
            dungeonController.RefreshNavigation();
            RefreshHud();
        }

        private void HideTransientViews()
        {
            encounterMessageView?.Hide();
            roomEventView?.Hide();
            rewardPopupView?.Hide();
            shopView?.Hide();
            perkChoiceView?.Hide();
        }

        private void RefreshHud()
        {
            if (runHud == null || dungeonController == null || runController == null || runController.State == null) return;
            runHud.Bind(dungeonController.State, runController.State, potion =>
            {
                if (runController.TryUsePotion(potion)) RefreshHud();
            });
        }

        private void ShowResult(bool victory)
        {
            State = victory ? GameFlowState.RunVictory : GameFlowState.RunDefeat;
            HideTransientViews();
            resultView.Show(victory ? "RUN COMPLETE" : "RUN FAILED", victory ? "The boss has fallen. Your run growth will be reset." : "You were defeated. Your run growth will be reset.", StartNewRun);
        }
    }
}
