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

public sealed class EventGroundingLiveProbe : MonoBehaviour
{
    const string Folder="Docs/Production/EventGrounding-2026-10-31/";
    GameFlowController flow; RunController run; DungeonController dungeon; ExplorationView view; RunExpansionView expansion;
    int checks,failures; bool background,hadHistory; string history; BaseInputModule[] modules;
    static T Ref<T>(object source,string name)=>(T)source.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(source);
    static void Call(object source,string name,params object[] args)=>source.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(source,args);
    void Awake()
    {
        background=Application.runInBackground; Application.runInBackground=true;
        hadHistory=PlayerPrefs.HasKey("Behemoth.EventHistory.v1"); history=PlayerPrefs.GetString("Behemoth.EventHistory.v1","");
        modules=EventSystem.current.GetComponents<BaseInputModule>().Where(m=>m.enabled).ToArray(); foreach(var m in modules)m.enabled=false;
    }
    void OnDestroy()
    {
        Application.runInBackground=background;
        if(hadHistory)PlayerPrefs.SetString("Behemoth.EventHistory.v1",history);else PlayerPrefs.DeleteKey("Behemoth.EventHistory.v1");
        foreach(var m in modules)if(m!=null)m.enabled=true;
    }
    void Check(bool ok,string message)
    {checks++;if(!ok)failures++;File.AppendAllText(Folder+"Live.txt",(ok?"PASS ":"FAIL ")+message+"\n");}
    bool Click(Button button)
    {
        Canvas.ForceUpdateCanvases();var canvas=button.GetComponentInParent<Canvas>().rootCanvas;
        var data=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,button.transform.TransformPoint(((RectTransform)button.transform).rect.center))};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
        var ok=hits.Count>0&&ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==button.gameObject&&button.IsInteractable();
        Check(ok,"actual pointer target "+button.name+"; top="+(hits.Count==0?"none":hits[0].gameObject.name));if(!ok)return false;
        ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);return true;
    }
    IEnumerator Capture(string name)
    {yield return new WaitForSecondsRealtime(.45f);yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+name+".jpg",t.EncodeToJPG(90));Destroy(t);}
    void Show()=>Call(flow,"ShowNavigation");
    void Prepare(RandomEventData story)
    {
        flow.DebugPlaceRoomObject(story.worldObject,0,false);
        var room=dungeon.State.Player.CurrentRoom;room.ObjectPlacementPrepared=false;room.Story=story;room.StoryPrepared=true;room.IsCleared=false;Show();
    }
    public void Run()=>StartCoroutine(Review());
    public void RunPlacement()=>StartCoroutine(ReviewPlacement());
    IEnumerator ReviewPlacement()
    {
        File.WriteAllText(Folder+"Live.txt","Scoped final placement/click review after navigation-occlusion correction.\n");
        flow=FindFirstObjectByType<GameFlowController>();run=FindFirstObjectByType<RunController>();dungeon=FindFirstObjectByType<DungeonController>();view=FindFirstObjectByType<ExplorationView>();expansion=Ref<RunExpansionView>(flow,"expansionView");
        flow.StartNewRun();yield return new WaitForEndOfFrame();
        var stories=Ref<GameContent>(flow,"content").events.Where(e=>e!=null&&!e.excludedFromRandomPool).ToArray();
        foreach(var act in new[]{1,2,3})
        {
            flow.DebugJumpToChapter(act);yield return new WaitForEndOfFrame();
            foreach(var story in stories)
            {
                Prepare(story);yield return new WaitForEndOfFrame();
                var slot=view.slots.First(s=>s.gameObject.activeInHierarchy&&s.BoundData==story.worldObject);
                Check(slot.painting.sprite==story.worldObject.front,$"B{act}/{story.id} visible first-facing painting after occlusion correction");
                yield return Capture($"B{act}-{story.id}-World");Click(slot.button);yield return new WaitForEndOfFrame();
                Check(expansion.IsDecisionOpen,$"B{act}/{story.id} first click opens owned event");
            }
            foreach(var id in new[]{"Training","Scholar","LostBeliever","Mirror"})
            {
                var story=stories.Single(s=>s.id==id);Prepare(story);var room=dungeon.State.Player.CurrentRoom;
                var option=id=="Training"||id=="Scholar"?story.pages.SelectMany(p=>p.options).First(o=>o.useWorldObject)
                    :id=="LostBeliever"?story.pages[0].options[1]:story.pages[2].options[0];
                room.IsCleared=true;Call(flow,"ApplyRoomObjectOutcome",room,option);Show();yield return new WaitForEndOfFrame();
                Check(option.removeWorldObject?view.VisibleObjects==0:view.VisibleObjects==1,$"B{act}/{id} final choice presence");
                if(act==1)yield return Capture("Used-"+id);
            }
            flow.DebugPlaceRoomObject(dungeon.Exploration.For(RoomEncounterType.Shop),0,false);
            dungeon.State.Player.CurrentRoom.ObjectPlacementPrepared=false;Show();yield return new WaitForEndOfFrame();
            yield return Capture("B"+act+"-Merchant");
        }
        flow.DebugJumpToChapter(1);yield return new WaitForEndOfFrame();
        File.WriteAllText(Folder+"PlacementComplete.txt",$"{checks} checks; {failures} failures. Actual Unity renders and UI raycasts for 19 event subjects in all 3 acts after final placement correction.\n");
        UnityEditor.EditorApplication.isPlaying=false;
    }
    IEnumerator Review()
    {
        File.WriteAllText(Folder+"Live.txt","Scoped event subjects, first facing, choice aftermath and camps.\n");
        if(File.Exists(Folder+"Complete.txt"))File.Delete(Folder+"Complete.txt");
        flow=FindFirstObjectByType<GameFlowController>();run=FindFirstObjectByType<RunController>();dungeon=FindFirstObjectByType<DungeonController>();view=FindFirstObjectByType<ExplorationView>();expansion=Ref<RunExpansionView>(flow,"expansionView");
        flow.StartNewRun();yield return new WaitForEndOfFrame();
        var content=Ref<GameContent>(flow,"content");var stories=content.events.Where(e=>e!=null&&!e.excludedFromRandomPool).ToArray();
        Check(stories.Length==19,"all 19 active event opportunities retained");
        Check(stories.All(s=>s.worldObject!=null)&&stories.Select(s=>s.worldObject).Distinct().Count()==stories.Length,"one unique object data per active event");
        Check(stories.Select(s=>s.worldObject.front).Distinct().Count()==stories.Length,"one unique front painting per active event");
        var services=dungeon.Exploration.roomObjects.Select(b=>b.artwork).Concat(dungeon.Exploration.concealedEntranceClues).ToArray();
        Check(stories.All(s=>!services.Contains(s.worldObject)),"no event uses a shop, shrine, fountain, chest, camp or hidden-door subject");
        var backdrop=FindFirstObjectByType<StageBackdropView>();
        foreach(var act in new[]{1,2,3})
        {
            flow.DebugJumpToChapter(act);yield return new WaitForEndOfFrame();
            foreach(var story in stories)
            {
                Prepare(story);yield return new WaitForEndOfFrame();var room=dungeon.State.Player.CurrentRoom;
                var slot=view.slots.FirstOrDefault(s=>s.gameObject.activeInHierarchy&&s.BoundData==story.worldObject);
                Check(slot!=null&&slot.painting.sprite==story.worldObject.front,$"B{act}/{story.id} visible authored front");
                if(story.worldObject.facePlayerOnFirstEncounter)
                {
                    foreach(var from in dungeon.State.Graph.Neighbors(room).ToArray())
                    {
                        dungeon.State.Player.MoveTo(from);dungeon.State.Player.MoveTo(room);room.ObjectPlacementPrepared=false;Show();yield return new WaitForEndOfFrame();
                        var facing=room.ObjectFacing;var corner=room.ObjectCorner;
                        slot=view.slots.First(s=>s.gameObject.activeInHierarchy&&s.BoundData==story.worldObject);
                        Check(((int)facing-(int)dungeon.State.Player.Facing+6)%4==0&&slot.painting.sprite==story.worldObject.front,$"B{act}/{story.id} first entry from {from.Position} faces player");
                        var another=dungeon.State.Graph.Neighbors(room).First(n=>n!=from);dungeon.State.Player.MoveTo(another);dungeon.State.Player.MoveTo(room);Show();yield return new WaitForEndOfFrame();
                        Check(room.ObjectFacing==facing&&room.ObjectCorner==corner,$"B{act}/{story.id} revisit preserves world transform");
                    }
                    Prepare(story);yield return new WaitForEndOfFrame();slot=view.slots.First(s=>s.gameObject.activeInHierarchy&&s.BoundData==story.worldObject);
                }
                yield return Capture($"B{act}-{story.id}-World");
                Click(slot.button);yield return new WaitForEndOfFrame();
                Check(flow.State==GameFlowState.RoomEvent&&expansion.IsDecisionOpen,$"B{act}/{story.id} opens actual event choices");
                if(act==1)yield return Capture("Popup-"+story.id);
                // Exercise every authored terminal through the same persistent-room primitive used by real choices.
                var options=story.pages.Count>0?story.pages.SelectMany(p=>p.options):story.options;
                foreach(var option in options.Where(o=>o.nextPage<0))
                {
                    Prepare(story);room=dungeon.State.Player.CurrentRoom;room.IsCleared=true;Call(flow,"ApplyRoomObjectOutcome",room,option);Show();yield return new WaitForEndOfFrame();
                    var remaining=view.slots.FirstOrDefault(s=>s.gameObject.activeInHierarchy&&s.BoundData==story.worldObject);
                    Check(option.removeWorldObject?remaining==null:remaining!=null,$"B{act}/{story.id}/{option.title} matching presence after choice");
                    Check(room.RevisitTextKey==option.revisitTextKey&&!string.IsNullOrWhiteSpace(GameText.Get(option.revisitTextKey)),"authored localized revisit text");
                    if(remaining!=null)
                    {
                        Check(remaining.painting.sprite==story.worldObject.Painting(0,option.useWorldObject),"resolved choice uses the corresponding authored painting");
                        if(act==1&&option.useWorldObject)yield return Capture("Used-"+story.id);
                        Click(remaining.button);yield return new WaitForEndOfFrame();
                        Check(!expansion.IsDecisionOpen&&view.narration.alpha>0&&view.narrationText.text==GameText.Get(option.revisitTextKey),"resolved click narrates without reopening choices");
                    }
                }
            }
            flow.DebugPlaceRoomObject(dungeon.Exploration.For(RoomEncounterType.Camp),0,false);yield return new WaitForEndOfFrame();
            Check(dungeon.State.Player.CurrentRoom.Connections.Count==1&&backdrop.VisibleDoorMask==0,"camp only has entry behind player");
            Check(backdrop.background.sprite==backdrop.artwork[act-1].campInterior&&view.slots.Where(s=>s.gameObject.activeInHierarchy).All(s=>!s.painting.enabled),"complete camping room replaces isolated fire painting");
            yield return Capture("B"+act+"-Camp");Click(view.slots.First(s=>s.gameObject.activeInHierarchy).button);yield return new WaitForEndOfFrame();Check(expansion.IsDecisionOpen,"camp remains interactive");
            flow.DebugPlaceRoomObject(dungeon.Exploration.For(RoomEncounterType.Shop),0,false);yield return new WaitForEndOfFrame();
            var merchant=view.slots.First(s=>s.gameObject.activeInHierarchy&&s.BoundData==dungeon.Exploration.For(RoomEncounterType.Shop));
            Check(merchant.painting.sprite==dungeon.Exploration.For(RoomEncounterType.Shop).front,"shop uses dedicated hooded merchant");yield return Capture("B"+act+"-Merchant");
        }
        var overlay=FindFirstObjectByType<DebugCheatOverlay>();overlay.SetOpen(true);yield return new WaitForEndOfFrame();Click(overlay.categoryButtons[4]);yield return new WaitForEndOfFrame();
        Check(stories.All(s=>overlay.roomObjectCatalog.Contains(s.worldObject)),"F1 includes all 19 live unique event subjects");
        yield return Capture("F1-Chapter");overlay.catalogScroll.StopMovement();overlay.catalogScroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();
        var tiles=overlay.catalogScroll.content.GetComponentsInChildren<DebugCatalogTile>();var last=overlay.roomObjectCatalog.Count-1;
        Click(tiles[last].button);Click(overlay.placeObjectButton);yield return new WaitForEndOfFrame();
        Check(flow.DebugInspectionArt==overlay.roomObjectCatalog[last]&&!overlay.IsOpen,"F1 scroll reaches final subject and actual place button");
        // Representative full choices: actual event UI handlers, result pages, then persistent room.
        foreach(var id in new[]{"AmberSeed","Contract","MagicTeacherFirst"})
        {
            var story=stories.Single(s=>s.id==id);Prepare(story);yield return new WaitForEndOfFrame();Click(view.slots.First(s=>s.gameObject.activeInHierarchy).button);yield return new WaitForEndOfFrame();
            var sequence=id=="AmberSeed"?new[]{1}:id=="Contract"?new[]{0}:new[]{0,1};
            foreach(var index in sequence){Click(expansion.ActiveChoices[index].button);yield return new WaitForEndOfFrame();}
            for(var i=0;i<12&&expansion.IsDecisionOpen;i++){Click(expansion.ActiveChoices[0].button);yield return new WaitForEndOfFrame();}
            Check(flow.State==GameFlowState.DungeonNavigation&&dungeon.State.Player.CurrentRoom.IsCleared,"full actual choice flow finishes: "+id);
            if(id=="MagicTeacherFirst"||id=="Contract")Check(view.VisibleObjects==0,"full choice removes departed/consumed subject: "+id);
            yield return Capture("Resolved-"+id);
        }
        flow.DebugJumpToChapter(1);yield return new WaitForEndOfFrame();
        File.WriteAllText(Folder+"Complete.txt",$"{checks} checks; {failures} failures. Actual Unity world render/UI raycasts, 19 events across 3 acts, first-facing/revisit transforms, all terminal-presence/narration states, 3 full dialogue choices, complete camping rooms.\n");
        UnityEditor.EditorApplication.isPlaying=false;
    }
}
#endif
