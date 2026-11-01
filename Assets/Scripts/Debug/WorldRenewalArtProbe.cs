#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed partial class ExplorationDepthLiveProbe
    {
        public void RunWorldRenewal(bool combatOnly=false)
        { outputFolder="Docs/Production/WorldRenewal-2026-11-01/"; reportPrefix=combatOnly?"CombatRuntime":"Runtime"; StartCoroutine(Guarded(WorldRenewal(combatOnly))); }
        IEnumerator WorldRenewal(bool combatOnly)
        {
            flow=FindFirstObjectByType<GameFlowController>(); dungeon=FindFirstObjectByType<DungeonController>(); run=FindFirstObjectByType<RunController>();
            combat=FindFirstObjectByType<CombatController>(); world=FindFirstObjectByType<ExplorationView>(); backdrop=FindFirstObjectByType<StageBackdropView>();
            records=FindFirstObjectByType<PlayRecordView>(); expansion=FindFirstObjectByType<RunExpansionView>();
            yield return Settle(.9f); yield return CloseMessages();
            var content=Ref<GameContent>(flow,"content");
            if(!combatOnly)
            {
            Check(content.events.Count(e=>e!=null&&!e.excludedFromRandomPool)==18,"18 active authored encounters, retired contract/mirror/trap excluded");
            foreach(var act in new[]{1,2,3})
            {
                Require(flow.DebugJumpToChapter(act),"actual chapter travel B"+act); yield return Settle(.4f);
                MapFatigueLiveChecks.CheckCurrentFloor(run.State,dungeon.State.Graph,Check);
                var art=backdrop.artwork[act-1];
                Check(art.roomVariations?.Length>=2&&art.roomVariations.All(t=>t.Resolve(act,FacingDirection.North)!=null),"B"+act+" saved ordinary room variants");
                var original=dungeon.State.Player.CurrentRoom;
                var normalFrames=new Dictionary<string,Rect>();
                foreach(var theme in art.roomVariations)
                {
                    var room=dungeon.State.Graph.Rooms.FirstOrDefault(r=>r.Type==RoomEncounterType.Empty&&StageBackdropView.ThemeFor(art,dungeon.State,r)==theme);
                    if(room==null){log.AppendLine("Variant fixture not present in this natural floor: "+theme.name);continue;}
                    foreach(FacingDirection facing in Enum.GetValues(typeof(FacingDirection)))
                    {
                        dungeon.State.Player.Inspect(room,facing);Navigation();yield return Settle();
                        Check(backdrop.VisibleTheme==theme&&backdrop.background.sprite==theme.Resolve(act,facing),"B"+act+" fixed room "+room.RoomId+" / "+facing+" retains architecture");
                        if(facing==FacingDirection.North||act==1)yield return Shot("B"+act+"-"+theme.name+"-"+facing);
                    }
                }
                foreach(var type in new[]{RoomEncounterType.Chest,RoomEncounterType.Fountain})
                foreach(var used in new[]{false,true})foreach(var view in new[]{0,1,2})
                {
                    var subject=dungeon.Exploration.For(type); Require(flow.DebugPlaceRoomObject(subject,view,used),"place "+type+" B"+act+"/"+view+" used="+used);yield return Settle();
                    var slot=Slot(subject);var painting=subject.Painting(view,used);
                    Check(slot.painting.sprite==painting&&painting!=null,"actual sprite bound "+type+" B"+act+"/"+view+" used="+used);
                    Check(subject.Geometry(painting).footprint.height>0,"derived physical support "+painting.name);
                    var frame=ScreenRect(slot.painting.rectTransform);var key=type+"/"+view;
                    if(!used)normalFrames[key]=frame;
                    else if(subject.Painting(view,false).rect.size==painting.rect.size)
                    {
                        var before=normalFrames[key];
                        Check(Vector2.Distance(before.min,frame.min)<.6f&&Vector2.Distance(before.size,frame.size)<.6f,"aligned state preserves exact support/pixel scale "+key+" B"+act);
                    }
                    if(view==0||act==1)yield return Shot("B"+act+"-"+type+"-"+view+"-"+(used?"Used":"Ready"));
                }
                foreach(var id in new[]{"Well","AmberSeed","Scholar","Delivery","Training"})
                {
                    var story=content.events.Single(e=>e.id==id);Check(story.roomTheme!=null,"saved purpose-built theme "+id);
                    foreach(var facing in new[]{FacingDirection.North,FacingDirection.East,FacingDirection.South,FacingDirection.West})
                    {
                        flow.DebugPlaceRoomObject(story.worldObject,0,false); var room=dungeon.State.Player.CurrentRoom;
                        dungeon.State.Player.Inspect(room,facing);Navigation();yield return Settle();
                        Check(backdrop.VisibleTheme==story.roomTheme,"B"+act+" "+id+" keeps event environment when facing "+facing);
                        if(facing==FacingDirection.North||act==1&&facing==FacingDirection.East)yield return Shot("B"+act+"-Room-"+id+"-"+facing);
                    }
                }
                var record=flow.CapturePlayRecord();var floorRecord=record.floors.Single(f=>f.floor==act);
                Check(floorRecord.rooms.Any(r=>!string.IsNullOrEmpty(r.roomTheme)),"play code records visible authored theme B"+act);
                var code=flow.CopyPlayCode();Check(!string.IsNullOrWhiteSpace(code),"current art/theme record can be copied B"+act);
            }
            yield return WorldRenewalEvents();
            }
            foreach(var species in new[]{"고블린사냥꾼","납골당의종","철니미믹","실인형","종토끼","공허의눈","숯불도롱뇽"})
            {
                var monster=combat.GetDebugMonsterCatalog().FirstOrDefault(m=>m.displayName.Replace(" ","")==species||m.name.Replace(" ","").Contains(species));
                Require(monster!=null,"active combat catalog contains "+species);
                var act=monster.name.Contains("B3")?3:monster.name.Contains("B2")?2:1;
                // Followers die when spawned without their actual summoner. Use
                // the authored leader and keep its real group placement/scale.
                var leader=monster.diesWithLeader?combat.GetDebugMonsterCatalog().FirstOrDefault(m=>m.supportRoster.Contains(monster)):monster;
                Require(leader!=null,"authored leader is available for "+species);
                flow.DebugJumpToChapter(act);yield return Settle();Require(flow.StartCheatBattle(leader),"actual combat art fixture "+species);yield return WaitCombat();
                var view=FindObjectsByType<MonsterView>(FindObjectsSortMode.None).First(v=>v.gameObject.activeInHierarchy&&Ref<MonsterData>(v,"boundData")==monster);
                var body=Ref<Image>(view,"body");var animator=body.GetComponent<CombatSpriteAnimator>();Require(animator!=null,"real combat animator "+species);
                // Pause only the presentation fixture. The actual authored body/hitbox scale and placement remain unchanged.
                combat.enabled=false;
                var runtime=combat.Enemies.First(e=>e.Data==monster);
                var center=view.Root.anchoredPosition+FindFirstObjectByType<CombatSceneView>().MonsterPosition;
                foreach(var pose in new[]{CombatVisualState.Idle,CombatVisualState.Ready,CombatVisualState.Attack,CombatVisualState.Stunned,CombatVisualState.Dead})
                {
                    if(pose==CombatVisualState.Stunned&&monster.combatAnimation.stunned==null)continue;
                    var sprite=monster.combatAnimation.For(pose);if(sprite==null)continue;
                    // Production Bind applies this native frame's own rect and
                    // pivot. Swapping Image.sprite alone stretches tight crops.
                    switch(pose)
                    {
                        case CombatVisualState.Idle:runtime.EndCharge();runtime.CompleteNormalAttack();break;
                        case CombatVisualState.Ready:
                            runtime.StartCharge(center);if(runtime.State!=MonsterState.Charging)runtime.BeginNormalAttack();break;
                        case CombatVisualState.Attack:runtime.EndCharge();runtime.BeginNormalAttack();view.PlayAttack(10);break;
                        case CombatVisualState.Stunned:runtime.EnterStun();break;
                        case CombatVisualState.Dead:runtime.TakeDamage(float.MaxValue);break;
                    }
                    view.Bind(runtime);Call(combat,"SyncWeakPointViews");yield return Settle();
                    Check(body.sprite==sprite&&body.enabled,"actual separate pose "+species+"/"+pose);
                    Check(sprite.texture!=null&&AssetDatabase.GetAssetPath(sprite.texture).EndsWith(".png",StringComparison.OrdinalIgnoreCase),"editable PNG bound "+species+"/"+pose);
                    Check(view.Root.localScale==Vector3.one*runtime.VisualScale,"authored species scale survives native pose bind "+species+"/"+pose);
                    yield return Shot("Combat-"+species+"-"+pose);
                }
                combat.enabled=true;
            }
            var wizard=content.characters.Single(c=>c.character==CharacterKind.Wizard);
            RunSession.SelectedCharacter=wizard;run.BeginRun();flow.DebugJumpToChapter(1);yield return Settle();
            yield return Shot("Player-Wizard-Navigation");
            Require(flow.StartCheatBattle(combat.GetDebugMonsterCatalog().First()),"actual wizard combat presentation");yield return WaitCombat();
            var player=FindFirstObjectByType<PlayerView>();var playerBody=Ref<Image>(player,"body");var playerAnimator=playerBody.GetComponent<CombatSpriteAnimator>();
            combat.enabled=false;
            foreach(var pose in new[]{CombatVisualState.Idle,CombatVisualState.Ready,CombatVisualState.Attack,CombatVisualState.Guard})
            {
                playerAnimator.Play(wizard.combatAnimation,pose,true);yield return Settle();
                Check(playerBody.enabled&&playerBody.sprite==wizard.combatAnimation.For(pose),"actual wizard PNG pose "+pose);
                yield return Shot("Player-Wizard-"+pose);
            }
            combat.enabled=true;RunSession.SelectedCharacter=priorCharacter;
            Require(flow.DebugJumpToChapter(1),"review returns to chapter start");yield return Settle();
            log.AppendLine("Art review uses real saved canvases and authored combat poses; no DPS comparison, whole-system regression or substitute for user five-run feel feedback.");
        }
    }
}
#endif
