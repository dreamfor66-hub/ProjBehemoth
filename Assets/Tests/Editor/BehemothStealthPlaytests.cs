using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DungeonSlash.Tests
{
    public sealed partial class BehemothPlaytests
    {
        // Select a known non-detecting sample without changing the game's probability or production data.
        private static void NextDetectionMiss()
        {
            for (var seed = 0; seed < 10000; seed++)
            {
                Random.InitState(seed);
                if (Random.value < .99f) continue;
                Random.InitState(seed); return;
            }
            Assert.Fail("Could not find detection sample");
        }

        [UnityTest] public IEnumerator Stealth_PhysicalChargeAndLongCast_FreezePackUntilImpact_NoDoubleAction()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity"); yield return new EnterPlayMode();
            SetupCharacter(CharacterKind.Rogue);
            var combat = Object.FindFirstObjectByType<CombatController>();
            var input = Object.FindFirstObjectByType<CombatInputController>();
            var scene = Object.FindFirstObjectByType<CombatSceneView>();
            combat.PrepareFixedEncounter(combat.Run, Encounter("Trial"), 1); combat.BeginPreparedCombat(); yield return null;
            var actor = Actors(combat)[0]; var enemy = Runtime(actor); var center = Position(actor);
            var start = center + Vector2.left * 180; var end = center + Vector2.right * 180;
            var normal = enemy.NormalAttackTimer; var charge = enemy.ChargeTimer; var hp = enemy.CurrentHp;
            Call(input, "Begin", 101, Screen(scene, start));
            for (var until = Time.time + 2f; Time.time < until;) { Call(input, "Move", 101, Screen(scene, start)); yield return null; }
            Assert.That(input.IsChargingAction, Is.True); Assert.That(enemy.NormalAttackTimer, Is.EqualTo(normal));
            Assert.That(enemy.ChargeTimer, Is.EqualTo(charge)); Assert.That(combat.Run.Awareness.CompletedActions, Is.Zero);
            Assert.That(Object.FindFirstObjectByType<RunExpansionView>().characterStatus.text, Does.Contain("25%"));
            ScreenCapture.CaptureScreenshot("Logs/Behemoth-StealthCharge.png"); yield return null;
            Call(input, "Move", 101, Screen(scene, end)); Call(input, "End", 101, Screen(scene, end), false); yield return null;
            Assert.That(enemy.CurrentHp, Is.LessThan(hp)); Assert.That(combat.Run.Awareness.IsConcealed, Is.False);
            Assert.That(combat.Run.Awareness.CompletedActions, Is.EqualTo(1)); Assert.That(enemy.PoisonStacks, Is.Zero);
            ScreenCapture.CaptureScreenshot("Logs/Behemoth-StealthReveal.png"); yield return null;
            Matrix("은신 물리 차지 2초: 적 일반·차지 타이머 정지 / 첫 차지 적중 후 발각·행동 1회 / 독 없음");

            input.CancelAll(); combat.PrepareFixedEncounter(combat.Run, Encounter("TwinHeart"), 1); combat.BeginPreparedCombat();
            var spell = Object.Instantiate(Content.spells.Single(item => item.id == "EnergyBolt"));
            spell.castingSeconds = 3f; spell.openingCooldownSeconds = 0; spell.damage = 1;
            Assert.That(combat.Run.LearnSpell(spell), Is.True);
            var pack = Actors(combat); var timers = pack.Select(item => Runtime(item).NormalAttackTimer).ToArray();
            var body = Screen(scene, scene.PlayerPosition);
            Call(input, "Begin", 201, body); yield return null;
            for (var cycle = 0; cycle < 4; cycle++)
            {
                Call(input, "Begin", 202, body + Vector2.right * 3); yield return null;
                Call(input, "End", 201, body, false); yield return null;
                Call(input, "Begin", 201, body); yield return null;
                Call(input, "End", 202, body, false); yield return null;
                Assert.That(input.IsGuardHeld, Is.True); Assert.That(combat.Run.Awareness.CompletedActions, Is.Zero);
            }
            for (var until = Time.time + 3.2f; Time.time < until;) yield return null;
            for (var i = 0; i < pack.Length; i++) Assert.That(Runtime(pack[i]).NormalAttackTimer, Is.EqualTo(timers[i]));
            Assert.That(input.CastingSeconds, Is.GreaterThanOrEqualTo(3));
            ScreenCapture.CaptureScreenshot("Logs/Behemoth-StealthCast.png"); yield return null;
            Call(input, "End", 201, Screen(scene, scene.PlayerPosition + Vector2.up * 120), false); yield return null;
            Assert.That(combat.Run.SpellCasts, Is.EqualTo(1)); Assert.That(combat.Run.Awareness.CompletedActions, Is.EqualTo(1));
            Assert.That(pack.All(item => !Runtime(item).IsUnaware), Is.True);
            Assert.That(input.IsGuardHeld, Is.False);
            Matrix("은신 캐스팅 3초+ / 두 접촉 교대 해제·재입력 4회: 준비 중 행동 0회 / 주문 발사 1회 / 무리 전체 즉시 발각");
            Object.Destroy(spell); yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator Stealth_PotionsNonDamageSpellGuardAndInvalidInput_RollOnlyCompletedActions()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity"); yield return new EnterPlayMode(); SetupCharacter(CharacterKind.Rogue);
            var combat = Object.FindFirstObjectByType<CombatController>(); var input = Object.FindFirstObjectByType<CombatInputController>();
            var scene = Object.FindFirstObjectByType<CombatSceneView>();
            combat.PrepareFixedEncounter(combat.Run, Encounter("Trial"), 1); combat.BeginPreparedCombat(); yield return null;
            var enemy = Runtime(Actors(combat)[0]); var timer = enemy.NormalAttackTimer;
            var spell = Object.Instantiate(Content.spells.Single(item => item.id == "EnergyBolt"));
            spell.damage = 0; spell.slowSeconds = 0; spell.openingCooldownSeconds = 0;
            combat.Run.LearnSpell(spell);
            Assert.That(combat.TryCastSpell(spell.defaultSlot, 0), Is.False);
            Assert.That(combat.Run.Awareness.CompletedActions, Is.Zero);
            NextDetectionMiss(); Assert.That(combat.TryCastSpell(spell.defaultSlot, 10), Is.True);
            Assert.That(combat.Run.Awareness.IsConcealed, Is.True); Assert.That(combat.Run.Awareness.CompletedActions, Is.EqualTo(1));
            Assert.That(combat.Run.Awareness.NextDetectionChance, Is.EqualTo(.5f));
            Assert.That(combat.TryCastSpell(spell.defaultSlot, 10), Is.False);
            Assert.That(combat.Run.Awareness.CompletedActions, Is.EqualTo(1));
            input.CancelAll(); Assert.That(combat.Run.Awareness.CompletedActions, Is.EqualTo(1));
            var body = Screen(scene, scene.PlayerPosition);
            Call(input, "Begin", 301, body); yield return null;
            NextDetectionMiss(); Call(input, "End", 301, body, false); yield return null;
            Assert.That(combat.Run.Awareness.CompletedActions, Is.EqualTo(2)); Assert.That(combat.Run.Awareness.IsConcealed, Is.True);
            Assert.That(enemy.NormalAttackTimer, Is.EqualTo(timer));
            Matrix("비공격 주문·방어 종료도 각 1회 판정 / 준비 부족·쿨타임 실패·입력 취소는 미집계");

            combat.PrepareFixedEncounter(combat.Run, Encounter("Trial"), 1); combat.BeginPreparedCombat();
            combat.Player.TakeEnvironmentalDamage(10);
            var potion = ScriptableObject.CreateInstance<EquipmentData>(); potion.itemKind = ShopItemKind.Potion;
            potion.abilities.Add(new EquipmentAbility { trigger = new EquipmentTrigger { triggerType = TriggerType.OnConsume, chancePercent = 100, triggerCount = 1 },
                target = TargetType.Owner, effects = new() { new EquipmentEffect { effectType = EffectType.Heal, effectMagnitude = 1, effectCount = 1 } } });
            for (var action = 1; action <= 4; action++)
            {
                Assert.That(combat.Run.TryStorePotion(potion), Is.True);
                NextDetectionMiss(); Assert.That(combat.Run.TryUsePotion(potion), Is.True);
                Assert.That(combat.Run.Awareness.CompletedActions, Is.EqualTo(action));
                Assert.That(combat.Run.Awareness.IsConcealed, Is.EqualTo(action < 4)); yield return null;
            }
            Matrix("회복 포션 4회: 행동 후 25→50→75→100% / 성공 시 은신 유지 / 4번째 확정 발각");
            input.CancelAll(); combat.PrepareFixedEncounter(combat.Run, Encounter("Trial"), 1); combat.BeginPreparedCombat();
            Assert.That(combat.Run.Awareness.IsConcealed, Is.True); Assert.That(combat.Run.Awareness.CompletedActions, Is.Zero);
            combat.Player.EndCharacterCombat(); Assert.That(combat.Run.Awareness.IsConcealed, Is.False);
            Matrix("다음 조우 은신·확률 초기화 / 전투 종료 은신 정리");
            Object.Destroy(spell); Object.Destroy(potion); yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator Stealth_OtherFiveClasses_ImmediateTimersAndOriginalUI_NoConcealmentDelay()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity"); yield return new EnterPlayMode();
            foreach (var kind in new[] { CharacterKind.Warrior, CharacterKind.Barbarian, CharacterKind.Samurai, CharacterKind.Monk, CharacterKind.Wizard })
            {
                SetupCharacter(kind); var combat = Object.FindFirstObjectByType<CombatController>();
                combat.PrepareFixedEncounter(combat.Run, Encounter("Trial"), 1); combat.BeginPreparedCombat();
                var enemy = Runtime(Actors(combat)[0]); var initial = enemy.NormalAttackTimer; var time = combat.Player.CombatTime;
                for (var until = Time.time + .25f; Time.time < until;) yield return null;
                Assert.That(enemy.IsUnaware, Is.False);
                Assert.That(initial - enemy.NormalAttackTimer, Is.EqualTo(combat.Player.CombatTime - time).Within(.025f));
                var ui = Object.FindFirstObjectByType<RunExpansionView>();
                Assert.That(ui.characterStatus.text, Does.Not.Contain("은신"));
                Assert.That(ui.encounterHint.text, Is.EqualTo(combat.EncounterHint));
                foreach (var view in Object.FindObjectsByType<MonsterView>(FindObjectsSortMode.None))
                    Assert.That(Field<TMPro.TextMeshProUGUI>(view, "stateLabel").text, Does.Not.Contain("미인지"));
                Matrix(kind + " 회귀: 전투 시작 즉시 기존 속도 타이머 / 기존 조우 안내·상태 UI / 은신 표시 없음");
            }
            yield return new ExitPlayMode();
        }
    }
}
