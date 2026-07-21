using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonSlash
{
    public readonly struct AttackSegment
    {
        public Vector2 Start { get; }
        public Vector2 End { get; }
        public float Width { get; }

        public AttackSegment(Vector2 start, Vector2 end, float width)
        {
            Start = start;
            End = end;
            Width = Mathf.Max(0f, width);
        }
    }

    public readonly struct CircleHitShape
    {
        public Vector2 Center { get; }
        public float Radius { get; }

        public CircleHitShape(Vector2 center, float radius)
        {
            Center = center;
            Radius = Mathf.Max(0f, radius);
        }
    }

    public readonly struct RectHitShape
    {
        public Vector2 Center { get; }
        public Vector2 Size { get; }

        public RectHitShape(Vector2 center, Vector2 size)
        {
            Center = center;
            Size = new Vector2(Mathf.Max(0f, size.x), Mathf.Max(0f, size.y));
        }
    }

    public static class SegmentHitResolver
    {
        public static bool Intersects(in AttackSegment segment, in CircleHitShape circle)
        {
            var radius = circle.Radius + segment.Width * .5f;
            return DistanceSquaredToSegment(circle.Center, segment.Start, segment.End) <= radius * radius;
        }

        public static float DistanceSquaredToSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            var direction = end - start;
            var lengthSquared = direction.sqrMagnitude;
            if (lengthSquared <= Mathf.Epsilon)
                return (point - start).sqrMagnitude;

            var t = Mathf.Clamp01(Vector2.Dot(point - start, direction) / lengthSquared);
            return (point - (start + direction * t)).sqrMagnitude;
        }
    }

    public static class RectHitResolver
    {
        public static bool Contains(in RectHitShape shape, Vector2 point, float padding = 0f)
        {
            var halfSize = shape.Size * .5f + Vector2.one * Mathf.Max(0f, padding);
            var localPoint = point - shape.Center;
            return Mathf.Abs(localPoint.x) <= halfSize.x && Mathf.Abs(localPoint.y) <= halfSize.y;
        }

        public static bool Intersects(in AttackSegment segment, in RectHitShape shape, float padding = 0f)
        {
            var halfSize = shape.Size * .5f + Vector2.one * Mathf.Max(0f, padding);
            var minimum = shape.Center - halfSize;
            var maximum = shape.Center + halfSize;
            if (Contains(shape, segment.Start, padding) || Contains(shape, segment.End, padding)) return true;

            var direction = segment.End - segment.Start;
            var enter = 0f;
            var exit = 1f;
            return IntersectsAxis(segment.Start.x, direction.x, minimum.x, maximum.x, ref enter, ref exit)
                && IntersectsAxis(segment.Start.y, direction.y, minimum.y, maximum.y, ref enter, ref exit);
        }

        private static bool IntersectsAxis(float start, float delta, float minimum, float maximum, ref float enter, ref float exit)
        {
            if (Mathf.Abs(delta) <= Mathf.Epsilon) return start >= minimum && start <= maximum;
            var inverseDelta = 1f / delta;
            var first = (minimum - start) * inverseDelta;
            var second = (maximum - start) * inverseDelta;
            if (first > second) (first, second) = (second, first);
            enter = Mathf.Max(enter, first);
            exit = Mathf.Min(exit, second);
            return enter <= exit;
        }
    }

    public static class TargetTraversalResolver
    {
        public static bool Contains(in CircleHitShape target, Vector2 point, float attackWidth)
        {
            var radius = target.Radius + Mathf.Max(0f, attackWidth) * .5f;
            return (point - target.Center).sqrMagnitude <= radius * radius;
        }

        public static bool Touches(in AttackSegment movement, in CircleHitShape target) =>
            SegmentHitResolver.Intersects(movement, target);

        public static float OutsideDistance(in CircleHitShape target, Vector2 point, float attackWidth)
        {
            var boundaryRadius = target.Radius + Mathf.Max(0f, attackWidth) * .5f;
            return Mathf.Max(0f, Vector2.Distance(point, target.Center) - boundaryRadius);
        }
    }

    /// <summary>Confirms a slash only after it has crossed a target and then exited it.</summary>
    public sealed class TargetTraversalConfirmation
    {
        private float stationarySince = -1f;
        public bool HasTouchedTarget { get; private set; }
        public bool HasExitedTarget { get; private set; }

        public void Reset()
        {
            HasTouchedTarget = false;
            HasExitedTarget = false;
            stationarySince = -1f;
        }

        public void BeginInsideTarget()
        {
            HasTouchedTarget = true;
            HasExitedTarget = false;
            stationarySince = -1f;
        }

        public bool Observe(bool pathTouchesTarget, bool wasInsideTarget, bool isInsideTarget, float outsideDistance, float pointerMovement, float time, float exitDistance, float stationaryDistance, float stopConfirmSeconds)
        {
            if (pathTouchesTarget) HasTouchedTarget = true;
            if (!HasTouchedTarget) return false;

            if (isInsideTarget)
            {
                HasExitedTarget = false;
                return ConfirmAfterStopping(pointerMovement, time, stationaryDistance, stopConfirmSeconds);
            }

            if (!HasExitedTarget && (wasInsideTarget || pathTouchesTarget))
                HasExitedTarget = true;
            if (!HasExitedTarget) return false;

            if (outsideDistance >= Mathf.Max(0f, exitDistance)) return true;
            return ConfirmAfterStopping(pointerMovement, time, stationaryDistance, stopConfirmSeconds);
        }

        private bool ConfirmAfterStopping(float pointerMovement, float time, float stationaryDistance, float stopConfirmSeconds)
        {
            if (pointerMovement > Mathf.Max(0f, stationaryDistance))
            {
                stationarySince = -1f;
                return false;
            }

            if (stationarySince < 0f) stationarySince = time;
            return time - stationarySince >= Mathf.Max(0f, stopConfirmSeconds);
        }
    }

    public static class WeakPointHitResolver
    {
        // Weak points are a precision objective, but should forgive a slash that only just grazes the lock-on.
        // This is deliberately separate from body collision and does not alter the displayed slash or debug cast.
        public const float HitForgivenessRadius = 12f;

        public static IReadOnlyList<WeakPointRuntime> Resolve(in AttackSegment segment, IReadOnlyList<WeakPointRuntime> points)
        {
            var hits = new List<WeakPointRuntime>();
            foreach (var point in points)
            {
                var forgivingHitShape = new CircleHitShape(point.HitShape.Center, point.HitShape.Radius + HitForgivenessRadius);
                if (point.IsActive && SegmentHitResolver.Intersects(segment, forgivingHitShape))
                    hits.Add(point);
            }

            return hits;
        }
    }
}
