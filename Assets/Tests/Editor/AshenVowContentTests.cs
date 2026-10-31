using System;
using System.Linq;
using DungeonSlash;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DungeonSlash.Tests
{
    public sealed class AshenVowContentTests
    {
        private GameContent content;
        private RunBalanceSettings balance;
        [SetUp] public void SetUp()
        {
            content=DungeonSlash.DesignerAssetPaths.Load<GameContent>("Assets/Data/GameContent.asset");
            balance=DungeonSlash.DesignerAssetPaths.Load<RunBalanceSettings>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:RunBalanceSettings").First()));
        }
        private RunState Run(CharacterKind kind)
        {var run=new RunState(content.characters.First(c=>c.character==kind),balance);run.TryEquip(run.Player.Data.startingRelic);run.BeginCombat();return run;}
        private void Take(RunState run,string id,int count=1)
        {var system=new PerkSystem(content.perks);for(var i=0;i<count;i++)system.Apply(run,content.perks.Single(p=>p.id==id));}
        private MonsterRuntime Target()=>new(content.stages[0].earlyCombats.First().members.First().monster);
        [Test] public void Pools_ContainAllThirtyNine_AndEveryClassGetsOnlyItsFivePlusNineShared()
        {
            Assert.That(content.perks.Count,Is.EqualTo(39));
            foreach(CharacterKind kind in Enum.GetValues(typeof(CharacterKind)))
            {
                var run=Run(kind);var pool=content.perks.Where(p=>PerkSystem.IsAvailable(p,run)).ToArray();
                Assert.That(pool.Length,Is.EqualTo(14),kind.ToString());Assert.That(pool.Count(p=>p.scope==RelicScope.Exclusive),Is.EqualTo(5));
                Assert.That(pool.All(p=>p.maxStacks>1),Is.True);
                var system=new PerkSystem(content.perks);for(var seed=0;seed<10;seed++)Assert.That(system.GetChoices(run,seed).All(pool.Contains),Is.True);
            }
        }
        [Test] public void Mp_ThreeBoltsFitButFourDoNot_EncounterRefills_StackedCapacityChangesThat()
        {
            var run=Run(CharacterKind.Warrior);Assert.That(run.Player.MaxMp,Is.EqualTo(30));
            for(var i=0;i<3;i++)Assert.That(run.Player.TrySpendMp(8),Is.True);
            Assert.That(run.Player.TrySpendMp(8),Is.False);Assert.That(run.Player.CurrentMp,Is.EqualTo(6));
            run.BeginCombat();Assert.That(run.Player.CurrentMp,Is.EqualTo(30));
            Take(run,"ManaVessel");run.BeginCombat();for(var i=0;i<4;i++)Assert.That(run.Player.TrySpendMp(8),Is.True);
        }
        [Test] public void CounterAndPoison_SecondCopyDoublesMagnitudeWithoutDuplicatingTrigger()
        {
            var run=Run(CharacterKind.Warrior);Take(run,"Counter",2);run.EmitEvent(TriggerType.OnGuard,default);run.Buffs.Tick(0);run.Buffs.Tick(0);
            var target=Target();var context=new EquipmentTriggerContext(default,TriggerAttackType.Normal,TriggerAttackWay.Horizontal,false,false,target);
            var hit=run.BeginHit(TriggerType.OnHit,context);var damage=hit.Where(h=>h.Effect.effectType==EffectType.Damage).Sum(h=>run.EffectValue(h.Effect,h.Context));
            Assert.That(damage,Is.EqualTo(12).Within(.001f));
            var rogue=Run(CharacterKind.Rogue);Take(rogue,"PoisonBlade",2);var poison=rogue.BeginHit(TriggerType.OnHit,context).Where(h=>h.Effect.effectType==EffectType.AddBuff && h.Effect.buff==CombatStatusCatalog.Instance.poison).ToArray();
            Assert.That(poison.Length,Is.EqualTo(1));Assert.That(rogue.EffectValue(poison[0].Effect,poison[0].Context),Is.EqualTo(2));
        }
        [Test] public void Assassination_StacksAndStillWorksOnPoisonedTarget()
        {
            var run=Run(CharacterKind.Rogue);Take(run,"Assassination",2);var target=Target();target.AddPoison(3);
            var context=new EquipmentTriggerContext(default,TriggerAttackType.Normal,TriggerAttackWay.Horizontal,false,false,target);
            Assert.That(run.Rule(AbilityRule.FlatWeaponDamage,context),Is.EqualTo(24));run.CompleteAction(TriggerType.OnAttack,context);
            Assert.That(run.Rule(AbilityRule.FlatWeaponDamage,context),Is.EqualTo(0));
        }
        [Test] public void SharedElements_AlsoScaleEnchantDamageForNonWizard()
        {
            var run=Run(CharacterKind.Warrior);Take(run,"FireMastery",2);Assert.That(run.Player.ElementMultiplier(SpellElement.Fire),Is.EqualTo(1.2f).Within(.001f));
            Assert.That(run.Player.ElementMultiplier(SpellElement.Ice),Is.EqualTo(1));
            Assert.That(content.spells.Where(s=>s.id.StartsWith("Enchant")).Count(),Is.EqualTo(3));
            foreach(var spell in content.spells.Where(s=>s.id.StartsWith("Enchant")))Assert.That(spell.abilities.SelectMany(a=>a.effects).Single().buff.Units.Any(a=>a.effects.Any(e=>e.effectType==EffectType.ElementDamage)),Is.True);
        }
        [Test] public void AdvancedLearning_PreservesEarlierSpell_AndBothCanBeSelected()
        {
            var run=Run(CharacterKind.Wizard);var old=content.spells.Single(s=>s.id=="FireBloom");var advanced=content.spells.Single(s=>s.id=="Meteor");
            Assert.That(run.LearnSpell(old,true),Is.True);Assert.That(run.LearnSpell(advanced,true),Is.True);
            Assert.That(run.RuntimeFor(old),Is.Not.Null);Assert.That(run.RuntimeFor(advanced),Is.Not.Null);
            Assert.That(run.Slot(old.defaultSlot),Is.EqualTo(advanced));Assert.That(run.EquipKnownSpell(old),Is.True);Assert.That(run.Slot(old.defaultSlot),Is.EqualTo(old));
        }
        [Test] public void LearningWithoutEquipping_PreservesPreparedSpellAndKeepsNewSpell()
        {
            var run=Run(CharacterKind.Warrior);var old=content.spells.Single(s=>s.id=="FireBloom");var advanced=content.spells.Single(s=>s.id=="Meteor");
            run.LearnSpell(old,true);Assert.That(run.RememberSpell(advanced),Is.True);
            Assert.That(run.Slot(old.defaultSlot),Is.EqualTo(old));Assert.That(run.RuntimeFor(advanced),Is.Not.Null);
            Assert.That(run.EquipKnownSpell(advanced),Is.True);
        }
        [Test] public void WizardGuardRecovery_AndMpPerk_AddToSmallNaturalRecovery()
        {
            var run=Run(CharacterKind.Wizard);Take(run,"ManaFlow",2);run.Player.TrySpendMp(20);run.EmitEvent(TriggerType.OnGuardStarted,default);
            run.Player.TrySetGuard(true);var before=run.Player.CurrentMp;run.Player.Tick(1);
            Assert.That(run.Player.CurrentMp-before,Is.EqualTo(1.1f).Within(.02f));
        }
        [Test] public void Narrative_AllTwentyCastleEventsHavePages_TeacherGrowthMissConsumesDifferentScene()
        {
            Assert.That(content.narrativeRevision,Is.GreaterThan(0));Assert.That(content.events.Count,Is.EqualTo(23));Assert.That(content.events.All(e=>e.pages.Count>0),Is.True);
            var teacher=content.events.Single(e=>e.id=="MagicTeacherReturn");var run=Run(CharacterKind.Warrior);
            var route=teacher.entryRoutes.First(r=>r.requirements.All(q=>q.Matches(run)));Assert.That(route.page,Is.EqualTo(4));
            Assert.That(teacher.pages[4].narrative,Does.Contain("아직 살아 있었군요"));Assert.That(teacher.pages[4].options.SelectMany(o=>o.effects).Any(e=>e.kind==EventEffectKind.LearnSpell||e.kind==EventEffectKind.Heal),Is.False);
        }
        [Test] public void EveryPortraitIconAndEnemyArtIsAuthored_AndAllCastsAreShort()
        {
            Assert.That(content.artRevision,Is.GreaterThan(0));Assert.That(content.characters.All(c=>c.portrait!=null&&c.combatSprite!=null),Is.True);
            Assert.That(content.perks.All(p=>p.icon!=null),Is.True);Assert.That(content.equipment.All(e=>e.icon!=null),Is.True);Assert.That(content.spells.All(s=>s.icon!=null&&s.castingSeconds<=.6f&&s.mpCost>=8),Is.True);
            foreach(var guid in AssetDatabase.FindAssets("t:MonsterData",new[]{"Assets/Data"}))Assert.That(DungeonSlash.DesignerAssetPaths.Load<MonsterData>(AssetDatabase.GUIDToAssetPath(guid)).portrait,Is.Not.Null,AssetDatabase.GUIDToAssetPath(guid));
        }
        private BuffData Buff(string name)=>DungeonSlash.DesignerAssetPaths.Load<BuffData>("Assets/Data/Combat/Buffs/"+name+".asset");
        private static void Flush(RunState run){run.Buffs.Tick(0);run.Buffs.Tick(0);}
        private static float Bonus(RunState run,EquipmentTriggerContext context)
        {return run.BeginHit(TriggerType.OnHit,context).Where(e=>e.Effect.effectType==EffectType.Damage).Sum(e=>run.EffectValue(e.Effect,e.Context));}
        [Test] public void FlowFinish_PaysOnlyOnFinalEnhancedNormal_AndAMissBreaksIt()
        {
            foreach(var miss in new[]{false,true})
            {
                var run=Run(CharacterKind.Monk);Take(run,"FlowFinish",2);var flow=Buff("FlowAcceleration");
                run.Buffs.Add(flow,2,run);Flush(run);
                var context=new EquipmentTriggerContext(default,TriggerAttackType.Normal,TriggerAttackWay.Horizontal,false,false,Target());
                run.StartAction(context);Assert.That(Bonus(run,context),Is.EqualTo(0));
                if(!miss)run.FinishHit(new(),0);
                run.EmitEvent(TriggerType.OnAttackResolved,context);Flush(run);
                run.StartAction(context);Assert.That(Bonus(run,context),Is.EqualTo(miss?0:20),"miss="+miss);
            }
        }
        [Test] public void Samurai_SheathedTimeIsCapturedBeforeDraw_AndEachCopyAddsRange()
        {
            var run=Run(CharacterKind.Samurai);Take(run,"PatientDraw",2);Take(run,"LongBlade",2);
            var sheath=Buff("Sheathed");run.Buffs.Add(sheath,1,run);Flush(run);run.Buffs.Tick(2);
            var drawTag=DungeonSlash.DesignerAssetPaths.Load<BuffTagData>("Assets/Data/Combat/BuffTags/DrawAttack.asset");
            var context=new EquipmentTriggerContext(default,TriggerAttackType.Charge,TriggerAttackWay.Horizontal,true,false,Target(),actionTags:new[]{drawTag});
            run.StartAction(context);run.Buffs.Remove(sheath);Flush(run);
            Assert.That(run.Rule(AbilityRule.FlatWeaponDamage,context),Is.EqualTo(24).Within(.01));
            Assert.That(run.Rule(AbilityRule.DrawReachBonus,context),Is.EqualTo(.16f).Within(.001));
        }
        [Test] public void Wizard_ElementSequenceConsumesMpUntilThirdDistinctElement_AndRepeatedElementGetsBonus()
        {
            var run=Run(CharacterKind.Wizard);Take(run,"ThreeElements",2);Take(run,"ElementFocus",2);run.Player.TrySpendMp(20);
            var spells=new[]{"Ember","EnergyBolt","Spark"}.Select(id=>content.spells.Single(s=>s.id==id)).ToArray();
            foreach(var spell in spells)
            {
                var ctx=new EquipmentTriggerContext(default,TriggerAttackType.None,TriggerAttackWay.None,false,false,spell:spell);
                run.EmitEvent(TriggerType.OnSpellCast,ctx);run.CompleteAction(TriggerType.OnSpellCast,ctx);
            }
            Assert.That(run.Player.CurrentMp,Is.EqualTo(18).Within(.01));
            var same=new EquipmentTriggerContext(default,TriggerAttackType.None,TriggerAttackWay.None,false,false,spell:spells[2]);
            Assert.That(run.Rule(AbilityRule.SpellDamageBonus,same),Is.EqualTo(.3f).Within(.001));
        }
        [Test] public void Barbarian_RageEntryUsesFurySnapshot_AndRageEndRestoresStackedEmbers()
        {
            var run=Run(CharacterKind.Barbarian);Take(run,"ConversionImpact",2);Take(run,"Embers",2);
            var fury=Buff("Fury");var rage=Buff("Rage");run.Buffs.Add(fury,40,run);Flush(run);
            var ctx=new EquipmentTriggerContext(default,TriggerAttackType.Charge,TriggerAttackWay.Horizontal,true,false,Target());
            run.StartAction(ctx);run.Buffs.Remove(fury);run.Buffs.Add(rage,40,run);Flush(run);
            Assert.That(Bonus(run,ctx),Is.EqualTo(8).Within(.01));
            run.Buffs.Remove(rage);Flush(run);Flush(run);Assert.That(run.Buffs.Find(fury)?.StackCount,Is.EqualTo(8));
        }
        [Test] public void PoisonBonus_AlsoAppliesToDetonationValue()
        {
            var run=Run(CharacterKind.Rogue);var target=Target();
            target.ApplyStatus(StatusKind.Poison,stacks:3,root:run);target.Buffs.Tick(0);
            var before=target.Buffs.PeriodicDamage(target.Buffs.Find(CombatStatusCatalog.Instance.poison));Take(run,"ConcentratedVenom",2);
            Assert.That(target.Buffs.PeriodicDamage(target.Buffs.Find(CombatStatusCatalog.Instance.poison)),Is.EqualTo(before*1.3f).Within(.01));
        }
    }
}
