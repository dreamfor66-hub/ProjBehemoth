using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DungeonSlash.Tests
{
    public sealed partial class BehemothPlaytests
    {
        [UnityTest] public IEnumerator BuffUnification_RealChargeTransfersFury_RageHudTintAndExpiry_No100Refill()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity"); yield return new EnterPlayMode();
            SetupCharacter(CharacterKind.Barbarian);
            var combat = Object.FindFirstObjectByType<CombatController>(); var input = Object.FindFirstObjectByType<CombatInputController>(); var scene = Object.FindFirstObjectByType<CombatSceneView>();
            combat.PrepareFixedEncounter(combat.Run, Encounter("Trial"), 1); combat.BeginPreparedCombat();
            var actor = Actors(combat)[0]; Runtime(actor).DelayActions(20);
            var fury = AssetDatabase.LoadAssetAtPath<BuffData>("Assets/Data/Behemoth/Buffs/Fury.asset"); var rage = AssetDatabase.LoadAssetAtPath<BuffData>("Assets/Data/Behemoth/Buffs/Rage.asset");
            var center = Position(actor); var start = center + Vector2.left * 180; var end = center + Vector2.right * 180;
            for (var i = 0; i < 3; i++) { combat.Player.ResetAttackCooldown(); combat.ExecuteAttack(new AttackSegment(start, end, 18), false); }
            combat.Run.Buffs.UpdateState(0); Assert.That(combat.Run.Buffs.Find(fury).StackCount, Is.EqualTo(36));
            combat.Player.ResetAttackCooldown(); combat.ExecuteAttack(new AttackSegment(start, end, 18), true);
            Assert.That(combat.Run.Buffs.Has(rage), Is.False); Assert.That(combat.Run.Buffs.Has(fury), Is.True);
            combat.Player.ResetAttackCooldown(); combat.ExecuteAttack(new AttackSegment(start, end, 18), false);
            combat.Player.ResetAttackCooldown(); Call(input, "Begin", 701, Screen(scene, start));
            for (var until = Time.time + combat.Player.CurrentChargeDuration + .1f; Time.time < until;)
            { Call(input, "Move", 701, Screen(scene, start)); yield return null; }
            var stored = combat.Run.Buffs.Find(fury).StackCount;
            Assert.That(input.IsChargingAction, Is.True, "The held contact must be inside the playable area before checking conversion.");
            Assert.That(stored, Is.InRange(40, 48));
            ScreenCapture.CaptureScreenshot("Logs/Behemoth-Buff-Fury.png"); yield return null;
            Call(input, "Move", 701, Screen(scene, end)); Call(input, "End", 701, Screen(scene, end), false); yield return null;
            var converted = combat.Run.Buffs.Find(rage); Assert.That(converted, Is.Not.Null);
            Assert.That(combat.Run.Buffs.Has(fury), Is.False); Assert.That(converted.StackCount, Is.LessThan(49));
            Assert.That(converted.StackCount, Is.EqualTo(stored).Within(1.5f), "Only elapsed idle/drain ticks may change the transferred amount.");
            var hud = Object.FindFirstObjectByType<RunExpansionView>(); Assert.That(hud.characterStatus.text, Does.Contain("분노")); Assert.That(hud.characterStatus.text, Does.Not.Contain("격노"));
            Assert.That(Field<UnityEngine.UI.Image>(Object.FindFirstObjectByType<PlayerView>(), "body").color.r, Is.GreaterThan(.95f));
            ScreenCapture.CaptureScreenshot("Logs/Behemoth-Buff-Rage.png"); yield return null;
            Matrix("격노 36 차지 불가 / 일반공격 12중첩 추가 / 실제 홀드 차지 후 격노 전량→분노 동일 중첩, 격노 제거·100 재충전 없음");
            for (var until = Time.time + 2.3f; Time.time < until;) yield return null;
            Assert.That(combat.Run.Buffs.Has(rage), Is.False); Assert.That(hud.characterStatus.text, Does.Not.Contain("분노"));
            Matrix("분노 HUD 중첩·몸 색 표시 / 초당25 중첩 소모 / 0에서 종료·원래 피해 및 색상 복귀");
            yield return new ExitPlayMode();
        }
    }
}
