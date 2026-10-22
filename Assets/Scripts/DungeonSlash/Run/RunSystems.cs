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
        public MonsterRuntime Target { get; }
        public SpellData Spell { get; }
        public FormData Form { get; }
        public float Damage { get; }
        public float GuardDamage { get; }
        public bool Draw { get; }
        public int EnemyCount { get; }
        public BuffInstance Buff { get; }
        public ActionEffectContext Mutable { get; }
        public BuffTagData Signal { get; }
        public IReadOnlyCollection<BuffTagData> ActionTags { get; }
        public SwipeDirection Direction => CombatController.ResolveDirection(Segment.End - Segment.Start);

        public EquipmentTriggerContext(AttackSegment segment, TriggerAttackType attackType, TriggerAttackWay attackWay, bool ownerCharging, bool enemyStunned, MonsterRuntime target = null, SpellData spell = null, FormData form = null, float damage = 0, float guardDamage = 0, bool draw = false, int enemyCount = 1, BuffInstance buff = null, ActionEffectContext mutable = null, BuffTagData signal = null, IReadOnlyCollection<BuffTagData> actionTags = null)
        {
            Segment = segment;
            AttackType = attackType;
            AttackWay = attackWay;
            OwnerCharging = ownerCharging;
            EnemyStunned = enemyStunned;
            Target = target; Spell = spell; Form = form; Damage = damage; GuardDamage = guardDamage; Draw = draw; EnemyCount = enemyCount;
            Buff = buff; Mutable = mutable; Signal = signal;
            ActionTags=actionTags ?? mutable?.Tags;
        }
        public EquipmentTriggerContext WithBuff(BuffInstance buff) => new(Segment, AttackType, AttackWay, OwnerCharging, EnemyStunned, Target, Spell, Form, Damage, GuardDamage, Draw, EnemyCount, buff, Mutable, Signal, ActionTags);
        public EquipmentTriggerContext WithSignal(BuffTagData signal) => new(Segment, AttackType, AttackWay, OwnerCharging, EnemyStunned, Target, Spell, Form, Damage, GuardDamage, Draw, EnemyCount, Buff, Mutable, signal, ActionTags);
        public EquipmentTriggerContext WithDamage(float damage) => new(Segment, AttackType, AttackWay, OwnerCharging, EnemyStunned, Target, Spell, Form, damage, GuardDamage, Draw, EnemyCount, Buff, Mutable, Signal, ActionTags);

        public static EquipmentTriggerContext Simple(bool ownerCharging = false, bool enemyStunned = false) =>
            new(default, TriggerAttackType.None, TriggerAttackWay.None, ownerCharging, enemyStunned);
    }

    public readonly struct EquipmentActivation
    {
        public EquipmentData Source { get; }
        public EquipmentEffect Effect { get; }
        public TargetType Target { get; }
        public EquipmentTriggerContext Context { get; }
        public string BuffKey { get; }

        public EquipmentActivation(EquipmentData source, EquipmentTriggerContext context)
        {
            Source = source;
            Effect = source.effect;
            Target = source.targetType;
            Context = context;
            BuffKey = source.GetInstanceID().ToString();
        }
        public EquipmentActivation(EquipmentData source, BuffUnitData ability, EquipmentEffect effect, EquipmentTriggerContext context, string key)
        { Source = source; Effect = effect; Target = ability.target; Context = context; BuffKey = key; }
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

    public sealed partial class RunState
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
        private readonly List<AbilityTriggerState> pendingPotionAbilities = new();
        private float revivalHealthFraction;
        private float poisonTravelDamage;

        public RunState(PlayerCombatData playerData, RunBalanceSettings balance)
        {
            settings = balance;
            Player = new PlayerCombatRuntime(playerData);
            Player.Owner = this;
            foreach (var spell in playerData.startingSpells) LearnSpell(spell);
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
            if (item.scope == RelicScope.Exclusive && (item.exclusiveCharacter != Player.Data.character || ExclusiveCount >= ExclusiveLimit)) return false;
            EquippedItems.Add(item);
            var state = new EquipmentTriggerState(item);
            equippedEquipment.Add(state);
            RegisterAbilities(item);
            Player.ApplyModifiers(item.additionalModifiers);
            if (item.abilities.Count == 0 && item.grantedSpell != null && RuntimeFor(item.grantedSpell) == null && !LearnSpell(item.grantedSpell)) PendingSpellOffers.Enqueue(item.grantedSpell);
            if (item.abilities.Count == 0 && state.TryActivate(TriggerType.OnAcquire, EquipmentTriggerContext.Simple(), out var activation))
                ApplyOwnerActivation(activation, permanent: true);
            foreach (var ability in abilityStates.Where(ability => ability.Source == item))
                foreach (var acquired in ability.Trigger(this, TriggerType.OnAcquire, EquipmentTriggerContext.Simple())) ApplyOwnerActivation(acquired, true);
            return true;
        }

        public bool TryRemovePerk(PerkData perk)
        {
            var active = ActivePerks.FirstOrDefault(candidate => candidate.Data == perk);
            if (active == null) return false;
            ActivePerks.Remove(active);
            for (var stack = 0; stack < active.StackCount; stack++)
                Player.RemoveModifiers(perk.modifiers);
            return true;
        }

        public bool TryRemoveEquipment(EquipmentData item)
        {
            if (item == null) return false;
            if (item.IsPotion)
            {
                var potion = potions.FirstOrDefault(candidate => candidate.Data == item);
                if (potion == null) return false;
                potions.Remove(potion);
                return true;
            }

            if (!EquippedItems.Remove(item)) return false;
            var equipped = equippedEquipment.FirstOrDefault(candidate => candidate.Data == item);
            if (equipped != null) equippedEquipment.Remove(equipped);
            abilityStates.RemoveAll(state => state.Source == item);
            Buffs.RemoveSource(item);
            foreach (var ability in item.abilities.Where(ability => ability.trigger.triggerType == TriggerType.OnAcquire && ability.target == TargetType.Owner))
                foreach (var effect in ability.effects.Where(effect => effect.effectType == EffectType.StatIncrease && effect.effectStatType != ModifierKind.Gold && effect.effectStatType != ModifierKind.RevivalHealthFraction))
                    Player.RemoveModifiers(new[] { new StatModifier { kind = effect.effectStatType, value = EffectValue(effect, default) } });
            RemovePermanentEquipmentEffect(item);
            Player.RemoveModifiers(item.additionalModifiers);
            return true;
        }

        public IReadOnlyList<EquipmentActivation> TriggerEquipment(TriggerType triggerType, EquipmentTriggerContext context)
        {
            Buffs.Dispatch(triggerType, context);
            var activations = new List<EquipmentActivation>();
            foreach (var state in equippedEquipment)
                if (state.Data.abilities.Count == 0 && state.TryActivate(triggerType, context, out var activation)) activations.Add(activation);
            foreach (var state in abilityStates) activations.AddRange(state.Trigger(this, triggerType, context));
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
            var onlyHealing = potion.abilities.Count > 0 ? potion.abilities.SelectMany(ability => ability.effects).All(effect => effect.effectType == EffectType.Heal) : IsHealingPotion(potion);
            if (onlyHealing && Player.CurrentHp >= Player.MaxHp) return false;

            potions.Remove(state);
            if (potion.abilities.Count > 0)
            {
                for (var i = 0; i < potion.abilities.Count; i++)
                {
                    var ability = new AbilityTriggerState(potion, potion.abilities[i], i);
                    if (ability.Ability.trigger.triggerType == TriggerType.OnCombatStartAfterConsume) pendingPotionAbilities.Add(ability);
                    else foreach (var activated in ability.Trigger(this, TriggerType.OnConsume, default))
                        if (activated.Target == TargetType.Owner) ApplyOwnerActivation(activated, false); else PendingAbilityEffects.Enqueue(activated);
                }
                CompletePotionAction();
                return true;
            }
            if (potion.trigger.triggerType == TriggerType.OnCombatStartAfterConsume)
            {
                pendingCombatPotions.Add(state);
                CompletePotionAction();
                return true;
            }
            if (state.TryActivate(TriggerType.OnConsume, EquipmentTriggerContext.Simple(), out var activation))
                ApplyOwnerActivation(activation, permanent: false);
            CompletePotionAction();
            return true;
        }

        private void CompletePotionAction()
        {
            if (!Awareness.IsConcealed) return;
            StartAction(default);
            CompleteAction(TriggerType.OnConsume, default);
        }

        public CombatStartEffectResult BeginCombat()
        {
            Awareness.End();
            Player.ClearCombatModifiers();
            ClearAbilityCombatState();
            Player.BeginCharacterCombat();
            if (EventBurdens.ContainsKey("Fatigue")) Player.ApplyCombatModifier(new StatModifier { kind = ModifierKind.AttackCooldownMultiplier, value = -.2f }, 0);
            if (EventBurdens.ContainsKey("CrackedGuard")) Player.ApplyCombatModifier(new StatModifier { kind = ModifierKind.ShieldMax, value = -8f }, 0);
            if (EventBurdens.ContainsKey("BluntEdge")) Player.ApplyCombatModifier(new StatModifier { kind = ModifierKind.AllDamageMultiplier, value = -.2f }, 0);
            EmitEvent(TriggerType.OnCombatStart, default);
            Awareness.Begin(Rule(AbilityRule.EncounterConcealment) > 0, Rule(AbilityRule.DetectionChancePerAction));
            foreach (var spell in spellbook) spell.BeginCombat(Player);
            var openingDamage = 0f;
            foreach (var ability in pendingPotionAbilities.ToArray())
            {
                ability.BeginCombat();
                foreach (var activation in ability.Trigger(this, TriggerType.OnCombatStartAfterConsume, default))
                    if (activation.Target == TargetType.Owner) ApplyOwnerActivation(activation, false);
                    else if (activation.Effect.effectType == EffectType.Damage) openingDamage += EffectValue(activation.Effect, default);
                    else PendingAbilityEffects.Enqueue(activation);
                if (ability.HasReachedMaximum) pendingPotionAbilities.Remove(ability);
            }
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
            if (activation.Target == TargetType.Enemy) return;
            if (ApplyExtendedOwnerEffect(activation, permanent)) return;
            var effect = activation.Effect;
            var count = Mathf.Max(1, effect.effectCount);
            switch (effect.effectType)
            {
                case EffectType.Heal:
                    Player.Heal(effect.effectMagnitude * count);
                    break;
                case EffectType.StatIncrease:
                    var value = EffectValue(effect, activation.Context);
                    if (effect.effectStatType == ModifierKind.Gold)
                    {
                        GainGold(Mathf.RoundToInt(value));
                        break;
                    }
                    if (effect.effectStatType == ModifierKind.RevivalHealthFraction)
                    {
                        revivalHealthFraction = Mathf.Max(revivalHealthFraction, value);
                        break;
                    }
                    var modifier = new StatModifier { kind = effect.effectStatType, value = value };
                    if (permanent) Player.ApplyModifiers(new[] { modifier });
                    else Player.ApplyCombatModifier(modifier, effect.effectTime);
                    break;
            }
        }

        private void RemovePermanentEquipmentEffect(EquipmentData item)
        {
            if (item.abilities.Count > 0) return;
            if (item.trigger.triggerType != TriggerType.OnAcquire || item.targetType != TargetType.Owner) return;
            var effect = item.effect;
            if (effect.effectType != EffectType.StatIncrease) return;
            if (effect.effectStatType == ModifierKind.RevivalHealthFraction)
            {
                revivalHealthFraction = 0f;
                foreach (var equipped in EquippedItems.Where(candidate => candidate.trigger.triggerType == TriggerType.OnAcquire && candidate.targetType == TargetType.Owner && candidate.effect.effectType == EffectType.StatIncrease && candidate.effect.effectStatType == ModifierKind.RevivalHealthFraction))
                    revivalHealthFraction = Mathf.Max(revivalHealthFraction, equipped.effect.effectMagnitude);
                return;
            }
            if (effect.effectStatType == ModifierKind.Gold) return;
            var modifier = new StatModifier { kind = effect.effectStatType, value = effect.effectMagnitude * Mathf.Max(1, effect.effectCount) };
            Player.RemoveModifiers(new[] { modifier });
        }

        public bool TryConsumeRevival()
        {
            if (revivalHealthFraction <= 0f || Player.IsAlive) return false;
            Player.Revive(revivalHealthFraction);
            revivalHealthFraction = 0f;
            return true;
        }
        public static bool IsHealingPotion(EquipmentData item) => item != null && item.IsPotion && (item.abilities.Count > 0
            ? item.abilities.Any(ability => ability.target == TargetType.Owner && ability.effects.Any(effect => effect.effectType == EffectType.Heal))
            : item.targetType == TargetType.Owner && item.effect.effectType == EffectType.Heal);

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
        public static bool IsAvailable(PerkData perk) => perk != null && !perk.modifiers.Any(modifier => modifier.kind is ModifierKind.AttackReach or ModifierKind.NormalShieldRegen or ModifierKind.ShieldOnHit or ModifierKind.ShieldOnWeakPoint or ModifierKind.MonsterKillHeal or ModifierKind.ChargeGuardEnabled or ModifierKind.DoubleChargeEnabled or ModifierKind.DamagePerAttackDistance);
        public PerkSystem(IEnumerable<PerkData> perks) => allPerks = perks.Where(IsAvailable).ToList();
        public IReadOnlyList<PerkData> GetChoices(RunState run, int seed)
        {
            var random = new System.Random(seed + run.Level * 1009 + run.ActivePerks.Count * 71);
            return allPerks.Where(perk => !run.ActivePerks.Any(active => active.Data == perk && !active.CanStack)).OrderBy(_ => random.Next()).Take(3).ToList();
        }
        public void Apply(RunState run, PerkData perk)
        {
            if (!IsAvailable(perk)) return;
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
