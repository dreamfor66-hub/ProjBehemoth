using UnityEngine;
using UnityEngine.UI;
namespace DungeonSlash
{
    // Pixel-aligned parallelogram; the fill rises inside the same slanted outline.
    [RequireComponent(typeof(CanvasRenderer))] public sealed class SlantedGuardGauge : MaskableGraphic
    {
        [Range(0,1)] public float amount=1;
        public float slant=32, border=4;
        public Color fillColor=new Color32(176,176,176,255);
        public InkUiStyle inkStyle;
        [Min(0)] public float lossHoldSeconds=.55f;
        [Min(.01f)] public float lossFadeSeconds=.6f;
        public float LossTrail {get;private set;}=1;
        public bool Guarding {get;private set;}
        public bool Broken {get;private set;}
        bool initialized;
        float lossUntil;
        int inkRevision=-1;
        protected override void OnEnable(){base.OnEnable();initialized=false;raycastTarget=false;}
        void LateUpdate()
        {
            if(inkStyle!=null&&inkRevision!=inkStyle.RenderRevision){inkRevision=inkStyle.RenderRevision;SetVerticesDirty();}
            if(Time.unscaledTime>lossUntil&&LossTrail>amount){LossTrail=Mathf.MoveTowards(LossTrail,amount,Time.unscaledDeltaTime/lossFadeSeconds);SetVerticesDirty();}
        }
        public void SetValue(float value,bool guarding=false,bool broken=false)
        {
            value=Mathf.Clamp01(value);
            if(!initialized){amount=LossTrail=value;initialized=true;}
            if(value<amount){LossTrail=Mathf.Max(LossTrail,amount);lossUntil=Time.unscaledTime+lossHoldSeconds;}
            else if(value>LossTrail)LossTrail=value;
            if(Mathf.Approximately(amount,value)&&Guarding==guarding&&Broken==broken)return;
            amount=value;Guarding=guarding;Broken=broken;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            if(inkStyle!=null){InkUiDrawing.Guard(vh,r,inkStyle,amount,color.a,LossTrail);if(Guarding)CombatInkDrawing.Quad(vh,new Vector2(r.xMin+7,r.yMax-1),new Vector2(r.xMin+7,r.yMax+1),new Vector2(r.xMax-7,r.yMax+1),new Vector2(r.xMax-7,r.yMax-1),inkStyle.bone);return;}
            void Quad(float y0,float y1,float inset,Color tint)
            {
                var x0=Mathf.Round((Mathf.InverseLerp(r.yMin,r.yMax,y0)-.5f)*slant/4)*4;
                var x1=Mathf.Round((Mathf.InverseLerp(r.yMin,r.yMax,y1)-.5f)*slant/4)*4;
                var i=vh.currentVertCount;vh.AddVert(new Vector3(r.xMin+x0+inset,y0),tint,Vector2.zero);vh.AddVert(new Vector3(r.xMin+x1+inset,y1),tint,Vector2.zero);vh.AddVert(new Vector3(r.xMax+x1-inset,y1),tint,Vector2.zero);vh.AddVert(new Vector3(r.xMax+x0-inset,y0),tint,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
            }
            Quad(r.yMin,r.yMax,0,new Color32(40,40,40,255));
            Quad(r.yMin+border,r.yMax-border,border,new Color32(72,72,72,255));
            var top=r.yMin+border+Mathf.Round((r.height-border*2)*amount/4)*4;
            if(top>r.yMin+border)Quad(r.yMin+border,top,border,fillColor);
        }
    }
}
