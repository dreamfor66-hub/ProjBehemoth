using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class RoomWallMarkPainting : MaskableGraphic
    {
        Sprite painting;int wall;
        public override Texture mainTexture=>painting!=null?painting.texture:Texture2D.whiteTexture;
        public void Set(Sprite value,int side){if(painting==value&&wall==side)return;painting=value;wall=side;raycastTarget=false;SetAllDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(painting==null)return;
            var viewport=rectTransform.rect;var uv=DataUtility.GetOuterUV(painting);var vp=FloorArtwork.RoomVanishingPoint;
            var height=.19f;var width=height*painting.rect.width/painting.rect.height*viewport.height/viewport.width;
            var center=wall==0?.5f:wall==1?.21f:.79f;var x0=center-width*(wall==0?.5f:.27f);var x1=center+width*(wall==0?.5f:.27f);
            var y0=.60f;var y1=y0+height;
            const int columns=8,rows=6;
            for(var row=0;row<=rows;row++)for(var column=0;column<=columns;column++)
            {
                var tx=column/(float)columns;var ty=row/(float)rows;var x=Mathf.Lerp(x0,x1,tx);var low=y0;var high=y1;
                if(wall!=0)
                {
                    // Reciprocal depth is a true planar projection, not a flipped flat UI decal.
                    var a=x0-vp.x;var b=x1-vp.x;x=vp.x+Mathf.Sign(a)/Mathf.Lerp(1/Mathf.Abs(a),1/Mathf.Abs(b),tx);
                    var ratio=Mathf.Abs((x-vp.x)/(center-vp.x));low=vp.y+(y0-vp.y)*ratio;high=vp.y+(y1-vp.y)*ratio;
                }
                vh.AddVert(new Vector3(viewport.xMin+x*viewport.width,viewport.yMin+Mathf.Lerp(low,high,ty)*viewport.height),color,
                    new Vector2(Mathf.Lerp(uv.x,uv.z,tx),Mathf.Lerp(uv.y,uv.w,ty)));
                if(row>0&&column>0){var i=row*(columns+1)+column;vh.AddTriangle(i-columns-2,i-columns-1,i);vh.AddTriangle(i-columns-2,i,i-1);}
            }
        }
    }
}
