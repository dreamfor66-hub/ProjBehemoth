using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonSlash
{
    public enum TriggerAttackType { None, Normal, Charge, Additional }
    public enum TriggerAttackWay { None, Horizontal, Upward, Downward }

    public readonly struct EquipmentTriggerContext
    {
        public AttackSegment Segment { get; }
        public TriggerAttackType AttackType { get; }
        public TriggerAttackWay AttackWay { get; }
        public bool OwnerCharging { get; }
        public bool EnemyStunned { get; }
        public bool HasAttack => AttackType != TriggerAttackType.None;

        public EquipmentTriggerContext(AttackSegment segment, TriggerAttackType attackType, TriggerAttackWay attackWay, bool ownerCharging, bool enemyStunned)
        {
            Segment = segment;
            AttackType = attackType;
            AttackWay = attackWay;
            OwnerCharging = ownerCharging;
            EnemyStunned = enemyStunned;
        }

        public static EquipmentTriggerContext Simple(bool ownerCharging = false, bool enemyStunned = false) =>
            new(default, TriggerAttackType.None, TriggerAttackWay.None, ownerCharging, enemyStunned);
    }

    public readonly struct EquipmentActivation
    {
        public EquipmentData Source { get; }
        public EquipmentEffect Effect { get; }
        public TargetType Target { get; }
        public EquipmentTriggerContext Context { get; }

        public EquipmentActivation(EquipmentData source, EquipmentTriggerContext context)
        {
            Source = source;
            Effect = source.effect;
            Target = source.targetType;
            Context = context;
        }
    }

    internal sealed class EquipmentTriggerState
    {
        public EquipmentData Data { get; }
        private int seenCount;
        private int activationCount;

        public EquipmentTriggerState(EquipmentData data) => Data = data;
        public bool HasReachedMaximum => Data != null && Data.trigger.triggerMaxCount > 0 && activationCount >= Data.trigger.triggerMaxCount;

        public bool TryActivate(TriggerType triggerType, EquipmentTriggerContext context, out EquipmentActivation activation)
        {
            activation = default;
            if (Data == null || Data.trigger.triggerType != triggerType || HasReachedMaximum || !MatchesFilters(context)) return false;
            seenCount++;
            var every = Mathf.Max(1, Data.trigger.triggerCount);
            if (seenCount % every != 0) return false;
            // Conditions are deliberately evaluated after a complete trigger (including its cadence) has passed.
            if (!MatchesCondition(context)) return false;
            activation = new EquipmentActivation(Data, context);
            activationCount++;
            return true;
        }

        private bool MatchesFilters(EquipmentTriggerContext context)
        {
            if (!context.HasAttack) return true;
            var type = context.AttackType switch
            {
                TriggerAttackType.Normal => TriggerAttackTypeFilter.NormalAttack,
                TriggerAttackType.Charge => TriggerAttackTypeFilter.ChargeAttack,
                TriggerAttackType.Additional => TriggerAttackTypeFilter.AdditionalAttack,
                _ => TriggerAttackTypeFilter.All
            };
            var way = context.AttackWay switch
            {
                TriggerAttackWay.Horizontal => TriggerAttackWayFilter.Horizontal,
                TriggerAttackWay.Upward => TriggerAttackWayFilter.Upward,
                TriggerAttackWay.Downward => TriggerAttackWayFilter.Downward,
                _ => TriggerAttackWayFilter.All
            };
            // Zero is treated as All so pre-existing or hand-authored assets are safe by default.
            var typeFilter = Data.trigger.triggerAttackTypeFilter == 0 ? TriggerAttackTypeFilter.All : Data.trigger.triggerAttackTypeFilter;
            var wayFilter = Data.trigger.triggerAttackWayFilter == 0 ? TriggerAttackWayFilter.All : Data.trigger.triggerAttackWayFilter;
            return (typeFilter & type) != 0 && (wayFilter & way) != 0;
        }

        private bool MatchesCondition(EquipmentTriggerContext context) => Data.condition.conditionType switch
        {
            ConditionType.None => true,
            ConditionType.OwnerCharge => context.OwnerCharging,
            ConditionType.EnemyStun => context.EnemyStunned,
            _ => false
        };
    }

    public readonly struct CombatStartEffectResult
    {
        public float OpeningRockDamage { get; }
        public CombatStartEffectResult(float openingRockDamage) => OpeningRockDamage = Mathf.Max(0f, openingRockDamage);
    }

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
        public IReadOnlyList<EquipmentData> Potions => potions.Select(potion => potion.Data).ToArray();
        public int PotionSlotCapacity => 3;
        public bool HasPotionSpace => potions.Count < PotionSlotCapacity;
        public int PoisonStacks { get; private set; }
        public int PendingPerkChoices { get; private set; }

        private readonly RunBalanceSettings settings;
        private readonly List<EquipmentTriggerState> equippedEquipment = new();
        private readonly List<EquipmentTriggerState> potions = new();
        private readonly List<EquipmentTriggerState> pendingCombatPotions = new();
        private float revivalHealthFraction;
        private float poisonTravelDamage;

        public RunState(PlayerCombatData playerData, RunBalanceSettings balance)
        {
            settings = balance;
            Player = new PlayerCombatRuntime(playerData);
            RequiredExperience = balance.initialExperienceRequirement;
            Gold = balance.startingGold;
        }

        public void GainExperience(int amount)
        {
            Experience += Mathf.Max(0, amount);
            while (Experience >= RequiredExperience)
            {
                Experience -= RequiredExperience;
                Level++;
                PendingPerkChoices++;
                RequiredExperience += settings.experienceRequirementGrowth;
            }
        }

        public void SpendGold(int amount) => Gold -= amount;
        public void GainGold(int amount) => Gold += Mathf.Max(0, amount);
        public void ConsumePendingPerkChoice() => PendingPerkChoices = Mathf.Max(0, PendingPerkChoices - 1);

        public bool TryEquip(EquipmentData item)
        {
            if (item == null || !item.IsRelic || EquippedItems.Contains(item)) return false;
            EquippedItems.Add(item);
            var state = new EquipmentTriggerState(item);
            equippedEquipment.Add(state);
            if (state.TryActivate(TriggerType.OnAcquire, EquipmentTriggerContext.Simple(), out var activation))
                ApplyOwnerActivation(activation, permanent: true);
            return true;
        }

        public IReadOnlyList<EquipmentActivation> TriggerEquipment(TriggerType triggerType, EquipmentTriggerContext context)
        {
            var activations = new List<EquipmentActivation>();
            foreach (var state in equippedEquipment)
                if (state.TryActivate(triggerType, context, out var activation)) activations.Add(activation);
            return activations;
        }

        public bool TryStorePotion(EquipmentData potion)
        {
            if (potion == null || !potion.IsPotion || !HasPotionSpace) return false;
            potions.Add(new EquipmentTriggerState(potion));
            return true;
        }

        public bool TryUsePotion(EquipmentData potion)
        {
            var state = potions.FirstOrDefault(candidate => candidate.Data == potion);
            if (state == null) return false;
            // A full-health heal is not consumed. The check is based on the generic effect, not a potion subtype.
            if (potion.effect.effectType == EffectType.Heal && potion.targetType == TargetType.Owner && Player.CurrentHp >= Player.MaxHp) return false;

            potions.Remove(state);
            if (potion.trigger.triggerType == TriggerType.OnCombatStartAfterConsume)
            {
                pendingCombatPotions.Add(state);
                return true;
            }
            if (state.TryActivate(TriggerType.OnConsume, EquipmentTriggerContext.Simple(), out var activation))
                ApplyOwnerActivation(activation, permanent: false);
            return true;
        }

        public CombatStartEffectResult BeginCombat()
        {
            Player.ClearCombatModifiers();
            var openingDamage = 0f;
            foreach (var state in pendingCombatPotions.ToArray())
            {
                if (state.TryActivate(TriggerType.OnCombatStartAfterConsume, EquipmentTriggerContext.Simple(), out var activation))
                {
                    if (activation.Target == TargetType.Enemy && activation.Effect.effectType == EffectType.Damage)
                        openingDamage += Mathf.Max(0f, activation.Effect.effectMagnitude) * Mathf.Max(1, activation.Effect.effectCount);
                    else
                        ApplyOwnerActivation(activation, permanent: false);
                }
                if (state.HasReachedMaximum) pendingCombatPotions.Remove(state);
            }
            return new CombatStartEffectResult(openingDamage);
        }

        public void ApplyOwnerActivation(EquipmentActivation activation, bool permanent)
        {
            if (activation.Target != TargetType.Owner) return;
            var effect = activation.Effect;
            var count = Mathf.Max(1, effect.effectCount);
            switch (effect.effectType)
            {
                case EffectType.Heal:
                    Player.Heal(effect.effectMagnitude * count);
                    break;
                case EffectType.StatIncrease:
                    if (effect.effectStatType == ModifierKind.Gold)
                    {
                        GainGold(Mathf.RoundToInt(effect.effectMagnitude * count));
                        break;
                    }
                    if (effect.effectStatType == ModifierKind.RevivalHealthFraction)
                    {
                        revivalHealthFraction = Mathf.Max(revivalHealthFraction, effect.effectMagnitude);
                        break;
                    }
                    var modifier = new StatModifier { kind = effect.effectStatType, value = effect.effectMagnitude * count };
                    if (permanent) Player.ApplyModifiers(new[] { modifier });
                    else Player.ApplyCombatModifier(modifier, effect.effectTime);
                    break;
            }
        }

        public bool TryConsumeRevival()
        {
            if (revivalHealthFraction <= 0f || Player.IsAlive) return false;
            Player.Revive(revivalHealthFraction);
            revivalHealthFraction = 0f;
            return true;
        }

        public void ApplyPoison(float travelDamage, float actionSlow)
        {
            PoisonStacks++;
            Player.ApplyModifiers(new[]
            {
                new StatModifier { kind = ModifierKind.AttackCooldownMultiplier, value = -Mathf.Abs(actionSlow) },
                new StatModifier { kind = ModifierKind.ChargeDurationMultiplier, value = -Mathf.Abs(actionSlow) }
            });
            poisonTravelDamage += Mathf.Max(0f, travelDamage);
        }

        public DamageResult ApplyTravelHazard() => poisonTravelDamage <= 0f ? default : Player.TakeEnvironmentalDamage(poisonTravelDamage);
    }

    public sealed class PerkSystem
    {
        private readonly List<PerkData> allPerks;
        public PerkSystem(IEnumerable<PerkData> perks) => allPerks = perks.Where(perk => perk != null).ToList();
        public IReadOnlyList<PerkData> GetChoices(RunState run, int seed)
        {
            var random = new System.Random(seed + run.Level * 1009 + run.ActivePerks.Count * 71);
            return allPerks.Where(perk => !run.ActivePerks.Any(active => active.Data == perk && !active.CanStack)).OrderBy(_ => random.Next()).Take(3).ToList();
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
        public bool Apply(RunState run, EquipmentData item)
        {
            if (item == null) return false;
            return item.IsPotion ? run.TryStorePotion(item) : run.TryEquip(item);
        }
    }

    public sealed class ShopSystem
    {
        private readonly EquipmentSystem equipment;
        private readonly Dictionary<string, int> purchaseCounts = new();
        public ShopSystem(EquipmentSystem equipmentSystem) => equipment = equipmentSystem;
        public bool TryPurchase(RunState run, EquipmentData item)
        {
            if (item == null) return false;
            purchaseCounts.TryGetValue(item.id, out var purchased);
            if (!CanPurchase(run, item, purchased) || !equipment.Apply(run, item)) return false;
            run.SpendGold(item.price);
            purchaseCounts[item.id] = purchased + 1;
            return true;
        }

        public bool CanPurchase(RunState run, EquipmentData item)
        {
            purchaseCounts.TryGetValue(item == null ? string.Empty : item.id, out var purchased);
            return CanPurchase(run, item, purchased);
        }

        private static bool CanPurchase(RunState run, EquipmentData item, int purchased)
        {
            if (run == null || item == null || purchased >= item.maxPurchaseCount || run.Gold < item.price) return false;
            return item.IsPotion ? run.HasPotionSpace : !run.EquippedItems.Contains(item);
        }
    }
}
