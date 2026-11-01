using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace DungeonSlash
{
    /// <summary>Architectural marks stay on compass walls, using the room camera's wall projection.</summary>
    public sealed class RoomWallMarkLayer : MonoBehaviour
    {
        [SerializeField] private RoomWallMarkPainting[] paintings;
        public void Prepare()
        {
            // Architectural marks belong directly above the backdrop, behind its
            // doorway paintings and principal room objects, including on reload.
            if(transform.GetSiblingIndex()!=0)transform.SetAsFirstSibling();
            if(paintings!=null&&paintings.Length==3)return;
            paintings=new RoomWallMarkPainting[3];
            for(var i=0;i<3;i++)
            {
                var go=new GameObject("Fixed wall mark "+i,typeof(RectTransform),typeof(CanvasRenderer),typeof(RoomWallMarkPainting));
                go.transform.SetParent(transform,false);var picture=go.GetComponent<RoomWallMarkPainting>();picture.raycastTarget=false;paintings[i]=picture;
                var rect=picture.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            }
        }
        public void Show(RoomThemeData theme,DungeonRunState state,int doorMask,Color light)
        {
            Prepare();foreach(var picture in paintings)picture.enabled=false;
            if(theme==null||state?.Player==null||theme.wallMarks==null)return;
            foreach(var mark in theme.wallMarks)
            {
                if(mark?.painting==null)continue;
                var relative=DirectionUtility.ToRelative(state.Player.Facing,mark.wall);
                if(relative==RelativeDirection.Back)continue;
                var index=relative==RelativeDirection.Forward?0:relative==RelativeDirection.Left?1:2;
                if((doorMask&(1<<index))!=0)continue;
                var picture=paintings[index];picture.color=light;picture.enabled=true;picture.Set(mark.painting,index);
            }
        }
    }

}
