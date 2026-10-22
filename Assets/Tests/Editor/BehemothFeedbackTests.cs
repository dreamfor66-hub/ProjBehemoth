using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
namespace DungeonSlash.Tests
{
    public sealed class BehemothFeedbackTests
    {
        private GameContent Content => AssetDatabase.LoadAssetAtPath<GameContent>("Assets/Data/Behemoth/GameContent.asset");
        private RunBalanceSettings balance;
        [SetUp] public void Setup() => balance = ScriptableObject.CreateInstance<RunBalanceSettings>();
        [TearDown] public void Cleanup() => Object.DestroyImmediate(balance);
        private RunState Run(CharacterKind kind) { var data=Content.characters.Single(data => data.character == kind); var run=new RunState(data,balance);run.TryEquip(data.startingRelic);run.BeginCombat();return run; }
        private EquipmentData Relic(RelicPower power) => Content.equipment.Single(data => data.power == power);
        private static DamageResult Hit(PlayerCombatRuntime player) => new DamageResolver().ApplyToPlayer(player, new DamageRequest(DamageType.MonsterAttack, 10, 10, true));
        [TestCase(CharacterKind.Warrior)] [TestCase(CharacterKind.Rogue)] [TestCase(CharacterKind.Barbarian)]
        [TestCase(CharacterKind.Monk)] [TestCase(CharacterKind.Wizard)]
        public void NoBaselinePerfectGuard(CharacterKind kind)
        {
            var player = Run(kind).Player; player.TrySetGuard(true); var result = Hit(player);
            Assert.That(result.WasPerfectGuard, Is.False); Assert.That(result.WasSheathParry, Is.False);
            Assert.That(result.HpDamage, Is.Zero); Assert.That(result.ShieldDamage, Is.EqualTo(10));
        }
        [Test] public void WarriorRelic_OpensPerfectGuard_RefreshesRatherThanStacks_ExpiresInCombatTime()
        {
            var run = Run(CharacterKind.Warrior); run.TryEquip(Relic(RelicPower.PerfectMending)); var player = run.Player;
            player.TrySetGuard(true);player.Buffs.Tick(0); Assert.That(Hit(player).WasPerfectGuard, Is.True);player.Buffs.Tick(0); Assert.That(player.AllDamageMultiplier, Is.EqualTo(1.3f).Within(.001));
            player.Tick(.2f); player.TrySetGuard(false); player.TrySetGuard(true);player.Buffs.Tick(0); Hit(player);player.Buffs.Tick(0); Assert.That(player.AllDamageMultiplier, Is.EqualTo(1.3f).Within(.001));
            player.TrySetGuard(false); player.Tick(10.01f); Assert.That(player.AllDamageMultiplier, Is.EqualTo(1).Within(.001));
            player.TrySetGuard(true); Hit(player); player.EndCharacterCombat(); Assert.That(player.AllDamageMultiplier, Is.EqualTo(1).Within(.001));
        }
        [Test] public void RageAttackSustain_RequiresExclusiveRelic()
        {
            var run = Run(CharacterKind.Barbarian); var player = run.Player; player.AddResource(player.Data.resources.FirstOrDefault(), 50); player.PrepareAttack(SwipeDirection.Up, true);player.Buffs.Tick(0); player.Tick(1);
            var before = player.Resource(player.Data.resources.FirstOrDefault()); player.PrepareAttack(SwipeDirection.Right, false); Assert.That(player.Resource(player.Data.resources.FirstOrDefault()), Is.EqualTo(before));
            run.TryEquip(Relic(RelicPower.LongRage)); player.PrepareAttack(SwipeDirection.Right, false); run.EmitEvent(TriggerType.OnAttack, run.ActionContext); Assert.That(player.Resource(player.Data.resources.FirstOrDefault()), Is.EqualTo(before + 6));
        }
        [Test] public void NormalPackProgression_200Seeds_NoEarlyPack_NoActOneTrio_GradualProbability()
        {
            var pairs = 0; var singles = 0; var triples = 0;
            for (var seed = 0; seed < 200; seed++)
            {
                var planner = new EncounterPlanner(Content, seed);
                for (var act = 1; act <= 3; act++) for (var count = 0; count < 13; count++)
                {
                    var encounter = planner.Get(act, RoomEncounterType.Combat, count + 100, count); var size = encounter.members.Count;
                    if (count < 6) Assert.That(size, Is.EqualTo(1));
                    if (act == 1 || count < 9) Assert.That(size, Is.LessThanOrEqualTo(2));
                    if (count >= 6) { if (size == 1) singles++; else if (size == 2) pairs++; else triples++; }
                }
            }
            Assert.That(pairs, Is.GreaterThan(0)); Assert.That(triples, Is.GreaterThan(0)); Assert.That(singles, Is.GreaterThan(pairs + triples));
        }
        [Test] public void Events_NoBlankPass_EvenBrokeAndOneHpHaveConsequentialOption()
        {
            var run = Run(CharacterKind.Warrior); run.SpendGold(run.Gold); run.Player.TakeEnvironmentalDamage(run.Player.MaxHp - 1);
            foreach (var story in Content.events)
            {
                Assert.That(story.options.All(option => option.effects.Count > 0), Is.True, story.id);
                // An entirely ineligible event is filtered by Draw, not changed into an IOU.
                if (story.options.All(option => RandomEventSystem.UnavailableReason(option, run) != null))
                    Assert.That(story.options.All(option => option.effects.All(effect => effect.kind != EventEffectKind.Burden && effect.kind != EventEffectKind.GoldDebt)), Is.True, story.id);
            }
        }
        [Test] public void EventBurden_VisibleFiniteCombatPenalty_AndDebtCannotMakeGoldNegative()
        {
            var run = Run(CharacterKind.Monk); var original = run.Player.CurrentCooldown; run.AddBurden("Fatigue", 2); run.BeginCombat();
            Assert.That(run.Player.CurrentCooldown, Is.EqualTo(original * 1.2f).Within(.001));
            run.EventGoldDebt = 80; run.SpendGold(run.Gold); run.CompleteEventConsequences(); Assert.That(run.Gold, Is.Zero); Assert.That(run.EventGoldDebt, Is.EqualTo(80));
            run.BeginCombat(); Assert.That(run.Player.CurrentCooldown, Is.EqualTo(original * 1.2f).Within(.001));
            run.GainGold(10); run.CompleteEventConsequences(); Assert.That(run.EventGoldDebt, Is.EqualTo(70)); Assert.That(run.EventBurdens.Count, Is.Zero);
            run.BeginCombat(); Assert.That(run.Player.CurrentCooldown, Is.EqualTo(original).Within(.001));
        }
        [Test] public void RemovedPerkSpecifications_CannotEnterChoicePool()
        {
            Assert.That(Content.perks.All(PerkSystem.IsAvailable), Is.True);
            var removed = ScriptableObject.CreateInstance<PerkData>(); removed.modifiers.Add(new StatModifier { kind = ModifierKind.AttackReach, value = 10 });
            Assert.That(new PerkSystem(new[] { removed }).GetChoices(Run(CharacterKind.Warrior), 1), Is.Empty); Object.DestroyImmediate(removed);
        }
        [Test] public void KoreanCsv_RoundtripQuotesCommasMultilineAndUnicode_TransactionalRejection()
        {
            var table = ScriptableObject.CreateInstance<GameTextDatabase>(); var other = ScriptableObject.CreateInstance<GameTextDatabase>();
            try
            {
                const string text = "따옴표 \"둘\", 쉼표\r\n다음 줄 {0:0.00}초"; table.Set("test.name", text); other.ImportCsv("\uFEFF" + table.ExportCsv());
                Assert.That(other.Get("test.name"), Is.EqualTo(text));
                Assert.Throws<FormatException>(() => other.ImportCsv("key,kr\na,good\na,duplicate")); Assert.That(other.Contains("a"), Is.False);
                Assert.Throws<FormatException>(() => other.ImportCsv("key,kr\na,\"unclosed"));
                Assert.Throws<FormatException>(() => other.ImportCsv("key,kr\na,{broken}"));
            }
            finally { Object.DestroyImmediate(table); Object.DestroyImmediate(other); }
        }
        [Test] public void DataTextSlots_AutomaticStableKeys_CoverNestedEventChoices()
        {
            var table = GameText.Database; Assert.That(table, Is.Not.Null);
            foreach (var asset in Content.characters.Cast<LocalizedData>().Concat(Content.equipment).Concat(Content.events))
            {
                Assert.That(asset.textKeys.Count, Is.GreaterThan(0), asset.name);
                foreach (var key in asset.textKeys) Assert.That(table.Contains(key.key), Is.True, key.key);
            }
            Assert.That(Content.events.All(story => story.textKeys.Any(binding => binding.propertyPath.StartsWith("options.Array.data[0]."))), Is.True);
        }
        [Test] public void EveryRuntimeTextKey_HasKoreanCopy()
        {
            var pattern = new System.Text.RegularExpressions.Regex("GameText\\.(?:Get|Format)\\(\"([^\"]+)\"");
            foreach (var path in System.IO.Directory.GetFiles("Assets/Scripts/DungeonSlash", "*.cs", System.IO.SearchOption.AllDirectories))
                foreach (System.Text.RegularExpressions.Match match in pattern.Matches(System.IO.File.ReadAllText(path)))
                    Assert.That(GameText.Database.Contains(match.Groups[1].Value), Is.True, path + ": " + match.Groups[1].Value);
        }
        [Test] public void InspectorTextAndCsvImport_UpdateTheSameKey_Bidirectionally()
        {
            var asset = Content.characters[0]; var table = GameText.Database; var original = asset.description;
            var binding = asset.textKeys.Single(item => item.propertyPath == "description"); var key = binding.key;
            var editor = Type.GetType("BehemothTextEditor, Assembly-CSharp-Editor"); Assert.That(editor, Is.Not.Null);
            var sync = editor.GetMethod("SyncAsset");
            try
            {
                asset.description = "Inspector에서 수정한 설명"; EditorUtility.SetDirty(asset); sync.Invoke(null, new object[] { asset, false });
                Assert.That(table.Get(key), Is.EqualTo(asset.description)); Assert.That(binding.key, Is.EqualTo(key));
                table.ImportCsv("key,kr\r\n" + key + ",\"CSV에서 수정, 줄바꿈\n둘째 줄\"\r\n");
                sync.Invoke(null, new object[] { asset, true }); Assert.That(asset.description, Is.EqualTo("CSV에서 수정, 줄바꿈\n둘째 줄"));
                EditorUtility.SetDirty(asset); AssetDatabase.SaveAssets();
                Assert.That(asset.textKeys.Single(item => item.propertyPath == "description").lastText, Is.EqualTo(asset.description));
                table.Set(key, "테이블에서 직접 수정한 설명"); sync.Invoke(null, new object[] { asset, false });
                Assert.That(asset.description, Is.EqualTo("테이블에서 직접 수정한 설명"), "A stale Inspector cache must not overwrite a table edit after CSV import.");
            }
            finally
            {
                table.Set(key, original); sync.Invoke(null, new object[] { asset, true });
                EditorUtility.SetDirty(asset); EditorUtility.SetDirty(table); AssetDatabase.SaveAssets();
            }
        }
    }
}
