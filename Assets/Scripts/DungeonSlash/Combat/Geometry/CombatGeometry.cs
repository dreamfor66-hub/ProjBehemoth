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

    public static class WeakPointHitResolver
    {
        public static IReadOnlyList<WeakPointRuntime> Resolve(in AttackSegment segment, IReadOnlyList<WeakPointRuntime> points)
        {
            var hits = new List<WeakPointRuntime>();
            foreach (var point in points)
            {
                if (point.IsActive && SegmentHitResolver.Intersects(segment, point.HitShape))
                    hits.Add(point);
            }

            return hits;
        }
    }
}
