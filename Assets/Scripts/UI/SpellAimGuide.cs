using UnityEngine;
using UnityEngine.UI;
namespace DungeonSlash
{
    [RequireComponent(typeof(CanvasRenderer))] public sealed class SpellAimGuide:MaskableGraphic
    {
        public CombatInputController input;
        public CombatController combat;
        void LateUpdate()=>SetVerticesDirty();
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(input==null||combat==null||!combat.IsActive||combat.InputPaused||!input.SpellAim.HasValue)return;
            var aim=input.SpellAim.Value;var delta=aim.End-aim.Start;var length=delta.magnitude;var direction=length>0?delta/length:Vector2.up;
            void Line(Vector2 a,Vector2 b,float width,Color tint){var normal=new Vector2(-(b-a).normalized.y,(b-a).normalized.x)*width*.5f;var i=vh.currentVertCount;vh.AddVert(a-normal,tint,Vector2.zero);vh.AddVert(a+normal,tint,Vector2.zero);vh.AddVert(b+normal,tint,Vector2.zero);vh.AddVert(b-normal,tint,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);}
            var tint=new Color32(124,167,163,255);
            for(float d=0;d<length;d+=32)Line(aim.Start+direction*d,aim.Start+direction*Mathf.Min(d+16,length),4,tint);
            Line(aim.End+Vector2.left*20,aim.End+Vector2.right*20,12,new Color32(11,12,22,255));Line(aim.End+Vector2.down*20,aim.End+Vector2.up*20,12,new Color32(11,12,22,255));
            Line(aim.End+Vector2.left*16,aim.End+Vector2.right*16,4,tint);Line(aim.End+Vector2.down*16,aim.End+Vector2.up*16,4,tint);
        }
    }
}
