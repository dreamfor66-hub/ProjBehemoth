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
        public IReadOnlyList<PerkData> AvailablePerks => perks;
        public PerkSystem Perks { get; private set; }
        public ShopSystem Shop { get; private set; }
        private EquipmentSystem equipmentSystem;
        [SerializeField] private ShopInventorySettings shopInventory;
        private readonly Dictionary<(int Seed, int Act, int Room), IReadOnlyList<EquipmentData>> shopInventories = new();
        private readonly Dictionary<(int Seed, int Act, int Room), IReadOnlyList<ShopOffer>> shopStocks = new();
        public ShopInventorySettings ShopInventory => shopInventory;
        public void ClearChapterShopCache() { shopInventories.Clear(); shopStocks.Clear(); }

        public void Configure(PlayerCombatData player, RunBalanceSettings balance, List<PerkData> availablePerks, List<EquipmentData> availableEquipment)
        {
            playerData = player;
            balanceSettings = balance;
            perks = availablePerks;
            equipment = availableEquipment;
        }

        public void BeginRun()
        {
            if (RunSession.SelectedCharacter != null) playerData = RunSession.SelectedCharacter;
            State = new RunState(playerData, balanceSettings);
            Perks = new PerkSystem(perks);
            equipmentSystem = new EquipmentSystem();
            Shop = new ShopSystem(equipmentSystem);
            shopInventories.Clear();
            shopStocks.Clear();
            if (playerData.startingRelic != null) State.TryEquip(playerData.startingRelic);
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
        public bool TryGrantPerk(PerkData perk)
        {
            if (State == null || Perks == null || !PerkSystem.IsAvailable(perk, State)) return false;
            var existing = State.ActivePerks.FirstOrDefault(active => active.Data == perk);
            if (existing != null && !existing.CanStack) return false;
            Perks.Apply(State, perk);
            return true;
        }
        public bool TryGrantEquipment(EquipmentData item) => State != null && item != null && equipmentSystem != null && equipmentSystem.Apply(State, item);
        public bool TryRemovePerk(PerkData perk) => State != null && State.TryRemovePerk(perk);
        public bool TryRemoveEquipment(EquipmentData item) => State != null && State.TryRemoveEquipment(item);
        public IReadOnlyList<EquipmentData> GetExclusiveChoices(int seed)
        {
            if (State.ExclusiveCount >= RunState.ExclusiveLimit) return System.Array.Empty<EquipmentData>();
            var random = new System.Random(seed);
            return equipment.Where(item => item != null && State.CanOffer(item) && item.scope == RelicScope.Exclusive && item.exclusiveCharacter == State.Player.Data.character && !State.EquippedItems.Contains(item))
                .OrderBy(_ => random.Next()).Take(3).ToArray();
        }
        public EquipmentData GetChestRelic(int seed, int salt)
        {
            var random = new System.Random(seed + salt * 577);
            var relics = equipment.Where(CanOfferSharedRelic).OrderBy(item => OfferScore(item, random)).ToList();
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
            return equipment.Where(CanOfferSharedRelic)
                .OrderBy(item => OfferScore(item, random)).Take(3).ToList();
        }

        public IReadOnlyList<EquipmentData> GetShopInventory(int seed, int roomId)
        {
            var key = (seed, State.CurrentAct, roomId);
            if (shopInventories.TryGetValue(key, out var inventory)) return inventory;
            // Compatibility for equipment-only consumers. The shop itself renders the full typed stock.
            inventory = GetShopStock(seed, roomId).Where(offer => offer.Item != null).Select(offer => offer.Item).ToList().AsReadOnly();
            shopInventories.Add(key, inventory);
            return inventory;
        }
        public IReadOnlyList<ShopOffer> GetShopStock(int seed, int roomId)
        {
            var key = (seed, State.CurrentAct, roomId);
            if (shopStocks.TryGetValue(key, out var stock)) return stock;
            var random = new System.Random(unchecked(seed + roomId * 4099 + State.CurrentAct * 7919));
            var potionSlots = shopInventory != null ? shopInventory.potionSlots : 3;
            var perkSlots = shopInventory != null ? shopInventory.perkSlots : 3;
            var relicSlots = shopInventory != null ? shopInventory.relicSlots : 4;
            var result = new List<ShopOffer>();
            result.AddRange(equipment.Where(item => item != null && item.IsPotion).Distinct()
                .OrderBy(_ => random.Next()).Take(potionSlots).Select(item => new ShopOffer(item)));
            result.AddRange(perks.Where(perk => perk != null && perk.soldByMerchant && PerkSystem.IsAvailable(perk, State)
                && !State.ActivePerks.Any(active => active.Data == perk && !active.CanStack)).Distinct()
                .OrderBy(_ => random.Next()).Take(perkSlots).Select(perk => new ShopOffer(perk)));
            result.AddRange(equipment.Where(CanOfferSharedRelic).Distinct().OrderBy(item => OfferScore(item, random))
                .Take(relicSlots).Select(item => new ShopOffer(item)));
            // A merchant's first stock is frozen. Purchasing never refills or rerolls a slot.
            stock = result.AsReadOnly(); shopStocks.Add(key, stock); return stock;
        }
        public bool TryBuy(ShopOffer offer) => Shop.TryPurchase(State, offer, Perks);
        private double OfferScore(EquipmentData item, System.Random random) => -System.Math.Log(System.Math.Max(.000001, random.NextDouble())) / (item.grantedSpell != null ? System.Math.Max(.001, State.MagicOfferWeight) : 1);
        private bool CanOfferSharedRelic(EquipmentData item) => item != null && State.CanOffer(item) && item.IsRelic && item.scope == RelicScope.Shared && !State.EquippedItems.Contains(item)
            && (item.grantedSpell == null || State.RuntimeFor(item.grantedSpell) == null);
    }
}
