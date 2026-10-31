using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    // Shared attack/charge recovery; placement is independently editable on AttackCooldownAnchor.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class AttackRecoveryGauge : MaskableGraphic
    {
        public Color readyColor=new Color32(224,212,182,255);
        public Color recoveryColor=new Color32(168,132,76,255);
        public Color emptyColor=new Color32(49,47,42,255);
        public float Progress { get; private set; }=1;
        public bool IsRecovering { get; private set; }
        float readyUntil;
        protected override void Awake(){base.Awake();raycastTarget=false;}
        public void Bind(PlayerCombatRuntime player)
        {
            var recovering=player.AttackCooldownRemaining>0;
            if(IsRecovering&&!recovering)readyUntil=Time.unscaledTime+.18f;
            IsRecovering=recovering;
            Progress=recovering?Mathf.Clamp01(1-player.AttackCooldownRemaining/Mathf.Max(.001f,player.CurrentCooldown)):1;
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;var x=r.xMin;var y=r.center.y;var w=r.width;var h=r.height;
            var black=new Color32(5,5,5,255);
            var ink=IsRecovering?recoveryColor:Time.unscaledTime<readyUntil?Color.white:readyColor;
            void Polygon(Color c,params Vector2[] points)
            {
                var start=vh.currentVertCount;
                foreach(var p in points)vh.AddVert(new Vector3(p.x,p.y),c,Vector2.zero);
                for(var i=1;i<points.Length-1;i++)vh.AddTriangle(start,start+i,start+i+1);
            }
            void Rect(float left,float bottom,float width,float height,Color c)=>Polygon(c,new(left,bottom),new(left,bottom+height),new(left+width,bottom+height),new(left+width,bottom));
            // Broad black rim, a quiet empty blade and a progressing ivory/amber inner face.
            Polygon(black,new(x+w*.25f,y-h*.29f),new(x+w*.86f,y-h*.29f),new(x+w,y),new(x+w*.86f,y+h*.29f),new(x+w*.25f,y+h*.29f));
            Polygon(emptyColor,new(x+w*.28f,y-h*.17f),new(x+w*.86f,y-h*.17f),new(x+w*.96f,y),new(x+w*.86f,y+h*.17f),new(x+w*.28f,y+h*.17f));
            var end=x+w*(.28f+.68f*Progress);
            if(Progress>0)Polygon(ink,new(x+w*.28f,y-h*.13f),new(Mathf.Max(x+w*.28f,end-w*.06f),y-h*.13f),new(end,y),new(Mathf.Max(x+w*.28f,end-w*.06f),y+h*.13f),new(x+w*.28f,y+h*.13f));
            Rect(x+w*.04f,y-h*.14f,w*.20f,h*.28f,black);
            Rect(x+w*.06f,y-h*.065f,w*.16f,h*.13f,IsRecovering?emptyColor:readyColor);
            Rect(x+w*.23f,y-h*.46f,w*.055f,h*.92f,black);
            Rect(x+w*.242f,y-h*.36f,w*.025f,h*.72f,ink);
        }
    }
}
