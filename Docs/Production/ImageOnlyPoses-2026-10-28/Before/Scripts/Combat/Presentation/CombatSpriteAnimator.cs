using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    [RequireComponent(typeof(Image))]
    public sealed class CombatSpriteAnimator : BaseMeshEffect
    {
        public ArcadePresentation presentation;
        public CombatSpriteAnimation library;
        public CombatVisualState State {get;private set;}
        private Image target;
        private float changedAt;
        public bool Available=>presentation!=null&&presentation.arcade&&library!=null;
        protected override void Awake(){base.Awake();target=GetComponent<Image>();}
        public void Play(CombatSpriteAnimation source,CombatVisualState state,bool restart=false)
        {
            var previous=library;
            library=source;
            if(!Available)return;
            if(State!=state||restart)changedAt=Time.time;
            State=state;
            target=target!=null?target:GetComponent<Image>();
            target.sprite=library.For(state)??library.idle;
            if(previous!=library||library.illustratedMotion||library.emphasizePoses)target.SetVerticesDirty();
        }
        private void LateUpdate(){if(Available&&(library.illustratedMotion||library.emphasizePoses))target?.SetVerticesDirty();}
        public override void ModifyMesh(VertexHelper vertices)
        {
            if(!IsActive()||!Available||(!library.illustratedMotion&&!library.emphasizePoses&&Mathf.Approximately(library.canvasScale,1f)))return;
            // Grow only the rendered quad around the authored cell center. Layout,
            // collision/guard rectangles and the RectTransform remain unchanged.
            var center=(Vector3)((RectTransform)transform).rect.center;
            var vertex=new UIVertex();
            var angle=0f;var offset=Vector3.zero;var scale=library.canvasScale;
            var poseScale = Vector2.one;
            if(library.illustratedMotion)
            {
                var t=Time.time-changedAt;
                if(State==CombatVisualState.Ready){angle=library.readyTilt;scale*=1.025f;}
                else if(State==CombatVisualState.Guard){angle=-3f;offset=Vector3.up*12f;scale*=1.025f;}
                else if(State==CombatVisualState.Attack){var strike=Mathf.Sin(Mathf.Clamp01(t/.3f)*Mathf.PI);angle=library.attackTilt*strike;offset=library.attackOffset*strike;}
                else if(State==CombatVisualState.Dead){angle=12f;offset=Vector3.down*16;scale*=.93f;}
            }
            if (library.emphasizePoses)
            {
                // Use the separate state painting and exaggerate its silhouette. No gameplay transform is moved.
                if (State == CombatVisualState.Ready)
                { angle = library.readyTilt; offset = library.readyOffset; poseScale = library.readyScale; }
                else if (State == CombatVisualState.Attack)
                {
                    var phase = Mathf.Clamp01((Time.time - changedAt) / library.strikeSeconds);
                    var strike = phase < .18f ? phase / .18f : 1f - Mathf.SmoothStep(0f, 1f, (phase - .18f) / .82f);
                    angle = library.attackTilt * strike; offset = library.attackOffset * strike;
                    poseScale = Vector2.Lerp(Vector2.one, library.strikeScale, strike);
                }
            }
            var rotation=Quaternion.Euler(0,0,angle);
            for(var i=0;i<vertices.currentVertCount;i++)
            {vertices.PopulateUIVertex(ref vertex,i);var local=(vertex.position-center)*scale;local.x*=poseScale.x;local.y*=poseScale.y;vertex.position=center+rotation*local+offset;vertices.SetUIVertex(vertex,i);}
        }
    }
}
