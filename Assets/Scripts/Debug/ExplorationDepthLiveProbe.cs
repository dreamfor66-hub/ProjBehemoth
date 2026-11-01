#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed partial class ExplorationDepthLiveProbe : MonoBehaviour
    {
        const string Folder="Docs/Production/ExplorationDepth-2026-11-01/";
        string outputFolder=Folder;
        readonly StringBuilder log=new();
        int checks,failures;
        string reportPrefix="Runtime";
        GameFlowController flow;DungeonController dungeon;RunController run;CombatController combat;ExplorationView world;RunExpansionView expansion;StageBackdropView backdrop;PlayRecordView records;
        BaseInputModule[] input;bool background;string clipboard,history;int? forcedSeed;
        PlayerCombatData priorCharacter;
        public void Run()=>StartCoroutine(Guarded(Review()));
        void Awake()
        {
            background=Application.runInBackground;Application.runInBackground=true;clipboard=GUIUtility.systemCopyBuffer;
            history=PlayerPrefs.GetString("Behemoth.EventHistory.v1","");forcedSeed=RunSession.ForcedSeed;
            priorCharacter=RunSession.SelectedCharacter;
            input=FindObjectsByType<BaseInputModule>(FindObjectsSortMode.None).Where(i=>i.enabled).ToArray();foreach(var module in input)module.enabled=false;
        }
        void OnDestroy()
        {Application.runInBackground=background;GUIUtility.systemCopyBuffer=clipboard;RunSession.ForcedSeed=forcedSeed;RunSession.SelectedCharacter=priorCharacter;if(history=="")PlayerPrefs.DeleteKey("Behemoth.EventHistory.v1");else PlayerPrefs.SetString("Behemoth.EventHistory.v1",history);foreach(var m in input??Array.Empty<BaseInputModule>())if(m!=null)m.enabled=true;}
        IEnumerator Guarded(IEnumerator routine)
        {
            var stack=new Stack<IEnumerator>();stack.Push(routine);
            while(stack.Count>0)
            {
                object current=null;bool moved;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}
                catch(Exception e){Check(false,e.ToString());break;}
                if(!moved){stack.Pop();continue;}if(current is IEnumerator child){stack.Push(child);continue;}yield return current;
            }
            if(failures>0)yield return Shot("Failure");
            log.AppendLine($"{checks} checks; {failures} failures. Scoped Unity Canvas/event-system review; no extra Editor or balance suite.");
            Directory.CreateDirectory(outputFolder);File.WriteAllText(outputFolder+reportPrefix+"Review.txt",log.ToString());EditorApplication.isPlaying=false;
        }
        void Check(bool value,string message){checks++;log.AppendLine((value?"PASS ":"FAIL ")+message);if(!value)failures++;Directory.CreateDirectory(outputFolder);File.WriteAllText(outputFolder+reportPrefix+"Progress.txt",log.ToString());}
        void Require(bool value,string message){Check(value,message);if(!value)throw new InvalidOperationException(message);}
        static T Ref<T>(object obj,string name)=>(T)obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(obj);
        static object Call(object obj,string name,params object[] args)=>obj.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Invoke(obj,args);
        IEnumerator Settle(float seconds=.08f){yield return new WaitForSecondsRealtime(seconds);yield return new WaitForEndOfFrame();}
        void Click(Button button)
        {
            Require(button!=null&&button.gameObject.activeInHierarchy&&button.IsInteractable(),"reachable button "+button?.name);
            Canvas.ForceUpdateCanvases();var canvas=button.GetComponentInParent<Canvas>().rootCanvas;var point=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,((RectTransform)button.transform).TransformPoint(((RectTransform)button.transform).rect.center));
            var data=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            var top=hits.Count>0?ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject):null;
            Require(top==button.gameObject,"pointer reaches "+button.name+"; actual "+top?.name);
            ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);
        }
        IEnumerator Shot(string name)
        {
            // A reveal is visible for .28s after a conclusion. Capture the settled
            // painting rather than treating its temporary dither as finished art.
            yield return Settle(.35f);Directory.CreateDirectory(outputFolder+"Screens");var texture=ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(outputFolder+"Screens/"+(reportPrefix=="Runtime"?"":"Final-")+name+".jpg",texture.EncodeToJPG(88));Destroy(texture);log.AppendLine("SCREEN "+name);
        }
        RoomObjectSlot Slot(RoomObjectData art)=>world.slots.First(s=>s.gameObject.activeInHierarchy&&s.BoundData==art);
        void Navigation()=>Call(flow,"ShowNavigation");
        void At(DungeonRoom room)
        {
            var previous=dungeon.State.Graph.Neighbors(room).FirstOrDefault(n=>!n.IsOptionalAnnex)??dungeon.State.Graph.Neighbors(room).First();
            var facing=DirectionUtility.FromDelta(new DungeonPosition(room.Position.X-previous.Position.X,room.Position.Y-previous.Position.Y));
            dungeon.State.Player.Inspect(room,facing);room.IsVisited=true;Navigation();
        }
        IEnumerator CloseMessages()
        {
            for(var i=0;i<8;i++)
            {
                yield return Settle();
                var modal=Ref<RoomEventView>(flow,"roomEventView");
                var yes=Ref<Button>(modal,"yesButton");var no=Ref<Button>(modal,"noButton");
                if(Ref<GameObject>(modal,"panel").activeInHierarchy&&yes.gameObject.activeInHierarchy&&!no.gameObject.activeInHierarchy){Click(yes);yield return Settle();continue;}
                if(!expansion.IsModalOpen)break;
                var buttons=expansion.ActiveChoices.Where(c=>c.button.gameObject.activeInHierarchy&&c.button.interactable).ToArray();
                if(buttons.Length!=1)break;Click(buttons[0].button);yield return Settle();
            }
        }
        IEnumerator WaitCombat()
        {
            var until=Time.realtimeSinceStartup+12;
            while(!combat.IsActive&&Time.realtimeSinceStartup<until)
            {
                if(expansion.IsModalOpen){yield return Settle();var options=expansion.ActiveChoices.Where(c=>c.button.gameObject.activeInHierarchy&&c.button.interactable).ToArray();if(options.Length==1)Click(options[0].button);}
                yield return null;
            }
            Require(combat.IsActive,"combat begins after actual confirmation/tutorial input");yield return Settle(.5f);
        }
        IEnumerator Review()
        {
            flow=FindFirstObjectByType<GameFlowController>();dungeon=FindFirstObjectByType<DungeonController>();run=FindFirstObjectByType<RunController>();combat=FindFirstObjectByType<CombatController>();world=FindFirstObjectByType<ExplorationView>();backdrop=FindFirstObjectByType<StageBackdropView>();records=FindFirstObjectByType<PlayRecordView>();expansion=FindFirstObjectByType<RunExpansionView>();
            yield return Settle(.85f);yield return CloseMessages();
            var content=Ref<GameContent>(flow,"content");var dogStory=content.events.Single(e=>e.id=="StrayDog");
            var dogRelic=dogStory.pages.SelectMany(p=>p.options).SelectMany(o=>o.effects).First(e=>e.kind==EventEffectKind.Relic).relic;
            run.TryGrantRelic(dogRelic);var dogData=run.State.Companions.First();
            for(var act=1;act<=3;act++)
            {
                Require(flow.DebugJumpToChapter(act),"chapter travel B"+act);yield return Settle(.6f);
                var dog=FindFirstObjectByType<CompanionView>();Require(dog!=null,"actual owned companion instantiated");
                var navGround=RectTransformUtility.WorldToScreenPoint(dog.GetComponentInParent<Canvas>().worldCamera,dog.transform.position);
                Check(navGround.y>0&&navGround.y<Screen.height*.20f,"B"+act+" dog ground is near foreground floor, not airborne: "+navGround);
                yield return Shot("B"+act+"-Hound-Navigation");
                Click(dog.body.GetComponent<Button>());yield return Settle();Check(world.narrationText.text==GameText.Get(dogData.narrationKey),"companion click narration");
                var normal=combat.GetDebugMonsterCatalog().First(m=>m.name.Contains("B1")&&m.maxHp>0);
                Require(flow.StartCheatBattle(normal),"begin companion combat presentation fixture");yield return WaitCombat();
                var combatGround=RectTransformUtility.WorldToScreenPoint(dog.GetComponentInParent<Canvas>().worldCamera,dog.transform.position);
                Check(Vector2.Distance(navGround,combatGround)<3,"B"+act+" ground remains stable across player framing");
                Check(!dog.body.GetComponent<Image>().raycastTarget,"companion painting yields combat input");
                dog.Attack(Vector2.right);yield return Shot("B"+act+"-Hound-Attack");
                flow.DebugJumpToChapter(act);yield return Settle(.5f);
                foreach(var entrance in new[]{dungeon.Exploration.eliteEntrance,dungeon.Exploration.bossEntrance})
                    foreach(var view in new[]{0,1,3})
                    {
                        Require(flow.DebugPlaceRoomObject(entrance,view,false),"place complete entrance B"+act+" view "+view);yield return Settle();
                        Check(entrance.includesDoorframe&&Slot(entrance).GetComponentInChildren<RoomOpeningPainting>().gameObject.activeInHierarchy,"complete frame uses actual wall projection");
                        yield return Shot("B"+act+"-"+(entrance==dungeon.Exploration.bossEntrance?"Boss":"Elite")+"-"+view);
                    }
                var fountain=dungeon.Exploration.For(RoomEncounterType.Fountain);
                foreach(var used in new[]{false,true})foreach(var view in new[]{0,1,2})
                {
                    flow.DebugPlaceRoomObject(fountain,view,used);yield return Settle();
                    Check(!fountain.wallMounted&&Slot(fountain).painting.sprite==fountain.Painting(view,used),"freestanding fountain correct directional/use painting");
                    yield return Shot("B"+act+"-Fountain-"+view+(used?"-Used":"-Unused"));
                }
            }
            flow.DebugJumpToChapter(1);yield return Settle();
            var font=dungeon.Exploration.For(RoomEncounterType.Fountain);flow.DebugPlaceRoomObject(font,0,false);yield return Settle();
            Click(Slot(font).button);yield return Settle();
            var eventView=Ref<RoomEventView>(flow,"roomEventView");
            var leave=Ref<Button>(eventView,"noButton");Click(leave);yield return Settle();
            Check(!dungeon.State.Player.CurrentRoom.IsCleared&&Slot(font).painting.sprite==font.front,"declining fountain leaves water and choice available");
            Click(Slot(font).button);yield return Settle();
            Click(Ref<Button>(eventView,"yesButton"));yield return Settle();yield return CloseMessages();yield return Settle(.4f);
            Check(dungeon.State.Player.CurrentRoom.IsCleared&&Slot(font).painting.sprite==font.usedFront,"drinking changes actual used state");
            Click(Slot(font).button);yield return Settle();Check(flow.State==GameFlowState.DungeonNavigation,"used fountain narration does not reopen popup");
            yield return Records();
            yield return Secrets();
            yield return Events(content);
        }
        IEnumerator Records()
        {
            var original=dungeon.State;var hp=run.State.Player.CurrentHp;var gold=run.State.Gold;var room=original.Player.CurrentRoom;
            records.Open();yield return Settle();Click(records.copyButton);yield return Settle();var code=GUIUtility.systemCopyBuffer;
            Check(code.StartsWith(PlayRecordCodec.Prefix)&&code.Length>100,"copy actual complete play code");
            Click(records.pasteButton);yield return Settle();Click(records.importButton);yield return Settle();
            Require(records.Displayed!=null,"pasted record loads");var decoded=records.Displayed;
            Check(decoded.floors.Last().rooms.Single(r=>r.id==room.RoomId).used,"used room state survives code round trip");
            yield return Shot("Play-Code-Imported");
            records.codeInput.text=code.Substring(0,code.Length-1)+(code.EndsWith("0")?"1":"0");Click(records.importButton);yield return Settle();
            Check(ReferenceEquals(records.Displayed,decoded)&&records.feedback.text==GameText.Get("ui.playrecord.invalid"),"malformed checksum rejected without replacing record");
            Click(records.previewButton);yield return Settle();Require(flow.IsPlayRecordInspection,"room preview enters read-only state");yield return Shot("Play-Code-Room-Preview");
            Click(records.turnButton);yield return Settle();Click(records.nextRoom);yield return Settle();
            Check(run.State.Player.CurrentHp==hp&&run.State.Gold==gold,"record browsing grants/consumes nothing");
            Click(records.leavePreview);yield return Settle();Check(ReferenceEquals(dungeon.State,original)&&dungeon.State.Player.CurrentRoom==room,"live graph and position restored intact");
            // Exercise the actual act-transition routine, not chapter reset, to retain all three floor snapshots.
            for(var i=0;i<2;i++)
            {
                var routine=(IEnumerator)Call(flow,"EnterNextFloorRoutine");flow.StartCoroutine(routine);yield return Settle();yield return CloseMessages();yield return Settle(.4f);
            }
            var record=flow.CapturePlayRecord();Check(record.floors.Count==3,"one play record retains B1/B2/B3 actual maps");
            var cross=PlayRecordCodec.Encode(record);Check(PlayRecordCodec.TryDecode(cross,out var restored,out _)&&restored.floors.Count==3,"three-act code round trip");
            File.WriteAllText(Folder+"SamplePlayCode.txt",cross);PlayRecordCodec.SaveReadable(record,false);
        }
        IEnumerator Secrets()
        {
            DungeonPassage hidden=null,locked=null;
            for(var i=0;i<40&&(hidden==null||locked==null);i++)
            {
                RunSession.ForcedSeed=7117+i*185917;flow.DebugJumpToChapter(2);
                hidden=dungeon.State.Graph.Passages.FirstOrDefault(p=>p.Concealed);locked=dungeon.State.Graph.Passages.FirstOrDefault(p=>p.Locked);
            }
            Require(hidden!=null&&locked!=null,"natural generated floor with independent secret and lock");
            At(hidden.Origin);yield return Settle();Check(StageBackdropView.ConnectionMask(dungeon.State)==0,"empty secret origin initially has three intact visible walls");
            var slot=Slot(dungeon.Exploration.hiddenWall);Check(!slot.painting.enabled&&!dungeon.State.CanTravel(hidden.Origin,hidden.Annex),"secret is actual wall with no accessible map exit");
            yield return Shot("Secret-Before-Discovery");
            for(var i=1;i<=3;i++){Click(Slot(dungeon.Exploration.hiddenWall).button);yield return Settle();Check(hidden.Discovered==(i==3),"secret click "+i);}
            Check(world.narrationText.text=="숨겨진 방을 찾았다!","exact discovery narration");Check(dungeon.State.CanTravel(hidden.Origin,hidden.Annex),"secret becomes accessible after third click");yield return Shot("Secret-Discovered");
            var keyRoom=dungeon.State.Graph.Rooms.Single(r=>r.KeyTokens.Contains(locked.KeyToken));At(keyRoom);yield return Settle();Click(Slot(dungeon.Exploration.key).button);yield return Settle();
            Check(dungeon.State.Keys.Contains(locked.KeyToken)&&locked.Locked,"key pickup does not unlock unseen door");At(locked.Origin);yield return Settle();
            Check(locked.Discovered&&!dungeon.State.CanTravel(locked.Origin,locked.Annex),"lock is visibly discovered but inaccessible");yield return Shot("Lock-Before-Unlock");
            Click(Slot(dungeon.Exploration.lockedDoor).button);yield return Settle();Check(!locked.Locked&&!dungeon.State.Keys.Contains(locked.KeyToken),"click visible lock consumes its key and opens connection");
        }
        IEnumerator Events(GameContent content)
        {
            foreach(var id in new[]{"MagicTeacherFirst","SalvageTrap","StrayDog"})
            {
                var story=content.events.Single(e=>e.id==id);flow.DebugJumpToChapter(1);flow.DebugPlaceRoomObject(story.worldObject,0,false);yield return Settle();
                yield return Shot("Event-"+id+"-World");Click(Slot(story.worldObject).button);yield return Settle();
                Check(expansion.modalNarrative.text==story.pages[0].narrative,"visible subject opens matching event "+id);yield return Shot("Event-"+id+"-Opening");
                if(id=="SalvageTrap")
                {
                    Click(expansion.ActiveChoices[1].button);yield return Settle();Click(expansion.ActiveChoices[0].button);yield return Settle();yield return CloseMessages();yield return Settle(.4f);
                    Check(Slot(story.worldObject).painting.sprite==story.worldObject.usedFront,"trap aftermath actually shows inactive emptied mechanism");yield return Shot("Event-Trap-Used");
                }
                else
                {
                    Click(expansion.ActiveChoices[1].button);yield return Settle();yield return CloseMessages();yield return Settle(.3f);
                    if(id=="MagicTeacherFirst")Check(!world.slots.Any(s=>s.gameObject.activeInHierarchy&&s.BoundData==story.worldObject),"departing surveyor is no longer painted in room");
                    else Check(world.slots.Any(s=>s.gameObject.activeInHierarchy&&s.BoundData==story.worldObject),"unrescued hound remains in encounter");
                }
            }
        }
    }
}
#endif
