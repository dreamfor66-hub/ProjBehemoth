using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonSlash
{
    public readonly struct CombatResult
    {
        public bool PlayerWon { get; }
        public bool PlayerDied { get; }
        public IReadOnlyList<MonsterData> Monsters { get; }
        public MonsterData Monster => Monsters.Count > 0 ? Monsters[0] : null;

        public CombatResult(bool playerWon, bool playerDied, IReadOnlyList<MonsterData> monsters)
        {
            PlayerWon = playerWon;
            PlayerDied = playerDied;
            Monsters = monsters ?? Array.Empty<MonsterData>();
        }
    }

    public sealed class CombatController : MonoBehaviour
    {
        private const float FinalImpactHoldSeconds = .5f;
        private const float MonsterFadeSeconds = .45f;
        private const float SummonedMinionScale = .62f;

        private sealed class ActiveMonster
        {
            public MonsterRuntime Runtime { get; }
            public MonsterView View { get; }
            public Vector2 Position { get; }
            public float VisualScale { get; }
            public List<WeakPointView> WeakPointViews { get; } = new();
            public float WeakPointVisualHoldUntil { get; set; }

            public ActiveMonster(MonsterRuntime runtime, MonsterView view, Vector2 position, float visualScale)
            {
                Runtime = runtime;
                View = view;
                Position = position;
                VisualScale = Mathf.Clamp(visualScale, .25f, 1f);
            }
        }

        [SerializeField] private CombatSceneView sceneView;
        [SerializeField] private CombatInputController input;
        [SerializeField] private CombatHudView hud;
        [SerializeField] private PlayerCombatData playerData;
        [SerializeField] private List<MonsterData> normalMonsters = new();
        [SerializeField] private List<MonsterData> floorTwoMonsters = new();
        [SerializeField] private List<MonsterData> floorThreeMonsters = new();
        [SerializeField] private List<MonsterData> eliteMonsters = new();
        [SerializeField] private MonsterData eliteFallback;
        [SerializeField] private List<MonsterData> bossMonsters = new();
        [SerializeField] private MonsterData mimicMonster;
        [SerializeField] private MonsterData merchantMutant;
        [SerializeField] private Transform playerSpawnRoot;
        [SerializeField] private Transform monsterSpawnRoot;
        [SerializeField] private Transform weakPointRoot;
        [SerializeField] private Transform weakPointBreakFxRoot;
        [SerializeField] private PlayerView playerPrefab;
        [SerializeField] private MonsterView monsterPrefab;
        [SerializeField] private WeakPointView weakPointPrefab;
        [SerializeField] private WeakPointBreakFxView weakPointBreakFxPrefab;
        [SerializeField] private AttackLineView attackLine;
        [SerializeField] private Transform damageNumberRoot;
        [SerializeField] private DamageNumberView damageNumberPrefab;

        private readonly DamageResolver damageResolver = new();
        private readonly List<ActiveMonster> activeMonsters = new();
        private PlayerView playerView;
        private RunState run;
        private bool isFinishing;
        private int encounterFloor = 1;
        private int trackedFloor = -1;
        private int normalEncountersOnFloor;

        public bool IsActive { get; private set; }
        public bool InfiniteBattleEnabled { get; private set; }
        public bool HasPreparedMonsters => activeMonsters.Any(monster => monster.Runtime.IsAlive);
        public PlayerCombatRuntime Player => run?.Player;
        public MonsterData MimicMonster => mimicMonster;
        public MonsterData MerchantMutant => merchantMutant;
        public event Action<CombatResult> Completed;

        public IReadOnlyList<MonsterData> GetDebugMonsterCatalog() => normalMonsters
            .Concat(floorTwoMonsters)
            .Concat(floorThreeMonsters)
            .Concat(eliteMonsters)
            .Concat(bossMonsters)
            .Append(mimicMonster)
            .Append(merchantMutant)
            .Where(monster => monster != null)
            .Distinct()
            .OrderBy(monster => monster.displayName)
            .ToArray();

        public void SetInfiniteBattle(bool enabled) => InfiniteBattleEnabled = enabled;

        public void ShowPlayer(RunState runState)
        {
            run = runState;
            EnsurePlayerView();
            playerView.Root.anchoredPosition = Vector2.zero;
            playerView.Bind(Player);
            RefreshHud();
        }

        public void Configure(CombatSceneView newSceneView, CombatInputController newInput, CombatHudView newHud, PlayerCombatData newPlayerData, List<MonsterData> newFloorOneMonsters, List<MonsterData> newFloorTwoMonsters, List<MonsterData> newFloorThreeMonsters, List<MonsterData> newEliteMonsters, List<MonsterData> newBosses, MonsterData newMimic, MonsterData newMerchantMutant, Transform newPlayerRoot, Transform newMonsterRoot, Transform newWeakPointRoot, Transform newWeakPointBreakFxRoot, PlayerView newPlayerPrefab, MonsterView newMonsterPrefab, WeakPointView newWeakPointPrefab, WeakPointBreakFxView newWeakPointBreakFxPrefab, AttackLineView newAttackLine, Transform newDamageNumberRoot, DamageNumberView newDamageNumberPrefab)
        {
            sceneView = newSceneView;
            input = newInput;
            hud = newHud;
            playerData = newPlayerData;
            normalMonsters = newFloorOneMonsters ?? new List<MonsterData>();
            floorTwoMonsters = newFloorTwoMonsters ?? new List<MonsterData>();
            floorThreeMonsters = newFloorThreeMonsters ?? new List<MonsterData>();
            eliteMonsters = newEliteMonsters ?? new List<MonsterData>();
            eliteFallback = eliteMonsters.FirstOrDefault(monster => monster != null);
            bossMonsters = newBosses ?? new List<MonsterData>();
            mimicMonster = newMimic;
            merchantMutant = newMerchantMutant;
            playerSpawnRoot = newPlayerRoot;
            monsterSpawnRoot = newMonsterRoot;
            weakPointRoot = newWeakPointRoot;
            weakPointBreakFxRoot = newWeakPointBreakFxRoot;
            playerPrefab = newPlayerPrefab;
            monsterPrefab = newMonsterPrefab;
            weakPointPrefab = newWeakPointPrefab;
            weakPointBreakFxPrefab = newWeakPointBreakFxPrefab;
            attackLine = newAttackLine;
            damageNumberRoot = newDamageNumberRoot;
            damageNumberPrefab = newDamageNumberPrefab;
        }

        public void BeginCombat(RunState runState, RoomEncounterType roomType, int roomId)
        {
            PrepareEncounter(runState, roomType, roomId, 1);
            BeginPreparedCombat();
        }

        public void PrepareEncounter(RunState runState, RoomEncounterType roomType, int roomId, int floor = 1)
        {
            var normalizedFloor = Mathf.Max(1, floor);
            if (trackedFloor != normalizedFloor)
            {
                trackedFloor = normalizedFloor;
                normalEncountersOnFloor = 0;
            }
            var encounter = GetEncounterMonsters(roomType, roomId, normalizedFloor);
            if (roomType == RoomEncounterType.Combat) normalEncountersOnFloor++;
            PrepareEncounter(runState, encounter, normalizedFloor);
        }

        /// <summary>Entry point for future room definitions that spawn a group rather than one monster.</summary>
        public void PrepareEncounter(RunState runState, IReadOnlyList<MonsterData> encounterMonsters, int floor = 1)
        {
            run = runState;
            encounterFloor = Mathf.Max(1, floor);
            sceneView?.ResetCameraShake();
            run?.Player?.ResetAttackCooldown();
            var combatStartEffect = run?.BeginCombat() ?? default;
            ClearMonsterViews();
            isFinishing = false;
            IsActive = false;
            EnsurePlayerView();
            playerView.Root.anchoredPosition = Vector2.zero;

            var validMonsters = encounterMonsters?.Where(data => data != null).ToArray() ?? Array.Empty<MonsterData>();
            var encounterLeader = validMonsters.FirstOrDefault();
            for (var index = 0; index < validMonsters.Length; index++)
            {
                var isOpeningSummon = index > 0 && encounterLeader != null && GetOpeningSummons(encounterLeader).Contains(validMonsters[index]);
                var scale = isOpeningSummon ? SummonedMinionScale : 1f;
                var actionDelay = isOpeningSummon ? .45f + (index - 1) * .75f : 0f;
                SpawnMonster(validMonsters[index], sceneView.GetMonsterFormationPosition(index, validMonsters.Length), scale, actionDelay);
            }

            ApplyOpeningRockDamage(combatStartEffect.OpeningRockDamage);

            if (attackLine != null) attackLine.Hide();
            RefreshHud();
        }

        public void BeginPreparedCombat()
        {
            if (!activeMonsters.Any(monster => monster.Runtime.IsAlive)) return;
            IsActive = true;
            RefreshHud();
        }

        public MonsterData GetMonsterData(RoomEncounterType roomType, int roomId, int floor = 1) => GetEncounterMonsters(roomType, roomId, floor).FirstOrDefault();

        private IReadOnlyList<MonsterData> GetEncounterMonsters(RoomEncounterType roomType, int roomId, int floor)
        {
            if (roomType == RoomEncounterType.Boss)
            {
                var boss = bossMonsters.Count == 0 ? null : bossMonsters[Mathf.Clamp(floor - 1, 0, bossMonsters.Count - 1)];
                if (boss == null) return Array.Empty<MonsterData>();
                var encounter = new List<MonsterData> { boss };
                encounter.AddRange(GetOpeningSummons(boss).Where(monster => monster != null));
                return encounter;
            }
            if (roomType == RoomEncounterType.Elite)
            {
                if (eliteFallback == null) eliteFallback = eliteMonsters.FirstOrDefault(monster => monster != null);
                var elite = eliteMonsters.Count == 0 ? eliteFallback : eliteMonsters[Mathf.Clamp(floor - 1, 0, eliteMonsters.Count - 1)];
                elite ??= eliteFallback ?? normalMonsters.FirstOrDefault(monster => monster != null);
                return elite == null ? Array.Empty<MonsterData>() : new[] { elite };
            }
            // A familiar bat is the first standard enemy on floor two; later rooms use its new local roster.
            if (floor == 2 && (trackedFloor != floor || normalEncountersOnFloor == 0) && normalMonsters.Count > 1)
                return new[] { normalMonsters[1] };
            var pool = GetNormalMonsterPool(floor);
            var data = pool.Count == 0 ? null : pool[Mathf.Abs(roomId) % pool.Count];
            return data == null ? Array.Empty<MonsterData>() : new[] { data };
        }

        private static IEnumerable<MonsterData> GetOpeningSummons(MonsterData monster)
        {
            return monster?.GetAttacks(MonsterAttackType.Charge)
                .SelectMany(attack => attack.chargeSummonMechanics?.openingSummons ?? Enumerable.Empty<MonsterData>())
                ?? Enumerable.Empty<MonsterData>();
        }

        private IReadOnlyList<MonsterData> GetNormalMonsterPool(int floor)
        {
            if (floor >= 3 && floorThreeMonsters.Count > 0) return floorThreeMonsters;
            if (floor == 2 && floorTwoMonsters.Count > 0) return floorTwoMonsters;
            return normalMonsters;
        }

        private void Update()
        {
            if (run == null) return;
            if (playerView != null) playerView.Bind(Player);
            if (!IsActive)
            {
                RefreshHud();
                return;
            }

            var deltaTime = Time.deltaTime;
            var keyboardGuard = Player != null && input != null && input.IsGuardKeyHeld;
            var chargeGuard = Player != null && Player.ChargeGuardEnabled && input != null && input.IsChargingAction;
            Player.SetKeyboardGuard(keyboardGuard);
            Player.SetChargeGuard(chargeGuard);
            Player.Tick(deltaTime);
            foreach (var monster in activeMonsters.Where(monster => monster.Runtime.IsAlive))
                monster.Runtime.Tick(deltaTime, monster.Position);

            ProcessMonsterActions();
            if (!IsActive) return;
            SyncWeakPointViews();
            foreach (var monster in activeMonsters)
                monster.View.Bind(monster.Runtime);
            RefreshHud();
        }

        private void ProcessMonsterActions()
        {
            for (var index = 0; index < activeMonsters.Count; index++)
            {
                var monster = activeMonsters[index];
                if (!monster.Runtime.IsAlive) continue;
                var runtime = monster.Runtime;
                if (runtime.ShouldCharge && runtime.HasChargeAttack)
                {
                    runtime.StartCharge(monster.Position);
                    monster.View.ShowAttackName(runtime.CurrentChargeAttack.displayName, runtime.StateTimer);
                    continue;
                }
                if (runtime.ShouldNormalAttack)
                {
                    runtime.BeginNormalAttack();
                    monster.View.ShowAttackName(runtime.NormalAttack?.displayName, runtime.NormalAttackPresentationDuration);
                    continue;
                }
                if (runtime.IsNormalAttackReadyToHit)
                {
                    var result = ApplyMonsterAttack(monster, runtime.NormalAttack);
                    runtime.AdvanceNormalAttack();
                    if (!IsActive) return;
                    if (result.TargetDied && !TryRevivePlayer()) { Finish(false); return; }
                }
                if (runtime.ChargeExpired)
                {
                    var result = ApplyMonsterAttack(monster, runtime.CurrentChargeAttack);
                    if (!IsActive) return;
                    if (result.TargetDied && !TryRevivePlayer()) { Finish(false); return; }
                    ClearWeakPointViews(monster);
                    if (runtime.AdvanceChargeAttack()) continue;
                    ResolveFailedCharge(monster);
                    runtime.EndCharge();
                }
            }
        }

        public bool TryBeginGesture(Vector2 start)
        {
            if (!IsActive || !Player.IsAlive) return false;
            Player.TrySetGuard(false);
            return true;
        }

        public bool IsGuardGesture(Vector2 pressPosition, Vector2 releasePosition) => IsActive && GuardGestureResolver.IsPlayerDownwardGuard(GetPlayerBodyShape(), pressPosition, releasePosition, Player.GuardSwipeDistance, Player.GuardBodyOuterMargin);

        public bool TryBeginGuard(Vector2 pressPosition, Vector2 releasePosition)
        {
            if (!IsGuardGesture(pressPosition, releasePosition) || !activeMonsters.Any(monster => monster.Runtime.IsAlive && !monster.Runtime.IsStunned)) return false;
            return Player.TrySetGuard(true);
        }

        public bool TryBeginRestGuard(Vector2 pressPosition, Vector2 holdingPosition)
        {
            if (!IsActive ||
                !IsRestGuardCandidate(pressPosition, holdingPosition) ||
                !activeMonsters.Any(monster => monster.Runtime.IsAlive && !monster.Runtime.IsStunned))
                return false;
            return Player.TrySetGuard(true);
        }

        public bool IsRestGuardCandidate(Vector2 pressPosition, Vector2 holdingPosition)
        {
            return IsActive && GuardGestureResolver.IsPlayerRestGuardCandidate(GetPlayerBodyShape(), pressPosition, holdingPosition, Player.GuardSwipeDistance);
        }

        public bool IsPointerInsidePlayerBody(Vector2 position) => RectHitResolver.Contains(GetPlayerBodyShape(), position);

        public bool TryBeginBodyEntryGuard(Vector2 pressPosition, Vector2 holdingPosition)
        {
            if (!IsActive ||
                !IsPointerInsidePlayerBody(holdingPosition) ||
                !GuardGestureResolver.IsDownwardApproach(pressPosition, holdingPosition) ||
                !activeMonsters.Any(monster => monster.Runtime.IsAlive && !monster.Runtime.IsStunned))
                return false;
            return Player.TrySetGuard(true);
        }

        public bool DoesPathTouchPlayerGuardZone(Vector2 from, Vector2 to)
        {
            return RectHitResolver.Intersects(new AttackSegment(from, to, 0f), GetPlayerBodyShape(), Player.GuardBodyOuterMargin);
        }

        /// <summary>Finds the first living monster crossed by the current pointer movement, using the same width as the player's hit sweep.</summary>
        public bool TryFindTraversalTarget(Vector2 from, Vector2 to, out int targetIndex)
        {
            targetIndex = -1;
            var movement = new AttackSegment(from, to, Player?.SegmentWidth ?? 0f);
            for (var index = 0; index < activeMonsters.Count; index++)
            {
                var monster = activeMonsters[index];
                if (!monster.Runtime.IsAlive) continue;
                var target = new CircleHitShape(monster.Position, monster.Runtime.Data.bodyHitRadius * monster.VisualScale);
                if (!TargetTraversalResolver.Touches(movement, target)) continue;
                targetIndex = index;
                return true;
            }
            return false;
        }

        public bool TryGetTraversalTargetState(int targetIndex, Vector2 position, out bool insideTarget, out float outsideDistance)
        {
            insideTarget = false;
            outsideDistance = 0f;
            if (targetIndex < 0 || targetIndex >= activeMonsters.Count) return false;
            var monster = activeMonsters[targetIndex];
            if (!monster.Runtime.IsAlive) return false;
            var target = new CircleHitShape(monster.Position, monster.Runtime.Data.bodyHitRadius * monster.VisualScale);
            var width = Player?.SegmentWidth ?? 0f;
            insideTarget = TargetTraversalResolver.Contains(target, position, width);
            outsideDistance = TargetTraversalResolver.OutsideDistance(target, position, width);
            return true;
        }

        public bool DoesTraversalTargetTouch(int targetIndex, Vector2 from, Vector2 to)
        {
            if (targetIndex < 0 || targetIndex >= activeMonsters.Count) return false;
            var monster = activeMonsters[targetIndex];
            if (!monster.Runtime.IsAlive) return false;
            var target = new CircleHitShape(monster.Position, monster.Runtime.Data.bodyHitRadius * monster.VisualScale);
            return TargetTraversalResolver.Touches(new AttackSegment(from, to, Player?.SegmentWidth ?? 0f), target);
        }

        public void ExecuteAttack(AttackSegment segment, bool charged) => ExecuteAttack(segment, charged, 1);

        public void ExecuteAttack(AttackSegment segment, bool charged, int chargeLevel)
        {
            if (!IsActive || !Player.IsAlive || (!charged && !Player.CanNormalAttack())) return;
            // Charge attacks use their own input preparation, but still consume the shared follow-up attack cooldown.
            Player.StartAttackCooldown();
            segment = ExtendForAttackReach(segment, Player.AttackReach);
            if (attackLine != null) attackLine.Show(segment, charged);
            var directDamageMultiplier = charged && chargeLevel >= 2 ? 2f : 1f;
            ExecutePlayerAttack(segment, charged, directDamageMultiplier, TriggerAttackTypeFor(charged), true, true, 0);

            if (activeMonsters.Count > 0 && activeMonsters.All(monster => !monster.Runtime.IsAlive)) Finish(true);
        }

        private void ExecutePlayerAttack(AttackSegment segment, bool charged, float damageMultiplier, TriggerAttackType attackType, bool resolveWeakPoints, bool isDirectAttack, int chainDepth)
        {
            if (!IsActive || chainDepth > 8) return;
            var target = activeMonsters.FirstOrDefault(monster => monster.Runtime.IsAlive);
            var context = BuildAttackContext(segment, attackType, charged, target);
            ResolveEquipmentTriggers(TriggerType.OnAttack, context, target, chainDepth);
            ApplyPlayerAttack(segment, charged, damageMultiplier, attackType, resolveWeakPoints, isDirectAttack, chainDepth);
        }

        private bool ApplyPlayerAttack(AttackSegment segment, bool charged, float damageMultiplier, TriggerAttackType attackType, bool resolveWeakPoints, bool isDirectAttack, int chainDepth)
        {
            var hitAny = false;
            foreach (var monster in activeMonsters.Where(monster => monster.Runtime.IsAlive))
            {
                var runtime = monster.Runtime;
                // Resolve weak points before body damage so a lethal charged slash still bursts the lock-on it crossed.
                if (resolveWeakPoints && charged && runtime.State == MonsterState.Charging)
                {
                    foreach (var point in WeakPointHitResolver.Resolve(segment, runtime.WeakPoints))
                    {
                        var previousRemainingHits = point.RemainingHits;
                        if (!point.ApplyChargeHit()) continue;
                        ShowWeakPointChargeHit(monster, point, previousRemainingHits);
                        Player.RestoreShield(Player.ShieldOnWeakPoint);
                        if (point.IsDestroyed) ShowWeakPointBreak(point.HitShape.Center);
                        ShakeCombat(point.IsDestroyed ? 12f : 6f, point.IsDestroyed ? .16f : .1f);
                    }
                    if (runtime.AllWeakPointsDestroyed)
                    {
                        runtime.EnterStun();
                        HoldWeakPointViewsForImpact(monster);
                    }
                }
                if (SegmentHitResolver.Intersects(segment, new CircleHitShape(monster.Position, runtime.Data.bodyHitRadius * monster.VisualScale)))
                {
                    var baseDamage = (charged ? Player.ChargeAttackDamage : Player.NormalAttackDamage) * damageMultiplier * GetDirectAttackDamageMultiplier(segment, isDirectAttack);
                    if (runtime.TryAbsorbDirectionalGuard(GetAttackWay(segment), baseDamage, out var absorbedDamage, out var matchedDirection, out var guardBroken))
                    {
                        monster.View?.Bind(runtime);
                        var impact = ClosestPointOnSegment(segment, monster.Position);
                        if (matchedDirection)
                            ShowDamage(impact, $"방벽 -{absorbedDamage:0}", new Color(.28f, .86f, 1f));
                        else
                            ShowDamage(impact, "방향 불일치", new Color(.62f, .72f, .82f));
                        if (guardBroken)
                            ShowDamage(monster.Position + new Vector2(0f, 42f), "방벽 파괴", new Color(.48f, 1f, 1f));
                        ShakeCombat(guardBroken ? 5f : 2.5f, .07f);
                        continue;
                    }

                    hitAny = true;
                    ShakeCombat(charged ? 8f : 3.5f, charged ? .13f : .08f);
                    var dealtDamage = runtime.IsStunned ? baseDamage * Player.StunDamageMultiplier : baseDamage;
                    if (!InfiniteBattleEnabled)
                        damageResolver.ApplyToMonster(runtime, baseDamage, Player.StunDamageMultiplier);
                    // Update immediately: the final hit must visibly empty the HP gauge before its impact hold.
                    monster.View?.Bind(runtime);
                    if (!runtime.IsAlive && Player.MonsterKillHeal > 0f) Player.Heal(Player.MonsterKillHeal);
                    Player.RestoreShield(Player.ShieldOnHit);
                    if (!InfiniteBattleEnabled)
                        ShowDamage(ClosestPointOnSegment(segment, monster.Position), $"{dealtDamage:0}", charged ? new Color(1f, .78f, .16f) : Color.white);
                    ResolveEquipmentTriggers(TriggerType.OnHit, BuildAttackContext(segment, attackType, charged, monster), monster, chainDepth);
                }

            }
            return hitAny;
        }

        private DamageResult ApplyMonsterAttack(ActiveMonster source, MonsterAttackData attack)
        {
            var runtime = source.Runtime;
            if (attack == null) return default;
            var rawDamage = InfiniteBattleEnabled ? 0f : attack.damage * runtime.DamageMultiplier;
            var result = damageResolver.ApplyToPlayer(Player, new DamageRequest(DamageType.MonsterAttack, rawDamage, attack.shieldDamage * runtime.DamageMultiplier, attack.guardable));
            ShowPlayerDamage(result);
            if (result.HpDamage > 0f)
                ShakeCombat(8f, .14f);
            else if (result.ShieldDamage > 0f)
                ShakeCombat(result.WasGuarded ? 3f : 4.5f, .09f);
            ResolveDamageTriggers(result, source);
            var mechanics = attack.combatMechanics;
            if (result.HpDamage > 0f && mechanics != null && mechanics.healOnUnguardedHit > 0f)
            {
                var healed = runtime.Heal(mechanics.healOnUnguardedHit);
                if (healed > 0f)
                {
                    source.View?.Bind(runtime);
                    ShowDamage(source.Position, $"+{healed:0}", new Color(.38f, 1f, .5f));
                }
            }
            return result;
        }

        private void ResolveFailedCharge(ActiveMonster source)
        {
            var mechanics = source.Runtime.CurrentChargeAttack?.chargeSummonMechanics;
            var summon = mechanics?.summonOnFailedCharge;
            if (summon == null) return;

            var hasLivingSummon = activeMonsters.Any(monster => monster != source && monster.Runtime.IsAlive && monster.Runtime.Data == summon);
            if (hasLivingSummon)
            {
                var healed = source.Runtime.Heal(mechanics.healOnFailedChargeWhileSummonAlive);
                if (healed > 0f)
                {
                    source.View?.Bind(source.Runtime);
                    source.View?.ShowAttackName("빙결 흡수", .8f);
                    ShowDamage(source.Position, $"+{healed:0}", new Color(.38f, 1f, .5f));
                }
                return;
            }

            var existingSummons = activeMonsters.Count(monster => monster.Runtime.IsAlive && monster.Runtime.Data == summon);
            var direction = existingSummons % 2 == 0 ? -1f : 1f;
            var position = source.Position + new Vector2(direction * 176f, -16f);
            SpawnMonster(summon, position, SummonedMinionScale, .45f).View?.ShowAttackName("소환됨", .7f);
            source.View?.ShowAttackName("빙결 소환", .8f);
        }

        private void ResolveDamageTriggers(DamageResult result, ActiveMonster source)
        {
            if (run == null) return;
            var context = EquipmentTriggerContext.Simple(input != null && input.IsChargingAction, source != null && source.Runtime.IsStunned);
            if (result.HpDamage > 0f) ResolveEquipmentTriggers(TriggerType.OnHurt, context, source, 0);
            if (result.WasGuarded) ResolveEquipmentTriggers(TriggerType.OnGuard, context, source, 0);
            if (activeMonsters.Count > 0 && activeMonsters.All(monster => !monster.Runtime.IsAlive)) Finish(true);
        }

        private void ResolveEquipmentTriggers(TriggerType triggerType, EquipmentTriggerContext context, ActiveMonster source, int chainDepth)
        {
            if (run == null || chainDepth > 8) return;
            foreach (var activation in run.TriggerEquipment(triggerType, context))
            {
                if (activation.Target == TargetType.Owner)
                {
                    run.ApplyOwnerActivation(activation, permanent: false);
                    continue;
                }

                var effect = activation.Effect;
                var target = source ?? activeMonsters.FirstOrDefault(monster => monster.Runtime.IsAlive);
                switch (effect.effectType)
                {
                    case EffectType.ExtraAttack when context.HasAttack:
                        for (var index = 0; index < Mathf.Max(1, effect.effectCount); index++)
                        {
                            var additional = RotateAtMidpoint(context.Segment, effect.effectAngle);
                            attackLine?.ShowAdditional(additional, context.AttackType == TriggerAttackType.Charge);
                            ExecutePlayerAttack(additional, context.AttackType == TriggerAttackType.Charge, Mathf.Max(0f, effect.effectMagnitude), TriggerAttackType.Additional, false, false, chainDepth + 1);
                        }
                        break;
                    case EffectType.Damage when target != null:
                        ApplyEquipmentDamage(target, effect);
                        break;
                }
            }
        }

        private void ApplyEquipmentDamage(ActiveMonster target, EquipmentEffect effect)
        {
            var baseDamage = Player.NormalAttackDamage * Mathf.Max(0f, effect.effectMagnitude) * Mathf.Max(1, effect.effectCount);
            var dealtDamage = target.Runtime.IsStunned ? baseDamage * Player.StunDamageMultiplier : baseDamage;
            if (!InfiniteBattleEnabled)
                damageResolver.ApplyToMonster(target.Runtime, baseDamage, Player.StunDamageMultiplier);
            target.View?.Bind(target.Runtime);
            if (!InfiniteBattleEnabled)
                ShowDamage(target.Position, $"{dealtDamage:0}", new Color(.8f, .55f, 1f));
        }

        private EquipmentTriggerContext BuildAttackContext(AttackSegment segment, TriggerAttackType attackType, bool charged, ActiveMonster target)
        {
            return new EquipmentTriggerContext(segment, attackType, GetAttackWay(segment), charged || (input != null && input.IsChargingAction), target != null && target.Runtime.IsStunned);
        }

        private float GetDirectAttackDamageMultiplier(AttackSegment segment, bool isDirectAttack)
        {
            return !isDirectAttack ? 1f : 1f + (segment.End - segment.Start).magnitude * Player.DamagePerAttackDistance;
        }

        private void SyncWeakPointViews()
        {
            foreach (var monster in activeMonsters)
            {
                if (monster.Runtime.State != MonsterState.Charging)
                {
                    ClearWeakPointViews(monster);
                    continue;
                }
                if (monster.WeakPointViews.Count != monster.Runtime.WeakPoints.Count)
                {
                    ClearWeakPointViews(monster);
                    foreach (var _ in monster.Runtime.WeakPoints)
                        monster.WeakPointViews.Add(Instantiate(weakPointPrefab, weakPointRoot));
                }
                for (var index = 0; index < monster.WeakPointViews.Count; index++)
                    monster.WeakPointViews[index].Bind(monster.Runtime.WeakPoints[index]);
            }
        }

        private void ClearWeakPointViews(ActiveMonster monster, bool force = false)
        {
            if (!force && monster.WeakPointVisualHoldUntil > Time.unscaledTime) return;
            foreach (var view in monster.WeakPointViews)
                if (view != null) Destroy(view.gameObject);
            monster.WeakPointViews.Clear();
            monster.WeakPointVisualHoldUntil = 0f;
        }

        private ActiveMonster SpawnMonster(MonsterData data, Vector2 position, float visualScale = 1f, float initialActionDelay = 0f)
        {
            // Floor identity and balance now live in each monster asset; never double-scale them here.
            var runtime = new MonsterRuntime(data, visualScale: visualScale);
            runtime.DelayActions(initialActionDelay);
            var view = Instantiate(monsterPrefab, monsterSpawnRoot);
            view.Root.anchoredPosition = position - sceneView.MonsterPosition;
            view.Root.localScale = Vector3.one * runtime.VisualScale;
            view.ResetOpacity();
            view.Bind(runtime);
            var active = new ActiveMonster(runtime, view, position, runtime.VisualScale);
            activeMonsters.Add(active);
            return active;
        }

        private void ClearMonsterViews()
        {
            foreach (var monster in activeMonsters)
            {
                ClearWeakPointViews(monster, true);
                if (monster.View != null) Destroy(monster.View.gameObject);
            }
            activeMonsters.Clear();
        }

        private void EnsurePlayerView()
        {
            if (playerView == null) playerView = Instantiate(playerPrefab, playerSpawnRoot);
        }

        private RectHitShape GetPlayerBodyShape()
        {
            var root = playerView != null ? playerView.Root : playerPrefab != null ? playerPrefab.Root : null;
            var size = root != null ? root.rect.size : new Vector2(270f, 270f);
            return new RectHitShape(sceneView.PlayerPosition, size);
        }

        private void RefreshHud()
        {
            if (hud == null || Player == null) return;
            var gesture = input == null ? PointerGestureState.None : input.State;
            var progress = input == null ? 0f : input.ChargeProgress;
            var secondProgress = input == null ? 0f : input.SecondChargeProgress;
            var origin = input == null ? Vector2.zero : input.ChargeOrigin;
            hud.Bind(Player, null, gesture, progress, origin, secondProgress);
        }

        private void ShowPlayerDamage(DamageResult result)
        {
            if (result.HpDamage > 0f) ShowDamage(sceneView.PlayerPosition, $"-{result.HpDamage:0}", new Color(1f, .32f, .32f));
            else if (result.ShieldDamage > 0f) ShowDamage(sceneView.PlayerPosition, $"-{result.ShieldDamage:0}", new Color(.3f, 1f, .7f));
        }

        private bool TryRevivePlayer()
        {
            if (run == null || !run.TryConsumeRevival()) return false;
            ShowDamage(sceneView.PlayerPosition, "\uBD80\uD65C", new Color(.42f, 1f, .65f));
            return true;
        }

        private void ShowDamage(Vector2 position, string message, Color color)
        {
            if (damageNumberRoot == null || damageNumberPrefab == null) return;
            var view = Instantiate(damageNumberPrefab, damageNumberRoot);
            view.Show(position, message, color);
        }

        private void ShakeCombat(float magnitude, float duration)
        {
            sceneView?.TriggerCameraShake(magnitude, duration);
        }

        private void ShowWeakPointBreak(Vector2 position)
        {
            if (weakPointBreakFxRoot == null || weakPointBreakFxPrefab == null) return;
            Instantiate(weakPointBreakFxPrefab, weakPointBreakFxRoot).Play(position, FinalImpactHoldSeconds);
        }

        private void ShowWeakPointChargeHit(ActiveMonster monster, WeakPointRuntime point, int previousRemainingHits)
        {
            for (var index = 0; index < monster.Runtime.WeakPoints.Count; index++)
            {
                if (monster.Runtime.WeakPoints[index] != point) continue;
                if (index < monster.WeakPointViews.Count)
                    monster.WeakPointViews[index]?.PlayChargeHit(previousRemainingHits, point.RemainingHits);
                return;
            }
        }

        private static void HoldWeakPointViewsForImpact(ActiveMonster monster)
        {
            monster.WeakPointVisualHoldUntil = Mathf.Max(monster.WeakPointVisualHoldUntil, Time.unscaledTime + .18f);
        }

        private void ApplyOpeningRockDamage(float damage)
        {
            if (damage <= 0f || InfiniteBattleEnabled) return;
            foreach (var monster in activeMonsters)
            {
                var appliedDamage = Mathf.Min(damage, Mathf.Max(0f, monster.Runtime.CurrentHp - 1f));
                if (appliedDamage <= 0f) continue;
                monster.Runtime.TakeDamage(appliedDamage);
                monster.View.Bind(monster.Runtime);
                ShowDamage(monster.Position, $"-{appliedDamage:0}", new Color(.72f, .72f, .72f));
            }
        }

        private static AttackSegment ExtendForAttackReach(AttackSegment segment, float attackReach)
        {
            if (attackReach <= 0f) return segment;
            var direction = segment.End - segment.Start;
            if (direction.sqrMagnitude <= Mathf.Epsilon) return segment;
            return new AttackSegment(segment.Start, segment.End + direction.normalized * attackReach, segment.Width);
        }

        private static Vector2 Direction(AttackSegment segment)
        {
            var delta = segment.End - segment.Start;
            return delta.sqrMagnitude <= Mathf.Epsilon ? Vector2.up : delta.normalized;
        }

        private static TriggerAttackType TriggerAttackTypeFor(bool charged) => charged ? TriggerAttackType.Charge : TriggerAttackType.Normal;

        private static TriggerAttackWay GetAttackWay(AttackSegment segment)
        {
            var direction = Direction(segment);
            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)) return TriggerAttackWay.Horizontal;
            return direction.y >= 0f ? TriggerAttackWay.Upward : TriggerAttackWay.Downward;
        }

        private static AttackSegment RotateAtMidpoint(AttackSegment segment, float angle)
        {
            var direction = Direction(segment);
            var radians = angle * Mathf.Deg2Rad;
            var rotated = new Vector2(direction.x * Mathf.Cos(radians) - direction.y * Mathf.Sin(radians), direction.x * Mathf.Sin(radians) + direction.y * Mathf.Cos(radians));
            var midpoint = (segment.Start + segment.End) * .5f;
            var halfLength = (segment.End - segment.Start).magnitude * .5f;
            return new AttackSegment(midpoint - rotated * halfLength, midpoint + rotated * halfLength, segment.Width);
        }

        private static Vector2 ClosestPointOnSegment(AttackSegment segment, Vector2 target)
        {
            var delta = segment.End - segment.Start;
            var lengthSquared = delta.sqrMagnitude;
            if (lengthSquared <= Mathf.Epsilon) return segment.End;
            var t = Mathf.Clamp01(Vector2.Dot(target - segment.Start, delta) / lengthSquared);
            return segment.Start + delta * t;
        }

        private void Finish(bool playerWon)
        {
            if (!IsActive || isFinishing) return;
            var result = new CombatResult(playerWon, !Player.IsAlive, activeMonsters.Select(monster => monster.Runtime.Data).ToArray());
            IsActive = false;
            Player?.SetKeyboardGuard(false);
            Player?.SetChargeGuard(false);
            isFinishing = true;
            if (playerWon && activeMonsters.Any(monster => monster.View != null))
            {
                // Preserve the final slash and weak-point burst long enough for the hit to register.
                attackLine?.KeepVisibleFor(FinalImpactHoldSeconds);
                StartCoroutine(FinishAfterMonsterFade(result));
                return;
            }
            if (attackLine != null) attackLine.Hide();
            CompleteFinish(result);
        }

        private IEnumerator FinishAfterMonsterFade(CombatResult result)
        {
            yield return new WaitForSecondsRealtime(FinalImpactHoldSeconds);
            if (attackLine != null) attackLine.Hide();
            foreach (var monster in activeMonsters)
                if (monster.View != null) StartCoroutine(monster.View.FadeOut(MonsterFadeSeconds));
            yield return new WaitForSecondsRealtime(MonsterFadeSeconds);
            CompleteFinish(result);
        }

        private void CompleteFinish(CombatResult result)
        {
            ClearMonsterViews();
            RefreshHud();
            isFinishing = false;
            Completed?.Invoke(result);
        }
    }
}
