using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonSlash
{
    public sealed class SpellRuntime
    {
        public SpellData Data { get; }
        public float Cooldown { get; private set; }
        public SpellRuntime(SpellData data) => Data = data;
        public void Tick(float dt) => Cooldown = Mathf.Max(0, Cooldown - dt);
        public void Use(PlayerCombatRuntime player) => Cooldown = Data.cooldownSeconds * player.SpellCooldownFor(Data);
        public void ResetCooldown() => Cooldown = 0;
        public void BeginCombat(PlayerCombatRuntime player) => Cooldown = Data.openingCooldownSeconds * player.SpellCooldownFor(Data);
        public void Reduce(float seconds) => Cooldown = Mathf.Max(0, Cooldown - seconds);
    }

    public sealed partial class RunState
    {
        public const int ExclusiveLimit = 3;
        public int Seed { get; set; }
        public int ExclusiveCount => EquippedItems.Count(item => item.scope == RelicScope.Exclusive);
        public IReadOnlyList<SpellRuntime> Spellbook => spellbook;
        private readonly List<SpellRuntime> spellbook = new();
        private readonly SpellData[] spellSlots = new SpellData[4];
        public Queue<SpellData> PendingSpellOffers { get; } = new();
        public HashSet<string> EventFlags { get; } = new();
        public HashSet<string> SeenEvents { get; } = new();
        public int WonCombats { get; set; }
        public int CurrentAct { get; set; } = 1;
        public int NormalCombatsInAct { get; set; }
        public bool MagicIntroduced => CurrentAct >= 2 || NormalCombatsInAct >= 4;
        public float MagicOfferWeight => CurrentAct >= 2 ? 1f : NormalCombatsInAct >= 4 ? .2f : 0;
        public bool CanOffer(EquipmentData item) => item != null && (item.grantedSpell == null || MagicIntroduced);
        public int SpellCasts { get; set; }
        public Dictionary<string, int> EventBurdens { get; } = new();
        public int EventGoldDebt { get; set; }
        public void AddBurden(string kind, int combats) { EventBurdens.TryGetValue(kind, out var count); EventBurdens[kind] = count + combats; }
        public void CompleteEventConsequences()
        {
            foreach (var key in EventBurdens.Keys.ToArray()) if (--EventBurdens[key] <= 0) EventBurdens.Remove(key);
            if (EventGoldDebt > 0) { var payment = Math.Min(Gold, Math.Min(15, EventGoldDebt)); SpendGold(payment); EventGoldDebt -= payment; }
        }
        public SpellData Slot(SwipeDirection slot) => spellSlots[(int)slot];
        public SpellRuntime RuntimeFor(SpellData spell) => spellbook.FirstOrDefault(item => item.Data == spell);
        public bool LearnSpell(SpellData spell) => LearnSpell(spell, false);
        public bool LearnSpell(SpellData spell, bool replace)
        {
            if (spell == null || spellbook.Any(item => item.Data == spell)) return false;
            var previous = Slot(spell.defaultSlot);
            if (previous != null && !replace) return false;
            if (previous != null) spellbook.RemoveAll(item => item.Data == previous);
            spellbook.Add(new SpellRuntime(spell));
            spellSlots[(int)spell.defaultSlot] = spell;
            return true;
        }
        public bool AssignSpell(SwipeDirection slot, SpellData spell)
        {
            // Compatibility API: directions are authored on the spell, never player-assigned.
            return spell != null && spell.defaultSlot == slot && Slot(slot) == spell;
        }
        public void TickSpells(float dt) { foreach (var spell in spellbook) spell.Tick(dt); }
        public void ReduceSpellCooldowns(float seconds) { foreach (var spell in spellbook) spell.Reduce(seconds); }
        public bool CanPay(int gold, float hp, float maxHp = 0) => Gold >= gold && Player.CurrentHp > hp && Player.MaxHp - maxHp >= 20f;
        public bool Pay(int gold, float hp, float maxHp = 0)
        {
            if (!CanPay(gold, hp, maxHp)) return false;
            SpendGold(gold); Player.TakeEnvironmentalDamage(hp);
            if (maxHp != 0) Player.ApplyModifiers(new[] { new StatModifier { kind = ModifierKind.MaxHp, value = -maxHp } });
            return true;
        }
    }
}
