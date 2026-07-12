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
        [SerializeField] private MonsterData eliteMonster;
        [SerializeField] private MonsterData bossMonster;
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

        public bool IsActive { get; private set; }
        public PlayerCombatRuntime Player => run?.Player;
        public event Action<CombatResult> Completed;

        public void ShowPlayer(RunState runState)
        {
            run = runState;
            EnsurePlayerView();
            playerView.Root.anchoredPosition = Vector2.zero;
            playerView.Bind(Player);
            RefreshHud();
        }

        public void Configure(CombatSceneView newSceneView, CombatInputController newInput, CombatHudView newHud, PlayerCombatData newPlayerData, List<MonsterData> newNormalMonsters, MonsterData newElite, MonsterData newBoss, Transform newPlayerRoot, Transform newMonsterRoot, Transform newWeakPointRoot, Transform newWeakPointBreakFxRoot, PlayerView newPlayerPrefab, MonsterView newMonsterPrefab, WeakPointView newWeakPointPrefab, WeakPointBreakFxView newWeakPointBreakFxPrefab, AttackLineView newAttackLine, Transform newDamageNumberRoot, DamageNumberView newDamageNumberPrefab)
        {
            sceneView = newSceneView;
            input = newInput;
            hud = newHud;
            playerData = newPlayerData;
            normalMonsters = newNormalMonsters;
            eliteMonster = newElite;
            bossMonster = newBoss;
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
            PrepareEncounter(runState, GetEncounterMonsters(roomType, roomId));
            BeginPreparedCombat();
        }

        public void PrepareEncounter(RunState runState, RoomEncounterType roomType, int roomId) => PrepareEncounter(runState, GetEncounterMonsters(roomType, roomId));

        /// <summary>Entry point for future room definitions that spawn a group rather than one monster.</summary>
        public void PrepareEncounter(RunState runState, IReadOnlyList<MonsterData> encounterMonsters)
        {
            run = runState;
            run?.Player?.ResetAttackCooldown();
            ClearMonsterViews();
            isFinishing = false;
            IsActive = false;
            EnsurePlayerView();
            playerView.Root.anchoredPosition = Vector2.zero;

            var validMonsters = encounterMonsters?.Where(data => data != null).ToArray() ?? Array.Empty<MonsterData>();
            for (var index = 0; index < validMonsters.Length; index++)
            {
                var position = sceneView.GetMonsterFormationPosition(index, validMonsters.Length);
                var runtime = new MonsterRuntime(validMonsters[index]);
                var view = Instantiate(monsterPrefab, monsterSpawnRoot);
                view.Root.anchoredPosition = position - sceneView.MonsterPosition;
                view.ResetOpacity();
                view.Bind(runtime);
                activeMonsters.Add(new ActiveMonster(runtime, view, position));
            }

            if (attackLine != null) attackLine.Hide();
            RefreshHud();
        }

        public void BeginPreparedCombat()
        {
            if (!activeMonsters.Any(monster => monster.Runtime.IsAlive)) return;
            IsActive = true;
            RefreshHud();
        }

        public MonsterData GetMonsterData(RoomEncounterType roomType, int roomId) => GetEncounterMonsters(roomType, roomId).FirstOrDefault();

        private IReadOnlyList<MonsterData> GetEncounterMonsters(RoomEncounterType roomType, int roomId)
        {
            if (roomType == RoomEncounterType.Boss) return bossMonster == null ? Array.Empty<MonsterData>() : new[] { bossMonster };
            if (roomType == RoomEncounterType.Elite) return eliteMonster == null ? Array.Empty<MonsterData>() : new[] { eliteMonster };
            var data = normalMonsters.Count == 0 ? null : normalMonsters[Mathf.Abs(roomId) % normalMonsters.Count];
            return data == null ? Array.Empty<MonsterData>() : new[] { data };
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
                    var result = damageResolver.ApplyToPlayer(Player, new DamageRequest(DamageType.MonsterAttack, attack.damage, attack.shieldDamage, attack.guardable));
                    ShowPlayerDamage(result);
                    runtime.CompleteNormalAttack();
                    if (result.TargetDied) { Finish(false); return; }
                }
                if (runtime.ChargeExpired)
                {
                    var attack = runtime.Data.chargeAttack;
                    var result = damageResolver.ApplyToPlayer(Player, new DamageRequest(DamageType.MonsterAttack, attack.damage, attack.shieldDamage, attack.guardable));
                    ShowPlayerDamage(result);
                    runtime.EndCharge();
                    ClearWeakPointViews(monster);
                    if (result.TargetDied) { Finish(false); return; }
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

        public void ExecuteAttack(AttackSegment segment, bool charged)
        {
            if (!IsActive || !Player.IsAlive || (!charged && !Player.CanNormalAttack())) return;
            if (!charged) Player.StartAttackCooldown();
            if (attackLine != null) attackLine.Show(segment, charged);

            foreach (var monster in activeMonsters.Where(monster => monster.Runtime.IsAlive))
            {
                var runtime = monster.Runtime;
                if (SegmentHitResolver.Intersects(segment, new CircleHitShape(monster.Position, runtime.Data.bodyHitRadius)))
                {
                    var baseDamage = charged ? Player.ChargeAttackDamage : Player.NormalAttackDamage;
                    var dealtDamage = runtime.IsStunned ? baseDamage * Player.StunDamageMultiplier : baseDamage;
                    damageResolver.ApplyToMonster(runtime, baseDamage, Player.StunDamageMultiplier);
                    // Update immediately: the final hit must visibly empty the HP gauge before its impact hold.
                    monster.View?.Bind(runtime);
                    Player.RestoreShield(Player.ShieldOnHit);
                    ShowDamage(ClosestPointOnSegment(segment, monster.Position), $"{dealtDamage:0}", charged ? new Color(1f, .78f, .16f) : Color.white);
                }

                if (!charged || runtime.State != MonsterState.Charging) continue;
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

            if (activeMonsters.Count > 0 && activeMonsters.All(monster => !monster.Runtime.IsAlive)) Finish(true);
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
            var origin = input == null ? Vector2.zero : input.ChargeOrigin;
            hud.Bind(Player, null, gesture, progress, origin);
        }

        private void ShowPlayerDamage(DamageResult result)
        {
            if (result.HpDamage > 0f) ShowDamage(sceneView.PlayerPosition, $"-{result.HpDamage:0}", new Color(1f, .32f, .32f));
            else if (result.ShieldDamage > 0f) ShowDamage(sceneView.PlayerPosition, $"-{result.ShieldDamage:0}", new Color(.3f, 1f, .7f));
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
