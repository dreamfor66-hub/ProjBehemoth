using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

// In-place schema migration only. Asset GUIDs and designer combat numbers are preserved.
internal static class BehemothBuffMigration
{
    private const string Root = "Assets/Data/Behemoth";
    private static BuffData sheath, ready, rage, parry, perfect;
    private static BuffTagData parrySignal, perfectSignal;
    private static CombatResourceData resource;
    [InitializeOnLoadMethod] private static void Schedule() => EditorApplication.delayCall += Run;
    internal static void Run()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.delayCall += Run; return; }
        var content = AssetDatabase.LoadAssetAtPath<GameContent>(BehemothContentBuilder.ContentPath);
        if (content == null || content.schemaVersion < 8 || content.schemaVersion >= 9) return;
        try
        {
            Directory.CreateDirectory(Root + "/Buffs"); Directory.CreateDirectory(Root + "/BuffTags"); Directory.CreateDirectory(Root + "/Resources"); AssetDatabase.Refresh();
            var catalog = Asset<CombatStatusCatalog>(Root + "/Resources/CombatStatusCatalog.asset");
            catalog.poison = Status("Poison", "독", 8, 8, 2, 1);
            catalog.burn = Status("Burn", "화상", 4, 1, 1, 1);
            catalog.slow = Status("Slow", "둔화", 2, 1, 0, 0);
            catalog.lightningRod = Status("LightningRod", "피뢰침", 8, 1, 0, 0);
            Dirty(catalog);
            resource = Asset<CombatResourceData>(Root + "/Resources/Fury.asset"); resource.displayName = "격노"; resource.maximum = 100; resource.showInHud = true; Dirty(resource);
            sheath = Buff("Sheathed", "납도", BuffReleaseType.Stage); sheath.showInHud = true;
            ready = Buff("PreparedDraw", "차지 발도 준비", BuffReleaseType.Stage); ready.showInHud = true;
            parrySignal = Tag("SheathParry"); perfectSignal = Tag("PerfectGuard");
            parry = Buff("SheathParryWindow", "납도 패리", BuffReleaseType.Time, .1f);
            perfect = Buff("PerfectGuardWindow", "퍼펙트 가드", BuffReleaseType.Time, .1f);
            var rageForm = Asset<FormData>(Root + "/Forms/Rage.asset"); rageForm.displayName = "분노"; rageForm.pattern = Array.Empty<SwipeDirection>(); rageForm.requiresUnlock = false;
            rage = Buff("Rage", "분노", BuffReleaseType.Stage); rage.form = rageForm; rage.showInHud = true; rage.tintOwner = true; rage.presentationColor = new Color(1,.32f,.14f); rageForm.buff = rage; Dirty(rageForm);

            var barbarian = content.characters.Single(c => c.character == CharacterKind.Barbarian);
            var samurai = content.characters.Single(c => c.character == CharacterKind.Samurai);
            var wizard = content.characters.Single(c => c.character == CharacterKind.Wizard);
            var barbRaw = File.ReadAllText(AssetDatabase.GetAssetPath(barbarian)); var samRaw = File.ReadAllText(AssetDatabase.GetAssetPath(samurai)); var wizRaw = File.ReadAllText(AssetDatabase.GetAssetPath(wizard));
            barbarian.resources.Add(resource); barbarian.forms.Add(rageForm); Dirty(barbarian);

            // Convert existing effects before composing the new core states.
            foreach (var item in content.equipment)
            {
                foreach (var unit in item.abilities) ConvertUnit(unit, item.name, catalog);
                Dirty(item);
            }
            foreach (var guid in AssetDatabase.FindAssets("t:FormData", new[] { Root + "/Forms" }))
            {
                var form = AssetDatabase.LoadAssetAtPath<FormData>(AssetDatabase.GUIDToAssetPath(guid)); if (form == rageForm) continue;
                var parent = Buff("Form_" + form.name, form.displayName, BuffReleaseType.Instant); parent.form = form;
                var unit = Unit(TriggerType.OnBuffStart, TargetType.Owner, form.effects.ToArray()); ConvertUnit(unit, "Form_" + form.name, catalog);
                parent.Units.Add(unit); form.effects.Clear(); form.buff = parent; Dirty(parent); Dirty(form);
            }

            var draw = E(EffectType.ActionDamage, F(samRaw,"drawDamageMultiplier",1.5f)-1);
            draw.valueVariables = new() { V(EffectScale.Rule,1, rule: AbilityRule.ActionDamageBonus) };
            var extraDraw = E(EffectType.ActionExtraHits,0); extraDraw.valueVariables = new() { V(EffectScale.Rule,1,rule:AbilityRule.ActionExtraHits) };
            sheath.Units.Add(Unit(TriggerType.OnActionPreparing, TargetType.Action, E(EffectType.MarkAction,1), draw, extraDraw));
            sheath.Units.Add(Unit(TriggerType.OnAttackResolved, TargetType.ThisBuff, E(EffectType.RemoveBuff,0)));
            ready.Units.Add(Unit(TriggerType.OnActionPreparing, TargetType.Action, E(EffectType.ActionCharge,1)));
            ready.Units.Add(Unit(TriggerType.OnAttackResolved, TargetType.ThisBuff, E(EffectType.RemoveBuff,0)));
            parry.ReleaseData.Time = F(samRaw,"sheathParrySeconds",.1f);
            var parryCost=E(EffectType.GuardCost,0); parryCost.guardFeedback=GuardFeedback.Parry;
            parry.Units.Add(Unit(TriggerType.OnBeforeGuard, TargetType.Hit, parryCost));
            parry.Units.Add(Unit(TriggerType.OnBeforeGuard, TargetType.Owner, Grant(ready), Signal(parrySignal)));
            var perfectCost=E(EffectType.GuardCost,0); perfectCost.guardFeedback=GuardFeedback.Perfect;
            perfect.Units.Add(Unit(TriggerType.OnBeforeGuard, TargetType.Hit, perfectCost));
            perfect.Units.Add(Unit(TriggerType.OnBeforeGuard, TargetType.Owner, Signal(perfectSignal)));
            var sheathe = Unit(TriggerType.OnGuardStarted, TargetType.Owner, Grant(sheath), Grant(parry)); sheathe.conditions.Add(Has(sheath,true)); samurai.startingRelic.abilities.Add(sheathe);

            var start = E(EffectType.SetResource,100); start.resource = resource;
            rage.Units.Add(Unit(TriggerType.OnBuffStart, TargetType.Owner,start));
            rage.Units.Add(Passive(AbilityRule.DamageMultiplier,F(barbRaw,"enragedDamageMultiplier",1.3f)-1));
            rage.Units.Add(Passive(AbilityRule.CooldownMultiplier,F(barbRaw,"enragedCooldownMultiplier",.8f)-1));
            rage.Units.Add(Passive(AbilityRule.DamageReduction,F(barbRaw,"enragedDamageReduction",.25f)));
            var drain = E(EffectType.AddResource,-F(barbRaw,"rageDrainPerSecond",25)*.05f); drain.resource = resource; drain.valueVariables = new() { V(EffectScale.Rule,.05f,rule:AbilityRule.ResourceDrainReduction) };
            var drainUnit = Unit(TriggerType.OnTimer, TargetType.Owner, drain); drainUnit.trigger.intervalSeconds = .05f; rage.Units.Add(drainUnit);
            var end = Unit(TriggerType.OnConditionMet, TargetType.ThisBuff, E(EffectType.RemoveBuff,0)); end.conditions.Add(Resource(0,Comparator.LessOrEqual)); rage.Units.Add(end);
            rage.Units.Add(Unit(TriggerType.OnBuffEnd,TargetType.Owner,E(EffectType.PayDeferredDamage,0)));
            var starter = barbarian.startingRelic;
            var gain = E(EffectType.AddResource,F(barbRaw,"ragePerNormalAttack",12)); gain.resource = resource; gain.valueVariables = new() { V(EffectScale.Rule,1,rule:AbilityRule.ResourceGainBonus) };
            var gainUnit = Unit(TriggerType.OnActionPreparing,TargetType.Owner,gain); gainUnit.trigger.triggerAttackTypeFilter = TriggerAttackTypeFilter.NormalAttack; gainUnit.conditions.Add(Has(rage,true)); starter.abilities.Add(gainUnit);
            var enter = E(EffectType.ApplyForm,0); enter.form = rageForm;
            var enterUnit = Unit(TriggerType.OnActionPreparing,TargetType.Owner,enter); enterUnit.trigger.triggerAttackTypeFilter = TriggerAttackTypeFilter.ChargeAttack; enterUnit.conditions.Add(Has(rage,true)); enterUnit.conditions.Add(Resource(F(barbRaw,"rageThreshold",40),Comparator.GreaterOrEqual)); starter.abilities.Add(enterUnit);
            var idle = E(EffectType.AddResource,-F(barbRaw,"rageIdleDecay",2)*.05f); idle.resource = resource;
            var idleUnit = Unit(TriggerType.OnTimer,TargetType.Owner,idle); idleUnit.trigger.intervalSeconds=.05f; idleUnit.conditions.Add(Has(rage,true)); starter.abilities.Add(idleUnit);
            wizard.startingRelic.abilities.Add(Passive(AbilityRule.SpellCooldownReduction,1-F(wizRaw,"wizardCooldownMultiplier",.75f)));
            var recovery = Passive(AbilityRule.GuardRecoveryPerSecond,F(wizRaw,"castingGuardRegen",.75f)); recovery.conditions.Add(new() { kind=AbilityConditionKind.GuardHeld }); recovery.conditions.Add(new() { kind=AbilityConditionKind.HasSpell }); wizard.startingRelic.abilities.Add(recovery);

            // Stack-based horizontal acceleration, expressed entirely as Buff Units.
            var twin = content.equipment.FirstOrDefault(e=>e.power==RelicPower.TwinDaggers);
            if (twin != null)
            {
                var speed = Buff("HorizontalFlow","가속",BuffReleaseType.Stage); speed.DuplicationData.Type=BuffDuplicationType.StackCount; speed.DuplicationData.MaxStackCount=10;
                var bonus=E(EffectType.RuleModifier,0); bonus.rule=AbilityRule.AttackCooldownReduction; bonus.valueVariables=new(){V(EffectScale.BuffStacks,.04f)}; speed.Units.Add(Unit(TriggerType.Passive,TargetType.OwnerStat,bonus));
                var reset=Unit(TriggerType.OnActionPreparing,TargetType.ThisBuff,E(EffectType.RemoveBuff,0)); reset.conditions.Add(new(){kind=AbilityConditionKind.Horizontal,invert=true}); speed.Units.Add(reset);
                twin.abilities.Clear(); var add=Unit(TriggerType.OnActionPreparing,TargetType.Owner,Grant(speed)); add.trigger.triggerAttackTypeFilter=TriggerAttackTypeFilter.NormalAttack; add.conditions.Add(new(){kind=AbilityConditionKind.Horizontal}); twin.abilities.Add(add); Dirty(speed); Dirty(twin);
            }
            foreach (var item in new UnityEngine.Object[]{sheath,ready,parry,perfect,rage,starter,samurai.startingRelic,wizard.startingRelic}) Dirty(item);
            content.schemaVersion=9; Dirty(content); AssetDatabase.SaveAssets(); BehemothTextEditor.PrepareForVerification();
            Debug.Log("Behemoth: Buff/Form schema 9 migrated in place; reference combat values preserved.");
        }
        catch(Exception e) { Debug.LogException(e); }
    }
    private static void ConvertUnit(EquipmentAbility unit,string owner,CombatStatusCatalog catalog)
    {
        unit.trigger.chancePercent=100;
        if (unit.trigger.triggerType==TriggerType.OnSheathParry) { unit.trigger.triggerType=TriggerType.OnSignal; unit.trigger.signal=parrySignal; }
        if (unit.trigger.triggerType==TriggerType.OnPerfectGuard) { unit.trigger.triggerType=TriggerType.OnSignal; unit.trigger.signal=perfectSignal; }
        foreach(var condition in unit.conditions)
        {
            if(condition.kind==AbilityConditionKind.Enraged) { condition.kind=AbilityConditionKind.OwnerHasBuff; condition.buff=rage; }
            if(condition.kind==AbilityConditionKind.Sheathed) { condition.kind=AbilityConditionKind.OwnerHasBuff; condition.buff=sheath; }
        }
        for(var i=0;i<unit.effects.Count;i++)
        {
            var fx=unit.effects[i];
            if(fx.effectType==EffectType.AddResource) fx.resource=resource;
            if(fx.scaleRatio!=0)
            {
                fx.valueVariables=new(){V(fx.scale==EffectScale.Rage?EffectScale.Resource:fx.scale==EffectScale.SheathTime?EffectScale.BuffElapsed:fx.scale,fx.scaleRatio,fx.scale==EffectScale.SheathTime?sheath:null,fx.scale==EffectScale.Rage?resource:null)};
                fx.scaleRatio=0; fx.scale=EffectScale.Flat;
            }
            if(fx.effectType==EffectType.ApplyStatus)
            {
                fx.effectType=EffectType.AddBuff; fx.buff=catalog.Get(fx.status);
                if(fx.status==StatusKind.Poison) unit.conditions.Add(new(){kind=AbilityConditionKind.Rule,rule=AbilityRule.BlockBuff,comparator=Comparator.LessOrEqual,threshold=0});
            }
            if(fx.effectType==EffectType.SpreadStatus) fx.buff=catalog.poison;
            if(fx.effectType==EffectType.RuleModifier && fx.rule==AbilityRule.PerfectGuardWindow)
            { perfect.ReleaseData.Time=fx.effectMagnitude; fx=Grant(perfect); unit.trigger.triggerType=TriggerType.OnGuardStarted; }
            if(fx.effectType==EffectType.RuleModifier && fx.rule is AbilityRule.ActionDamageBonus or AbilityRule.ActionExtraHits)
                if(!unit.conditions.Any(c=>c.buff==sheath)) unit.conditions.Add(Has(sheath));
            if(unit.trigger.triggerType!=TriggerType.Passive && (fx.effectType==EffectType.RuleModifier || fx.effectType==EffectType.StatIncrease && (fx.effectTime>0 || fx.buffUses>0 || !string.IsNullOrEmpty(fx.buffGroup))))
            {
                var uses=fx.buffUses; var state=Buff(owner+"_"+i+"_"+(string.IsNullOrEmpty(fx.buffGroup)?fx.rule.ToString():fx.buffGroup),owner,BuffReleaseType.Stage);
                if(fx.effectTime>0) { state.ReleaseData.Type=BuffReleaseType.Time; state.ReleaseData.Time=fx.effectTime; }
                if(uses>0) { state.DuplicationData.Type=BuffDuplicationType.StackCount; state.DuplicationData.MaxStackCount=20; }
                else state.DuplicationData.Type=BuffDuplicationType.UseNew;
                if(fx.effectType==EffectType.StatIncrease) { fx.effectType=EffectType.RuleModifier; fx.rule=StatRule(fx.effectStatType); }
                fx.effectTime=0; fx.buffUses=0; fx.buffGroup=null;
                if(uses>0) { fx.valueVariables??=new(); fx.valueVariables.Add(V(EffectScale.Rule,1,rule:AbilityRule.BuffPotencyBonus)); }
                state.Units.Add(Unit(TriggerType.Passive,TargetType.OwnerStat,fx));
                if(uses>0)
                {
                    var consume=Unit(fx.consumeOn==TriggerType.OnAttack?TriggerType.OnAttackResolved:fx.consumeOn,TargetType.ThisBuff,E(EffectType.ChangeBuffStack,-1)); consume.trigger.triggerAttackTypeFilter=fx.consumeAttackTypes; state.Units.Add(consume);
                }
                var grant=Grant(state,Mathf.Max(1,uses)); if(uses>0) grant.valueVariables=new(){V(EffectScale.Rule,1,rule:AbilityRule.BuffExtraUses)};
                fx=grant; Dirty(state);
            }
            unit.effects[i]=fx;
        }
    }
    private static AbilityRule StatRule(ModifierKind kind)=>kind switch { ModifierKind.AllDamageMultiplier=>AbilityRule.DamageMultiplier,ModifierKind.DamageReduction=>AbilityRule.DamageReduction,ModifierKind.AttackCooldownMultiplier=>AbilityRule.AttackCooldownReduction,ModifierKind.NormalDamage=>AbilityRule.NormalDamageBonus,ModifierKind.ChargeDamage=>AbilityRule.ChargeDamageBonus,_=>throw new InvalidOperationException("Unsupported transient stat: "+kind) };
    private static BuffData Status(string id,string name,float seconds,int cap,float interval,float damage)
    {
        var data=Buff(id,name,BuffReleaseType.Time,seconds); var tag=Tag(id); data.Tags.Add(tag); data.DuplicationData.ConsiderTagIsDuplicate=tag;
        data.DuplicationData.Type=cap>1?BuffDuplicationType.StackCount:BuffDuplicationType.UseNew; data.DuplicationData.MaxStackCount=cap; data.ReleaseData.ResetOnStack=true;
        if(interval>0) { var fx=E(EffectType.Damage,0); fx.valueVariables=new(){V(EffectScale.BuffStacks,damage)}; var unit=Unit(TriggerType.OnTimer,TargetType.Owner,fx); unit.trigger.intervalSeconds=interval; data.Units.Add(unit); }
        Dirty(data); return data;
    }
    private static BuffData Buff(string id,string name,BuffReleaseType type,float time=0)
    { var data=Asset<BuffData>(Root+"/Buffs/"+id+".asset"); data.displayName=name; data.Units.Clear(); data.Tags.Clear(); data.ReleaseData=new(){Type=type,Time=time,Count=1}; data.DuplicationData=new(){Type=BuffDuplicationType.UsePrev,MaxStackCount=1}; Dirty(data); return data; }
    private static BuffTagData Tag(string id)=>Asset<BuffTagData>(Root+"/BuffTags/"+id+".asset");
    private static T Asset<T>(string path) where T:ScriptableObject { var data=AssetDatabase.LoadAssetAtPath<T>(path); if(data!=null)return data; data=ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(data,path); return data; }
    private static void Dirty(UnityEngine.Object data)=>EditorUtility.SetDirty(data);
    private static float F(string text,string key,float fallback) { var match=Regex.Match(text,@"(?m)^\s+"+key+@":\s*([-\d.]+)"); return match.Success?float.Parse(match.Groups[1].Value,CultureInfo.InvariantCulture):fallback; }
    private static EquipmentEffect E(EffectType type,float value)=>new(){effectType=type,effectMagnitude=value,effectCount=1};
    private static EquipmentEffect Grant(BuffData data,int count=1) {var fx=E(EffectType.AddBuff,count);fx.buff=data;return fx;}
    private static EquipmentEffect Signal(BuffTagData data) {var fx=E(EffectType.EmitSignal,0);fx.buffTag=data;return fx;}
    private static EquipmentAbility Unit(TriggerType trigger,TargetType target,params EquipmentEffect[] effects)=>new(){trigger=new(){triggerType=trigger,triggerCount=1,triggerAttackTypeFilter=TriggerAttackTypeFilter.All,triggerAttackWayFilter=TriggerAttackWayFilter.All,chancePercent=100,resetCountEachCombat=true},target=target,effects=effects.ToList()};
    private static EquipmentAbility Passive(AbilityRule rule,float value){var fx=E(EffectType.RuleModifier,value);fx.rule=rule;return Unit(TriggerType.Passive,TargetType.OwnerStat,fx);}
    private static AbilityCondition Has(BuffData data,bool invert=false)=>new(){kind=AbilityConditionKind.OwnerHasBuff,buff=data,invert=invert};
    private static AbilityCondition Resource(float value,Comparator comparator)=>new(){kind=AbilityConditionKind.Resource,resource=resource,threshold=value,comparator=comparator};
    private static EffectValueVariable V(EffectScale type,float multiplier,BuffData buff=null,CombatResourceData resource=null,AbilityRule rule=0)=>new(){type=type,multiplier=multiplier,buff=buff,resource=resource,rule=rule};
}
