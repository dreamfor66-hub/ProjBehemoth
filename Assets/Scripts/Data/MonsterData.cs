using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonSlash
{
    public enum MonsterPattern { Standard = 0, MirrorCounter = 1, BloodFury = 2, LinkedHeart = 4, Summoner = 5 }
    [Serializable]
    public sealed class MonsterHitBox
    {
        public Vector2 center;
        [Min(1)] public float radius = 100;
    }
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
    public sealed class MonsterData : LocalizedData
    {
        public string displayName = "Skeleton";
        public Sprite portrait;
        public CombatSpriteAnimation combatAnimation;
        [TextArea] public string encounterHint;
        public MonsterPattern pattern;
        public string supportId;
        public bool diesWithLeader;
        [Tooltip("Support slots, also used for opening summons. Repeat entries to author multiple copies.")]
        public List<MonsterData> supportRoster = new();
        [Min(0), Tooltip("Seconds to restore one missing support independently of charges. Zero disables; stun pauses the timer.")]
        public float reinforcementInterval;
        public bool useOrbit;
        public float orbitRadius = 170f;
        public float orbitSpeed = .55f;
        [Min(1f)] public float maxHp = 80f;
        [Min(.1f)] public float normalAttackInterval = 2f;
        [Min(.1f)] public float chargeInterval = 6f;
        [Min(.1f)] public float stunDuration = 3f;
        [Min(1f)] public float stunDamageMultiplier = 1.5f;
        [Min(0)] public int experienceReward = 18;
        [Min(0)] public int goldReward = 10;
        [Min(.1f)] public float combatScale = 1f;
        [Tooltip("Actor-local circular collision shapes. The lowest edge determines the footprint automatically.")]
        public List<MonsterHitBox> hitBoxes = new() { new MonsterHitBox() };
        public Rect HitBoxBounds
        {
            get
            {
                var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                foreach (var box in hitBoxes ?? Enumerable.Empty<MonsterHitBox>())
                {
                    if (box == null) continue;
                    var extent = Vector2.one * Mathf.Max(0, box.radius);
                    min = Vector2.Min(min, box.center - extent); max = Vector2.Max(max, box.center + extent);
                }
                return float.IsInfinity(min.x) ? new Rect() : Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            }
        }
        public float FootprintY => HitBoxBounds.yMin;
        public Vector2 BodySize => HitBoxBounds.size;
        public Sprite ReferenceArtwork => combatAnimation != null && combatAnimation.idle != null ? combatAnimation.idle : portrait;
        public const float ArtworkUnitsPerWorldUnit = 100f;
        public Rect ArtworkBounds => ReferenceArtwork != null ? ArtworkBoundsOf(ReferenceArtwork) : HitBoxBounds;
        public static Rect ArtworkBoundsOf(Sprite sprite)
        {
            var min = Vector2.one * float.PositiveInfinity; var max = Vector2.one * float.NegativeInfinity;
            foreach (var vertex in sprite.vertices) { min = Vector2.Min(min, vertex); max = Vector2.Max(max, vertex); }
            return Rect.MinMaxRect(min.x * ArtworkUnitsPerWorldUnit, min.y * ArtworkUnitsPerWorldUnit,
                max.x * ArtworkUnitsPerWorldUnit, max.y * ArtworkUnitsPerWorldUnit);
        }
        // Default formations place the artwork's ground line; saved encounters place the physical footprint.
        // Both are derived from the native Sprite geometry and hitboxes, without an editable correction.
        public float FormationFootprintOffset => combatAnimation != null && combatAnimation.tightPoseFrames
            ? (FootprintY - ArtworkBounds.yMin) * Mathf.Max(.1f, combatScale) : 0;
        /// <summary>Convert the physical hitbox footprint position into the actor's local origin.</summary>
        public Vector2 BodyCenterFromPlacement(Vector2 placement, float visualScale = 1f) =>
            placement - Vector2.up * (FootprintY * Mathf.Max(.1f, visualScale));
        public IEnumerable<CircleHitShape> BodyShapes(Vector2 center, float scale = 1f)
        {
            foreach (var box in hitBoxes ?? Enumerable.Empty<MonsterHitBox>())
                if (box != null) yield return new CircleHitShape(center + box.center * scale, box.radius * scale);
        }
        public bool IntersectsBody(AttackSegment segment, Vector2 center, float scale = 1f) => BodyShapes(center, scale).Any(shape => SegmentHitResolver.Intersects(segment, shape));
        public bool OverlapsArea(Vector2 area, float radius, Vector2 center, float scale = 1f) => IntersectsBody(new AttackSegment(area, area, radius * 2), center, scale);
        [Tooltip("Every action this monster can perform. AttackData owns its timing-specific mechanics.")]
        public List<MonsterAttackData> attacks = new();
        [Tooltip("Optional ordered normal actions. Empty uses the first Normal in attacks. Each completed action advances the sequence; interrupted actions keep their place.")]
        public List<MonsterAttackData> normalAttackSequence = new();

        public MonsterAttackData GetAttack(MonsterAttackType type) => attacks?.FirstOrDefault(attack => attack != null && attack.type == type);
        public IEnumerable<MonsterAttackData> GetAttacks(MonsterAttackType type) => attacks?.Where(attack => attack != null && attack.type == type) ?? Enumerable.Empty<MonsterAttackData>();
    }
}
