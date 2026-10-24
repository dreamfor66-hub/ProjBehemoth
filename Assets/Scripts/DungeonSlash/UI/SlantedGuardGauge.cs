using UnityEngine;
using UnityEngine.UI;
namespace DungeonSlash
{
    // Pixel-aligned parallelogram; the fill rises inside the same slanted outline.
    [RequireComponent(typeof(CanvasRenderer))] public sealed class SlantedGuardGauge : MaskableGraphic
    {
        [Range(0,1)] public float amount=1;
        public float slant=32, border=8;
        public Color fillColor=new Color32(147,171,182,255);
        public void SetValue(float value){value=Mathf.Clamp01(value);if(Mathf.Approximately(amount,value))return;amount=value;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            void Quad(float y0,float y1,float inset,Color tint)
            {
                var x0=Mathf.Round((Mathf.InverseLerp(r.yMin,r.yMax,y0)-.5f)*slant/4)*4;
                var x1=Mathf.Round((Mathf.InverseLerp(r.yMin,r.yMax,y1)-.5f)*slant/4)*4;
                var i=vh.currentVertCount;vh.AddVert(new Vector3(r.xMin+x0+inset,y0),tint,Vector2.zero);vh.AddVert(new Vector3(r.xMin+x1+inset,y1),tint,Vector2.zero);vh.AddVert(new Vector3(r.xMax+x1-inset,y1),tint,Vector2.zero);vh.AddVert(new Vector3(r.xMax+x0-inset,y0),tint,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
            }
            Quad(r.yMin,r.yMax,0,new Color32(11,12,22,255));
            Quad(r.yMin+border,r.yMax-border,border,new Color32(48,49,72,255));
            var top=r.yMin+border+Mathf.Round((r.height-border*2)*amount/4)*4;
            if(top>r.yMin+border)Quad(r.yMin+border,top,border,fillColor);
        }
    }
}
