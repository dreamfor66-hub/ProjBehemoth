using System;
using UnityEngine;
namespace DungeonSlash
{
    [Serializable] public sealed class CharacterMechanicSettings
    {
        public float ragePerNormalAttack = 12, rageThreshold = 40, rageDrainPerSecond = 20, rageIdleDecay = 2;
        public float enragedDamageMultiplier = 1.5f, enragedCooldownMultiplier = .62f, enragedDamageReduction = .3f;
        public float drawDamageMultiplier = 1.5f, sheathParrySeconds = .1f;
        public float wizardCooldownMultiplier = .5f, castingGuardRegen;
    }
}
