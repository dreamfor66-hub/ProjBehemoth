using UnityEngine;
using UnityEngine.UI;
namespace DungeonSlash
{
    // Peripheral stepped haze; gameplay center, weakpoints and HUD stay clear.
    public sealed class FatigueHaze:MaskableGraphic
    {
        [Range(0,1)]public float intensity;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            for(var band=0;band<8;band++)
            {
                var width=4f;var a=intensity*(1-band/8f)*.45f;
                Quad(r.xMin+band*width,r.yMin,r.xMin+(band+1)*width,r.yMax,a);
                Quad(r.xMax-(band+1)*width,r.yMin,r.xMax-band*width,r.yMax,a);
            }
            void Quad(float l,float b,float right,float t,float alpha)
            {
                var index=vh.currentVertCount;var c=new Color(.06f,.06f,.09f,alpha);
                vh.AddVert(new Vector3(l,b),c,Vector2.zero);vh.AddVert(new Vector3(l,t),c,Vector2.zero);vh.AddVert(new Vector3(right,t),c,Vector2.zero);vh.AddVert(new Vector3(right,b),c,Vector2.zero);
                vh.AddTriangle(index,index+1,index+2);vh.AddTriangle(index,index+2,index+3);
            }
        }
    }
}
