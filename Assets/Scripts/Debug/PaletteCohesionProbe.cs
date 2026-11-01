#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed partial class ExplorationDepthLiveProbe
    {
        public void RunPaletteCohesion(bool companionOnly=false,bool propsOnly=false)
        {outputFolder="Docs/Production/PaletteCohesion-2026-11-01/";reportPrefix=propsOnly?"Props":companionOnly?"Companion":"Palette";StartCoroutine(Guarded(PaletteCohesion(companionOnly,propsOnly)));}
        IEnumerator PaletteCohesion(bool companionOnly,bool propsOnly)
        {
            flow=FindFirstObjectByType<GameFlowController>();dungeon=FindFirstObjectByType<DungeonController>();run=FindFirstObjectByType<RunController>();
            combat=FindFirstObjectByType<CombatController>();world=FindFirstObjectByType<ExplorationView>();backdrop=FindFirstObjectByType<StageBackdropView>();
            expansion=FindFirstObjectByType<RunExpansionView>();yield return Settle(.9f);yield return CloseMessages();
            var content=Ref<GameContent>(flow,"content");
            var dogStory=content.events.Single(e=>e.id=="StrayDog");
            var dogRelic=dogStory.pages.SelectMany(p=>p.options).SelectMany(o=>o.effects).First(e=>e.kind==EventEffectKind.Relic).relic;
            var themes=AssetDatabase.FindAssets("t:RoomThemeData").Select(g=>AssetDatabase.LoadAssetAtPath<RoomThemeData>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            if(!companionOnly)
            for(var act=1;act<=3;act++)
            {
                Require(flow.DebugJumpToChapter(act),"actual chapter travel B"+act);yield return Settle(.3f);
                if(!propsOnly)
                {
                var art=backdrop.artwork[act-1];
                flow.DebugClearInspectionRoom();var room=dungeon.State.Player.CurrentRoom;
                var fixture=ScriptableObject.CreateInstance<RandomEventData>();room.Story=fixture;room.StoryPrepared=true;
                fixture.roomInteriors=new Sprite[3];fixture.roomInteriors[act-1]=art.walls;
                Navigation();yield return Settle();
                Check(backdrop.background.sprite==art.walls&&backdrop.background.color==Color.white,"canonical background has one authored color pass B"+act);
                Check(backdrop.VisibleDoorMask==7,"same three real opening overlays visible for palette comparison B"+act);
                yield return Shot("B"+act+"-Base-Doors");
                fixture.roomInteriors=null;
                foreach(var theme in themes)
                {
                    fixture.roomTheme=theme;
                    foreach(FacingDirection facing in Enum.GetValues(typeof(FacingDirection)))
                    {
                        dungeon.State.Player.Inspect(room,facing);Navigation();yield return Settle();
                        Check(backdrop.background.sprite==theme.Resolve(act,facing),"B"+act+" "+theme.name+" fixed facing "+facing);
                        Check(backdrop.background.color==Color.white,"B"+act+" "+theme.name+" no second whole-frame color multiplication");
                        Check(backdrop.VisibleDoorMask==7,"B"+act+" "+theme.name+" real forward/side doors");
                        if(facing==FacingDirection.North||theme.name.Contains("서약")&&facing==FacingDirection.East)yield return Shot("B"+act+"-"+theme.name+"-"+facing);
                    }
                }
                room.Story=null;Destroy(fixture);
                }
                foreach(var type in new[]{RoomEncounterType.Goddess,RoomEncounterType.Fountain})
                {
                    var subject=dungeon.Exploration.For(type);Require(subject!=null,"saved prop "+type);
                    foreach(var used in type==RoomEncounterType.Fountain?new[]{false,true}:new[]{false})
                    foreach(var view in type==RoomEncounterType.Goddess?new[]{0,1,3}:new[]{0,1,2,3})
                    {
                        Require(flow.DebugPlaceRoomObject(subject,view,used),"place B"+act+" "+type+"/"+view+" used="+used);yield return Settle();
                        var slot=Slot(subject);var painting=subject.Painting(view,used);
                        Check(slot.painting.sprite==painting&&painting!=null,"actual prop painting "+type+" B"+act+"/"+view);
                        Check(subject.Geometry(painting).footprint.height>0,"PNG-derived physical support "+painting.name);
                        var marks=FindFirstObjectByType<RoomWallMarkLayer>();
                        Check(marks!=null&&marks.transform.parent==world.objectLayer.parent&&marks.transform.GetSiblingIndex()<world.objectLayer.GetSiblingIndex(),
                            "architectural wall marks remain behind physical prop layer "+type+" B"+act+"/"+view);
                        if(type==RoomEncounterType.Goddess)
                        {
                            var geometry=subject.Geometry(painting);var rect=slot.painting.rectTransform;
                            var canvas=rect.GetComponentInParent<Canvas>().rootCanvas;
                            var top=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,
                                rect.TransformPoint(rect.rect.min+Vector2.Scale(rect.rect.size,new Vector2(geometry.bounds.center.x,geometry.bounds.yMax))));
                            var roof=ScreenRect(world.objectLayer).yMax;
                            Check(top.y<roof-8,"statue hood remains below world/HUD boundary B"+act+"/"+view+" top="+top.y+" roof="+roof);
                        }
                        yield return Shot("B"+act+"-"+type+"-"+view+"-"+(used?"Used":"Ready"));
                    }
                }
            }
            if(!propsOnly)
            {
            run.TryGrantRelic(dogRelic);
            for(var act=1;act<=3;act++)
            {
                Require(flow.DebugJumpToChapter(act),"dog on actual B"+act);yield return Settle(.4f);
                var dog=FindFirstObjectByType<CompanionView>();Require(dog!=null,"owned dog present");
                var painting=dog.body.GetComponent<Image>();Check(painting.sprite!=null&&painting.enabled,"forward companion painting bound B"+act);
                var ground=RectTransformUtility.WorldToScreenPoint(dog.GetComponentInParent<Canvas>().worldCamera,dog.transform.position);
                log.AppendLine("DOG B"+act+" support="+ground+" actualPainting="+ScreenRect(painting.rectTransform)+" source="+painting.sprite.name);
                // The player's own near-camera pose is cropped. The companion
                // can share that depth with paws below frame; a fully shown
                // body is not a correctness requirement.
                Check(ground.y>-Screen.height*.30f&&ground.y<Screen.height*.20f,"companion support remains in near player floor B"+act);
                yield return Shot("B"+act+"-Dog-Navigation");
                ClickVisibleCompanion(dog);yield return Settle();
                Check(world.narrationText.text==GameText.Get(run.State.Companions.First().narrationKey),"companion actual click narration B"+act);
                Require(flow.StartCheatBattle(combat.GetDebugMonsterCatalog().First(m=>m.name.Contains("B1")&&m.maxHp>0)),"dog actual combat context B"+act);yield return WaitCombat();
                Check(!Ref<Image>(FindFirstObjectByType<PlayerView>(),"body").raycastTarget,"player painting yields input to actual interaction surfaces B"+act);
                var idle=painting.sprite;yield return Shot("B"+act+"-Dog-CombatIdle");
                dog.Attack(Vector2.up);yield return Settle(.025f);
                Check(painting.sprite!=idle,"dog uses a distinct authored attack painting B"+act);
                // Capture while the real attack timer is active, without freezing runtime motion.
                dog.Attack(Vector2.up);yield return Settle(.03f);
                yield return CaptureImmediate("B"+act+"-Dog-Attack");
            }
            }
            Require(flow.DebugJumpToChapter(1),"return to chapter start");yield return Settle();
            log.AppendLine("Scoped visual composition/click checks only; source palette statistics are diagnostic. No DPS, broad balance suite or claim of user five-run feel validation.");
        }
        IEnumerator CaptureImmediate(string name)
        {
            yield return new WaitForEndOfFrame();System.IO.Directory.CreateDirectory(outputFolder+"Screens");var t=ScreenCapture.CaptureScreenshotAsTexture();
            System.IO.File.WriteAllBytes(outputFolder+"Screens/Final-"+name+".jpg",t.EncodeToJPG(94));Destroy(t);log.AppendLine("SCREEN "+name);
        }
        void ClickVisibleCompanion(CompanionView dog)
        {
            var button=dog.body.GetComponent<Button>();Require(button!=null&&button.IsInteractable(),"visible companion button available");
            Canvas.ForceUpdateCanvases();var rect=dog.body;var painting=rect.GetComponent<Image>().sprite;
            var definition=run.State.Companions.First();var bounds=definition.Geometry(painting).bounds;
            // Click the visible upper back, rather than the centre of a cropped
            // full canvas, which can lie below the screen or behind the player.
            var local=rect.rect.min+Vector2.Scale(rect.rect.size,new Vector2(bounds.center.x,bounds.yMin+bounds.height*.84f));
            var canvas=rect.GetComponentInParent<Canvas>().rootCanvas;
            var point=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rect.TransformPoint(local));
            var data=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=point,button=UnityEngine.EventSystems.PointerEventData.InputButton.Left};
            var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();UnityEngine.EventSystems.EventSystem.current.RaycastAll(data,hits);
            var top=hits.Count>0?UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IPointerClickHandler>(hits[0].gameObject):null;
            Require(top==button.gameObject,"visible companion upper-back pointer reaches actual button; first hit="+(hits.Count>0?hits[0].gameObject.name:"none"));
            UnityEngine.EventSystems.ExecuteEvents.Execute(button.gameObject,data,UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
            UnityEngine.EventSystems.ExecuteEvents.Execute(button.gameObject,data,UnityEngine.EventSystems.ExecuteEvents.pointerUpHandler);
            UnityEngine.EventSystems.ExecuteEvents.Execute(button.gameObject,data,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
        }
    }
}
#endif
