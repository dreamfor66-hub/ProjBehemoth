using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonSlash
{
    public enum GameFlowState { RunStart, DungeonNavigation, RoomEntering, RoomEvent, Combat, RoomReward, PerkChoice, Shop, RunVictory, RunDefeat }

    public sealed partial class GameFlowController : MonoBehaviour
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
        public void SetEventArt(Sprite[] sprites)
        {
            if(sprites==null||sprites.Length!=4)return;
            goldEventSprite=sprites[0];fountainEventSprite=sprites[1];goddessEventSprite=sprites[2];merchantEventSprite=sprites[3];
        }
        [SerializeField] private DebugCheatOverlay cheatOverlay;
        [SerializeField] private int seed = 12345;

        private readonly Dictionary<int, RoomEventProgress> roomEvents = new();
        private int previousSeed;
        private bool isCheatCombat;
        public GameFlowState State { get; private set; }

        public void Configure(RunController run, DungeonController dungeon, CombatController combat, PerkChoiceView perks, ShopView shop, ResultView result, RunHudView hud, RoomTransitionView transition, EncounterMessageView encounterMessage, RoomEventView roomEvent, RewardPopupView rewardPopup, Sprite goldSprite, Sprite fountainSprite, Sprite goddessSprite, Sprite merchantSprite, DebugCheatOverlay cheat = null)
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
            cheatOverlay = cheat;
        }

        private void Awake()
        {
            dungeonController.RoomEntered += EnterRoom;
            combatController.Completed += CombatCompleted;
            if (cheatOverlay != null)
            {
                cheatOverlay.BattleRequested += HandleCheatBattleRequested;
                cheatOverlay.InventoryChanged += RefreshHud;
                cheatOverlay.VisibilityChanged += RefreshHud;
            }
        }

        private void OnDestroy()
        {
            if (dungeonController != null) dungeonController.RoomEntered -= EnterRoom;
            if (combatController != null) combatController.Completed -= CombatCompleted;
            if (cheatOverlay != null)
            {
                cheatOverlay.BattleRequested -= HandleCheatBattleRequested;
                cheatOverlay.InventoryChanged -= RefreshHud;
                cheatOverlay.VisibilityChanged -= RefreshHud;
            }
        }

        private void Start() => StartNewRun();

        public void StartNewRun()
        {
            StopAllCoroutines();
            combatController.ResetEncounter();
            seed = CreateRandomSeed();
            roomEvents.Clear();
            isCheatCombat = false;
            State = GameFlowState.RunStart;
            HideTransientViews();
            resultView?.Hide();
            runController.BeginRun();
            runController.State.Seed = seed;
            BindExpansion();
            combatController.ShowPlayer(runController.State);
            dungeonController.BeginRun(seed);
            runController.State.Fatigue.BeginFloor(content.fatigue,dungeonController.State.Graph,runController.State.CurrentAct);
            dungeonController.State.Player.CurrentRoom.IsCleared = true;
            cheatOverlay?.Bind(runController, combatController);
            ShowNavigation();
            ShowStartExplanation();
        }

        public bool StartCheatBattle(MonsterData monster)
        {
            if (monster == null || runController?.State == null || combatController == null || combatController.IsActive || State is GameFlowState.RunVictory or GameFlowState.RunDefeat)
                return false;
            StopAllCoroutines();
            HideTransientViews();
            dungeonController.HideNavigation();
            isCheatCombat = true;
            StartCoroutine(BeginCombatEvent(dungeonController.State.Player.CurrentRoom, new[] { monster }));
            return true;
        }

        private void HandleCheatBattleRequested(MonsterData monster) => StartCheatBattle(monster);

        private int CreateRandomSeed()
        {
            if (RunSession.ForcedSeed.HasValue) return RunSession.ForcedSeed.Value;
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
            runController.State.Fatigue.Step();
            if(runController.State.Fatigue.IsDead){runController.State.Player.DieFromExhaustion();ShowResult(false);yield break;}
            var travelDamage = runController.ApplyTravelHazard();
            if (travelDamage.HpDamage > 0f)
            {
                RefreshHud();
                if (travelDamage.TargetDied) { ShowResult(false); yield break; }
            }
            BeginEventPhase();
            if (roomTransitionView != null) yield return roomTransitionView.Play();
            if(room.Type==RoomEncounterType.Camp){ShowCamp(room);yield break;}
            if (room.IsCleared) { ShowNavigation(); yield break; }

            switch (room.Type)
            {
                case RoomEncounterType.Combat:
                case RoomEncounterType.Elite:
                case RoomEncounterType.Boss:
                    yield return BeginCombatEvent(room);
                    yield break;
                case RoomEncounterType.Reward:
                    yield return ShowEventMessage(GameText.Get("ui.gameflowcontroller.5ca48218"), goldEventSprite);
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
                    yield return ShowEventMessage(GameText.Get("ui.gameflowcontroller.79570a59"), goddessEventSprite);
                    room.IsCleared = true;
                    GrantAndShowReward(runController.GetGoddessRoomReward());
                    yield break;
                case RoomEncounterType.Shop:
                    HandleMerchantRoom(room);
                    yield break;
                case RoomEncounterType.Empty:
                    yield return ShowEventMessage(GameText.Get("ui.gameflowcontroller.b2f5746"), null);
                    room.IsCleared = true;
                    ShowNavigation();
                    yield break;
                case RoomEncounterType.Event:
                    ShowRandomEvent(room);
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
            var monster = forcedMonsters == null ? combatController.GetMonsterData(room.Type, room.EncounterOriginId, dungeonController.Floor) : forcedMonsters.Count > 0 ? forcedMonsters[0] : null;
            if (forcedMonsters == null) combatController.PrepareEncounter(runController.State, room.Type, room.EncounterOriginId, dungeonController.Floor);
            else combatController.PrepareEncounter(runController.State, forcedMonsters, dungeonController.Floor);
            if (!combatController.HasPreparedMonsters)
            {
                // A malformed encounter must never leave the flow parked behind an empty announcement.
                room.IsCleared = true;
                yield return ShowEventMessage(GameText.Get("ui.gameflowcontroller.247f5404"), null);
                ShowNavigation();
                yield break;
            }
            if (encounterMessageView != null)
            {
                var monsterName = monster == null || string.IsNullOrWhiteSpace(monster.displayName) ? GameText.Get("ui.gameflowcontroller.9b3dd503") : monster.displayName;
                combatController.BeginCombatPresentation();
                yield return encounterMessageView.Show(string.IsNullOrWhiteSpace(announcement) ? GameText.Format("ui.gameflowcontroller.a61799fd", monsterName) : announcement,
                    () => combatController.IsCombatPresentationReady);
            }
            State = GameFlowState.Combat;
            var arcadeHud=UnityEngine.Object.FindFirstObjectByType<ArcadeHudController>();
            if(arcadeHud!=null)yield return arcadeHud.ShowCombatTutorial();
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
            roomEventView.ShowChoice(GameText.Get("ui.gameflowcontroller.543192e4"), fountainEventSprite, GameText.Get("ui.gameflowcontroller.ca483b7"), GameText.Get("ui.gameflowcontroller.9c8a9f7f"), () => StartCoroutine(DrinkFountain(memory)), ShowNavigation);
        }

        private IEnumerator DrinkFountain(RoomEventProgress memory)
        {
            var before = runController.State.Player.CurrentHp;
            runController.State.Player.Heal(runController.FountainHealAmount);
            var healed = runController.State.Player.CurrentHp - before;
            memory.DrinkFromFountain();
            RefreshHud();
            yield return ShowEventMessage(GameText.Format("ui.gameflowcontroller.93765ef1", healed), fountainEventSprite);
            ShowNavigation();
        }

        private IEnumerator ShowDriedFountain(DungeonRoom room)
        {
            State = GameFlowState.RoomEvent;
            yield return ShowEventMessage(GameText.Get("ui.gameflowcontroller.896390ff"), fountainEventSprite);
            room.IsCleared = true;
            ShowNavigation();
        }

        private void ShowPoisonFountainChoice(DungeonRoom room)
        {
            State = GameFlowState.RoomEvent;
            if (roomEventView == null) { room.IsCleared = true; ShowNavigation(); return; }
            // Poison is intentionally a gamble: it must present exactly like an ordinary fountain.
            roomEventView.ShowChoice(GameText.Get("ui.gameflowcontroller.543192e4"), fountainEventSprite, GameText.Get("ui.gameflowcontroller.ca483b7"), GameText.Get("ui.gameflowcontroller.9c8a9f7f"), () => StartCoroutine(DrinkPoisonFountain(room)), () => { room.IsCleared = true; ShowNavigation(); });
        }

        private IEnumerator DrinkPoisonFountain(DungeonRoom room)
        {
            var healed = runController.DrinkToxicFountain();
            RefreshHud();
            yield return ShowEventMessage(GameText.Format("ui.gameflowcontroller.1fd680ca", healed), fountainEventSprite);
            room.IsCleared = true;
            ShowNavigation();
        }

        private void ShowChestChoice(DungeonRoom room)
        {
            State = GameFlowState.RoomEvent;
            if (roomEventView == null) { room.IsCleared = true; ShowNavigation(); return; }
            roomEventView.ShowChoice(GameText.Get("ui.gameflowcontroller.f792ceab"), goldEventSprite, GameText.Get("ui.gameflowcontroller.ca483b7"), GameText.Get("ui.gameflowcontroller.9c8a9f7f"), () => StartCoroutine(OpenChest(room)), () => { room.IsCleared = true; ShowNavigation(); });
        }

        private IEnumerator OpenChest(DungeonRoom room)
        {
            var mimic = room.ChestContent == ChestContent.Mimic && combatController.MimicMonster != null;
            if (mimic)
            {
                var trapDamage = runController.State.Player.TakeEnvironmentalDamage(8f);
                RefreshHud();
                if (trapDamage.TargetDied) { ShowResult(false); yield break; }
                yield return BeginCombatEvent(room, new[] { combatController.MimicMonster }, GameText.Get("ui.gameflowcontroller.f8df763f"));
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
                yield return ShowEventMessage(GameText.Format("ui.gameflowcontroller.cf8e328f", relic.displayName), goldEventSprite);
            else
                yield return ShowEventMessage(GameText.Get("ui.gameflowcontroller.f46f36d2"), goldEventSprite);
            if (expansionView != null && runController.State.PendingSpellOffers.Count > 0)
            {
                var resolved = false; expansionView.ResolvePendingSpells(() => resolved = true);
                while (!resolved) yield return null;
            }
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
            roomEventView.ShowChoice(GameText.Get("ui.gameflowcontroller.5db06efc"), merchantEventSprite, GameText.Get("ui.gameflowcontroller.ca483b7"), GameText.Get("ui.gameflowcontroller.9c8a9f7f"), () =>
            {
                memory.MarkMerchantDeparted();
                if (RollsMerchantMutantAmbush(dungeonController.State.Seed, room.RoomId, dungeonController.Floor) && combatController.MerchantMutant != null)
                {
                    StartCoroutine(BeginMerchantMutantAmbush(room));
                    return;
                }
                State = GameFlowState.Shop;
                ShowShop(room);
            }, () =>
            {
                memory.MarkMerchantDeparted();
                ShowNavigation();
            });
        }

        /// <summary>Each merchant independently has a 5% ambush roll from B2 onward.</summary>
        public static bool RollsMerchantMutantAmbush(int runSeed, int roomId, int floor)
        {
            if (floor < 2) return false;
            return new System.Random(unchecked(runSeed + roomId * 1619 + floor * 7919)).Next(100) < 5;
        }

        private IEnumerator BeginMerchantMutantAmbush(DungeonRoom room)
        {
            yield return BeginCombatEvent(room, new[] { combatController.MerchantMutant }, GameText.Get("ui.gameflowcontroller.417a7ff1"));
        }

        private IEnumerator ShowMerchantGone(DungeonRoom room)
        {
            State = GameFlowState.RoomEvent;
            yield return ShowEventMessage(GameText.Get("ui.gameflowcontroller.c9592d2e"), null);
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
            if (isCheatCombat)
            {
                isCheatCombat = false;
                StartCoroutine(CompleteCheatBattle(result.PlayerWon));
                return;
            }
            var room = dungeonController.State.Player.CurrentRoom;
            if (result.PlayerDied) { ShowResult(false); return; }
            runController.State.WonCombats++;
            if (HandleEventCombat(result)) return;
            if (room.Type == RoomEncounterType.Combat) runController.State.NormalCombatsInAct++;
            room.IsCleared = true;
            Action afterReward = room.Type switch
            {
                RoomEncounterType.Boss => () => ShowRelicChoice(room.RoomId, true),
                _ => null
            };
            GrantAndShowReward(runController.GetCombatReward(result.Monsters, room.Type == RoomEncounterType.Elite), afterReward);
        }

        private IEnumerator CompleteCheatBattle(bool won)
        {
            State = GameFlowState.RoomEvent;
            if (!won && !runController.State.Player.IsAlive)
                runController.State.Player.Revive(.5f);
            RefreshHud();
            yield return ShowEventMessage(won ? GameText.Get("ui.gameflowcontroller.8cea5541") : GameText.Get("ui.gameflowcontroller.57fa3b03"), null);
            ShowNavigation();
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
            if (bossRoom && dungeonController.Floor >= 3) { ShowResult(true); return; }
            var choices = bossRoom ? runController.GetExclusiveChoices(dungeonController.State.Seed + roomId + dungeonController.Floor * 1000) : runController.GetRelicChoices(dungeonController.State.Seed, roomId + dungeonController.Floor * 1000);
            if (choices.Count == 0)
            {
                if (bossRoom) ResolveBossFloor();
                else AfterRoomResolution();
                return;
            }
            State = GameFlowState.PerkChoice;
            if (expansionView != null)
            {
                expansionView.ShowModal(bossRoom ? GameText.Get("ui.gameflowcontroller.803375ba") : GameText.Get("ui.gameflowcontroller.f79abb6d"),
                    bossRoom ? GameText.Get("ui.gameflowcontroller.18c9beaf") : GameText.Get("ui.gameflowcontroller.74aabc17"),
                    choices.Select(relic => new PresentationChoice {
                        Title = relic.displayName, Description = relic.description, Icon = relic.icon, PreviewItem = relic,
                        Selected = () => {
                            runController.TryGrantRelic(relic); RefreshHud();
                            expansionView.ResolvePendingSpells(() => { if (bossRoom) ResolveBossFloor(); else AfterRoomResolution(); });
                        }
                    }));
                return;
            }
            perkChoiceView.ShowRelics(choices, relic =>
            {
                runController.TryGrantRelic(relic);
                perkChoiceView.Hide();
                RefreshHud();
                if (bossRoom) ResolveBossFloor();
                else AfterRoomResolution();
            });
            perkChoiceView.SetTitle(bossRoom ? GameText.Get("ui.gameflowcontroller.803375ba") : GameText.Get("ui.gameflowcontroller.f79abb6d"));
        }

        private void ResolveBossFloor()
        {
            if (dungeonController.Floor >= 3) { ShowResult(true); return; }
            StartCoroutine(EnterNextFloorRoutine());
        }

        private IEnumerator EnterNextFloorRoutine()
        {
            State = GameFlowState.RoomEntering;
            var player = runController.State.Player;
            player.Heal(player.MaxHp);
            player.RestoreShieldForNavigation();
            RefreshHud();
            var nextFloor = dungeonController.Floor + 1;
            yield return ShowEventMessage(GameText.Format("ui.gameflowcontroller.dfe4d6f2", nextFloor), null);
            roomEvents.Clear();
            dungeonController.BeginNextFloor();
            runController.State.Fatigue.BeginFloor(content.fatigue,dungeonController.State.Graph,runController.State.CurrentAct);
            runController.State.CurrentAct = nextFloor;
            runController.State.NormalCombatsInAct = 0;
            dungeonController.State.Player.CurrentRoom.IsCleared = true;
            ShowNavigation();
        }

        private void ShowShop(DungeonRoom room)
        {
            var inventory = runController.GetShopInventory(dungeonController.State.Seed, room.RoomId);
            shopView.Show(inventory, runController.State, runController.Shop, item =>
            {
                void Purchase() { runController.TryBuy(item); expansionView.ResolvePendingSpells(() => { ShowShop(room); RefreshHud(); }); }
                if (item.grantedSpell != null && runController.State.Slot(item.grantedSpell.defaultSlot) != null)
                    expansionView.OfferSpell(item.grantedSpell, accepted =>
                    {
                        expansionView.ResolvePendingSpells(() => { ShowShop(room); RefreshHud(); });
                    }, () => runController.TryBuy(item));
                else Purchase();
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
            expansionView?.HideModal();
        }

        private void RefreshHud()
        {
            if (runHud == null || dungeonController == null || runController == null || runController.State == null) return;
            var cheatOpen = cheatOverlay != null && cheatOverlay.IsOpen;
            expansionView?.RefreshCombat(combatController);
            expansionView?.SetStage(combatController.StageName(dungeonController.Floor), dungeonController.Floor);
            runHud.Bind(dungeonController.State, runController.State, potion =>
            {
                if (runController.TryUsePotion(potion)) RefreshHud();
            }, cheatOpen ? perk =>
            {
                if (runController.TryRemovePerk(perk)) RefreshHud();
            } : null, cheatOpen ? item =>
            {
                if (runController.TryRemoveEquipment(item)) RefreshHud();
            } : null);
        }

        private void ShowResult(bool victory)
        {
            State = victory ? GameFlowState.RunVictory : GameFlowState.RunDefeat;
            dungeonController.HideNavigation();
            HideTransientViews();
            resultView.Show(victory ? GameText.Get("ui.gameflowcontroller.91119a7") : GameText.Get("ui.gameflowcontroller.4f664436"), victory ? GameText.Get("ui.gameflowcontroller.543f0daf") : GameText.Get(runController.State.Fatigue.IsDead?"ui.fatigue.death":"ui.gameflowcontroller.c34162cb"), RunSession.ReturnToSelection);
        }
    }
}
