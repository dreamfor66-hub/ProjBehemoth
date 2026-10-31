#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonSlash;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class CombatInkRevisionLiveProbe:MonoBehaviour
{
    const string Folder="Docs/Production/CombatInkRevision-2026-10-30/";
    const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    CombatController combat;CombatSceneView scene;RunController run;CombatInputController input;OctoberReviewLiveProbe practice;
    int passed,failed;bool background;string log="Live.txt";
    void Awake(){background=Application.runInBackground;Application.runInBackground=true;DontDestroyOnLoad(gameObject);}
    void OnDestroy(){Application.runInBackground=background;if(input!=null)input.GuardModifierForVerification=null;}
    T Ref<T>(object target,string field)=>(T)target.GetType().GetField(field,Fields).GetValue(target);
    void Check(bool ok,string name){if(ok)passed++;else failed++;File.AppendAllText(Folder+log,(ok?"PASS ":"FAIL ")+name+"\n");}
    void Setup(CharacterKind kind,bool spells=false,string name="Data_Monster_B1_Minion_Bat")
    {
        practice??=new GameObject("Combat ink practice").AddComponent<OctoberReviewLiveProbe>();practice.Setup(kind,spells,"Assets/Data/Combat/Monsters/"+name+".asset");
        combat=Object.FindFirstObjectByType<CombatController>();scene=Object.FindFirstObjectByType<CombatSceneView>();run=Object.FindFirstObjectByType<RunController>();input=Object.FindFirstObjectByType<CombatInputController>();input.CancelAll();input.GuardModifierForVerification=false;
    }
    IEnumerator Capture(string name)
    {yield return null;Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Folder+name+".png");yield return null;}
    Rect RectInScene(RectTransform rect)
    {var c=new Vector3[4];rect.GetWorldCorners(c);var p=c.Select(v=>(Vector2)scene.LogicalArea.InverseTransformPoint(v)).ToArray();return Rect.MinMaxRect(p.Min(v=>v.x),p.Min(v=>v.y),p.Max(v=>v.x),p.Max(v=>v.y));}
    Vector2 Origin(MonsterView view)=>(Vector2)scene.LogicalArea.InverseTransformPoint(view.Root.position);
    MonsterData Clone(string name)
    {
        var source=DesignerAssetPaths.Load<MonsterData>("Assets/Data/Combat/Monsters/"+name+".asset");var clone=Object.Instantiate(source);clone.maxHp=50000;clone.diesWithLeader=false;clone.normalAttackInterval=clone.chargeInterval=999;
        clone.attacks=source.attacks.Select(a=>Object.Instantiate(a)).ToList();clone.normalAttackSequence=source.normalAttackSequence.Select(a=>clone.attacks[source.attacks.IndexOf(a)]).ToList();return clone;
    }
    public void Run()=>StartCoroutine(Matrix());
    public void RunResults()=>StartCoroutine(Results());
    IEnumerator Results()
    {
        log="Results.txt";File.WriteAllText(Folder+log,"Focused victory-spacing preservation and final death-to-title flow.\n");
        Setup(CharacterKind.Warrior);yield return new WaitForSeconds(.7f);
        var view=Object.FindFirstObjectByType<ResultView>(FindObjectsInactive.Include);var button=Ref<Button>(view,"restart");var details=Ref<TextMeshProUGUI>(view,"details");
        view.Show(GameText.Get("ui.result.victory"),GameText.Get("ui.gameflowcontroller.543f0daf"),()=>{},true);yield return new WaitForSeconds(.7f);
        Check(details.gameObject.activeInHierarchy,"victory retains its existing explanation");
        Check(!RectInScene(details.rectTransform).Overlaps(RectInScene((RectTransform)button.transform)),"victory explanation and return button retain separate space");view.Hide();
        combat.Player.TakeEnvironmentalDamage(combat.Player.MaxHp*2);var flow=Object.FindFirstObjectByType<GameFlowController>();var deadline=Time.unscaledTime+8;
        while(flow.State!=GameFlowState.RunDefeat&&Time.unscaledTime<deadline)yield return null;yield return new WaitForSeconds(.6f);
        Check(view.IsRevealing&&!view.actions.interactable,"real death preserves the delayed action reveal");yield return Capture("Death-Reveal");yield return new WaitForSeconds(3f);
        Check(!details.gameObject.activeInHierarchy&&button.interactable&&button.GetComponentInChildren<TextMeshProUGUI>().text=="타이틀로"&&view.GetComponentsInChildren<Button>().Count(b=>b.gameObject.activeInHierarchy)==1,"final death view contains one Title action and no paragraph");yield return Capture("Death-TitleOnly");
        button.onClick.Invoke();deadline=Time.unscaledTime+10;while(SceneManager.GetActiveScene().name!="OutGameScene"&&Time.unscaledTime<deadline)yield return null;yield return null;
        var menu=Object.FindFirstObjectByType<OutGameMenuView>();Check(menu!=null&&menu.Page==OutGamePage.Title&&menu.titlePage.activeInHierarchy&&!menu.characterPage.activeInHierarchy,"final production button reaches the actual Title page");yield return Capture("Returned-ToTitle");
        File.AppendAllText(Folder+log,$"COMPLETE {passed} PASS / {failed} FAIL\n");File.WriteAllText(Folder+"ResultsComplete.txt",$"{passed} PASS / {failed} FAIL\n");
    }
    IEnumerator Matrix()
    {
        Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"Live.txt","Scoped ink presentation verification in existing Unity; runtime-only clones.\n");
        Setup(CharacterKind.Warrior);yield return new WaitForSeconds(.7f);
        var hud=Object.FindFirstObjectByType<CombatHudView>();var hp=(RectTransform)Ref<Image>(hud,"playerHp").transform.parent;var guard=hud.guardGauge.rectTransform;var recovery=hud.attackRecovery;
        var hpRect=RectInScene(hp);var guardRect=RectInScene(guard);var attackRect=RectInScene(recovery.rectTransform);
        Check(!guardRect.Overlaps(attackRect)&&guardRect.yMin>hpRect.yMax&&attackRect.yMin>hpRect.yMax,"guard and recovery share a separated row above unchanged HP/MP");
        Check(guardRect.xMin>=hpRect.xMin-5&&attackRect.xMax<=hpRect.xMax+5,"action row fits the HP column and leaves player artwork clear");
        Check(recovery.GetComponent<AttackCooldownAnchor>().placement==AttackCooldownPlacement.Authored,"cooldown stays in the authored action row rather than floating over the head");
        var readyMesh=hud.guardGauge.canvasRenderer.GetMesh();Check(readyMesh!=null&&readyMesh.vertexCount>0&&!readyMesh.colors.Any(c=>c.b>.8f&&c.g>.8f&&c.r<.7f),"guard uses visible warm iron/bone mesh without cyan rails");
        yield return Capture("HUD-Ready");
        combat.Player.StartAttackCooldown();yield return new WaitForSeconds(.2f);Check(recovery.IsRecovering&&recovery.Progress>0&&recovery.Progress<1,"blade fill visibly progresses during actual attack recovery");yield return Capture("HUD-Recovering");
        yield return new WaitForSeconds(combat.Player.CurrentCooldown+.1f);Check(!recovery.IsRecovering&&recovery.Progress==1,"blade returns to ready after cooldown");
        input.GuardModifierForVerification=true;yield return null;Check(hud.guardGauge.Guarding,"guard-held state reaches warm shield treatment");yield return Capture("HUD-GuardHeld");
        var damage=new DamageResolver().ApplyToPlayer(combat.Player,new DamageRequest(DamageType.MonsterAttack,5,15,true,combat.Enemies[0]));combat.pressureFeedback.MonsterAttack(scene.MonsterPosition,false,damage);yield return null;
        Check(damage.WasGuarded&&hud.guardGauge.LossTrail>hud.guardGauge.amount,"guard damage retains the readable rust loss trail");yield return Capture("VFX-Blocked");
        damage=new DamageResolver().ApplyToPlayer(combat.Player,new DamageRequest(DamageType.MonsterAttack,1,1000,true,combat.Enemies[0]));yield return null;Check(hud.guardGauge.Broken,"broken guard has its own dark red state");yield return Capture("HUD-GuardBroken");
        Setup(CharacterKind.Wizard,true);yield return new WaitForSeconds(.7f);yield return Capture("HUD-WizardSpells");
        Check(Object.FindFirstObjectByType<RunExpansionView>().spellLabels.All(t=>!t.isTextOverflowing),"four spell controls remain readable beside the consolidated action HUD");
        // Short regular enemy, human-sized caster, multi-target encounter and a wide boss.
        foreach(var id in new[]{"Data_Monster_B1_Minion_Bat","Data_Monster_B1_Minion_Skeleton","LampPriest","Data_Monster_B3_Boss_SoulEater"})
        {
            var clone=Clone(id);combat.ResetEncounter();combat.PrepareEncounter(run.State,new[]{clone},id.Contains("B3")?3:1);combat.BeginPreparedCombat();yield return new WaitForSeconds(.6f);
            var view=Object.FindFirstObjectByType<MonsterView>();combat.Enemies[0].StartCharge(Origin(view));yield return null;
            yield return null;Canvas.ForceUpdateCanvases();var label=Ref<TextMeshProUGUI>(view,"attackNameLabel");var plate=Ref<Image>(view,"attackNameBackdrop");var timer=Ref<TextMeshProUGUI>(view,"chargeTimeLabel");label.ForceMeshUpdate();timer.ForceMeshUpdate();var original=id.Contains("SoulEater")?370:560;
            Check(plate.rectTransform.rect.width<original&&plate.rectTransform.rect.width>=original*.75f,id+" name plaque shrinks conservatively while keeping brush padding");
            Check(!label.isTextOverflowing&&label.textInfo.lineCount==1&&!timer.isTextOverflowing,id+" name and countdown remain unclipped on one line");
            var center=RectTransformUtility.WorldToScreenPoint(scene.UiCamera,timer.transform.position);Check(center.y>130&&center.y<Screen.height-130,id+" countdown stays inside the usable combat view");
            File.AppendAllText(Folder+"Widths.txt",$"{clone.displayName}: {label.text}, name={plate.rectTransform.rect.width:0.0}, preferred={label.GetPreferredValues(label.text).x:0.0}; timer={Ref<Image>(view,"chargeTimeBackdrop").rectTransform.rect.width:0.0}\n");
            yield return Capture("Caption-"+id);
            if(id=="Data_Monster_B1_Minion_Bat")
            {
                var point=combat.Enemies[0].WeakPoints.First(w=>!w.IsOptional);var before=point.RemainingHits;combat.Player.ResetAttackCooldown();combat.ExecuteAttack(new AttackSegment(point.HitShape.Center+Vector2.left*85,point.HitShape.Center+Vector2.right*85,45),true);yield return null;
                Check(point.RemainingHits<before,"painted target still resolves the real charged weak-point hit");yield return Capture("WeakPoint-Break");
            }
            if(id=="LampPriest")
            {combat.Enemies[0].EndCharge();view.ShowAttackName("검 베기");yield return null;var shortWidth=plate.rectTransform.rect.width;view.ShowAttackName("연속 불꽃 휘두르기");yield return null;Check(!label.isTextOverflowing&&plate.rectTransform.rect.width>=shortWidth,"longer captions derive enough width instead of clipping or squeezing text");yield return Capture("Caption-Long");}
        }
        var group=combat.Content.stages.First(s=>s.act==2).bosses.First(e=>e.members.Any(m=>m.monster.displayName=="폭풍의 대붕"));var cloneGroup=Object.Instantiate(group);cloneGroup.members=group.members.Select(m=>new EncounterMember{monster=Object.Instantiate(m.monster),offset=m.offset,actionDelay=m.actionDelay}).ToList();foreach(var m in cloneGroup.members)m.monster.normalAttackInterval=m.monster.chargeInterval=999;
        combat.ResetEncounter();combat.PrepareFixedEncounter(run.State,cloneGroup,2);combat.BeginPreparedCombat();yield return new WaitForSeconds(.6f);yield return Capture("HUD-RocGroup");Check(combat.Enemies.Count==group.members.Count,"group composition and species sizes remain unchanged");
        // Real AI resolution, with only clone timing shortened to reach the presentation promptly.
        var enemy=Clone("Data_Monster_B1_Minion_Skeleton");enemy.normalAttackInterval=.35f;foreach(var a in enemy.attacks){a.windupDuration=.5f;a.recoveryDuration=.3f;}
        combat.ResetEncounter();combat.PrepareEncounter(run.State,new[]{enemy},1);combat.BeginPreparedCombat();var deadline=Time.unscaledTime+6;
        while(!combat.pressureFeedback.impact.IsPlaying&&Time.unscaledTime<deadline)yield return null;
        Check(combat.pressureFeedback.impact.IsPlaying,"real normal-attack AI drives the broad ink impact");yield return Capture("VFX-ActualHit");
        enemy=Clone("Data_Monster_B1_Minion_Skeleton");foreach(var a in enemy.GetAttacks(MonsterAttackType.Charge))a.chargeTimeLimit=.5f;
        combat.ResetEncounter();combat.PrepareEncounter(run.State,new[]{enemy},1);combat.BeginPreparedCombat();yield return new WaitForSeconds(.6f);combat.Enemies[0].StartCharge(Origin(Object.FindFirstObjectByType<MonsterView>()));deadline=Time.unscaledTime+4;
        while(!combat.pressureFeedback.impact.IsPlaying&&Time.unscaledTime<deadline)yield return null;
        Check(combat.pressureFeedback.impact.IsPlaying&&Ref<bool>(combat.pressureFeedback.impact,"charged"),"failed real weak-point window produces the heavier ink strike");yield return Capture("VFX-ActualHeavy");
        combat.Player.TakeEnvironmentalDamage(combat.Player.MaxHp*2);var flow=Object.FindFirstObjectByType<GameFlowController>();deadline=Time.unscaledTime+8;while(flow.State!=GameFlowState.RunDefeat&&Time.unscaledTime<deadline)yield return null;
        var result=Object.FindFirstObjectByType<ResultView>(FindObjectsInactive.Include);yield return new WaitForSeconds(.6f);
        Check(result.IsRevealing&&!result.actions.interactable,"YOU DIED keeps its quiet reveal before the single action");yield return Capture("Death-Reveal");yield return new WaitForSeconds(3f);
        var button=Ref<Button>(result,"restart");Check(Ref<TextMeshProUGUI>(result,"title").text=="YOU DIED"&&!Ref<TextMeshProUGUI>(result,"details").gameObject.activeInHierarchy,"death result has no explanation paragraph");
        Check(result.GetComponentsInChildren<Button>().Count(b=>b.gameObject.activeInHierarchy)==1&&button.GetComponentInChildren<TextMeshProUGUI>().text=="타이틀로"&&button.interactable,"death result exposes exactly one usable Title button");yield return Capture("Death-TitleOnly");
        button.onClick.Invoke();deadline=Time.unscaledTime+10;while(SceneManager.GetActiveScene().name!="OutGameScene"&&Time.unscaledTime<deadline)yield return null;yield return null;
        var menu=Object.FindFirstObjectByType<OutGameMenuView>();Check(menu!=null&&menu.Page==OutGamePage.Title&&menu.titlePage.activeInHierarchy&&!menu.characterPage.activeInHierarchy,"production result button returns to the actual Title page");yield return Capture("Returned-ToTitle");
        File.AppendAllText(Folder+"Live.txt",$"COMPLETE {passed} PASS / {failed} FAIL\n");File.WriteAllText(Folder+"Complete.txt",$"{passed} PASS / {failed} FAIL\n");
    }
}
#endif
