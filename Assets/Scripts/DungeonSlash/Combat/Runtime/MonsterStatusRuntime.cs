using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace DungeonSlash
{
    public sealed partial class MonsterRuntime
    {
        private BuffManager buffs;
        public BuffManager Buffs => buffs ??= new BuffManager(monster: this);
        private BuffInstance Status(StatusKind kind) { var data = CombatStatusCatalog.Instance?.Get(kind); return data != null ? Buffs.Find(data, data.DuplicationData.ConsiderTagIsDuplicate) : null; }
        public int PoisonStacks => Status(StatusKind.Poison)?.StackCount ?? 0;
        public float PoisonRemaining => Remaining(StatusKind.Poison);
        public float SlowRemaining => Remaining(StatusKind.Slow);
        public float LightningRodUntil { get => Remaining(StatusKind.LightningRod) > 0 ? (Buffs.Run?.Player.CombatTime ?? 0)+Remaining(StatusKind.LightningRod) : 0; set => ApplyStatus(StatusKind.LightningRod, Mathf.Max(.01f, value-(Buffs.Run?.Player.CombatTime ?? 0))); }
        public bool HasTakenDirectHit { get; set; }
        public float LastPoisonDamage { get; private set; }
        public HashSet<string> LivingSupports { get; } = new();
        public SwipeDirection? LastDirectDirection { get; set; }
        public float CounterAt { get; set; } = -1f;
        public int FuryHits { get; set; }
        public bool HeartsBroken { get; set; }
        private float Remaining(StatusKind kind) { var buff = Status(kind); return buff == null ? 0 : Mathf.Max(0, buff.Duration - buff.ElapsedTime + (buff.Data.ReleaseData.ResetOnStack ? buff.TimeLastStack : 0)); }
        internal void RecordStatusDamage(BuffData data, float damage) { if (data.Tags.Any(tag => CombatStatusCatalog.Instance.poison.Tags.Contains(tag))) LastPoisonDamage += damage; }
        public float ConsumePoisonDamage() { var damage = LastPoisonDamage; LastPoisonDamage = 0; return damage; }
        private IEnumerable<MonsterAttackData> AllowedChargeAttacks() => Data == null ? Enumerable.Empty<MonsterAttackData>() : Data.GetAttacks(MonsterAttackType.Charge)
            .Where(attack => string.IsNullOrEmpty(attack.requiredSupportId) || LivingSupports.Contains(attack.requiredSupportId));
        public void ApplyStatus(StatusKind kind, float duration = 0, float potency = 1, int stacks = 1, RunState root = null, EquipmentData source = null)
        {
            var data = CombatStatusCatalog.Instance?.Get(kind); if (data == null || !IsAlive) return;
            Buffs.Add(data, stacks, root, source, potency,duration:duration>0?duration:-1);
        }
        public void AddPoison(int stacks, float damage = 3f, float duration = 8f, int cap = 60)
        {
            var data = CombatStatusCatalog.Instance?.poison; if (data == null || !IsAlive) return;
            // Compatibility entry point. Authored effects use AddBuff with a saved BuffData.
            Buffs.Add(data, stacks, potency: damage,duration:duration,maximumStacks:cap);
            Buffs.UpdateState(0); Buffs.Cast();
        }
        public float DetonatePoison(float multiplier = 2.5f)
        {
            var buff = Status(StatusKind.Poison); if (buff == null) return 0;
            var damage = Buffs.PeriodicDamage(buff) * multiplier;
            buff.IsEnd = true; return damage;
        }
        public void Slow(float seconds) => ApplyStatus(StatusKind.Slow, seconds);
        public void AddBurn(float damage, float duration) => ApplyStatus(StatusKind.Burn, duration, damage);
        public void Hasten(float seconds) { NormalAttackTimer = Mathf.Max(.25f, NormalAttackTimer - seconds); ChargeTimer = Mathf.Max(.25f, ChargeTimer - seconds); }
        public void Interrupt(float seconds)
        {
            if (!IsAlive || IsStunned) return;
            if (State == MonsterState.Attacking) StateTimer += seconds;
            else if (State == MonsterState.Idle) NormalAttackTimer += seconds;
        }
        private void TickStatus(float dt) => Buffs.Tick(dt);
        public void MoveWeakPoints(Vector2 delta) { foreach (var point in weakPoints) point.Move(delta); }
    }
}
