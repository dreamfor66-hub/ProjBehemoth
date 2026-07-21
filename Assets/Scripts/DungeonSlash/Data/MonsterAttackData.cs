using System.Collections.Generic;
using UnityEngine;

namespace DungeonSlash
{
    public enum MonsterAttackType { Normal, Charge }

    [CreateAssetMenu(menuName = "Dungeon Slash/Monster Attack Data")]
    public sealed class MonsterAttackData : ScriptableObject
    {
        public MonsterAttackType type = MonsterAttackType.Normal;
        [Tooltip("Leave empty when this attack should not announce a name.")]
        public string displayName = "";
        [Min(0f)] public float damage = 12f;
        [Min(0f)] public float shieldDamage = 24f;
        [Tooltip("Normal attacks only: telegraph time. A Charge attack's telegraph is chargeTimeLimit.")]
        [Min(.01f)] public float windupDuration = .75f;
        [Min(.01f)] public float recoveryDuration = .5f;
        public bool guardable = true;

        [Header("Charge")]
        [Tooltip("Only used by Charge attacks. This is the weak-point exposure window.")]
        [Min(.1f)] public float chargeTimeLimit = 2.5f;
        public List<WeakPointDefinition> chargeWeakPoints = new();

        [Header("Mechanics")]
        public MonsterCombatMechanics combatMechanics = new();
        public ChargeSummonMechanics chargeSummonMechanics = new();
    }
}
