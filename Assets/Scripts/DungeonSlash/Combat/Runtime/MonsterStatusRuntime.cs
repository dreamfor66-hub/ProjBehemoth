using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace DungeonSlash
{
    public sealed partial class MonsterRuntime
    {
        public int PoisonStacks { get; private set; }
        public float PoisonRemaining { get; private set; }
        public float SlowRemaining { get; private set; }
        public float LightningRodUntil { get; set; }
        public bool HasTakenDirectHit { get; set; }
        public float LastPoisonDamage { get; private set; }
        public HashSet<string> LivingSupports { get; } = new();
        public SwipeDirection? LastDirectDirection { get; set; }
        public float CounterAt { get; set; } = -1f;
        public int FuryHits { get; set; }
        public bool HeartsBroken { get; set; }
        public float ConsumePoisonDamage() { var damage = LastPoisonDamage; LastPoisonDamage = 0; return damage; }
        private float poisonTick;
        private float poisonPerStack = 3f;
        private float burnRemaining, burnDamage, burnTick;
        private IEnumerable<MonsterAttackData> AllowedChargeAttacks() => Data == null ? Enumerable.Empty<MonsterAttackData>() : Data.GetAttacks(MonsterAttackType.Charge)
            .Where(attack => string.IsNullOrEmpty(attack.requiredSupportId) || LivingSupports.Contains(attack.requiredSupportId));
        public void AddPoison(int stacks, float damage = 3f, float duration = 8f, int cap = 60)
        {
            if (!IsAlive) return;
            PoisonStacks = Mathf.Min(cap, PoisonStacks + Mathf.Max(0, stacks));
            poisonPerStack = Mathf.Max(.01f, damage); PoisonRemaining = Mathf.Max(.1f, duration);
        }
        public float DetonatePoison(float multiplier = 2.5f)
        {
            var damage = PoisonStacks * poisonPerStack * multiplier;
            PoisonStacks = 0; PoisonRemaining = poisonTick = 0; return damage;
        }
        public void Slow(float seconds) => SlowRemaining = Mathf.Max(SlowRemaining, seconds);
        public void AddBurn(float damage, float duration) { burnDamage = damage; burnRemaining = duration; }
        public void Hasten(float seconds) { NormalAttackTimer = Mathf.Max(.25f, NormalAttackTimer - seconds); ChargeTimer = Mathf.Max(.25f, ChargeTimer - seconds); }
        public void Interrupt(float seconds)
        {
            if (!IsAlive || IsStunned) return;
            if (State == MonsterState.Attacking) StateTimer += seconds;
            else if (State == MonsterState.Idle) NormalAttackTimer += seconds;
        }
        private void TickStatus(float dt)
        {
            LastPoisonDamage = 0; SlowRemaining = Mathf.Max(0, SlowRemaining - dt);
            if (burnRemaining > 0) { burnTick += Mathf.Min(dt, burnRemaining); burnRemaining = Mathf.Max(0, burnRemaining - dt); while (burnTick >= 1 && IsAlive) { burnTick--; TakeDamage(burnDamage); } }
            if (PoisonStacks <= 0) return;
            var activeDt = Mathf.Min(dt, PoisonRemaining);
            PoisonRemaining = Mathf.Max(0, PoisonRemaining - dt); poisonTick += activeDt;
            while (poisonTick >= 2f && IsAlive)
            {
                poisonTick -= 2f; var damage = PoisonStacks * poisonPerStack;
                TakeDamage(damage); LastPoisonDamage += damage;
            }
            if (PoisonRemaining <= 0) { PoisonStacks = 0; poisonTick = 0; }
        }
        public void MoveWeakPoints(Vector2 delta) { foreach (var point in weakPoints) point.Move(delta); }
    }
}
