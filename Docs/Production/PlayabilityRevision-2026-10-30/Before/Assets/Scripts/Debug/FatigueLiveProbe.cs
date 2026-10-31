#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonSlash;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Object=UnityEngine.Object;

public sealed class FatigueLiveProbe:MonoBehaviour
{
    const string Log="Logs/Fatigue-Live.txt";
    RunController run;DungeonController dungeon;GameFlowController flow;CombatController combat;RunExpansionView expansion;FatigueHudView hud;
    bool background;
    void Awake(){background=Application.runInBackground;Application.runInBackground=true;}
    void OnDestroy(){Application.runInBackground=background;}
    void Bind(){run=Object.FindFirstObjectByType<RunController>();dungeon=Object.FindFirstObjectByType<DungeonController>();flow=Object.FindFirstObjectByType<GameFlowController>();combat=Object.FindFirstObjectByType<CombatController>();expansion=Object.FindFirstObjectByType<RunExpansionView>();hud=Object.FindFirstObjectByType<FatigueHudView>();}
    void Check(bool yes,string text){File.AppendAllText(Log,(yes?"PASS ":"FAIL ")+text+"\n");if(!yes)Debug.LogError("Fatigue: "+text);}
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(owner,args);
    void Value(float value)
    {run.State.Fatigue.BeginFloor(combat.Content.fatigue,dungeon.State.Graph);while(run.State.Fatigue.GraceRemaining>0||run.State.Fatigue.Value>value)run.State.Fatigue.Step();hud.Refresh();}
    void Fresh(){var original=RunSession.ForcedSeed;try{RunSession.ForcedSeed=0;flow.StartNewRun();expansion.HideModal();}finally{RunSession.ForcedSeed=original;}}
    void StepTo(DungeonRoom room)=>Call(dungeon,"Move",DirectionUtility.ToRelative(dungeon.State.Player.Facing,DirectionUtility.FromDelta(new DungeonPosition(room.Position.X-dungeon.State.Player.CurrentRoom.Position.X,room.Position.Y-dungeon.State.Player.CurrentRoom.Position.Y))));
    public void Run()=>StartCoroutine(Matrix());
    public void Manual(string mode)
    {Bind();if(mode=="capture"){StartCoroutine(CaptureLater("Manual-Final"));return;}Fresh();if(mode=="danger"){Value(8);hud.hover.OnPointerEnter(new PointerEventData(EventSystem.current));}else if(mode=="camp"){Value(40);var camp=dungeon.State.Graph.Rooms.Single(r=>r.Type==RoomEncounterType.Camp);dungeon.State.Player.MoveTo(camp);dungeon.HideNavigation();Call(flow,"ShowCamp",camp);}else if(mode=="faces")StartCoroutine(Faces());}
    IEnumerator CaptureLater(string name){yield return new WaitForSeconds(.4f);yield return new WaitForEndOfFrame();Capture(name);}
    static void Capture(string name)
    {
        var frame=Object.FindFirstObjectByType<ArcadeRasterCamera>().Frame;var old=RenderTexture.active;RenderTexture.active=frame;
        var tex=new Texture2D(frame.width,frame.height,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,tex.width,tex.height),0,0);tex.Apply();Directory.CreateDirectory("Docs/Production/Fatigue/Screenshots");File.WriteAllBytes("Docs/Production/Fatigue/Screenshots/"+name+".png",tex.EncodeToPNG());Object.Destroy(tex);RenderTexture.active=old;
    }
    IEnumerator Faces()
    {
        var selected=RunSession.SelectedCharacter;
        foreach(var character in combat.Content.characters)
        {
            RunSession.SelectedCharacter=character;Fresh();
            foreach(var value in new[]{100,80,40,8}){Value(value);yield return null;yield return new WaitForEndOfFrame();Capture($"Face-{character.character}-{value}");}
        }
        RunSession.SelectedCharacter=selected;Fresh();
    }
    IEnumerator Matrix()
    {
        Bind();File.WriteAllText(Log,"Scoped fatigue mechanics and UI in existing Unity instance; runtime practice monsters cloned.\n");Fresh();yield return null;
        var settings=combat.Content.fatigue;var config=UnityEditor.AssetDatabase.LoadAssetAtPath<DungeonGenerationSettings>("Assets/Data/Settings/DungeonGenerationSettings.asset");
        var maps=true;for(var floor=1;floor<=3;floor++)for(var seed=0;seed<32;seed++)
        {var state=new DungeonGenerator(config).Generate(seed,floor);maps&=state.Graph.Rooms.Count(r=>r.Type==RoomEncounterType.Camp)==1;maps&=state.Graph.DistancesFrom(state.Graph.StartRoom).Count==state.Graph.Rooms.Count;}
        Check(maps,"96 B1/B2/B3 maps have exactly one reachable fixed camp");
        var originalCamp=dungeon.State.Graph.Rooms.Single(r=>r.Type==RoomEncounterType.Camp);var boss=dungeon.State.Graph.Rooms.Single(r=>r.Type==RoomEncounterType.Boss);var count=dungeon.State.Graph.Rooms.Count(r=>r.Type==RoomEncounterType.Combat&&!r.IsCleared);
        CampMonsterMovement.Move(dungeon.State);Check(originalCamp.Type==RoomEncounterType.Camp&&boss.Type==RoomEncounterType.Boss&&count==dungeon.State.Graph.Rooms.Count(r=>r.Type==RoomEncounterType.Combat&&!r.IsCleared),"camp shift preserves fixed camp/boss and living ordinary encounter count");
        var graph=new DungeonGraph();var start=graph.AddRoom(new DungeonPosition(0,0));var source=graph.AddRoom(new DungeonPosition(1,0));var destination=graph.AddRoom(new DungeonPosition(2,0));graph.Connect(start,source);graph.Connect(source,destination);start.Type=RoomEncounterType.Start;source.Type=RoomEncounterType.Combat;destination.Type=RoomEncounterType.Empty;destination.IsVisited=destination.IsCleared=true;
        var moved=CampMonsterMovement.Move(new DungeonRunState(7,graph));Check(moved==1&&destination.Type==RoomEncounterType.Combat&&!destination.IsCleared&&destination.IsVisited&&destination.EncounterOriginId==source.RoomId&&source.IsCleared,"living monster can occupy cleared route without reroll or resurrection");
        Fresh();yield return null;var next=dungeon.State.Graph.Neighbors(dungeon.State.Player.CurrentRoom).First();next.Type=RoomEncounterType.Empty;next.IsCleared=true;var first=dungeon.State.Player.CurrentRoom;var before=run.State.Fatigue.GraceRemaining;
        StepTo(next);yield return new WaitForSeconds(.7f);StepTo(first);yield return new WaitForSeconds(.7f);
        Check(run.State.Fatigue.Steps==2&&run.State.Fatigue.GraceRemaining==before-2&&run.State.Fatigue.Value==100,"actual forward/back movement consumes two grace steps, no fatigue yet");
        if(dungeon.State.Player.CurrentRoom.Connections.Count==4)dungeon.State.Player.MoveTo(dungeon.State.Graph.Rooms.First(r=>r.Connections.Count<4));
        var current=dungeon.State.Player.CurrentRoom;var invalid=Enum.GetValues(typeof(RelativeDirection)).Cast<RelativeDirection>().FirstOrDefault(dir=>!current.Connections.Contains(dungeon.State.Graph.At(current.Position+DirectionUtility.Offset(DirectionUtility.ToAbsolute(dungeon.State.Player.Facing,dir)))?.RoomId??-1));
        if(current.Connections.Count<4){Call(dungeon,"Move",invalid);Check(run.State.Fatigue.Steps==2,"wall input consumes no fatigue or grace");}
        var steps=run.State.Fatigue.Steps;yield return new WaitForSeconds(.2f);Check(run.State.Fatigue.Steps==steps,"waiting does not consume movement resource");
        Value(90);Check(run.State.Fatigue.AttackSpeedMultiplier<1&&run.State.Fatigue.FinalDamageMultiplier==1,"attack speed penalty begins before final damage penalty");
        Value(60);Check(run.State.Fatigue.AttackSpeedMultiplier<.8f&&run.State.Fatigue.FinalDamageMultiplier<1,"later fatigue also reduces final damage");
        var practice=Object.FindFirstObjectByType<OctoberReviewLiveProbe>()??new GameObject("Fatigue practice").AddComponent<OctoberReviewLiveProbe>();practice.Setup(CharacterKind.Rogue,false);yield return null;Bind();combat.InputPaused=true;var cd=run.State.Player.CurrentCooldown;Value(40);
        Check(run.State.Player.CurrentCooldown>cd*1.5f,"fatigue increases actual weapon recovery interval");var target=combat.Enemies[0];var hp=target.CurrentHp;var resolver=new DamageResolver();
        var result=resolver.ApplyToMonster(target,100,run.State.Player,new System.Random(1));Check(Mathf.Abs(hp-target.CurrentHp-100*run.State.Fatigue.FinalDamageMultiplier)<.05f,"direct damage final multiplier applies to actual enemy HP");
        var bolt=combat.Content.spells.First(s=>s.id=="EnergyBolt");run.State.LearnSpell(bolt,true);run.State.RuntimeFor(bolt).ResetCooldown();run.State.Player.RefillMp();combat.InputPaused=false;hp=target.CurrentHp;
        var scene=Object.FindFirstObjectByType<CombatSceneView>();var position=scene.MonsterPosition;
        var cast=combat.TryCastSpell(bolt.defaultSlot,1,new AttackSegment(position+Vector2.down*100,position,24));combat.InputPaused=true;
        Check(cast&&Mathf.Abs(hp-target.CurrentHp-bolt.damage*run.State.Fatigue.FinalDamageMultiplier)<.05f,"aimed spell damage also respects fatigue final multiplier");
        target.AddBurn(2,8,run.State);target.Buffs.UpdateState(0);hp=target.CurrentHp;target.Buffs.Tick(1.1f);Check(Mathf.Abs(hp-target.CurrentHp-2*run.State.Fatigue.FinalDamageMultiplier)<.05f,"burn tick respects current fatigue final damage");target.Buffs.Cleanse();
        target.AddPoison(1,3,8,60,run.State);target.Buffs.UpdateState(0);hp=target.CurrentHp;target.Buffs.Tick(2.1f);Check(Mathf.Abs(hp-target.CurrentHp-3*run.State.Fatigue.FinalDamageMultiplier)<.05f,"poison ticks also respect current fatigue final damage");target.Buffs.Cleanse();
        var potion=run.Equipment.Single(e=>e.id=="FatiguePotion");run.State.TryStorePotion(potion);Check(run.State.CanUsePotion(potion,false)&&!run.State.CanUsePotion(potion,true),"fatigue potion offers drink, never throw");var value=run.State.Fatigue.Value;Check(run.TryUsePotion(potion)&&run.State.Fatigue.Value==value+30&&run.State.Fatigue.GraceRemaining==10,"drinking recovers30 and grants10 safe steps");
        Fresh();yield return null;run.State.TryStorePotion(potion);Check(!run.TryUsePotion(potion)&&run.State.Potions.Contains(potion),"full fatigue meter preserves potion");
        var stock=false;for(var seed=0;seed<128;seed++)stock|=run.GetShopInventory(seed,1).Contains(potion);Check(stock,"fatigue potion can occur in natural merchant stock");
        Value(40);run.State.Player.TakeEnvironmentalDamage(20);var health=run.State.Player.CurrentHp;var camp=dungeon.State.Graph.Rooms.Single(r=>r.Type==RoomEncounterType.Camp);dungeon.State.Player.MoveTo(camp);dungeon.HideNavigation();Call(flow,"ShowCamp",camp);yield return new WaitForSeconds(.4f);yield return new WaitForEndOfFrame();Capture("Camp-Choices");
        Check(expansion.ActiveChoices.Length==3&&expansion.ActiveChoices.All(c=>c.button.gameObject.activeInHierarchy)&&!expansion.nextPage.gameObject.activeSelf&&!hud.face.gameObject.activeSelf,"camp shows all three choices on one page and hides exploration face");
        Check(expansion.ActiveChoices[0].button.interactable&&expansion.ActiveChoices[1].button.interactable,"camp offers both full camp and brief rest at fatigue40");expansion.ActiveChoices[1].button.onClick.Invoke();yield return null;
        Check(run.State.Fatigue.Value==60&&run.State.Player.CurrentHp==health&&!run.State.Fatigue.CampUsed&&run.State.Fatigue.GraceRemaining==0,"brief rest restores only fatigue60, preserving one camp and giving no HP or grace");
        expansion.HideModal();Call(flow,"ShowCamp",camp);expansion.ActiveChoices[0].button.onClick.Invoke();yield return null;Check(run.State.Fatigue.Value==100&&run.State.Fatigue.CampUsed&&run.State.Player.CurrentHp==health+10&&run.State.Fatigue.GraceRemaining>0,"one full camp restores100, HP10 and walking grace");
        expansion.HideModal();Call(flow,"ShowCamp",camp);Check(!expansion.ActiveChoices[0].button.interactable,"full camp stays spent on revisiting fixed location");expansion.HideModal();
        while(run.State.Fatigue.GraceRemaining>0||run.State.Fatigue.Value>40)run.State.Fatigue.Step();Call(flow,"ShowCamp",camp);expansion.ActiveChoices[1].button.onClick.Invoke();Check(run.State.Fatigue.Value==60&&run.State.Fatigue.CampUsed,"brief rest remains available after full camp");expansion.HideModal();
        Value(8);hud.hover.OnPointerEnter(new PointerEventData(EventSystem.current));yield return null;yield return new WaitForEndOfFrame();Capture("Danger-Tooltip");
        Check(hud.hover.view.IsVisible&&hud.hover.view.GetComponentsInChildren<TextMeshProUGUI>().Any(t=>t.text.Contains("더 무리해서 움직였다가는 죽을 수도 있다.")),"at8 tooltip includes exact death warning");hud.hover.OnPointerExit(new PointerEventData(EventSystem.current));
        Check(hud.faces.heroes.Length==6&&hud.faces.heroes.All(h=>h.expressions.Length==4&&h.expressions.Distinct().Count()==4)&&hud.face.color==Color.white,"six heroes each have four distinct expression sprites without changing icon color");
        yield return Faces();
        Value(2);next=dungeon.State.Graph.Neighbors(dungeon.State.Player.CurrentRoom).First();next.Type=RoomEncounterType.Empty;next.IsCleared=true;StepTo(next);yield return null;
        Check(flow.State==GameFlowState.RunDefeat&&!run.State.Player.IsAlive&&run.State.Fatigue.Value==0,"actual final step at fatigue2 ends run at0 with death");yield return new WaitForEndOfFrame();Capture("Fatigue-Death");
        Fresh();Value(10);Call(flow,"ResolveBossFloor");yield return null;Object.FindFirstObjectByType<RoomEventView>().Hide();yield return null;yield return new WaitForSeconds(.2f);
        Check(dungeon.Floor==2&&run.State.Fatigue.Value==100&&!run.State.Fatigue.CampUsed&&run.State.Fatigue.GraceRemaining>0,"real next-floor flow restores fatigue and one new camp opportunity");
        Fresh();File.AppendAllText(Log,"COMPLETE\n");
    }
}
#endif

