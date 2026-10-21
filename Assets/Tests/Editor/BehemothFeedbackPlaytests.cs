using System;
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
        [UnityTest] public IEnumerator Feedback_NoSpellsNoSlotsNoCasting_ThenFixedSlotReplacementUI()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity"); yield return new EnterPlayMode(); SetupCharacter(CharacterKind.Warrior);
            var combat = Object.FindFirstObjectByType<CombatController>(); var ui = Object.FindFirstObjectByType<RunExpansionView>();
            var input = Object.FindFirstObjectByType<CombatInputController>(); var scene = Object.FindFirstObjectByType<CombatSceneView>();
            combat.PrepareFixedEncounter(combat.Run, Encounter("Trial"), 1); combat.BeginPreparedCombat();
            Call(input, "Begin", 701, Screen(scene, scene.PlayerPosition)); for (var frame = 0; frame < 10; frame++) yield return null;
            ui.RefreshCombat(combat); Assert.That(input.CastingSeconds, Is.Zero); Assert.That(ui.spellButtons.All(button => !button.gameObject.activeSelf), Is.True);
            Assert.That(combat.Player.StatusText, Does.Not.Contain("주문"));
            Call(input, "End", 701, Screen(scene, scene.PlayerPosition), false);
            var ember = Content.spells.Single(spell => spell.id == "Ember"); var chain = Content.spells.Single(spell => spell.id == "ChainLightning");
            combat.Run.LearnSpell(ember); combat.Run.RuntimeFor(ember).Use(combat.Player); var originalCooldown = combat.Run.RuntimeFor(ember).Cooldown;
            bool? accepted = null; ui.OfferSpell(chain, result => accepted = result); Assert.That(ui.IsModalOpen, Is.True);
            ui.choices[1].button.onClick.Invoke(); Assert.That(accepted, Is.False); Assert.That(combat.Run.Slot(SwipeDirection.Right), Is.SameAs(ember));
            Assert.That(combat.Run.RuntimeFor(ember).Cooldown, Is.EqualTo(originalCooldown));
            ui.OfferSpell(chain, result => accepted = result); ui.choices[0].button.onClick.Invoke(); Assert.That(accepted, Is.True);
            Assert.That(combat.Run.Slot(SwipeDirection.Right), Is.SameAs(chain)); Assert.That(combat.Run.RuntimeFor(ember), Is.Null);
            ui.RefreshCombat(combat); Assert.That(ui.spellButtons.Count(button => button.gameObject.activeSelf), Is.EqualTo(1));
            var rect = (RectTransform)ui.spellButtons[(int)SwipeDirection.Right].transform;
            Assert.That(rect.anchoredPosition.x, Is.GreaterThan(100)); Assert.That(rect.anchoredPosition.y, Is.EqualTo(scene.PlayerPosition.y));
            ScreenCapture.CaptureScreenshot("Logs/Behemoth-SpellCross.png"); yield return null;
            Matrix("주문 없음: 슬롯·캐스팅 숨김 / 고정 방향 충돌: 유지 시 쿨타임 보존·교체 시 기존 주문 제거 / 캐릭터 우측 슬롯");
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator Feedback_SwipeContinuesAcrossGapAndDiagonal_StopsAtLastTarget_AndEarlyRelease()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity"); yield return new EnterPlayMode(); SetupCharacter(CharacterKind.Warrior);
            var combat = Object.FindFirstObjectByType<CombatController>(); var input = Object.FindFirstObjectByType<CombatInputController>(); var view = Object.FindFirstObjectByType<CombatSceneView>();
            // Replay actual contact handlers across multiple frames, not only geometry helpers.
            for (var scenario = 0; scenario < 3; scenario++)
            {
                var diagonal = scenario == 1; combat.PrepareFixedEncounter(combat.Run, Encounter(diagonal ? "ForgeTrio" : "MazePair"), 1); combat.BeginPreparedCombat();
                var actors = Actors(combat); var type = actors[0].GetType();
                for (var i = 0; i < actors.Length; i++)
                { type.GetProperty("Position").SetValue(actors[i], diagonal ? new Vector2(-160 + i * 160, 20 + i * 80) : new Vector2(-130 + i * 260, 100)); Runtime(actors[i]).DelayActions(100); }
                var hps = actors.Select(actor => Runtime(actor).CurrentHp).ToArray();
                Vector2 Point(float x) => new(x, diagonal ? 100 + x * .5f : 100);
                Call(input, "Begin", 801, Screen(view, Point(-245))); yield return null;
                Call(input, "Move", 801, Screen(view, Point(-30))); yield return null;
                Assert.That(combat.Player.AttackCooldownRemaining, Is.Zero, "Crossing the first target must not truncate a multi-target stroke.");
                if (scenario == 2)
                {
                    Call(input, "Move", 801, Screen(view, Point(-30)));
                    for (var until = Time.unscaledTime + .13f; Time.unscaledTime < until;) yield return null;
                    Call(input, "Move", 801, Screen(view, Point(-30))); yield return null;
                    Assert.That(Runtime(actors[0]).CurrentHp, Is.LessThan(hps[0])); Assert.That(Runtime(actors[1]).CurrentHp, Is.EqualTo(hps[1]));
                    Matrix("다음 적이 앞에 있어도 손을 멈추면 0.1초 뒤 현재 궤적 확정");
                }
                else
                {
                    Call(input, "Move", 801, Screen(view, Point(65))); yield return null;
                    Call(input, "Move", 801, Screen(view, Point(285))); yield return null;
                    Assert.That(combat.Player.AttackCooldownRemaining, Is.GreaterThan(0));
                    for (var i = 0; i < actors.Length; i++) Assert.That(Runtime(actors[i]).CurrentHp, Is.LessThan(hps[i]), "Missed crossed body " + i);
                    Matrix(diagonal ? "대각선 3체: 마지막 대상 뒤 자동 확정·전원 적중" : "2체 사이 빈 공간에서 공격 유지·마지막 대상 뒤 즉시 자동 확정");
                }
                Call(input, "End", 801, Screen(view, Point(285)), false); input.CancelAll();
            }
            // Single target still auto-commits promptly, with no mandatory wait for pointer-up.
            combat.PrepareFixedEncounter(combat.Run, Encounter("Trial"), 3); combat.BeginPreparedCombat(); var single = Actors(combat)[0]; Runtime(single).DelayActions(100);
            Assert.That(Runtime(single).MaxHp, Is.EqualTo(Runtime(single).Data.maxHp), "Act index must not multiply authored HP.");
            var center = Position(single); var before = Runtime(single).CurrentHp;
            Call(input, "Begin", 802, Screen(view, center + Vector2.left * 180)); yield return null;
            Call(input, "Move", 802, Screen(view, center + Vector2.right * 200)); yield return null;
            Assert.That(Runtime(single).CurrentHp, Is.LessThan(before)); Call(input, "End", 802, Screen(view, center + Vector2.right * 200), false);
            Matrix("단일 대상: 떼기 전 자동 공격 감각 유지 / 3막에서도 몬스터 자산 HP 그대로 사용");
            yield return new ExitPlayMode();
        }
    }
}
