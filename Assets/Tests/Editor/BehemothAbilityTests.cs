using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
namespace DungeonSlash.Tests
{
    public sealed class BehemothAbilityTests
    {
        private GameContent Content => AssetDatabase.LoadAssetAtPath<GameContent>("Assets/Data/GameContent.asset");
        private RunBalanceSettings balance;
        [SetUp] public void Setup() => balance = ScriptableObject.CreateInstance<RunBalanceSettings>();
        [TearDown] public void Cleanup() => Object.DestroyImmediate(balance);
        private RunState Run(CharacterKind kind)
        { var data = Content.characters.Single(item => item.character == kind); var run = new RunState(data, balance); run.TryEquip(data.startingRelic); run.BeginCombat(); return run; }
        [Test] public void EveryProductionEquipmentUsesEditableAbilityList_NoPowerDispatchInCombat()
        {
            Assert.That(Content.equipment.All(item => item.abilities.Count > 0), Is.True);
            foreach (var path in System.IO.Directory.GetFiles("Assets/Scripts/Combat", "*.cs", System.IO.SearchOption.AllDirectories))
            {
                var source = System.IO.File.ReadAllText(path);
                Assert.That(source.Contains("HasPower(RelicPower."), Is.False, path);
                Assert.That(source.Contains("case RelicPower."), Is.False, path);
            }
            Assert.That(Content.perks.All(PerkSystem.IsAvailable), Is.True);
        }
        [Test] public void NewUnnamedItemCanComposeTriggersEffectsAndRules_WithoutAnyPowerEnum()
        {
            var item = ScriptableObject.CreateInstance<EquipmentData>(); item.scope = RelicScope.Shared;
            var onHit = new EquipmentAbility { trigger = new EquipmentTrigger { chancePercent = 100,  triggerType = TriggerType.OnHit, triggerCount = 2 }, target = TargetType.Owner };
            onHit.effects.Add(new EquipmentEffect { effectType = EffectType.RestoreGuard, effectMagnitude = 3, effectCount = 1 });
            onHit.effects.Add(new EquipmentEffect { effectType = EffectType.Heal, effectMagnitude = 2, effectCount = 1 });
            item.abilities.Add(onHit);
            var run = Run(CharacterKind.Rogue); run.Player.TakeEnvironmentalDamage(10); run.Player.TrySetGuard(true); run.Player.Tick(.3f); new DamageResolver().ApplyToPlayer(run.Player, new DamageRequest(DamageType.MonsterAttack, 5, 5, true)); run.Player.TrySetGuard(false);
            run.TryEquip(item); var hp = run.Player.CurrentHp; var guard = run.Player.ShieldCurrent;
            run.EmitEvent(TriggerType.OnHit, default); Assert.That(run.Player.CurrentHp, Is.EqualTo(hp));
            run.EmitEvent(TriggerType.OnHit, default); Assert.That(run.Player.CurrentHp, Is.EqualTo(hp + 2)); Assert.That(run.Player.ShieldCurrent, Is.EqualTo(guard + 3));
            // Editing an effect value changes runtime behavior; the ID, name and Power are all unset.
            var effect = onHit.effects[0]; effect.effectMagnitude = 1; onHit.effects[0] = effect;
            run.EmitEvent(TriggerType.OnHit, default); run.EmitEvent(TriggerType.OnHit, default); Assert.That(run.Player.ShieldCurrent, Is.EqualTo(guard + 4));
            run.TryRemoveEquipment(item); run.EmitEvent(TriggerType.OnHit, default); run.EmitEvent(TriggerType.OnHit, default); Assert.That(run.Player.ShieldCurrent, Is.EqualTo(guard + 4));
            Object.DestroyImmediate(item);
        }
        [Test] public void SpellIntroduction_AllRewardSources_NoEarlyBooks_LateActOneRare_ActTwoNormal()
        {
            var previous = RunSession.SelectedCharacter; RunSession.SelectedCharacter = null;
            var go = new GameObject("Progression pool"); var controller = go.AddComponent<RunController>();
            try
            {
                controller.Configure(Content.characters.Single(item => item.character == CharacterKind.Warrior), balance, Content.perks, Content.equipment); controller.BeginRun();
                int CountBooks(int act, int normals)
                {
                    // These are new merchants in a new progression scenario, not a restock
                    // of the same cached merchant after the character's progress changes.
                    controller.BeginRun();
                    controller.State.CurrentAct = act; controller.State.NormalCombatsInAct = normals; var count = 0;
                    for (var i = 0; i < 250; i++)
                    {
                        count += controller.GetRelicChoices(i * 739, i).Count(item => item.grantedSpell != null);
                        count += controller.GetShopInventory(i * 739, i).Count(item => item.grantedSpell != null);
                        if (controller.GetChestRelic(i * 739, i)?.grantedSpell != null) count++;
                        if (act == 1 && normals < 4) Assert.That(controller.GetExclusiveChoices(i).All(item => item.grantedSpell == null), Is.True);
                    }
                    return count;
                }
                Assert.That(CountBooks(1, 0), Is.Zero); var late = CountBooks(1, 4); var normal = CountBooks(2, 0);
                Assert.That(late, Is.GreaterThan(0)); Assert.That(normal, Is.GreaterThan(late * 2));
                TestContext.WriteLine($"Spellbook offers / 1250 slots: early=0, late B1={late}, B2={normal}");
            }
            finally { Object.DestroyImmediate(go); RunSession.SelectedCharacter = previous; }
        }
        [Test] public void EventsHaveDirectReferencesDistinctAlternativesAndNoDeferredPurchaseFallback()
        {
            foreach (var story in Content.events)
            {
                Assert.That(story.referenceUrl, Does.StartWith("https://op.gg/ko/slay-the-spire2/events/"), story.id);
                Assert.That(story.referenceExperience, Is.Not.Null.And.Not.Empty, story.id);
                Assert.That(story.options.All(option => option.effects.All(effect => effect.kind != EventEffectKind.Burden && effect.kind != EventEffectKind.GoldDebt)), Is.True, story.id);
                Assert.That(story.options.Select(option => string.Join(",", option.effects.Select(effect => effect.kind + ":" + effect.amount + ":" + effect.relic?.id))).Distinct().Count(), Is.GreaterThanOrEqualTo(2), story.id);
            }
        }
        [Test] public void EachClassHasMagicRoute_AndWizardHasMeleeDefenseRoute()
        {
            foreach (var kind in Enum.GetValues(typeof(CharacterKind)).Cast<CharacterKind>())
                Assert.That(Content.equipment.Any(item => item.scope == RelicScope.Exclusive && item.exclusiveCharacter == kind && item.grantedSpell != null), Is.True, kind.ToString());
            var staff = Content.equipment.Single(item => item.power == RelicPower.StaffConduit);
            Assert.That(staff.abilities.Any(ability => ability.trigger.triggerType == TriggerType.OnHit && ability.trigger.triggerAttackTypeFilter == TriggerAttackTypeFilter.ChargeAttack), Is.True);
            Assert.That(staff.abilities.Any(ability => ability.trigger.triggerType == TriggerType.OnWeakPointBreak), Is.True);
        }
        [Test] public void WizardAndMonkRestoreGuardThroughCoreDefensiveLoop()
        {
            foreach (var kind in new[] { CharacterKind.Wizard, CharacterKind.Monk })
            {
                var run = Run(kind); run.Player.TrySetGuard(true); run.Player.Tick(.3f); new DamageResolver().ApplyToPlayer(run.Player, new DamageRequest(DamageType.MonsterAttack, 8, 8, true)); run.Player.TrySetGuard(false); var shield = run.Player.ShieldCurrent;
                run.EmitEvent(kind == CharacterKind.Wizard ? TriggerType.OnSpellCast : TriggerType.OnAttackBuffUsed, default);
                var expected=run.Player.Data.startingRelic.abilities.Where(a=>a.trigger.triggerType==(kind==CharacterKind.Wizard?TriggerType.OnSpellCast:TriggerType.OnAttackBuffUsed)).SelectMany(a=>a.effects).Where(e=>e.effectType==EffectType.RestoreGuard).Sum(e=>e.effectMagnitude);
                Assert.That(run.Player.ShieldCurrent, Is.EqualTo(shield + expected).Within(.001), kind.ToString());
            }
        }
        [Test] public void OpeningSpellsRequirePreparation_AndTwoBaseBoltsDoNotKillMazeRat()
        {
            var wizard = Run(CharacterKind.Wizard); var spell = wizard.Spellbook.Single();
            Assert.That(spell.Cooldown, Is.GreaterThan(0));
            var rat = AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/Combat/Monsters/TunnelRat.asset");
            Assert.That(spell.Data.damage * 2, Is.LessThan(rat.maxHp));
        }
        [Test] public void ElementConditionsApplyToTheSpellBeingPrepared_NotThePreviousAction()
        {
            var item = ScriptableObject.CreateInstance<EquipmentData>();
            var ability = new EquipmentAbility { trigger = new EquipmentTrigger { chancePercent = 100,  triggerType = TriggerType.Passive }, target = TargetType.Owner };
            ability.conditions.Add(new AbilityCondition { kind = AbilityConditionKind.ElementIs, element = SpellElement.Fire });
            ability.effects.Add(new EquipmentEffect { effectType = EffectType.RuleModifier, rule = AbilityRule.CastTimeReduction, effectMagnitude = .4f, effectCount = 1 });
            ability.effects.Add(new EquipmentEffect { effectType = EffectType.RuleModifier, rule = AbilityRule.SpellCooldownReduction, effectMagnitude = .3f, effectCount = 1 });
            item.abilities.Add(ability); var run = Run(CharacterKind.Warrior); run.TryEquip(item);
            var fire = Content.spells.Single(spell => spell.id == "Ember"); var ice = Content.spells.Single(spell => spell.id == "EnergyBolt");
            Assert.That(run.Player.SpellCastingFor(fire), Is.EqualTo(.6f).Within(.001)); Assert.That(run.Player.SpellCooldownFor(fire), Is.EqualTo(.7f).Within(.001));
            Assert.That(run.Player.SpellCastingFor(ice), Is.EqualTo(1)); Assert.That(run.Player.SpellCooldownFor(ice), Is.EqualTo(1));
            Object.DestroyImmediate(item);
        }
        [Test] public void MaximumHealthEventCostOnlyClampsCapacity_NeverKillsAWoundedPlayer()
        {
            var run = Run(CharacterKind.Warrior); run.Player.TakeEnvironmentalDamage(run.Player.CurrentHp - 1);
            Assert.That(RandomEventSystem.Pay(new EventOption { maxHpCost = 18 }, run), Is.True);
            Assert.That(run.Player.MaxHp, Is.EqualTo(82)); Assert.That(run.Player.CurrentHp, Is.EqualTo(1));
        }
    }
}
