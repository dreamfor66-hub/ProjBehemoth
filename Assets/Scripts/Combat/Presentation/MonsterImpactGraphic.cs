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
            var tint=perfect?new Color(.93f,.80f,.48f,fade):guarded?new Color(.84f,.78f,.62f,fade):new Color(.70f,.18f,.105f,fade);
            void Stroke(Vector2[] points,float w){CombatInkDrawing.Stroke(vh,points,w+11,ink);CombatInkDrawing.Stroke(vh,points,w,tint);}
            var direction=(target-origin).normalized;var normal=new Vector2(-direction.y,direction.x);
            // A single broad curved strike with a dry companion stroke; no beams or circular shockwave.
            for(var n=0;n<2;n++)
            {
                var points=new Vector2[13];
                for(var p=0;p<points.Length;p++)
                {var u=p/(float)(points.Length-1);var along=Mathf.Lerp(t*.65f,Mathf.Clamp01(.86f+t),u);points[p]=Vector2.Lerp(origin,target,along)+normal*(Mathf.Sin(u*Mathf.PI)*(charged?64:38)+n*18);}
                Stroke(points,(charged?30:20)*(n==0?1:.24f));
            }
            var radius=(guarded?58:48)+t*(charged?100:65);
            var count=guarded?5:3;
            for(var n=0;n<count;n++)
            {
                var angle=(-155+n*67+(n%2)*13)*Mathf.Deg2Rad;var d=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                var side=new Vector2(-d.y,d.x);
                Stroke(new[]{target+d*radius*.30f,target+d*radius*.65f+side*8,target+d*radius*(n%2==0?1.2f:.85f)+side*19},charged?13:8);
            }
        }
    }
}
