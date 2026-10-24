using UnityEngine;

namespace DungeonSlash
{
    public sealed partial class PlayerCombatRuntime
    {
        public float MaxMp => Mathf.Max(0, data.maxMp + BuffStat(ModifierKind.MaxMp));
        public float CurrentMp { get; private set; }
        public void RefillMp() => CurrentMp = MaxMp;
        public void RestoreMp(float amount) => CurrentMp = Mathf.Clamp(CurrentMp + Mathf.Max(0, amount), 0, MaxMp);
        public bool CanPayMp(float amount) => CurrentMp + .0001f >= Mathf.Max(0, amount);
        public bool TrySpendMp(float amount)
        {
            if (!CanPayMp(amount)) return false;
            CurrentMp = Mathf.Max(0, CurrentMp - Mathf.Max(0, amount));
            return true;
        }
        public float ElementMultiplier(SpellElement element) => Mathf.Max(0, 1 + BuffStat(element switch
        {
            SpellElement.Fire => ModifierKind.FireDamage,
            SpellElement.Ice => ModifierKind.IceDamage,
            _ => ModifierKind.LightningDamage
        }));
        public float PoisonMultiplier => Mathf.Max(0, 1 + BuffStat(ModifierKind.PoisonDamage));
    }
}
