using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonSlash
{
    public sealed partial class GameFlowController
    {
        private PlayRecord playRecord;
        private DungeonRunState recordReturnState;
        private int recordReturnAct;
        private GameFlowState recordReturnFlow;
        public bool IsPlayRecordInspection => State == GameFlowState.PlayRecordInspection;
        public bool CanInspectPlayRecord => State is GameFlowState.DungeonNavigation or GameFlowState.RunDefeat or GameFlowState.RunVictory || IsPlayRecordInspection;
        private void ResetPlayRecord() => playRecord = new PlayRecord { createdUtc=DateTime.UtcNow.ToString("O"),build=Application.version };
        private IEnumerable<RoomObjectData> RecordObjects()
        {
            var settings=dungeonController.Exploration;
            var all=settings.roomObjects.Select(b=>b.artwork).Concat(content.events.Where(e=>e!=null).Select(e=>e.worldObject))
                .Concat(content.events.Where(e=>e!=null).SelectMany(e=>e.options.Concat(e.pages.SelectMany(p=>p.options))).Select(o=>o.resultingWorldObject))
                .Concat(new[]{settings.key,settings.lockedDoor,settings.hiddenWall,settings.bossEntrance,settings.eliteEntrance});
            return all.Where(a=>a!=null).SelectMany(a=>new[]{a,a.usedForm}).Where(a=>a!=null).Distinct();
        }
        public PlayRecord CapturePlayRecord()
        {
            if(IsPlayRecordInspection || dungeonController?.State==null || runController?.State==null)return playRecord;
            if(playRecord==null)ResetPlayRecord();
            var state=dungeonController.State; var act=state.Floor;
            playRecord.character=runController.State.Player.Data.name;
            playRecord.artRevision=dungeonController.Exploration.presentationRevision;
            var previous=playRecord.floors.FirstOrDefault(f=>f.floor==act);
            if(previous!=null&&previous.seed!=state.Seed){playRecord.floors.Remove(previous);previous=null;}
            var saved=new FloorRecord{floor=act,seed=state.Seed,currentRoom=state.Player.CurrentRoom.RoomId,facing=(int)state.Player.Facing,keys=state.Keys.OrderBy(k=>k).ToArray()};
            saved.trail=previous?.trail??new List<int>{state.Graph.StartRoom.RoomId};
            var background=FindFirstObjectByType<StageBackdropView>();
            var floorArt=background?.artwork?.Length>=act?background.artwork[act-1]:null;
            foreach(var r in state.Graph.Rooms.OrderBy(r=>r.RoomId))
            {
                var old=previous?.rooms.FirstOrDefault(x=>x.id==r.RoomId);
                var art=r.ObjectAftermath!=null?r.ObjectAftermath:r.Type==RoomEncounterType.Event?r.Story?.worldObject:dungeonController.Exploration.For(r.Type);
                var used=r.Type==RoomEncounterType.Camp?runController.State.Fatigue.CampUsed:r.Type==RoomEncounterType.Shop?GetRoomMemory(r).MerchantDeparted:r.Type==RoomEncounterType.Event?r.ObjectUsed:r.IsCleared;
                var recordedFacing=old?.lastEntryFacing>=0?(FacingDirection)old.lastEntryFacing:state.Player.Facing;
                var theme=StageBackdropView.ThemeFor(floorArt,state,r);
                var walls=floorArt!=null?StageBackdropView.PaintingFor(floorArt,state,r,recordedFacing,used,out theme):null;
                var memory=GetRoomMemory(r);
                var item=new RoomRecord{id=r.RoomId,x=r.Position.X,y=r.Position.Y,type=(int)r.Type,chest=(int)r.ChestContent,origin=r.EncounterOriginId,
                    corner=r.ObjectCorner,objectFacing=(int)r.ObjectFacing,wall=(int)r.ObjectWall,visited=r.IsVisited,cleared=r.IsCleared,revealed=r.IsRevealed,major=r.IsMajorRoom,annex=r.IsOptionalAnnex,
                    alcove=r.IsExplorationAlcove,placed=r.ObjectPlacementPrepared,storyPrepared=r.StoryPrepared,investigated=r.StoryInvestigated,removed=r.ObjectRemoved,used=used,
                    story=r.Story?.id,subject=art?.name,aftermath=r.ObjectAftermath?.name,revisit=r.RevisitTextKey,background=walls?.name,
                    roomTheme=theme?.name,merchantPurchased=memory.MerchantPurchased,merchantDeparted=memory.MerchantDeparted,
                    conversationStarted=memory.ConversationStarted,conversationIgnored=memory.ConversationIgnored,canRearApproach=memory.CanRearApproach,
                    connections=r.Connections.OrderBy(k=>k).ToArray(),keys=r.KeyTokens.ToArray(),
                    paintings=art==null?Array.Empty<string>():Enumerable.Range(0,4).Select(v=>art.Painting(v,used)?.name??"").ToArray(),
                    entries=old?.entries??new List<int>(),choices=old?.choices??new List<string>(),monsters=old?.monsters??Array.Empty<string>(),lastEntryFacing=old?.lastEntryFacing??-1};
                saved.rooms.Add(item);
            }
            foreach(var p in state.Graph.Passages)saved.passages.Add(new PassageRecord{origin=p.Origin.RoomId,entrance=p.Annex.RoomId,concealed=p.Concealed,discovered=p.Discovered,locked=p.Locked,searches=p.SearchProgress,clue=p.ClueVariant});
            if(previous!=null)playRecord.floors.Remove(previous);playRecord.floors.Add(saved);playRecord.floors.Sort((a,b)=>a.floor.CompareTo(b.floor));
            return playRecord;
        }
        private RoomRecord CurrentRecordedRoom()
        {
            CapturePlayRecord();return playRecord?.floors.FirstOrDefault(f=>f.floor==dungeonController.Floor)?.rooms.FirstOrDefault(r=>r.id==dungeonController.State.Player.CurrentRoom.RoomId);
        }
        private void RecordRoomEntry()
        {
            var room=CurrentRecordedRoom();if(room==null)return;
            var facing=(int)dungeonController.State.Player.Facing;room.lastEntryFacing=facing;
            if(room.entries.Count<512)room.entries.Add(facing);
            var floor=playRecord.floors.First(f=>f.floor==dungeonController.Floor);if(floor.trail.Count<4096)floor.trail.Add(room.id);
        }
        private void RecordRoomChoice(EventOption option)
        {var room=CurrentRecordedRoom();if(room!=null&&room.choices.Count<128)room.choices.Add(option.title);}
        private void RecordRoomMonsters(IEnumerable<MonsterData> monsters)
        {var room=CurrentRecordedRoom();if(room!=null)room.monsters=monsters.Where(m=>m!=null).Select(m=>m.name).ToArray();}
        public string CopyPlayCode()
        {
            var record=CapturePlayRecord();if(record?.floors.Count>0){PlayRecordCodec.SaveReadable(record,false);return PlayRecordCodec.Encode(record);}return "";
        }
        public bool PreviewRecordedRoom(FloorRecord saved, int roomId, FacingDirection facing)
        {
            if(!CanInspectPlayRecord||saved==null||roomId<0||roomId>=saved.rooms.Count)return false;
            var preview=PlayRecordCodec.Restore(saved,content.events,RecordObjects());
            if(!IsPlayRecordInspection){recordReturnState=dungeonController.State;recordReturnAct=runController.State.CurrentAct;recordReturnFlow=State;CapturePlayRecord();FindFirstObjectByType<ArcadeHudController>()?.Close();}
            resultView?.Hide();
            HideTransientViews(); State=GameFlowState.PlayRecordInspection;
            preview.Player.Inspect(preview.Graph.ById(roomId),facing);
            dungeonController.SetRecordedState(preview);runController.State.CurrentAct=saved.floor;
            FindFirstObjectByType<StageBackdropView>()?.Refresh(saved.floor,preview);RefreshExploration();RefreshHud();return true;
        }
        public void EndPlayRecordInspection()
        {
            if(recordReturnState==null)return;
            dungeonController.SetRecordedState(recordReturnState);runController.State.CurrentAct=recordReturnAct;recordReturnState=null;
            if(recordReturnFlow is GameFlowState.RunDefeat or GameFlowState.RunVictory)ShowResult(recordReturnFlow==GameFlowState.RunVictory);else ShowNavigation();
        }
    }
}
