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
            var radius=diameter*.34f;var travel=1-Mathf.Pow(1-progress,2);
            for(var sector=0;sector<4;sector++)
            {
                var start=sector*90+s.arcGap;var end=(sector+1)*90-s.arcGap;
                var direction=Vector2FromAngle(start+31);
                var offset=direction*(s.breakDistance*travel*2)+Vector2.down*(progress*progress*12);
                var alpha=1-progress;
                Arc(vh,radius,start,end,s.ringThickness*3.2f+5,s.outline*tint,offset,alpha);
                Arc(vh,radius,start,end,s.ringThickness*3.2f,s.stroke*tint,offset,alpha);
            }
            var core=s.core*tint;core.a*=1-progress;
            var black=s.outline*tint;black.a*=1-progress;
            var c=Vector2.down*progress*16;var k=s.coreRadius*4*(1-progress*.5f);
            Quad(vh,c+Vector2.left*(k+3),c+Vector2.up*(k+5),c+Vector2.right*(k+3),c+Vector2.down*(k+5),black);
            Quad(vh,c+Vector2.left*k,c+Vector2.up*(k+2),c+Vector2.right*k,c+Vector2.down*(k+2),core);
        }
        static Vector2 Vector2FromAngle(float angle)=>new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad));
        static void Arc(VertexHelper vh,float radius,float start,float end,float width,Color c,Vector2 offset,float alpha)
        {
            c.a*=alpha;
            for(var i=0;i<20;i++)
            {
                var t=i/20f;var u=(i+1)/20f;
                var w0=width*(.3f+.7f*Mathf.Sin(t*Mathf.PI));var w1=width*(.3f+.7f*Mathf.Sin(u*Mathf.PI));
                var a=Vector2FromAngle(Mathf.Lerp(start,end,t));var b=Vector2FromAngle(Mathf.Lerp(start,end,u));
                Quad(vh,offset+a*(radius-w0*.5f),offset+a*(radius+w0*.5f),offset+b*(radius+w1*.5f),offset+b*(radius-w1*.5f),c);
            }
        }
    }
}
