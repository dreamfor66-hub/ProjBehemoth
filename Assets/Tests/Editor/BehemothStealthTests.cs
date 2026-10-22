using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DungeonSlash.Tests
{
    public sealed class BehemothStealthTests
    {
        private GameContent Content => AssetDatabase.LoadAssetAtPath<GameContent>("Assets/Data/Behemoth/GameContent.asset");
        private RunBalanceSettings balance;
        [SetUp] public void Setup() => balance = ScriptableObject.CreateInstance<RunBalanceSettings>();
        [TearDown] public void Cleanup() => Object.DestroyImmediate(balance);
        private RunState Run(CharacterKind kind)
        {
            var data = Content.characters.Single(character => character.character == kind);
            var run = new RunState(data, balance); run.TryEquip(data.startingRelic); run.BeginCombat(); return run;
        }

        [Test] public void Detection_RollsAfterActions_IncreasesBy25Points_AndFourthAlwaysReveals()
        {
            var awareness = new EncounterAwarenessRuntime(); var reveals = 0; awareness.Revealed += () => reveals++;
            awareness.Begin(true, .25f);
            for (var action = 1; action <= 4; action++)
            {
                Assert.That(awareness.NextDetectionChance, Is.EqualTo(action * .25f));
                awareness.CompleteAction(.999999f);
                Assert.That(awareness.IsConcealed, Is.EqualTo(action < 4));
            }
            awareness.Reveal(); Assert.That(reveals, Is.EqualTo(1));
            awareness.Begin(true, .25f); awareness.CompleteAction(.2499f); Assert.That(awareness.IsConcealed, Is.False);
            awareness.Begin(true, .25f); awareness.CompleteAction(.25f); Assert.That(awareness.IsConcealed, Is.True);
            awareness.CompleteAction(.4999f); Assert.That(awareness.IsConcealed, Is.False);
            awareness.Begin(true, .25f); awareness.CompleteAction(.9f); awareness.CompleteAction(.9f);
            awareness.CompleteAction(.7499f); Assert.That(awareness.IsConcealed, Is.False);
        }

        [Test] public void AnyMemberHit_RevealsPack_EvenLethalOrGuarded_AndPreservesTimers()
        {
            var run = Run(CharacterKind.Rogue);
            var data = AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/DungeonSlashPrototype/Monsters/Data_Monster_B1_Minion_Skeleton.asset");
            var a = new MonsterRuntime(data) { Awareness = run.Awareness };
            var b = new MonsterRuntime(data) { Awareness = run.Awareness }; b.DelayActions(.75f);
            var normal = b.NormalAttackTimer; var charge = b.ChargeTimer;
            a.Tick(30, Vector2.zero); b.Tick(30, Vector2.zero);
            Assert.That(a.ShouldNormalAttack || a.ShouldCharge, Is.False);
            Assert.That(b.NormalAttackTimer, Is.EqualTo(normal)); Assert.That(b.ChargeTimer, Is.EqualTo(charge));
            a.TakeDamage(a.MaxHp);
            Assert.That(a.IsAlive, Is.False); Assert.That(b.IsUnaware, Is.False);
            b.Tick(.1f, Vector2.zero); Assert.That(b.NormalAttackTimer, Is.EqualTo(normal - .1f).Within(.0001f));
            run.BeginCombat(); Assert.That(b.IsUnaware, Is.True);
            b.NotifyAttacked(); Assert.That(run.Awareness.IsConcealed, Is.False);
        }

        [TestCase(CharacterKind.Warrior)] [TestCase(CharacterKind.Barbarian)] [TestCase(CharacterKind.Samurai)]
        [TestCase(CharacterKind.Monk)] [TestCase(CharacterKind.Wizard)]
        public void OtherClasses_KeepIdenticalTimersAndStatus_NoDetectionRandomConsumption(CharacterKind kind)
        {
            var run = Run(kind); Assert.That(run.Awareness.IsConcealed, Is.False);
            var data = AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/DungeonSlashPrototype/Monsters/Data_Monster_B1_Minion_Skeleton.asset");
            var connected = new MonsterRuntime(data) { Awareness = run.Awareness }; var baseline = new MonsterRuntime(data);
            for (var frame = 0; frame < 500; frame++) { connected.Tick(.016f, Vector2.zero); baseline.Tick(.016f, Vector2.zero); }
            Assert.That(connected.NormalAttackTimer, Is.EqualTo(baseline.NormalAttackTimer));
            Assert.That(connected.ChargeTimer, Is.EqualTo(baseline.ChargeTimer));
            Assert.That(connected.ShouldNormalAttack, Is.EqualTo(baseline.ShouldNormalAttack));
            Assert.That(connected.ShouldCharge, Is.EqualTo(baseline.ShouldCharge));
            Assert.That(run.Player.StatusText, Does.Not.Contain("은신"));
            var before = Random.state; run.CompleteAction(TriggerType.OnAttack, default);
            Assert.That(Random.state, Is.EqualTo(before));
        }

        [Test] public void StarterIsEditableRules_NoPoison_StatsUnchanged_ConditionsAndRevealTriggerCompose()
        {
            var run = Run(CharacterKind.Rogue); var player = run.Player;
            Assert.That(run.Rule(AbilityRule.EncounterConcealment), Is.EqualTo(1));
            Assert.That(run.Awareness.NextDetectionChance, Is.EqualTo(.25f));
            Assert.That(player.MaxHp, Is.EqualTo(75)); Assert.That(player.ShieldMax, Is.EqualTo(18));
            Assert.That(player.NormalAttackDamage, Is.EqualTo(5.5f)); Assert.That(player.ChargeAttackDamage, Is.EqualTo(17));
            Assert.That(player.CurrentCooldown, Is.EqualTo(.48f)); Assert.That(player.CurrentChargeDuration, Is.EqualTo(.65f));
            Assert.That(player.Data.startingRelic.abilities.SelectMany(unit => unit.effects).All(effect => effect.effectType == EffectType.RuleModifier), Is.True);
            Assert.That(new AbilityCondition { kind = AbilityConditionKind.OwnerConcealed }.Matches(run, default), Is.True);
            var item = ScriptableObject.CreateInstance<EquipmentData>();
            try
            {
                item.abilities.Add(new EquipmentAbility { trigger = new EquipmentTrigger { triggerType = TriggerType.OnEncounterRevealed, chancePercent = 100, triggerCount = 1 },
                    target = TargetType.Owner, effects = new() { new EquipmentEffect { effectType = EffectType.Heal, effectMagnitude = 2, effectCount = 1 } } });
                run.TryEquip(item); player.TakeEnvironmentalDamage(10); var hp = player.CurrentHp;
                run.Awareness.Reveal(); Assert.That(player.CurrentHp, Is.EqualTo(hp + 2));
                run.CompleteAction(TriggerType.OnAttack, default);
                Assert.That(new AbilityCondition { kind = AbilityConditionKind.EncounterActions, threshold = 1 }.Matches(run, default), Is.True);
                player.EndCharacterCombat(); Assert.That(run.Awareness.IsActive, Is.False);
                run.BeginCombat(); Assert.That(run.Awareness.IsConcealed, Is.True); Assert.That(run.Awareness.CompletedActions, Is.Zero);
            }
            finally { Object.DestroyImmediate(item); }
        }
    }
}
