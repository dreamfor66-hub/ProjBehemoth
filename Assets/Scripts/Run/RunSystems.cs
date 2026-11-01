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
        public BuffData AttackBuff { get; }
        public float Damage { get; }
        public float GuardDamage { get; }
        public bool Draw { get; }
        public int EnemyCount { get; }
        public BuffInstance Buff { get; }
        public ActionEffectContext Mutable { get; }
        public BuffTagData Signal { get; }
        public IReadOnlyCollection<BuffTagData> ActionTags { get; }
        public float Potency { get; }
        public SwipeDirection Direction => CombatController.ResolveDirection(Segment.End - Segment.Start);

        public EquipmentTriggerContext(AttackSegment segment, TriggerAttackType attackType, TriggerAttackWay attackWay, bool ownerCharging, bool enemyStunned, MonsterRuntime target = null, SpellData spell = null, BuffData attackBuff = null, float damage = 0, float guardDamage = 0, bool draw = false, int enemyCount = 1, BuffInstance buff = null, ActionEffectContext mutable = null, BuffTagData signal = null, IReadOnlyCollection<BuffTagData> actionTags = null, float potency = 1)
        {
            Segment = segment;
            AttackType = attackType;
            AttackWay = attackWay;
            OwnerCharging = ownerCharging;
            EnemyStunned = enemyStunned;
            Target = target; Spell = spell; AttackBuff = attackBuff; Damage = damage; GuardDamage = guardDamage; Draw = draw; EnemyCount = enemyCount;
            Buff = buff; Mutable = mutable; Signal = signal;
            ActionTags=actionTags ?? mutable?.Tags;
            Potency = potency;
        }
        public EquipmentTriggerContext WithBuff(BuffInstance buff) => new(Segment, AttackType, AttackWay, OwnerCharging, EnemyStunned, Target, Spell, AttackBuff, Damage, GuardDamage, Draw, EnemyCount, buff, Mutable, Signal, ActionTags, Potency);
        public EquipmentTriggerContext WithSignal(BuffTagData signal) => new(Segment, AttackType, AttackWay, OwnerCharging, EnemyStunned, Target, Spell, AttackBuff, Damage, GuardDamage, Draw, EnemyCount, Buff, Mutable, signal, ActionTags, Potency);
        public EquipmentTriggerContext WithDamage(float damage) => new(Segment, AttackType, AttackWay, OwnerCharging, EnemyStunned, Target, Spell, AttackBuff, damage, GuardDamage, Draw, EnemyCount, Buff, Mutable, Signal, ActionTags, Potency);
        public EquipmentTriggerContext WithPotency(float potency) => new(Segment, AttackType, AttackWay, OwnerCharging, EnemyStunned, Target, Spell, AttackBuff, Damage, GuardDamage, Draw, EnemyCount, Buff, Mutable, Signal, ActionTags, potency);

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
        private readonly Dictionary<SupplyData,int> supplies = new();
        public int SupplyCount(SupplyData supply) => supply != null && supplies.TryGetValue(supply, out var count) ? count : 0;
        public bool SpendSupply(SupplyData supply, int count) { if (SupplyCount(supply) < count) return false; if (supply != null) supplies[supply] = SupplyCount(supply) - count; return true; }
        public IReadOnlyList<EquipmentData> Potions => potions.Select(potion => potion.Data).ToArray();
        public const int DefaultPotionSlotCapacity = 3;
        public int PotionSlotCapacity => DefaultPotionSlotCapacity;
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
        private float poisonActionSlowTotal;

        public RunState(PlayerCombatData playerData, RunBalanceSettings balance)
        {
            settings = balance;
            Player = new PlayerCombatRuntime(playerData);
            Player.Owner = this;
            foreach (var supply in playerData.startingSupplies) if (supply.supply != null) supplies[supply.supply] = supply.count;
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
            Player.ClampResources();
            return true;
        }

        public bool TryRemovePerk(PerkData perk)
        {
            var active = ActivePerks.FirstOrDefault(candidate => candidate.Data == perk);
            if (active == null) return false;
            ActivePerks.Remove(active);
            RemovePerkAbilities(perk);
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
            if (!CanUsePotion(potion, false)) return false;
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

        public bool CanUsePotion(EquipmentData potion, bool thrown)
        {
            if (potion == null || !Player.IsAlive || !potions.Any(p => p.Data == potion)) return false;
            var trigger = thrown ? TriggerType.OnThrow : TriggerType.OnConsume;
            if (potion.abilities.Count == 0) return !thrown && potion.trigger.triggerType is TriggerType.OnConsume or TriggerType.OnCombatStartAfterConsume;
            var actions = potion.abilities.Where(a => a.trigger.triggerType == trigger || !thrown && a.trigger.triggerType == TriggerType.OnCombatStartAfterConsume).ToArray();
            if (actions.Length == 0) return false;
            if(actions.SelectMany(a=>a.effects).All(e=>e.effectType==EffectType.RecoverFatigue)&&Fatigue.Value>=Fatigue.Capacity)return false;
            return !actions.SelectMany(a => a.effects).All(e => e.effectType == EffectType.Heal) || Player.CurrentHp < Player.MaxHp;
        }

        public bool TryThrowPotion(EquipmentData potion, MonsterRuntime target)
        {
            if (target == null || !target.IsAlive || !CanUsePotion(potion, true)) return false;
            var context = new EquipmentTriggerContext(default, TriggerAttackType.None, TriggerAttackWay.None, false, target.IsStunned, target);
            var effects = potion.abilities.SelectMany((a, i) => new AbilityTriggerState(potion, a, i).Trigger(this, TriggerType.OnThrow, context)).ToArray();
            if (effects.Length == 0) return false;
            potions.Remove(potions.First(p => p.Data == potion));
            foreach (var effect in effects)
                if (effect.Target == TargetType.Owner || effect.Target == TargetType.OwnerStat) ApplyOwnerActivation(effect, false);
                else PendingAbilityEffects.Enqueue(effect);
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
            if (Player.IsAlive) return false;
            // Carried consumables react to death without ever being drunk or equipped.
            foreach (var potion in potions.ToArray())
            {
                foreach (var ability in potion.Data.abilities.Where(a => a.trigger.triggerType == TriggerType.OnDeath && a.target == TargetType.Owner))
                {
                    var effects = ability.effects.Where(e => e.effectType == EffectType.StatIncrease && e.effectStatType == ModifierKind.RevivalHealthFraction).ToArray();
                    if (!ability.Matches(this, default) || effects.Length == 0) continue;
                    var fraction = effects.Max(e => EffectValue(e, default));
                    if (fraction <= 0) continue;
                    potions.Remove(potion); Player.Revive(fraction); return true;
                }
            }
            if (revivalHealthFraction <= 0f) return false;
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
            poisonActionSlowTotal += Mathf.Abs(actionSlow);
        }

        public void Cleanse()
        {
            Buffs.Cleanse();
            Player.RemoveModifiers(new[] { new StatModifier { kind = ModifierKind.AttackCooldownMultiplier, value = -poisonActionSlowTotal }, new StatModifier { kind = ModifierKind.ChargeDurationMultiplier, value = -poisonActionSlowTotal } });
            PoisonStacks = 0; poisonTravelDamage = poisonActionSlowTotal = 0;
        }

        public DamageResult ApplyTravelHazard() => poisonTravelDamage <= 0f ? default : Player.TakeEnvironmentalDamage(poisonTravelDamage);
    }

    public sealed class PerkSystem
    {
        private readonly List<PerkData> allPerks;
        public static bool IsAvailable(PerkData perk) => perk != null;
        public static bool IsAvailable(PerkData perk, RunState run) => IsAvailable(perk) && (perk.scope == RelicScope.Shared || perk.exclusiveCharacter == run.Player.Data.character);
        public PerkSystem(IEnumerable<PerkData> perks) => allPerks = perks.Where(IsAvailable).ToList();
        public IReadOnlyList<PerkData> GetChoices(RunState run, int seed)
        {
            var random = new System.Random(seed + run.Level * 1009 + run.ActivePerks.Count * 71);
            var buckets = allPerks.Distinct()
                .Where(perk => IsAvailable(perk, run) && !run.ActivePerks.Any(active => active.Data == perk && !active.CanStack))
                .GroupBy(perk => perk.offerGroup != null ? (UnityEngine.Object)perk.offerGroup : perk)
                .Select(group => new { Members = group.ToArray(), Weight = group.Key is PerkOfferGroup data ? data.weight : 1f })
                .Where(group => group.Weight > 0);
            // A weighted permutation of buckets, followed by one uniform member from each chosen bucket.
            // More members expand a group's variety, never its total chance or number of offer slots.
            return buckets.OrderBy(group => -Math.Log(1 - random.NextDouble()) / group.Weight).Take(3)
                .Select(group => group.Members[random.Next(group.Members.Length)]).ToList();
        }
        public void Apply(RunState run, PerkData perk, bool consumePendingChoice = true)
        {
            if (!IsAvailable(perk, run)) return;
            var existing = run.ActivePerks.FirstOrDefault(active => active.Data == perk);
            if (existing != null && !existing.CanStack) return;
            if (existing != null) existing.AddStack(); else run.ActivePerks.Add(new PerkRuntime(perk));
            if (existing == null) run.RegisterPerkAbilities(perk);
            run.Player.ApplyModifiers(perk.modifiers);
            if (consumePendingChoice) run.ConsumePendingPerkChoice();
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
        public bool TryPurchase(RunState run, ShopOffer offer, PerkSystem perks)
        {
            if (!CanPurchase(run, offer) || perks == null) return false;
            if (offer.Perk != null) perks.Apply(run, offer.Perk, false);
            else if (!equipment.Apply(run, offer.Item)) return false;
            run.SpendGold(offer.Price); offer.Purchased = true; return true;
        }
        public bool CanPurchase(RunState run, ShopOffer offer) => offer != null && UnavailableReason(run, offer) == null;
        public string UnavailableReason(RunState run, ShopOffer offer)
        {
            if (run == null || offer == null || offer.Perk == null && offer.Item == null) return GameText.Get("ui.shop.unavailablelabel");
            if (offer.Purchased) return GameText.Get("ui.shop.soldout");
            if (offer.Perk != null)
            {
                if (!PerkSystem.IsAvailable(offer.Perk, run) || run.ActivePerks.Any(active => active.Data == offer.Perk && !active.CanStack)) return GameText.Get("ui.shop.owned");
            }
            else
            {
                var item = offer.Item;
                if (!item.IsPotion && run.EquippedItems.Contains(item)) return GameText.Get("ui.shop.owned");
                if (item.grantedSpell != null && run.RuntimeFor(item.grantedSpell) != null) return GameText.Get("ui.shop.known");
                if (item.IsPotion && !run.HasPotionSpace) return GameText.Get("ui.shop.potionfull");
            }
            return run.Gold < offer.Price ? GameText.Get("ui.shop.notenoughgold") : null;
        }
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

        public string UnavailableReason(RunState run, EquipmentData item)
        {
            if (run == null || item == null) return string.Empty;
            purchaseCounts.TryGetValue(item.id, out var purchased);
            if (purchased >= item.maxPurchaseCount) return GameText.Get("ui.shop.soldout");
            if (!item.IsPotion && run.EquippedItems.Contains(item)) return GameText.Get("ui.shop.owned");
            if (item.grantedSpell != null && run.RuntimeFor(item.grantedSpell) != null) return GameText.Get("ui.shop.known");
            if (item.IsPotion && !run.HasPotionSpace) return GameText.Get("ui.shop.potionfull");
            if (run.Gold < item.price) return GameText.Get("ui.shop.notenoughgold");
            return null;
        }

        private static bool CanPurchase(RunState run, EquipmentData item, int purchased)
        {
            if (run == null || item == null || purchased >= item.maxPurchaseCount || run.Gold < item.price) return false;
            if (item.grantedSpell != null && run.RuntimeFor(item.grantedSpell) != null) return false;
            return item.IsPotion ? run.HasPotionSpace : !run.EquippedItems.Contains(item);
        }
    }
}
