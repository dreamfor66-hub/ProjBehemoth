using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace DungeonSlash.Tests
{
    // Golden contracts transcribed from Smash-Dungeon's BuffManager/Buff/BuffUnit.
    public sealed class BehemothBuffTests
    {
        private readonly List<Object> temporary = new();
        private RunState run;
        private T New<T>() where T:ScriptableObject {var result=ScriptableObject.CreateInstance<T>();result.name=typeof(T).Name+"_"+temporary.Count;temporary.Add(result);return result;}
        [SetUp] public void Setup(){var player=New<PlayerCombatData>();run=new RunState(player,New<RunBalanceSettings>());run.BeginCombat();run.Player.TakeEnvironmentalDamage(50);}
        [TearDown] public void Cleanup(){foreach(var item in temporary)Object.DestroyImmediate(item);temporary.Clear();}
        private BuffData Buff(BuffDuplicationType policy=BuffDuplicationType.UsePrev)
        {var data=New<BuffData>();data.DuplicationData.Type=policy;data.DuplicationData.MaxStackCount=10;return data;}
        private static EquipmentAbility Unit(TriggerType type,TargetType target,params EquipmentEffect[] effects)=>new(){trigger=new(){triggerType=type,triggerCount=1,chancePercent=100},target=target,effects=effects.ToList()};
        private static EquipmentEffect E(EffectType type,float value=0,BuffData buff=null)=>new(){effectType=type,effectMagnitude=value,effectCount=1,buff=buff};
        [Test] public void CommandsAreDeferred_ChildStartOccursInNextStateCast_NotRecursive()
        {
            var child=Buff();child.Units.Add(Unit(TriggerType.OnBuffStart,TargetType.Owner,E(EffectType.Heal,7)));
            var parent=Buff();parent.ReleaseData.Type=BuffReleaseType.Instant;parent.Units.Add(Unit(TriggerType.OnBuffStart,TargetType.Owner,E(EffectType.AddBuff,1,child)));
            var before=run.Player.CurrentHp;run.Buffs.Add(parent);Assert.That(run.Buffs.Active,Is.Empty);
            run.Buffs.Tick(0);Assert.That(run.Player.CurrentHp,Is.EqualTo(before));Assert.That(run.Buffs.Has(child),Is.False);
            run.Buffs.Tick(0);Assert.That(run.Player.CurrentHp,Is.EqualTo(before+7));Assert.That(run.Buffs.Has(child),Is.True);
        }
        [TestCase(BuffDuplicationType.None,2,1)] [TestCase(BuffDuplicationType.UsePrev,1,1)]
        [TestCase(BuffDuplicationType.UseNew,1,1)] [TestCase(BuffDuplicationType.StackCount,1,2)]
        public void DuplicatePoliciesMatchSource(BuffDuplicationType policy,int instances,int stacks)
        {var data=Buff(policy);run.Buffs.Add(data);run.Buffs.Tick(0);run.Buffs.Add(data);run.Buffs.Tick(0);Assert.That(run.Buffs.Active.Count,Is.EqualTo(instances));Assert.That(run.Buffs.Active[0].StackCount,Is.EqualTo(stacks));}
        [Test] public void StackCap_RefreshOnStack_NegativeStackEnds_AndEndRunsOnce()
        {
            var data=Buff(BuffDuplicationType.StackCount);data.DuplicationData.MaxStackCount=3;data.ReleaseData=new(){Type=BuffReleaseType.Time,Time=2,ResetOnStack=true};
            data.Units.Add(Unit(TriggerType.OnBuffEnd,TargetType.Owner,E(EffectType.Heal,5)));
            run.Buffs.Add(data);run.Buffs.Tick(0);run.Buffs.Tick(1.5f);run.Buffs.Add(data,9);run.Buffs.Tick(0);
            Assert.That(run.Buffs.Find(data).StackCount,Is.EqualTo(3));run.Buffs.Tick(1);Assert.That(run.Buffs.Has(data),Is.True);
            var hp=run.Player.CurrentHp;run.Buffs.ChangeStack(run.Buffs.Find(data),-3);run.Buffs.Tick(0);run.Buffs.Tick(0);
            Assert.That(run.Player.CurrentHp,Is.EqualTo(hp+5));Assert.That(run.Buffs.Has(data),Is.False);
        }
        [Test] public void MergedUnitsShareOneCadence_OrderedEffects_AllTargetsExecute()
        {
            var data=Buff();var host=Unit(TriggerType.OnGuard,TargetType.Owner,E(EffectType.Heal,2));host.trigger.triggerCount=2;data.Units.Add(host);
            var tail=Unit(TriggerType.OnGuard,TargetType.Owner,E(EffectType.Heal,3));tail.mergeConditionTrigger=true;tail.conditions.Add(new(){kind=AbilityConditionKind.HasSpell});data.Units.Add(tail);
            data.ReleaseData=new(){Type=BuffReleaseType.TriggerCount,Count=1};run.Buffs.Add(data);run.Buffs.Tick(0);var hp=run.Player.CurrentHp;
            run.EmitEvent(TriggerType.OnGuard,default);Assert.That(run.Player.CurrentHp,Is.EqualTo(hp));
            run.EmitEvent(TriggerType.OnGuard,default);Assert.That(run.Player.CurrentHp,Is.EqualTo(hp+5));Assert.That(run.Buffs.Has(data),Is.False);
        }
        [Test] public void TimerIgnoresStaleAttackFilters_CatchesUp_AndPausesWhenConditionIsFalse()
        {
            var data=Buff();var unit=Unit(TriggerType.OnTimer,TargetType.Owner,E(EffectType.Heal,1));unit.trigger.intervalSeconds=.5f;unit.trigger.triggerAttackTypeFilter=TriggerAttackTypeFilter.ChargeAttack;unit.conditions.Add(new(){kind=AbilityConditionKind.GuardHeld});data.Units.Add(unit);
            run.Buffs.Add(data);run.Buffs.Tick(0);var hp=run.Player.CurrentHp;run.Buffs.Tick(10);Assert.That(run.Player.CurrentHp,Is.EqualTo(hp));
            run.Player.TrySetGuard(true);run.Buffs.Tick(1.5f);Assert.That(run.Player.CurrentHp,Is.EqualTo(hp+3));
        }
        [Test] public void RootOwnerAndThisBuffRemainDistinct_EnemyPoisonUsesSameManager()
        {
            var enemyData=New<MonsterData>();enemyData.maxHp=200;var enemy=new MonsterRuntime(enemyData);var data=Buff(BuffDuplicationType.StackCount);
            var damage=E(EffectType.Damage);damage.valueVariables=new(){new(){type=EffectScale.BuffStacks,multiplier=2}};
            var tick=Unit(TriggerType.OnTimer,TargetType.Owner,damage);tick.trigger.intervalSeconds=2;data.Units.Add(tick);
            enemy.Buffs.Add(data,3,run);enemy.Buffs.Tick(0);Assert.That(enemy.Buffs.Find(data).Root,Is.SameAs(run));enemy.Buffs.Tick(2);Assert.That(enemy.CurrentHp,Is.EqualTo(194));Assert.That(run.Player.CurrentHp,Is.EqualTo(50));
        }
        [Test] public void CoreAssetsHaveNoClassSettings_PoisonAndRageAreInspectableBuffs()
        {
            var content=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/Data/GameContent.asset");Assert.That(content.schemaVersion,Is.GreaterThanOrEqualTo(9));
            Assert.That(typeof(PlayerCombatData).GetField("mechanics"),Is.Null);Assert.That(typeof(EquipmentAbility).GetField("editorLabel"),Is.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<BuffData>("Assets/Data/Combat/Buffs/Rage.asset"),Is.Not.Null);
            Assert.That(content.characters.Single(c=>c.character==CharacterKind.Monk).attackBuffs.All(b=>b.attackPattern.Length>0),Is.True);
            Assert.That(CombatStatusCatalog.Instance.poison.Units.Any(u=>u.trigger.triggerType==TriggerType.OnTimer && u.effects.Any(e=>e.effectType==EffectType.Damage)),Is.True);
            var wizard=content.characters.Single(c=>c.character==CharacterKind.Wizard);Assert.That(wizard.startingRelic.abilities.Any(u=>u.trigger.triggerType==TriggerType.Passive && u.effects.Any(e=>e.rule==AbilityRule.SpellCooldownReduction)),Is.True);
        }
        [Test] public void MergedPreHitAndPostHit_UseOneCadenceAndActualResolvedDamage()
        {
            var item=New<EquipmentData>();var host=Unit(TriggerType.OnHit,TargetType.Hit,E(EffectType.ActionDamage,.5f));host.trigger.triggerCount=2;item.abilities.Add(host);
            var heal=E(EffectType.Heal);heal.valueVariables=new(){new(){type=EffectScale.DamageDealt,multiplier=.1f}};var tail=Unit(TriggerType.OnHit,TargetType.Owner,heal);tail.mergeConditionTrigger=true;item.abilities.Add(tail);run.TryEquip(item);
            var packet=new ActionEffectContext();var context=new EquipmentTriggerContext(default,TriggerAttackType.Normal,TriggerAttackWay.Horizontal,false,false,damage:20,mutable:packet);
            var hp=run.Player.CurrentHp;run.FinishHit(run.BeginHit(TriggerType.OnHit,context),20);Assert.That(run.Player.CurrentHp,Is.EqualTo(hp));
            var pending=run.BeginHit(TriggerType.OnHit,context);Assert.That(packet.DamageBonus,Is.EqualTo(.5f));Assert.That(run.Player.CurrentHp,Is.EqualTo(hp));run.FinishHit(pending,30);Assert.That(run.Player.CurrentHp,Is.EqualTo(hp+3));
        }
        [Test] public void AnyCharacterCanEnterAnAuthoredForm_WithoutCharacterKindDispatch()
        {
            var data=Buff();data.ReleaseData=new(){Type=BuffReleaseType.Time,Time=1};var speed=E(EffectType.RuleModifier,-.5f);speed.rule=AbilityRule.CooldownMultiplier;data.Units.Add(Unit(TriggerType.Passive,TargetType.OwnerStat,speed));
            var item=New<EquipmentData>();var enter=E(EffectType.AddBuff,1,data);item.abilities.Add(Unit(TriggerType.OnGuardStarted,TargetType.Owner,enter));run.TryEquip(item);var before=run.Player.CurrentCooldown;
            run.Player.TrySetGuard(true);run.Buffs.Tick(0);Assert.That(run.Buffs.Has(data),Is.True);Assert.That(run.Player.CurrentCooldown,Is.EqualTo(before*.5f).Within(.001));run.Buffs.Tick(1.01f);Assert.That(run.Buffs.Has(data),Is.False);
        }
        [Test] public void BuffDurationModifiersAreScoped_AndPassiveStatsUseTheSharedLifetime()
        {
            var first=Buff();first.ReleaseData=new(){Type=BuffReleaseType.Time,Time=1};var other=Buff();other.ReleaseData=new(){Type=BuffReleaseType.Time,Time=1};
            var item=New<EquipmentData>();var duration=E(EffectType.RuleModifier,.2f,first);duration.rule=AbilityRule.BuffDurationBonus;item.abilities.Add(Unit(TriggerType.Passive,TargetType.OwnerStat,duration));run.TryEquip(item);
            var stat=E(EffectType.StatIncrease,.5f);stat.effectStatType=ModifierKind.NormalDamage;first.Units.Add(Unit(TriggerType.Passive,TargetType.OwnerStat,stat));var damage=run.Player.NormalAttackDamage;
            run.Buffs.Add(first);run.Buffs.Add(other);run.Buffs.Tick(0);Assert.That(run.Buffs.Find(first).Duration,Is.EqualTo(1.2f).Within(.001));Assert.That(run.Buffs.Find(other).Duration,Is.EqualTo(1));Assert.That(run.Player.NormalAttackDamage,Is.EqualTo(damage*1.5f).Within(.001));
            run.Player.EndCharacterCombat();Assert.That(run.Player.NormalAttackDamage,Is.EqualTo(damage));
        }
        [Test] public void ZeroChanceNeverExecutes_AndPermanentBuffSurvivesCombatBoundary()
        {
            var data=Buff();var unit=Unit(TriggerType.OnGuard,TargetType.Owner,E(EffectType.Heal,10));unit.trigger.chancePercent=0;data.Units.Add(unit);
            run.Buffs.Add(data);run.Buffs.Tick(0);var hp=run.Player.CurrentHp;run.EmitEvent(TriggerType.OnGuard,default);Assert.That(run.Player.CurrentHp,Is.EqualTo(hp));run.Player.EndCharacterCombat();Assert.That(run.Buffs.Has(data),Is.True);
        }
        [Test] public void SameAssetNameAndDuplicateTagMatchSmashDungeonIdentityPolicy()
        {
            var first=Buff();var second=Buff();second.name=first.name;run.Buffs.Add(first);run.Buffs.Tick(0);run.Buffs.Add(second);run.Buffs.Tick(0);Assert.That(run.Buffs.Active.Count,Is.EqualTo(1));
            var tag=New<BuffTagData>();first.Tags.Add(tag);var third=Buff();third.DuplicationData.ConsiderTagIsDuplicate=tag;third.DuplicationData.Type=BuffDuplicationType.UseNew;run.Buffs.Add(third);run.Buffs.Tick(0);Assert.That(run.Buffs.Active.Count,Is.EqualTo(1));Assert.That(run.Buffs.Active[0].Data,Is.SameAs(third));
        }
        [Test] public void EquipmentAndBuffPassiveStatsShareConditionsAndMergedUnitSemantics()
        {
            var item=New<EquipmentData>();var normal=E(EffectType.StatIncrease,.25f);normal.effectStatType=ModifierKind.NormalDamage;var host=Unit(TriggerType.Passive,TargetType.OwnerStat,normal);host.conditions.Add(new(){kind=AbilityConditionKind.HpBelowFraction,threshold=.6f});item.abilities.Add(host);
            var charge=E(EffectType.StatIncrease,.5f);charge.effectStatType=ModifierKind.ChargeDamage;var tail=Unit(TriggerType.Passive,TargetType.OwnerStat,charge);tail.mergeConditionTrigger=true;tail.conditions.Add(new(){kind=AbilityConditionKind.HasSpell});item.abilities.Add(tail);
            var normalBase=run.Player.NormalAttackDamage;var chargeBase=run.Player.ChargeAttackDamage;run.TryEquip(item);Assert.That(run.Player.NormalAttackDamage,Is.EqualTo(normalBase*1.25f));Assert.That(run.Player.ChargeAttackDamage,Is.EqualTo(chargeBase*1.5f));
            run.Player.Heal(100);Assert.That(run.Player.NormalAttackDamage,Is.EqualTo(normalBase));Assert.That(run.Player.ChargeAttackDamage,Is.EqualTo(chargeBase));
        }
        [Test] public void TimedBuffCanGrantAndRevokeCompanionAndFormAccess_WithoutAnItemIdentity()
        {
            var companion=New<CompanionData>();var form=New<BuffData>();form.requiresUnlock=true;
            var data=Buff();data.ReleaseData=new(){Type=BuffReleaseType.Time,Time=1};var presence=E(EffectType.SummonCompanion);presence.companion=companion;var access=E(EffectType.UnlockBuff);access.buff=form;data.Units.Add(Unit(TriggerType.Passive,TargetType.OwnerStat,presence,access));
            Assert.That(run.HasCompanion(companion),Is.False);Assert.That(run.IsBuffUnlocked(form),Is.False);run.Buffs.Add(data);run.Buffs.Tick(0);Assert.That(run.HasCompanion(companion),Is.True);Assert.That(run.IsBuffUnlocked(form),Is.True);
            run.Buffs.Tick(1.01f);Assert.That(run.HasCompanion(companion),Is.False);Assert.That(run.IsBuffUnlocked(form),Is.False);
        }
        [Test] public void EquipmentAndBuffTimersPauseAndCatchUpIdentically()
        {
            EquipmentAbility Timer(){var unit=Unit(TriggerType.OnTimer,TargetType.Owner,E(EffectType.Heal,1));unit.trigger.intervalSeconds=.5f;unit.conditions.Add(new(){kind=AbilityConditionKind.GuardHeld});return unit;}
            var item=New<EquipmentData>();item.abilities.Add(Timer());run.TryEquip(item);var data=Buff();data.Units.Add(Timer());run.Buffs.Add(data);run.Buffs.Tick(0);
            var hp=run.Player.CurrentHp;run.Player.Tick(5);Assert.That(run.Player.CurrentHp,Is.EqualTo(hp));run.Player.TrySetGuard(true);run.Player.Tick(1.5f);Assert.That(run.Player.CurrentHp,Is.EqualTo(hp+6));
        }
        [Test] public void MigratedTemporaryDamageStatAddsToTheSamePermanentStatGroup()
        {
            run.Player.ApplyModifiers(new[]{new StatModifier{kind=ModifierKind.AllDamageMultiplier,value=.25f}});
            var data=Buff();data.ReleaseData=new(){Type=BuffReleaseType.Time,Time=1};var bonus=E(EffectType.RuleModifier,.3f);bonus.rule=AbilityRule.DamageMultiplier;data.Units.Add(Unit(TriggerType.Passive,TargetType.OwnerStat,bonus));run.Buffs.Add(data);run.Buffs.Tick(0);
            Assert.That(run.Player.AllDamageMultiplier,Is.EqualTo(1.55f).Within(.001));run.Player.EndCharacterCombat();Assert.That(run.Player.AllDamageMultiplier,Is.EqualTo(1.25f).Within(.001));
        }
    }
}
