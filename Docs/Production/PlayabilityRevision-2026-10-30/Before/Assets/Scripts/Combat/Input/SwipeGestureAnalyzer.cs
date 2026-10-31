using System.Collections.Generic;
using UnityEngine;

namespace DungeonSlash
{
    public readonly struct SwipeGesture
    {
        public Vector2 TrailStart { get; }
        public Vector2 Position { get; }
        public Vector2 Direction { get; }
        public float Distance { get; }
        public float Speed { get; }
        public float DirectionalConsistency { get; }

        public SwipeGesture(Vector2 trailStart, Vector2 position, Vector2 direction, float distance, float speed, float directionalConsistency)
        {
            TrailStart = trailStart;
            Position = position;
            Direction = direction;
            Distance = distance;
            Speed = speed;
            DirectionalConsistency = directionalConsistency;
        }

        public AttackSegment ToAttackSegment(float width) => new(TrailStart, Position, width);
    }

    public sealed class SwipeGestureAnalyzer
    {
        private readonly List<PointerSample> samples = new();

        public void Reset(Vector2 position, float time)
        {
            samples.Clear();
            samples.Add(new PointerSample(position, time));
        }

        public void AddSample(Vector2 position, float time, float sampleWindowSeconds)
        {
            samples.Add(new PointerSample(position, time));
            var cutoff = time - Mathf.Max(.02f, sampleWindowSeconds);
            while (samples.Count > 2 && samples[1].Time < cutoff)
                samples.RemoveAt(0);
        }

        public bool TryGetGesture(float minimumDistance, float minimumSpeed, float minimumDirectionalConsistency, out SwipeGesture gesture)
        {
            gesture = default;
            if (samples.Count < 2)
                return false;

            var last = samples[^1];
            var totalTravel = 0f;
            var weightedDirection = Vector2.zero;
            for (var index = 1; index < samples.Count; index++)
            {
                var delta = samples[index].Position - samples[index - 1].Position;
                var deltaDistance = delta.magnitude;
                if (deltaDistance <= Mathf.Epsilon)
                    continue;

                totalTravel += deltaDistance;
                var deltaTime = Mathf.Max(.001f, samples[index].Time - samples[index - 1].Time);
                weightedDirection += delta / deltaDistance * (deltaDistance / deltaTime);
            }

            if (totalTravel < minimumDistance || weightedDirection.sqrMagnitude <= Mathf.Epsilon)
                return false;

            var direction = weightedDirection.normalized;
            var alignedTravel = 0f;
            for (var index = 1; index < samples.Count; index++)
            {
                var delta = samples[index].Position - samples[index - 1].Position;
                var deltaDistance = delta.magnitude;
                if (deltaDistance > Mathf.Epsilon)
                    alignedTravel += deltaDistance * Mathf.Max(-1f, Vector2.Dot(delta / deltaDistance, direction));
            }

            var elapsed = Mathf.Max(.001f, last.Time - samples[0].Time);
            var speed = totalTravel / elapsed;
            var consistency = alignedTravel / totalTravel;
            if (speed < minimumSpeed || consistency < minimumDirectionalConsistency)
                return false;

            gesture = new SwipeGesture(samples[0].Position, last.Position, direction, totalTravel, speed, consistency);
            return true;
        }

        private readonly struct PointerSample
        {
            public Vector2 Position { get; }
            public float Time { get; }

            public PointerSample(Vector2 position, float time)
            {
                Position = position;
                Time = time;
            }
        }
    }
}
