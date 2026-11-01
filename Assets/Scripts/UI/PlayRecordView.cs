using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class PlayRecordView : MonoBehaviour
    {
        public GameFlowController flow;
        public GameObject panel, previewToolbar;
        public TMP_InputField codeInput;
        public TextMeshProUGUI summary, detail, feedback, previewLabel;
        public RectTransform roomList;
        public Button roomTemplate, copyButton, pasteButton, importButton, currentButton, closeButton, previewButton;
        public Button previousRoom, nextRoom, turnButton, tableButton, leavePreview;
        public Button[] floorButtons;
        private readonly List<Button> rows=new();
        private PlayRecord displayed;
        private FloorRecord floor;
        private int roomId, facing;
        public PlayRecord Displayed => displayed;

        private void Awake()
        {
            copyButton.onClick.AddListener(()=>
            {
                var code=flow.CopyPlayCode();if(string.IsNullOrEmpty(code)){Feedback("ui.playrecord.empty");return;}
                GUIUtility.systemCopyBuffer=code;codeInput.SetTextWithoutNotify(code);Feedback("ui.playrecord.copied");
            });
            pasteButton.onClick.AddListener(()=>{codeInput.text=GUIUtility.systemCopyBuffer;Feedback("ui.playrecord.pasted");});
            importButton.onClick.AddListener(()=>Import(codeInput.text));
            currentButton.onClick.AddListener(()=>{flow.EndPlayRecordInspection();displayed=flow.CapturePlayRecord();ChooseFloor(displayed?.floors.FirstOrDefault());});
            closeButton.onClick.AddListener(()=>{panel.SetActive(false);if(flow.IsPlayRecordInspection)previewToolbar.SetActive(true);});
            previewButton.onClick.AddListener(Preview);
            previousRoom.onClick.AddListener(()=>{roomId=(roomId+floor.rooms.Count-1)%floor.rooms.Count;UseRecordedFacing();Preview();});
            nextRoom.onClick.AddListener(()=>{roomId=(roomId+1)%floor.rooms.Count;UseRecordedFacing();Preview();});
            turnButton.onClick.AddListener(()=>{var views=EntryViews();facing=views[(Array.IndexOf(views,facing)+1)%views.Length];Preview();});
            tableButton.onClick.AddListener(()=>{previewToolbar.SetActive(false);panel.SetActive(true);Draw();});
            leavePreview.onClick.AddListener(()=>{flow.EndPlayRecordInspection();previewToolbar.SetActive(false);});
            for(var i=0;i<floorButtons.Length;i++){var act=i+1;floorButtons[i].onClick.AddListener(()=>ChooseFloor(displayed?.floors.FirstOrDefault(f=>f.floor==act)));}
            panel.SetActive(false);previewToolbar.SetActive(false);roomTemplate.gameObject.SetActive(false);
        }
        public void Open()
        {
            FindFirstObjectByType<DebugCheatOverlay>()?.SetOpen(false);
            if(!flow.IsPlayRecordInspection){displayed=flow.CapturePlayRecord();ChooseFloor(displayed?.floors.LastOrDefault());}
            previewToolbar.SetActive(false);panel.SetActive(true);transform.SetAsLastSibling();Feedback("ui.playrecord.help");Draw();
        }
        public bool Import(string code)
        {
            if(!PlayRecordCodec.TryDecode(code,out var record,out var error)){Feedback(error);return false;}
            // Validation completes before replacing the displayed record. The live run is untouched.
            var path=PlayRecordCodec.SaveReadable(record,true);displayed=record;ChooseFloor(record.floors.Last());
            Feedback("ui.playrecord.imported");Debug.Log("Play record imported: "+path);return true;
        }
        private void ChooseFloor(FloorRecord value)
        {
            floor=value;roomId=value?.currentRoom??0;UseRecordedFacing();Draw();
        }
        private void UseRecordedFacing()
        {
            if(floor==null)return;var r=floor.rooms[roomId];var views=EntryViews();
            var recorded=r.lastEntryFacing>=0?r.lastEntryFacing:r.id==0?0:floor.facing;facing=views.Contains(recorded)?recorded:views[0];
        }
        private int[] EntryViews()
        {
            if(floor==null)return new[]{0};var room=floor.rooms[roomId];
            var views=room.connections.Select(id=>floor.rooms[id]).Select(other=>(int)DirectionUtility.FromDelta(new DungeonPosition(room.x-other.x,room.y-other.y))).ToList();
            if(roomId==0)views.Add(0);return views.Count==0?new[]{0}:views.Distinct().OrderBy(v=>v).ToArray();
        }
        private static string RoomName(int type)=>type==(int)RoomEncounterType.Empty?GameText.Get("ui.playrecord.emptyroom"):MapRoomIcon.RoomLabel((RoomEncounterType)type);
        private void Draw()
        {
            if(summary==null)return;
            summary.text=displayed==null?GameText.Get("ui.playrecord.empty"):GameText.Format("ui.playrecord.summary",displayed.floors.Count,displayed.createdUtc?.Replace('T',' ').Replace("Z"," UTC"));
            for(var i=0;i<floorButtons.Length;i++)
            {floorButtons[i].interactable=displayed?.floors.Any(f=>f.floor==i+1)==true;floorButtons[i].image.color=floor?.floor==i+1?new Color32(110,79,40,255):new Color32(36,38,40,255);}
            var count=floor?.rooms.Count??0;
            while(rows.Count<count){var row=Instantiate(roomTemplate,roomList);rows.Add(row);}
            for(var i=0;i<rows.Count;i++)
            {
                var row=rows[i];row.gameObject.SetActive(i<count);if(i>=count)continue;
                var r=floor.rooms[i];var id=i;
                row.GetComponentInChildren<TextMeshProUGUI>().text=GameText.Format("ui.playrecord.row",r.id,r.x,r.y,RoomName(r.type),GameText.Get(r.visited?"ui.playrecord.visited":"ui.playrecord.unvisited"));
                row.onClick.RemoveAllListeners();row.onClick.AddListener(()=>{roomId=id;UseRecordedFacing();Draw();});
                row.image.color=id==roomId?new Color32(110,79,40,255):new Color32(29,31,33,255);
            }
            previewButton.interactable=floor!=null&&flow.CanInspectPlayRecord;
            if(floor==null){detail.text="";return;}
            var selected=floor.rooms[roomId];
            detail.text=GameText.Format("ui.playrecord.detail",floor.floor,selected.id,selected.x,selected.y,RoomName(selected.type),
                selected.type!=(int)RoomEncounterType.Event?GameText.Get("ui.playrecord.none"):selected.storyPrepared?string.IsNullOrEmpty(selected.story)?GameText.Get("ui.playrecord.none"):selected.story:GameText.Get("ui.playrecord.pending"),
                selected.subject??GameText.Get("ui.playrecord.none"),selected.background??"",string.Join(", ",selected.connections),
                string.Join(" → ",selected.entries.Select(e=>GameText.Get("ui.playrecord.facing"+e))),string.Join("\n",selected.choices));
            if(!string.IsNullOrEmpty(selected.revisit))detail.text+="\n\n"+GameText.Get(selected.revisit);
            detail.text+="\n"+GameText.Format("ui.playrecord.state",GameText.Get(selected.removed?"ui.playrecord.removed":selected.used?"ui.playrecord.used":selected.cleared?"ui.playrecord.resolved":"ui.playrecord.available"));
            detail.rectTransform.anchoredPosition=Vector2.zero;
        }
        public void Preview()
        {
            if(floor==null||!flow.PreviewRecordedRoom(floor,roomId,(FacingDirection)facing)){Feedback("ui.playrecord.busy");return;}
            panel.SetActive(false);previewToolbar.SetActive(true);transform.SetAsLastSibling();
            turnButton.interactable=EntryViews().Length>1;
            previewLabel.text=GameText.Format("ui.playrecord.preview",floor.floor,roomId,GameText.Get("ui.playrecord.facing"+facing));
        }
        private void Feedback(string key){if(feedback!=null)feedback.text=GameText.Get(key);}
        private void Update()
        {
            // F1 chapter travel/new run can end inspection while the toolbar is open.
            if(previewToolbar.activeSelf&&!flow.IsPlayRecordInspection)previewToolbar.SetActive(false);
        }
    }
}
