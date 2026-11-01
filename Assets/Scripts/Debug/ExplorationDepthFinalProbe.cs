#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed partial class ExplorationDepthLiveProbe
    {
        public void RunFinal(string mode="final")
        {reportPrefix=mode=="final-input"?"FinalInput":mode=="final-art"?"FinalArt":"FinalRuntime";StartCoroutine(Guarded(FinalReview(mode)));}
        Rect ScreenRect(RectTransform rect,Rect? portion=null)
        {
            var canvas=rect.GetComponentInParent<Canvas>().rootCanvas;var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            var bounds=rect.rect;if(portion.HasValue){var p=portion.Value;bounds=new Rect(bounds.min+Vector2.Scale(bounds.size,p.min),Vector2.Scale(bounds.size,p.size));}
            var a=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(bounds.min));var b=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(bounds.max));return Rect.MinMaxRect(a.x,a.y,b.x,b.y);
        }
        void ClickAt(RectTransform rect,Vector2 uv)
        {
            Canvas.ForceUpdateCanvases();var canvas=rect.GetComponentInParent<Canvas>().rootCanvas;
            var local=rect.rect.min+Vector2.Scale(rect.rect.size,uv);var point=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rect.TransformPoint(local));
            var data=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            var target=hits.Count==0?null:ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);Require(target!=null,"world-space pointer target at "+point);
            ExecuteEvents.Execute(target,data,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(target,data,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(target,data,ExecuteEvents.pointerClickHandler);
        }
        IEnumerator FinalReview(string mode)
        {
            flow=FindFirstObjectByType<GameFlowController>();dungeon=FindFirstObjectByType<DungeonController>();run=FindFirstObjectByType<RunController>();combat=FindFirstObjectByType<CombatController>();world=FindFirstObjectByType<ExplorationView>();backdrop=FindFirstObjectByType<StageBackdropView>();records=FindFirstObjectByType<PlayRecordView>();expansion=FindFirstObjectByType<RunExpansionView>();
            yield return Settle(.85f);yield return CloseMessages();var content=Ref<GameContent>(flow,"content");
            Check(content.events.Count(e=>e!=null&&!e.excludedFromRandomPool)==19,"19 active encounters; mirror and rejected trap prototype excluded");
            Check(content.events.Where(e=>e!=null&&(e.id=="Mirror"||e.id=="SalvageTrap")).All(e=>e.excludedFromRandomPool),"retired premises cannot be drawn");
            var dogRelic=content.events.Single(e=>e.id=="StrayDog").pages.SelectMany(p=>p.options).SelectMany(o=>o.effects).First(e=>e.kind==EventEffectKind.Relic).relic;
            run.TryGrantRelic(dogRelic);var dogData=run.State.Companions.First();
            if(mode!="final-input")
            {
                for(var act=1;act<=3;act++)
                {
                    flow.DebugJumpToChapter(act);flow.DebugClearInspectionRoom();yield return Settle(.4f);
                    var dog=FindFirstObjectByType<CompanionView>();Require(dog!=null,"owned mature hound B"+act);
                    var navFoot=ScreenRect((RectTransform)dog.transform).center;var body=dog.body.GetComponent<Image>();
                    Check(body.sprite==dogData.oppositeAnimation.idle,"right-side hound uses separately painted inward-facing idle");
                    Check(navFoot.x>Screen.width*.70f&&navFoot.y>0&&navFoot.y<Screen.height*.2f,"hound support is on right foreground floor");
                    yield return Shot("B"+act+"-Hound-Navigation");Click(dog.body.GetComponent<Button>());yield return Settle();Check(world.narrationText.text==GameText.Get(dogData.narrationKey),"hound click speaks");
                    Require(flow.StartCheatBattle(combat.GetDebugMonsterCatalog().First(m=>m.name.Contains("B1"))),"companion framing fixture");yield return WaitCombat();
                    var combatFoot=ScreenRect((RectTransform)dog.transform).center;Check(Mathf.Abs(navFoot.y-combatFoot.y)<3,"ground height persists across combat framing");
                    var gauges=FindObjectsByType<RectTransform>(FindObjectsSortMode.None).Single(t=>t.name=="PlayerStatusGauges");
                    var dogBounds=ScreenRect(dog.body,dogData.Geometry(body.sprite).bounds);
                    var overlap=gauges.GetComponentsInChildren<Graphic>().Where(g=>g.enabled&&g.gameObject.activeInHierarchy).Sum(g=>
                    {var r=ScreenRect(g.rectTransform);return Mathf.Max(0,Mathf.Min(r.xMax,dogBounds.xMax)-Mathf.Max(r.xMin,dogBounds.xMin))*Mathf.Max(0,Mathf.Min(r.yMax,dogBounds.yMax)-Mathf.Max(r.yMin,dogBounds.yMin));});
                    Check(overlap<1,"hound is clear of actual combat gauges");yield return Shot("B"+act+"-Hound-Combat-Idle");dog.Attack(Vector2.left);yield return Shot("B"+act+"-Hound-Combat-Attack");Check(body.sprite==dogData.oppositeAnimation.attack,"authored inward-facing attack");
                    flow.DebugJumpToChapter(act);yield return Settle();
                    foreach(var art in new[]{dungeon.Exploration.eliteEntrance,dungeon.Exploration.bossEntrance})foreach(var view in new[]{0,1,3})
                    {
                        flow.DebugPlaceRoomObject(art,view,false);yield return Settle();var slot=Slot(art);
                        var ground=world.Wall(view==0?RelativeDirection.Forward:view==1?RelativeDirection.Left:RelativeDirection.Right);
                        var floor=backdrop.artwork[act-1];var c=floor.openingContours[view==0?0:view==1?1:2];
                        var picture=slot.GetComponentInChildren<RoomOpeningPainting>();Check(picture!=null&&picture.gameObject.activeInHierarchy,"complete projected frame B"+act+"/"+view);
                        Check(!backdrop.doorLayers[view==0?0:view==1?1:2].enabled,"complete facade suppresses ordinary arch layer");
                        var rb=(RectTransform)slot.transform;var min=rb.anchorMin.y-rb.rect.height/world.objectLayer.rect.height*.5f;
                        Check(min<=ground.y+.01f,"frame extends down to physical wall-floor support B"+act+"/"+view);
                        yield return Shot("B"+act+"-"+(art==dungeon.Exploration.bossEntrance?"Boss":"Elite")+"-"+view);
                        if(view==0&&(act==1&&art==dungeon.Exploration.eliteEntrance||act==3&&art==dungeon.Exploration.bossEntrance))
                        {
                            ClickAt(slot.hitArea,new Vector2(.17f,.38f));yield return Settle(.42f);
                            Check(dungeon.State.Player.CurrentRoom.Type==(art==dungeon.Exploration.bossEntrance?RoomEncounterType.Boss:RoomEncounterType.Elite),"actual entrance click travels to matching encounter");
                            if(art==dungeon.Exploration.bossEntrance){Check(backdrop.background.sprite==floor.bossInterior,"boss interior appears during real entry");yield return Shot("Boss-Actual-Entry");}
                            flow.DebugJumpToChapter(act);yield return Settle();
                        }
                    }
                    foreach(var id in new[]{"Investment","StormBottle","Tooth","MagicTeacherFirst"})foreach(var view in new[]{0,1})
                    {var story=content.events.Single(e=>e.id==id);flow.DebugPlaceRoomObject(story.worldObject,view,false);yield return Shot("B"+act+"-"+id+"-"+view);}
                }
            }
            if(mode!="final-art")
            {
                flow.DebugJumpToChapter(1);var font=dungeon.Exploration.For(RoomEncounterType.Fountain);
                flow.DebugPlaceRoomObject(font,2,false);yield return Settle(.3f);var dog=FindFirstObjectByType<CompanionView>();
                Check(dog.body.GetComponent<Image>().sprite==dogData.combatAnimation.idle,"nearby right-floor prop reserves the opposite support for companion");yield return Shot("Hound-Prop-Clearance");
                var mage=content.events.Single(e=>e.id=="MagicTeacherFirst");flow.DebugPlaceRoomObject(mage.worldObject,0,false);
                dungeon.State.Graph.At(new DungeonPosition(0,1)).Type=RoomEncounterType.Boss;Navigation();yield return Settle();
                ClickAt(Slot(mage.worldObject).hitArea,new Vector2(.68f,.68f));yield return Settle();Check(flow.State==GameFlowState.RoomEvent&&expansion.modalNarrative.text==mage.pages[0].narrative,"foreground NPC wins pointer ordering in front of larger doorway");yield return Shot("NPC-Entrance-Interaction");
                flow.DebugJumpToChapter(1);flow.DebugPlaceRoomObject(font,0,true);yield return Settle();
                Click(expansion.returnButton);yield return Settle();var open=expansion.ActiveChoices.First(c=>c.button.gameObject.activeInHierarchy&&c.title.text.Contains(GameText.Get("ui.playrecord.open")));Click(open.button);yield return Settle();
                Check(records.panel.activeInHierarchy&&!records.detail.text.Contains(GameText.Get("ui.playrecord.pending")),"menu opens recording UI and non-event rooms are not marked unentered events");
                Check(records.codeInput.textComponent.font.name.StartsWith("LiberationSans"),"play code uses readable ASCII face rather than the gameplay calligraphy");
                Click(records.copyButton);yield return Settle();Click(records.pasteButton);yield return Settle();Click(records.importButton);yield return Settle();yield return Shot("Play-Code-UI");
                var original=dungeon.State;Click(records.previewButton);yield return Settle();Check(flow.IsPlayRecordInspection&&Slot(font).painting.sprite==font.usedFront,"record preview uses actual consumed fountain state");Click(records.leavePreview);yield return Settle();Check(ReferenceEquals(original,dungeon.State),"new rendering/query controls preserve live run");
                yield return WalkSecretChain();
            }
        }
        IEnumerator WalkSecretChain()
        {
            DungeonPassage hidden=null;DungeonRoom terminal=null;
            for(var i=0;i<90;i++)
            {
                RunSession.ForcedSeed=7117+i*185917;flow.DebugJumpToChapter(2);hidden=dungeon.State.Graph.Passages.FirstOrDefault(p=>p.Concealed);if(hidden==null)continue;
                var found=new List<DungeonRoom>();var at=hidden.Annex;var previous=hidden.Origin;
                while(true){found.Add(at);var next=dungeon.State.Graph.Neighbors(at).FirstOrDefault(n=>n!=previous);if(next==null)break;previous=at;at=next;}
                if(found.Count==3&&found.Last().Type==RoomEncounterType.Chest&&found.Take(2).All(r=>r.Type==RoomEncounterType.Empty)){terminal=found.Last();break;}
            }
            Require(hidden!=null&&terminal!=null,"natural seeded two-room secret corridor ending in chest");At(hidden.Origin);yield return Settle();
            var map=Ref<DungeonMapView>(dungeon,"mapView");
            Check(!Ref<List<GameObject>>(map,"activeViews").Any(v=>v!=null&&v.name.StartsWith("Map "+hidden.Annex.RoomId+" ")),"closed secret branch is absent from accessible map");
            for(var i=0;i<3;i++){Click(Slot(dungeon.Exploration.hiddenWall).button);yield return Settle();}
            var current=hidden.Origin;var nextRoom=hidden.Annex;
            while(true)
            {
                var absolute=DirectionUtility.FromDelta(new DungeonPosition(nextRoom.Position.X-current.Position.X,nextRoom.Position.Y-current.Position.Y));var relative=DirectionUtility.ToRelative(dungeon.State.Player.Facing,absolute);
                var navigation=Ref<NavigationChoiceView>(dungeon,"navigation");var choices=Ref<List<RoomChoiceButton>>(navigation,"choices");Click(Ref<Button>(choices[(int)relative],"button"));yield return Settle(.9f);
                Check(dungeon.State.Player.CurrentRoom==nextRoom,"actual input traverses secret corridor room "+nextRoom.RoomId);
                if(nextRoom==terminal)break;var previous=current;current=nextRoom;nextRoom=dungeon.State.Graph.Neighbors(current).First(r=>r!=previous);
            }
            Check(world.slots.Any(s=>s.gameObject.activeInHierarchy&&s.BoundData==dungeon.Exploration.For(RoomEncounterType.Chest)),"terminal reward is furnished and interactive");yield return Shot("Secret-Corridor-Terminal");
            var record=flow.CapturePlayRecord();Check(record.floors.Last().trail.Count>=4,"play record preserves actual secret traversal order");
            System.IO.File.WriteAllText(Folder+"NaturalSecretPlayCode.txt",flow.CopyPlayCode());
        }
    }
}
#endif
