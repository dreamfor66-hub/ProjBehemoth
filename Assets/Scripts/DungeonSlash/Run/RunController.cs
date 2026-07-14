using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonSlash
{
    public readonly struct CombatReward
    {
        public int Experience { get; }
        public int Gold { get; }

        public CombatReward(int experience, int gold)
        {
            Experience = Mathf.Max(0, experience);
            Gold = Mathf.Max(0, gold);
        }
    }

    public sealed class RunController : MonoBehaviour
    {
        [SerializeField] private PlayerCombatData playerData;
        [SerializeField] private RunBalanceSettings balanceSettings;
        [SerializeField] private List<PerkData> perks = new();
        [SerializeField] private List<EquipmentData> equipment = new();

        public RunState State { get; private set; }
        public IReadOnlyList<EquipmentData> Equipment => equipment;
        public PerkSystem Perks { get; private set; }
        public ShopSystem Shop { get; private set; }
        private EquipmentSystem equipmentSystem;

        public void Configure(PlayerCombatData player, RunBalanceSettings balance, List<PerkData> availablePerks, List<EquipmentData> availableEquipment)
        {
            playerData = player;
            balanceSettings = balance;
            perks = availablePerks;
            equipment = availableEquipment;
        }

        public void BeginRun()
        {
            State = new RunState(playerData, balanceSettings);
            Perks = new PerkSystem(perks);
            equipmentSystem = new EquipmentSystem();
            Shop = new ShopSystem(equipmentSystem);
        }

        public CombatReward GetCombatReward(MonsterData monster, bool elite)
        {
            if (monster == null) return new CombatReward(0, 0);
            return new CombatReward(
                monster.experienceReward * (elite ? 2 : 1),
                monster.goldReward + (elite ? balanceSettings.eliteGoldBonus : 0));
        }

        public CombatReward GetCombatReward(IEnumerable<MonsterData> monsters, bool elite)
        {
            var reward = new CombatReward(0, 0);
            foreach (var monster in monsters?.Where(monster => monster != null) ?? Enumerable.Empty<MonsterData>())
            {
                var individual = GetCombatReward(monster, elite);
                reward = new CombatReward(reward.Experience + individual.Experience, reward.Gold + individual.Gold);
            }
            return reward;
        }

        public CombatReward GetGoldRoomReward(bool major) => new CombatReward(0, major ? balanceSettings.rewardGold * 2 : balanceSettings.rewardGold);
        public CombatReward GetGoddessRoomReward() => new CombatReward(balanceSettings.goddessExperience, 0);
        public float FountainHealAmount => balanceSettings.fountainHeal;
        public float DrinkToxicFountain()
        {
            var before = State.Player.CurrentHp;
            State.Player.Heal(balanceSettings.toxicFountainHeal);
            State.ApplyPoison(balanceSettings.poisonTravelDamage, balanceSettings.poisonActionSlow);
            return State.Player.CurrentHp - before;
        }
        public DamageResult ApplyTravelHazard() => State.ApplyTravelHazard();

        public void GrantCombatReward(CombatReward reward)
        {
            State.GainExperience(reward.Experience);
            State.GainGold(reward.Gold);
        }

        public void AwardCombat(MonsterData monster, bool elite) => GrantCombatReward(GetCombatReward(monster, elite));
        public bool TryBuy(EquipmentData item) => Shop.TryPurchase(State, item);
        public bool TryUsePotion(EquipmentData potion) => State != null && State.TryUsePotion(potion);
        public bool TryGrantRelic(EquipmentData relic) => State != null && relic != null && relic.IsRelic && !State.EquippedItems.Contains(relic) && equipmentSystem.Apply(State, relic);
        public EquipmentData GetChestRelic(int seed, int salt)
        {
            var random = new System.Random(seed + salt * 577);
            var relics = equipment.Where(item => item != null && item.IsRelic && !State.EquippedItems.Contains(item)).OrderBy(_ => random.Next()).ToList();
            return relics.Count == 0 ? null : relics[0];
        }
        public EquipmentData GetPotionDrop(int seed, int salt)
        {
            var potions = equipment.Where(item => item != null && item.IsPotion).OrderBy(item => new System.Random(seed + salt + item.id.GetHashCode()).Next()).ToList();
            return potions.Count == 0 ? null : potions[0];
        }

        public IReadOnlyList<EquipmentData> GetRelicChoices(int seed, int salt)
        {
            var random = new System.Random(seed + salt * 997);
            return equipment.Where(item => item != null && item.IsRelic && !State.EquippedItems.Contains(item))
                .OrderBy(_ => random.Next()).Take(3).ToList();
        }

        public IReadOnlyList<EquipmentData> GetShopInventory(int seed, int roomId)
        {
            var random = new System.Random(seed + roomId * 4099);
            var result = new List<EquipmentData>();
            var premiumRelics = equipment.Where(item => item != null && item.IsRelic && !State.EquippedItems.Contains(item))
                .OrderByDescending(item => item.price).ThenBy(_ => random.Next()).ToList();
            if (premiumRelics.Count > 0) result.Add(premiumRelics[0]);
            var potionCandidates = equipment.Where(item => item != null && item.IsPotion && !result.Contains(item)).OrderBy(_ => random.Next()).ToList();
            foreach (var candidate in potionCandidates)
            {
                if (result.Count >= 3) break;
                result.Add(candidate);
            }
            var fallbackCandidates = equipment.Where(item => item != null && item.IsRelic && !State.EquippedItems.Contains(item) && !result.Contains(item)).OrderBy(_ => random.Next()).ToList();
            foreach (var candidate in fallbackCandidates)
            {
                if (result.Count >= 3) break;
                result.Add(candidate);
            }
            return result;
        }
    }
}
