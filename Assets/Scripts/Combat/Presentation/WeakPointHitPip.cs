using UnityEngine;
using UnityEngine.UI;
namespace DungeonSlash
{
    // Short painted tally, shared by the remaining-hit mark and its fading fragment.
    public sealed class WeakPointHitPip : Image
    {
        public override Texture mainTexture=>Texture2D.whiteTexture;
        public override Material defaultMaterial
        {
            get
            {
                var style=GetComponentInParent<WeakPointView>()?.reticle?.Style;
                return style!=null&&style.material!=null?style.material:base.defaultMaterial;
            }
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            CombatInkDrawing.Quad(vh,new Vector2(r.xMin,r.yMin+2),new Vector2(r.xMin+3,r.yMax),new Vector2(r.xMax,r.yMax-2),new Vector2(r.xMax-3,r.yMin),color);
        }
    }
}
