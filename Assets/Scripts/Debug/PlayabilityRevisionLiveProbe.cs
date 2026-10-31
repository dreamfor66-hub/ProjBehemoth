#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonSlash;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class PlayabilityRevisionLiveProbe:MonoBehaviour
{
    const string Folder="Docs/Production/PlayabilityRevision-2026-10-30/";
    const BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    CombatController combat;CombatSceneView scene;CombatInputController input;RunController run;
    OctoberReviewLiveProbe practice;
    int failed,passed;bool background;
    string log="Live.txt";
    void Awake(){background=Application.runInBackground;Application.runInBackground=true;}
    void OnDestroy(){Application.runInBackground=background;if(input!=null)input.GuardModifierForVerification=null;}
    T Ref<T>(object target,string field)=>(T)target.GetType().GetField(field,Fields).GetValue(target);
    object Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,Fields).Invoke(target,args);
    void Check(bool value,string name){if(value)passed++;else failed++;File.AppendAllText(Folder+log,(value?"PASS ":"FAIL ")+name+"\n");}
    void Contact(string name,params object[] args)=>Call(input,name,args);
    Vector2 ScreenAt(Vector2 point)=>RectTransformUtility.WorldToScreenPoint(scene.UiCamera,scene.LogicalArea.TransformPoint(point));
    Vector2 MonsterOrigin()=>(Vector2)scene.LogicalArea.InverseTransformPoint(Object.FindFirstObjectByType<MonsterView>().Root.position);
    EquipmentData Item(string id)=>run.Equipment.Single(i=>i.id==id);
    void Setup()
    {
        practice??=new GameObject("Playability practice").AddComponent<OctoberReviewLiveProbe>();
        practice.Setup(CharacterKind.Warrior,false,"Assets/Data/Combat/Monsters/Data_Monster_B1_Minion_Skeleton.asset");
        combat=Object.FindFirstObjectByType<CombatController>();scene=Object.FindFirstObjectByType<CombatSceneView>();input=Object.FindFirstObjectByType<CombatInputController>();run=Object.FindFirstObjectByType<RunController>();
        input.CancelAll();input.GuardModifierForVerification=false;
    }
    IEnumerator Capture(string name){yield return null;Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Folder+name+".png");yield return null;}
    public void Run()=>StartCoroutine(Matrix());
    public void RunResult()=>StartCoroutine(ResultMatrix());
    public void RunVisuals()=>StartCoroutine(VisualMatrix());
    IEnumerator ResultMatrix()
    {
        log="Result.txt";File.WriteAllText(Folder+log,"Focused death-flow repair verification in existing Editor.\n");Setup();yield return new WaitForSeconds(.65f);
        combat.Player.TakeEnvironmentalDamage(combat.Player.MaxHp*2);
        var flow=Object.FindFirstObjectByType<GameFlowController>();var deadline=Time.unscaledTime+8;
        while(flow.State!=GameFlowState.RunDefeat&&Time.unscaledTime<deadline)yield return null;
        var view=Object.FindFirstObjectByType<ResultView>(FindObjectsInactive.Include);
        yield return new WaitForSeconds(.65f);
        Check(flow.State==GameFlowState.RunDefeat&&!combat.IsActive,"real player death reaches the production result flow and stops combat");
        Check(view.veil!=null&&view.headingGroup!=null&&view.actions!=null,"saved fade groups survive entering Play mode");
        Check(view.IsRevealing&&!view.actions.interactable&&view.headingGroup.alpha>0,"death title fades in before controls become actionable");yield return Capture("DeathReveal");
        yield return new WaitForSeconds(3f);
        Check(!view.IsRevealing&&view.actions.interactable&&Ref<Button>(view,"restart").interactable,"result controls become usable after the atmosphere beat");
        Check(Ref<TextMeshProUGUI>(view,"title").text=="YOU DIED"&&Ref<Button>(view,"restart").GetComponentInChildren<TextMeshProUGUI>().text==GameText.Get("ui.result.return"),"death title and Korean return control use the intended text");
        yield return Capture("DeathResult");
        File.AppendAllText(Folder+log,$"COMPLETE {passed} PASS / {failed} FAIL\n");File.WriteAllText(Folder+"ResultComplete.txt",$"{passed} PASS / {failed} FAIL\n");
    }
    MonsterData Clone(string old)
    {
        var source=DesignerAssetPaths.Load<MonsterData>("Assets/Data/Combat/Monsters/"+old+".asset");var clone=Object.Instantiate(source);clone.maxHp=50000;clone.diesWithLeader=false;clone.normalAttackInterval=999;clone.chargeInterval=999;clone.attacks=source.attacks.Select(a=>Object.Instantiate(a)).ToList();clone.normalAttackSequence=source.normalAttackSequence.Select(a=>clone.attacks[source.attacks.IndexOf(a)]).ToList();foreach(var attack in clone.attacks)attack.chargeTimeLimit=120;return clone;
    }
    IEnumerator VisualMatrix()
    {
        log="Visuals.txt";File.WriteAllText(Folder+log,"Focused changed species sizes, authored giant poses and real AI feedback.\n");Setup();yield return new WaitForSeconds(.65f);
        var species=new[]{"Data_Monster_B1_Minion_Skeleton","Data_Monster_B1_Minion_Bat","TunnelRat","Data_Monster_B1_Minion_Leech","OrbitBat","AlarmHound","MirrorKnight","Data_Monster_B1_Elite_Minotaur","ArmoredExecutioner","BellKeeper","Data_Monster_B2_Minion_FrostBat","IceNeedle","SnowBell","StormOrb","Data_Monster_B2_Elite_FrostOgre","IceMirror","CrystalSpider","Data_Monster_B3_Elite_SoulKnight","BloodExecutioner","AshGeneral","EclipseBeast","LampPriest","TrialGuardian","DeepTrialGuardian","Data_Monster_B2_Boss_FrostQueen","Data_Monster_B3_Boss_SoulEater","StormMatriarch"};
        foreach(var id in species)
        {
            var clone=Clone(id);combat.ResetEncounter();Call(Object.FindFirstObjectByType<GameFlowController>(),"HideTransientViews");var originalPath=UnityEditor.AssetDatabase.GetAssetPath(DesignerAssetPaths.Load<MonsterData>("Assets/Data/Combat/Monsters/"+id+".asset"));var act=originalPath.Contains("_B2_")?2:originalPath.Contains("_B3_")?3:1;combat.PrepareEncounter(run.State,new[]{clone},act);combat.BeginPreparedCombat();yield return null;yield return null;
            while(!combat.IsCombatPresentationReady)yield return null;
            var view=Object.FindFirstObjectByType<MonsterView>();var art=Ref<Image>(view,"body");var corners=new Vector3[4];art.rectTransform.GetWorldCorners(corners);var points=corners.Select(p=>(Vector2)scene.LogicalArea.InverseTransformPoint(p)).ToArray();
            Check(art.enabled&&art.sprite!=null&&art.sprite.rect.width>100&&art.sprite.rect.height>100&&art.sprite.vertices.Length>=4,id+" has a nonempty native painted Sprite");
            Check(points.Min(p=>p.x)>scene.LogicalArea.rect.xMin+30&&points.Max(p=>p.x)<scene.LogicalArea.rect.xMax-30&&points.Max(p=>p.y)<scene.LogicalArea.rect.yMax-60,id+" full painted bounds fit combat viewport");
            Check(Mathf.Abs(view.Root.localScale.x-clone.combatScale)<.001f,id+" uses its authored species scale");
            yield return Capture("Species-"+id);
            if(id is "Data_Monster_B2_Boss_FrostQueen" or "Data_Monster_B3_Boss_SoulEater" or "StormMatriarch")
            {
                var runtime=combat.Enemies[0];runtime.StartCharge(MonsterOrigin());yield return null;yield return Capture("Ready-"+id);
                Check(art.sprite==clone.combatAnimation.ready&&clone.GetAttacks(MonsterAttackType.Charge).SelectMany(a=>a.chargeWeakPoints).All(w=>clone.OverlapsArea(w.localPosition,0,Vector2.zero)),id+" authored Ready and anatomical weak points match shared body hitboxes");
                var timer=Ref<TextMeshProUGUI>(view,"chargeTimeLabel");var timerScreen=RectTransformUtility.WorldToScreenPoint(scene.UiCamera,timer.transform.position);Check(timer.enabled&&timerScreen.y>140&&timerScreen.y<Screen.height-130,id+" attack countdown stays readable within the combat viewport");
                view.PlayAttack();yield return null;yield return Capture("Attack-"+id);runtime.EnterStun();yield return null;
                Check(art.sprite!=null,id+" stun fallback remains a valid painted image");
            }
        }
        foreach(var id in new[]{"Data_Monster_B1_Minion_Bat","TunnelRat","Data_Monster_B1_Minion_Leech","OrbitBat"})
        {
            var clone=Clone(id);var scale=clone.combatScale;combat.ResetEncounter();combat.PrepareEncounter(run.State,new[]{clone,clone},1);combat.BeginPreparedCombat();yield return null;yield return null;
            Check(combat.Enemies.All(e=>Mathf.Abs(e.VisualScale-scale)<.001f),id+" retains identical scale when grouped");
        }
        // Exercise the production saved roc/jelly formation with runtime clones, preserving all saved data.
        var encounter=combat.Content.stages.First(s=>s.act==2).bosses.First(e=>e.members.Any(m=>m.monster.displayName=="폭풍의 대붕"));var group=Object.Instantiate(encounter);group.members=encounter.members.Select(m=>new EncounterMember{monster=Object.Instantiate(m.monster),offset=m.offset,actionDelay=m.actionDelay}).ToList();
        foreach(var member in group.members){member.monster.maxHp=50000;member.monster.normalAttackInterval=member.monster.chargeInterval=999;}
        combat.ResetEncounter();combat.PrepareFixedEncounter(run.State,group,2);combat.BeginPreparedCombat();yield return null;while(!combat.IsCombatPresentationReady)yield return null;yield return Capture("RocWithSupport");Check(combat.Enemies.Count==encounter.members.Count,"wide roc preserves both separate small supports in the authored encounter");
        var enemy=Clone("Data_Monster_B1_Minion_Skeleton");enemy.normalAttackInterval=.35f;enemy.chargeInterval=999;foreach(var attack in enemy.attacks){attack.windupDuration=.35f;attack.recoveryDuration=.25f;}
        combat.ResetEncounter();combat.PrepareEncounter(run.State,new[]{enemy},1);combat.BeginPreparedCombat();yield return null;
        var gs=ScreenAt(new Vector2(0,-320));var ge=ScreenAt(new Vector2(0,-470));Contact("Begin",900,gs);Contact("Move",900,ge);var shield=combat.Player.ShieldCurrent;var deadline=Time.unscaledTime+6;
        while(!combat.pressureFeedback.impact.IsPlaying&&Time.unscaledTime<deadline)yield return null;
        Check(combat.Player.ShieldCurrent<shield&&combat.pressureFeedback.impact.IsPlaying,"real normal-attack AI produces guard impact feedback at resolution");yield return Capture("ActualMonsterGuardImpact");Contact("End",900,ge,false);
        yield return new WaitForSeconds(.5f);var hp=combat.Player.CurrentHp;deadline=Time.unscaledTime+6;
        while(combat.Player.CurrentHp>=hp&&Time.unscaledTime<deadline)yield return null;
        yield return null;Check(combat.Player.CurrentHp<hp&&combat.pressureFeedback.danger.intensity>.3f,"real unguarded monster attack drives the HP-loss screen warning");yield return Capture("ActualMonsterHit");
        var summary=Object.FindFirstObjectByType<QuickInventoryView>().characterSummary;Check(Ref<TextMeshProUGUI>(summary,"healthText").text==GameText.Format("ui.combathudview.dfe9f229",combat.Player.CurrentHp,combat.Player.MaxHp),"top health summary stays current while inventory drawer is closed");
        enemy=Clone("Data_Monster_B1_Minion_Skeleton");foreach(var attack in enemy.GetAttacks(MonsterAttackType.Charge))attack.chargeTimeLimit=.35f;
        combat.ResetEncounter();combat.PrepareEncounter(run.State,new[]{enemy},1);combat.BeginPreparedCombat();yield return new WaitForSeconds(.7f);combat.Enemies[0].StartCharge(MonsterOrigin());deadline=Time.unscaledTime+4;
        while(!combat.pressureFeedback.impact.IsPlaying&&Time.unscaledTime<deadline)yield return null;
        Check(combat.pressureFeedback.impact.IsPlaying&&Ref<bool>(combat.pressureFeedback.impact,"charged"),"real failed weak-point window produces the heavier charge impact");yield return Capture("ActualMonsterChargeImpact");
        combat.InputPaused=true;input.CancelAll();
        File.AppendAllText(Folder+log,$"COMPLETE {passed} PASS / {failed} FAIL\n");File.WriteAllText(Folder+"VisualsComplete.txt",$"{passed} PASS / {failed} FAIL\n");
    }
    public void Show(string id){Setup();StartCoroutine(ShowRoutine(id));}
    IEnumerator ShowRoutine(string id)
    {
        yield return new WaitForSeconds(.6f);
        if(id=="core"){foreach(var a in combat.Enemies[0].Data.attacks)a.chargeTimeLimit=120;combat.Enemies[0].StartCharge(MonsterOrigin());yield return Capture("WeakPointTargets-Final");yield break;}
        var row=JsonUtility.FromJson<DesignerAssetPaths.Index>(File.ReadAllText(DesignerAssetPaths.IndexPath)).entries.First(e=>e.type=="MonsterData"&&(e.displayName==id||Path.GetFileNameWithoutExtension(e.oldPath)==id));
        var source=UnityEditor.AssetDatabase.LoadAssetAtPath<MonsterData>(row.path);var clone=Object.Instantiate(source);clone.maxHp=50000;clone.normalAttackInterval=999;clone.chargeInterval=999;
        clone.attacks=source.attacks.Select(a=>Object.Instantiate(a)).ToList();foreach(var a in clone.attacks)a.chargeTimeLimit=120;
        combat.PrepareEncounter(run.State,new[]{clone},1);combat.BeginPreparedCombat();combat.Enemies[0].StartCharge(scene.MonsterPosition);
    }
    IEnumerator Matrix()
    {
        Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"Live.txt","Focused existing Unity Editor runtime; production input handlers, no DPS/balance suite.\n");
        Setup();yield return new WaitForSeconds(.65f);
        var offense=ScreenAt(new Vector2(-320,10));var gs=ScreenAt(new Vector2(0,-320));var ge=ScreenAt(new Vector2(0,-470));
        Contact("Begin",700,offense);
        for(var t=0f;t<combat.Player.CurrentChargeDuration+.06f;t+=Time.deltaTime){Contact("Move",700,offense);yield return null;}
        Contact("RefreshState");Check(input.ChargeProgress>=.99f&&input.IsPreparingAttack,"held offense reaches charged Ready before guard");
        Contact("Begin",701,gs);Contact("Move",701,ge);Contact("RefreshState");yield return null;
        Check(combat.Player.IsGuarding&&!input.IsPreparingAttack&&!input.IsChargingAction&&input.ChargeProgress==0,"downward guard immediately cancels charge and Ready");
        var enemyHp=combat.Enemies[0].CurrentHp;
        Contact("Move",700,ScreenAt(scene.MonsterPosition+Vector2.right*300));Contact("End",700,offense,false);
        Check(Mathf.Approximately(enemyHp,combat.Enemies[0].CurrentHp),"canceled charge cannot commit a hit on move/release");
        Contact("End",701,ge,false);yield return null;
        var cycles=true;
        Contact("Begin",710,offense);
        for(var cycle=0;cycle<3;cycle++)
        {
            Contact("Move",710,offense);Contact("Begin",711,gs);Contact("Move",711,ge);yield return null;
            cycles&=combat.Player.IsGuarding&&!input.IsChargingAction;
            Contact("End",711,ge,false);Contact("Move",710,offense);Contact("RefreshState");yield return null;
            cycles&=!combat.Player.IsGuarding&&!input.IsPreparingAttack&&input.ChargeProgress==0;
            Contact("Begin",712,gs);Contact("Move",712,ge);
            Contact("End",710,offense,true);Contact("Begin",710,offense);yield return null;
            cycles&=combat.Player.IsGuarding&&!input.IsChargingAction;
            Contact("End",712,ge,false);
        }
        Check(cycles,"three sustained overlap/lift/repress cycles never resume canceled offense or lose defense contact");input.CancelAll();
        Contact("Begin",720,offense);Contact("Move",720,offense);input.GuardModifierForVerification=true;yield return null;
        Check(combat.Player.IsGuarding&&!input.IsPreparingAttack&&!combat.Player.CanNormalAttack(),"Space guard wins before offensive processing and blocks new attacks");
        input.GuardModifierForVerification=false;Contact("End",720,offense,true);yield return null;
        // A three-hit action exposes pending hits, rather than trying to roll back resolved damage.
        var extra=ScriptableObject.CreateInstance<EquipmentData>();extra.id="qa-multi";extra.abilities.Add(new EquipmentAbility{trigger=new EquipmentTrigger{triggerType=TriggerType.OnActionPreparing,chancePercent=100,triggerCount=1},target=TargetType.Owner,effects=new(){new EquipmentEffect{effectType=EffectType.ActionExtraHits,effectMagnitude=2,effectCount=1}}});run.State.TryEquip(extra);
        combat.Player.ResetAttackCooldown();var center=scene.MonsterPosition;
        combat.ExecuteAttack(new AttackSegment(center+Vector2.left*400,center+Vector2.right*400,60),false);var afterFirst=combat.Enemies[0].CurrentHp;
        Contact("Begin",730,gs);Contact("Move",730,ge);yield return new WaitForSeconds(.4f);
        Check(Mathf.Approximately(afterFirst,combat.Enemies[0].CurrentHp)&&combat.Player.IsGuarding,"guard interrupts the remaining hits of a committed three-hit action");
        var player=Object.FindFirstObjectByType<PlayerView>();var animator=Ref<Image>(player,"body").GetComponent<CombatSpriteAnimator>();Check(animator.State==CombatVisualState.Guard,"attack painting immediately yields to authored Guard painting");
        Contact("End",730,ge,false);run.State.TryRemoveEquipment(extra);Object.Destroy(extra);input.CancelAll();
        var baseDamage=combat.Player.NormalAttackDamage;run.TryGrantRelic(Item("AzureTalon"));Check(combat.Player.NormalAttackDamage>baseDamage&&combat.Player.AttackReach>=100,"Azure Talon grants immediate attack damage as well as reach");run.TryRemoveEquipment(Item("AzureTalon"));
        var baseRecovery=combat.Player.CurrentCooldown;run.TryGrantRelic(Item("DistanceMomentum"));Check(combat.Player.CurrentCooldown<baseRecovery&&combat.Player.DamagePerAttackDistance>0,"Distance Momentum grants immediate recovery reduction");run.TryRemoveEquipment(Item("DistanceMomentum"));
        run.TryGrantRelic(Item("RaggedBanner"));Check(combat.Player.Rule(AbilityRule.DamageMultiplier,new EquipmentTriggerContext(default,TriggerAttackType.None,TriggerAttackWay.None,false,false,enemyCount:1))>=.159f,"Ragged Banner has a meaningful solo damage floor");run.TryRemoveEquipment(Item("RaggedBanner"));
        run.TryGrantRelic(Item("BloodContract"));combat.Player.TakeEnvironmentalDamage(50);var before=combat.Player.CurrentHp;combat.Player.Heal(20);Check(Mathf.Abs(combat.Player.CurrentHp-before-12)<.02f,"Blood Contract reduces a20HP heal to12HP through the shared healing rule");run.TryRemoveEquipment(Item("BloodContract"));
        foreach(var item in run.State.Potions.ToArray())run.TryRemoveEquipment(item);
        var ember=Item("RevivalPotion");run.State.TryStorePotion(ember);var menu=Object.FindFirstObjectByType<PotionActionView>(FindObjectsInactive.Include);var quick=Object.FindFirstObjectByType<QuickInventoryView>();quick.Refresh();yield return null;
        menu.Open(ember,(RectTransform)quick.potions[0].transform);yield return null;
        Check(menu.IsOpen&&!menu.drink.gameObject.activeSelf&&!menu.throwButton.gameObject.activeSelf&&menu.discard.gameObject.activeInHierarchy,"carried Ember menu offers discard without drink/throw buttons");
        Check(!run.TryUsePotion(ember)&&!run.State.CanUsePotion(ember,true)&&run.State.Potions.Contains(ember),"Ember cannot be consumed/thrown early and remains carried");menu.Close();
        combat.Player.TakeEnvironmentalDamage(combat.Player.MaxHp*2);Check(run.State.TryConsumeRevival()&&Mathf.Abs(combat.Player.CurrentHp/combat.Player.MaxHp-.35f)<.001f&&!run.State.Potions.Contains(ember),"carried Ember auto-revives at35percent and consumes exactly one bottle");
        Check(!run.State.TryConsumeRevival(),"living player cannot consume another revival");
        var fatigue=new FatigueRuntime();var graph=Object.FindFirstObjectByType<DungeonController>().State.Graph;fatigue.BeginFloor(combat.Content.fatigue,graph,1);fatigue.Step();
        Check(fatigue.Value==100-combat.Content.fatigue.costPerStep&&fatigue.GraceRemaining==0,"first floor step immediately reduces fatigue from100");fatigue.Camp();fatigue.Step();Check(fatigue.Value<100&&fatigue.GraceRemaining==0,"camp restores100 without hidden grace");fatigue.Recover(30,10);fatigue.Step();Check(fatigue.Value==100-combat.Content.fatigue.costPerStep,"recovering fatigue never creates extra safe steps");
        while(!fatigue.IsDead)fatigue.Step();Check(fatigue.Value==0&&fatigue.IsDead,"fatigue reaches exactly0 and exhaustion");
        var packs=false;var safe=true;
        for(var seed=0;seed<20;seed++)
        {
            var planner=new EncounterPlanner(combat.Content,seed);
            for(var room=0;room<12;room++)
            {var low=planner.Get(1,RoomEncounterType.Combat,room,10,2);safe&=low!=null&&low.members.Count==1;var early=planner.Get(1,RoomEncounterType.Combat,100+room,5,8);safe&=early!=null&&early.members.Count==1;var grown=planner.Get(1,RoomEncounterType.Combat,200+room,6,4);packs|=grown!=null&&grown.members.Count>1;}
        }
        Check(safe&&packs,"normal packs require both six victories and level4; eligible grown encounters still include packs");
        var hud=Object.FindFirstObjectByType<CombatHudView>();var anchor=hud.attackRecovery.GetComponent<AttackCooldownAnchor>();yield return null;
        Check(anchor.placement==AttackCooldownPlacement.AbovePlayer&&hud.attackRecovery.rectTransform.anchoredPosition.y>scene.PlayerPosition.y+200,"cooldown stays above player head between combatants");
        Check(hud.guardGauge.shieldSilhouette&&hud.guardGauge.rectTransform.rect.height>=50,"guard uses a thick bright shield silhouette");
        combat.Player.StartAttackCooldown();Contact("Begin",740,gs);Contact("Move",740,ge);yield return null;yield return Capture("GuardAndCooldown");
        var source=combat.Enemies[0];var incoming=source.Data.GetAttack(MonsterAttackType.Normal);var result=new DamageResolver().ApplyToPlayer(combat.Player,new DamageRequest(DamageType.MonsterAttack,10,20,true,source));combat.pressureFeedback.MonsterAttack(scene.MonsterPosition,false,result);yield return Capture("GuardImpact");
        Check(combat.pressureFeedback.impact.IsPlaying&&result.WasGuarded,"guarded monster strike shows a distinct cyan impact");Contact("End",740,ge,false);yield return new WaitForSeconds(.6f);
        combat.Player.TakeEnvironmentalDamage(10);yield return null;Check(combat.pressureFeedback.danger.intensity>.4f,"recent HP damage produces a red peripheral wash");yield return Capture("RecentDamage");
        combat.Player.TakeEnvironmentalDamage(Mathf.Max(0,combat.Player.CurrentHp-combat.Player.MaxHp*.2f));yield return new WaitForSeconds(1.1f);Check(combat.pressureFeedback.danger.intensity>.1f,"low HP warning persists after recent-hit flash fades");yield return Capture("LowHealth");
        combat.Player.Heal(combat.Player.MaxHp);yield return new WaitForSeconds(.1f);Check(combat.pressureFeedback.danger.intensity==0,"healing above danger threshold clears the low-health wash");
        practice.Charge();yield return null;yield return Capture("WeakPointTargets");Check(Object.FindObjectsByType<WeakPointReticle>(FindObjectsSortMode.None).Any(r=>r.isActiveAndEnabled),"production weak points show bright circular crosshair targets");
        var resultView=Object.FindFirstObjectByType<ResultView>(FindObjectsInactive.Include);resultView.Show(GameText.Get("ui.result.defeat"),GameText.Get("ui.gameflowcontroller.c34162cb"),()=>{});yield return new WaitForSeconds(.65f);
        Check(resultView.IsRevealing&&!resultView.actions.interactable&&resultView.headingGroup.alpha>0,"death title fades in before the result controls become actionable");yield return Capture("DeathReveal");yield return new WaitForSeconds(3f);
        Check(!resultView.IsRevealing&&resultView.actions.interactable&&Ref<Button>(resultView,"restart").interactable,"death result controls become usable after the atmosphere beat");yield return Capture("DeathResult");resultView.Hide();
        input.CancelAll();input.GuardModifierForVerification=null;
        File.AppendAllText(Folder+"Live.txt",$"COMPLETE {passed} PASS / {failed} FAIL\n");File.WriteAllText(Folder+"LiveComplete.txt",$"{passed} PASS / {failed} FAIL\n");
    }
}
#endif
