using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonSlash
{
    [Serializable]
    public sealed class GrowthOpportunity
    {
        public string source;
        [Min(0)] public float opportunities = 1;
        [Range(0, 1)] public float acquisitionRate = 1;
        [Min(0)] public float unitsPerAcquisition = 1;
        [TextArea] public string assumption;
        public float ExpectedUnits => opportunities * acquisitionRate * unitsPerAcquisition;
    }
    [Serializable]
    public sealed class GrowthValuation
    {
        public LocalizedData content;
        [Min(0)] public float estimatedUnits;
        public float offenseUnits;
        public float healthUnits;
        public float guardUnits;
        public float utilityUnits;
        [Range(0, 1)] public float assumedUptime = 1;
        public bool needsDesignerReview;
        [TextArea] public string basis;
    }
    public enum GrowthScenario { UnderBudget, Required, Strong }

    public enum GrowthCheckpoint { Opening, Middle, PreBoss }
    [Serializable] public sealed class GrowthPerkGrant
    {
        public PerkData perk;
        [Min(1)] public int stacks = 1;
        public bool fromEvent;
    }
    [Serializable] public sealed class GrowthReferenceBuild
    {
        public string label;
        public GrowthCheckpoint checkpoint;
        public GrowthScenario scenario;
        [TextArea] public string acquisitionBasis;
        [Min(0)] public int experience;
        [Min(0)] public int shopGold;
        [Range(1,100)] public float fatigue = 100;
        public List<GrowthPerkGrant> perks = new();
        public List<EquipmentData> relics = new();
        public void Apply(RunState run)
        {
            run.GainExperience(experience);
            var system = new PerkSystem(perks.ConvertAll(p => p.perk));
            foreach (var grant in perks)
                for (var i = 0; i < grant.stacks; i++) system.Apply(run, grant.perk);
            foreach (var relic in relics) run.TryEquip(relic);
        }
    }

    [CreateAssetMenu(menuName = "Dungeon Slash/Balance/Act Growth Budget")]
    public sealed class ActGrowthBudget : ScriptableObject
    {
        [Range(1, 3)] public int act = 1;
        [TextArea] public string designIntent;
        public StageSetData stage;
        [Header("Pre-boss route assumptions, not random-run measurements")]
        [Min(0)] public int normalCombatOpportunities = 8;
        [Min(0)] public int eliteCombatOpportunities = 1;
        [Min(0)] public int goddessOpportunities = 1;
        [Min(0)] public float inheritedExperience;
        [Min(0)] public float expectedExperience;
        [Min(0)] public int cumulativeLevelUpPerks;
        [Min(0)] public float requiredUnits = 5;
        [Min(0)] public float requiredOffenseUnits = 3.5f;
        [Min(0)] public float requiredHealthUnits = 1;
        [Header("Actual authored rewards; no normalized stat injection")]
        public List<GrowthReferenceBuild> referenceBuilds = new();
        [Range(0, 1)] public float referenceOffenseAllocation = .8f;
        [Min(0)] public float damageBonusPerOffenseUnit = .1f;
        [Min(0)] public float healthPerDefenseUnit = 10;
        [Range(.1f, 1)] public float underBudgetRatio = .65f;
        [Min(1)] public float strongRatio = 1.35f;
        public List<GrowthOpportunity> opportunities = new();
        [Header("Item valuation assumptions")]
        [Range(0, 1)] public float normalDamageShare = .7f;
        [Range(0, 1)] public float breakFollowupShare = .25f;
        [Min(1)] public float referenceFightSeconds = 20;
        public List<GrowthValuation> valuations = new();
        public float AvailableExpectedUnits { get { var total = 0f; foreach (var row in opportunities) total += row.ExpectedUnits; return total; } }
        public float UnitsFor(GrowthScenario scenario) => requiredUnits * (scenario == GrowthScenario.UnderBudget ? underBudgetRatio : scenario == GrowthScenario.Strong ? strongRatio : 1);
        public void ApplyReferenceBuild(RunState run, GrowthScenario scenario)
        {
            var units = UnitsFor(scenario);
            // Test-only normalized purchasing power; live runs still gain actual authored perks and relics.
            var offense = units * referenceOffenseAllocation * damageBonusPerOffenseUnit;
            run.Player.ApplyModifiers(new[] { new StatModifier { kind = ModifierKind.NormalDamage, value = offense },
                new StatModifier { kind = ModifierKind.ChargeDamage, value = offense },
                new StatModifier { kind = ModifierKind.MaxHp, value = units * (1 - referenceOffenseAllocation) * healthPerDefenseUnit } });
        }
    }
}
