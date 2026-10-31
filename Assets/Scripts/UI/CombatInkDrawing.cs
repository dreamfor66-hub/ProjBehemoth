using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    // Continuous authored curves: no integer grid, palette projection or square particles.
    public static class CombatInkDrawing
    {
        public static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
        {
            var n=vh.currentVertCount;
            vh.AddVert(a,tint,Vector2.zero);vh.AddVert(b,tint,Vector2.zero);
            vh.AddVert(c,tint,Vector2.zero);vh.AddVert(d,tint,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
        public static void Brush(VertexHelper vh,Rect r,Color ink)
        {
            // A loaded flat brush: straight body, a few long bristles at the ends.
            const int strips=18;
            for(var i=0;i<strips;i++)
            {
                var y0=r.yMin+r.height*i/strips;var y1=r.yMin+r.height*(i+1)/strips;
                var edge=Mathf.Abs((i+.5f)/strips*2-1);
                var left=r.xMin+r.width*(.015f+edge*.025f+(i%3)*.008f);
                var right=r.xMax-r.width*(edge*.055f+.008f*(1+Mathf.Sin(i*2.39f)));
                Quad(vh,new Vector2(left,y0),new Vector2(left-2,y1),new Vector2(right,y1),new Vector2(right+3,y0),ink);
            }
            var dry=ink;dry.a*=.65f;
            Quad(vh,new Vector2(r.xMin+8,r.yMax+1),new Vector2(r.xMin+25,r.yMax+3),new Vector2(r.xMax-28,r.yMax+1),new Vector2(r.xMax-4,r.yMax),dry);
            Quad(vh,new Vector2(r.xMin+18,r.yMin-2),new Vector2(r.xMin+30,r.yMin),new Vector2(r.xMax-35,r.yMin),new Vector2(r.xMax-50,r.yMin-3),dry);
        }
        public static void CountdownShadow(VertexHelper vh,Rect r,float alpha)
        {
            // A tapered ink wash holds the numeral; no second plaque or enclosing rail.
            var heights=new[]{.08f,.55f,.9f,1f,.96f,.85f,.5f,.04f};
            for(var i=0;i<heights.Length-1;i++)
            {
                var l=Mathf.Lerp(r.xMin,r.xMax,i/7f);var x=Mathf.Lerp(r.xMin,r.xMax,(i+1)/7f);
                Quad(vh,new Vector2(l,r.center.y-r.height*.5f*heights[i]),new Vector2(l,r.center.y+r.height*.46f*heights[i]),new Vector2(x,r.center.y+r.height*.46f*heights[i+1]),new Vector2(x,r.center.y-r.height*.5f*heights[i+1]),new Color(0,0,0,alpha*.8f));
            }
        }
        public static void Target(VertexHelper vh,float diameter,WeakPointVisualStyle s,Color tint,float progress=0)
        {
            // Chalk/bone paint around a red wound. Uneven loaded strokes, never an optical sight.
            var radius=diameter*.34f;var travel=1-Mathf.Pow(1-progress,2);
            for(var sector=0;sector<3;sector++)
            {
                var start=sector*120+5;var end=start+113;
                var direction=Vector2FromAngle(start+55);
                var offset=direction*(s.breakDistance*travel*2)+Vector2.down*(progress*progress*12);
                var alpha=1-progress;
                var points=new Vector2[19];
                for(var n=0;n<points.Length;n++)
                {var angle=Mathf.Lerp(start,end,n/(float)(points.Length-1));var r=radius*(1+.035f*Mathf.Sin(angle*.09f)+.02f*Mathf.Cos(angle*.17f));points[n]=offset+Vector2FromAngle(angle)*r;}
                var ink=s.outline*tint;ink.a*=alpha;var bone=s.core*tint;bone.a*=alpha;
                Stroke(vh,points,s.ringThickness*4.2f+6,ink);Stroke(vh,points,s.ringThickness*4.2f,bone);
            }
            var core=s.stroke*tint;core.a*=1-progress;
            var black=s.outline*tint;black.a*=1-progress;
            var c=Vector2.down*progress*16;var k=diameter*.16f*(1-progress*.5f);
            var wound=new[]{c+new Vector2(-k*.55f,-k),c+new Vector2(-k*.10f,-k*.18f),c+new Vector2(k*.10f,k*.25f),c+new Vector2(k*.55f,k)};
            Stroke(vh,wound,s.coreRadius*8+7,black);Stroke(vh,wound,s.coreRadius*8,core);
            var light=s.core*tint;light.a*=1-progress;
            Stroke(vh,new[]{c+new Vector2(-k*.65f,-k*.2f),c+new Vector2(0,k*.1f),c+new Vector2(k*.52f,k*.27f)},3,light);
        }
        public static void Stroke(VertexHelper vh,Vector2[] points,float width,Color tint)
        {
            for(var n=0;n<points.Length-1;n++)
            {
                var normal=new Vector2(-(points[n+1]-points[n]).y,(points[n+1]-points[n]).x).normalized;
                var a=Mathf.Pow(Mathf.Max(.025f,Mathf.Sin(n/(float)(points.Length-1)*Mathf.PI)),.6f);
                var b=Mathf.Pow(Mathf.Max(.025f,Mathf.Sin((n+1)/(float)(points.Length-1)*Mathf.PI)),.6f);
                Quad(vh,points[n]-normal*width*a*.42f,points[n]+normal*width*a*.58f,points[n+1]+normal*width*b*.58f,points[n+1]-normal*width*b*.42f,tint);
            }
        }
        static Vector2 Vector2FromAngle(float angle)=>new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad));
        static void Arc(VertexHelper vh,float radius,float start,float end,float width,Color c,Vector2 offset,float alpha,bool tapered=true)
        {
            c.a*=alpha;
            for(var i=0;i<20;i++)
            {
                var t=i/20f;var u=(i+1)/20f;
                var w0=tapered?width*(.3f+.7f*Mathf.Sin(t*Mathf.PI)):width;var w1=tapered?width*(.3f+.7f*Mathf.Sin(u*Mathf.PI)):width;
                var a=Vector2FromAngle(Mathf.Lerp(start,end,t));var b=Vector2FromAngle(Mathf.Lerp(start,end,u));
                Quad(vh,offset+a*(radius-w0*.5f),offset+a*(radius+w0*.5f),offset+b*(radius+w1*.5f),offset+b*(radius-w1*.5f),c);
            }
        }
    }
}
