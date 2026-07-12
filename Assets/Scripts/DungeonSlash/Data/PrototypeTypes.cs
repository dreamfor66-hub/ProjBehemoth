using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonSlash
{
    [Serializable]
    public sealed class WeakPointDefinition
    {
        public string id = "weak-point";
        public Vector2 normalizedPosition;
        [Min(1f)] public float hitRadius = 30f;
        [Min(1)] public int requiredChargeHits = 1;
    }

    public enum ModifierKind { NormalDamage, ChargeDamage, AttackCooldownMultiplier, ChargeDurationMultiplier, MaxHp, Heal, ShieldMax, NormalShieldRegen, BrokenShieldRegen, ShieldOnHit, ShieldOnWeakPoint, StunDamageMultiplier, Gold }

    [Serializable]
    public struct StatModifier { public ModifierKind kind; public float value; }

    public enum RoomEncounterType { Start, Combat, Reward, Fountain, Goddess, Shop, Elite, MajorReward, Boss, Empty }
}
