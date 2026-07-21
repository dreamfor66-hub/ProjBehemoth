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
        public void WeakPointHit_ForgivesASmallNearMissWithoutChangingBodyCollision()
        {
            var segment = new AttackSegment(new Vector2(-100f, 0f), new Vector2(100f, 0f), 18f);
            var point = new WeakPointRuntime(new WeakPointDefinition { id = "near", normalizedPosition = Vector2.zero, hitRadius = 30f }, new Vector2(0f, 50f));
            var bodyEquivalent = new CircleHitShape(point.HitShape.Center, point.HitShape.Radius);

            Assert.That(SegmentHitResolver.Intersects(segment, bodyEquivalent), Is.False, "The raw circle is deliberately just outside the exact capsule cast.");
            Assert.That(WeakPointHitResolver.Resolve(segment, new[] { point }), Has.Count.EqualTo(1), "Weak points accept a near-grazing slash.");
        }

        [Test]
        public void TargetTraversal_ConfirmsWhenAStrokeCrossesThenEscapesBeyondTheConfiguredDistance()
        {
            var target = new CircleHitShape(Vector2.zero, 40f);
            var stroke = new AttackSegment(new Vector2(-100f, 0f), new Vector2(110f, 0f), 18f);
            var confirmation = new TargetTraversalConfirmation();
            var escapedDistance = TargetTraversalResolver.OutsideDistance(target, stroke.End, stroke.Width);

            Assert.That(TargetTraversalResolver.Touches(stroke, target), Is.True);
            Assert.That(TargetTraversalResolver.Contains(target, stroke.Start, stroke.Width), Is.False);
            Assert.That(TargetTraversalResolver.Contains(target, stroke.End, stroke.Width), Is.False);
            Assert.That(confirmation.Observe(true, false, false, escapedDistance, 210f, 0f, 48f, 1f, .1f), Is.True);
        }

        [Test]
        public void TargetTraversal_ConfirmsAfterPointStartsInsideThenRestsOutsideForPointOneSeconds()
        {
            var target = new CircleHitShape(Vector2.zero, 40f);
            var confirmation = new TargetTraversalConfirmation();
            confirmation.BeginInsideTarget();
            var outside = new Vector2(52f, 0f); // 3 logical pixels beyond the 40 + 9 hit boundary.
            var escapedDistance = TargetTraversalResolver.OutsideDistance(target, outside, 18f);

            Assert.That(confirmation.Observe(true, true, false, escapedDistance, 3f, 0f, 48f, 1f, .1f), Is.False);
            Assert.That(confirmation.Observe(false, false, false, escapedDistance, 0f, .05f, 48f, 1f, .1f), Is.False);
            Assert.That(confirmation.Observe(false, false, false, escapedDistance, 0f, .15f, 48f, 1f, .1f), Is.True);
        }

        [Test]
        public void TargetTraversal_ConfirmsAfterPointRestsInsideTheCrossedMonsterForPointOneSeconds()
        {
            var confirmation = new TargetTraversalConfirmation();

            Assert.That(confirmation.Observe(true, false, true, 0f, 18f, 0f, 48f, 1f, .1f), Is.False);
            Assert.That(confirmation.Observe(false, true, true, 0f, 0f, .04f, 48f, 1f, .1f), Is.False);
            Assert.That(confirmation.Observe(false, true, true, 0f, 0f, .14f, 48f, 1f, .1f), Is.True);
        }

        [Test]
        public void AttackHitCast_ShowsTheExactCapsuleUsedByTheSlashSegment()
        {
            var root = new GameObject("FxRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            var lineObject = new GameObject("Line", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(AttackLineView));
            lineObject.transform.SetParent(root, false);
            var view = lineObject.GetComponent<AttackLineView>();
            view.Configure(lineObject.GetComponent<RectTransform>(), lineObject.GetComponent<UnityEngine.UI.Image>());

            view.Show(new AttackSegment(new Vector2(-50f, 30f), new Vector2(50f, 30f), 18f), false);
            var cast = root.Find("AttackHitCast").GetComponent<RectTransform>();

            Assert.That(cast.anchoredPosition, Is.EqualTo(new Vector2(0f, 30f)));
            Assert.That(cast.sizeDelta, Is.EqualTo(new Vector2(118f, 18f)), "The capsule has the segment length plus one radius at each end.");
            Assert.That(cast.GetComponent<CapsuleCastGraphic>().color, Is.EqualTo(new Color(1f, .12f, .12f, .34f)));
            Object.DestroyImmediate(root.gameObject);
        }

        [Test]
        public void WeakPointView_UsesTheSameDiameterAsItsGameplayHitCircle()
        {
            var root = new GameObject("WeakPoint", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();
            var view = root.gameObject.AddComponent<WeakPointView>();
            view.Configure(root, root.GetComponent<UnityEngine.UI.Image>(), null);
            var runtime = new WeakPointRuntime(new WeakPointDefinition { id = "core", normalizedPosition = Vector2.zero, hitRadius = 30f }, new Vector2(42f, -18f));

            view.Bind(runtime);

            Assert.That(root.anchoredPosition, Is.EqualTo(new Vector2(42f, -18f)));
            Assert.That(root.sizeDelta, Is.EqualTo(new Vector2(60f, 60f)));
            Object.DestroyImmediate(root.gameObject);
        }

        [Test]
        public void WeakPoint_ConsumesOnlyOneHitPerResolvedAttack()
        {
            var point = new WeakPointRuntime(new WeakPointDefinition { id = "core", normalizedPosition = Vector2.zero, hitRadius = 20f, requiredChargeHits = 2 }, Vector2.zero);
            Assert.That(point.RequiredHits, Is.EqualTo(2));
            Assert.That(point.ApplyChargeHit(), Is.True);
            Assert.That(point.RemainingHits, Is.EqualTo(1));
            Assert.That(point.ApplyChargeHit(), Is.True);
            Assert.That(point.IsDestroyed, Is.True);
            Assert.That(point.ApplyChargeHit(), Is.False);
        }

        [Test]
        public void WeakPointView_BuildsOneSegmentForEachRequiredChargeHit()
        {
            var root = new GameObject("WeakPoint", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();
            var display = new GameObject("RemainingHitDisplay", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>();
            display.transform.SetParent(root, false);
            var view = root.gameObject.AddComponent<WeakPointView>();
            view.Configure(root, root.GetComponent<UnityEngine.UI.Image>(), display);
            var runtime = new WeakPointRuntime(new WeakPointDefinition { id = "core", normalizedPosition = Vector2.zero, hitRadius = 20f, requiredChargeHits = 2 }, Vector2.zero);

            view.Bind(runtime);

            var grid = root.Find("RemainingHitDisplay/HitSegments");
            Assert.That(grid, Is.Not.Null);
            Assert.That(grid.childCount, Is.EqualTo(2));
            Object.DestroyImmediate(root.gameObject);
        }

        [Test]
        public void WeakPointView_HidesTheHitBarForOneHitWeakPoints()
        {
            var root = new GameObject("WeakPoint", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();
            var display = new GameObject("RemainingHitDisplay", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>();
            display.transform.SetParent(root, false);
            var view = root.gameObject.AddComponent<WeakPointView>();
            view.Configure(root, root.GetComponent<UnityEngine.UI.Image>(), display);
            var runtime = new WeakPointRuntime(new WeakPointDefinition { id = "core", normalizedPosition = Vector2.zero, hitRadius = 20f, requiredChargeHits = 1 }, Vector2.zero);

            view.Bind(runtime);

            var grid = root.Find("RemainingHitDisplay/HitSegments");
            Assert.That(grid, Is.Not.Null);
            Assert.That(grid.gameObject.activeSelf, Is.False);
            Object.DestroyImmediate(root.gameObject);
        }

        [Test]
        public void CombatSceneView_CameraShakeReturnsTheCombatAreaToItsRestingPosition()
        {
            var area = new GameObject("CombatArea", typeof(RectTransform)).GetComponent<RectTransform>();
            area.anchoredPosition = new Vector2(12f, -20f);
            var player = new GameObject("PlayerAnchor", typeof(RectTransform)).GetComponent<RectTransform>();
            var monster = new GameObject("MonsterAnchor", typeof(RectTransform)).GetComponent<RectTransform>();
            var view = area.gameObject.AddComponent<CombatSceneView>();
            view.Configure(area, player, monster);

            view.TriggerCameraShake(12f, .15f);
            view.TickCameraShake(.03f);
            Assert.That(view.IsCameraShaking, Is.True);
            Assert.That(area.anchoredPosition, Is.Not.EqualTo(new Vector2(12f, -20f)));

            view.TickCameraShake(.2f);
            Assert.That(view.IsCameraShaking, Is.False);
            Assert.That(area.anchoredPosition, Is.EqualTo(new Vector2(12f, -20f)));

            Object.DestroyImmediate(player.gameObject);
            Object.DestroyImmediate(monster.gameObject);
            Object.DestroyImmediate(area.gameObject);
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
            var body = new RectHitShape(player, new Vector2(270f, 270f));
            Assert.That(GuardGestureResolver.IsPlayerDownwardGuard(body, new Vector2(128f, -190f), new Vector2(118f, -300f), 70f, 16f), Is.True);
            Assert.That(GuardGestureResolver.IsPlayerDownwardGuard(body, new Vector2(0f, -220f), new Vector2(100f, -270f), 70f, 16f), Is.False, "A mostly horizontal motion must not guard.");
            Assert.That(GuardGestureResolver.IsPlayerDownwardGuard(body, new Vector2(172f, -210f), new Vector2(170f, -292f), 70f, 16f), Is.False, "Gestures beyond the body and its configured outer margin must not guard.");
        }

        [Test]
        public void PlayerDownwardRest_ConfirmsGuardForAnOutsideOrInsidePressThatSettlesOnThePlayer()
        {
            var player = new Vector2(0f, -275f);
            var body = new RectHitShape(player, new Vector2(270f, 270f));
            var confirmation = new GuardRestConfirmation();
            var outsidePress = new Vector2(0f, -100f);
            var insideHold = new Vector2(0f, -260f);

            Assert.That(RectHitResolver.Intersects(new AttackSegment(outsidePress, insideHold, 0f), body, 16f), Is.True, "A drag that began outside must register its entry through the body guard zone.");
            Assert.That(GuardGestureResolver.IsPlayerRestGuardCandidate(body, outsidePress, insideHold, 70f), Is.True, "A downward stroke may begin outside the player before settling inside.");
            Assert.That(GuardGestureResolver.IsPlayerRestGuardCandidate(body, new Vector2(0f, -200f), new Vector2(0f, -300f), 70f), Is.True, "A downward stroke may also begin inside the player.");
            Assert.That(GuardGestureResolver.IsPlayerRestGuardCandidate(body, outsidePress, new Vector2(185f, -260f), 70f), Is.False, "The held pointer must settle inside the actual body, not merely its outer guard margin.");
            Assert.That(GuardGestureResolver.IsDownwardApproach(outsidePress, insideHold), Is.True, "An outside-to-body downward entry should qualify without the normal guard distance threshold.");
            Assert.That(GuardGestureResolver.IsDownwardApproach(insideHold, outsidePress), Is.False, "Upward motion into the player must not use the loose entry guard.");

            Assert.That(confirmation.Observe(true, true, 6f, 0f, 1f, .1f), Is.False);
            Assert.That(confirmation.Observe(false, true, 0f, .05f, 1f, .1f), Is.False);
            Assert.That(confirmation.Observe(false, true, 0f, .16f, 1f, .1f), Is.True);
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
        public void MonsterRuntime_DeadMonsterCannotBeReturnedToIdleByAResolvedCounterEffect()
        {
            var data = ScriptableObject.CreateInstance<MonsterData>();
            data.maxHp = 10f;
            var runtime = new MonsterRuntime(data);

            runtime.TakeDamage(10f);
            runtime.CompleteNormalAttack();
            runtime.EndCharge();

            Assert.That(runtime.IsAlive, Is.False);
            Assert.That(runtime.State, Is.EqualTo(MonsterState.Dead));
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
        public void ChargeStart_UsesTheSharedAttackCooldown()
        {
            var data = ScriptableObject.CreateInstance<PlayerCombatData>();
            data.attackCooldown = 1f;
            var player = new PlayerCombatRuntime(data);

            Assert.That(player.CanStartCharge(), Is.True);
            player.StartAttackCooldown();
            Assert.That(player.CanStartCharge(), Is.False);

            player.Tick(1f);
            Assert.That(player.CanStartCharge(), Is.True);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void Potion_CombatStartStatEffectLastsForItsConfiguredTriggerMaximum()
        {
            var playerData = ScriptableObject.CreateInstance<PlayerCombatData>();
            playerData.maxHp = 100f; playerData.shieldMax = 50f; playerData.baseAttackDamage = 10f;
            var balance = ScriptableObject.CreateInstance<RunBalanceSettings>();
            balance.initialExperienceRequirement = 100;
            var potion = ScriptableObject.CreateInstance<EquipmentData>();
            potion.itemKind = ShopItemKind.Potion;
            potion.trigger = new EquipmentTrigger { triggerType = TriggerType.OnCombatStartAfterConsume, triggerAttackTypeFilter = TriggerAttackTypeFilter.All, triggerAttackWayFilter = TriggerAttackWayFilter.All, triggerCount = 1, triggerMaxCount = 2 };
            potion.targetType = TargetType.Owner;
            potion.effect = new EquipmentEffect { effectType = EffectType.StatIncrease, effectMagnitude = .3f, effectCount = 1, effectStatType = ModifierKind.AllDamageMultiplier };
            var state = new RunState(playerData, balance);

            Assert.That(state.TryStorePotion(potion), Is.True);
            Assert.That(state.TryUsePotion(potion), Is.True);
            state.BeginCombat();
            Assert.That(state.Player.NormalAttackDamage, Is.EqualTo(13f).Within(.001f));
            state.BeginCombat();
            Assert.That(state.Player.NormalAttackDamage, Is.EqualTo(13f).Within(.001f));
            state.BeginCombat();
            Assert.That(state.Player.NormalAttackDamage, Is.EqualTo(10f).Within(.001f));

            Object.DestroyImmediate(playerData);
            Object.DestroyImmediate(balance);
            Object.DestroyImmediate(potion);
        }

        [Test]
        public void Potion_RevivesThePlayerOnceAfterDeath()
        {
            var playerData = ScriptableObject.CreateInstance<PlayerCombatData>();
            playerData.maxHp = 100f; playerData.shieldMax = 50f;
            var balance = ScriptableObject.CreateInstance<RunBalanceSettings>();
            balance.initialExperienceRequirement = 100;
            var potion = ScriptableObject.CreateInstance<EquipmentData>();
            potion.itemKind = ShopItemKind.Potion;
            potion.trigger = new EquipmentTrigger { triggerType = TriggerType.OnConsume, triggerAttackTypeFilter = TriggerAttackTypeFilter.All, triggerAttackWayFilter = TriggerAttackWayFilter.All, triggerCount = 1, triggerMaxCount = 1 };
            potion.targetType = TargetType.Owner;
            potion.effect = new EquipmentEffect { effectType = EffectType.StatIncrease, effectMagnitude = .35f, effectCount = 1, effectStatType = ModifierKind.RevivalHealthFraction };
            var state = new RunState(playerData, balance);

            state.TryStorePotion(potion);
            Assert.That(state.TryUsePotion(potion), Is.True);
            state.Player.TakeEnvironmentalDamage(150f);
            Assert.That(state.Player.IsAlive, Is.False);
            Assert.That(state.TryConsumeRevival(), Is.True);
            Assert.That(state.Player.CurrentHp, Is.EqualTo(35f).Within(.001f));
            Assert.That(state.TryConsumeRevival(), Is.False);

            Object.DestroyImmediate(playerData);
            Object.DestroyImmediate(balance);
            Object.DestroyImmediate(potion);
        }

        [Test]
        public void Poison_SlowsActionsAndDamagesOnEachRoomMove()
        {
            var playerData = ScriptableObject.CreateInstance<PlayerCombatData>();
            playerData.maxHp = 100f; playerData.shieldMax = 50f; playerData.attackCooldown = 1f; playerData.minimumAttackCooldown = .1f; playerData.chargeDuration = 1f;
            var balance = ScriptableObject.CreateInstance<RunBalanceSettings>();
            balance.initialExperienceRequirement = 100;
            var state = new RunState(playerData, balance);

            state.ApplyPoison(6f, .15f);
            var travelDamage = state.ApplyTravelHazard();

            Assert.That(state.Player.CurrentCooldown, Is.EqualTo(1.15f).Within(.001f));
            Assert.That(state.Player.CurrentChargeDuration, Is.EqualTo(1.15f).Within(.001f));
            Assert.That(travelDamage.HpDamage, Is.EqualTo(6f).Within(.001f));
            Assert.That(state.Player.CurrentHp, Is.EqualTo(94f).Within(.001f));
            Object.DestroyImmediate(playerData);
            Object.DestroyImmediate(balance);
        }

        [Test]
        public void SecondFloorGeneration_UsesHiddenChestOutcomes()
        {
            var settings = ScriptableObject.CreateInstance<DungeonGenerationSettings>();
            settings.minRooms = 20; settings.maxRooms = 20; settings.minimumBossDistance = 7; settings.minimumChestDistance = 4;
            settings.minimumRewardRooms = 2; settings.minimumFountainRooms = 1; settings.minimumShopRooms = 1; settings.minimumGoddessRooms = 1; settings.eliteRoomCount = 1; settings.chestRoomCount = 1; settings.loopChance = .2f; settings.maxGenerationAttempts = 200;
            var state = new DungeonGenerator(settings).Generate(8812, 2);
            var chests = state.Graph.Rooms.Where(room => room.Type == RoomEncounterType.Chest).ToArray();

            Assert.That(state.Floor, Is.EqualTo(2));
            Assert.That(chests.Length, Is.EqualTo(2));
            Assert.That(chests.All(room => room.ChestContent is ChestContent.Gold or ChestContent.Relic or ChestContent.Mimic), Is.True);
            Assert.That(state.Graph.Rooms.Any(room => room.Type == RoomEncounterType.MajorReward), Is.False);
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void FirstFloorGeneration_HasExactlyOneGoldChestAndOneRelicChest()
        {
            var settings = ScriptableObject.CreateInstance<DungeonGenerationSettings>();
            settings.minRooms = 20; settings.maxRooms = 20; settings.minimumBossDistance = 7; settings.minimumChestDistance = 4;
            settings.minimumRewardRooms = 2; settings.minimumFountainRooms = 1; settings.minimumShopRooms = 1; settings.minimumGoddessRooms = 1; settings.eliteRoomCount = 1; settings.chestRoomCount = 1; settings.loopChance = .2f; settings.maxGenerationAttempts = 200;
            var state = new DungeonGenerator(settings).Generate(5581, 1);
            var chests = state.Graph.Rooms.Where(room => room.Type == RoomEncounterType.Chest).ToArray();

            Assert.That(chests.Length, Is.EqualTo(2));
            Assert.That(chests.All(room => room.IsRevealed), Is.True, "Both first-floor chests must be discoverable on the map.");
            Assert.That(chests.Count(room => room.ChestContent == ChestContent.Gold), Is.EqualTo(1));
            Assert.That(chests.Count(room => room.ChestContent == ChestContent.Relic), Is.EqualTo(1));
            Assert.That(state.Graph.Rooms.Any(room => room.Type == RoomEncounterType.MajorReward), Is.False);
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void FirstFloorGeneration_HasNoPoisonFountain()
        {
            var settings = ScriptableObject.CreateInstance<DungeonGenerationSettings>();
            settings.minRooms = 20; settings.maxRooms = 20; settings.minimumBossDistance = 7; settings.minimumChestDistance = 4;
            settings.minimumRewardRooms = 2; settings.minimumFountainRooms = 1; settings.minimumShopRooms = 1; settings.minimumGoddessRooms = 1; settings.eliteRoomCount = 1; settings.chestRoomCount = 1; settings.loopChance = .2f; settings.maxGenerationAttempts = 200;
            var state = new DungeonGenerator(settings).Generate(9217, 1);

            Assert.That(state.Graph.Rooms.Any(room => room.Type == RoomEncounterType.PoisonFountain), Is.False);
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void DungeonGeneration_CreatesOneMerchantPerBasementFloor()
        {
            var settings = ScriptableObject.CreateInstance<DungeonGenerationSettings>();
            settings.minRooms = 20; settings.maxRooms = 20; settings.minimumBossDistance = 7; settings.minimumChestDistance = 4;
            settings.minimumRewardRooms = 2; settings.minimumFountainRooms = 1; settings.minimumShopRooms = 1; settings.minimumGoddessRooms = 1; settings.eliteRoomCount = 1; settings.chestRoomCount = 1; settings.loopChance = .2f; settings.maxGenerationAttempts = 200;
            var generator = new DungeonGenerator(settings);

            for (var floor = 1; floor <= 3; floor++)
            {
                var state = generator.Generate(3500 + floor, floor);
                Assert.That(state.Graph.Rooms.Count(room => room.Type == RoomEncounterType.Shop), Is.EqualTo(floor), $"B{floor} must contain exactly {floor} merchants.");
            }
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void DungeonGeneration_CreatesOneElitePerBasementFloor()
        {
            var settings = ScriptableObject.CreateInstance<DungeonGenerationSettings>();
            settings.minRooms = 20; settings.maxRooms = 20; settings.minimumBossDistance = 7; settings.minimumChestDistance = 4;
            settings.minimumRewardRooms = 2; settings.minimumFountainRooms = 1; settings.minimumShopRooms = 1; settings.minimumGoddessRooms = 1; settings.eliteRoomCount = 1; settings.chestRoomCount = 1; settings.loopChance = .2f; settings.maxGenerationAttempts = 200;
            var generator = new DungeonGenerator(settings);

            for (var floor = 1; floor <= 3; floor++)
            {
                var state = generator.Generate(4100 + floor, floor);
                Assert.That(state.Graph.Rooms.Count(room => room.Type == RoomEncounterType.Elite), Is.EqualTo(floor), $"B{floor} must contain exactly {floor} elite rooms.");
            }
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void MonsterProfiles_B2AndBeyondUseDistinctCadencesAndMimicIsEliteStrength()
        {
            var frostBat = AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/DungeonSlashPrototype/Monsters/Data_Monster_B2_Minion_FrostBat.asset");
            var yeti = AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/DungeonSlashPrototype/Monsters/Data_Monster_B2_Minion_Yeti.asset");
            var shadeLancer = AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/DungeonSlashPrototype/Monsters/Data_Monster_B3_Minion_ShadeLancer.asset");
            var frostOgre = AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/DungeonSlashPrototype/Monsters/Data_Monster_B2_Elite_FrostOgre.asset");
            var frostQueen = AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/DungeonSlashPrototype/Monsters/Data_Monster_B2_Boss_FrostQueen.asset");
            var soulKnight = AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/DungeonSlashPrototype/Monsters/Data_Monster_B3_Elite_SoulKnight.asset");
            var mimicAssets = AssetDatabase.FindAssets("t:MonsterData", new[] { "Assets/Data/DungeonSlashPrototype/Monsters" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<MonsterData>)
                .Where(monster => monster.displayName == "\uBBF8\uBBF9")
                .ToArray();
            Assert.That(mimicAssets.Length, Is.EqualTo(1), "The chest event must reference the single special Mimic asset.");
            var mimic = mimicAssets[0];
            var monsterAssetNames = AssetDatabase.FindAssets("t:MonsterData", new[] { "Assets/Data/DungeonSlashPrototype/Monsters" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(System.IO.Path.GetFileNameWithoutExtension)
                .ToArray();
            var monsterAssets = AssetDatabase.FindAssets("t:MonsterData", new[] { "Assets/Data/DungeonSlashPrototype/Monsters" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => (asset: AssetDatabase.LoadAssetAtPath<MonsterData>(path), fileName: System.IO.Path.GetFileNameWithoutExtension(path)))
                .ToArray();

            Assert.That(monsterAssetNames.All(name => name.StartsWith("Data_Monster_", System.StringComparison.Ordinal)), Is.True, "Generated predecessor assets must not coexist with canonical Data_Monster assets.");
            Assert.That(monsterAssets.All(entry => entry.asset != null && entry.asset.name == entry.fileName), Is.True, "The ScriptableObject name must match its canonical monster filename.");
            Assert.That(monsterAssets.All(entry => entry.asset.attacks != null && entry.asset.GetAttack(MonsterAttackType.Normal) != null && entry.asset.GetAttack(MonsterAttackType.Charge) != null), Is.True, "Every generated monster must own its normal and charge definitions through AttackData.");
            Assert.That(monsterAssets.SelectMany(entry => entry.asset.GetAttacks(MonsterAttackType.Charge)).All(attack => attack.chargeWeakPoints != null && attack.chargeWeakPoints.Count > 0), Is.True, "Charge weak points must live directly on the charge AttackData.");
            Assert.That(monsterAssets.SelectMany(entry => entry.asset.GetAttacks(MonsterAttackType.Charge)).Select(attack => attack.chargeTimeLimit).Distinct().Count(), Is.GreaterThan(1), "Charge windows retain the distinct weak-point and pattern timings.");
            Assert.That(System.IO.Directory.Exists("Assets/Data/DungeonSlashPrototype/ChargePatterns"), Is.False, "ChargePatternData assets must not remain after the AttackData migration.");
            Assert.That(frostBat.normalAttackInterval, Is.GreaterThanOrEqualTo(2f), "The frost bat is a low-pressure summoned minion, not an independent rapid-fire threat.");
            Assert.That(frostBat.maxHp, Is.LessThanOrEqualTo(80f));
            Assert.That(frostBat.GetAttack(MonsterAttackType.Charge).chargeTimeLimit, Is.EqualTo(3.6f).Within(.001f), "The 2.1-second frost bat pattern receives a +1.5 second reaction window.");
            Assert.That(frostBat.GetAttack(MonsterAttackType.Normal).windupDuration, Is.EqualTo(.62f).Within(.001f), "Every normal-attack telegraph receives the global +0.12 second reaction buffer.");
            Assert.That(yeti.normalAttackInterval, Is.GreaterThan(5f), "The yeti needs a long, punishable heavy-attack cycle.");
            Assert.That(yeti.GetAttack(MonsterAttackType.Normal).windupDuration, Is.EqualTo(1.17f).Within(.001f));
            Assert.That(yeti.GetAttack(MonsterAttackType.Charge).chargeTimeLimit, Is.EqualTo(8.48f).Within(.001f), "The two-hit heavy weak points receive 1.6 times the one-hit charge window.");
            Assert.That(yeti.GetAttack(MonsterAttackType.Normal).shieldDamage, Is.GreaterThanOrEqualTo(30f), "The yeti's single hit must break a full base shield.");
            Assert.That(shadeLancer.normalAttackInterval, Is.GreaterThan(5f), "The shade lancer also supplies a slow heavy rhythm on B3.");
            Assert.That(frostOgre.GetAttack(MonsterAttackType.Normal).combatMechanics.directionalGuard.maxGuard, Is.LessThanOrEqualTo(45f), "The directional gate must be breakable with an early-run horizontal combo.");
            Assert.That(frostQueen.normalAttackInterval, Is.GreaterThanOrEqualTo(2.5f), "The queen and her two minions need breathing room between attack cycles.");
            Assert.That(soulKnight.GetAttack(MonsterAttackType.Normal).combatMechanics.hitCount, Is.EqualTo(3));
            Assert.That(soulKnight.GetAttack(MonsterAttackType.Normal).damage, Is.LessThanOrEqualTo(20f), "Soul Barrage may be a triple hit, but cannot delete a full HP bar after one guard break.");
            Assert.That(mimic.maxHp, Is.GreaterThanOrEqualTo(350f), "A chest mimic must be elite-grade, not a regular minion.");
            Assert.That(mimic.GetAttack(MonsterAttackType.Normal).shieldDamage, Is.GreaterThanOrEqualTo(30f));
            Assert.That(mimic.GetAttack(MonsterAttackType.Charge).chargeTimeLimit, Is.EqualTo(6.4f).Within(.001f), "The two-hit Mimic weak points receive 1.6 times the one-hit charge window.");
        }

        [Test]
        public void SummonedMonsterScale_ScalesWeakPointGeometryAndCanStaggerActions()
        {
            var chargeAttack = ScriptableObject.CreateInstance<MonsterAttackData>();
            chargeAttack.type = MonsterAttackType.Charge;
            chargeAttack.chargeWeakPoints = new List<WeakPointDefinition>
            {
                new() { id = "core", normalizedPosition = new Vector2(100f, 20f), hitRadius = 30f, requiredChargeHits = 1 }
            };
            var data = ScriptableObject.CreateInstance<MonsterData>();
            data.normalAttackInterval = 2f;
            data.chargeInterval = 6f;
            data.attacks = new List<MonsterAttackData> { chargeAttack };
            var runtime = new MonsterRuntime(data, visualScale: .62f);

            runtime.DelayActions(.45f);
            Assert.That(runtime.NormalAttackTimer, Is.EqualTo(2.45f).Within(.001f));
            Assert.That(runtime.ChargeTimer, Is.EqualTo(6.45f).Within(.001f));
            runtime.StartCharge(Vector2.zero);
            Assert.That(runtime.WeakPoints[0].HitShape.Center, Is.EqualTo(new Vector2(62f, 12.4f)));
            Assert.That(runtime.WeakPoints[0].HitShape.Radius, Is.EqualTo(18.6f).Within(.001f));

            Object.DestroyImmediate(data);
            Object.DestroyImmediate(chargeAttack);
        }

        [Test]
        public void DirectionalGuard_BlocksUntilTheMatchingSlashBreaksItThenRebuilds()
        {
            var data = ScriptableObject.CreateInstance<MonsterData>();
            data.maxHp = 100f;
            data.normalAttackInterval = 2f;
            data.chargeInterval = 6f;
            var normalAttack = ScriptableObject.CreateInstance<MonsterAttackData>();
            normalAttack.type = MonsterAttackType.Normal;
            normalAttack.combatMechanics = new MonsterCombatMechanics
            {
                directionalGuard = new DirectionalGuardData
                {
                    enabled = true,
                    breakByAttackWay = TriggerAttackWayFilter.Horizontal,
                    maxGuard = 30f,
                    rebuildDelay = .5f,
                    rebuildPerSecond = 30f
                }
            };
            data.attacks = new List<MonsterAttackData> { normalAttack };
            var runtime = new MonsterRuntime(data);

            Assert.That(runtime.TryAbsorbDirectionalGuard(TriggerAttackWay.Upward, 15f, out var wrongWayAbsorbed, out var matchedWrongWay, out var wrongWayBroke), Is.True);
            Assert.That(matchedWrongWay, Is.False);
            Assert.That(wrongWayAbsorbed, Is.EqualTo(0f));
            Assert.That(wrongWayBroke, Is.False);
            Assert.That(runtime.DirectionalGuardCurrent, Is.EqualTo(30f));

            Assert.That(runtime.TryAbsorbDirectionalGuard(TriggerAttackWay.Horizontal, 30f, out var absorbed, out var matched, out var broken), Is.True);
            Assert.That(matched, Is.True);
            Assert.That(absorbed, Is.EqualTo(30f));
            Assert.That(broken, Is.True);
            Assert.That(runtime.IsDirectionalGuardActive, Is.False);
            runtime.Tick(.5f, Vector2.zero);
            runtime.Tick(1f, Vector2.zero);
            Assert.That(runtime.IsDirectionalGuardActive, Is.True);
            Assert.That(runtime.DirectionalGuardCurrent, Is.EqualTo(30f));
            Object.DestroyImmediate(normalAttack);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void MultiHitMonster_AttacksAgainAfterTheFirstHitInsteadOfEndingItsSequence()
        {
            var data = ScriptableObject.CreateInstance<MonsterData>();
            var attack = ScriptableObject.CreateInstance<MonsterAttackData>();
            attack.type = MonsterAttackType.Normal;
            attack.windupDuration = .1f;
            attack.combatMechanics.hitCount = 3;
            attack.combatMechanics.followupDelay = .2f;
            data.attacks = new List<MonsterAttackData> { attack };
            data.normalAttackInterval = 3f;
            data.chargeInterval = 9f;
            var runtime = new MonsterRuntime(data);

            runtime.BeginNormalAttack();
            runtime.Tick(.1f, Vector2.zero);
            Assert.That(runtime.IsNormalAttackReadyToHit, Is.True);
            Assert.That(runtime.AdvanceNormalAttack(), Is.True);
            Assert.That(runtime.RemainingNormalAttackHits, Is.EqualTo(2));
            runtime.Tick(.2f, Vector2.zero);
            Assert.That(runtime.AdvanceNormalAttack(), Is.True);
            runtime.Tick(.2f, Vector2.zero);
            Assert.That(runtime.AdvanceNormalAttack(), Is.False);
            Assert.That(runtime.State, Is.EqualTo(MonsterState.Idle));
            Object.DestroyImmediate(attack);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void MultiHitCharge_AttacksAgainAfterWeakPointExposureEnds()
        {
            var data = ScriptableObject.CreateInstance<MonsterData>();
            var attack = ScriptableObject.CreateInstance<MonsterAttackData>();
            attack.type = MonsterAttackType.Charge;
            attack.chargeTimeLimit = .1f;
            attack.chargeWeakPoints = new List<WeakPointDefinition> { new() { id = "core", normalizedPosition = Vector2.zero, hitRadius = 30f } };
            attack.combatMechanics.hitCount = 3;
            attack.combatMechanics.followupDelay = .2f;
            data.attacks = new List<MonsterAttackData> { attack };
            var runtime = new MonsterRuntime(data);

            runtime.StartCharge(Vector2.zero);
            runtime.Tick(.1f, Vector2.zero);
            Assert.That(runtime.ChargeExpired, Is.True);
            Assert.That(runtime.AdvanceChargeAttack(), Is.True);
            Assert.That(runtime.RemainingChargeHits, Is.EqualTo(2));
            Assert.That(runtime.WeakPoints, Is.Empty, "Follow-up hits must not leave the expired weak-point exposure active.");
            runtime.Tick(.2f, Vector2.zero);
            Assert.That(runtime.AdvanceChargeAttack(), Is.True);
            runtime.Tick(.2f, Vector2.zero);
            Assert.That(runtime.AdvanceChargeAttack(), Is.False);
            Assert.That(runtime.RemainingChargeHits, Is.EqualTo(0));
            Object.DestroyImmediate(attack);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void MonsterMechanicAssets_ConfigureLeechEliteAndFrostQueenRules()
        {
            var leech = AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/DungeonSlashPrototype/Monsters/Data_Monster_B1_Minion_Leech.asset");
            var minotaur = AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/DungeonSlashPrototype/Monsters/Data_Monster_B1_Elite_Minotaur.asset");
            var mazeGuardian = AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/DungeonSlashPrototype/Monsters/Data_Monster_B1_Boss_MazeGuardian.asset");
            var frostOgre = AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/DungeonSlashPrototype/Monsters/Data_Monster_B2_Elite_FrostOgre.asset");
            var frostQueen = AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/DungeonSlashPrototype/Monsters/Data_Monster_B2_Boss_FrostQueen.asset");
            var soulKnight = AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/DungeonSlashPrototype/Monsters/Data_Monster_B3_Elite_SoulKnight.asset");

            Assert.That(leech.GetAttack(MonsterAttackType.Normal).combatMechanics.healOnUnguardedHit, Is.GreaterThan(0f));
            Assert.That(frostOgre.GetAttack(MonsterAttackType.Normal).combatMechanics.directionalGuard.enabled, Is.True);
            Assert.That(frostOgre.GetAttack(MonsterAttackType.Normal).combatMechanics.directionalGuard.breakByAttackWay, Is.EqualTo(TriggerAttackWayFilter.Horizontal));
            Assert.That(frostOgre.GetAttack(MonsterAttackType.Normal).combatMechanics.hitCount, Is.EqualTo(2));
            Assert.That(soulKnight.GetAttack(MonsterAttackType.Normal).combatMechanics.hitCount, Is.EqualTo(3));
            var minotaurCharge = minotaur.GetAttack(MonsterAttackType.Charge);
            Assert.That(minotaurCharge.combatMechanics.hitCount, Is.EqualTo(3));
            Assert.That(minotaurCharge.damage, Is.EqualTo(22.5f).Within(.001f));
            Assert.That(minotaurCharge.shieldDamage, Is.EqualTo(43.2f).Within(.001f));
            var mazeGuard = mazeGuardian.GetAttack(MonsterAttackType.Normal).combatMechanics.directionalGuard;
            Assert.That(mazeGuard.enabled, Is.True);
            Assert.That(mazeGuard.breakByAttackWay, Is.EqualTo(TriggerAttackWayFilter.Upward | TriggerAttackWayFilter.Downward));
            var queenCharge = frostQueen.GetAttack(MonsterAttackType.Charge);
            Assert.That(queenCharge.chargeSummonMechanics.openingSummons.Count, Is.EqualTo(2));
            Assert.That(queenCharge.chargeSummonMechanics.openingSummons.All(monster => monster == queenCharge.chargeSummonMechanics.summonOnFailedCharge), Is.True);
            Assert.That(queenCharge.chargeSummonMechanics.healOnFailedChargeWhileSummonAlive, Is.GreaterThan(0f));
        }

        [Test]
        public void MerchantMutantAmbush_StartsAtB2AndUsesFivePercentRolls()
        {
            Assert.That(Enumerable.Range(1, 100).Any(seed => GameFlowController.RollsMerchantMutantAmbush(seed, 4, 1)), Is.False);
            var ambushes = Enumerable.Range(1, 1000).Count(seed => GameFlowController.RollsMerchantMutantAmbush(seed, 4, 2));
            Assert.That(ambushes, Is.InRange(35, 65), "The deterministic merchant ambush sequence should remain near its 5% design rate.");
        }

        [Test]
        public void EquipmentTrigger_UsesAttackFiltersCountsAndConditionsInsteadOfHardcodedRelicRules()
        {
            var playerData = ScriptableObject.CreateInstance<PlayerCombatData>(); playerData.maxHp = 100f; playerData.shieldMax = 30f;
            var balance = ScriptableObject.CreateInstance<RunBalanceSettings>(); balance.initialExperienceRequirement = 100;
            var relic = ScriptableObject.CreateInstance<EquipmentData>();
            relic.itemKind = ShopItemKind.Relic;
            relic.trigger = new EquipmentTrigger { triggerType = TriggerType.OnHit, triggerAttackTypeFilter = TriggerAttackTypeFilter.NormalAttack | TriggerAttackTypeFilter.ChargeAttack, triggerAttackWayFilter = TriggerAttackWayFilter.Downward, triggerCount = 2, triggerMaxCount = 1 };
            relic.condition = new EquipmentCondition { conditionType = ConditionType.OwnerCharge };
            relic.targetType = TargetType.Enemy;
            relic.effect = new EquipmentEffect { effectType = EffectType.ExtraAttack, effectMagnitude = .55f, effectCount = 1 };
            var state = new RunState(playerData, balance);
            Assert.That(state.TryEquip(relic), Is.True);

            var ordinary = new EquipmentTriggerContext(default, TriggerAttackType.Normal, TriggerAttackWay.Downward, true, false);
            var extra = new EquipmentTriggerContext(default, TriggerAttackType.Additional, TriggerAttackWay.Downward, true, false);
            var noCharge = new EquipmentTriggerContext(default, TriggerAttackType.Normal, TriggerAttackWay.Downward, false, false);
            Assert.That(state.TriggerEquipment(TriggerType.OnHit, noCharge), Is.Empty, "Condition must run after the trigger filters and cadence.");
            Assert.That(state.TriggerEquipment(TriggerType.OnHit, extra), Is.Empty, "Additional attacks only chain when a data filter explicitly allows them.");
            Assert.That(state.TriggerEquipment(TriggerType.OnHit, ordinary).Count, Is.EqualTo(1), "The failed condition still consumed the first matching trigger cadence.");
            Assert.That(state.TriggerEquipment(TriggerType.OnHit, ordinary), Is.Empty, "TriggerMaxCount=1 must stop later activations.");
            Assert.That(state.Player.NormalDamageMultiplier, Is.EqualTo(1f));
            Object.DestroyImmediate(playerData); Object.DestroyImmediate(balance); Object.DestroyImmediate(relic);
        }

        [Test]
        public void EquipmentData_RemovesLegacyRelicPotionAndModifierFields()
        {
            foreach (var legacyField in new[] { "RelicEffect", "RelicMagnitude", "Modifiers", "PotionEffect", "PotionMagnitude", "PotionBattleCount" })
                Assert.That(typeof(EquipmentData).GetField(legacyField, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreCase), Is.Null);
        }

        [Test]
        public void DebugCheatOverlay_AwakeWiresSerializedActionButtons()
        {
            var root = new GameObject("CheatOverlay");
            var panel = new GameObject("CheatPanel");
            var start = new GameObject("Start", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button)).GetComponent<UnityEngine.UI.Button>();
            var addPerk = new GameObject("AddPerk", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button)).GetComponent<UnityEngine.UI.Button>();
            var addEquipment = new GameObject("AddEquipment", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button)).GetComponent<UnityEngine.UI.Button>();
            var status = new GameObject("Status", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>();
            var overlay = root.AddComponent<DebugCheatOverlay>();
            var serialized = new SerializedObject(overlay);
            serialized.FindProperty("panel").objectReferenceValue = panel;
            serialized.FindProperty("startBattleButton").objectReferenceValue = start;
            serialized.FindProperty("addPerkButton").objectReferenceValue = addPerk;
            serialized.FindProperty("addEquipmentButton").objectReferenceValue = addEquipment;
            serialized.FindProperty("statusLabel").objectReferenceValue = status;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Awake runs after scene fields are deserialized. Invoke it after assigning
            // the test fields to reproduce that runtime lifecycle exactly.
            typeof(DebugCheatOverlay).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.Invoke(overlay, null);
            start.onClick.Invoke();
            Assert.That(status.text, Is.Not.Empty, "The serialized Start Battle button must invoke the overlay callback at runtime.");
            status.text = string.Empty;
            addPerk.onClick.Invoke();
            Assert.That(status.text, Is.Not.Empty, "The serialized Add Perk button must invoke the overlay callback at runtime.");
            status.text = string.Empty;
            addEquipment.onClick.Invoke();
            Assert.That(status.text, Is.Not.Empty, "The serialized Add Equipment button must invoke the overlay callback at runtime.");
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(start.gameObject);
            Object.DestroyImmediate(addPerk.gameObject);
            Object.DestroyImmediate(addEquipment.gameObject);
            Object.DestroyImmediate(status.gameObject);
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
        public void CheatInventoryRemoval_ReversesPerkAndPermanentRelicStatsAndCanDiscardPotions()
        {
            var player = ScriptableObject.CreateInstance<PlayerCombatData>();
            player.maxHp = 100f;
            player.baseAttackDamage = 10f;
            var balance = ScriptableObject.CreateInstance<RunBalanceSettings>();
            balance.initialExperienceRequirement = 40;
            var perk = ScriptableObject.CreateInstance<PerkData>();
            perk.maxStacks = 3;
            perk.modifiers = new List<StatModifier> { new() { kind = ModifierKind.NormalDamage, value = .25f } };
            var relic = ScriptableObject.CreateInstance<EquipmentData>();
            relic.itemKind = ShopItemKind.Relic;
            relic.trigger = new EquipmentTrigger { triggerType = TriggerType.OnAcquire, triggerAttackTypeFilter = TriggerAttackTypeFilter.All, triggerAttackWayFilter = TriggerAttackWayFilter.All, triggerCount = 1 };
            relic.targetType = TargetType.Owner;
            relic.effect = new EquipmentEffect { effectType = EffectType.StatIncrease, effectMagnitude = .4f, effectCount = 1, effectStatType = ModifierKind.NormalDamage };
            var potion = ScriptableObject.CreateInstance<EquipmentData>();
            potion.itemKind = ShopItemKind.Potion;
            var state = new RunState(player, balance);
            var perkSystem = new PerkSystem(new[] { perk });

            perkSystem.Apply(state, perk);
            perkSystem.Apply(state, perk);
            Assert.That(state.Player.NormalAttackDamage, Is.EqualTo(15f).Within(.001f));
            Assert.That(state.TryRemovePerk(perk), Is.True);
            Assert.That(state.Player.NormalAttackDamage, Is.EqualTo(10f).Within(.001f));

            Assert.That(state.TryEquip(relic), Is.True);
            Assert.That(state.Player.NormalAttackDamage, Is.EqualTo(14f).Within(.001f));
            Assert.That(state.TryRemoveEquipment(relic), Is.True);
            Assert.That(state.Player.NormalAttackDamage, Is.EqualTo(10f).Within(.001f));

            Assert.That(state.TryStorePotion(potion), Is.True);
            Assert.That(state.TryRemoveEquipment(potion), Is.True);
            Assert.That(state.Potions, Is.Empty);
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(balance);
            Object.DestroyImmediate(perk);
            Object.DestroyImmediate(relic);
            Object.DestroyImmediate(potion);
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
            settings.minRooms = 20; settings.maxRooms = 20; settings.minimumBossDistance = 7; settings.minimumChestDistance = 4;
            settings.minimumRewardRooms = 2; settings.minimumFountainRooms = 1; settings.minimumShopRooms = 1; settings.minimumGoddessRooms = 1; settings.eliteRoomCount = 1; settings.chestRoomCount = 1; settings.loopChance = .2f; settings.maxGenerationAttempts = 200;
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
        public void Minimap_RevealedChestKeepsItsTypeLabelWhileItIsReachable()
        {
            var root = new GameObject("MapRoot", typeof(RectTransform), typeof(Canvas));
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.transform.SetParent(root.transform, false);
            var iconRoot = new GameObject("IconPrefab", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var iconLabel = new GameObject("Label", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>(); iconLabel.transform.SetParent(iconRoot.transform, false);
            var iconPrefab = iconRoot.AddComponent<MapRoomIcon>(); iconPrefab.Configure(iconRoot.GetComponent<UnityEngine.UI.Image>(), iconLabel);
            var connectionRoot = new GameObject("ConnectionPrefab", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var view = root.AddComponent<DungeonMapView>(); view.Configure(content, iconPrefab, connectionRoot.GetComponent<UnityEngine.UI.Image>());
            var graph = new DungeonGraph();
            var start = graph.AddRoom(new DungeonPosition(0, 0)); start.Type = RoomEncounterType.Start; start.IsCleared = true;
            var chest = graph.AddRoom(new DungeonPosition(1, 0)); chest.Type = RoomEncounterType.Chest; chest.IsRevealed = true;
            graph.Connect(start, chest);

            view.Render(new DungeonRunState(1, graph), FacingDirection.East);

            var targetIcon = content.GetComponentsInChildren<MapRoomIcon>().Single(icon => icon.GetComponent<RectTransform>().anchoredPosition.x > 0f);
            Assert.That(targetIcon.transform.Find("Label").GetComponent<UnityEngine.UI.Text>().text, Is.EqualTo("\uC0C1\uC790"));
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
            foreach (var propertyName in new[] { "runController", "dungeonController", "combatController", "perkChoiceView", "shopView", "resultView", "runHud", "roomTransitionView", "encounterMessageView", "roomEventView", "rewardPopupView", "goldEventSprite", "fountainEventSprite", "goddessEventSprite", "merchantEventSprite", "cheatOverlay" })
                Assert.That(serializedFlow.FindProperty(propertyName).objectReferenceValue, Is.Not.Null, $"GameFlowController.{propertyName} is not connected.");

            var cheat = components.OfType<DebugCheatOverlay>().Single();
            var serializedCheat = new SerializedObject(cheat);
            foreach (var propertyName in new[] { "panel", "startBattleButton", "infiniteBattleButton", "addPerkButton", "addEquipmentButton", "infiniteBattleLabel", "statusLabel" })
                Assert.That(serializedCheat.FindProperty(propertyName).objectReferenceValue, Is.Not.Null, $"DebugCheatOverlay.{propertyName} is not connected.");
            Assert.That(serializedCheat.FindProperty("monsterButtons").arraySize, Is.GreaterThanOrEqualTo(18));
            Assert.That(serializedCheat.FindProperty("perkButtons").arraySize, Is.GreaterThanOrEqualTo(18));
            Assert.That(serializedCheat.FindProperty("equipmentButtons").arraySize, Is.GreaterThanOrEqualTo(18));

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
            foreach (var propertyName in new[] { "damageNumberRoot", "damageNumberPrefab", "weakPointBreakFxRoot", "weakPointBreakFxPrefab", "mimicMonster", "merchantMutant" })
                Assert.That(serializedCombat.FindProperty(propertyName).objectReferenceValue, Is.Not.Null, $"CombatController.{propertyName} is not connected.");
            Assert.That(serializedCombat.FindProperty("bossMonsters").arraySize, Is.EqualTo(3), "Each floor needs its own boss definition.");
            Assert.That(serializedCombat.FindProperty("floorTwoMonsters").arraySize, Is.GreaterThanOrEqualTo(4), "Floor two needs its own roster plus the returning foe.");
            Assert.That(serializedCombat.FindProperty("floorThreeMonsters").arraySize, Is.GreaterThanOrEqualTo(3), "Floor three needs its own roster.");
            Assert.That(serializedCombat.FindProperty("eliteMonsters").arraySize, Is.EqualTo(3), "Each floor needs its own elite definition.");

            var runController = components.OfType<RunController>().Single();
            var serializedRun = new SerializedObject(runController);
            var equipment = serializedRun.FindProperty("equipment");
            Assert.That(equipment.arraySize, Is.GreaterThanOrEqualTo(9), "The shop needs both premium relics and tactical potions.");
            var allShopItems = Enumerable.Range(0, equipment.arraySize).Select(index => equipment.GetArrayElementAtIndex(index).objectReferenceValue as EquipmentData).ToArray();
            Assert.That(allShopItems.Count(item => item.IsPotion), Is.GreaterThanOrEqualTo(5), "The run needs several potion types.");
            Assert.That(allShopItems.Count(item => item.IsRelic), Is.GreaterThanOrEqualTo(4), "The run needs a meaningful relic pool.");
            Assert.That(allShopItems.Count(item => item.IsRelic && item.effect.effectType is EffectType.ExtraAttack or EffectType.Damage), Is.GreaterThanOrEqualTo(3), "Relics should supply distinct trigger-driven actions instead of perk-like stat bundles.");
            Assert.That(allShopItems.All(item => item.icon != null), Is.True, "Every relic and potion needs an icon asset for the run HUD.");
            Assert.That(allShopItems.All(item => AssetDatabase.GetAssetPath(item.icon).EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)), Is.True, "Run HUD item icons must be editable PNG sprite assets, not generated Texture2D assets.");
            var allPerks = Enumerable.Range(0, serializedRun.FindProperty("perks").arraySize).Select(index => serializedRun.FindProperty("perks").GetArrayElementAtIndex(index).objectReferenceValue as PerkData).ToArray();
            Assert.That(allPerks.All(perk => perk.icon != null), Is.True, "Every perk needs a skill icon asset for the run HUD.");
            Assert.That(allPerks.All(perk => AssetDatabase.GetAssetPath(perk.icon).EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)), Is.True, "Run HUD skill icons must be editable PNG sprite assets, not generated Texture2D assets.");
            var serializedRunHud = new SerializedObject(components.OfType<RunHudView>().Single());
            foreach (var propertyName in new[] { "perkGrid", "relicGrid", "potionGrid", "iconPrefab", "tooltip" })
                Assert.That(serializedRunHud.FindProperty(propertyName).objectReferenceValue, Is.Not.Null, $"RunHudView.{propertyName} must support the icon inventory UI.");
            foreach (var propertyName in new[] { "perkGrid", "relicGrid", "potionGrid" })
            {
                var grid = (serializedRunHud.FindProperty(propertyName).objectReferenceValue as RectTransform).GetComponent<UnityEngine.UI.GridLayoutGroup>();
                Assert.That(grid.startAxis, Is.EqualTo(UnityEngine.UI.GridLayoutGroup.Axis.Horizontal));
                Assert.That(grid.constraint, Is.EqualTo(UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount));
                Assert.That(grid.constraintCount, Is.EqualTo(5));
                var expectedSlotCount = propertyName == "potionGrid" ? 3 : 15;
                Assert.That(grid.GetComponentsInChildren<RunHudIconView>(true).Length, Is.EqualTo(expectedSlotCount), $"{propertyName} needs its visible icon slots as scene children.");
            }
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/RunHudIconView.prefab").GetComponent<RunHudIconView>(), Is.Not.Null, "The run HUD needs a reusable hoverable item-icon view.");
            Assert.That(new SerializedObject(components.OfType<PerkChoiceView>().Single()).FindProperty("title").objectReferenceValue, Is.Not.Null, "Relic and perk selections need a panel title.");
            var perkChoiceItem = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/PerkChoiceItem.prefab").GetComponent<PerkChoiceItem>();
            foreach (var propertyName in new[] { "button", "label", "icon" })
                Assert.That(new SerializedObject(perkChoiceItem).FindProperty(propertyName).objectReferenceValue, Is.Not.Null, $"PerkChoiceItem.{propertyName} must render icon-equipped reward choices.");
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
            foreach (var propertyName in new[] { "chargeGaugeRoot", "chargeGaugeTrack", "chargeGaugeFill", "chargeGaugeSecondFill", "playerHpText", "shieldText" })
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
