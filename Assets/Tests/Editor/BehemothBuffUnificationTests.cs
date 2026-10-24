using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DungeonSlash.Tests
{
    public sealed class BehemothBuffUnificationTests
    {
        private RunBalanceSettings balance;
        private RunState run;
        private static BuffData State(string id) => AssetDatabase.LoadAssetAtPath<BuffData>("Assets/Data/Combat/Buffs/" + id + ".asset");
        [SetUp] public void Setup()
        {
            balance = ScriptableObject.CreateInstance<RunBalanceSettings>();
            var content = AssetDatabase.LoadAssetAtPath<GameContent>("Assets/Data/GameContent.asset");
            var player = content.characters.Single(character => character.character == CharacterKind.Barbarian);
            run = new RunState(player, balance); run.TryEquip(player.startingRelic); run.BeginCombat();
        }
        [TearDown] public void Cleanup() => Object.DestroyImmediate(balance);

        [TestCase(39.9f)] [TestCase(40f)] [TestCase(48f)] [TestCase(77.25f)] [TestCase(100f)]
        public void ChargeTransfersExactlyTheAuthoredFuryStacks_At40OrMore(float amount)
        {
            run.Buffs.Add(State("Fury"), amount);
            run.Player.PrepareAttack(SwipeDirection.Up, true);
            if (amount < 40)
            {
                Assert.That(run.Buffs.Has(State("Rage")), Is.False);
                Assert.That(run.Buffs.Find(State("Fury")).StackCount, Is.EqualTo(amount).Within(.0001f));
            }
            else
            {
                Assert.That(run.Buffs.Has(State("Fury")), Is.False);
                Assert.That(run.Buffs.Find(State("Rage")).StackCount, Is.EqualTo(amount).Within(.0001f));
                run.Buffs.Tick(0); Assert.That(run.Buffs.Find(State("Rage")).StackCount, Is.EqualTo(amount).Within(.0001f), "OnBuffStart must never refill Rage to 100.");
                Assert.That(run.Player.NormalAttackDamage, Is.EqualTo(run.Player.Data.baseAttackDamage * 1.3f).Within(.001f));
                Assert.That(run.Player.StatusText, Does.Contain("분노")); Assert.That(run.Player.StatusText, Does.Not.Contain("격노"));
            }
        }

        [Test] public void NormalsGrant12Stacks_NotAnAlternatePrivateMeter_AndIdleDecayKeepsFractions()
        {
            for (var attack = 0; attack < 4; attack++) run.Player.PrepareAttack(SwipeDirection.Right, false);
            run.Buffs.UpdateState(0); Assert.That(run.Buffs.Find(State("Fury")).StackCount, Is.EqualTo(48));
            run.Player.Tick(.5f); run.Buffs.UpdateState(0);
            Assert.That(run.Buffs.Find(State("Fury")).StackCount, Is.EqualTo(47).Within(.0001f));
            run.Player.PrepareAttack(SwipeDirection.Up, true);
            Assert.That(run.Buffs.Find(State("Rage")).StackCount, Is.EqualTo(47).Within(.0001f));
        }

        [Test] public void RageDrains25PerSecond_ExpiresAtZero_AndCannotBeRefilledByBaselineNormals()
        {
            run.Buffs.Add(State("Fury"), 48); run.Player.PrepareAttack(SwipeDirection.Up, true); run.Buffs.Tick(0);
            run.Player.Tick(1); run.Buffs.UpdateState(0);
            Assert.That(run.Buffs.Find(State("Rage")).StackCount, Is.EqualTo(23).Within(.001f));
            run.Player.PrepareAttack(SwipeDirection.Right, false); run.Buffs.UpdateState(0);
            Assert.That(run.Buffs.Find(State("Rage")).StackCount, Is.EqualTo(23).Within(.001f));
            run.Player.Tick(.95f); run.Buffs.Tick(0);
            Assert.That(run.Buffs.Has(State("Rage")), Is.False);
            Assert.That(run.Player.NormalAttackDamage, Is.EqualTo(run.Player.Data.baseAttackDamage));
            run.Player.PrepareAttack(SwipeDirection.Left, false); run.Buffs.UpdateState(0);
            Assert.That(run.Buffs.Find(State("Fury")).StackCount, Is.EqualTo(12));
            run.Player.EndCharacterCombat(); Assert.That(run.Buffs.Has(State("Fury")), Is.False);
        }

        [Test] public void CalculatorCanReadAnyNamedBuff_ThenGrantExactFractionalStacksBeforeRemovingSource()
        {
            var a = ScriptableObject.CreateInstance<BuffData>(); var b = ScriptableObject.CreateInstance<BuffData>(); var item = ScriptableObject.CreateInstance<EquipmentData>();
            a.name = "Source stacks"; b.name = "Destination stacks";
            a.DuplicationData = b.DuplicationData = new() { Type = BuffDuplicationType.StackCount, MaxStackCount = 200 };
            try
            {
                run.Buffs.Add(a, 37.25f); run.Buffs.UpdateState(0);
                item.abilities.Add(new EquipmentAbility { trigger = new() { triggerType = TriggerType.OnGuardStarted, triggerCount = 1, chancePercent = 100 }, target = TargetType.Owner,
                    effects = new() {
                        new EquipmentEffect { effectType = EffectType.AddBuff, buff = b, effectCount = 1, valueVariables = new() { new() { type = EffectScale.BuffStacks, buff = a, multiplier = 1 } } },
                        new EquipmentEffect { effectType = EffectType.RemoveBuff, buff = a, effectCount = 1 }
                    } });
                run.TryEquip(item); run.EmitEvent(TriggerType.OnGuardStarted, default); run.Buffs.UpdateState(0);
                Assert.That(run.Buffs.Has(a), Is.False); Assert.That(run.Buffs.Find(b).StackCount, Is.EqualTo(37.25f));
                var condition = new AbilityCondition { kind = AbilityConditionKind.BuffStack, buff = b, threshold = 37.25f };
                Assert.That(condition.Matches(run, default), Is.True);
                Assert.That(new AbilityCondition { kind = AbilityConditionKind.BuffStack, buff = a, threshold = 0, comparator = Comparator.Equal }.Matches(run, default), Is.True);
            }
            finally { Object.DestroyImmediate(a); Object.DestroyImmediate(b); Object.DestroyImmediate(item); }
        }

        [Test] public void ZeroStackGrantDoesNotCreateOneFreeStack_AndProductionHasNoRemovedTypesOrEnumChoices()
        {
            run.ApplyAbilityEffect(new EquipmentEffect { effectType = EffectType.AddBuff, buff = State("Rage"), effectCount = 1 }, default);
            run.Buffs.UpdateState(0); Assert.That(run.Buffs.Has(State("Rage")), Is.False);
            Assert.That(typeof(BuffData).Assembly.GetType("DungeonSlash.FormData"), Is.Null);
            Assert.That(typeof(BuffData).Assembly.GetType("DungeonSlash.CombatResourceData"), Is.Null);
            Assert.That(typeof(PlayerCombatData).GetField("forms"), Is.Null); Assert.That(typeof(PlayerCombatData).GetField("resources"), Is.Null);
            Assert.That(Enum.GetNames(typeof(EffectType)).Any(name => name.Contains("Resource") || name.Contains("Form")), Is.False);
            Assert.That(Enum.GetNames(typeof(EffectScale)).Contains("Resource"), Is.False);
        }
    }
}
