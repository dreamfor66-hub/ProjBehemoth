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

    public sealed partial class CombatController : MonoBehaviour
    {
        private const float FinalImpactHoldSeconds = .5f;
        private const float MonsterFadeSeconds = .45f;

        private sealed class ActiveMonster
        {
            public MonsterRuntime Runtime { get; }
            public MonsterView View { get; }
            public Vector2 Position { get; set; }
            public Vector2 BasePosition { get; }
            public float VisualScale { get; }
            public List<WeakPointView> WeakPointViews { get; } = new();
            public float WeakPointVisualHoldUntil { get; set; }

            public ActiveMonster(MonsterRuntime runtime, MonsterView view, Vector2 position, float visualScale)
            {
                Runtime = runtime;
                View = view;
                Position = position;
                BasePosition = position;
                VisualScale = Mathf.Max(.1f, visualScale);
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
        private bool resolvingAttackSequence;
        public CombatPressureFeedback pressureFeedback;
        private int playerAttackVersion;
        private bool playerActionOpen;
        public bool SetManualGuard(bool held)
        {
            if (Player == null) return false;
            var requested=held&&!Player.HoldingGuard;
            var accepted=Player.TrySetGuard(held);
            if (requested)
            {
                playerAttackVersion++;
                attackLine?.Hide(); playerView?.InterruptAttack();
                if (playerActionOpen) CompletePlayerAttack(true);
            }
            return accepted;
        }
        private int encounterFloor = 1;
        private int trackedFloor = -1;
        private int normalEncountersOnFloor;
        private bool showedConcealed;

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

        public void ResetEncounter()
        {
            reinforcementTimers.Clear();
            StopAllCoroutines(); resolvingAttackSequence=false; IsActive = false; isFinishing = false; InputPaused = false;
            input?.CancelAll(); run?.Player?.EndCharacterCombat(); ClearMonsterViews(); attackLine?.Hide(); sceneView?.ResetCameraShake();
            pendingEncounter = null; encounterPlanner = null; trackedFloor = -1; normalEncountersOnFloor = 0;
            playerView?.SetCombatFraming(sceneView.PlayerAnchor, false, true);
        }

        public void ShowPlayer(RunState runState)
        {
            if (run != runState) { encounterPlanner = null; trackedFloor = -1; normalEncountersOnFloor = 0; }
            run = runState;
            EnsurePlayerView();
            playerView.SetCombatFraming(sceneView.PlayerAnchor, false);
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
            if (run != runState) { run = runState; encounterPlanner = null; }
            EnsurePlanner();
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
            if (run.CurrentAct != encounterFloor) { run.CurrentAct = encounterFloor; run.NormalCombatsInAct = 0; }
            sceneView?.ResetCameraShake();
            run?.Player?.ResetAttackCooldown();
            var combatStartEffect = run?.BeginCombat() ?? default;
            showedConcealed = run?.Awareness.IsConcealed == true;
            ClearMonsterViews();
            playerActionOpen=false;playerAttackVersion++;
            pressureFeedback?.ResetFeedback();
            reportedKills.Clear();
            reinforcementTimers.Clear();
            isFinishing = false;
            IsActive = false;
            lastStaffTarget = null;
            spellRandom = new System.Random(runState.Seed ^ ++encounterSerial);
            criticalRandom = new System.Random(runState.Seed ^ encounterSerial*104729);
            EnsurePlayerView();
            playerView.Bind(Player);

            var members = encounterMonsters?.Where(data => data != null).ToList() ?? new List<MonsterData>();
            var leader = members.FirstOrDefault();
            foreach (var slots in GetOpeningSummons(leader).GroupBy(data=>data))
                for(var count=members.Count(data=>data==slots.Key);count<slots.Count();count++) members.Add(slots.Key);
            var validMonsters = members.ToArray();
            var encounterLeader = validMonsters.FirstOrDefault();
            EncounterHint = pendingEncounter != null ? pendingEncounter.hint : encounterLeader != null ? encounterLeader.encounterHint : "";
            for (var index = 0; index < validMonsters.Length; index++)
            {
                var isOpeningSummon = index > 0 && encounterLeader != null && GetOpeningSummons(encounterLeader).Contains(validMonsters[index]);
                var actionDelay = isOpeningSummon ? .45f + (index - 1) * .75f : 0f;
                var member = pendingEncounter != null && index < pendingEncounter.members.Count && pendingEncounter.members[index].monster == validMonsters[index] ? pendingEncounter.members[index] : null;
                var placement = member != null ? sceneView.MonsterPosition + member.offset
                    : (leader != null && leader.supportRoster.Count > 0
                        ? index==0?sceneView.MonsterPosition:sceneView.GetMonsterFormationPosition(index%2==1?0:2,3)
                        : sceneView.GetMonsterFormationPosition(index, validMonsters.Length)) + Vector2.up * validMonsters[index].FormationFootprintOffset;
                SpawnMonster(validMonsters[index], placement, member != null ? member.actionDelay : actionDelay);
            }
            pendingEncounter = null;

            ApplyOpeningRockDamage(combatStartEffect.OpeningRockDamage);

            if (attackLine != null) attackLine.Hide();
            RefreshHud();
        }

        // Presentation can run beneath the encounter announcement while input/AI stay stopped.
        public void BeginCombatPresentation()
        {
            if (!HasPreparedMonsters) return;
            playerView.SetCombatFraming(sceneView.PlayerAnchor, true);
        }

        public bool IsCombatPresentationReady => playerView != null && playerView.IsCombatFramingReady;

        public void BeginPreparedCombat()
        {
            if (!activeMonsters.Any(monster => monster.Runtime.IsAlive)) return;
            IsActive = true;
            BeginCombatPresentation();
            RefreshHud();
        }

        public MonsterData GetMonsterData(RoomEncounterType roomType, int roomId, int floor = 1) => GetEncounterMonsters(roomType, roomId, floor).FirstOrDefault();

        private IReadOnlyList<MonsterData> GetEncounterMonsters(RoomEncounterType roomType, int roomId, int floor)
        {
            EnsurePlanner();
            if (encounterPlanner != null)
            {
                var normalCount = trackedFloor == floor ? normalEncountersOnFloor : 0;
                var planned = encounterPlanner.Get(floor, roomType, roomId, normalCount, run.Level);
                if (planned != null) { pendingEncounter = planned; return planned.members.Select(member => member.monster).ToArray(); }
            }
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
            if (monster != null && monster.supportRoster != null && monster.supportRoster.Count > 0)
                return monster.supportRoster.Where(support => support != null);
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
            if (playerView != null)
            {
                playerView.PreparingAttack = IsActive && !InputPaused && input != null && input.IsPreparingAttack;
                playerView.ShowLegacyRecovery = hud == null || hud.attackRecovery == null;
                playerView.PreparingSwing = input!=null?input.PreparingSwing:SwordSwingDirection.RightToLeft;
                playerView.Bind(Player);
            }
            if (InputPaused) return;
            if (!IsActive)
            {
                RefreshHud();
                return;
            }

            var deltaTime = Time.deltaTime;
            var chargeGuard = Player != null && Player.ChargeGuardEnabled && input != null && input.IsChargingAction;
            Player.SetChargeGuard(chargeGuard);
            Player.Tick(deltaTime);
            run.TickSpells(deltaTime);
            if (!Player.IsAlive && !TryRevivePlayer()) { Finish(false); return; }
            foreach (var monster in activeMonsters.Where(monster => monster.Runtime.IsAlive))
                monster.Runtime.Tick(deltaTime, monster.Position);
            TickExpandedCombat(deltaTime);
            if (!IsActive) return;
            TickReinforcements(deltaTime);
            if (!Player.IsAlive && !TryRevivePlayer()) { Finish(false); return; }

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
            return SetManualGuard(true);
        }

        public bool TryBeginRestGuard(Vector2 pressPosition, Vector2 holdingPosition)
        {
            if (!IsActive ||
                !IsRestGuardCandidate(pressPosition, holdingPosition) ||
                !activeMonsters.Any(monster => monster.Runtime.IsAlive && !monster.Runtime.IsStunned))
                return false;
            return SetManualGuard(true);
        }

        public bool IsRestGuardCandidate(Vector2 pressPosition, Vector2 holdingPosition)
        {
            return IsActive && GuardGestureResolver.IsPlayerRestGuardCandidate(GetPlayerBodyShape(), pressPosition, holdingPosition, Player.GuardSwipeDistance);
        }

        public bool IsDownwardGuardGesture(Vector2 start,Vector2 current)=>GuardGestureResolver.IsPlayerDownwardGuard(GetPlayerBodyShape(),start,current,Player.GuardSwipeDistance,0);

        public bool IsPointerInsidePlayerBody(Vector2 position) => RectHitResolver.Contains(GetPlayerBodyShape(), position);

        public bool TryBeginBodyEntryGuard(Vector2 pressPosition, Vector2 holdingPosition)
        {
            if (!IsActive ||
                !IsPointerInsidePlayerBody(holdingPosition) ||
                !GuardGestureResolver.IsDownwardApproach(pressPosition, holdingPosition) ||
                !activeMonsters.Any(monster => monster.Runtime.IsAlive && !monster.Runtime.IsStunned))
                return false;
            return SetManualGuard(true);
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
                if (!monster.Runtime.Data.IntersectsBody(movement, monster.Position, monster.VisualScale)) continue;
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
            var width = Player?.SegmentWidth ?? 0f;
            outsideDistance = float.PositiveInfinity;
            foreach (var target in TraversalShapes(monster))
            {
                insideTarget |= TargetTraversalResolver.Contains(target, position, width);
                outsideDistance = Mathf.Min(outsideDistance, TargetTraversalResolver.OutsideDistance(target, position, width));
            }
            return true;
        }

        public bool DoesTraversalTargetTouch(int targetIndex, Vector2 from, Vector2 to)
        {
            if (targetIndex < 0 || targetIndex >= activeMonsters.Count) return false;
            var monster = activeMonsters[targetIndex];
            if (!monster.Runtime.IsAlive) return false;
            var segment = new AttackSegment(from, to, Player?.SegmentWidth ?? 0f);
            return TraversalShapes(monster).Any(target => TargetTraversalResolver.Touches(segment, target));
        }
        private static System.Collections.Generic.IEnumerable<CircleHitShape> TraversalShapes(ActiveMonster monster)
        {
            foreach (var shape in monster.Runtime.Data.BodyShapes(monster.Position, monster.VisualScale)) yield return shape;
            foreach (var point in monster.Runtime.WeakPoints.Where(point => point.IsActive && !point.IsDestroyed))
                yield return new CircleHitShape(point.HitShape.Center, point.HitShape.Radius + WeakPointHitResolver.HitForgivenessRadius);
        }

        public int FarthestTraversalTarget(Vector2 from, Vector2 to, Vector2 origin, Vector2 direction)
        {
            var result = -1; var distance = float.NegativeInfinity;
            for (var i = 0; i < activeMonsters.Count; i++)
            {
                if (!DoesTraversalTargetTouch(i, from, to)) continue;
                var projection = Vector2.Dot(activeMonsters[i].Position - origin, direction);
                if (projection > distance) { result = i; distance = projection; }
            }
            return result;
        }
        public bool HasTraversalTargetAhead(int crossedIndex, Vector2 position, Vector2 direction)
        {
            if (direction.sqrMagnitude < .5f) return false;
            var ray = new AttackSegment(position, position + direction * 650f, Player.SegmentWidth);
            for (var i = 0; i < activeMonsters.Count; i++)
            {
                var target = activeMonsters[i];
                if (i == crossedIndex || !target.Runtime.IsAlive) continue;
                if (TraversalShapes(target).Any(shape => Vector2.Dot(shape.Center - position, direction) > 0f && TargetTraversalResolver.Touches(ray, shape))) return true;
            }
            return false;
        }

        public void ExecuteAttack(AttackSegment segment, bool charged) => ExecuteAttack(segment, charged, 1);

        public void ExecuteAttack(AttackSegment segment, bool charged, int chargeLevel) => ExecuteAttack(segment,charged,chargeLevel,null);

        public void ExecuteAttack(AttackSegment segment, bool charged, int chargeLevel,SwordSwingDirection? visualSwing)
        {
            if (!IsActive || InputPaused || resolvingAttackSequence || !Player.IsAlive || !Player.CanNormalAttack()) return;
            // Charge attacks use their own input preparation, but still consume the shared follow-up attack cooldown.
            input?.ResetCasting();
            lastStaffDistance = float.MaxValue;
            segment = ExtendForAttackReach(segment, Player.AttackReach);
            if (attackLine != null) attackLine.Show(segment, charged);
            currentAttack = Player.PrepareAttack(ResolveDirection(segment.End - segment.Start), charged, chargeLevel);
            playerActionOpen=true;
            if (currentAttack.Draw)
            {
                var range = 1 + Mathf.Max(0, Player.Rule(AbilityRule.DrawReachBonus));
                segment = new AttackSegment(segment.Start, segment.Start + (segment.End - segment.Start) * range, segment.Width * range);
            }
            Player.StartAttackCooldown();
            var swing=visualSwing??(segment.Start.x<sceneView.LogicalArea.rect.center.x?SwordSwingDirection.LeftToRight:SwordSwingDirection.RightToLeft);
            playerView?.PlayAttack(swing);
            if (currentAttack.AttackBuff != null) ShowDamage(sceneView.PlayerPosition + new Vector2(0, 120), currentAttack.AttackBuff.displayName, new Color(.55f, 1f, .7f));
            if (currentAttack.Draw) ShowDamage(sceneView.PlayerPosition + new Vector2(0, 120), GameText.Get(currentAttack.FeedbackKey ?? "ui.combatcontroller.9b76b68d"), new Color(1f, .65f, .8f));
            if(currentAttack.Hits>1) { StartCoroutine(ExecuteAttackSequence(segment,currentAttack,swing,playerAttackVersion)); return; }
            ExecutePlayerAttack(segment, currentAttack.Charged, currentAttack.Multiplier, TriggerAttackTypeFor(currentAttack.Charged), true, true, 0);
            CompletePlayerAttack();
        }

        private IEnumerator ExecuteAttackSequence(AttackSegment segment, CharacterAttack attack,SwordSwingDirection swing,int version)
        {
            resolvingAttackSequence=true;
            try
            {
                for(var hit=0;hit<attack.Hits;hit++)
                {
                    if(hit>0)
                    {
                        var elapsed=0f;
                        while(elapsed<attack.HitInterval && IsActive && Player.IsAlive && version==playerAttackVersion)
                        { yield return null; if(!InputPaused)elapsed+=Time.deltaTime; }
                        while(InputPaused && IsActive)yield return null;
                    }
                    if(!IsActive || !Player.IsAlive || Player.HoldingGuard || version!=playerAttackVersion)yield break;
                    currentAttack=attack;
                    if(hit==0)ExecutePlayerAttack(segment,attack.Charged,attack.Multiplier,TriggerAttackTypeFor(attack.Charged),true,true,0);
                    else
                    {
                        attackLine?.Show(segment,attack.Charged);
                        playerView?.PlayAttack(swing);
                        ApplyPlayerAttack(segment,attack.Charged,attack.Multiplier,TriggerAttackTypeFor(attack.Charged),true,true,0);
                    }
                    if(activeMonsters.All(m=>!m.Runtime.IsAlive))break;
                }
                if(IsActive && version==playerAttackVersion)CompletePlayerAttack();
            }
            finally { resolvingAttackSequence=false; }
        }

        private void CompletePlayerAttack(bool interrupted=false)
        {
            if (!playerActionOpen) return;
            playerActionOpen=false;
            if(!interrupted){run.EmitEvent(TriggerType.OnAttackResolved, run.ActionContext); ResolveQueuedAbilities(0);}
            run.CompleteAction(TriggerType.OnAttack, run.ActionContext);
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
                        var applied = false;
                        applied = point.ApplyChargeHit();
                        if (!applied) continue;
                        ShowWeakPointChargeHit(monster, point, previousRemainingHits);
                        Player.RestoreShield(Player.ShieldOnWeakPoint);
                        if (point.IsDestroyed) ShowWeakPointBreak(monster,point);
                        if (point.IsDestroyed) ResolveEquipmentTriggers(TriggerType.OnWeakPointBreak, BuildAttackContext(segment, attackType, charged, monster), monster, chainDepth);
                        ShakeCombat(point.IsDestroyed ? 12f : 6f, point.IsDestroyed ? .16f : .1f);
                    }
                    if (runtime.AllWeakPointsDestroyed)
                    {
                        runtime.EnterStun();
                        HoldWeakPointViewsForImpact(monster);
                    }
                }
            if (runtime.Data.IntersectsBody(segment, monster.Position, monster.VisualScale))
                {
                    var baseDamage = (charged ? Player.ChargeAttackDamage : Player.NormalAttackDamage) * damageMultiplier * GetDirectAttackDamageMultiplier(segment, isDirectAttack);
                    if (isDirectAttack) baseDamage = ModifyDirectDamage(monster, segment, charged, baseDamage);
                    if (!(isDirectAttack && charged && Player.Rule(AbilityRule.IgnoreDirectionalGuard) > 0) && runtime.TryAbsorbDirectionalGuard(GetAttackWay(segment), baseDamage, out var absorbedDamage, out var matchedDirection, out var guardBroken))
                    {
                        runtime.NotifyAttacked();
                        monster.View?.Bind(runtime);
                        var impact = ClosestPointOnSegment(segment, monster.Position);
                        if (matchedDirection)
                            ShowDamage(impact, GameText.Format("ui.combatcontroller.12ab6e58", absorbedDamage), new Color(.28f, .86f, 1f));
                        else
                            ShowDamage(impact, GameText.Get("ui.combatcontroller.c0f8301f"), new Color(.62f, .72f, .82f));
                        if (guardBroken)
                            ShowDamage(monster.Position + new Vector2(0f, 42f), GameText.Get("ui.combatcontroller.8e353679"), new Color(.48f, 1f, 1f));
                        ShakeCombat(guardBroken ? 5f : 2.5f, .07f);
                        continue;
                    }

                    var hitContext=BuildAttackContext(segment,attackType,charged,monster,baseDamage);
                    var hitPacket=new ActionEffectContext();
                    hitContext=new EquipmentTriggerContext(hitContext.Segment,hitContext.AttackType,hitContext.AttackWay,hitContext.OwnerCharging,hitContext.EnemyStunned,runtime,attackBuff:hitContext.AttackBuff,damage:baseDamage,draw:hitContext.Draw,enemyCount:hitContext.EnemyCount,mutable:hitPacket,actionTags:currentAttack.Tags);
                    var hitEffects=run.BeginHit(TriggerType.OnHit,hitContext);
                    runtime.NotifyAttacked();
                    baseDamage*=Mathf.Max(0,1+hitPacket.DamageBonus);
                    hitAny = true;
                    ShakeCombat(charged ? 8f : 3.5f, charged ? .13f : .08f);
                    var hitResult = damageResolver.ApplyToMonster(runtime,baseDamage,Player,criticalRandom,!InfiniteBattleEnabled);
                    var dealtDamage = hitResult.Damage;
                    // Update immediately: the final hit must visibly empty the HP gauge before its impact hold.
                    monster.View?.Bind(runtime);
                    if (!runtime.IsAlive && Player.MonsterKillHeal > 0f) Player.Heal(Player.MonsterKillHeal);
                    Player.RestoreShield(Player.ShieldOnHit);
                    if (!InfiniteBattleEnabled)
                        ShowHitDamage(ClosestPointOnSegment(segment, monster.Position), dealtDamage, hitResult.IsCritical);
                    run.FinishHit(hitEffects,dealtDamage);ResolveQueuedAbilities(chainDepth);
                    if (!runtime.IsAlive) ReportKill(runtime, BuildAttackContext(segment, attackType, charged, monster, dealtDamage));
                    if (isDirectAttack) OnExpandedDirectHit(monster, segment, charged, dealtDamage);
                }

            }
            return hitAny;
        }

        private DamageResult ApplyMonsterAttack(ActiveMonster source, MonsterAttackData attack)
        {
            var runtime = source.Runtime;
            if (attack == null) return default;
            source.View?.PlayAttack(attack.type==MonsterAttackType.Charge?.5f:.4f);
            var rawDamage = InfiniteBattleEnabled ? 0f : attack.damage * runtime.DamageMultiplier;
            var result = damageResolver.ApplyToPlayer(Player, new DamageRequest(DamageType.MonsterAttack, rawDamage, attack.shieldDamage * runtime.DamageMultiplier, attack.guardable, runtime));
            ShowPlayerDamage(result);
            if (result.HpDamage > 0f)
                ShakeCombat(8f, .14f);
            else if (result.ShieldDamage > 0f)
                ShakeCombat(result.WasGuarded ? 3f : 4.5f, .09f);
            ResolveDamageTriggers(result, source);
            if (result.WasPerfectGuard)
            {
                ShowDamage(sceneView.PlayerPosition, GameText.Get("ui.combatcontroller.bf583bce"), new Color(1f, .9f, .35f));
            }
            if (result.WasSheathParry)
            {
                ShowDamage(sceneView.PlayerPosition, GameText.Get("ui.combatcontroller.f4a2f0dc"), new Color(1f, .9f, .35f));
            }
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
                    source.View?.ShowAttackName(GameText.Get("ui.combatcontroller.e55ce145"), .8f);
                    ShowDamage(source.Position, $"+{healed:0}", new Color(.38f, 1f, .5f));
                }
                return;
            }

            var existingSummons = activeMonsters.Count(monster => monster.Runtime.IsAlive && monster.Runtime.Data == summon);
            var direction = existingSummons % 2 == 0 ? -1f : 1f;
            var position = source.Position + Vector2.up * source.Runtime.Data.FootprintY * source.VisualScale + new Vector2(direction * 300f, -16f);
            SpawnMonster(summon, position, .45f).View?.ShowAttackName(GameText.Get("ui.combatcontroller.bdcc2933"), .7f);
            source.View?.ShowAttackName(GameText.Get("ui.combatcontroller.f6ad320"), .8f);
        }

        private void ResolveDamageTriggers(DamageResult result, ActiveMonster source)
        {
            if (run == null) return;
            ResolveQueuedAbilities(0);
            if (activeMonsters.Count > 0 && activeMonsters.All(monster => !monster.Runtime.IsAlive)) Finish(true);
        }

        private void ResolveEquipmentTriggers(TriggerType triggerType, EquipmentTriggerContext context, ActiveMonster source, int chainDepth)
        {
            if (run == null || chainDepth > 8) return;
            run.EmitEvent(triggerType, context); ResolveQueuedAbilities(chainDepth);
        }

        private void ApplyEquipmentDamage(ActiveMonster target, EquipmentEffect effect)
        {
            var baseDamage = Player.NormalAttackDamage * Mathf.Max(0f, effect.effectMagnitude) * Mathf.Max(1, effect.effectCount);
            var result=damageResolver.ApplyToMonster(target.Runtime,baseDamage,Player,criticalRandom,!InfiniteBattleEnabled);
            target.View?.Bind(target.Runtime);
            if (!InfiniteBattleEnabled)
                ShowHitDamage(target.Position,result.Damage,result.IsCritical);
        }

        private EquipmentTriggerContext BuildAttackContext(AttackSegment segment, TriggerAttackType attackType, bool charged, ActiveMonster target, float damage = 0)
        {
            return new EquipmentTriggerContext(segment, attackType, GetAttackWay(segment), charged || (input != null && input.IsChargingAction), target != null && target.Runtime.IsStunned, target?.Runtime, attackBuff: currentAttack.AttackBuff, damage: damage, draw: currentAttack.Draw, enemyCount: activeMonsters.Count(item => item.Runtime.IsAlive),actionTags:currentAttack.Tags);
        }

        private float GetDirectAttackDamageMultiplier(AttackSegment segment, bool isDirectAttack)
        {
            return !isDirectAttack ? 1f : 1f + (segment.End - segment.Start).magnitude * Player.DamagePerAttackDistance;
        }

        private void SyncWeakPointViews()
        {
            if(weakPointRoot!=null)weakPointRoot.SetAsLastSibling();
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

        private ActiveMonster SpawnMonster(MonsterData data, Vector2 position, float initialActionDelay = 0f)
        {
            // Floor identity and balance now live in each monster asset; never double-scale them here.
            var runtime = new MonsterRuntime(data, visualScale: sceneView.GetMonsterScale(data));
            position = data.BodyCenterFromPlacement(sceneView.FitMonsterPlacement(data,position), runtime.VisualScale);
            runtime.Awareness = run.Awareness;
            runtime.Buffs.Run=run;
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
            playerView.gameObject.SetActive(true);
        }

        private RectHitShape GetPlayerBodyShape()
        {
            var root = playerView != null ? playerView.Root : playerPrefab != null ? playerPrefab.Root : null;
            var size = playerView!=null?playerView.GuardBodySize:root != null ? root.rect.size : new Vector2(270f, 270f);
            var center = sceneView.PlayerPosition + (playerView != null ? playerView.GuardBodyOffset : Vector2.zero);
            // The foreground character can be cropped by the screen. Guard gestures must
            // cross the visible body center, rather than an unreachable point below it.
            var area = sceneView.LogicalArea.rect;
            var min = Vector2.Max(center - size * .5f, area.min);
            var max = Vector2.Min(center + size * .5f, area.max);
            return new RectHitShape((min + max) * .5f, Vector2.Max(Vector2.zero, max - min));
        }

        private void RefreshHud()
        {
            RefreshCompanions();
            if (run != null)
            {
                if (showedConcealed && !run.Awareness.IsConcealed && IsActive)
                    ShowDamage(sceneView.PlayerPosition + Vector2.up * 130, GameText.Get("ui.stealth.revealed"), new Color(1f, .7f, .35f));
                showedConcealed = run.Awareness.IsConcealed;
            }
            if (hud == null || Player == null) return;
            var gesture = input == null ? PointerGestureState.None : input.State;
            var progress = input == null ? 0f : input.ChargeProgress;
            var secondProgress = input == null ? 0f : input.SecondChargeProgress;
            var origin = input == null ? Vector2.zero : input.ChargeOrigin;
            hud.Bind(IsActive ? Player : null, null, gesture, progress, origin, secondProgress);
        }

        private void ShowPlayerDamage(DamageResult result)
        {
            if (result.HpDamage > 0f) ShowDamage(sceneView.PlayerPosition, $"-{result.HpDamage:0}", new Color(1f, .32f, .32f));
            else if (result.ShieldDamage > 0f) ShowDamage(sceneView.PlayerPosition, $"-{result.ShieldDamage:0}", new Color(.3f, 1f, .7f));
        }

        private bool TryRevivePlayer()
        {
            if (run == null || !run.TryConsumeRevival()) return false;
            ShowDamage(sceneView.PlayerPosition, GameText.Get("ui.combatcontroller.fd5c20f9"), new Color(.42f, 1f, .65f));
            return true;
        }

        private void ShowDamage(Vector2 position, string message, Color color)
        {
            if (damageNumberRoot == null || damageNumberPrefab == null) return;
            var view = Instantiate(damageNumberPrefab, damageNumberRoot);
            view.Show(position, message, color);
        }

        private void ShowHitDamage(Vector2 position,float damage,bool critical)
        {
            if(damageNumberRoot==null||damageNumberPrefab==null)return;
            var view=Instantiate(damageNumberPrefab,damageNumberRoot);
            view.ShowHit(position,damage,critical);
            if(critical)sceneView?.TriggerCriticalFlash();
        }

        private void ShakeCombat(float magnitude, float duration)
        {
            sceneView?.TriggerCameraShake(magnitude, duration);
        }

        private void ShowWeakPointBreak(ActiveMonster monster,WeakPointRuntime point)
        {
            if (weakPointBreakFxRoot == null || weakPointBreakFxPrefab == null) return;
            var index=-1;for(var i=0;i<monster.Runtime.WeakPoints.Count;i++)if(monster.Runtime.WeakPoints[i]==point){index=i;break;}
            var source=index>=0&&index<monster.WeakPointViews.Count?monster.WeakPointViews[index]?.reticle:null;
            Instantiate(weakPointBreakFxPrefab, weakPointBreakFxRoot).Play(point.HitShape.Center, FinalImpactHoldSeconds,source);
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
                var appliedDamage = Mathf.Min(damage*(run.Fatigue.FinalDamageMultiplier), Mathf.Max(0f, monster.Runtime.CurrentHp - 1f));
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
            Player?.EndCharacterCombat();
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
            if (result.PlayerDied) PresentDefeat();
            else
            {
                playerView?.SetCombatFraming(sceneView.PlayerAnchor, false);
                ClearMonsterViews();
            }
            RefreshHud();
            isFinishing = false;
            Completed?.Invoke(result);
        }

        /// <summary>Keep the final enemy composition and authored pose beneath the death overlay.</summary>
        public void PresentDefeat()
        {
            IsActive = false;
            StopAllCoroutines();
            resolvingAttackSequence = false;
            input?.CancelAll();
            if (playerView != null) playerView.gameObject.SetActive(false);
        }
    }
}
