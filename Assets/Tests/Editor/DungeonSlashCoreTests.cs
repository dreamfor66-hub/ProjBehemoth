using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DungeonSlash.Tests
{
    public sealed class DungeonSlashCoreTests
    {
        [Test]
        public void WideSegment_HitsBodyAndEveryCrossedWeakPoint()
        {
            var segment = new AttackSegment(new Vector2(-100f, 0f), new Vector2(100f, 0f), 10f);
            var left = new WeakPointRuntime(new WeakPointDefinition { id = "left", normalizedPosition = Vector2.zero, hitRadius = 8f }, new Vector2(-42f, 3f));
            var right = new WeakPointRuntime(new WeakPointDefinition { id = "right", normalizedPosition = Vector2.zero, hitRadius = 8f }, new Vector2(48f, -4f));

            Assert.That(SegmentHitResolver.Intersects(segment, new CircleHitShape(Vector2.zero, 25f)), Is.True);
            Assert.That(WeakPointHitResolver.Resolve(segment, new[] { left, right }).Select(point => point.Id), Is.EquivalentTo(new[] { "left", "right" }));
        }

        [Test]
        public void WeakPoint_ConsumesOnlyOneHitPerResolvedAttack()
        {
            var point = new WeakPointRuntime(new WeakPointDefinition { id = "core", normalizedPosition = Vector2.zero, hitRadius = 20f, requiredChargeHits = 2 }, Vector2.zero);
            Assert.That(point.ApplyChargeHit(), Is.True);
            Assert.That(point.RemainingHits, Is.EqualTo(1));
            Assert.That(point.ApplyChargeHit(), Is.True);
            Assert.That(point.IsDestroyed, Is.True);
            Assert.That(point.ApplyChargeHit(), Is.False);
        }

        [Test]
        public void CombatResult_PreservesEveryMonsterForGroupRewards()
        {
            var first = ScriptableObject.CreateInstance<MonsterData>();
            var second = ScriptableObject.CreateInstance<MonsterData>();
            var result = new CombatResult(true, false, new[] { first, second });

            Assert.That(result.Monsters, Is.EqualTo(new[] { first, second }));
            Assert.That(result.Monster, Is.EqualTo(first));
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
        }

        [Test]
        public void PlayerDownwardSwipe_IsPrioritizedAsGuardAcrossTheWholePlayerArea()
        {
            var player = new Vector2(0f, -275f);
            Assert.That(GuardGestureResolver.IsPlayerDownwardGuard(player, new Vector2(128f, -190f), new Vector2(118f, -300f), 70f), Is.True);
            Assert.That(GuardGestureResolver.IsPlayerDownwardGuard(player, new Vector2(0f, -220f), new Vector2(100f, -270f), 70f), Is.False, "A mostly horizontal motion must not guard.");
            Assert.That(GuardGestureResolver.IsPlayerDownwardGuard(player, new Vector2(172f, -210f), new Vector2(170f, -292f), 70f), Is.False, "Gestures that begin outside the player slot must not guard.");
        }

        [Test]
        public void SwipeGesture_RequiresFastStraightMotionInsteadOfReleaseEndpoints()
        {
            var analyzer = new SwipeGestureAnalyzer();
            analyzer.Reset(Vector2.zero, 0f);
            analyzer.AddSample(new Vector2(35f, 0f), .1f, .25f);
            analyzer.AddSample(new Vector2(70f, 0f), .2f, .25f);
            Assert.That(analyzer.TryGetGesture(68f, 520f, .8f, out _), Is.False, "A slow drag must not become an attack.");

            analyzer.Reset(Vector2.zero, 0f);
            analyzer.AddSample(new Vector2(38f, 0f), .03f, .12f);
            analyzer.AddSample(new Vector2(78f, 0f), .06f, .12f);
            Assert.That(analyzer.TryGetGesture(68f, 520f, .8f, out var swipe), Is.True);
            Assert.That(swipe.Direction, Is.EqualTo(Vector2.right));
            Assert.That(swipe.Speed, Is.GreaterThan(520f));
            var attackSegment = swipe.ToAttackSegment(18f);
            Assert.That(attackSegment.Start, Is.EqualTo(Vector2.zero));
            Assert.That(attackSegment.End, Is.EqualTo(new Vector2(78f, 0f)));

            analyzer.Reset(Vector2.zero, 0f);
            analyzer.AddSample(new Vector2(55f, 0f), .03f, .12f);
            analyzer.AddSample(new Vector2(55f, 55f), .06f, .12f);
            Assert.That(analyzer.TryGetGesture(68f, 520f, .8f, out _), Is.False, "A rubbing turn must not become a slash.");
        }

        [Test]
        public void ChargeArc_AppearsAtPointerAndFillsDuringCharge()
        {
            var root = new GameObject("HudTestRoot", typeof(RectTransform), typeof(Canvas));
            var playerHp = CreateBar(root.transform, "PlayerHp");
            var shield = CreateBar(root.transform, "Shield");
            var monsterHp = CreateBar(root.transform, "MonsterHp");
            var gaugeRoot = new GameObject("ChargeGaugeRoot", typeof(RectTransform));
            gaugeRoot.transform.SetParent(root.transform, false);
            var gaugeRect = gaugeRoot.GetComponent<RectTransform>();
            gaugeRect.pivot = new Vector2(.5f, 1f);
            gaugeRect.sizeDelta = new Vector2(150f, 96f);
            var track = new GameObject("Track", typeof(RectTransform), typeof(ArcGaugeGraphic));
            track.transform.SetParent(gaugeRoot.transform, false);
            track.GetComponent<RectTransform>().sizeDelta = new Vector2(150f, 96f);
            var trackGraphic = track.GetComponent<ArcGaugeGraphic>();
            trackGraphic.Configure(100f, 1f);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(ArcGaugeGraphic));
            fill.transform.SetParent(gaugeRoot.transform, false);
            fill.GetComponent<RectTransform>().sizeDelta = new Vector2(150f, 96f);
            var fillGraphic = fill.GetComponent<ArcGaugeGraphic>();
            fillGraphic.Configure(100f, 0f);
            var hudObject = new GameObject("Hud", typeof(CombatHudView));
            hudObject.transform.SetParent(root.transform, false);
            var hud = hudObject.GetComponent<CombatHudView>();
            hud.Configure(playerHp, shield, monsterHp, null, null, gaugeRect, trackGraphic, fillGraphic);
            var data = ScriptableObject.CreateInstance<PlayerCombatData>();
            data.maxHp = 100f;
            data.shieldMax = 100f;
            var player = new PlayerCombatRuntime(data);

            var pointer = new Vector2(36f, -82f);
            hud.Bind(player, null, PointerGestureState.Charging, .4f, pointer);
            Assert.That(gaugeRoot.activeSelf, Is.True);
            Assert.That(gaugeRect.anchoredPosition, Is.EqualTo(pointer + new Vector2(0f, 90f)));
            Assert.That(fillGraphic.FillAmount, Is.EqualTo(.4f).Within(.001f));
            Canvas.ForceUpdateCanvases();
            var trackMesh = trackGraphic.canvasRenderer.GetMesh();
            var fillMesh = fillGraphic.canvasRenderer.GetMesh();
            Assert.That(trackMesh.vertexCount, Is.GreaterThan(0), "The charge track must generate visible geometry.");
            Assert.That(fillMesh.vertexCount, Is.GreaterThan(0), "The filled charge arc must generate visible geometry.");

            hud.Bind(player, null, PointerGestureState.None, 0f, pointer);
            Assert.That(gaugeRoot.activeSelf, Is.False);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void MonsterView_ResetOpacity_DoesNotRequireCanvasGroup()
        {
            var root = new GameObject("Monster", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var labelObject = new GameObject("State", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            labelObject.transform.SetParent(root.transform, false);
            var view = root.AddComponent<MonsterView>();
            view.Configure(root.GetComponent<RectTransform>(), root.GetComponent<UnityEngine.UI.Image>(), labelObject.GetComponent<UnityEngine.UI.Text>());

            Assert.DoesNotThrow(() => view.ResetOpacity());
            Assert.That(root.GetComponent<UnityEngine.UI.Image>().color.a, Is.EqualTo(1f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void MonsterView_Bind_EmptiesTheHpGaugeImmediatelyAfterDeath()
        {
            var root = new GameObject("Monster", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var labelObject = new GameObject("State", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            labelObject.transform.SetParent(root.transform, false);
            var hpBackground = new GameObject("Hp", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            hpBackground.transform.SetParent(root.transform, false);
            hpBackground.GetComponent<RectTransform>().sizeDelta = new Vector2(100f, 10f);
            var hpFill = new GameObject("Fill", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            hpFill.transform.SetParent(hpBackground.transform, false);
            var view = root.AddComponent<MonsterView>();
            view.Configure(root.GetComponent<RectTransform>(), root.GetComponent<UnityEngine.UI.Image>(), labelObject.GetComponent<UnityEngine.UI.Text>(), hpFill.GetComponent<UnityEngine.UI.Image>(), null, null);
            var data = ScriptableObject.CreateInstance<MonsterData>();
            data.maxHp = 10f;
            var runtime = new MonsterRuntime(data);

            runtime.TakeDamage(10f);
            view.Bind(runtime);

            Assert.That(hpFill.GetComponent<RectTransform>().sizeDelta.x, Is.EqualTo(0f).Within(.001f));
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void PlayerHud_ShowsHpAndShieldValuesInsideTheirGauges()
        {
            var root = new GameObject("HudRoot", typeof(RectTransform), typeof(Canvas));
            var hp = CreateBar(root.transform, "Hp");
            var shield = CreateBar(root.transform, "Shield");
            var monster = CreateBar(root.transform, "Monster");
            var hpLabel = new GameObject("HpValue", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>(); hpLabel.transform.SetParent(hp.transform.parent, false);
            var shieldLabel = new GameObject("ShieldValue", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>(); shieldLabel.transform.SetParent(shield.transform.parent, false);
            var hud = new GameObject("Hud", typeof(CombatHudView)).GetComponent<CombatHudView>(); hud.transform.SetParent(root.transform, false);
            hud.Configure(hp, shield, monster, null, null, null, null, null, hpLabel, shieldLabel);
            var data = ScriptableObject.CreateInstance<PlayerCombatData>(); data.maxHp = 100f; data.shieldMax = 75f;

            hud.Bind(new PlayerCombatRuntime(data), null, PointerGestureState.None, 0f, Vector2.zero);

            Assert.That(hpLabel.text, Is.EqualTo("HP 100 / 100"));
            Assert.That(shieldLabel.text, Is.EqualTo("SHIELD 75 / 75"));
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void Shield_RechargesWhileIdleAndUsesMutedColorWhenGuardIsUnavailable()
        {
            var root = new GameObject("HudRoot", typeof(RectTransform), typeof(Canvas));
            var hp = CreateBar(root.transform, "Hp");
            var shield = CreateBar(root.transform, "Shield");
            var monster = CreateBar(root.transform, "Monster");
            var hud = new GameObject("Hud", typeof(CombatHudView)).GetComponent<CombatHudView>();
            hud.transform.SetParent(root.transform, false);
            hud.Configure(hp, shield, monster, null, null, null, null, null);
            var data = ScriptableObject.CreateInstance<PlayerCombatData>();
            data.maxHp = 100f;
            data.shieldMax = 100f;
            data.normalShieldRegen = 10f;
            var player = new PlayerCombatRuntime(data);
            player.TrySetGuard(true);
            new DamageResolver().ApplyToPlayer(player, new DamageRequest(DamageType.MonsterAttack, 0f, 60f, true));
            player.TrySetGuard(false);
            hud.Bind(player, null, PointerGestureState.None, 0f, Vector2.zero);
            var before = shield.rectTransform.sizeDelta.x;
            player.Tick(1f);
            hud.Bind(player, null, PointerGestureState.None, 0f, Vector2.zero);

            Assert.That(player.ShieldState, Is.EqualTo(ShieldState.Recharging));
            Assert.That(shield.rectTransform.sizeDelta.x, Is.GreaterThan(before));
            Assert.That(shield.color, Is.EqualTo(new Color(.96f, .98f, 1f, 1f)));
            Assert.That(player.TrySetGuard(true), Is.True, "A partially recharged shield must remain guardable.");
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void Guard_BreaksOnlyAtZeroAndTheBreakingHitDealsNoHpDamage()
        {
            var data = ScriptableObject.CreateInstance<PlayerCombatData>();
            data.maxHp = 100f;
            data.shieldMax = 30f;
            data.brokenShieldRegen = 5f;
            var player = new PlayerCombatRuntime(data);
            player.TrySetGuard(true);

            var result = new DamageResolver().ApplyToPlayer(player, new DamageRequest(DamageType.MonsterAttack, 99f, 30f, true));

            Assert.That(result.HpDamage, Is.EqualTo(0f));
            Assert.That(player.CurrentHp, Is.EqualTo(100f));
            Assert.That(player.ShieldCurrent, Is.EqualTo(0f));
            Assert.That(player.ShieldState, Is.EqualTo(ShieldState.Broken));
            Assert.That(player.TrySetGuard(true), Is.False);
            player.Tick(1f);
            Assert.That(player.ShieldCurrent, Is.EqualTo(5f));
            Assert.That(player.ShieldState, Is.EqualTo(ShieldState.Broken));
            Object.DestroyImmediate(data);
        }

        [Test]
        public void Shield_IsFullyRestoredAndGuardReadyWhenReturningToNavigation()
        {
            var data = ScriptableObject.CreateInstance<PlayerCombatData>();
            data.maxHp = 100f;
            data.shieldMax = 100f;
            var player = new PlayerCombatRuntime(data);
            player.TrySetGuard(true);
            new DamageResolver().ApplyToPlayer(player, new DamageRequest(DamageType.MonsterAttack, 0f, 100f, true));
            Assert.That(player.ShieldState, Is.EqualTo(ShieldState.Broken));

            player.RestoreShieldForNavigation();

            Assert.That(player.ShieldCurrent, Is.EqualTo(player.ShieldMax));
            Assert.That(player.ShieldState, Is.EqualTo(ShieldState.Ready));
            Assert.That(player.TrySetGuard(true), Is.True);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void AttackCooldown_FillsFromZeroThenHidesAtCompletion()
        {
            var root = new GameObject("Player", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var cooldown = new GameObject("Cooldown", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
            cooldown.transform.SetParent(root.transform, false);
            cooldown.type = UnityEngine.UI.Image.Type.Filled;
            var view = root.AddComponent<PlayerView>();
            view.Configure(root.GetComponent<RectTransform>(), root.GetComponent<UnityEngine.UI.Image>(), null, null, cooldown);
            var data = ScriptableObject.CreateInstance<PlayerCombatData>(); data.maxHp = 100f; data.shieldMax = 100f; data.attackCooldown = 1f; data.minimumAttackCooldown = .1f;
            var player = new PlayerCombatRuntime(data);
            var fullCooldownWidth = cooldown.rectTransform.sizeDelta.x;

            player.StartAttackCooldown();
            view.Bind(player);
            Assert.That(cooldown.enabled, Is.True);
            Assert.That(cooldown.fillAmount, Is.EqualTo(0f).Within(.001f));
            Assert.That(cooldown.rectTransform.sizeDelta.x, Is.EqualTo(0f).Within(.001f));
            player.Tick(.5f);
            view.Bind(player);
            Assert.That(cooldown.fillAmount, Is.EqualTo(.5f).Within(.001f));
            Assert.That(cooldown.rectTransform.sizeDelta.x, Is.EqualTo(fullCooldownWidth * .5f).Within(.01f));
            player.Tick(.5f);
            view.Bind(player);
            Assert.That(cooldown.enabled, Is.False);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void CombatStart_ResetClearsAnyRemainingAttackCooldown()
        {
            var data = ScriptableObject.CreateInstance<PlayerCombatData>();
            data.maxHp = 100f;
            data.shieldMax = 100f;
            var player = new PlayerCombatRuntime(data);
            player.StartAttackCooldown();
            Assert.That(player.AttackCooldownRemaining, Is.GreaterThan(0f));

            player.ResetAttackCooldown();

            Assert.That(player.AttackCooldownRemaining, Is.EqualTo(0f));
            Assert.That(player.CanNormalAttack(), Is.True);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void RewardExperienceGauge_UsesCurrentExperienceOverRequiredExperience()
        {
            var root = new GameObject("RewardRoot", typeof(RectTransform), typeof(Canvas));
            var panel = new GameObject("Panel", typeof(RectTransform)); panel.transform.SetParent(root.transform, false);
            var gauge = new GameObject("Gauge", typeof(RectTransform), typeof(UnityEngine.UI.Image)); gauge.transform.SetParent(panel.transform, false); gauge.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 20f);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>(); fill.transform.SetParent(gauge.transform, false); fill.rectTransform.sizeDelta = new Vector2(200f, 20f);
            var experienceText = new GameObject("Experience", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>(); experienceText.transform.SetParent(panel.transform, false);
            var goldText = new GameObject("Gold", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>(); goldText.transform.SetParent(panel.transform, false);
            var buttonRoot = new GameObject("Confirm", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button)); buttonRoot.transform.SetParent(panel.transform, false);
            var popup = new GameObject("Reward", typeof(RewardPopupView)).GetComponent<RewardPopupView>(); popup.Configure(panel, experienceText, goldText, fill, buttonRoot.GetComponent<UnityEngine.UI.Button>());
            var playerData = ScriptableObject.CreateInstance<PlayerCombatData>(); playerData.maxHp = 100f; playerData.shieldMax = 100f;
            var balance = ScriptableObject.CreateInstance<RunBalanceSettings>(); balance.initialExperienceRequirement = 100;
            var state = new RunState(playerData, balance); state.GainExperience(50);

            popup.ResumeAfterPerk(new CombatReward(1, 0), state, null);

            Assert.That(fill.rectTransform.sizeDelta.x, Is.EqualTo(100f).Within(.01f));
            Assert.That(experienceText.text, Is.EqualTo("LV 1  EXP 0050 / 0100"));
            Assert.That(experienceText.gameObject.activeSelf, Is.True);
            Assert.That(goldText.gameObject.activeSelf, Is.False);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(popup.gameObject);
            Object.DestroyImmediate(playerData);
            Object.DestroyImmediate(balance);
        }

        [Test]
        public void RewardPopup_ShowsOnlyTheRewardRowsThatWereActuallyEarned()
        {
            var root = new GameObject("RewardRoot", typeof(RectTransform), typeof(Canvas));
            var panel = new GameObject("Panel", typeof(RectTransform)); panel.transform.SetParent(root.transform, false);
            var gauge = new GameObject("Gauge", typeof(RectTransform), typeof(UnityEngine.UI.Image)); gauge.transform.SetParent(panel.transform, false);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>(); fill.transform.SetParent(gauge.transform, false);
            var experienceText = new GameObject("Experience", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>(); experienceText.transform.SetParent(panel.transform, false);
            var goldText = new GameObject("Gold", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>(); goldText.transform.SetParent(panel.transform, false);
            var buttonRoot = new GameObject("Confirm", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button)); buttonRoot.transform.SetParent(panel.transform, false);
            var popup = new GameObject("Reward", typeof(RewardPopupView)).GetComponent<RewardPopupView>(); popup.Configure(panel, experienceText, goldText, fill, buttonRoot.GetComponent<UnityEngine.UI.Button>());
            var playerData = ScriptableObject.CreateInstance<PlayerCombatData>(); playerData.maxHp = 100f; playerData.shieldMax = 100f;
            var balance = ScriptableObject.CreateInstance<RunBalanceSettings>(); balance.initialExperienceRequirement = 100;
            var state = new RunState(playerData, balance);

            popup.Show(new CombatReward(0, 24), 1, 0, 100, state, null, null);

            Assert.That(experienceText.gameObject.activeSelf, Is.False);
            Assert.That(gauge.activeSelf, Is.False);
            Assert.That(goldText.gameObject.activeSelf, Is.True);
            Assert.That(goldText.text, Does.Contain("0024"));
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(popup.gameObject);
            Object.DestroyImmediate(playerData);
            Object.DestroyImmediate(balance);
        }

        [Test]
        public void RunHud_HidesTheCurrentRoomConceptText()
        {
            var roomText = new GameObject("Room", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>();
            var runText = new GameObject("Run", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>();
            var perkText = new GameObject("Perk", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>();
            var view = new GameObject("RunHud", typeof(RunHudView)).GetComponent<RunHudView>(); view.Configure(roomText, runText, perkText);
            var playerData = ScriptableObject.CreateInstance<PlayerCombatData>(); playerData.maxHp = 100f; playerData.shieldMax = 100f;
            var balance = ScriptableObject.CreateInstance<RunBalanceSettings>(); balance.initialExperienceRequirement = 100;
            var graph = new DungeonGraph(); graph.AddRoom(new DungeonPosition(0, 0));

            view.Bind(new DungeonRunState(1, graph), new RunState(playerData, balance));

            Assert.That(roomText.enabled, Is.False);
            Assert.That(roomText.text, Is.Empty);
            Object.DestroyImmediate(view.gameObject);
            Object.DestroyImmediate(roomText.gameObject);
            Object.DestroyImmediate(runText.gameObject);
            Object.DestroyImmediate(perkText.gameObject);
            Object.DestroyImmediate(playerData);
            Object.DestroyImmediate(balance);
        }

        [Test]
        public void DungeonGeneration_IsDeterministicAndRespectsBossDistance()
        {
            var settings = ScriptableObject.CreateInstance<DungeonGenerationSettings>();
            settings.minRooms = 20; settings.maxRooms = 20; settings.minimumBossDistance = 7; settings.minimumMajorRewardDistance = 4;
            settings.minimumRewardRooms = 2; settings.minimumFountainRooms = 1; settings.minimumShopRooms = 1; settings.minimumGoddessRooms = 1; settings.eliteRoomCount = 1; settings.majorRewardRoomCount = 1; settings.loopChance = .2f; settings.maxGenerationAttempts = 200;
            var generator = new DungeonGenerator(settings);
            var first = generator.Generate(4512); var second = generator.Generate(4512);
            Assert.That(first.Graph.Rooms.Count, Is.EqualTo(20));
            Assert.That(first.Graph.Rooms.Select(room => (room.Position, room.Type)), Is.EqualTo(second.Graph.Rooms.Select(room => (room.Position, room.Type))));
            var distances = first.Graph.DistancesFrom(first.Graph.StartRoom);
            Assert.That(first.Graph.Rooms.Any(room => room.Type == RoomEncounterType.Boss && distances[room.RoomId] >= settings.minimumBossDistance), Is.True);
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void RelativeDirections_FollowThePlayerFacing()
        {
            Assert.That(DirectionUtility.ToAbsolute(FacingDirection.South, RelativeDirection.Forward), Is.EqualTo(FacingDirection.South));
            Assert.That(DirectionUtility.ToAbsolute(FacingDirection.South, RelativeDirection.Left), Is.EqualTo(FacingDirection.East));
            Assert.That(DirectionUtility.ToRelative(FacingDirection.North, FacingDirection.West), Is.EqualTo(RelativeDirection.Left));
            Assert.That(DirectionUtility.FromDelta(new DungeonPosition(0, -1)), Is.EqualTo(FacingDirection.South));
        }

        [Test]
        public void NavigationButtons_StayRelativeToFacingInsteadOfWorldNorth()
        {
            var root = new GameObject("Choice", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            var label = new GameObject("Label", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>(); label.transform.SetParent(root.transform, false);
            var choice = root.AddComponent<RoomChoiceButton>(); choice.Configure(root.GetComponent<UnityEngine.UI.Button>(), label);

            choice.Bind(RelativeDirection.Forward, FacingDirection.South, true, _ => { });
            Assert.That(root.GetComponent<RectTransform>().anchoredPosition, Is.EqualTo(new Vector2(0f, 180f)));
            Assert.That(label.text, Is.EqualTo("\uC804\uC9C4"));
            choice.Bind(RelativeDirection.Back, FacingDirection.North, true, _ => { });
            Assert.That(root.GetComponent<RectTransform>().anchoredPosition, Is.EqualTo(new Vector2(0f, -112f)));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void NavigationHover_ReportsTheAbsoluteDirectionForMinimapHighlighting()
        {
            var root = new GameObject("Choice", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            var label = new GameObject("Label", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>();
            label.transform.SetParent(root.transform, false);
            var choice = root.AddComponent<RoomChoiceButton>();
            choice.Configure(root.GetComponent<UnityEngine.UI.Button>(), label);
            FacingDirection? hovered = null;

            choice.Bind(RelativeDirection.Right, FacingDirection.West, true, _ => { }, direction => hovered = direction);
            choice.OnPointerEnter(null);
            Assert.That(hovered, Is.EqualTo(FacingDirection.West));
            choice.OnPointerExit(null);
            Assert.That(hovered, Is.Null);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void Minimap_UsesKoreanRoomNamesInsteadOfEnglishInitials()
        {
            var root = new GameObject("MapIcon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var label = new GameObject("Label", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>(); label.transform.SetParent(root.transform, false);
            var icon = root.AddComponent<MapRoomIcon>(); icon.Configure(root.GetComponent<UnityEngine.UI.Image>(), label);
            var room = new DungeonRoom(1, new DungeonPosition(0, 0)) { Type = RoomEncounterType.Boss, IsRevealed = true };

            icon.Bind(room, false, FacingDirection.North);

            Assert.That(label.text, Is.EqualTo("\uBCF4\uC2A4"));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void Minimap_CurrentRoomUsesOnlyFacingArrowWhileAdjacentRoomStaysUnknownDuringNavigation()
        {
            var root = new GameObject("MapRoot", typeof(RectTransform), typeof(Canvas));
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.transform.SetParent(root.transform, false);
            content.sizeDelta = new Vector2(260f, 260f);
            var iconRoot = new GameObject("IconPrefab", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var iconLabel = new GameObject("Label", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>(); iconLabel.transform.SetParent(iconRoot.transform, false);
            var arrow = new GameObject("Arrow", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>(); arrow.transform.SetParent(iconRoot.transform, false);
            var iconPrefab = iconRoot.AddComponent<MapRoomIcon>(); iconPrefab.Configure(iconRoot.GetComponent<UnityEngine.UI.Image>(), iconLabel, arrow);
            var connectionRoot = new GameObject("ConnectionPrefab", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var view = root.AddComponent<DungeonMapView>(); view.Configure(content, iconPrefab, connectionRoot.GetComponent<UnityEngine.UI.Image>());
            var graph = new DungeonGraph();
            var start = graph.AddRoom(new DungeonPosition(0, 0)); start.Type = RoomEncounterType.Fountain; start.IsCleared = true;
            var unknown = graph.AddRoom(new DungeonPosition(0, 1)); unknown.Type = RoomEncounterType.Combat;
            graph.Connect(start, unknown);
            var state = new DungeonRunState(1, graph);

            view.Render(state, FacingDirection.North);
            var icons = content.GetComponentsInChildren<MapRoomIcon>();
            Assert.That(icons.Length, Is.EqualTo(2));
            var currentIcon = icons.Single(icon => icon.GetComponent<RectTransform>().anchoredPosition == Vector2.zero);
            var currentLabel = currentIcon.transform.Find("Label").GetComponent<UnityEngine.UI.Text>();
            Assert.That(currentLabel.enabled, Is.False);
            Assert.That(currentLabel.text, Is.Empty);
            var facingArrow = currentIcon.transform.Find("Arrow").GetComponent<UnityEngine.UI.Text>();
            Assert.That(facingArrow.enabled, Is.True);
            Assert.That(facingArrow.rectTransform.anchoredPosition.y, Is.GreaterThan(0f));
            var unknownIcon = icons.Single(icon => icon != currentIcon);
            Assert.That(unknownIcon.transform.Find("Label").GetComponent<UnityEngine.UI.Text>().text, Is.EqualTo("?"));
            Assert.That(unknownIcon.GetComponent<UnityEngine.UI.Image>().color, Is.EqualTo(new Color(1f, .76f, .18f, 1f)));
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(iconRoot);
            Object.DestroyImmediate(connectionRoot);
        }

        [Test]
        public void Minimap_HidesUnknownTargetsOutsideNavigation()
        {
            var root = new GameObject("MapRoot", typeof(RectTransform), typeof(Canvas));
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.transform.SetParent(root.transform, false);
            content.sizeDelta = new Vector2(260f, 260f);
            var iconRoot = new GameObject("IconPrefab", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var iconLabel = new GameObject("Label", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>(); iconLabel.transform.SetParent(iconRoot.transform, false);
            var arrow = new GameObject("Arrow", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>(); arrow.transform.SetParent(iconRoot.transform, false);
            var iconPrefab = iconRoot.AddComponent<MapRoomIcon>(); iconPrefab.Configure(iconRoot.GetComponent<UnityEngine.UI.Image>(), iconLabel, arrow);
            var connectionRoot = new GameObject("ConnectionPrefab", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var view = root.AddComponent<DungeonMapView>(); view.Configure(content, iconPrefab, connectionRoot.GetComponent<UnityEngine.UI.Image>());
            var graph = new DungeonGraph();
            var current = graph.AddRoom(new DungeonPosition(0, 0)); current.Type = RoomEncounterType.Combat;
            var unknown = graph.AddRoom(new DungeonPosition(0, 1)); unknown.Type = RoomEncounterType.Shop;
            graph.Connect(current, unknown);
            var state = new DungeonRunState(1, graph);

            view.Render(state, null, true);
            Assert.That(content.GetComponentsInChildren<MapRoomIcon>().Length, Is.EqualTo(2));
            view.Render(state, null, false);
            Assert.That(content.GetComponentsInChildren<MapRoomIcon>().Length, Is.EqualTo(1));
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(iconRoot);
            Object.DestroyImmediate(connectionRoot);
        }

        [Test]
        public void Minimap_UsesFixedSpacingInsteadOfShrinkingForLongExploration()
        {
            var root = new GameObject("MapRoot", typeof(RectTransform), typeof(Canvas));
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.transform.SetParent(root.transform, false);
            content.sizeDelta = new Vector2(280f, 280f);
            var iconRoot = new GameObject("IconPrefab", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var iconLabel = new GameObject("Label", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>(); iconLabel.transform.SetParent(iconRoot.transform, false);
            var arrow = new GameObject("Arrow", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>(); arrow.transform.SetParent(iconRoot.transform, false);
            var iconPrefab = iconRoot.AddComponent<MapRoomIcon>(); iconPrefab.Configure(iconRoot.GetComponent<UnityEngine.UI.Image>(), iconLabel, arrow);
            var connectionRoot = new GameObject("ConnectionPrefab", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var view = root.AddComponent<DungeonMapView>(); view.Configure(content, iconPrefab, connectionRoot.GetComponent<UnityEngine.UI.Image>());
            var graph = new DungeonGraph();
            graph.AddRoom(new DungeonPosition(0, 0));
            var distantVisited = graph.AddRoom(new DungeonPosition(4, 0)); distantVisited.IsVisited = true; distantVisited.IsCleared = true;
            var state = new DungeonRunState(1, graph);

            view.Render(state, null, false);

            var distantIcon = content.GetComponentsInChildren<MapRoomIcon>().Single(icon => icon.GetComponent<RectTransform>().anchoredPosition.x > 0f);
            Assert.That(distantIcon.GetComponent<RectTransform>().anchoredPosition.x, Is.EqualTo(168f).Within(.01f));
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(iconRoot);
            Object.DestroyImmediate(connectionRoot);
        }

        [Test]
        public void Minimap_VisitedHubKeepsEveryExitAfterThePlayerLeaves()
        {
            var root = new GameObject("MapRoot", typeof(RectTransform), typeof(Canvas));
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.transform.SetParent(root.transform, false);
            content.sizeDelta = new Vector2(260f, 260f);
            var iconRoot = new GameObject("IconPrefab", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var iconLabel = new GameObject("Label", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>(); iconLabel.transform.SetParent(iconRoot.transform, false);
            var arrow = new GameObject("Arrow", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>(); arrow.transform.SetParent(iconRoot.transform, false);
            var north = new GameObject("RouteNorth", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>(); north.transform.SetParent(iconRoot.transform, false);
            var east = new GameObject("RouteEast", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>(); east.transform.SetParent(iconRoot.transform, false);
            var south = new GameObject("RouteSouth", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>(); south.transform.SetParent(iconRoot.transform, false);
            var west = new GameObject("RouteWest", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>(); west.transform.SetParent(iconRoot.transform, false);
            var iconPrefab = iconRoot.AddComponent<MapRoomIcon>(); iconPrefab.Configure(iconRoot.GetComponent<UnityEngine.UI.Image>(), iconLabel, arrow, north, east, south, west);
            var connectionRoot = new GameObject("ConnectionPrefab", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var view = root.AddComponent<DungeonMapView>(); view.Configure(content, iconPrefab, connectionRoot.GetComponent<UnityEngine.UI.Image>());
            var graph = new DungeonGraph();
            var hub = graph.AddRoom(new DungeonPosition(0, 0)); hub.IsCleared = true;
            var northRoom = graph.AddRoom(new DungeonPosition(0, 1));
            var eastRoom = graph.AddRoom(new DungeonPosition(1, 0));
            var southRoom = graph.AddRoom(new DungeonPosition(0, -1));
            var westRoom = graph.AddRoom(new DungeonPosition(-1, 0));
            graph.Connect(hub, northRoom); graph.Connect(hub, eastRoom); graph.Connect(hub, southRoom); graph.Connect(hub, westRoom);
            var state = new DungeonRunState(1, graph);
            state.Player.MoveTo(southRoom);

            view.Render(state, null, false);
            var hubIcon = content.GetComponentsInChildren<MapRoomIcon>().Single(icon => icon.GetComponent<RectTransform>().anchoredPosition.y > 0f);
            foreach (var routeName in new[] { "RouteNorth", "RouteEast", "RouteSouth", "RouteWest" })
                Assert.That(hubIcon.transform.Find(routeName).GetComponent<UnityEngine.UI.Image>().enabled, Is.True, $"Visited hub must retain its {routeName} exit.");
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(iconRoot);
            Object.DestroyImmediate(connectionRoot);
        }

        [Test]
        public void Minimap_KeepsEveryVisitedRoomAsAnUnlabeledConnectedTile()
        {
            var root = new GameObject("MapRoot", typeof(RectTransform), typeof(Canvas));
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.transform.SetParent(root.transform, false);
            content.sizeDelta = new Vector2(260f, 260f);
            var iconRoot = new GameObject("IconPrefab", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var iconLabel = new GameObject("Label", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>(); iconLabel.transform.SetParent(iconRoot.transform, false);
            var arrow = new GameObject("Arrow", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>(); arrow.transform.SetParent(iconRoot.transform, false);
            var iconPrefab = iconRoot.AddComponent<MapRoomIcon>(); iconPrefab.Configure(iconRoot.GetComponent<UnityEngine.UI.Image>(), iconLabel, arrow);
            var connectionRoot = new GameObject("ConnectionPrefab", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var view = root.AddComponent<DungeonMapView>(); view.Configure(content, iconPrefab, connectionRoot.GetComponent<UnityEngine.UI.Image>());
            var graph = new DungeonGraph();
            var start = graph.AddRoom(new DungeonPosition(0, 0)); start.Type = RoomEncounterType.Start; start.IsCleared = true;
            var passed = graph.AddRoom(new DungeonPosition(0, 1)); passed.Type = RoomEncounterType.Combat; passed.IsCleared = true;
            var current = graph.AddRoom(new DungeonPosition(0, 2)); current.Type = RoomEncounterType.Reward;
            var unknown = graph.AddRoom(new DungeonPosition(1, 2)); unknown.Type = RoomEncounterType.Shop;
            graph.Connect(start, passed); graph.Connect(passed, current); graph.Connect(current, unknown);
            var state = new DungeonRunState(1, graph);
            state.RevealConnection(start, passed); state.Player.MoveTo(passed);
            state.RevealConnection(passed, current); state.Player.MoveTo(current);

            view.Render(state);
            var icons = content.GetComponentsInChildren<MapRoomIcon>();
            Assert.That(icons.Length, Is.EqualTo(4), "Current, every visited room, and the direct unknown room each need a tile.");
            var clearedPastTile = icons.OrderBy(icon => icon.GetComponent<RectTransform>().anchoredPosition.y).First();
            Assert.That(clearedPastTile.transform.Find("Label").GetComponent<UnityEngine.UI.Text>().enabled, Is.False);
            Assert.That(clearedPastTile.GetComponent<UnityEngine.UI.Image>().color, Is.EqualTo(new Color(.14f, .29f, .42f, 1f)));
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(iconRoot);
            Object.DestroyImmediate(connectionRoot);
        }

        [Test]
        public void Minimap_RendersEachConnectionAsAPathInsideTheRoomTile()
        {
            var root = new GameObject("MapIcon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var label = new GameObject("Label", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>(); label.transform.SetParent(root.transform, false);
            var arrow = new GameObject("Arrow", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>(); arrow.transform.SetParent(root.transform, false);
            var north = new GameObject("RouteNorth", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>(); north.transform.SetParent(root.transform, false);
            var east = new GameObject("RouteEast", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>(); east.transform.SetParent(root.transform, false);
            var south = new GameObject("RouteSouth", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>(); south.transform.SetParent(root.transform, false);
            var west = new GameObject("RouteWest", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>(); west.transform.SetParent(root.transform, false);
            var icon = root.AddComponent<MapRoomIcon>();
            icon.Configure(root.GetComponent<UnityEngine.UI.Image>(), label, arrow, north, east, south, west);
            var room = new DungeonRoom(1, new DungeonPosition(0, 0)) { Type = RoomEncounterType.Combat };

            icon.Bind(room, MapRoomIconStyle.Current, FacingDirection.North, false, new HashSet<FacingDirection> { FacingDirection.North, FacingDirection.East });

            Assert.That(north.enabled, Is.True);
            Assert.That(east.enabled, Is.True);
            Assert.That(south.enabled, Is.False);
            Assert.That(west.enabled, Is.False);
            Assert.That(north.transform.parent, Is.EqualTo(root.transform));
            Assert.That(east.transform.parent, Is.EqualTo(root.transform));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void ExperienceReward_CarriesPastTheLevelBoundaryAndQueuesAPerk()
        {
            var player = ScriptableObject.CreateInstance<PlayerCombatData>();
            player.maxHp = 100f;
            player.shieldMax = 100f;
            var balance = ScriptableObject.CreateInstance<RunBalanceSettings>();
            balance.initialExperienceRequirement = 40;
            balance.experienceRequirementGrowth = 15;
            var run = new RunState(player, balance);

            run.GainExperience(53);

            Assert.That(run.Level, Is.EqualTo(2));
            Assert.That(run.Experience, Is.EqualTo(13));
            Assert.That(run.RequiredExperience, Is.EqualTo(55));
            Assert.That(run.PendingPerkChoices, Is.EqualTo(1));
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(balance);
        }

        [Test]
        public void RoomEventChoice_HidesItsPanelAndOnlyInvokesTheSelectedAnswer()
        {
            var root = new GameObject("RoomEventRoot", typeof(RectTransform), typeof(Canvas));
            var panel = new GameObject("Panel", typeof(RectTransform)); panel.transform.SetParent(root.transform, false);
            var illustration = new GameObject("Illustration", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>(); illustration.transform.SetParent(panel.transform, false);
            var message = new GameObject("Message", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>(); message.transform.SetParent(panel.transform, false);
            var yesRoot = new GameObject("Yes", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button)); yesRoot.transform.SetParent(panel.transform, false);
            var yesLabel = new GameObject("Label", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>(); yesLabel.transform.SetParent(yesRoot.transform, false);
            var noRoot = new GameObject("No", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button)); noRoot.transform.SetParent(panel.transform, false);
            var noLabel = new GameObject("Label", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>(); noLabel.transform.SetParent(noRoot.transform, false);
            var view = panel.AddComponent<RoomEventView>();
            view.Configure(panel, illustration, message, yesRoot.GetComponent<UnityEngine.UI.Button>(), yesLabel, noRoot.GetComponent<UnityEngine.UI.Button>(), noLabel);

            var accepted = false;
            var declined = false;
            view.ShowChoice("Question", null, "Yes", "No", () => accepted = true, () => declined = true);
            Assert.That(panel.activeSelf, Is.True);
            Assert.That(yesRoot.activeSelf, Is.True);
            Assert.That(noRoot.activeSelf, Is.True);
            Assert.That(illustration.enabled, Is.False);
            noRoot.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(accepted, Is.False);
            Assert.That(declined, Is.True);
            Assert.That(panel.activeSelf, Is.False);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void MerchantEvent_OnlyShowsItsDepartureMessageOnceAfterTheInteraction()
        {
            var progress = new RoomEventProgress();
            Assert.That(progress.ConsumeMerchantVisit(), Is.EqualTo(MerchantRoomEvent.Offer));
            progress.MarkMerchantDeparted();
            Assert.That(progress.ConsumeMerchantVisit(), Is.EqualTo(MerchantRoomEvent.GoneMessage));
            Assert.That(progress.ConsumeMerchantVisit(), Is.EqualTo(MerchantRoomEvent.Finished));
        }

        [Test]
        public void FountainEvent_AllowsOneDrinkThenShowsTheDriedFountainOnce()
        {
            var progress = new RoomEventProgress();
            Assert.That(progress.ConsumeFountainVisit(), Is.EqualTo(FountainRoomEvent.Offer));

            progress.DrinkFromFountain();
            Assert.That(progress.FountainDrained, Is.True);
            Assert.That(progress.ConsumeFountainVisit(), Is.EqualTo(FountainRoomEvent.DriedMessage));
            Assert.That(progress.ConsumeFountainVisit(), Is.EqualTo(FountainRoomEvent.Finished));
        }

        [Test]
        public void GamePrototypeScene_HasNoMissingScriptsAndCoreReferences()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/GamePrototype.unity");
            var components = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
            Assert.That(components.Any(component => component == null), Is.False, "The scene contains a missing script.");
            var currentRoomInfo = components.OfType<UnityEngine.UI.Text>().Single(text => text.gameObject.name == "CurrentRoomInfo");
            Assert.That(currentRoomInfo.gameObject.activeSelf, Is.False, "The minimap should not be accompanied by current-room concept text.");

            var flow = components.OfType<GameFlowController>().Single();
            var serializedFlow = new SerializedObject(flow);
            foreach (var propertyName in new[] { "runController", "dungeonController", "combatController", "perkChoiceView", "shopView", "resultView", "runHud", "roomTransitionView", "encounterMessageView", "roomEventView", "rewardPopupView", "goldEventSprite", "fountainEventSprite", "goddessEventSprite", "merchantEventSprite" })
                Assert.That(serializedFlow.FindProperty(propertyName).objectReferenceValue, Is.Not.Null, $"GameFlowController.{propertyName} is not connected.");

            var transition = components.OfType<RoomTransitionView>().Single();
            Assert.That(new SerializedObject(transition).FindProperty("walkBackdrop").objectReferenceValue, Is.Not.Null, "Room travel needs a visible walking layer, not a full-screen fade.");
            Assert.That(new SerializedObject(transition).FindProperty("footfallMarkers").arraySize, Is.GreaterThanOrEqualTo(3), "Room travel needs several moving depth markers.");
            Assert.That(components.OfType<EncounterMessageView>().Count(), Is.EqualTo(1));
            Assert.That(components.OfType<RoomEventView>().Count(), Is.EqualTo(1));
            Assert.That(components.OfType<RewardPopupView>().Count(), Is.EqualTo(1));

            var combatController = components.OfType<CombatController>().Single();
            Assert.That(components.OfType<CombatController>().Count(), Is.EqualTo(1));
            Assert.That(components.OfType<DungeonController>().Count(), Is.EqualTo(1));
            var serializedCombat = new SerializedObject(combatController);
            foreach (var propertyName in new[] { "damageNumberRoot", "damageNumberPrefab", "weakPointBreakFxRoot", "weakPointBreakFxPrefab" })
                Assert.That(serializedCombat.FindProperty(propertyName).objectReferenceValue, Is.Not.Null, $"CombatController.{propertyName} is not connected.");
            var mapView = components.OfType<DungeonMapView>().Single();
            Assert.That(new SerializedObject(mapView).FindProperty("connectionPrefab").objectReferenceValue, Is.Not.Null, "DungeonMapView.connectionPrefab is not connected.");
            Assert.That(new SerializedObject(mapView).FindProperty("spacing").floatValue, Is.InRange(38f, 44f), "Dungeon-style routes must be drawn inside nearly adjoining room tiles, not in wide gaps.");
            var mapViewport = mapView.GetComponent<RectTransform>();
            Assert.That(mapViewport.sizeDelta.x, Is.EqualTo(mapViewport.sizeDelta.y), "The minimap viewport must remain square.");
            Assert.That(mapView.GetComponent<UnityEngine.UI.RectMask2D>(), Is.Not.Null, "Long maps must be clipped by a fixed square minimap mask.");
            var navigation = components.OfType<NavigationChoiceView>().Single();
            Assert.That(navigation.transform.parent.name, Is.EqualTo("CombatRoot"), "Room navigation must be clicked directly in the combat area, not under the minimap.");
            Assert.That(navigation.GetComponentsInChildren<RoomChoiceButton>(true).Length, Is.EqualTo(4));
            var roomChoice = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/RoomChoiceButton.prefab").GetComponent<RoomChoiceButton>();
            var serializedRoomChoice = new SerializedObject(roomChoice);
            foreach (var propertyName in new[] { "icon", "forwardIcon", "sideIcon", "backIcon" })
                Assert.That(serializedRoomChoice.FindProperty(propertyName).objectReferenceValue, Is.Not.Null, $"RoomChoiceButton.{propertyName} must reference a directional image asset.");
            var mapIcon = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/MapRoomIcon.prefab").GetComponent<MapRoomIcon>();
            var serializedMapIcon = new SerializedObject(mapIcon);
            foreach (var propertyName in new[] { "facingArrow", "northRoute", "eastRoute", "southRoute", "westRoute" })
                Assert.That(serializedMapIcon.FindProperty(propertyName).objectReferenceValue, Is.Not.Null, $"MapRoomIcon.{propertyName} is required for in-tile dungeon paths.");
            var mapOutline = mapIcon.GetComponent<UnityEngine.UI.Outline>();
            Assert.That(mapOutline, Is.Null, "Room backgrounds must remain plain tiles without an unsolicited outline.");

            var combatHud = components.OfType<CombatHudView>().Single();
            var serializedHud = new SerializedObject(combatHud);
            foreach (var propertyName in new[] { "chargeGaugeRoot", "chargeGaugeTrack", "chargeGaugeFill", "playerHpText", "shieldText" })
                Assert.That(serializedHud.FindProperty(propertyName).objectReferenceValue, Is.Not.Null, $"CombatHudView.{propertyName} is not connected.");

            var chargeFill = serializedHud.FindProperty("chargeGaugeFill").objectReferenceValue as ArcGaugeGraphic;
            Assert.That(chargeFill.FillAmount, Is.EqualTo(0f), "The charge arc should start empty.");
            var monsterHp = serializedHud.FindProperty("monsterHp").objectReferenceValue as UnityEngine.UI.Image;
            Assert.That(monsterHp.transform.parent.gameObject.activeSelf, Is.False, "Monster HP must be hidden before combat starts.");
            var playerHp = serializedHud.FindProperty("playerHp").objectReferenceValue as UnityEngine.UI.Image;
            Assert.That(playerHp.type, Is.EqualTo(UnityEngine.UI.Image.Type.Simple), "Health gauges should resize by their 0-1 value.");
            Assert.That(playerHp.transform.parent.parent.name, Is.EqualTo("CombatRoot"), "Player gauges must overlap the player area in the combat space.");
            Assert.That(playerHp.color.r, Is.GreaterThan(.8f), "Player HP must be red.");
            Assert.That(playerHp.color.g, Is.LessThan(.3f), "Player HP must be red.");
            Assert.That((playerHp.transform.parent as RectTransform).sizeDelta.y, Is.GreaterThanOrEqualTo(20f), "Player gauges must be thick enough to contain their values.");
            var shield = serializedHud.FindProperty("shield").objectReferenceValue as UnityEngine.UI.Image;
            Assert.That(shield.color.r, Is.GreaterThan(.9f), "Player shield must be white.");
            Assert.That(shield.color.g, Is.GreaterThan(.9f), "Player shield must be white.");

            var sceneView = components.OfType<CombatSceneView>().Single();
            var serializedSceneView = new SerializedObject(sceneView);
            var playerAnchor = serializedSceneView.FindProperty("playerAnchor").objectReferenceValue as RectTransform;
            var monsterAnchor = serializedSceneView.FindProperty("monsterAnchor").objectReferenceValue as RectTransform;
            Assert.That(playerAnchor.anchoredPosition.y, Is.InRange(-300f, -260f), "The player sprite area should fill the lower third of the combat screen.");
            Assert.That(monsterAnchor.anchoredPosition.y, Is.InRange(80f, 120f), "The monster anchor should occupy the upper-middle combat area.");

            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Combat/PlayerView.prefab");
            Assert.That(playerPrefab.GetComponent<RectTransform>().sizeDelta.y, Is.GreaterThanOrEqualTo(260f), "The player sprite area should occupy the lower third of the combat area.");
            var cooldownAnchor = playerPrefab.transform.Find("AttackCooldownAnchor") as RectTransform;
            Assert.That(cooldownAnchor.anchoredPosition.y, Is.InRange(115f, 130f), "The cooldown gauge should sit only slightly lower than the top of the player slot.");
            var weakPointPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Combat/WeakPointView.prefab").GetComponent<WeakPointView>();
            var weakPointSprite = new SerializedObject(weakPointPrefab).FindProperty("body").objectReferenceValue as UnityEngine.UI.Image;
            Assert.That(weakPointSprite.sprite, Is.Not.Null, "Weak points need the target-lock sprite.");
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/WeakPointBreakFxView.prefab").GetComponent<WeakPointBreakFxView>(), Is.Not.Null, "Weak point breaks need a UI particle prefab.");
            var monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Combat/MonsterView.prefab").GetComponent<MonsterView>();
            var serializedMonster = new SerializedObject(monsterPrefab);
            foreach (var propertyName in new[] { "hpFill", "attackNameLabel", "chargeTimeLabel", "attackNameBackdrop", "chargeTimeBackdrop" })
                Assert.That(serializedMonster.FindProperty(propertyName).objectReferenceValue, Is.Not.Null, $"MonsterView.{propertyName} is required for multi-monster combat feedback.");
        }

        private static UnityEngine.UI.Image CreateBar(Transform parent, string name)
        {
            var background = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            background.transform.SetParent(parent, false);
            background.GetComponent<RectTransform>().sizeDelta = new Vector2(100f, 10f);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            fill.transform.SetParent(background.transform, false);
            fill.GetComponent<RectTransform>().sizeDelta = new Vector2(100f, 10f);
            return fill.GetComponent<UnityEngine.UI.Image>();
        }
    }
}
