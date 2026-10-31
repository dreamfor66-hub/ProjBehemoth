#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonSlash;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class AngularRestorationLiveProbe:MonoBehaviour
{
    const string Folder="Docs/Production/AngularRestore-2026-10-26/";
    bool background;
    void Awake(){background=Application.runInBackground;Application.runInBackground=true;}
    void OnDestroy(){Application.runInBackground=background;}
    void Check(bool ok,string label)=>File.AppendAllText(Folder+"Live.txt",(ok?"PASS ":"FAIL ")+label+"\n");
    public void Run()=>StartCoroutine(Review());
    IEnumerator Capture(string name)
    {
        yield return null;yield return new WaitForEndOfFrame();
        var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+"Screenshots/"+name+".png",texture.EncodeToPNG());Object.Destroy(texture);
    }
    static DungeonRunState State(int mask,FacingDirection facing)
    {
        var graph=new DungeonGraph();var delta=DirectionUtility.Offset(facing);
        var origin=graph.AddRoom(new DungeonPosition(-delta.X,-delta.Y));var room=graph.AddRoom(new DungeonPosition(0,0));graph.Connect(origin,room);
        for(var i=0;i<3;i++){var offset=DirectionUtility.Offset(DirectionUtility.ToAbsolute(facing,(RelativeDirection)i));var neighbor=graph.AddRoom(offset);if((mask&(1<<i))!=0)graph.Connect(room,neighbor);}
        var state=new DungeonRunState(1,graph);state.Player.MoveTo(room);return state;
    }
    IEnumerator Review()
    {
        File.AppendAllText(Folder+"Live.txt","\nRestored composition: scoped existing Editor play session\n");
        var practice=Object.FindFirstObjectByType<OctoberReviewLiveProbe>()??new GameObject("Restored art scoped practice").AddComponent<OctoberReviewLiveProbe>();
        practice.Setup(CharacterKind.Warrior,false,"Assets/Data/Combat/Monsters/TunnelRat.asset");yield return null;
        var battle=Object.FindFirstObjectByType<CombatController>();var scene=Object.FindFirstObjectByType<CombatSceneView>();
        var backdrop=Object.FindFirstObjectByType<StageBackdropView>();var navigation=Object.FindFirstObjectByType<NavigationChoiceView>(FindObjectsInactive.Include);
        var original=UnityEditor.AssetDatabase.LoadAssetAtPath<CombatSpriteAnimation>("Assets/Data/Presentation/Trials/Angular01_WarriorAnimation.asset");
        Check(Mathf.Approximately(battle.Player.Data.combatAnimation.canvasScale,original.canvasScale),"hero image expansion exactly matches accepted trial");
        Check(backdrop.artwork[0].walls.texture.width==1672&&backdrop.artwork[0].walls.texture.height==941,"original full-width environment canvas");
        Check(backdrop.artwork[0].imageOffset==new Vector2(0,-24),"background lowered 24 logical units, without zoom");
        var input=Object.FindFirstObjectByType<CombatInputController>();
        Vector2 ScreenAt(Vector2 p)=>RectTransformUtility.WorldToScreenPoint(scene.UiCamera,scene.LogicalArea.TransformPoint(p));
        void Contact(string method,params object[] args)=>typeof(CombatInputController).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(input,args);
        var hp=battle.Enemies[0].CurrentHp;var center=scene.MonsterPosition+battle.Enemies[0].Data.combatAnimation.combatPositionOffset;
        var from=ScreenAt(center+Vector2.left*110);var to=ScreenAt(center+Vector2.right*110);
        Contact("Begin",-1,from);Contact("Move",-1,to);Contact("End",-1,to,false);yield return null;
        Check(battle.Enemies[0].CurrentHp<hp,"production swipe hits enemy with restored hero framing");
        yield return Capture("Combat-Attack");yield return new WaitForSeconds(.4f);
        var guardFrom=ScreenAt(scene.PlayerPosition+Vector2.up*90);var guardTo=ScreenAt(scene.PlayerPosition+Vector2.down*55);
        Contact("Begin",410,guardFrom);Contact("Move",410,guardTo);yield return new WaitForSeconds(.24f);
        Check(battle.Player.IsGuarding,"original drag-and-hold guard survives art-size restoration");
        yield return Capture("Combat-Guard");Contact("End",410,guardTo,false);yield return null;
        battle.ResetEncounter();backdrop.enabled=false;navigation.SetVisible(true);
        var choices=(List<RoomChoiceButton>)typeof(NavigationChoiceView).GetField("choices",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(navigation);
        foreach(FacingDirection facing in Enum.GetValues(typeof(FacingDirection)))for(var mask=0;mask<8;mask++)
        {
            var state=State(mask,facing);backdrop.Refresh(1,state);navigation.Bind(state,_=>{});
            Check(backdrop.VisibleDoorMask==mask&&backdrop.doorLayers.Select((l,i)=>l.enabled==((mask&(1<<i))!=0)).All(v=>v),$"doors mask={mask} facing={facing}");
        }
        var full=State(7,FacingDirection.North);backdrop.Refresh(1,full);navigation.Bind(full,_=>{});yield return null;Canvas.ForceUpdateCanvases();
        for(var i=0;i<4;i++)
        {
            var rect=(RectTransform)choices[i].transform;
            Check(rect.anchoredPosition==navigation.artworkPositions[i],"accepted navigation slot "+(RelativeDirection)i);
            var results=new List<RaycastResult>();var point=RectTransformUtility.WorldToScreenPoint(scene.UiCamera,rect.position);
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},results);
            Check(results.Count>0&&(results[0].gameObject.transform==choices[i].transform||results[0].gameObject.transform.IsChildOf(choices[i].transform)),"navigation click target unobstructed "+(RelativeDirection)i);
        }
        yield return Capture("Navigation-AllDoors");
        backdrop.Refresh(1,State(0,FacingDirection.North));navigation.Bind(State(0,FacingDirection.North),_=>{});yield return Capture("Navigation-Walls");
        backdrop.Refresh(1,State(5,FacingDirection.North));navigation.Bind(State(5,FacingDirection.North),_=>{});yield return Capture("Navigation-ForwardRight");
        backdrop.enabled=true;Object.FindFirstObjectByType<GameFlowController>().StartNewRun();
        File.AppendAllText(Folder+"Live.txt","COMPLETE. No balance suite or extra Editor instance.\n");
    }
}
#endif
