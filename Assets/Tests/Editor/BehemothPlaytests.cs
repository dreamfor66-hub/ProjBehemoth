using TMPro;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DungeonSlash.Tests
{
    public sealed partial class BehemothPlaytests
    {
        [UnityTearDown] public IEnumerator RestoreEditorAfterFailure()
        { Time.timeScale = 1; if (Application.isPlaying) yield return new ExitPlayMode(); RunSession.ForcedSeed = null; }
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static GameContent Content => AssetDatabase.LoadAssetAtPath<GameContent>("Assets/Data/Behemoth/GameContent.asset");
        private static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target, args);
        private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Private).GetValue(target);
        private static object[] Actors(CombatController combat) => ((IList)Field<object>(combat, "activeMonsters")).Cast<object>().ToArray();
        private static MonsterRuntime Runtime(object actor) => (MonsterRuntime)actor.GetType().GetProperty("Runtime").GetValue(actor);
        private static Vector2 Position(object actor) => (Vector2)actor.GetType().GetProperty("Position").GetValue(actor);
        private static EncounterData Encounter(string id) => AssetDatabase.LoadAssetAtPath<EncounterData>("Assets/Data/Behemoth/Encounters/" + id + ".asset");
        private static Vector2 Screen(CombatSceneView view, Vector2 logical) => RectTransformUtility.WorldToScreenPoint(null, view.LogicalArea.TransformPoint(logical));
        private static void Matrix(string item, string result = "PASS") { Directory.CreateDirectory("Logs"); File.AppendAllText("Logs/Behemoth-PlaytestMatrix.txt", DateTime.Now.ToString("s") + " | " + item + " | " + result + "\n"); }
        private static void SetupCharacter(CharacterKind kind)
        {
            Application.runInBackground = true;
            RunSession.ForcedSeed = 165713 + (int)kind * 719;
            RunSession.SelectedCharacter = Content.characters.Single(item => item.character == kind);
            Object.FindFirstObjectByType<GameFlowController>().StartNewRun();
            Object.FindFirstObjectByType<RunExpansionView>().HideModal();
            Object.FindFirstObjectByType<DungeonController>().HideNavigation();
        }

        [UnityTest] public IEnumerator Selection_AllSixCharacters_StartAndReturnWithSerializedUI()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/OutGameScene.unity");
            yield return new EnterPlayMode();
            for (var i = 0; i < 6; i++)
            {
                var selection = Object.FindFirstObjectByType<OutGameView>(); Assert.That(selection, Is.Not.Null);
                selection.characterButtons[i].onClick.Invoke();
                Assert.That(selection.stats.text, Does.Contain("가드 피해 감소")); Assert.That(selection.startingRelic.text.Length, Is.GreaterThan(30));
                selection.startButton.onClick.Invoke(); yield return null; yield return null;
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainGameScene"));
                var run = Object.FindFirstObjectByType<RunController>().State;
                Assert.That((int)run.Player.Data.character, Is.EqualTo(i)); Assert.That(run.EquippedItems.Contains(run.Player.Data.startingRelic), Is.True);
                var ui = Object.FindFirstObjectByType<RunExpansionView>(); Assert.That(ui.IsModalOpen, Is.True); ui.choices[0].button.onClick.Invoke();
                Assert.That(ui.IsModalOpen, Is.False);
                Matrix("선택 → 시작 → " + run.Player.Data.displayName + " 시작 유물·스탯·조작 안내");
                RunSession.ReturnToSelection(); yield return null; yield return null;
            }
            ScreenCapture.CaptureScreenshot("Logs/Behemoth-Selection.png"); yield return null;
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator Input_OverlappingGuardLiftRepress_ChargeSwipeAndSpellRelease()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity"); yield return new EnterPlayMode();
            SetupCharacter(CharacterKind.Wizard);
            var combat = Object.FindFirstObjectByType<CombatController>(); var input = Object.FindFirstObjectByType<CombatInputController>(); var view = Object.FindFirstObjectByType<CombatSceneView>();
            combat.PrepareFixedEncounter(combat.Run, Encounter("DeepTrial"), 1); combat.BeginPreparedCombat();
            var body = Screen(view, view.PlayerPosition);
            Call(input, "Begin", 101, body); yield return null;
            for (var cycle = 0; cycle < 4; cycle++)
            {
                Call(input, "Begin", 202, body + Vector2.right * 3); yield return null;
                Call(input, "End", 101, body, false); yield return null; Assert.That(input.IsGuardHeld, Is.True);
                Call(input, "Begin", 101, body); yield return null;
                Call(input, "End", 202, body, false); yield return null; Assert.That(input.IsGuardHeld, Is.True);
            }
            Assert.That(input.CastingSeconds, Is.GreaterThan(0));
            Call(input, "End", 101, body, false); yield return null; Assert.That(input.IsGuardHeld, Is.False); Assert.That(combat.Player.IsGuarding, Is.False);
            Matrix("가드 두 접촉 겹침 → 하나만 해제 → 다시 누르기 4회 / 전체 손가락 초기화 없음");

            var target = Actors(combat)[0]; var before = Runtime(target).CurrentHp;
            // This test isolates gesture release; opening preparation is covered separately.
            foreach (var spell in combat.Run.Spellbook) spell.ResetCooldown();
            Call(input, "Begin", 303, body);
            for (var until = Time.time + .72f; Time.time < until;) yield return null;
            Call(input, "End", 303, Screen(view, view.PlayerPosition + Vector2.up * 110), false); yield return null;
            Assert.That(combat.Run.SpellCasts, Is.EqualTo(1)); Assert.That(Runtime(target).CurrentHp, Is.LessThan(before));
            Assert.That(input.IsGuardHeld, Is.False); Matrix("홀드 캐스팅 → 위 방향 떼기 → 에너지 볼트 자동 타깃 적중·쿨타임");

            var guardTarget = Actors(combat)[0]; Runtime(guardTarget).StartCharge(Position(guardTarget));
            var points = Runtime(guardTarget).WeakPoints.ToArray(); Assert.That(points.Length, Is.GreaterThan(0));
            var center = Position(guardTarget); var start = center + Vector2.left * 110; var end = center + Vector2.right * 145;
            combat.Player.ResetAttackCooldown(); Call(input, "Begin", 404, Screen(view, start));
            for (var t = 0f; t < combat.Player.CurrentChargeDuration + .1f; t += Time.deltaTime)
            { Call(input, "Move", 404, Screen(view, start)); yield return null; }
            Call(input, "Move", 404, Screen(view, start)); yield return null;
            Call(input, "Move", 404, Screen(view, end)); Call(input, "End", 404, Screen(view, end), false); yield return null;
            Assert.That(points.All(point => point.RemainingHits == 0), Is.True, "The held physical swipe must break crossed weakpoints.");
            Matrix("전장 홀드 → 차지 궤적 → 약점 동시 파괴·기절");
            var afterCharge = Runtime(guardTarget).CurrentHp;
            combat.ExecuteAttack(new AttackSegment(start, end, 18), true);
            Assert.That(Runtime(guardTarget).CurrentHp, Is.EqualTo(afterCharge), "Parallel contacts cannot bypass the shared attack recovery.");
            Matrix("동시 차지 접촉도 공용 공격 대기시간 우회 불가");
            ScreenCapture.CaptureScreenshot("Logs/Behemoth-Combat.png"); yield return null;
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator Spell_TargetRules_PathPiercing_NoWeakpointBreak_AndModalPauses()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity"); yield return new EnterPlayMode(); SetupCharacter(CharacterKind.Wizard);
            var combat = Object.FindFirstObjectByType<CombatController>(); var run = combat.Run;
            combat.PrepareFixedEncounter(run, Encounter("TwinHeart"), 1); combat.BeginPreparedCombat();
            var actors = Actors(combat); var leader = Runtime(actors[0]); leader.StartCharge(Position(actors[0]));
            foreach (var spell in Content.spells) run.LearnSpell(spell);
            var fire = Content.spells.Single(item => item.id == "Ember"); var ice = Content.spells.Single(item => item.id == "EnergyBolt"); var lightning = Content.spells.Single(item => item.id == "Spark");
            Runtime(actors[1]).TakeDamage(Runtime(actors[1]).MaxHp * .8f);
            Assert.That(Call(combat, "ChooseSpellTarget", fire), Is.SameAs(actors[1]));
            Assert.That(Call(combat, "ChooseSpellTarget", ice), Is.SameAs(actors[0]));
            var pointHits = leader.WeakPoints.Sum(point => point.RemainingHits);
            foreach (var known in run.Spellbook) known.ResetCooldown();
            run.AssignSpell(SwipeDirection.Up, ice); Assert.That(combat.TryCastSpell(SwipeDirection.Up, 10), Is.True);
            Assert.That(leader.WeakPoints.Sum(point => point.RemainingHits), Is.EqualTo(pointHits));
            Matrix("화염 최저 체력 비율 / 얼음 최고 현재 체력 자동 타깃 / 주문은 약점 미파괴");
            var hp = leader.CurrentHp;
            var ui = Object.FindFirstObjectByType<RunExpansionView>(); ui.ShowMessage("멈춤 검증", "사건·주문 선택 중 전투 시간은 멈춘다.", null);
            var time = run.Player.CombatTime; var cooldown = run.RuntimeFor(ice).Cooldown;
            for (var until = Time.time + .35f; Time.time < until;) yield return null;
            Assert.That(run.Player.CombatTime, Is.EqualTo(time)); Assert.That(run.RuntimeFor(ice).Cooldown, Is.EqualTo(cooldown)); ui.HideModal();
            Matrix("선택창·이벤트 중 전투·버프·주문 쿨타임 일시정지");
            run.TryEquip(Content.equipment.Single(item => item.power == RelicPower.LightningRod)); Runtime(actors[2]).LightningRodUntil = run.Player.CombatTime + 8;
            Assert.That(Call(combat, "ChooseSpellTarget", lightning), Is.SameAs(actors[2]));
            // Author two bodies on the same spell ray. Damage must reach both, regardless of the priority target.
            var actorType = actors[0].GetType(); var p1 = new Vector2(0, 20); var p2 = new Vector2(0, 160);
            actorType.GetProperty("Position").SetValue(actors[0], p1); actorType.GetProperty("Position").SetValue(actors[2], p2);
            var rearHp = Runtime(actors[2]).CurrentHp; run.RuntimeFor(ice).ResetCooldown(); run.AssignSpell(SwipeDirection.Up, ice);
            combat.Player.ResetAttackCooldown();
            Assert.That(combat.TryCastSpell(SwipeDirection.Up, 10), Is.True); Assert.That(Runtime(actors[2]).CurrentHp, Is.LessThan(rearHp));
            Matrix("피뢰침 타깃 우선 / 단일 타깃 주문 경로의 뒤쪽 적도 관통");
            var bloom = Content.spells.Single(item => item.id == "FireBloom"); var frost = Content.spells.Single(item => item.id == "FrostRing");
            actorType.GetProperty("Position").SetValue(actors[1], new Vector2(-225, -40));
            run.AssignSpell(SwipeDirection.Left, bloom); var leaderBeforeBurst = leader.CurrentHp; var outsideBurst = Runtime(actors[2]).CurrentHp;
            combat.Player.ResetAttackCooldown();
            Assert.That(combat.TryCastSpell(SwipeDirection.Left, 10), Is.True);
            Assert.That(leader.CurrentHp, Is.LessThan(leaderBeforeBurst)); Assert.That(Runtime(actors[2]).CurrentHp, Is.EqualTo(outsideBurst), "A burst is local to its priority target, not global damage.");
            run.LearnSpell(frost, true); combat.Player.ResetAttackCooldown(); Assert.That(combat.TryCastSpell(SwipeDirection.Left, 10), Is.True);
            foreach(var actor in actors)Runtime(actor).Buffs.Tick(0);
            Assert.That(leader.SlowRemaining, Is.GreaterThan(0)); Assert.That(Runtime(actors[2]).SlowRemaining, Is.GreaterThan(0));
            Matrix("폭발은 타깃 주변 실제 범위만 적중 / 서리 파동의 범위 둔화");
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator Events_CostRewardFollowupPaging_CompanionActuallyAttacks()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity"); yield return new EnterPlayMode(); SetupCharacter(CharacterKind.Warrior);
            var combat = Object.FindFirstObjectByType<CombatController>(); var flow = Object.FindFirstObjectByType<GameFlowController>(); var run = combat.Run;
            var ui = Object.FindFirstObjectByType<RunExpansionView>(); run.GainGold(100);
            // Exercise the actual event dispatcher and reward continuation on a known, eligible story.
            var story = Content.events.Single(item => item.id == "StrayDog");
            var option = story.options[1]; Assert.That(RandomEventSystem.Pay(option, run), Is.True);
            Call(flow, "ApplyEventEffects", story, option.effects, 0, new System.Collections.Generic.List<string>());
            Assert.That(run.Player.HasPower(RelicPower.StrayDog), Is.True); Assert.That(ui.IsModalOpen, Is.True);
            ScreenCapture.CaptureScreenshot("Logs/Behemoth-Event.png"); yield return null; ui.HideModal();
            combat.PrepareFixedEncounter(run, Encounter("Trial"), 1); combat.BeginPreparedCombat();
            var enemy = Runtime(Actors(combat)[0]); var hp = enemy.CurrentHp;
            combat.Player.TrySetGuard(true);
            for (var until = Time.time + 4.15f; Time.time < until;) yield return null;
            Assert.That(enemy.CurrentHp, Is.LessThan(hp)); Matrix("강아지 사건 대가 → 전용 유물 획득 → 동료의 실제 자동 협공");
            ui.ShowModal("긴 선택 목록", "모든 후보 접근 가능", Enumerable.Range(0, 11).Select(i => new PresentationChoice { Title = "후보 " + i }));
            Assert.That(ui.nextPage.gameObject.activeSelf, Is.True); ui.nextPage.onClick.Invoke(); Assert.That(ui.choices[0].title.text, Is.EqualTo("후보 4"));
            ui.nextPage.onClick.Invoke(); Assert.That(ui.choices[0].title.text, Is.EqualTo("후보 8")); Assert.That(ui.choices[3].button.gameObject.activeSelf, Is.False);
            Matrix("주문·유물 보유량이 많아도 후보 페이지 누락 없음");
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator LinkedHearts_CancelDependentAttack_ThenStunBody()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity"); yield return new EnterPlayMode(); SetupCharacter(CharacterKind.Warrior);
            var combat = Object.FindFirstObjectByType<CombatController>(); combat.PrepareFixedEncounter(combat.Run, Encounter("TwinHeart"), 1); combat.BeginPreparedCombat();
            yield return null; var actors = Actors(combat); var leader = Runtime(actors[0]);
            leader.StartCharge(Position(actors[0])); Assert.That(leader.CurrentChargeAttack.requiredSupportId, Is.EqualTo("left_heart"));
            Runtime(actors[1]).TakeDamage(9999); yield return null; Assert.That(leader.State, Is.EqualTo(MonsterState.Idle));
            Runtime(actors[2]).TakeDamage(9999); yield return null; Assert.That(leader.IsStunned, Is.True);
            Matrix("왼심장 파괴로 연타 취소 → 오른심장 파괴로 본체 기절");
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator ClassCoreLoops_RealWeaponHits_StealthRageDrawAndMonkMultiHit()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity"); yield return new EnterPlayMode();
            foreach (var kind in new[] { CharacterKind.Rogue, CharacterKind.Barbarian, CharacterKind.Samurai, CharacterKind.Monk })
            {
                SetupCharacter(kind); var combat = Object.FindFirstObjectByType<CombatController>();
                combat.PrepareFixedEncounter(combat.Run, Encounter("Trial"), 1); combat.BeginPreparedCombat();
                var actor = Actors(combat)[0]; var enemy = Runtime(actor); var center = Position(actor);
                var horizontal = new AttackSegment(center + Vector2.left * 160, center + Vector2.right * 160, 18);
                if (kind == CharacterKind.Rogue)
                {
                    Assert.That(enemy.IsUnaware, Is.True); var timer = enemy.NormalAttackTimer;
                    for (var until = Time.time + 2.1f; Time.time < until;) yield return null;
                    Assert.That(enemy.NormalAttackTimer, Is.EqualTo(timer));
                    combat.ExecuteAttack(horizontal, false); yield return null;
                    Assert.That(enemy.PoisonStacks, Is.Zero); Assert.That(enemy.IsUnaware, Is.False);
                    Assert.That(enemy.NormalAttackTimer, Is.LessThan(timer)); Matrix("로그 은신 대기 → 일반 적중 → 독 없이 전체 발각·기존 쿨타임 시작");
                }
                else if (kind == CharacterKind.Barbarian)
                {
                    for (var i = 0; i < 4; i++) { combat.Player.ResetAttackCooldown(); combat.ExecuteAttack(horizontal, false); }
                    var rage=AssetDatabase.LoadAssetAtPath<BuffData>("Assets/Data/Behemoth/Buffs/Rage.asset");
                    Assert.That(combat.Run.Buffs.Has(rage), Is.False); combat.Player.ResetAttackCooldown(); combat.ExecuteAttack(horizontal, true);yield return null; Assert.That(combat.Run.Buffs.Has(rage), Is.True);
                    for (var until = Time.time + 5.1f; Time.time < until;) yield return null;
                    Assert.That(combat.Run.Buffs.Has(rage), Is.False); Matrix("바바리안 격노 중첩 → 같은 양의 분노 중첩으로 전환 → 실제 소모 종료");
                }
                else if (kind == CharacterKind.Samurai)
                {
                    combat.Player.TrySetGuard(true);combat.Player.Buffs.Tick(0); var parry = new DamageResolver().ApplyToPlayer(combat.Player, new DamageRequest(DamageType.MonsterAttack, 15, 15, true));combat.Player.Buffs.Tick(0);
                    Assert.That(parry.WasSheathParry, Is.True); enemy.StartCharge(center); var points = enemy.WeakPoints.ToArray();
                    combat.ExecuteAttack(horizontal, false);combat.Player.Buffs.Tick(0); Assert.That(points.All(point => point.RemainingHits == 0), Is.True); Assert.That(combat.Player.Buffs.Active.Any(b => b.Data.name == "Sheathed"), Is.False);
                    Matrix("사무라이 납도 패리 → 일반 입력의 차지 발도 → 약점 파괴");
                }
                else
                {
                    var buff=combat.Player.Data.attackBuffs.First(b=>b.name=="Multi" && !b.requiresUnlock);
                    foreach(var direction in buff.attackPattern){var vector=direction==SwipeDirection.Left?Vector2.left:direction==SwipeDirection.Right?Vector2.right:direction==SwipeDirection.Up?Vector2.up:Vector2.down;combat.Player.ResetAttackCooldown();combat.ExecuteAttack(new AttackSegment(center-vector*140,center+vector*140,18),false);}
                    enemy.StartCharge(center); var points = enemy.WeakPoints.ToArray();
                    combat.Player.ResetAttackCooldown(); combat.ExecuteAttack(horizontal, true);
                    Assert.That(points.All(point => point.RemainingHits == 0), Is.True); Assert.That(combat.Player.Combo.Count, Is.Zero);
                    Matrix("몽크 ↑↑ 준비 → 가로 차지 백렬권 → 다중 약점 적중·초식 초기화");
                }
                yield return null;
            }
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator OptionalEventFight_WinReward_ThenEventOnlyRelic_ThenNavigation()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity"); yield return new EnterPlayMode(); SetupCharacter(CharacterKind.Warrior);
            var flow = Object.FindFirstObjectByType<GameFlowController>(); var combat = Object.FindFirstObjectByType<CombatController>(); var ui = Object.FindFirstObjectByType<RunExpansionView>();
            var story = Content.events.Single(item => item.id == "Banner"); var option = story.options[0];
            Call(flow, "ApplyEventEffects", story, option.effects, 0, new System.Collections.Generic.List<string>());
            Assert.That(ui.IsModalOpen, Is.True); ui.choices[0].button.onClick.Invoke(); Assert.That(combat.IsActive, Is.True);
            combat.Player.ApplyModifiers(new[] { new StatModifier { kind = ModifierKind.AllDamageMultiplier, value = 100 } });
            var actor = Actors(combat)[0]; var center = Position(actor);
            combat.ExecuteAttack(new AttackSegment(center + Vector2.left * 160, center + Vector2.right * 160, 18), true);
            var deadline = Time.time + 6;
            while (flow.State != GameFlowState.RoomReward && Time.time < deadline) yield return null;
            Assert.That(flow.State, Is.EqualTo(GameFlowState.RoomReward));
            var reward = Object.FindFirstObjectByType<RewardPopupView>(); var confirm = Field<UnityEngine.UI.Button>(reward, "confirmButton");
            while (!confirm.interactable && Time.time < deadline) yield return null;
            Assert.That(confirm.interactable, Is.True); confirm.onClick.Invoke();
            Assert.That(combat.Run.Player.HasPower(RelicPower.RaggedBanner), Is.True); Assert.That(ui.IsModalOpen, Is.True);
            ui.choices[0].button.onClick.Invoke(); Assert.That(flow.State, Is.EqualTo(GameFlowState.DungeonNavigation));
            Matrix("추가 강적 사건 → 실제 전투 승리 → 전투 보상 → 사건 전용 깃발 → 지도 복귀");
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator ThreeActs_BossRewardsCompeteOnlyWithExclusives_HealAdvanceAndVictoryReturn()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity"); yield return new EnterPlayMode(); SetupCharacter(CharacterKind.Warrior);
            var flow = Object.FindFirstObjectByType<GameFlowController>(); var combat = Object.FindFirstObjectByType<CombatController>(); var dungeon = Object.FindFirstObjectByType<DungeonController>();
            var run = combat.Run; run.Player.ApplyModifiers(new[] { new StatModifier { kind = ModifierKind.AllDamageMultiplier, value = 1000 } });
            for (var act = 1; act <= 3; act++)
            {
                var boss = dungeon.State.Graph.Rooms.Single(room => room.Type == RoomEncounterType.Boss); dungeon.State.Player.MoveTo(boss); Call(flow, "EnterRoom", boss);
                var deadline = Time.time + 18;
                while (!combat.IsActive && Time.time < deadline) yield return null;
                Assert.That(combat.IsActive, Is.True); run.Player.TakeEnvironmentalDamage(10);
                foreach (var actor in Actors(combat).Where(actor => Runtime(actor).IsDirectionalGuardActive))
                {
                    var runtime = Runtime(actor); var center = Position(actor); var way = runtime.NormalAttack.combatMechanics.directionalGuard.breakByAttackWay;
                    var direction = way == TriggerAttackWayFilter.Upward ? Vector2.up : way == TriggerAttackWayFilter.Downward ? Vector2.down : Vector2.right;
                    run.Player.ResetAttackCooldown(); combat.ExecuteAttack(new AttackSegment(center - direction * 180, center + direction * 180, 18), true);
                    Assert.That(runtime.IsDirectionalGuardActive, Is.False);
                }
                run.Player.ResetAttackCooldown();
                combat.ExecuteAttack(new AttackSegment(new Vector2(-480, 105), new Vector2(480, 105), 360), true);
                while (Time.time < deadline && (act < 3 ? dungeon.Floor == act : flow.State != GameFlowState.RunVictory))
                {
                    var perkView = Object.FindFirstObjectByType<PerkChoiceView>();
                    var expansion = Object.FindFirstObjectByType<RunExpansionView>();
                    if (flow.State == GameFlowState.PerkChoice && expansion.IsModalOpen)
                    {
                        Assert.That(expansion.modalTitle.text, Does.Contain("전용 유물"));
                        var ownNames = Content.equipment.Where(item => item.scope == RelicScope.Exclusive && item.exclusiveCharacter == CharacterKind.Warrior).Select(item => item.displayName).ToArray();
                        foreach (var candidate in expansion.choices.Where(item => item.button.gameObject.activeSelf)) Assert.That(ownNames.Contains(candidate.title.text), Is.True);
                        ScreenCapture.CaptureScreenshot("Logs/Behemoth-ExclusiveChoice.png");
                        yield return null; // ScreenCapture samples at end of frame, before the selection closes.
                        expansion.choices[0].button.onClick.Invoke();
                    }
                    else if (flow.State == GameFlowState.PerkChoice && perkView != null && Field<GameObject>(perkView, "panel").activeSelf)
                    {
                        if (run.PendingPerkChoices == 0)
                        {
                            var title = Field<TextMeshProUGUI>(perkView, "title"); Assert.That(title.text, Does.Contain("전용 유물"));
                            var ownNames = Content.equipment.Where(item => item.scope == RelicScope.Exclusive && item.exclusiveCharacter == CharacterKind.Warrior).Select(item => item.displayName).ToArray();
                            foreach (var candidate in Field<System.Collections.Generic.List<PerkChoiceItem>>(perkView, "items").Where(item => item.gameObject.activeSelf))
                                Assert.That(ownNames.Any(name => Field<TextMeshProUGUI>(candidate, "label").text.StartsWith(name)), Is.True);
                        }
                        var first = Field<System.Collections.Generic.List<PerkChoiceItem>>(perkView, "items").First(item => item.gameObject.activeSelf);
                        Field<UnityEngine.UI.Button>(first, "button").onClick.Invoke();
                    }
                    else if (flow.State == GameFlowState.RoomReward)
                    {
                        var confirm = Field<UnityEngine.UI.Button>(Object.FindFirstObjectByType<RewardPopupView>(), "confirmButton");
                        if (confirm.interactable && confirm.gameObject.activeInHierarchy) confirm.onClick.Invoke();
                    }
                    yield return null;
                }
                if (act < 3)
                { Assert.That(dungeon.Floor, Is.EqualTo(act + 1)); Assert.That(run.Player.CurrentHp, Is.EqualTo(run.Player.MaxHp)); Assert.That(run.ExclusiveCount, Is.EqualTo(act)); }
                else Assert.That(flow.State, Is.EqualTo(GameFlowState.RunVictory));
                Matrix("B" + act + " 보스 전투 → 보상·퍽 처리 → " + (act < 3 ? "전용끼리 3택1·회복·다음 막" : "3막 승리 화면"));
            }
            Field<UnityEngine.UI.Button>(Object.FindFirstObjectByType<ResultView>(), "restart").onClick.Invoke(); yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("OutGameScene")); Matrix("최종 승리 → 캐릭터 선택 복귀");
            yield return new ExitPlayMode();
        }
    }
}
