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
        public bool shieldSilhouette = true;
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
            if(shieldSilhouette){DrawShield(vh,r);return;}
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
        void DrawShield(VertexHelper vh,Rect r)
        {
            var edge=Broken?new Color32(255,91,73,255):Guarding?Color.white:new Color32(159,230,243,255);
            void Poly(Color c,params Vector2[] points)
            {var i=vh.currentVertCount;foreach(var p in points)vh.AddVert(p,c,Vector2.zero);for(var k=1;k<points.Length-1;k++)vh.AddTriangle(i,i+k,i+k+1);}
            var h=r.height;var w=r.width;var y=r.center.y;var x=r.xMin;
            Poly(new Color32(5,9,13,250),new(x,y-h*.33f),new(x,y+h*.33f),new(x+w*.12f,y+h*.50f),new(r.xMax,y+h*.50f),new(r.xMax,y-h*.50f),new(x+w*.12f,y-h*.50f));
            Poly(edge,new(x+5,y-h*.26f),new(x+5,y+h*.26f),new(x+w*.12f,y+h*.41f),new(r.xMax-5,y+h*.41f),new(r.xMax-5,y-h*.41f),new(x+w*.12f,y-h*.41f));
            Poly(new Color32(12,24,31,255),new(x+10,y-h*.21f),new(x+10,y+h*.21f),new(x+w*.13f,y+h*.32f),new(r.xMax-10,y+h*.32f),new(r.xMax-10,y-h*.32f),new(x+w*.13f,y-h*.32f));
            var l=x+w*.15f;var right=r.xMax-14;var bottom=y-h*.23f;var top=y+h*.23f;
            void Fill(float fraction,Color c){if(fraction<=0)return;CombatInkDrawing.Quad(vh,new(l,bottom),new(l,top),new(Mathf.Lerp(l,right,fraction),top),new(Mathf.Lerp(l,right,fraction),bottom),c);}
            Fill(LossTrail,new Color32(243,92,74,255));
            Fill(amount,Broken?new Color32(181,69,57,255):Guarding?new Color32(207,250,255,255):new Color32(80,199,223,255));
            for(var n=1;n<5;n++){var t=Mathf.Lerp(l,right,n/5f);CombatInkDrawing.Quad(vh,new(t-1,bottom),new(t-1,top),new(t+1,top),new(t+1,bottom),new Color32(8,24,31,220));}
        }
    }
}
