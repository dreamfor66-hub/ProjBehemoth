using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    // Deliberately spare pen-cut rails rather than polished bevels or decorative gold filigree.
    public static class InkUiDrawing
    {
        static void Quad(VertexHelper vh, float l, float b, float r, float t, Color c)
        {
            if (r <= l || t <= b) return;
            var i = vh.currentVertCount;
            vh.AddVert(new Vector3(l,b),c,Vector2.zero); vh.AddVert(new Vector3(l,t),c,Vector2.zero);
            vh.AddVert(new Vector3(r,t),c,Vector2.zero); vh.AddVert(new Vector3(r,b),c,Vector2.zero);
            vh.AddTriangle(i,i+1,i+2); vh.AddTriangle(i,i+2,i+3);
        }
        static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color c)
        {
            var n = new Vector2(-(b-a).y,(b-a).x).normalized * width * .5f;
            var i = vh.currentVertCount;
            vh.AddVert(a-n,c,Vector2.zero); vh.AddVert(a+n,c,Vector2.zero); vh.AddVert(b+n,c,Vector2.zero); vh.AddVert(b-n,c,Vector2.zero);
            vh.AddTriangle(i,i+1,i+2); vh.AddTriangle(i,i+2,i+3);
        }
        static Color Alpha(Color c,float a) { c.a *= a; return c; }
        static void Rail(VertexHelper vh, Vector2 a, Vector2 b, float width, float wear, Color c, int seed)
        {
            var length = Vector2.Distance(a,b); var pieces = Mathf.Max(1,Mathf.CeilToInt(length / 54));
            var normal = new Vector2(-(b-a).y,(b-a).x).normalized;
            for (var n=0;n<pieces;n++)
            {
                var t0=n/(float)pieces; var t1=(n+1)/(float)pieces;
                var gap = n>0 && (n+seed)%5==1 ? Mathf.Min(.015f,2.3f/Mathf.Max(1,length))*wear : 0;
                var offset = (((n*17+seed*11)%7)-3)*.17f*wear;
                Line(vh,Vector2.Lerp(a,b,t0+gap)+normal*offset,Vector2.Lerp(a,b,t1)+normal*offset,width*(.85f+((n+seed)%3)*.12f),c);
            }
        }
        static void Frame(VertexHelper vh, Rect r, float inset, float width, float wear, Color c)
        {
            var l=r.xMin+inset;var b=r.yMin+inset;var x=r.xMax-inset;var t=r.yMax-inset;
            if(x<=l||t<=b)return;
            Rail(vh,new Vector2(l,b),new Vector2(l,t),width,wear,c,1);
            Rail(vh,new Vector2(l,t),new Vector2(x,t),width,wear,c,2);
            Rail(vh,new Vector2(x,b),new Vector2(x,t),width,wear,c,3);
            Rail(vh,new Vector2(l,b),new Vector2(x,b),width,wear,c,4);
        }
        public static void Surface(VertexHelper vh,Rect r,InkUiStyle s,InkSurfaceRole role,Color resource,float titleHeight,bool focus,bool pressed,bool active,float alpha)
        {
            var edge=Alpha(focus?s.brass:s.iron,alpha*(active?1:.5f));
            var bright=Alpha(focus?s.bone:s.edge,alpha*(active?1:.45f));
            var dark=Alpha(s.paper,alpha);
            if(role==InkSurfaceRole.Divider)
            {
                Rail(vh,new Vector2(r.xMin,r.center.y),new Vector2(r.xMax,r.center.y),s.railWidth,s.wear,edge,2);return;
            }
            if(role==InkSurfaceRole.GaugeFill)
            {
                if(r.width<=2 || r.height<=2)return;
                Quad(vh,r.xMin,r.yMin,r.xMax,r.yMax,Alpha(s.recess,alpha));
                var pad=Mathf.Min(3,r.height*.13f);
                var fill=Alpha(resource,alpha);
                Quad(vh,r.xMin+1,r.yMin+pad,r.xMax-1,r.yMax-pad,fill);
                var ridge=Alpha(resource==s.hp?s.hpRidge:Color.Lerp(resource,s.bone,.3f),alpha);
                Rail(vh,new Vector2(r.xMin+2,r.yMax-pad),new Vector2(r.xMax-2,r.yMax-pad),Mathf.Min(2,r.height*.1f),s.wear,ridge,2);
                if(r.width>12) Line(vh,new Vector2(r.xMax-4,r.yMin+pad),new Vector2(r.xMax-2,r.yMax-pad),2,Alpha(s.recess,alpha));
                return;
            }
            Quad(vh,r.xMin,r.yMin,r.xMax,r.yMax,dark);
            if(role==InkSurfaceRole.Field)return;
            if(role==InkSurfaceRole.GaugeTrack)
            {
                Frame(vh,r,1,s.railWidth,s.wear,edge);
                var cap=Mathf.Min(7,r.width*.04f);
                Quad(vh,r.xMin,r.yMin,r.xMin+cap,r.yMax,edge); Quad(vh,r.xMax-cap,r.yMin,r.xMax,r.yMax,edge);
                return;
            }
            var choice=role==InkSurfaceRole.Choice;
            var slot=role==InkSurfaceRole.Slot;
            if(pressed) Quad(vh,r.xMin+3,r.yMin+3,r.xMax-3,r.yMax-3,Alpha(s.header,alpha));
            if(choice)
            {
                if(focus)Quad(vh,r.xMin+2,r.yMin+2,r.xMax-2,r.yMax-2,Alpha(pressed?s.header:new Color32(31,27,20,255),alpha));
                Rail(vh,new Vector2(r.xMin+8,r.yMin+2),new Vector2(r.xMax-8,r.yMin+2),s.railWidth,s.wear,edge,3);
                if(focus)
                {
                    Quad(vh,r.xMin+1,r.yMin+10,r.xMin+4,r.yMax-10,bright);
                    Rail(vh,new Vector2(r.xMin+8,r.yMax-2),new Vector2(r.xMax-8,r.yMax-2),s.railWidth,s.wear,edge,1);
                }
            }
            else
            {
                Frame(vh,r,2,s.railWidth,s.wear,edge);
                Frame(vh,r,slot?6:s.railGap+2,slot?1:s.railWidth*.65f,s.wear,Alpha(focus?s.brass:s.iron,alpha*.65f));
                var length=Mathf.Min(slot?13:22,Mathf.Min(r.width,r.height)*.25f);
                foreach(var sign in new[]{-1,1})
                {
                    var x=sign<0?r.xMin+2:r.xMax-2;
                    Line(vh,new Vector2(x,r.yMax-2),new Vector2(x-sign*length,r.yMax-2),s.railWidth,bright);
                    Line(vh,new Vector2(x,r.yMax-2),new Vector2(x,r.yMax-length),s.railWidth,bright);
                }
            }
            if(titleHeight>0 && r.height>titleHeight+40)
            {
                var y=r.yMax-titleHeight;
                Quad(vh,r.xMin+12,y,r.xMax-12,r.yMax-12,Alpha(s.header,alpha));
                Rail(vh,new Vector2(r.xMin+7,y),new Vector2(r.xMax-7,y),s.railWidth,s.wear,Alpha(s.brass,alpha*.85f),1);
                Rail(vh,new Vector2(r.xMin+4,r.yMax-5),new Vector2(r.xMax-4,r.yMax-5),s.railWidth,s.wear,Alpha(s.brass,alpha*.7f),2);
            }
            // Sparse scratches stay at the edge; the body remains quiet behind running text.
            if(!choice && !slot && r.width>250)
                for(var n=0;n<Mathf.Min(8,(int)(r.width/100));n++)
                {
                    var x=r.xMin+34+n*91;
                    Line(vh,new Vector2(x,r.yMax-4),new Vector2(x+5,r.yMax-8),1,Alpha(s.recess,alpha));
                }
        }
        public static void Guard(VertexHelper vh,Rect r,InkUiStyle s,float amount,float alpha,float lossTrail=-1)
        {
            Surface(vh,r,s,InkSurfaceRole.GaugeTrack,Color.white,0,false,false,true,alpha);
            if(amount<.15f)Line(vh,new Vector2(r.xMin+2,r.yMin+2),new Vector2(r.xMin+2,r.yMax-2),2,Alpha(s.brokenGuard,alpha));
            var vertical=r.height>r.width;var cells=Mathf.Max(4,s.guardCells);var gap=3f;
            var length=(vertical?r.height:r.width)-14;var cell=length/cells;
            for(var n=0;n<cells;n++)
            {
                var part=Mathf.Clamp01(amount*cells-n);
                var l=vertical?r.xMin+8:r.xMin+7+n*cell;
                var b=vertical?r.yMin+7+n*cell:r.yMin+6;
                var w=vertical?r.width-16:cell-gap;
                var h=vertical?cell-gap:r.height-12;
                Quad(vh,l,b,l+w,b+h,Alpha(s.iron,alpha*.6f));
                var trail=Mathf.Clamp01(lossTrail*cells-n);
                if(trail>part)
                {
                    var left=vertical?l+1:l+1+(w-2)*part;
                    var bottom=vertical?b+1+(h-2)*part:b+1;
                    Quad(vh,left,bottom,vertical?l+w-1:l+1+(w-2)*trail,vertical?b+1+(h-2)*trail:b+h-1,Alpha(s.brokenGuard,alpha));
                }
                if(part>0) Quad(vh,l+1,b+1,l+(vertical?w-1:(w-2)*part+1),b+(vertical?(h-2)*part+1:h-1),Alpha(amount<.15f?s.brokenGuard:s.guard,alpha));
            }
        }
        public static void Arc(VertexHelper vh,Rect r,InkUiStyle s,float degrees,float start,float amount,float thickness,Color color,bool track)
        {
            if(amount<=0)return;
            var outer=Mathf.Min(r.width*.45f,r.height*.62f);
            var center=new Vector2(0,r.yMax-r.height*.62f);
            var segments=Mathf.Max(1,Mathf.CeilToInt(32*amount));
            for(var n=0;n<segments;n++)
            {
                var a=(start-degrees*amount*n/segments)*Mathf.Deg2Rad;
                var b=(start-degrees*amount*(n+1)/segments)*Mathf.Deg2Rad;
                var radius=outer-thickness*.5f;
                var from=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;
                var to=center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius;
                Line(vh,from,to,thickness,track?s.recess:color);
                var rim=outer+((n%5)-2)*.15f*s.wear;
                Line(vh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*rim,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*rim,1.5f,track?s.iron:Color.Lerp(color,s.bone,.3f));
                if(track)Line(vh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(outer-thickness),center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*(outer-thickness),1,s.iron);
            }
        }
    }
}
