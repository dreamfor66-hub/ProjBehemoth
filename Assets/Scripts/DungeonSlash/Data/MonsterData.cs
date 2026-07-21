using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonSlash
{
    [Serializable]
    public sealed class DirectionalGuardData
    {
        public bool enabled;
        [Tooltip("Only these slash directions damage the guard while it is active.")]
        public TriggerAttackWayFilter breakByAttackWay = TriggerAttackWayFilter.Horizontal;
        [Min(1f)] public float maxGuard = 72f;
        [Min(0f)] public float rebuildDelay = 5f;
        [Min(.1f)] public float rebuildPerSecond = 24f;
    }

    [Serializable]
    public sealed class MonsterCombatMechanics
    {
        public DirectionalGuardData directionalGuard = new();
        [Tooltip("HP restored only when this attack deals unguarded HP damage.")]
        [Min(0f)] public float healOnUnguardedHit;
        [Min(1)] public int hitCount = 1;
        [Min(.01f)] public float followupDelay = .25f;
    }

    [Serializable]
    public sealed class ChargeSummonMechanics
    {
        [Tooltip("Minions that join this monster when this charge-capable monster enters combat.")]
        public List<MonsterData> openingSummons = new();
        [Tooltip("Summoned when this charge resolves and none of this minion remains.")]
        public MonsterData summonOnFailedCharge;
        [Tooltip("HP restored instead when a matching summoned minion is still alive.")]
        [Min(0f)] public float healOnFailedChargeWhileSummonAlive;
    }

    [CreateAssetMenu(menuName = "Dungeon Slash/Monster Data")]
    public sealed class MonsterData : ScriptableObject
    {
        public string displayName = "Skeleton";
        [Min(1f)] public float maxHp = 80f;
        [Min(.1f)] public float normalAttackInterval = 2f;
        [Min(.1f)] public float chargeInterval = 6f;
        [Min(.1f)] public float stunDuration = 3f;
        [Min(1f)] public float stunDamageMultiplier = 1.5f;
        [Min(0)] public int experienceReward = 18;
        [Min(0)] public int goldReward = 10;
        [Min(10f)] public float bodyHitRadius = 115f;
        [Tooltip("Every action this monster can perform. AttackData owns its timing-specific mechanics.")]
        public List<MonsterAttackData> attacks = new();
        public Color bodyColor = new(.78f, .3f, .3f);

        public MonsterAttackData GetAttack(MonsterAttackType type) => attacks?.FirstOrDefault(attack => attack != null && attack.type == type);
        public IEnumerable<MonsterAttackData> GetAttacks(MonsterAttackType type) => attacks?.Where(attack => attack != null && attack.type == type) ?? Enumerable.Empty<MonsterAttackData>();
    }
}
