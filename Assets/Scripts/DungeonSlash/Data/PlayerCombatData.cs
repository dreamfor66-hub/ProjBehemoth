using UnityEngine;
namespace DungeonSlash
{
    [CreateAssetMenu(menuName = "Dungeon Slash/Player Combat Data")]
    public sealed class PlayerCombatData : ScriptableObject
    {
        [Header("Health and Shield")]
        [Min(1f)] public float maxHp = 100f;
        [Min(0f)] public float shieldMax = 100f;
        [Min(0f)] public float normalShieldRegen = 8f;
        [Min(0f)] public float brokenShieldRegen = 22f;

        [Header("Attacks")]
        [Min(0f)] public float baseAttackDamage = 12f;
        [Min(0f)] public float baseChargeDamage = 28f;
        [Min(.01f)] public float attackCooldown = .5f;
        [Min(.01f)] public float minimumAttackCooldown = .15f;
        [Min(.01f)] public float chargeDuration = .75f;
        [Min(0f)] public float inputMoveTolerance = 24f;
        [Min(0f)] public float attackSegmentWidth = 18f;
        [Min(1f)] public float guardSwipeDistance = 64f;

        [Header("Swipe Recognition")]
        [Min(.02f)] public float swipeSampleWindowSeconds = .12f;
        [Min(1f)] public float minimumSwipeDistance = 68f;
        [Min(1f)] public float minimumSwipeSpeed = 520f;
        [Range(0f, 1f)] public float minimumDirectionConsistency = .8f;
    }
}
