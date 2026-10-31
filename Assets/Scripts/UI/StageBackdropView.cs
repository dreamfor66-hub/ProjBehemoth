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
        public int VisibleDoorMask { get; private set; }
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
                var walls = camping ? run?.State?.Fatigue.CampUsed == true && art.campUsedInterior != null ? art.campUsedInterior : art.campInterior : art.walls;
                // Swap artwork without overwriting the authored RectTransform layout.
                if(current!=index||background.sprite!=walls){background.sprite=walls;current=index;}
                VisibleDoorMask=ConnectionMask(state);
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
                    layer.enabled=!camping&&i<3&&(VisibleDoorMask&(1<<i))!=0&&layer.sprite!=null;
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
