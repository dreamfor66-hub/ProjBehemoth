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

        private sealed class ActiveMonster
        {
            public MonsterRuntime Runtime { get; }
            public MonsterView View { get; }
            public Vector2 Position { get; }
            public List<WeakPointView> WeakPointViews { get; } = new();

            public ActiveMonster(MonsterRuntime runtime, MonsterView view, Vector2 position)
            {
                Runtime = runtime;
                View = view;
                Position = position;
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
        public bool HasPreparedMonsters => activeMonsters.Any(monster => monster.Runtime.IsAlive);
        public PlayerCombatRuntime Player => run?.Player;
        public MonsterData MimicMonster => mimicMonster;
        public event Action<CombatResult> Completed;

        public void ShowPlayer(RunState runState)
        {
            run = runState;
            EnsurePlayerView();
            playerView.Root.anchoredPosition = Vector2.zero;
            playerView.Bind(Player);
            RefreshHud();
        }

        public void Configure(CombatSceneView newSceneView, CombatInputController newInput, CombatHudView newHud, PlayerCombatData newPlayerData, List<MonsterData> newFloorOneMonsters, List<MonsterData> newFloorTwoMonsters, List<MonsterData> newFloorThreeMonsters, List<MonsterData> newEliteMonsters, List<MonsterData> newBosses, MonsterData newMimic, Transform newPlayerRoot, Transform newMonsterRoot, Transform newWeakPointRoot, Transform newWeakPointBreakFxRoot, PlayerView newPlayerPrefab, MonsterView newMonsterPrefab, WeakPointView newWeakPointPrefab, WeakPointBreakFxView newWeakPointBreakFxPrefab, AttackLineView newAttackLine, Transform newDamageNumberRoot, DamageNumberView newDamageNumberPrefab)
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
            run?.Player?.ResetAttackCooldown();
            var combatStartEffect = run?.BeginCombat() ?? default;
            ClearMonsterViews();
            isFinishing = false;
            IsActive = false;
            EnsurePlayerView();
            playerView.Root.anchoredPosition = Vector2.zero;

            var validMonsters = encounterMonsters?.Where(data => data != null).ToArray() ?? Array.Empty<MonsterData>();
            for (var index = 0; index < validMonsters.Length; index++)
            {
                var position = sceneView.GetMonsterFormationPosition(index, validMonsters.Length);
                // Floor identity and balance now live in each monster asset; never double-scale them here.
                var runtime = new MonsterRuntime(validMonsters[index]);
                var view = Instantiate(monsterPrefab, monsterSpawnRoot);
                view.Root.anchoredPosition = position - sceneView.MonsterPosition;
                view.ResetOpacity();
                view.Bind(runtime);
                activeMonsters.Add(new ActiveMonster(runtime, view, position));
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
                return boss == null ? Array.Empty<MonsterData>() : new[] { boss };
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
            var chargeGuard = Player != null && Player.ChargeGuardEnabled && input != null && input.IsChargingAction;
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
            foreach (var monster in activeMonsters.Where(monster => monster.Runtime.IsAlive))
            {
                var runtime = monster.Runtime;
                if (runtime.ShouldCharge && runtime.Data.chargePatterns.Count > 0)
                {
                    runtime.StartCharge(monster.Position);
                    monster.View.ShowAttackName(runtime.Data.chargeAttack.displayName, runtime.StateTimer);
                    continue;
                }
                if (runtime.ShouldNormalAttack)
                {
                    runtime.BeginNormalAttack();
                    monster.View.ShowAttackName(runtime.Data.normalAttack.displayName, runtime.Data.normalAttack.windupDuration);
                    continue;
                }
                if (runtime.IsNormalAttackReadyToHit)
                {
                    var attack = runtime.Data.normalAttack;
                    var result = damageResolver.ApplyToPlayer(Player, new DamageRequest(DamageType.MonsterAttack, attack.damage * runtime.DamageMultiplier, attack.shieldDamage * runtime.DamageMultiplier, attack.guardable));
                    ShowPlayerDamage(result);
                    ResolveDamageTriggers(result, monster);
                    runtime.CompleteNormalAttack();
                    if (!IsActive) return;
                    if (result.TargetDied && !TryRevivePlayer()) { Finish(false); return; }
                }
                if (runtime.ChargeExpired)
                {
                    var attack = runtime.Data.chargeAttack;
                    var result = damageResolver.ApplyToPlayer(Player, new DamageRequest(DamageType.MonsterAttack, attack.damage * runtime.DamageMultiplier, attack.shieldDamage * runtime.DamageMultiplier, attack.guardable));
                    ShowPlayerDamage(result);
                    ResolveDamageTriggers(result, monster);
                    runtime.EndCharge();
                    ClearWeakPointViews(monster);
                    if (!IsActive) return;
                    if (result.TargetDied && !TryRevivePlayer()) { Finish(false); return; }
                }
            }
        }

        public bool TryBeginGesture(Vector2 start)
        {
            if (!IsActive || !Player.IsAlive) return false;
            Player.TrySetGuard(false);
            return true;
        }

        public bool IsGuardGesture(Vector2 pressPosition, Vector2 releasePosition) => IsActive && GuardGestureResolver.IsPlayerDownwardGuard(sceneView.PlayerPosition, pressPosition, releasePosition, Player.GuardSwipeDistance);

        public bool TryBeginGuard(Vector2 pressPosition, Vector2 releasePosition)
        {
            if (!IsGuardGesture(pressPosition, releasePosition) || !activeMonsters.Any(monster => monster.Runtime.IsAlive && !monster.Runtime.IsStunned)) return false;
            return Player.TrySetGuard(true);
        }

        public void ExecuteAttack(AttackSegment segment, bool charged) => ExecuteAttack(segment, charged, 1);

        public void ExecuteAttack(AttackSegment segment, bool charged, int chargeLevel)
        {
            if (!IsActive || !Player.IsAlive || (!charged && !Player.CanNormalAttack())) return;
            if (!charged) Player.StartAttackCooldown();
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
                        if (!point.ApplyChargeHit()) continue;
                        Player.RestoreShield(Player.ShieldOnWeakPoint);
                        if (point.IsDestroyed) ShowWeakPointBreak(point.HitShape.Center);
                    }
                    if (runtime.AllWeakPointsDestroyed)
                    {
                        runtime.EnterStun();
                        ClearWeakPointViews(monster);
                    }
                }
                if (SegmentHitResolver.Intersects(segment, new CircleHitShape(monster.Position, runtime.Data.bodyHitRadius)))
                {
                    hitAny = true;
                    var baseDamage = (charged ? Player.ChargeAttackDamage : Player.NormalAttackDamage) * damageMultiplier * GetDirectAttackDamageMultiplier(segment, isDirectAttack);
                    var dealtDamage = runtime.IsStunned ? baseDamage * Player.StunDamageMultiplier : baseDamage;
                    damageResolver.ApplyToMonster(runtime, baseDamage, Player.StunDamageMultiplier);
                    // Update immediately: the final hit must visibly empty the HP gauge before its impact hold.
                    monster.View?.Bind(runtime);
                    if (!runtime.IsAlive && Player.MonsterKillHeal > 0f) Player.Heal(Player.MonsterKillHeal);
                    Player.RestoreShield(Player.ShieldOnHit);
                    ShowDamage(ClosestPointOnSegment(segment, monster.Position), $"{dealtDamage:0}", charged ? new Color(1f, .78f, .16f) : Color.white);
                    ResolveEquipmentTriggers(TriggerType.OnHit, BuildAttackContext(segment, attackType, charged, monster), monster, chainDepth);
                }

            }
            return hitAny;
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
            damageResolver.ApplyToMonster(target.Runtime, baseDamage, Player.StunDamageMultiplier);
            target.View?.Bind(target.Runtime);
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

        private void ClearWeakPointViews(ActiveMonster monster)
        {
            foreach (var view in monster.WeakPointViews)
                if (view != null) Destroy(view.gameObject);
            monster.WeakPointViews.Clear();
        }

        private void ClearMonsterViews()
        {
            foreach (var monster in activeMonsters)
            {
                ClearWeakPointViews(monster);
                if (monster.View != null) Destroy(monster.View.gameObject);
            }
            activeMonsters.Clear();
        }

        private void EnsurePlayerView()
        {
            if (playerView == null) playerView = Instantiate(playerPrefab, playerSpawnRoot);
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

        private void ShowWeakPointBreak(Vector2 position)
        {
            if (weakPointBreakFxRoot == null || weakPointBreakFxPrefab == null) return;
            Instantiate(weakPointBreakFxPrefab, weakPointBreakFxRoot).Play(position, FinalImpactHoldSeconds);
        }

        private void ApplyOpeningRockDamage(float damage)
        {
            if (damage <= 0f) return;
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
