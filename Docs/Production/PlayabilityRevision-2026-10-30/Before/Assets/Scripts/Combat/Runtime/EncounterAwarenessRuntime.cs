using System;
using UnityEngine;

namespace DungeonSlash
{
    /// <summary>Shared by the encounter: one hit alerts every member, including opening summons.</summary>
    public sealed class EncounterAwarenessRuntime
    {
        public bool IsConcealed { get; private set; }
        public bool IsActive { get; private set; }
        public int CompletedActions { get; private set; }
        public float DetectionStep { get; private set; }
        public float NextDetectionChance => Mathf.Clamp01((CompletedActions + 1) * DetectionStep);
        public event Action Revealed;

        public void Begin(bool concealed, float detectionStep)
        {
            IsConcealed = concealed;
            IsActive = true;
            CompletedActions = 0;
            DetectionStep = Mathf.Clamp01(detectionStep);
        }

        // The action has already resolved. Holding a charge/cast never enters this method.
        public void CompleteAction(float sample)
        {
            if (!IsActive) return;
            CompletedActions++;
            if (!IsConcealed) return;
            if (sample < Mathf.Clamp01(CompletedActions * DetectionStep) || CompletedActions * DetectionStep >= 1f) Reveal();
        }

        public void Reveal()
        {
            if (!IsConcealed) return;
            IsConcealed = false;
            Revealed?.Invoke();
        }

        public void End() { IsConcealed = false; IsActive = false; }
    }
}
