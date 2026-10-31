using UnityEngine;
using UnityEngine.UI;
namespace DungeonSlash
{
    // Hand-cut iron shield and an unsegmented bone-coloured reserve.
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
            var edge=Broken?new Color32(143,67,48,255):Guarding?new Color32(224,208,161,255):new Color32(124,112,82,255);
            void Poly(Color c,params Vector2[] points)
            {var i=vh.currentVertCount;foreach(var p in points)vh.AddVert(p,c,Vector2.zero);for(var k=1;k<points.Length-1;k++)vh.AddTriangle(i,i+k,i+k+1);}
            var h=r.height;var w=r.width;var y=r.center.y;var x=r.xMin;
            var black=new Color32(7,7,6,255);
            Poly(black,new(x+w*.08f,y-h*.50f),new(x,y-h*.12f),new(x+2,y+h*.46f),new(x+w*.15f,y+h*.39f),new(x+w*.16f,y-h*.09f));
            Poly(edge,new(x+w*.08f,y-h*.36f),new(x+5,y-h*.08f),new(x+6,y+h*.31f),new(x+w*.125f,y+h*.26f),new(x+w*.13f,y-h*.05f));
            Poly(new Color32(54,51,43,255),new(x+w*.08f,y-h*.24f),new(x+9,y-h*.04f),new(x+9,y+h*.22f),new(x+w*.10f,y+h*.18f),new(x+w*.105f,y-h*.02f));
            var l=x+w*.19f;var right=r.xMax-3;
            var track=new Rect(l,y-h*.28f,right-l,h*.56f);
            CombatInkDrawing.Brush(vh,new Rect(track.x-5,track.y-5,track.width+10,track.height+10),black);
            Poly(edge,new(l-1,y-h*.31f),new(l+2,y+h*.30f),new(right,y+h*.27f),new(right-2,y-h*.28f));
            Poly(new Color32(30,28,24,255),new(l+3,y-h*.21f),new(l+4,y+h*.20f),new(right-3,y+h*.18f),new(right-5,y-h*.19f));
            void Fill(float fraction,Color c){if(fraction<=0)return;var end=Mathf.Lerp(l+4,right-5,fraction);Poly(c,new(l+4,y-h*.16f),new(l+4,y+h*.15f),new(end,y+h*.13f),new(end-1,y-h*.15f));}
            Fill(LossTrail,new Color32(154,74,49,255));
            Fill(amount,Broken?new Color32(126,54,39,255):Guarding?new Color32(233,218,177,255):new Color32(191,177,137,255));
        }
    }
}
