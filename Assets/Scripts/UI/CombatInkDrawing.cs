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
        public static void Target(VertexHelper vh,float diameter,WeakPointVisualStyle s,Color tint,float pulse=0,float hit=0)
        {
            // A painted bullseye: broad interrupted brush marks and a single unmistakable hit centre.
            // Opaque black separation survives both pale bone/metal and dark fur underneath it.
            var scale=diameter/96f;var radius=diameter*.34f;
            var ink=s.outline*tint;var bone=s.core*tint;var red=s.stroke*tint;
            var width=s.ringThickness*5.2f*scale;
            for(var sector=0;sector<4;sector++)
            {
                var start=sector*90+s.arcGap*.5f+3;
                var end=(sector+1)*90-s.arcGap*.5f+3;
                TargetArc(vh,radius,start,end,width+7*scale,ink,Vector2.zero,0);
                TargetArc(vh,radius,start,end,width,bone,Vector2.zero,0);
            }
            // Two short inward loaded-brush tips point at the centre, rather than resembling a wound.
            for(var side=0;side<2;side++)
            {
                var direction=Vector2FromAngle(side*180+3);var normal=new Vector2(-direction.y,direction.x);
                var tip=direction*(radius-13*scale-pulse*1.5f*scale);
                Triangle(vh,direction*(radius+3*scale)+normal*7*scale,direction*(radius+3*scale)-normal*7*scale,tip-direction*3*scale,ink);
                Triangle(vh,direction*radius+normal*4*scale,direction*radius-normal*4*scale,tip,bone);
            }
            var coreRadius=(10.5f*s.coreRadius+1.2f*pulse+3*hit)*scale;
            Blob(vh,Vector2.zero,coreRadius+4*scale,ink);
            Blob(vh,Vector2.zero,coreRadius,Color.Lerp(red,bone,hit));
            Blob(vh,new Vector2(-.7f,1.1f)*scale,coreRadius*.35f,bone);
        }

        public static void Shatter(VertexHelper vh,float diameter,WeakPointVisualStyle s,Color tint,float progress)
        {
            if(progress>=1)return;
            var scale=diameter/96f;var radius=diameter*.34f;
            var travel=1-Mathf.Pow(1-progress,4); // Immediate impulse, then drag; no slow expanding fade.
            var fade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.42f,1,progress));
            var ink=s.outline*tint;ink.a*=fade;
            var bone=s.core*tint;bone.a*=fade;
            var red=s.stroke*tint;red.a*=fade;
            var distance=s.breakDistance*scale;

            // The actual painted ring breaks into eight recognizable, independently thrown pieces.
            for(var piece=0;piece<8;piece++)
            {
                var sector=piece/2;var half=piece%2;
                var start=sector*90+s.arcGap*.5f+3+half*(45-s.arcGap*.5f);
                var end=start+42-s.arcGap*.5f;
                var angle=(start+end)*.5f;var direction=Vector2FromAngle(angle);
                var offset=direction*(distance*travel*(.68f+(piece%3)*.14f))+Vector2.down*(progress*progress*34*scale);
                var spin=(piece%2==0?1:-1)*progress*(35+piece*7);
                var width=s.ringThickness*5.2f*scale*(1-progress*.35f);
                TargetArc(vh,radius,start,end,width+6*scale,ink,offset,spin);
                TargetArc(vh,radius,start,end,width,piece%3==0?red:bone,offset,spin);
            }

            // Broad tapered streaks and a few solid chips, kept asymmetric and free of tiny debris noise.
            for(var i=0;i<9;i++)
            {
                var direction=Vector2FromAngle(i*40+13+(i%3)*6);var normal=new Vector2(-direction.y,direction.x);
                var reach=radius+distance*travel*(.8f+(i%4)*.16f);
                var centre=direction*reach+Vector2.down*(progress*progress*24*scale);
                var length=(17+(i%3)*6)*scale*(1-progress*.65f);
                var width=(6+(i%2)*3)*scale*(1-progress*.55f);
                Chip(vh,centre,direction,normal,length+3*scale,width+2*scale,ink);
                Chip(vh,centre,direction,normal,length,width,i%3==1?red:bone);
                if(progress<.48f)
                {
                    var tail=direction*Mathf.Max(12*scale,reach-(42+18*travel)*scale);
                    var a=centre-direction*length*.6f;
                    var line=new[]{tail,Vector2.Lerp(tail,a,.55f)+normal*2*scale,a};
                    Stroke(vh,line,11*scale*(1-progress),ink);
                    Stroke(vh,line,7*scale*(1-progress),i%3==1?red:bone);
                }
            }
            // Brief local impact star. It collapses while the solid fragments continue outwards.
            var flash=1-Mathf.InverseLerp(.08f,.34f,progress);
            if(flash>0)
            {
                Star(vh,(38+travel*32)*scale,17*scale,ink,flash);
                Star(vh,(34+travel*28)*scale,13*scale,red,flash);
                Star(vh,(26+travel*22)*scale,9*scale,bone,flash);
            }
        }

        static void TargetArc(VertexHelper vh,float radius,float start,float end,float width,Color tint,Vector2 offset,float spin)
        {
            var centre=Vector2FromAngle((start+end)*.5f)*radius;
            for(var i=0;i<16;i++)
            {
                var t=i/16f;var u=(i+1)/16f;
                Vector2 Point(float v,float edge)
                {
                    var angle=Mathf.Lerp(start,end,v);
                    var r=radius*(1+.025f*Mathf.Sin(angle*.11f)+.018f*Mathf.Cos(angle*.19f));
                    var w=width*(.52f+.48f*Mathf.Sin(v*Mathf.PI));
                    var p=Vector2FromAngle(angle)*(r+edge*w)-centre;
                    var rotation=spin*Mathf.Deg2Rad;
                    return offset+centre+new Vector2(p.x*Mathf.Cos(rotation)-p.y*Mathf.Sin(rotation),p.x*Mathf.Sin(rotation)+p.y*Mathf.Cos(rotation));
                }
                Quad(vh,Point(t,-.5f),Point(t,.5f),Point(u,.5f),Point(u,-.5f),tint);
            }
        }
        static void Triangle(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Color tint)
        {var n=vh.currentVertCount;vh.AddVert(a,tint,Vector2.zero);vh.AddVert(b,tint,Vector2.zero);vh.AddVert(c,tint,Vector2.zero);vh.AddTriangle(n,n+1,n+2);}
        static void Blob(VertexHelper vh,Vector2 centre,float radius,Color tint)
        {
            for(var i=0;i<24;i++)
            {
                Vector2 Point(int n){var angle=n*15f;return centre+Vector2FromAngle(angle)*(radius*(1+.045f*Mathf.Sin(angle*.09f)));}
                Triangle(vh,centre,Point(i),Point(i+1),tint);
            }
        }
        static void Chip(VertexHelper vh,Vector2 centre,Vector2 direction,Vector2 normal,float length,float width,Color tint)
        {Quad(vh,centre-direction*length*.65f,centre+normal*width,centre+direction*length*.6f,centre-normal*width*.7f,tint);}
        static void Star(VertexHelper vh,float outer,float inner,Color tint,float alpha)
        {
            tint.a*=alpha;
            for(var i=0;i<16;i++)
            {
                Vector2 Point(int n)=>Vector2FromAngle(n*22.5f+8)*(n%2==0?outer*(.78f+(n%3)*.11f):inner);
                Triangle(vh,Vector2.zero,Point(i),Point(i+1),tint);
            }
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
