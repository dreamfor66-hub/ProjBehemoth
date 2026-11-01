using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class StageBackdropView : MonoBehaviour
    {
        public RunController run;
        public Image background;
        public Sprite[] stages;
        public DungeonController dungeon;
        public FloorArtwork[] artwork;
        public Image[] doorLayers;
        private int current = -1;
        private ExplorationView exploration;
        public int VisibleDoorMask { get; private set; }
        public RoomThemeData VisibleTheme { get; private set; }
        [SerializeField] private RoomWallMarkLayer wallMarks;

        public static RoomThemeData ThemeFor(FloorArtwork art, DungeonRunState state, DungeonRoom room)
        {
            if(room==null||art==null||room.Type is RoomEncounterType.Camp or RoomEncounterType.Boss)return null;
            if(room.Story?.roomTheme!=null)return room.Story.roomTheme;
            if(room.Story?.roomInteriors!=null&&System.Array.Exists(room.Story.roomInteriors,s=>s!=null))return null;
            var choices=art.roomVariations;if(choices==null||choices.Length==0||state==null)return null;
            // Include the installed base room in the distribution. This is a spatial identity,
            // not an entry-time random roll, so revisits and imported records remain stable.
            var hash=unchecked((uint)state.Seed*2654435761u^(uint)(room.RoomId+1)*2246822519u);
            hash^=hash>>16;var index=(int)(hash%(uint)(choices.Length+1));
            return index==choices.Length?null:choices[index];
        }

        public static Sprite PaintingFor(FloorArtwork art,DungeonRunState state,DungeonRoom room,FacingDirection facing,bool campUsed,out RoomThemeData theme)
        {
            theme=ThemeFor(art,state,room);var themed=theme?.Resolve(state?.Floor??1,facing);
            if(themed!=null)return themed;
            if(room?.Type==RoomEncounterType.Camp&&art.campInterior!=null)return campUsed&&art.campUsedInterior!=null?art.campUsedInterior:art.campInterior;
            if(room?.Type==RoomEncounterType.Boss&&art.bossInterior!=null)return art.bossInterior;
            var index=(state?.Floor??1)-1;var interiors=room?.Story?.roomInteriors;
            if(interiors!=null&&index>=0&&index<interiors.Length&&interiors[index]!=null)return interiors[index];
            return art.walls;
        }
        private void LateUpdate()
        {
            if (run?.State == null || background == null) return;
            Refresh(run.State.CurrentAct, dungeon?.State);
        }
        public void Refresh(int floor, DungeonRunState state)
        {
            if(artwork!=null&&artwork.Length>0)
            {
                var index=Mathf.Clamp(floor-1,0,artwork.Length-1);
                var art=artwork[index];if(art==null)return;
                var camping = state?.Player?.CurrentRoom?.Type == RoomEncounterType.Camp && art.campInterior != null;
                var campUsed = state?.Inspection == true ? state.Player.CurrentRoom.ObjectUsed : run?.State?.Fatigue.CampUsed == true;
                var walls=PaintingFor(art,state,state?.Player?.CurrentRoom,state?.Player?.Facing??FacingDirection.North,campUsed,out var theme);
                VisibleTheme=theme;
                background.color=theme!=null&&theme.useFloorLighting?art.objectIllumination:Color.white;
                // Swap artwork without overwriting the authored RectTransform layout.
                if(current!=index||background.sprite!=walls){background.sprite=walls;current=index;}
                VisibleDoorMask=ConnectionMask(state);
                if(wallMarks==null)wallMarks=background.GetComponentInChildren<RoomWallMarkLayer>(true);
                if(wallMarks!=null)wallMarks.Show(theme,state,VisibleDoorMask,art.objectIllumination);
                if(doorLayers==null)return;
                for(var i=0;i<doorLayers.Length;i++)
                {
                    var layer=doorLayers[i];if(layer==null)continue;
                    layer.sprite=art.openings!=null&&i<art.openings.Length?art.openings[i]:null;
                    var shape=art.openingContours!=null&&i<art.openingContours.Length?art.openingContours[i]:null;
                    var canvasScale=shape!=null&&shape.canvasScale.x>0?shape.canvasScale:Vector2.one;
                    var canvasOffset=shape!=null?shape.canvasOffset:Vector2.zero;
                    var rect=layer.rectTransform;
                    rect.anchorMin=canvasOffset;rect.anchorMax=canvasOffset+canvasScale;
                    rect.offsetMin=rect.offsetMax=Vector2.zero;
                    var light=shape!=null&&shape.exposure>0?shape.exposure:1;
                    layer.color=new Color(light,light,light,1);
                    if(exploration==null)exploration=FindFirstObjectByType<ExplorationView>();
                    var paintedFrame=false;
                    if(exploration?.slots!=null)foreach(var slot in exploration.slots)
                        if(slot!=null&&slot.gameObject.activeInHierarchy&&slot.BoundData!=null&&slot.BoundData.includesDoorframe
                            &&slot.BoundView==(i==0?0:i==1?1:3)){paintedFrame=true;break;}
                    layer.enabled=!camping&&!paintedFrame&&i<3&&(VisibleDoorMask&(1<<i))!=0&&layer.sprite!=null;
                }
                return;
            }
            if(stages==null||stages.Length==0)return;
            var legacy=Mathf.Clamp(floor-1,0,stages.Length-1);
            if(legacy!=current){current=legacy;background.sprite=stages[legacy];}
        }
        public static int ConnectionMask(DungeonRunState state)
        {
            if(state?.Player?.CurrentRoom==null)return 0;
            var mask=0;var room=state.Player.CurrentRoom;
            for(var i=0;i<3;i++)
            {
                var direction=DirectionUtility.ToAbsolute(state.Player.Facing,(RelativeDirection)i);
                var target=state.Graph.At(room.Position+DirectionUtility.Offset(direction));
                if(target!=null&&room.Connections.Contains(target.RoomId)&&state.CanSeePassage(room,target))mask|=1<<i;
            }
            return mask;
        }
    }
}
