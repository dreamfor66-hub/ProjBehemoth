using UnityEngine;
using UnityEngine.UI;
namespace DungeonSlash
{
    // Broad crimson edge washes leave the central targeting area unobscured.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HealthDangerGraphic : MaskableGraphic
    {
        [Range(0,1)] public float intensity;
        public void SetIntensity(float value){value=Mathf.Clamp01(value);if(Mathf.Abs(value-intensity)<.002f)return;intensity=value;SetVerticesDirty();}
        protected override void Awake(){base.Awake();raycastTarget=false;}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(intensity<=0)return;var r=rectTransform.rect;
            for(var side=0;side<4;side++)
            for(var n=0;n<32;n++)
            {
                var t=n/32f;var u=(n+1)/32f;
                Vector2 Edge(float p)=>side switch{0=>new(r.xMin,Mathf.Lerp(r.yMin,r.yMax,p)),1=>new(r.xMax,Mathf.Lerp(r.yMin,r.yMax,p)),2=>new(Mathf.Lerp(r.xMin,r.xMax,p),r.yMin),_=>new(Mathf.Lerp(r.xMin,r.xMax,p),r.yMax)};
                var inward=side==0?Vector2.right:side==1?Vector2.left:side==2?Vector2.up:Vector2.down;
                var width=(side<2?r.width*.12f:r.height*.15f)*(1+.22f*Mathf.Sin(n*.85f+side*2));
                var a=Edge(t);var b=Edge(u);var ink=new Color(.52f,.018f,.025f,intensity*.9f);var clear=new Color(.30f,0,0,0);
                var i=vh.currentVertCount;vh.AddVert(a,ink,Vector2.zero);vh.AddVert(b,ink,Vector2.zero);vh.AddVert(b+inward*width,clear,Vector2.zero);vh.AddVert(a+inward*width,clear,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
            }
        }
    }
}
