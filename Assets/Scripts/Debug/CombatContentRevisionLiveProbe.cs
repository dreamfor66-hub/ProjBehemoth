#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonSlash;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Scoped runtime verification in the existing Editor. Never writes practice tuning into production assets.
public sealed class CombatContentRevisionLiveProbe:MonoBehaviour
{
    const string Folder="Docs/Production/CombatAndContentRevision-2026-10-31/";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    CombatController combat;CombatSceneView scene;RunController run;GameFlowController flow;RunExpansionView expansion;
    int passed,failed;bool background;
    string log="Live.txt",completion="LiveComplete.txt";
    void Awake(){background=Application.runInBackground;Application.runInBackground=true;}
    void OnDestroy()=>Application.runInBackground=background;
    T Ref<T>(object owner,string name)=>(T)owner.GetType().GetField(name,Flags).GetValue(owner);
    object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethod(name,Flags).Invoke(owner,args);
    void Check(bool ok,string text){if(ok)passed++;else failed++;File.AppendAllText(Folder+log,(ok?"PASS ":"FAIL ")+text+"\n");}
    MonsterData Source(string old)=>DesignerAssetPaths.Load<MonsterData>("Assets/Data/Combat/Monsters/"+old+".asset");
    MonsterData Clone(string old)
    {
        var source=Source(old);var data=Object.Instantiate(source);data.maxHp=50000;data.normalAttackInterval=data.chargeInterval=999;
        data.attacks=source.attacks.Select(a=>Object.Instantiate(a)).ToList();data.normalAttackSequence=source.normalAttackSequence.Select(a=>data.attacks[source.attacks.IndexOf(a)]).ToList();
        foreach(var attack in data.attacks){attack.damage=attack.shieldDamage=0;}
        var supportClones=new Dictionary<MonsterData,MonsterData>();
        data.supportRoster=source.supportRoster.Select(s=>{
            if(!supportClones.TryGetValue(s,out var clone)){clone=Object.Instantiate(s);clone.normalAttackInterval=clone.chargeInterval=999;supportClones[s]=clone;}
            return clone;
        }).ToList();
        return data;
    }
    void Bind()
    {
        combat=Object.FindFirstObjectByType<CombatController>();scene=Object.FindFirstObjectByType<CombatSceneView>();run=Object.FindFirstObjectByType<RunController>();flow=Object.FindFirstObjectByType<GameFlowController>();expansion=Object.FindFirstObjectByType<RunExpansionView>();
    }
    void Fresh()
    {
        flow.StartNewRun();expansion.HideModal();Object.FindFirstObjectByType<DungeonController>().HideNavigation();
    }
    void Click(Button button)
    {
        Check(button!=null&&button.gameObject.activeInHierarchy&&button.interactable,"visible selectable UI button: "+button?.name);
        if(button==null)return;
        var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
        ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
    }
    IEnumerator Capture(string name)
    {
        yield return null;Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();
        var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+name+".png",texture.EncodeToPNG());Object.Destroy(texture);
    }
    public void Run()=>StartCoroutine(Matrix());
    public void RunVisuals(){log="VisualReview.txt";completion="VisualReviewComplete.txt";StartCoroutine(Matrix(false));}
    IEnumerator Matrix(bool cadence=true)
    {
        File.WriteAllText(Folder+log,"Scoped production scene verification. Existing Unity instance; runtime clones, real frames, UI EventSystem clicks. No DPS/regression suite.\n");
        Bind();Fresh();yield return new WaitForSeconds(.8f);combat.ResetEncounter();
        var fatigue=run.State.Fatigue;var settings=combat.Content.fatigue;
        fatigue.BeginFloor(settings,null);var initialCooldown=run.State.Player.CurrentCooldown;
        fatigue.Step();Check(fatigue.Value==98&&fatigue.IsAtEase&&fatigue.Expression==0,"first step immediately consumes2; at98 no penalty and ease portrait");
        while(fatigue.Value>70)fatigue.Step();
        Check(fatigue.Steps==15&&fatigue.AttackSpeedMultiplier==1&&fatigue.FinalDamageMultiplier==1&&fatigue.Expression==0,"15 steps reach70 with no penalty; shared curves determine ease stage");
        fatigue.Step();Check(fatigue.Value==68&&fatigue.AttackSpeedMultiplier<1&&fatigue.FinalDamageMultiplier==1&&fatigue.Expression==1&&run.State.Player.CurrentCooldown>initialCooldown,"16th step at68 affects actual weapon cooldown; damage remains intact");
        while(fatigue.Value>46)fatigue.Step();Check(fatigue.FinalDamageMultiplier==1,"at46 final damage remains100%");
        fatigue.Step();Check(fatigue.Value==44&&fatigue.FinalDamageMultiplier<1&&fatigue.Expression==2,"28th step at44 starts final damage penalty and exhausted stage");
        var damageData=Object.Instantiate(Source("Data_Monster_B1_Minion_Skeleton"));damageData.maxHp=1000;var damageTarget=new MonsterRuntime(damageData);
        var health=damageTarget.CurrentHp;var damageResult=new DamageResolver().ApplyToMonster(damageTarget,20,run.State.Player,new System.Random(7));
        Check(Mathf.Abs(health-damageTarget.CurrentHp-20*fatigue.FinalDamageMultiplier*(damageResult.IsCritical?run.State.Player.CriticalDamageMultiplier:1))<.001f,"at44 actual enemy HP damage uses the fatigue multiplier");
        fatigue.Recover(1);Check(fatigue.Value==45&&fatigue.FinalDamageMultiplier==1,"exact45 threshold recovers full damage");
        health=damageTarget.CurrentHp;damageResult=new DamageResolver().ApplyToMonster(damageTarget,20,run.State.Player,new System.Random(7));
        Check(Mathf.Abs(health-damageTarget.CurrentHp-20*(damageResult.IsCritical?run.State.Player.CriticalDamageMultiplier:1))<.001f,"at45 actual enemy HP receives full final damage");
        while(fatigue.Value>30)fatigue.Step();fatigue.ShortRest();Check(fatigue.Value==settings.shortRestCeiling&&fatigue.AttackSpeedMultiplier<1&&fatigue.FinalDamageMultiplier==1,"brief rest60 restores damage, retains small speed penalty");
        fatigue.Camp();Check(fatigue.Value==100&&fatigue.IsAtEase&&fatigue.CampUsed,"camp restores ease at100");
        var budget=new System.Text.StringBuilder("# 피로도 이동 예산\n\n공속 <70, 최종 피해 <45. 비용2: 15보까지 무페널티, 16보째 공속 감소, 28보째 피해 감소, 50보째 사망. 짧은 휴식60은 피해만 완전 복구하며 야영100은 여유 복구.\n\n|피로도|공속 배율|최종 피해 배율|\n|---|---|---|\n");
        foreach(var value in new[]{100,98,70,68,60,50,46,45,44,40,30,10,2})budget.AppendLine($"|{value}|{settings.attackSpeed.Evaluate(value):F4}|{settings.finalDamage.Evaluate(value):F4}|");
        File.WriteAllText(Folder+"FatigueBudget.md",budget.ToString());

        foreach(var old in new[]{"BroodKeeper","Data_Monster_B2_Boss_FrostQueen","StormMatriarch","Data_Monster_B3_Boss_SoulEater","EclipseBeast"})
        {
            combat.ResetEncounter();var data=Clone(old);combat.PrepareEncounter(run.State,new[]{data},old=="BroodKeeper"?1:old=="Data_Monster_B3_Boss_SoulEater"||old=="EclipseBeast"?3:2);combat.BeginPreparedCombat();
            yield return new WaitForSeconds(.65f);combat.InputPaused=true;
            var views=Object.FindObjectsByType<MonsterView>(FindObjectsSortMode.None).Where(v=>v.gameObject.activeInHierarchy).ToArray();
            foreach(var view in views)
            {
                var body=Ref<Image>(view,"body");var corners=new Vector3[4];body.rectTransform.GetWorldCorners(corners);var points=corners.Select(p=>(Vector2)scene.LogicalArea.InverseTransformPoint(p)).ToArray();var safe=scene.MonsterSafeBounds;
                Check(points.All(p=>p.x>=safe.xMin-1&&p.x<=safe.xMax+1&&p.y>=safe.yMin-1&&p.y<=safe.yMax+1),old+" actual enemy body lies in requested safe area");
                Check(Mathf.Approximately(view.Root.localScale.x,view.Root.localScale.y),old+" uses uniform aspect-preserving scale");
            }
            var leader=combat.Enemies[0];var main=views.Single(v=>Ref<MonsterData>(v,"boundData")==data);var art=Ref<Image>(main,"body");
            var groupedScale=leader.VisualScale;Check(Mathf.Approximately(groupedScale,scene.GetMonsterScale(data)),old+" species scale independent of support population");
            yield return Capture("Battle-"+old);
            if(old is "Data_Monster_B2_Boss_FrostQueen" or "StormMatriarch" or "Data_Monster_B3_Boss_SoulEater")
            {
                var origin=(Vector2)scene.LogicalArea.InverseTransformPoint(main.Root.position);leader.StartCharge(origin);main.Bind(leader);
                combat.InputPaused=false;yield return null;combat.InputPaused=true;
                Check(art.sprite==data.combatAnimation.ready&&leader.WeakPoints.Count>0,old+" Ready painting and actual charge weak points");
                var attackCaption=Ref<TextMeshProUGUI>(main,"attackNameLabel");var countdown=Ref<TextMeshProUGUI>(main,"chargeTimeLabel");
                var nameBottom=scene.LogicalArea.InverseTransformPoint(attackCaption.transform.position).y-attackCaption.rectTransform.rect.height*.5f;
                var timeTop=scene.LogicalArea.InverseTransformPoint(countdown.transform.position).y+countdown.rectTransform.rect.height*.5f;
                Check(nameBottom>=timeTop+8,old+" attack name and countdown have separate readable rows");
                Canvas.ForceUpdateCanvases();Check(attackCaption.depth>views.Max(v=>Ref<Image>(v,"body").depth),old+" attack captions draw above all monster bodies");
                Check(attackCaption.depth>Ref<Image>(main,"attackNameBackdrop").depth&&countdown.depth>Ref<Image>(main,"chargeTimeBackdrop").depth,old+" caption lettering draws above its brush backdrops");
                Check(data.GetAttacks(MonsterAttackType.Charge).SelectMany(a=>a.chargeWeakPoints).All(p=>data.OverlapsArea(p.localPosition,0,Vector2.zero)),old+" anatomical weak points lie on authored body hitboxes");
                yield return Capture("Ready-"+old);main.PlayAttack(1);main.Bind(leader);yield return Capture("Attack-"+old);
                leader.EnterStun();main.Bind(leader);Check(art.sprite==data.combatAnimation.stunned&&data.combatAnimation.stunned!=data.combatAnimation.idle,old+" distinct authored stun painting");yield return Capture("Stunned-"+old);
            }
        }

        // Real-time support cadence, then force a charge with all supports dead.
        combat.ResetEncounter();var puppet=Clone("BroodKeeper");combat.PrepareEncounter(run.State,new[]{puppet},1);combat.BeginPreparedCombat();yield return new WaitForSeconds(.25f);
        var boss=combat.Enemies[0];foreach(var support in combat.Enemies.Skip(1))support.TakeDamage(support.MaxHp*2);
        Check(boss.HasChargeAttack,"boss retains charge after both supports die");boss.StartCharge(scene.MonsterPosition);Check(boss.CurrentChargeAttack!=null&&boss.WeakPoints.Count>0,"boss can expose meaningful charge weak points with zero supports");boss.EndCharge();
        if(cadence)
        {
        yield return new WaitForSeconds(10.5f);Check(combat.Enemies.Count(e=>e.IsAlive)==1,"no support restored before interval");
        boss.EnterStun();yield return new WaitForSeconds(puppet.stunDuration+.4f);Check(combat.Enemies.Count(e=>e.IsAlive)==1,"stun pauses independent reinforcement timer");
        var deadline=Time.time+4;while(combat.Enemies.Count(e=>e.IsAlive)==1&&Time.time<deadline)yield return null;
        Check(combat.Enemies.Count(e=>e.IsAlive)==2,"first interval restores exactly one support");yield return Capture("FirstSupportRestored");
        yield return new WaitForSeconds(2);Check(combat.Enemies.Count(e=>e.IsAlive)==2,"second support does not reappear with first");
        deadline=Time.time+12;while(combat.Enemies.Count(e=>e.IsAlive)==2&&Time.time<deadline)yield return null;
        Check(combat.Enemies.Count(e=>e.IsAlive)==3,"next interval restores the second support without a charge");
        yield return Capture("SecondSupportRestored");
        }
        combat.InputPaused=true;

        // Freeze the actual final enemy pose/position through production death and result flow.
        var before=Object.FindObjectsByType<MonsterView>(FindObjectsSortMode.None).Where(v=>v.gameObject.activeInHierarchy).ToDictionary(v=>v,v=>(Ref<Image>(v,"body").sprite,v.Root.position,v.Root.localScale));
        var attacker=before.Keys.First();attacker.PlayAttack(1);before[attacker]=(Ref<Image>(attacker,"body").sprite,attacker.Root.position,attacker.Root.localScale);
        combat.Player.TakeEnvironmentalDamage(combat.Player.MaxHp*2);combat.InputPaused=false;yield return null;yield return new WaitForSeconds(1.8f);
        Check(flow.State==GameFlowState.RunDefeat&&!combat.IsActive,"actual death reaches YOU DIED and stops battle");
        Check(before.All(k=>k.Key!=null&&k.Key.gameObject.activeInHierarchy&&Ref<Image>(k.Key,"body").sprite==k.Value.Item1&&k.Key.Root.position==k.Value.Item2&&k.Key.Root.localScale==k.Value.Item3),"all final enemy sprites, positions and scales survive death unchanged");
        Check(!Ref<PlayerView>(combat,"playerView").gameObject.activeSelf,"only foreground player hidden at death");yield return Capture("DeathRetainsBattle");

        Fresh();yield return new WaitForSeconds(.7f);combat.ResetEncounter();
        Check(Ref<PlayerView>(combat,"playerView").gameObject.activeSelf,"new run restores visible player after death");
        var first=combat.Content.events.Single(e=>e.id=="MagicTeacherFirst");var lesson=first.pages[2].options[0];
        Call(flow,"ShowEventPage",new DungeonRoom(999,new DungeonPosition(0,0)){Type=RoomEncounterType.Event},first,2,0);yield return null;Click(expansion.ActiveChoices[0].button);yield return null;
        var offered=Ref<PresentationChoice[]>(expansion,"currentChoices");Check(offered.Length==4&&offered.Take(3).Select(c=>c.Title).Distinct().Count()==3,"teacher offers three distinct random low-tier choices plus leave");yield return Capture("TeacherSpellChoices");
        var chosenTitle=offered[0].Title;var chosen=lesson.effects[0].spellPool.Single(s=>chosenTitle.Contains(s.displayName));Click(expansion.ActiveChoices[0].button);yield return null;
        // OfferSpell may first show an explicit learn/equip confirmation.
        if(!run.State.EventSpellChoices.ContainsKey("TeacherFirstSpell"))Click(expansion.ActiveChoices[0].button);
        yield return null;Check(run.State.EventSpellChoices.TryGetValue("TeacherFirstSpell",out var remembered)&&remembered==chosen&&run.State.RuntimeFor(chosen)!=null,"accepted lesson remembers exact chosen spell and grants it");
        var follow=combat.Content.events.Single(e=>e.id=="MagicTeacherReturn");Call(flow,"ShowEventPage",new DungeonRoom(998,new DungeonPosition(0,0)){Type=RoomEncounterType.Event},follow,0,0);yield return null;
        var route=follow.pages[0].routes.First(r=>r.requirements.All(c=>c.Matches(run.State)));Check(route.requirements.Any(r=>r.kind==EventRequirementKind.ChosenSpell&&r.spell==chosen)&&expansion.modalNarrative.text==follow.pages[route.page].narrative,"return encounter routes on remembered selection rather than current spell ownership");yield return Capture("TeacherRememberedChoice");expansion.HideModal();
        var pools=new HashSet<string>();var withoutFire=false;
        for(var seed=0;seed<20;seed++)
        {
            run.State.Seed=seed;Call(flow,"ApplyEventEffects",first,lesson.effects,0,new List<string>());var choices=Ref<PresentationChoice[]>(expansion,"currentChoices").Take(3).Select(c=>c.Title).ToArray();pools.Add(string.Join("|",choices));withoutFire|=choices.All(t=>!t.Contains("파이어볼트"));
        }
        Check(pools.Count>1&&withoutFire&&lesson.effects[0].spellPool.Any(s=>s.id=="Cleanse"),"random pool varies across seeds, includes utility and does not require FireBolt");expansion.HideModal();

        var dungeon=Object.FindFirstObjectByType<DungeonController>();var hud=Object.FindFirstObjectByType<ArcadeHudController>();hud.mapPanel.SetActive(true);var map=hud.mapPanel.GetComponentInChildren<DungeonMapView>(true);
        var eventRoom=dungeon.State.Graph.Neighbors(dungeon.State.Player.CurrentRoom).First();eventRoom.Type=RoomEncounterType.Event;eventRoom.IsVisited=true;eventRoom.IsCleared=false;map.Render(dungeon.State);yield return null;
        var icon=Object.FindObjectsByType<MapRoomIcon>(FindObjectsSortMode.None).Single(i=>i.Room==eventRoom);Check(Ref<TextMeshProUGUI>(icon,"label").enabled,"unresolved visited event retains question marker");
        eventRoom.IsCleared=true;map.Render(dungeon.State);yield return null;icon=Object.FindObjectsByType<MapRoomIcon>(FindObjectsSortMode.None).Single(i=>i.Room==eventRoom);Check(!Ref<TextMeshProUGUI>(icon,"label").enabled,"consumed one-shot event becomes an empty visited cell");yield return Capture("ConsumedEventMap");
        hud.mapPanel.SetActive(false);Fresh();yield return null;
        File.AppendAllText(Folder+log,$"COMPLETE {passed} PASS / {failed} FAIL\n");File.WriteAllText(Folder+completion,$"{passed} PASS / {failed} FAIL\n");
    }
}
#endif
