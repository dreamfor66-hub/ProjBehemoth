using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonSlash
{
    public sealed class PerkRuntime
    {
        public PerkData Data { get; }
        public int StackCount { get; private set; }
        public PerkRuntime(PerkData data) { Data = data; StackCount = 1; }
        public bool CanStack => StackCount < Data.maxStacks;
        public void AddStack() => StackCount++;
    }

    public sealed class RunState
    {
        public PlayerCombatRuntime Player { get; }
        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public int RequiredExperience { get; private set; }
        public int Gold { get; private set; }
        public List<PerkRuntime> ActivePerks { get; } = new();
        public List<EquipmentData> EquippedItems { get; } = new();
        public int PendingPerkChoices { get; private set; }
        private readonly RunBalanceSettings settings;

        public RunState(PlayerCombatData playerData, RunBalanceSettings balance)
        {
            settings = balance; Player = new PlayerCombatRuntime(playerData); RequiredExperience = balance.initialExperienceRequirement; Gold = balance.startingGold;
        }

        public void GainExperience(int amount)
        {
            Experience += Mathf.Max(0, amount);
            while (Experience >= RequiredExperience)
            {
                Experience -= RequiredExperience; Level++; PendingPerkChoices++;
                RequiredExperience += settings.experienceRequirementGrowth;
            }
        }
        public void SpendGold(int amount) => Gold -= amount;
        public void GainGold(int amount) => Gold += Mathf.Max(0, amount);
        public void ConsumePendingPerkChoice() => PendingPerkChoices = Mathf.Max(0, PendingPerkChoices - 1);
    }

    public sealed class PerkSystem
    {
        private readonly List<PerkData> allPerks;
        public PerkSystem(IEnumerable<PerkData> perks) => allPerks = perks.Where(perk => perk != null).ToList();
        public IReadOnlyList<PerkData> GetChoices(RunState run, int seed)
        {
            var random = new System.Random(seed + run.Level * 1009 + run.ActivePerks.Count * 71);
            return allPerks.Where(perk => !run.ActivePerks.Any(active => active.Data == perk && !active.CanStack))
                .OrderBy(_ => random.Next()).Take(3).ToList();
        }
        public void Apply(RunState run, PerkData perk)
        {
            var existing = run.ActivePerks.FirstOrDefault(active => active.Data == perk);
            if (existing != null) existing.AddStack(); else run.ActivePerks.Add(new PerkRuntime(perk));
            run.Player.ApplyModifiers(perk.modifiers);
            run.ConsumePendingPerkChoice();
        }
    }

    public sealed class EquipmentSystem
    {
        public void Apply(RunState run, EquipmentData item) { run.EquippedItems.Add(item); run.Player.ApplyModifiers(item.modifiers); }
    }

    public sealed class ShopSystem
    {
        private readonly EquipmentSystem equipment;
        private readonly Dictionary<string, int> purchaseCounts = new();
        public ShopSystem(EquipmentSystem equipmentSystem) => equipment = equipmentSystem;
        public bool TryPurchase(RunState run, EquipmentData item)
        {
            purchaseCounts.TryGetValue(item.id, out var purchased);
            if (purchased >= item.maxPurchaseCount || run.Gold < item.price) return false;
            run.SpendGold(item.price); purchaseCounts[item.id] = purchased + 1; equipment.Apply(run, item); return true;
        }
    }

}
