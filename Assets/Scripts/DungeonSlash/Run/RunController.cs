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
            Shop = new ShopSystem(new EquipmentSystem());
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

        public void GrantCombatReward(CombatReward reward)
        {
            State.GainExperience(reward.Experience);
            State.GainGold(reward.Gold);
        }

        public void AwardCombat(MonsterData monster, bool elite) => GrantCombatReward(GetCombatReward(monster, elite));
        public bool TryBuy(EquipmentData item) => Shop.TryPurchase(State, item);
    }
}
