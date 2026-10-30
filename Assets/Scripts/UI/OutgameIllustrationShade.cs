using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    // Authored UI value fields keep text readable over full-screen paintings.
    // The artwork and its figures are never moved or deformed.
    [ExecuteAlways, RequireComponent(typeof(CanvasRenderer))]
    public sealed class OutgameIllustrationShade : MaskableGraphic
    {
        [Range(0,1)] public float leftOpacity=.96f;
        [Range(0,1)] public float bottomOpacity=.96f;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var rect=rectTransform.rect;
            var xs=new[]{0f,.28f,.42f,.60f,1f};
            var ys=new[]{0f,.16f,.28f,.82f,1f};
            for(var y=0;y<ys.Length;y++)for(var x=0;x<xs.Length;x++)
            {
                var left=leftOpacity*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.28f,.60f,xs[x])));
                var bottom=bottomOpacity*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.29f,ys[y])));
                var top=.18f*Mathf.InverseLerp(.82f,1,ys[y]);
                var alpha=1-(1-left)*(1-bottom)*(1-top);
                vh.AddVert(new Vector3(rect.xMin+rect.width*xs[x],rect.yMin+rect.height*ys[y]),new Color(.018f,.02f,.018f,alpha),Vector2.zero);
            }
            for(var y=0;y<ys.Length-1;y++)for(var x=0;x<xs.Length-1;x++)
            {var i=y*xs.Length+x;vh.AddTriangle(i,i+xs.Length,i+1);vh.AddTriangle(i+1,i+xs.Length,i+xs.Length+1);}
        }
        protected override void OnEnable(){base.OnEnable();raycastTarget=false;}
    }
}
