using UnityEngine;
namespace DungeonSlash
{
    [CreateAssetMenu(menuName = "Dungeon Slash/Game Variables")]
    public sealed class GameVariables : ScriptableObject
    {
        [Header("Attack feel")]
        [Min(.01f)] public float minimumAttackCooldown = .2f;
        [Min(0)] public float inputMoveTolerance = 24f;
        [Min(1)] public float attackSegmentWidth = 18f;
        [Min(1)] public float guardSwipeDistance = 70f;
        [Min(0)] public float guardBodyOuterMargin = 70f;
        [Min(.01f)] public float guardRestConfirmSeconds = .1f;
        [Min(0)] public float guardRestStationaryDistance = 1f;
        [Header("Traversal confirmation")]
        [Min(1)] public float traversalExitDistance = 48f;
        [Min(.01f)] public float traversalStopConfirmSeconds = .1f;
        [Min(0)] public float traversalStationaryDistance = 1f;
        [Header("Swipe recognition")]
        [Min(.02f)] public float swipeSampleWindowSeconds = .12f;
        [Min(1)] public float minimumSwipeDistance = 84f;
        [Min(1)] public float minimumSwipeSpeed = 520f;
        [Range(0, 1)] public float minimumDirectionConsistency = .8f;
        [Min(20)] public float spellSwipeDistance = 65f;
        public float perfectGuardSeconds = .14f;
        private static GameVariables fallback;
        public static GameVariables Default
        {
            get
            {
                if (fallback == null) { fallback = CreateInstance<GameVariables>(); fallback.hideFlags = HideFlags.HideAndDontSave; }
                return fallback;
            }
        }
    }
}
