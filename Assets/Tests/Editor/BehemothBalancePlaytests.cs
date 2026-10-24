using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private sealed class FightMeasure
        {
            public float elapsed, damage, hpLost, guardUsed, firstBreak = -1, followup;
            public int attacks, casts; public bool won;
            public override string ToString() => $"{elapsed:0.00}s | damage {damage:0.0} | DPS {damage / Mathf.Max(.01f, elapsed):0.00} | HP lost {hpLost:0.0} | guard spent {guardUsed:0.0} | first break {firstBreak:0.00}s | follow-up {followup:0.0} | attacks {attacks} | casts {casts} | won {won}";
        }
        private IEnumerator MeasureFight(CombatController combat, object actor, float seconds, FightMeasure result)
        {
            var enemy = Runtime(actor); var player = combat.Player; var center = Position(actor);
            var startHp = player.CurrentHp; var startGuard = player.ShieldCurrent; var startEnemyHp = enemy.CurrentHp;
            var kind = player.Data.character; var chargeStarted = -1f; var castStarted = -1f; var previousStun = false; var previousHp = enemy.CurrentHp;
            var input = Object.FindFirstObjectByType<CombatInputController>(); var scene = Object.FindFirstObjectByType<CombatSceneView>(); input.CancelAll();
            void Guard(bool held)
            {
                var at = Screen(scene, scene.PlayerPosition);
                if (held && !input.IsGuardHeld) Call(input, "Begin", 9001, at);
                else if (!held && input.IsGuardHeld) Call(input, "End", 9001, at, true);
            }
            var start = player.CombatTime;
            AttackSegment Stroke(SwipeDirection direction, bool weakpoint = false)
            {
                var vector = direction == SwipeDirection.Up ? Vector2.up : direction == SwipeDirection.Down ? Vector2.down : direction==SwipeDirection.Left?Vector2.left:Vector2.right;
                if (weakpoint && enemy.State == MonsterState.Charging)
                {
                    var remaining = enemy.WeakPoints.Where(point => !point.IsDestroyed).ToArray();
                    if (remaining.Length > 0) { var a = remaining.First().HitShape.Center; var b = remaining.Last().HitShape.Center; var delta = a == b ? Vector2.right : (b - a).normalized; return new AttackSegment(a - delta * 160, b + delta * 160, 18); }
                }
                return new AttackSegment(center - vector * 180, center + vector * 180, 18);
            }
            while (combat.IsActive && player.IsAlive && enemy.IsAlive && player.CombatTime - start < seconds)
            {
                var now = player.CombatTime;
                if (enemy.IsStunned && !previousStun && result.firstBreak < 0) result.firstBreak = now - start;
                if (previousStun) result.followup += Mathf.Max(0, previousHp - enemy.CurrentHp);
                previousStun = enemy.IsStunned; previousHp = enemy.CurrentHp;
                if (enemy.State == MonsterState.Attacking)
                { Guard(true); chargeStarted = castStarted = -1; yield return null; continue; }
                var completingForm = kind == CharacterKind.Monk && player.PreparedAttackBuff == null && (enemy.State != MonsterState.Charging || enemy.StateTimer > player.CurrentChargeDuration + player.CurrentCooldown * (2 - player.Combo.Count));
                var fury=AssetDatabase.LoadAssetAtPath<BuffData>("Assets/Data/Combat/Buffs/Fury.asset");
                var needsCharge = enemy.State == MonsterState.Charging && !completingForm || kind == CharacterKind.Barbarian && (player.Buffs.Find(fury)?.StackCount ?? 0) >= 40 || kind == CharacterKind.Monk && player.PreparedAttackBuff != null;
                if (needsCharge)
                {
                    Guard(false); castStarted = -1;
                    if (player.CanNormalAttack() && chargeStarted < 0) chargeStarted = now;
                    if (chargeStarted >= 0 && now - chargeStarted >= player.CurrentChargeDuration)
                    { combat.ExecuteAttack(Stroke(SwipeDirection.Right, true), true); result.attacks++; chargeStarted = -1; }
                    yield return null; continue;
                }
                chargeStarted = -1;
                var spell = kind == CharacterKind.Wizard ? combat.Run.Spellbook.FirstOrDefault(item => item.Cooldown <= 0) : null;
                if (spell != null && player.CanNormalAttack())
                {
                    Guard(true); if (castStarted < 0) castStarted = now;
                    if (now - castStarted >= spell.Data.castingSeconds * player.SpellCastingMultiplier)
                    { Guard(false); if (combat.TryCastSpell(spell.Data.defaultSlot, now - castStarted)) result.casts++; castStarted = -1; }
                    yield return null; continue;
                }
                castStarted = -1;
                if (kind == CharacterKind.Samurai) Guard(true); else Guard(false);
                if (player.CanNormalAttack())
                {
                    if (kind == CharacterKind.Samurai && (player.Buffs.Active.FirstOrDefault(b=>b.Data.name=="Sheathed")?.ElapsedTime ?? 0) < .12f) { yield return null; continue; }
                    Guard(false);
                    var direction = SwipeDirection.Right;
                    if (kind == CharacterKind.Monk)
                    {
                        var finisher=player.ShieldCurrent<player.ShieldMax*.5f && enemy.State!=MonsterState.Charging?"Shelter":enemy.State==MonsterState.Charging && enemy.WeakPoints.Any(point=>point.RemainingHits>1)?"Multi":"Burst";
                        var pattern=player.Data.attackBuffs.First(b=>b.name==finisher && !b.requiresUnlock).attackPattern;
                        var progress=0;for(var i=1;i<pattern.Length && i<=player.Combo.Count;i++)if(player.Combo.Skip(player.Combo.Count-i).SequenceEqual(pattern.Take(i)))progress=i;
                        direction=pattern[progress];
                    }
                    combat.ExecuteAttack(Stroke(direction), false); result.attacks++;
                }
                yield return null;
            }
            result.elapsed = player.CombatTime - start; result.damage = startEnemyHp - enemy.CurrentHp; result.hpLost = startHp - player.CurrentHp; result.guardUsed = startGuard - player.ShieldCurrent; result.won = !enemy.IsAlive;
            Guard(false); input.CancelAll();
        }
        [UnityTest] public IEnumerator Balance_SixCoreLoops_SustainedDpsProbe_AndVisibleDog()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity"); yield return new EnterPlayMode();
            var oldScale = Time.timeScale; Time.timeScale = 4;
            var measurements = new List<float>(); File.AppendAllText("Logs/Behemoth-BalanceMatrix.txt", "\n30s sustained probe. Test-only 10000 HP target; no authored enemy or Warrior data changed. Input preparation and shared recovery respected.\n");
            foreach (var kind in Enum.GetValues(typeof(CharacterKind)).Cast<CharacterKind>())
            {
                SetupCharacter(kind); var combat = Object.FindFirstObjectByType<CombatController>();
                var dummy = Object.Instantiate(Encounter("TrainingHeavy").members[0].monster); dummy.maxHp = 10000;
                combat.PrepareEncounter(combat.Run, new[] { dummy }, 1); combat.BeginPreparedCombat(); var actor = Actors(combat)[0]; Runtime(actor).DelayActions(1000);
                var measure = new FightMeasure(); yield return MeasureFight(combat, actor, 30, measure);
                measurements.Add(measure.damage / measure.elapsed);
                File.AppendAllText("Logs/Behemoth-BalanceMatrix.txt", kind + " | " + measure + "\n"); Object.Destroy(dummy);
            }
            Time.timeScale = oldScale;
            for (var i = 1; i < measurements.Count; i++) Assert.That(measurements[i] / measurements[0], Is.InRange(.75f, 1.25f), ((CharacterKind)i) + " / Warrior");
            SetupCharacter(CharacterKind.Warrior); var dogCombat = Object.FindFirstObjectByType<CombatController>();
            dogCombat.Run.TryEquip(Content.equipment.Single(item => item.power == RelicPower.StrayDog)); dogCombat.PrepareFixedEncounter(dogCombat.Run, Encounter("Trial"), 1); dogCombat.BeginPreparedCombat(); yield return null;
            var dog = Object.FindFirstObjectByType<CompanionView>(); Assert.That(dog, Is.Not.Null); Assert.That(dog.body.GetComponentsInChildren<UnityEngine.UI.Image>().Length, Is.GreaterThan(5));
            Assert.That(dog.transform.position, Is.Not.EqualTo(Object.FindFirstObjectByType<PlayerView>().transform.position));
            for (var until = Time.unscaledTime + .95f; Time.unscaledTime < until;) yield return null;
            ScreenCapture.CaptureScreenshot("Logs/Behemoth-DogCompanion.png"); yield return null;
            Matrix("6직업 30초 지속 피해 비교 / 차지·캐스팅 준비·공유 쿨타임 준수 / 강아지 별도 프리팹 화면 표시");
            yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator Balance_AuthoredElites_ThreeActs_MatchedProgression_DefenseAndFollowup()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity"); yield return new EnterPlayMode(); var oldScale = Time.timeScale; Time.timeScale = 4;
            File.WriteAllText("Logs/Behemoth-BalanceMatrix.txt", "Actual authored first elite / each act. Equal shared progression per later act: normal+30%, charge+40%, HP+40, guard+25. Actual guard contact held/released, remaining weakpoints targeted, Monk switches to Shelter at low guard, real charge preparation, no heal/reset during fight. Scripted policy, not a full run or human skill assessment.\n");
            for (var act = 1; act <= 3; act++)
            {
                var encounter = Content.stages.Single(stage => stage.act == act).elites[0]; FightMeasure reference = null;
                foreach (var kind in Enum.GetValues(typeof(CharacterKind)).Cast<CharacterKind>())
                {
                    SetupCharacter(kind); var combat = Object.FindFirstObjectByType<CombatController>();
                    for (var step = 1; step < act; step++) combat.Player.ApplyModifiers(new[] { new StatModifier { kind = ModifierKind.NormalDamage, value = .3f }, new StatModifier { kind = ModifierKind.ChargeDamage, value = .4f }, new StatModifier { kind = ModifierKind.MaxHp, value = 40 }, new StatModifier { kind = ModifierKind.ShieldMax, value = 25 } });
                    combat.PrepareFixedEncounter(combat.Run, encounter, act); combat.BeginPreparedCombat(); var actor = Actors(combat)[0]; var measure = new FightMeasure();
                    Assert.That(Runtime(actor).MaxHp, Is.EqualTo(Runtime(actor).Data.maxHp));
                    yield return MeasureFight(combat, actor, 60, measure);
                    File.AppendAllText("Logs/Behemoth-BalanceMatrix.txt", $"B{act} {encounter.id} | {kind} | {measure}\n");
                    if (kind == CharacterKind.Warrior) reference = measure;
                    else if (reference.firstBreak >= 0 && measure.won) Assert.That(measure.firstBreak, Is.GreaterThanOrEqualTo(0), kind + " must not erase the reference weakpoint opportunity");
                }
            }
            Time.timeScale = oldScale; Matrix("1·2·3막 실제 엘리트 × 6직업: 동일 성장량, 피격·가드·약점 파괴·후속 피해·처치 시간 기록"); yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator Ability_SpellCannotBypassOpeningPreparationOrWeaponRecovery()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity"); yield return new EnterPlayMode(); SetupCharacter(CharacterKind.Wizard);
            var combat = Object.FindFirstObjectByType<CombatController>(); combat.PrepareFixedEncounter(combat.Run, Encounter("Trial"), 1); combat.BeginPreparedCombat();
            Assert.That(combat.TryCastSpell(SwipeDirection.Up, 10), Is.False, "No guaranteed opening nuke.");
            combat.Run.RuntimeFor(combat.Run.Slot(SwipeDirection.Up)).ResetCooldown();
            var actor = Actors(combat)[0]; var center = Position(actor); combat.ExecuteAttack(new AttackSegment(center + Vector2.left * 160, center + Vector2.right * 160, 18), false);
            Assert.That(combat.TryCastSpell(SwipeDirection.Up, 10), Is.False, "A second contact must not bypass weapon recovery.");
            for (var until = Time.time + combat.Player.CurrentCooldown + .1f; Time.time < until;) yield return null;
            Assert.That(combat.TryCastSpell(SwipeDirection.Up, 10), Is.True); Assert.That(combat.Player.AttackCooldownRemaining, Is.GreaterThan(0));
            Matrix("주문 개막 준비시간 / 무기-주문·다중 접촉의 공유 행동 회복 확인"); yield return new ExitPlayMode();
        }
    }
}
