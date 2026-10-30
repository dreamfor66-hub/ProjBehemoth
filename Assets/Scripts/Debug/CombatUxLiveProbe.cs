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
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class CombatUxLiveProbe:MonoBehaviour
{
    const string Folder="Docs/Production/CombatUX-2026-10-29/";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    CombatController combat;CombatInputController input;CombatSceneView scene;PlayerView player;CombatSpriteAnimator pose;int failures;
    static T Ref<T>(object owner,string name)=>(T)owner.GetType().GetField(name,Private).GetValue(owner);
    void Check(bool ok,string text){if(!ok)failures++;File.AppendAllText(Folder+"Runtime.txt",(ok?"PASS ":"FAIL ")+text+"\n");}
    void Contact(string name,params object[] args)=>typeof(CombatInputController).GetMethod(name,Private).Invoke(input,args);
    Vector2 ScreenAt(Vector2 p)=>RectTransformUtility.WorldToScreenPoint(scene.UiCamera,scene.LogicalArea.TransformPoint(p));
    IEnumerator Capture(string name,float delay=.7f){yield return new WaitForSecondsRealtime(delay);Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Folder+name+".png");yield return null;}
    public bool CardsOnly,EventsOnly;
    public void Run()=>StartCoroutine(Review());
    IEnumerator RecoveryCycle(int id,bool capture)
    {
        var point=ScreenAt(scene.MonsterPosition+Vector2.left*300);
        combat.Player.StartAttackCooldown();Contact("Begin",id,point);Contact("RefreshState");
        yield return null;
        var safe=true;var until=Time.time+combat.Player.CurrentCooldown+1;
        while(combat.Player.AttackCooldownRemaining>0&&Time.time<until)
        {
            Contact("Move",id,point);Contact("RefreshState");
            safe&=!input.IsPreparingAttack&&input.ChargeProgress==0;
            if(pose.State==CombatVisualState.Ready)safe=false;
            yield return null;
        }
        Check(safe,"cycle "+id+": blocked hold has no Ready or charge during recovery");
        Contact("Move",id,point);Contact("RefreshState");yield return null;yield return null;
        Check(input.IsPreparingAttack&&input.ChargeProgress<.2f,"cycle "+id+": sustained hold re-arms with fresh charge time");
        Check(pose.State==(combat.Player.IsGuarding?CombatVisualState.Guard:CombatVisualState.Ready),"cycle "+id+": authored pose matches available action");
        until=Time.time+combat.Player.CurrentChargeDuration+.1f;
        while(Time.time<until){Contact("Move",id,point);Contact("RefreshState");yield return null;}
        Check(input.ChargeProgress>=.99f,"cycle "+id+": continued hold can finish charge");
        if(capture)yield return Capture("Recovery-Ready-And-Charge");
        Contact("End",id,point,true);Contact("RefreshState");yield return null;
    }
    IEnumerator Review()
    {
        Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"Runtime.txt","Scoped existing Unity Editor: cards, event paintings, footprints and recovery input. No DPS/balance suite.\n");
        yield return null;yield return null;
        combat=FindFirstObjectByType<CombatController>();scene=FindFirstObjectByType<CombatSceneView>();input=FindFirstObjectByType<CombatInputController>();
        var expansion=FindFirstObjectByType<RunExpansionView>(FindObjectsInactive.Include);var perks=FindFirstObjectByType<PerkChoiceView>(FindObjectsInactive.Include);
        if(EventsOnly)
        {
            var flow=FindFirstObjectByType<GameFlowController>();var dungeon=FindFirstObjectByType<DungeonController>();
            var showPage=typeof(GameFlowController).GetMethod("ShowEventPage",Private);var conclusion=typeof(GameFlowController).GetMethod("ShowEventConclusion",Private);
            foreach(var story in combat.Content.events)
            {
                AshenVowLiveProbe.ShowEvent(story.id);
                for(var page=0;page<story.pages.Count;page++)
                {
                    showPage.Invoke(flow,new object[]{dungeon.State.Player.CurrentRoom,story,page,0});yield return null;
                    Check(expansion.modalIllustration.sprite==story.illustration,story.id+" page "+page+": inherited placeholder survives authored branch rendering");
                }
                var result=story.pages.SelectMany(p=>p.options).Select(o=>o.resultText).FirstOrDefault(t=>!string.IsNullOrWhiteSpace(t));
                if(result!=null)
                {
                    conclusion.Invoke(flow,new object[]{story,result,0});yield return null;
                    Check(expansion.modalIllustration.sprite==story.illustration,story.id+": outcome retains temporary painting");
                }
            }
            yield return Capture("Event-Temporary-Outcome");
            File.AppendAllText(Folder+"Runtime.txt","COMPLETE event page/outcome follow-up failures="+failures+".\n");
            UnityEditor.EditorApplication.isPlaying=false;yield break;
        }
        var selected="";combat.InputPaused=true;
        var options=new[]{"Exploit","IceMastery","DecisiveBlow"}.Select(id=>combat.Content.perks.First(p=>p.name==id)).ToArray();
        perks.Show(options,p=>selected=p.name);yield return null;yield return null;
        var cards=Ref<List<PerkChoiceItem>>(perks,"items");
        Check(cards.Take(3).All(c=>c.effect!=null&&c.effect.text.Length>0),"all three perk effects visible inside their own cards");
        Check(cards.Take(3).All(c=>Ref<Image>(c,"icon").sprite!=null&&UnityEditor.AssetDatabase.GetAssetPath(Ref<Image>(c,"icon").sprite).Contains("Items2D")),"perk cards use new 2D paintings");
        EventSystem.current.SetSelectedGameObject(Ref<Button>(cards[1],"button").gameObject);Ref<Button>(cards[1],"button").onClick.Invoke();yield return null;
        Check(selected==""&&cards[1].selectedMark.enabled&&!cards[0].selectedMark.enabled,"candidate selection highlights without premature grant");
        yield return Capture("Perks-Final");perks.confirm.onClick.Invoke();Check(selected==options[1].name,"confirm grants the selected candidate");perks.Hide();
        var relics=new[]{"Starter_Monk","Tome_EnchantLightning","StormBottle"}.Select(id=>combat.Content.equipment.First(e=>e.name==id)).ToArray();
        perks.ShowRelics(relics,_=>{});yield return null;yield return null;
        var scroll=cards[0].effectScroll;
        Canvas.ForceUpdateCanvases();var hits=new List<RaycastResult>();
        var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(scene.UiCamera,scroll.viewport.position)};
        EventSystem.current.RaycastAll(pointer,hits);
        Check(hits.Count>0&&ExecuteEvents.GetEventHandler<IScrollHandler>(hits[0].gameObject)==scroll.gameObject,"actual effect viewport raycast routes scrolling to its own card");
        if(scroll.content.rect.height>scroll.viewport.rect.height)
        {
            ExecuteEvents.Execute(scroll.gameObject,new PointerEventData(EventSystem.current){scrollDelta=new Vector2(0,-2)},ExecuteEvents.scrollHandler);yield return null;
            Check(scroll.verticalNormalizedPosition<.99f,"long relic description scrolls within its own card");scroll.verticalNormalizedPosition=1;
        }
        yield return Capture("Relics-Final");perks.Hide();
        if(CardsOnly)
        {
            File.AppendAllText(Folder+"Runtime.txt","COMPLETE card follow-up failures="+failures+". Remaining scoped combat/event/size checks passed in Runtime-FirstPass.txt.\n");
            perks.Show(options,_=>{});yield return null;UnityEditor.EditorApplication.isPaused=true;yield break;
        }
        var allEvents=combat.Content.events.ToArray();
        foreach(var story in allEvents)
        {
            AshenVowLiveProbe.ShowEvent(story.id);yield return null;
            Check(story.illustratedPopup&&expansion.modalIllustration.sprite!=null&&expansion.modalIllustration.sprite.texture.width>1000,story.id+": actual event opens with temporary portrait painting");
            Check(!expansion.nextPage.gameObject.activeSelf,story.id+": choices together without page controls");
        }
        AshenVowLiveProbe.ShowEvent("Delivery");yield return Capture("Event-Temporary-Painting");expansion.HideModal();
        var practice=new GameObject("Scoped recovery practice").AddComponent<OctoberReviewLiveProbe>();practice.Setup(CharacterKind.Warrior,false);
        player=FindFirstObjectByType<PlayerView>();pose=Ref<Image>(player,"body").GetComponent<CombatSpriteAnimator>();input.SpellModifierForVerification=false;
        yield return new WaitForSecondsRealtime(.7f);
        var hud=FindFirstObjectByType<CombatHudView>();Check(hud.attackRecovery!=null&&hud.attackRecovery.gameObject.activeSelf,"saved fixed sword recovery HUD visible in combat");
        combat.Player.StartAttackCooldown();yield return null;yield return null;
        Check(hud.attackRecovery.IsRecovering&&hud.attackRecovery.Progress<1,"recovery HUD shows unavailable state without text");yield return Capture("Recovery-HUD",.03f);
        combat.Player.ResetAttackCooldown();yield return null;
        var earlyPoint=ScreenAt(scene.MonsterPosition+Vector2.left*300);
        combat.Player.StartAttackCooldown();Contact("Begin",909,earlyPoint);Contact("RefreshState");Contact("End",909,earlyPoint,false);Contact("RefreshState");
        Check(!input.IsPreparingAttack&&Ref<IDictionary>(input,"contacts").Count==0,"press and release during cooldown leaves no queued attack or contact");
        for(var cycle=0;cycle<3;cycle++)yield return RecoveryCycle(910+cycle,cycle==0);
        var guardStart=ScreenAt(new Vector2(scene.PlayerPosition.x,scene.LogicalArea.rect.yMin+140));var guardEnd=ScreenAt(new Vector2(scene.PlayerPosition.x,scene.LogicalArea.rect.yMin+40));
        Contact("Begin",950,guardStart);Contact("Move",950,guardEnd);yield return null;
        for(var cycle=0;cycle<3;cycle++)
        {
            yield return RecoveryCycle(960+cycle,false);
            Check(combat.Player.IsGuarding&&Ref<IDictionary>(input,"contacts").Count==1,"guard remains held across recovery/attack lift/repress "+cycle);
        }
        Contact("End",950,guardEnd,false);Contact("RefreshState");yield return null;
        Check(!combat.Player.IsGuarding&&!input.IsPreparingAttack,"final independent release restores Idle");
        var skeleton=UnityEditor.AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/Combat/Monsters/Data_Monster_B1_Minion_Skeleton.asset");
        var goblin=UnityEditor.AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/Combat/Monsters/Data_Monster_B1_Minion_Bat.asset");
        var bat=UnityEditor.AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/Combat/Monsters/Data_Monster_B1_Minion_Leech.asset");
        var spider=UnityEditor.AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/Combat/Monsters/TunnelRat.asset");
        var heights=new Dictionary<MonsterData,float>();
        foreach(var m in new[]{goblin,skeleton,bat,spider})
        {
            combat.ResetEncounter();combat.PrepareEncounter(combat.Run,new[]{m},1);combat.BeginPreparedCombat();combat.InputPaused=true;yield return null;
            var single=FindFirstObjectByType<MonsterView>();var size=Ref<Image>(single,"body").rectTransform.sizeDelta;
            heights[m]=size.y;
            combat.ResetEncounter();combat.PrepareEncounter(combat.Run,new[]{m,m},1);combat.BeginPreparedCombat();combat.InputPaused=true;yield return null;
            var pair=FindObjectsByType<MonsterView>(FindObjectsSortMode.None).Where(v=>v.gameObject.activeInHierarchy).ToArray();
            Check(pair.Length==2&&pair.All(v=>Vector2.Distance(Ref<Image>(v,"body").rectTransform.sizeDelta,size)<.1f),m.displayName+": same artwork size alone and in a pair");
            foreach(var active in Ref<IList>(combat,"activeMonsters"))
            {
                var type=active.GetType();var enemy=(MonsterRuntime)type.GetProperty("Runtime").GetValue(active);
                var position=(Vector2)type.GetProperty("Position").GetValue(active);enemy.StartCharge(position);
                Check(enemy.State==MonsterState.Charging&&enemy.WeakPoints.Count>0,m.displayName+": resized species exposes authored weak points");
            }
            typeof(CombatController).GetMethod("SyncWeakPointViews",Private).Invoke(combat,null);yield return null;
            foreach(var active in Ref<IList>(combat,"activeMonsters"))
            {
                var type=active.GetType();var enemy=(MonsterRuntime)type.GetProperty("Runtime").GetValue(active);
                var views=(List<WeakPointView>)type.GetProperty("WeakPointViews").GetValue(active);
                Check(views.Count==enemy.WeakPoints.Count&&views.Select((v,i)=>Vector2.Distance(((RectTransform)v.transform).anchoredPosition,enemy.WeakPoints[i].HitShape.Center)<.1f).All(v=>v),m.displayName+": displayed weak points agree with resized runtime hit centers");
            }
            if(m==spider)yield return Capture("Small-Species-WeakPoints");
        }
        Check(Mathf.Abs(heights[goblin]-heights[skeleton])<2,"Goblin Hunter matches Skeleton Warrior baseline height");
        Check(heights[bat]<heights[skeleton]*.6f&&heights[spider]<heights[skeleton]*.6f,"bat and spider are visibly smaller species than humanoid baseline");
        combat.ResetEncounter();combat.PrepareEncounter(combat.Run,new[]{goblin,skeleton},1);combat.BeginPreparedCombat();combat.InputPaused=true;yield return null;
        yield return Capture("Pair-Scale-And-Spacing");
        combat.ResetEncounter();combat.PrepareEncounter(combat.Run,new[]{bat,spider},1);combat.BeginPreparedCombat();combat.InputPaused=true;yield return null;
        yield return Capture("Small-Species");
        Check(Mathf.Abs(scene.GetMonsterFormationPosition(1,2).x-scene.GetMonsterFormationPosition(0,2).x)<540,"default paired center distance reduced");
        File.AppendAllText(Folder+"Runtime.txt","COMPLETE failures="+failures+". Physical touch hardware remains untested.\n");
        combat.ResetEncounter();perks.Show(options,_=>{});yield return null;UnityEditor.EditorApplication.isPaused=true;
    }
}
#endif
