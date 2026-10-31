using UnityEngine;
using UnityEngine.UI;
namespace DungeonSlash
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MonsterImpactGraphic : MaskableGraphic
    {
        Vector2 origin,target;
        float began=-10,duration;
        bool charged,guarded,perfect;
        public bool IsPlaying => Time.unscaledTime-began<duration;
        public void Play(Vector2 source,Vector2 destination,bool heavy,bool blocked,bool parried)
        {origin=source;target=destination;charged=heavy;guarded=blocked;perfect=parried;began=Time.unscaledTime;duration=heavy?.46f:.32f;SetVerticesDirty();}
        public void Clear(){began=-10;SetVerticesDirty();}
        protected override void Awake(){base.Awake();raycastTarget=false;}
        void Update(){if(Time.unscaledTime-began<duration+.05f)SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(!IsPlaying)return;var t=Mathf.Clamp01((Time.unscaledTime-began)/duration);var fade=1-t;
            var ink=new Color(.025f,.025f,.025f,fade*.94f);
            var tint=perfect?new Color(1,.89f,.5f,fade):guarded?new Color(.56f,.94f,1,fade):new Color(1,.28f,.14f,fade);
            void Line(Vector2 a,Vector2 b,float w,Color c){var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*w*.5f;CombatInkDrawing.Quad(vh,a-n,a+n,b+n,b-n,c);}
            void Stroke(Vector2 a,Vector2 b,float w){Line(a,b,w+7,ink);Line(a,b,w,tint);}
            var direction=(target-origin).normalized;var normal=new Vector2(-direction.y,direction.x);
            for(var n=-1;n<=1;n++)
            {
                var end=Vector2.Lerp(origin,target,Mathf.Clamp01(.75f+t*2))+normal*n*12;
                var start=Vector2.Lerp(origin,target,Mathf.Clamp01(t*1.7f))+normal*n*(charged?80:48);
                if(Vector2.Distance(start,end)>10)Stroke(start,end,charged?9:5);
            }
            var radius=(guarded?92:64)+t*(charged?140:95);
            var count=guarded?24:10;
            for(var n=0;n<count;n++)
            {
                var a=n*Mathf.PI*2/count;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                if(guarded)
                {var b=(n+1)*Mathf.PI*2/count;Stroke(target+d*radius,target+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,perfect?8:5);}
                else Stroke(target+d*(radius*.35f),target+d*(radius*(n%2==0?1.1f:.8f)),charged?10:6);
            }
            for(var n=0;n<4;n++)
            {var d=new Vector2(n<2?-1:1,n%2==0?-1:1).normalized;Stroke(origin+d*110,origin+d*(160+t*55),charged?9:5);}
        }
    }
}
