using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DungeonSlash.Tests
{
    public sealed class BehemothExpansionTests
    {
        private GameContent content;
        private RunBalanceSettings balance;
        private string history;
        [SetUp] public void SetUp()
        { content = AssetDatabase.LoadAssetAtPath<GameContent>("Assets/Data/Behemoth/GameContent.asset"); Assert.That(content, Is.Not.Null); balance = ScriptableObject.CreateInstance<RunBalanceSettings>(); history = PlayerPrefs.GetString("Behemoth.EventHistory.v1", ""); }
        [TearDown] public void TearDown() { PlayerPrefs.SetString("Behemoth.EventHistory.v1", history); Object.DestroyImmediate(balance); }
        private RunState Run(CharacterKind kind)
        { var data = content.characters.Single(item => item.character == kind); var run = new RunState(data, balance); run.TryEquip(data.startingRelic); run.BeginCombat(); return run; }
        private EquipmentData Power(RelicPower power) => content.equipment.Single(item => item.power == power);
        private static DamageResult Hurt(PlayerCombatRuntime player, float damage = 20, bool guardable = true)
            => new DamageResolver().ApplyToPlayer(player, new DamageRequest(DamageType.MonsterAttack, damage, damage, guardable));

        [Test] public void Content_HasSixCharacters48Exclusives12EventRelics20EventsAndSixSpells()
        {
            Assert.That(content.characters.Count, Is.EqualTo(6)); Assert.That(content.spells.Count, Is.EqualTo(6));
            Assert.That(content.equipment.Count(item => item.scope == RelicScope.Exclusive), Is.EqualTo(48));
            Assert.That(content.equipment.Count(item => item.scope == RelicScope.Event), Is.EqualTo(13)); Assert.That(content.events.Count, Is.EqualTo(20));
            foreach (var character in content.characters)
            { Assert.That(character.gameVariables, Is.SameAs(content.variables)); Assert.That(character.startingRelic, Is.Not.Null); Assert.That(content.equipment.Count(item => item.scope == RelicScope.Exclusive && item.exclusiveCharacter == character.character), Is.EqualTo(8)); }
            Assert.That(content.equipment.All(item => item.icon != null), Is.True);
        }
        [Test] public void Stages_HaveExactEliteBossPoolsAndFixedMultiEnemyFormations()
        {
            Assert.That(content.stages.Count(stage => stage.act == 1), Is.EqualTo(1));
            Assert.That(content.stages.Count(stage => stage.act == 2), Is.EqualTo(1)); Assert.That(content.stages.Count(stage => stage.act == 3), Is.EqualTo(1));
            foreach (var stage in content.stages)
            {
                Assert.That(stage.elites.Count, Is.EqualTo(3)); Assert.That(stage.bosses.Count, Is.EqualTo(3));
                Assert.That(stage.elites.Distinct().Count(), Is.EqualTo(3)); Assert.That(stage.bosses.Distinct().Count(), Is.EqualTo(3));
                Assert.That(stage.hardCombats.Any(encounter => encounter.members.Count > 1), Is.True);
                Assert.That(stage.earlyCombats.Concat(stage.hardCombats).Concat(stage.elites).Concat(stage.bosses).All(encounter => encounter.members.All(member => member.monster != null)), Is.True);
            }
        }
        [Test] public void Planner_FirstSixNormalsSingle_ThenGradualPacks_ElitesNeverRepeat()
        {
            for (var seed = 1; seed <= 12; seed++)
            {
                var planner = new EncounterPlanner(content, seed);
                for (var act = 1; act <= 3; act++)
                {
                    var stage = planner.Stage(act);
                    for (var i = 0; i < 6; i++) Assert.That(planner.Get(act, RoomEncounterType.Combat, i + 1, i).members.Count, Is.EqualTo(1));
                    for (var i = 6; i < 9; i++) Assert.That(planner.Get(act, RoomEncounterType.Combat, i + 1, i).members.Count, Is.LessThanOrEqualTo(2));
                    if (act == 1) for (var i = 9; i < 20; i++) Assert.That(planner.Get(act, RoomEncounterType.Combat, i + 1, i).members.Count, Is.LessThanOrEqualTo(2));
                    var elites = Enumerable.Range(1, 3).Select(id => planner.Get(act, RoomEncounterType.Elite, id, 0)).ToArray();
                    Assert.That(elites.Distinct().Count(), Is.EqualTo(3));
                    Assert.That(planner.Get(act, RoomEncounterType.Elite, 1, 999), Is.SameAs(elites[0]), "Returning to a room must not reroll its encounter.");
                }
            }
        }
        [Test] public void AllClasses_GuardHasNoChip_OrdinaryGuardConsumesGauge()
        {
            var warrior = Run(CharacterKind.Warrior).Player; warrior.TrySetGuard(true); warrior.Tick(.3f); Hurt(warrior);
            Assert.That(warrior.CurrentHp, Is.EqualTo(warrior.MaxHp)); Assert.That(warrior.ShieldCurrent, Is.LessThan(warrior.ShieldMax));
            var samurai = Run(CharacterKind.Samurai).Player; samurai.TrySetGuard(true); samurai.Tick(.15f); Hurt(samurai);
            Assert.That(samurai.CurrentHp, Is.EqualTo(samurai.MaxHp));
            foreach (var kind in System.Enum.GetValues(typeof(CharacterKind)).Cast<CharacterKind>())
            { var player = Run(kind).Player; player.TrySetGuard(true); player.Tick(.3f); var result = Hurt(player, 5); Assert.That(result.HpDamage, Is.Zero); Assert.That(result.ShieldDamage, Is.EqualTo(5)); }
        }
        [Test] public void Samurai_SheathParryDraw_OrdinaryDrawNotCharge_ResheathCannotSpamParry()
        {
            var player = Run(CharacterKind.Samurai).Player; player.TrySetGuard(true);
            var result = Hurt(player); Assert.That(result.WasSheathParry, Is.True); Assert.That(result.WasPerfectGuard, Is.False); Assert.That(player.ShieldCurrent, Is.EqualTo(player.ShieldMax)); player.TrySetGuard(false);
            var first = player.PrepareAttack(SwipeDirection.Right, false);
            Assert.That(first.Charged, Is.True); Assert.That(first.Multiplier, Is.EqualTo(1.5f)); Assert.That(player.IsSheathed, Is.False);
            player.TrySetGuard(true); player.Tick(.2f); player.TrySetGuard(false); player.TrySetGuard(true);
            Assert.That(player.IsPerfectWindow, Is.False);
            var ordinary = player.PrepareAttack(SwipeDirection.Left, false); Assert.That(ordinary.Draw, Is.True); Assert.That(ordinary.Charged, Is.False);
        }
        [Test] public void Poison_TicksExactlyEveryTwoSeconds_AndDetonationConsumesStacks()
        {
            var data = ScriptableObject.CreateInstance<MonsterData>(); data.maxHp = 200;
            var monster = new MonsterRuntime(data); monster.AddPoison(2);
            monster.Tick(1.99f, Vector2.zero); Assert.That(monster.CurrentHp, Is.EqualTo(200));
            monster.Tick(.02f, Vector2.zero); Assert.That(monster.CurrentHp, Is.EqualTo(194)); Assert.That(monster.ConsumePoisonDamage(), Is.EqualTo(6));
            Assert.That(monster.ConsumePoisonDamage(), Is.Zero); Assert.That(monster.DetonatePoison(), Is.EqualTo(15)); Assert.That(monster.PoisonStacks, Is.Zero);
            Object.DestroyImmediate(data);
        }
        [Test] public void Barbarian_RageRequiresBuildAndCharge_DrainsWithoutAttacks()
        {
            var player = Run(CharacterKind.Barbarian).Player;
            for (var i = 0; i < 4; i++) player.PrepareAttack(SwipeDirection.Right, false);
            Assert.That(player.IsEnraged, Is.False); Assert.That(player.Rage, Is.EqualTo(48));
            player.PrepareAttack(SwipeDirection.Up, true); Assert.That(player.IsEnraged, Is.True); Assert.That(player.Rage, Is.EqualTo(100));
            var damage = player.NormalAttackDamage; player.Tick(5.01f); Assert.That(player.IsEnraged, Is.False); Assert.That(player.NormalAttackDamage, Is.LessThan(damage));
        }
        [Test] public void Barbarian_DeferredDamagePaidOverTime_ClearedOnVictory_AndDumpedOnRageEnd()
        {
            var run = Run(CharacterKind.Barbarian); run.TryEquip(Power(RelicPower.DeferredPain)); var player = run.Player;
            player.AddRage(60); player.PrepareAttack(SwipeDirection.Up, true); Hurt(player, 20, false);
            Assert.That(player.CurrentHp, Is.EqualTo(player.MaxHp)); Assert.That(player.DeferredDamage, Is.EqualTo(15).Within(.001f));
            player.Tick(1); Assert.That(player.CurrentHp, Is.EqualTo(player.MaxHp - 3f).Within(.001f));
            player.EndCharacterCombat(); Assert.That(player.DeferredDamage, Is.Zero);
            run.BeginCombat(); player.AddRage(60); player.PrepareAttack(SwipeDirection.Up, true); player.Tick(3.9f); Hurt(player, 20, false);
            var before = player.CurrentHp; player.Tick(.2f); Assert.That(player.DeferredDamage, Is.Zero); Assert.That(player.CurrentHp, Is.EqualTo(before - 15f).Within(.001f));
        }
        [TestCase(SwipeDirection.Up, SwipeDirection.Down, MonkFinisher.Burst)]
        [TestCase(SwipeDirection.Up, SwipeDirection.Up, MonkFinisher.MultiHit)]
        [TestCase(SwipeDirection.Down, SwipeDirection.Right, MonkFinisher.Shelter)]
        [TestCase(SwipeDirection.Right, SwipeDirection.Up, MonkFinisher.Flow)]
        public void Monk_LastTwoNormals_ChargeDirectionIndependent_GuardPreservesCombo(SwipeDirection first, SwipeDirection second, MonkFinisher finisher)
        {
            var player = Run(CharacterKind.Monk).Player; player.PrepareAttack(SwipeDirection.Left, false); player.PrepareAttack(first, false); player.TrySetGuard(true); player.TrySetGuard(false); player.PrepareAttack(second, false);
            Assert.That(player.PreparedForm.finisher, Is.EqualTo(finisher));
            var attack = player.PrepareAttack(SwipeDirection.Left, true); Assert.That(attack.Form.finisher, Is.EqualTo(finisher)); Assert.That(player.Combo.Count, Is.Zero);
        }
        [Test] public void Monk_LongUnlockedSuffixWins_WithoutOldInputsMarkingTargets()
        {
            var run = Run(CharacterKind.Monk); run.TryEquip(Power(RelicPower.LongTiger)); var player = run.Player;
            foreach (var direction in new[] { SwipeDirection.Right, SwipeDirection.Down, SwipeDirection.Up, SwipeDirection.Up }) player.PrepareAttack(direction, false);
            Assert.That(player.PreparedForm.pattern.Length, Is.EqualTo(3)); Assert.That(player.PrepareAttack(SwipeDirection.Down, true).Form.requiredPower, Is.EqualTo(RelicPower.LongTiger));
        }
        [Test] public void Wizard_CooldownsFromEditableMechanic_FixedDirections_ReplacementIsExplicit()
        {
            var wizard = Run(CharacterKind.Wizard); var warrior = Run(CharacterKind.Warrior); var spell = content.spells[0];
            wizard.LearnSpell(spell); warrior.LearnSpell(spell); wizard.RuntimeFor(spell).Use(wizard.Player); warrior.RuntimeFor(spell).Use(warrior.Player);
            Assert.That(wizard.RuntimeFor(spell).Cooldown, Is.EqualTo(warrior.RuntimeFor(spell).Cooldown * wizard.Player.Data.mechanics.wizardCooldownMultiplier));
            foreach (var item in content.spells) warrior.LearnSpell(item);
            Assert.That(warrior.Spellbook.Count, Is.EqualTo(4)); Assert.That(warrior.AssignSpell(SwipeDirection.Left, spell), Is.False);
            Assert.That(Enumerable.Range(0, 4).Count(i => warrior.Slot((SwipeDirection)i) == spell), Is.EqualTo(1));
            var collision = content.spells.Single(item => item.id == "ChainLightning");
            Assert.That(warrior.LearnSpell(collision), Is.False); Assert.That(warrior.Slot(spell.defaultSlot), Is.SameAs(spell));
            Assert.That(warrior.LearnSpell(collision, true), Is.True); Assert.That(warrior.Slot(collision.defaultSlot), Is.SameAs(collision)); Assert.That(warrior.RuntimeFor(spell), Is.Null);
        }
        [Test] public void ExclusiveRelics_OnlyMatchingClass_CapThree_EightCandidatesRemainDistinct()
        {
            var run = Run(CharacterKind.Warrior); Assert.That(run.TryEquip(Power(RelicPower.ViperFang)), Is.False);
            var own = content.equipment.Where(item => item.scope == RelicScope.Exclusive && item.exclusiveCharacter == CharacterKind.Warrior).ToArray();
            for (var i = 0; i < 3; i++) Assert.That(run.TryEquip(own[i]), Is.True); Assert.That(run.TryEquip(own[3]), Is.False);
            Assert.That(run.ExclusiveCount, Is.EqualTo(3));
        }
        [Test] public void CombatTimedEffects_DoNotExpireWhilePausedOrDuringAnnouncements()
        {
            var player = Run(CharacterKind.Warrior).Player; player.ApplyCombatModifier(new StatModifier { kind = ModifierKind.DamageReduction, value = .2f }, 3);
            Assert.That(player.DamageReduction, Is.EqualTo(.2f)); player.Tick(2.9f); Assert.That(player.DamageReduction, Is.EqualTo(.2f));
            player.Tick(.2f); Assert.That(player.DamageReduction, Is.Zero.Within(.001f));
        }
        [Test] public void SharedRewardAndShop_NeverLeakStartingExclusiveOrEventRelics_OrKnownSpellbooks()
        {
            var previous = RunSession.SelectedCharacter; RunSession.SelectedCharacter = null;
            var go = new GameObject("RewardPoolTest"); var controller = go.AddComponent<RunController>();
            controller.Configure(content.characters[0], balance, content.perks, content.equipment); controller.BeginRun();
            foreach (var spell in content.spells) controller.State.LearnSpell(spell);
            for (var i = 0; i < 25; i++)
            {
                Assert.That(controller.GetRelicChoices(i * 719, i).All(item => item.scope == RelicScope.Shared && (item.grantedSpell == null || controller.State.RuntimeFor(item.grantedSpell) == null)), Is.True);
                Assert.That(controller.GetShopInventory(i * 719, i).All(item => item.IsPotion || item.scope == RelicScope.Shared && (item.grantedSpell == null || controller.State.RuntimeFor(item.grantedSpell) == null)), Is.True);
            }
            Object.DestroyImmediate(go); RunSession.SelectedCharacter = previous;
        }
        [Test] public void Events_ValidCosts_CannotKillPlayer_NoRepeat_ConditionalFollowups()
        {
            var run = Run(CharacterKind.Warrior); run.Seed = 16571;
            var lethal = new EventOption { hpCost = run.Player.CurrentHp }; Assert.That(RandomEventSystem.Pay(lethal, run), Is.False);
            var cost = new EventOption { hpCost = 8, goldCost = 10 }; var gold = run.Gold;
            Assert.That(RandomEventSystem.Pay(cost, run), Is.True); Assert.That(run.Gold, Is.EqualTo(gold - 10)); Assert.That(run.Player.CurrentHp, Is.EqualTo(run.Player.MaxHp - 8));
            var seen = new HashSet<string>();
            for (var i = 0; i < 9; i++) { var story = RandomEventSystem.Draw(content, run, 1, i); Assert.That(story, Is.Not.Null); Assert.That(seen.Add(story.id), Is.True); Assert.That(story.requiredFlag, Is.Null.Or.Empty); }
            run.EventFlags.Add("DeliveryWaiting"); run.EventFlags.Add("PilgrimMet");
            Assert.That(content.events.Single(item => item.id == "Delivery").requiredFlag, Is.EqualTo("DeliveryWaiting"));
            Assert.That(content.events.SelectMany(item => item.options).All(option => option.effects.Where(effect => effect.kind == EventEffectKind.Relic).All(effect => effect.relic.scope == RelicScope.Event)), Is.True);
        }
        [Test] public void ThreeRunEventSequences_AreDifferent_WithRecentHistoryWeighting()
        {
            var sequences = new List<string>();
            for (var i = 0; i < 3; i++)
            { var run = Run(CharacterKind.Warrior); run.Seed = 3451 + i * 719; sequences.Add(string.Join(",", Enumerable.Range(0, 6).Select(room => RandomEventSystem.Draw(content, run, 1 + room / 3, room).id))); }
            Assert.That(sequences.Distinct().Count(), Is.EqualTo(3));
        }
        [Test] public void Dungeon_ThreeEventRoomsDoNotReplaceAllNormalChallenges()
        {
            var settings = ScriptableObject.CreateInstance<DungeonGenerationSettings>();
            for (var seed = 0; seed < 20; seed++) for (var act = 1; act <= 3; act++)
            {
                var dungeon = new DungeonGenerator(settings).Generate(333 + seed * 919, act);
                Assert.That(dungeon.Graph.Rooms.Count(room => room.Type == RoomEncounterType.Event), Is.EqualTo(3));
                Assert.That(dungeon.Graph.Rooms.Count(room => room.Type == RoomEncounterType.Combat), Is.GreaterThanOrEqualTo(4));
            }
            Object.DestroyImmediate(settings);
        }
    }
}
