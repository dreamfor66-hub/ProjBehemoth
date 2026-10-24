using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class WeakPointShatterGraphic : MaskableGraphic
    {
        public WeakPointVisualStyle style;
        [Min(16)] public float diameter=96;
        [Range(0,1)] public float progress;
        readonly List<WeakPointVisualStyle.Pixel> pixels=new();
        public void Show(float value){progress=Mathf.Clamp01(value);SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(progress>=1)return;
            var appearance=style!=null?style:WeakPointVisualStyle.Default;
            appearance.BuildPixels(diameter/4,pixels);
            var travel=1-Mathf.Pow(1-progress,2);
            foreach(var pixel in pixels)
            {
                // Pixel dropout keeps authored red signal colors intact through the final palette pass.
                var seed=Mathf.Repeat(Mathf.Sin(Vector2.Dot(pixel.position,new Vector2(12.9898f,78.233f)))*43758.5453f,1);
                if(progress>Mathf.Lerp(.42f,1,seed))continue;
                // Arc/prong sectors separate as intact pieces, then shed individual embers.
                var angle=Mathf.Atan2(pixel.position.y,pixel.position.x);
                var sector=Mathf.Round(angle/(Mathf.PI*.25f))*(Mathf.PI*.25f);
                var direction=new Vector2(Mathf.Cos(sector),Mathf.Sin(sector));
                if(pixel.position.sqrMagnitude<4)direction=new Vector2(Mathf.Sin(angle+.6f),Mathf.Cos(angle+.6f));
                var drift=direction*(appearance.breakDistance*travel);
                drift+=new Vector2(pixel.position.y*.12f*travel,-5*progress*progress);
                if(progress>.45f)drift+=pixel.position.normalized*((progress-.45f)*5);
                var p=new Vector2(Mathf.Round(pixel.position.x+drift.x),Mathf.Round(pixel.position.y+drift.y))*4;
                var tint=pixel.color*color;
                var i=vh.currentVertCount;
                vh.AddVert(p+new Vector2(-2,-2),tint,Vector2.zero);vh.AddVert(p+new Vector2(-2,2),tint,Vector2.zero);
                vh.AddVert(p+new Vector2(2,2),tint,Vector2.zero);vh.AddVert(p+new Vector2(2,-2),tint,Vector2.zero);
                vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
            }
        }
    }
}
