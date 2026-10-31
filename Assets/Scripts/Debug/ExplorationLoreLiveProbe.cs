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

public sealed class ExplorationLoreLiveProbe : MonoBehaviour
{
    const string Folder="Docs/Production/ExplorationLore-2026-11-01/";
    GameFlowController flow;RunController run;DungeonController dungeon;ExplorationView view;RunExpansionView expansion;StageBackdropView backdrop;CombatController combat;
    int checks,failures;bool background,hadHistory;string history;BaseInputModule[] modules;
    string logName="Live",completeName="Complete";
    static T Ref<T>(object source,string name)=>(T)source.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(source);
    static void Call(object source,string name,params object[] args)=>source.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(source,args);
    void Awake(){background=Application.runInBackground;Application.runInBackground=true;hadHistory=PlayerPrefs.HasKey("Behemoth.EventHistory.v1");history=PlayerPrefs.GetString("Behemoth.EventHistory.v1","");modules=EventSystem.current.GetComponents<BaseInputModule>().Where(m=>m.enabled).ToArray();foreach(var m in modules)m.enabled=false;}
    void OnDestroy(){Application.runInBackground=background;if(hadHistory)PlayerPrefs.SetString("Behemoth.EventHistory.v1",history);else PlayerPrefs.DeleteKey("Behemoth.EventHistory.v1");if(modules!=null)foreach(var m in modules)if(m!=null)m.enabled=true;}
    void Check(bool ok,string text){checks++;if(!ok)failures++;File.AppendAllText(Folder+logName+".txt",(ok?"PASS ":"FAIL ")+text+"\n");}
    bool Click(Button button)
    {
        Canvas.ForceUpdateCanvases();var canvas=button.GetComponentInParent<Canvas>().rootCanvas;
        var data=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,button.transform.TransformPoint(((RectTransform)button.transform).rect.center))};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
        var ok=hits.Count>0&&ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==button.gameObject&&button.IsInteractable();Check(ok,"pointer "+button.name+"; top="+(hits.Count==0?"none":hits[0].gameObject.name));if(!ok)return false;
        ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);return true;
    }
    IEnumerator Capture(string name,float delay=.16f){yield return new WaitForSecondsRealtime(delay);yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+"Screens/"+name+".jpg",t.EncodeToJPG(90));Destroy(t);}
    void Show()=>Call(flow,"ShowNavigation");
    void Prepare(RandomEventData story){flow.DebugPlaceRoomObject(story.worldObject,0,false);var room=dungeon.State.Player.CurrentRoom;room.ObjectPlacementPrepared=false;room.Story=story;room.StoryPrepared=true;room.IsCleared=false;Show();}
    RoomObjectSlot Slot(RoomObjectData data)=>view.slots.First(s=>s.gameObject.activeInHierarchy&&s.BoundData==data);
    bool OnMap(DungeonRoom room)=>Ref<List<GameObject>>(Ref<DungeonMapView>(dungeon,"mapView"),"activeViews").Any(v=>v!=null&&v.name.StartsWith("Map "+room.RoomId+" "));
    public void Run(){Directory.CreateDirectory(Folder+"Screens");if(File.Exists(Folder+"Complete.txt"))File.Delete(Folder+"Complete.txt");File.WriteAllText(Folder+"Live.txt","Scoped runtime: actual Canvas renders, EventSystem raycasts and changed interactions. No DPS or broad regression.\n");StartCoroutine(Guard(Review()));}
    public void RunFinishing(){logName="Followup";completeName="FollowupComplete";File.WriteAllText(Folder+logName+".txt","Only final fixes and unresolved art review: prayer, companion, stable interiors, entrance travel and event monster catalog.\n");StartCoroutine(Guard(Finishing()));}
    public void RunArtSides(){logName="ArtReview";completeName="ArtComplete";File.WriteAllText(Folder+logName+".txt","Scoped art only: newly painted world subjects, side/rear/mirrored views and the bandit corpse in all three acts.\n");StartCoroutine(Guard(ArtSides()));}
    public void RunEntryCombat(){logName="EntryCombat";completeName="EntryCombatComplete";File.WriteAllText(Folder+logName+".txt","Actual room travel and actual event fight choice: pre-entry furnishing yields correctly to combat input.\n");StartCoroutine(Guard(EntryCombat()));}
    IEnumerator Guard(IEnumerator routine)
    {
        while(true){object current;try{if(!routine.MoveNext())break;current=routine.Current;}catch(Exception e){Check(false,e.ToString());break;}yield return current;}
        File.WriteAllText(Folder+completeName+".txt",$"{checks} checks; {failures} failures. Scoped Unity runtime verification. See {logName}.txt for exact coverage.\n");UnityEditor.EditorApplication.isPlaying=false;
    }
    IEnumerator Review()
    {
        flow=FindFirstObjectByType<GameFlowController>();run=FindFirstObjectByType<RunController>();dungeon=FindFirstObjectByType<DungeonController>();view=FindFirstObjectByType<ExplorationView>();backdrop=FindFirstObjectByType<StageBackdropView>();combat=FindFirstObjectByType<CombatController>();expansion=Ref<RunExpansionView>(flow,"expansionView");
        flow.StartNewRun();yield return new WaitForEndOfFrame();var content=Ref<GameContent>(flow,"content");var stories=content.events.Where(e=>e!=null&&!e.excludedFromRandomPool).ToArray();
        var persons=stories.Where(s=>s.encounterKind==EventEncounterKind.Person).ToArray();Check(persons.Length==10,"10 authored human event identities, weighted less frequently");
        foreach(var act in new[]{1,2,3})
        {
            flow.DebugJumpToChapter(act);yield return new WaitForEndOfFrame();
            foreach(var story in persons.Concat(stories.Where(s=>s.roomLayout==EventRoomLayout.Alcove||s.id=="RecoveryCutpurse")))
            {
                Prepare(story);yield return new WaitForEndOfFrame();var slot=Slot(story.worldObject);Check(slot.painting.sprite==story.worldObject.front,$"B{act}/{story.id} first face");
                Check(story.roomLayout!=EventRoomLayout.Alcove||dungeon.State.Player.CurrentRoom.Connections.Count==1&&backdrop.VisibleDoorMask==0,$"B{act}/{story.id} layout valid");
                yield return Capture($"B{act}-{story.id}");Click(slot.button);yield return new WaitForEndOfFrame();Check(expansion.IsDecisionOpen,$"B{act}/{story.id} correct event opens");if(act==1)yield return Capture("Popup-"+story.id);
                var options=story.pages.Count==0?story.options:story.pages.SelectMany(p=>p.options).ToList();
                foreach(var option in options.Where(o=>o.nextPage<0))
                {
                    Prepare(story);var room=dungeon.State.Player.CurrentRoom;room.IsCleared=true;Call(flow,"ApplyRoomObjectOutcome",room,option);Show();yield return new WaitForEndOfFrame();
                    var remaining=view.slots.FirstOrDefault(s=>s.gameObject.activeInHierarchy&&s.BoundData==story.worldObject);Check(option.removeWorldObject?remaining==null:remaining!=null,$"{story.id}/{option.title} aftermath presence");
                    if(remaining!=null){Check(remaining.painting.sprite==story.worldObject.Painting(0,option.useWorldObject),"matching resolved painting");Click(remaining.button);yield return new WaitForEndOfFrame();Check(!expansion.IsDecisionOpen&&view.narrationText.text==GameText.Get(option.revisitTextKey),"resolved narration without another event");}
                }
            }
            foreach(var door in new[]{dungeon.Exploration.eliteEntrance,dungeon.Exploration.bossEntrance})foreach(var facing in new[]{0,1,3})
            {flow.DebugPlaceRoomObject(door,facing,false);yield return new WaitForEndOfFrame();Check(Slot(door)!=null,$"B{act}/{door.name}/{facing} full painted entrance");yield return Capture($"B{act}-{door.name}-{facing}");}
            flow.DebugPlaceRoomObject(dungeon.Exploration.bossEntrance,0,false);Click(Slot(dungeon.Exploration.bossEntrance).button);yield return null;
            Check(backdrop.background.sprite==backdrop.artwork[act-1].bossInterior,$"B{act} boss interior during entry");yield return Capture($"B{act}-BossInterior",.08f);
            flow.DebugPlaceRoomObject(dungeon.Exploration.For(RoomEncounterType.Shop),0,false);yield return new WaitForEndOfFrame();yield return Capture($"B{act}-Merchant");
            run.State.GainGold(500);var stock=run.GetShopInventory(dungeon.State.Seed,dungeon.State.Player.CurrentRoom.RoomId);Call(flow,"ShowShop",dungeon.State.Player.CurrentRoom);yield return new WaitForEndOfFrame();var shop=Ref<ShopView>(flow,"shopView");
            Check(stock.Count>0&&stock.Count<=6*act,$"B{act} merged stable stock {stock.Count}");yield return Capture($"B{act}-Shop-1");
            var initial=Ref<List<ShopItemView>>(shop,"items").First().Data;
            if(act>1){Check(shop.nextPage.gameObject.activeInHierarchy,"next stock control visible");Click(shop.nextPage);yield return new WaitForEndOfFrame();Check(Ref<List<ShopItemView>>(shop,"items").First().Data!=initial,"next stock page changes goods");yield return Capture($"B{act}-Shop-2");Click(shop.previousPage);yield return new WaitForEndOfFrame();Check(Ref<List<ShopItemView>>(shop,"items").First().Data==initial,"previous page restores same goods");}
            var purchase=shop.purchase;if(purchase.IsInteractable()){Click(purchase);yield return new WaitForEndOfFrame();Check(ReferenceEquals(stock,run.GetShopInventory(dungeon.State.Seed,dungeon.State.Player.CurrentRoom.RoomId)),"purchase does not refill or reshuffle stock");}
            flow.DebugJumpToChapter(act);
            // Locked passage: visible immediately, inaccessible before key; key then door.
            flow.DebugPlaceRoomObject(dungeon.Exploration.lockedDoor,0,false);yield return new WaitForEndOfFrame();var locked=dungeon.State.Graph.Passages.Single();Check(locked.Discovered&&locked.Locked&&!OnMap(locked.Annex),"locked leaf visible while map excludes annex");
            yield return Capture($"B{act}-Locked-Before");Click(Slot(dungeon.Exploration.lockedDoor).button);Check(locked.Locked,"door cannot open without picking up key");Click(Slot(dungeon.Exploration.key).button);yield return new WaitForEndOfFrame();Click(Slot(dungeon.Exploration.lockedDoor).button);yield return new WaitForEndOfFrame();Check(!locked.Locked&&OnMap(locked.Annex)&&dungeon.State.CanTravel(locked.Origin,locked.Annex),"key opens visible locked door and only then shows route");
            flow.DebugPlaceRoomObject(dungeon.Exploration.hiddenWall,0,false);yield return new WaitForEndOfFrame();var hidden=dungeon.State.Graph.Passages.Single();Check(!hidden.Locked&&!hidden.Discovered&&!OnMap(hidden.Annex),"secret masonry has no lock and no map route");Check(!Slot(dungeon.Exploration.hiddenWall).painting.enabled,"secret is unpainted original masonry");yield return Capture($"B{act}-Secret-Before");
            for(var touch=1;touch<=5;touch++){Click(Slot(dungeon.Exploration.hiddenWall).button);yield return new WaitForEndOfFrame();Check(hidden.Discovered==(touch==5),"secret discovery touch "+touch);if(touch<5)Check(!OnMap(hidden.Annex),"unrevealed route absent from map");}
            Check(!hidden.Locked&&OnMap(hidden.Annex)&&dungeon.State.CanTravel(hidden.Origin,hidden.Annex),"fifth search opens directly without secondary lock");yield return Capture($"B{act}-Secret-Opened");
        }
        // Real travel, including the walking beat, to a populated destination.
        flow.DebugJumpToChapter(1);flow.DebugClearInspectionRoom();var target=dungeon.State.Graph.At(new DungeonPosition(0,1));var npc=stories.Single(s=>s.id=="StormBottle");target.Type=RoomEncounterType.Event;target.IsCleared=false;target.Story=npc;target.StoryPrepared=true;
        dungeon.TravelThrough(FacingDirection.North);yield return null;Check(Ref<RectTransform>(Ref<RoomTransitionView>(flow,"roomTransitionView"),"walkBackdrop").gameObject.activeInHierarchy&&Slot(npc.worldObject)!=null,"destination object is visible during active entry beat");yield return Capture("Entry-Object-Already-Present",.06f);yield return new WaitForSecondsRealtime(1);
        // Prayer can be declined and opened again; accepting is the only consumption.
        flow.DebugPlaceRoomObject(dungeon.Exploration.For(RoomEncounterType.Goddess),0,false);yield return new WaitForEndOfFrame();var goddess=dungeon.State.Player.CurrentRoom;var eventView=Ref<RoomEventView>(flow,"roomEventView");Click(Slot(dungeon.Exploration.For(RoomEncounterType.Goddess)).button);yield return new WaitForEndOfFrame();Check(Ref<TMPro.TextMeshProUGUI>(eventView,"message").text==GameText.Get("ui.explore.goddess.ask"),"explicit prayer question");yield return Capture("Goddess-Question");Click(Ref<Button>(eventView,"noButton"));yield return new WaitForEndOfFrame();Check(!goddess.IsCleared&&flow.State==GameFlowState.DungeonNavigation,"declining leaves goddess unused");Click(Slot(dungeon.Exploration.For(RoomEncounterType.Goddess)).button);yield return new WaitForEndOfFrame();Click(Ref<Button>(eventView,"yesButton"));yield return new WaitForEndOfFrame();Click(Ref<Button>(eventView,"yesButton"));yield return new WaitForEndOfFrame();Check(goddess.IsCleared,"accepted prayer consumes once");
        foreach(var id in new[]{"Shelter","QuietPrayer","MagicTeacherFirst"})
        {
            var story=stories.Single(s=>s.id==id);Prepare(story);yield return new WaitForEndOfFrame();Click(Slot(story.worldObject).button);yield return new WaitForEndOfFrame();
            if(id=="MagicTeacherFirst"){Click(expansion.ActiveChoices[0].button);yield return new WaitForEndOfFrame();}
            var before=run.State.Gold;Click(expansion.ActiveChoices[1].button);yield return new WaitForEndOfFrame();yield return Capture("Aggressive-"+id);
            for(var count=0;count<8&&expansion.IsDecisionOpen;count++){Click(expansion.ActiveChoices[0].button);yield return new WaitForEndOfFrame();}
            Check(dungeon.State.Player.CurrentRoom.IsCleared&&flow.State==GameFlowState.DungeonNavigation,"actual dishonorable choice resolves: "+id);
            if(id=="MagicTeacherFirst")Check(run.State.EventFlags.Contains("RobbedFieldScholar")&&run.State.Gold>=before+25,"coerced scholar payment and future teaching flag");
        }
        // Render actual combat actors without running a balance suite.
        var bandit=UnityEditor.AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/Combat/Monsters/Data_Monster_Special_회수꾼도적.asset");
        foreach(var monster in new[]{bandit,combat.MerchantMutant})
        {
            flow.DebugJumpToChapter(1);combat.PrepareEncounter(run.State,new[]{monster},1);combat.BeginCombatPresentation();view.SetVisible(false);dungeon.HideNavigation();yield return new WaitForSecondsRealtime(.5f);
            var member=Ref<IList>(combat,"activeMonsters")[0];var runtime=(MonsterRuntime)member.GetType().GetProperty("Runtime").GetValue(member);var actor=(MonsterView)member.GetType().GetProperty("View").GetValue(member);var body=Ref<Image>(actor,"body");
            foreach(var state in new[]{CombatVisualState.Idle,CombatVisualState.Ready,CombatVisualState.Attack,CombatVisualState.Dead})
            {if(state==CombatVisualState.Ready){runtime.StartCharge((Vector2)member.GetType().GetProperty("Position").GetValue(member));actor.Bind(runtime);Call(combat,"SyncWeakPointViews");}else if(state!=CombatVisualState.Idle){runtime.CompleteNormalAttack();Call(combat,"SyncWeakPointViews");}body.GetComponent<CombatSpriteAnimator>().Play(monster.combatAnimation,state,true);Check(body.sprite==monster.combatAnimation.For(state),monster.displayName+" distinct "+state);yield return Capture("Combat-"+monster.displayName+"-"+state);}
        }
        flow.DebugJumpToChapter(1);var dog=UnityEditor.AssetDatabase.LoadAssetAtPath<CompanionData>("Assets/Data/Characters/Companions/Data_Companion_길동무.asset");combat.PrepareEncounter(run.State,new[]{bandit},1);combat.BeginCombatPresentation();view.SetVisible(false);dungeon.HideNavigation();
        var puppy=Instantiate(dog.prefab,Ref<Transform>(combat,"playerSpawnRoot"));puppy.Configure(dog);yield return Capture("Dog-Idle",.5f);puppy.Attack(Vector2.zero);yield return Capture("Dog-Attack",.04f);Check(puppy.body.GetComponent<Image>().sprite==dog.combatAnimation.attack,"companion uses matching new attack painting");Destroy(puppy.gameObject);
        flow.DebugJumpToChapter(1);var overlay=FindFirstObjectByType<DebugCheatOverlay>();overlay.SetOpen(true);yield return new WaitForEndOfFrame();Click(overlay.categoryButtons[4]);yield return new WaitForEndOfFrame();Check(stories.All(s=>overlay.roomObjectCatalog.Contains(s.worldObject))&&overlay.roomObjectCatalog.Contains(dungeon.Exploration.hiddenWall)&&overlay.roomObjectCatalog.Contains(dungeon.Exploration.bossEntrance),"F1 includes all live subjects and new entrances");yield return Capture("F1-Chapter");overlay.SetOpen(false);
    }
    IEnumerator Finishing()
    {
        flow=FindFirstObjectByType<GameFlowController>();run=FindFirstObjectByType<RunController>();dungeon=FindFirstObjectByType<DungeonController>();view=FindFirstObjectByType<ExplorationView>();backdrop=FindFirstObjectByType<StageBackdropView>();combat=FindFirstObjectByType<CombatController>();expansion=Ref<RunExpansionView>(flow,"expansionView");
        flow.StartNewRun();yield return new WaitForEndOfFrame();var content=Ref<GameContent>(flow,"content");
        foreach(var act in new[]{1,2,3})
        {
            flow.DebugJumpToChapter(act);flow.DebugPlaceRoomObject(dungeon.Exploration.bossEntrance,0,false);yield return new WaitForEndOfFrame();Click(Slot(dungeon.Exploration.bossEntrance).button);yield return Capture($"B{act}-BossInterior-Final",.42f);Check(backdrop.background.sprite==backdrop.artwork[act-1].bossInterior&&backdrop.VisibleDoorMask==0,"boss chamber after real travel B"+act);
            foreach(var story in content.events.Where(e=>e.roomLayout==EventRoomLayout.Alcove&&!e.excludedFromRandomPool))
            {Prepare(story);yield return Capture($"B{act}-{story.id}-Final",.4f);Check(backdrop.background.sprite==story.roomInteriors[act-1]&&backdrop.VisibleDoorMask==0,"stable authored alcove "+story.id);}
            flow.DebugPlaceRoomObject(dungeon.Exploration.For(RoomEncounterType.Shop),0,false);run.State.GainGold(500);Call(flow,"ShowShop",dungeon.State.Player.CurrentRoom);yield return new WaitForEndOfFrame();var shop=Ref<ShopView>(flow,"shopView");
            if(act>1)
            {
                var parent=(RectTransform)Ref<GameObject>(shop,"panel").transform;var pageBounds=RectTransformUtility.CalculateRelativeRectTransformBounds(parent,shop.nextPage.transform);var buyBounds=RectTransformUtility.CalculateRelativeRectTransformBounds(parent,shop.purchase.transform);Check(!pageBounds.Intersects(buyBounds),"stock paging clear of purchase button");
                for(var page=1;page<=act;page++){yield return Capture($"B{act}-Shop-{page}-Final",.35f);if(page<act){Click(shop.nextPage);yield return new WaitForEndOfFrame();}}
                Check(!shop.nextPage.interactable&&shop.previousPage.interactable,"last stock page boundaries");Click(shop.previousPage);yield return new WaitForEndOfFrame();Check(shop.nextPage.interactable,"stock previous button works");
            }
        }
        flow.DebugJumpToChapter(1);var npc=content.events.Single(e=>e.id=="StormBottle");flow.DebugClearInspectionRoom();var destination=dungeon.State.Graph.At(new DungeonPosition(0,1));destination.Type=RoomEncounterType.Event;destination.IsCleared=false;destination.Story=npc;destination.StoryPrepared=true;dungeon.TravelThrough(FacingDirection.North);yield return Capture("Entry-Object-Already-Present-Final",.42f);Check(Ref<RectTransform>(Ref<RoomTransitionView>(flow,"roomTransitionView"),"walkBackdrop").gameObject.activeInHierarchy&&Slot(npc.worldObject)!=null,"object remains visible during walk after dither clears");yield return new WaitForSecondsRealtime(.5f);
        foreach(var act in new[]{1,2,3})
        {
            flow.DebugJumpToChapter(act);flow.DebugPlaceRoomObject(dungeon.Exploration.For(RoomEncounterType.Goddess),0,false);yield return new WaitForEndOfFrame();var room=dungeon.State.Player.CurrentRoom;var art=dungeon.Exploration.For(RoomEncounterType.Goddess);var modal=Ref<RoomEventView>(flow,"roomEventView");
            Click(Slot(art).button);yield return new WaitForEndOfFrame();Check(Ref<TMPro.TextMeshProUGUI>(modal,"message").text==GameText.Get("ui.explore.goddess.ask"),"prayer question B"+act);Click(Ref<Button>(modal,"noButton"));yield return new WaitForEndOfFrame();Check(!room.IsCleared,"decline preserves unused statue");
            Click(Slot(art).button);yield return new WaitForEndOfFrame();Click(Ref<Button>(modal,"yesButton"));yield return new WaitForEndOfFrame();Click(Ref<Button>(modal,"yesButton"));yield return new WaitForEndOfFrame();Check(room.IsCleared,"accepted prayer consumes statue B"+act);
            // Leave reward selection via chapter reset; then verify the resolved statue interaction.
            Call(flow,"HideTransientViews");Show();yield return new WaitForEndOfFrame();Click(Slot(art).button);yield return new WaitForEndOfFrame();Check(flow.State==GameFlowState.DungeonNavigation&&!expansion.IsDecisionOpen&&!Ref<GameObject>(modal,"panel").activeSelf,"used statue click only narrates");
        }
        flow.DebugJumpToChapter(1);var bandit=UnityEditor.AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/Combat/Monsters/Data_Monster_Special_회수꾼도적.asset");Check(combat.GetDebugMonsterCatalog().Contains(bandit),"event monster accessible in F1 catalog");
        var dog=UnityEditor.AssetDatabase.LoadAssetAtPath<CompanionData>("Assets/Data/Characters/Companions/Data_Companion_길동무.asset");var dogRelic=content.equipment.First(e=>e!=null&&e.abilities.SelectMany(a=>a.effects).Any(f=>f.effectType==EffectType.SummonCompanion&&f.companion==dog));Check(run.TryGrantRelic(dogRelic),"actual dog companion relic granted");combat.PrepareEncounter(run.State,new[]{bandit},1);combat.BeginCombatPresentation();view.SetVisible(false);dungeon.HideNavigation();yield return new WaitForSecondsRealtime(.5f);
        var puppy=Ref<Dictionary<CompanionData,CompanionView>>(combat,"companionViews")[dog];var body=puppy.body.GetComponent<Image>();Check(body.enabled&&body.sprite==dog.combatAnimation.idle,"real owned companion renders idle");
        var corners=new Vector3[4];puppy.body.GetWorldCorners(corners);var camera=body.GetComponentInParent<Canvas>().worldCamera;var low=RectTransformUtility.WorldToScreenPoint(camera,corners[0]);var high=RectTransformUtility.WorldToScreenPoint(camera,corners[2]);Check(low.x>=0&&low.y>=0&&high.x<=Screen.width&&high.y<=Screen.height,$"companion complete screen bounds {low}..{high}");yield return Capture("Dog-Idle-Final",.4f);puppy.Attack(Vector2.zero);yield return Capture("Dog-Attack-Final",.03f);Check(body.sprite==dog.combatAnimation.attack,"real owned companion distinct attack painting");
        flow.DebugJumpToChapter(1);
    }
    IEnumerator ArtSides()
    {
        flow=FindFirstObjectByType<GameFlowController>();run=FindFirstObjectByType<RunController>();dungeon=FindFirstObjectByType<DungeonController>();view=FindFirstObjectByType<ExplorationView>();backdrop=FindFirstObjectByType<StageBackdropView>();
        flow.StartNewRun();yield return new WaitForEndOfFrame();var ids=new[]{"StormBottle","Investment","Pilgrim","MagicTeacherFirst","MagicTeacherReturn","Shelter","RecoveryCutpurse"};var stories=Ref<GameContent>(flow,"content").events;
        foreach(var act in new[]{1,2,3})
        {
            flow.DebugJumpToChapter(act);
            foreach(var id in ids)foreach(var side in new[]{1,2,3})
            {
                var story=stories.Single(s=>s.id==id);flow.DebugPlaceRoomObject(story.worldObject,side,false);yield return new WaitForEndOfFrame();
                Check(Slot(story.worldObject).painting.sprite==story.worldObject.Painting(side,false),$"B{act}/{id} authored view {side}");yield return Capture($"Art-B{act}-{id}-{side}");
            }
            var bandit=stories.Single(s=>s.id=="RecoveryCutpurse");flow.DebugPlaceRoomObject(bandit.worldObject,0,true);yield return new WaitForEndOfFrame();Check(Slot(bandit.worldObject).painting.sprite==bandit.worldObject.usedFront,"bandit corpse after actual outcome primitive");yield return Capture($"Art-B{act}-Bandit-Corpse");
        }
        flow.DebugJumpToChapter(1);
    }
    IEnumerator EntryCombat()
    {
        flow=FindFirstObjectByType<GameFlowController>();run=FindFirstObjectByType<RunController>();dungeon=FindFirstObjectByType<DungeonController>();view=FindFirstObjectByType<ExplorationView>();combat=FindFirstObjectByType<CombatController>();expansion=Ref<RunExpansionView>(flow,"expansionView");
        flow.StartNewRun();yield return new WaitForEndOfFrame();flow.DebugPlaceRoomObject(dungeon.Exploration.eliteEntrance,0,false);yield return new WaitForEndOfFrame();var destination=dungeon.State.Graph.At(new DungeonPosition(0,1));destination.KeyTokens.Add(999);
        Click(Slot(dungeon.Exploration.eliteEntrance).button);yield return new WaitForSecondsRealtime(.4f);Check(view.slots.Any(s=>s.gameObject.activeInHierarchy&&s.BoundData==dungeon.Exploration.key),"destination key visible during walking");yield return new WaitForSecondsRealtime(.55f);
        Check(combat.HasPreparedMonsters&&!view.objectLayer.gameObject.activeSelf&&!view.interactionLayer.gameObject.activeSelf&&view.combatInputSurface.raycastTarget,"actual elite entry releases world hit areas to combat gestures");yield return new WaitForSecondsRealtime(1.3f);
        if(expansion.IsDecisionOpen&&expansion.modalTitle.text==GameText.Get("ui.tutorial.combat.title")){Check(!combat.IsActive,"existing combat tutorial safely waits for confirmation");Click(expansion.ActiveChoices[0].button);yield return new WaitForEndOfFrame();}
        yield return Capture("Entry-Combat-Input-Final",.2f);Check(flow.State==GameFlowState.Combat&&combat.IsActive,"actual elite combat starts");
        flow.DebugJumpToChapter(1);var story=Ref<GameContent>(flow,"content").events.Single(e=>e.id=="RecoveryCutpurse");flow.DebugClearInspectionRoom();destination=dungeon.State.Graph.At(new DungeonPosition(0,1));destination.Type=RoomEncounterType.Event;destination.IsCleared=false;destination.Story=story;destination.StoryPrepared=true;dungeon.TravelThrough(FacingDirection.North);yield return new WaitForSecondsRealtime(.95f);Check(expansion.IsDecisionOpen&&view.objectLayer.gameObject.activeSelf,"automatic hostile encounter begins after actual entry with its world subject");Click(expansion.ActiveChoices[0].button);yield return new WaitForEndOfFrame();
        Check(combat.HasPreparedMonsters&&combat.Enemies.Single().Data==story.options[0].effects.Single(e=>e.kind==EventEffectKind.Fight).encounter.members[0].monster,"actual hostile choice prepares the authored bandit encounter");Check(!view.objectLayer.gameObject.activeSelf&&!view.interactionLayer.gameObject.activeSelf&&view.combatInputSurface.raycastTarget,"event fight removes duplicate world figure and world hit areas");Click(expansion.ActiveChoices[0].button);yield return Capture("Event-Bandit-Combat-Final",.45f);Check(flow.State==GameFlowState.Combat&&combat.IsActive,"actual bandit combat starts after confirmation");
        flow.DebugJumpToChapter(1);
    }
}
#endif
