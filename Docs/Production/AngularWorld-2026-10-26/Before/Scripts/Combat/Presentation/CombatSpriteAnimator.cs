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
        public bool Available=>presentation!=null&&presentation.arcade&&library!=null;
        protected override void Awake(){base.Awake();target=GetComponent<Image>();}
        public void Play(CombatSpriteAnimation source,CombatVisualState state,bool restart=false)
        {
            var previous=library;
            library=source;
            if(!Available)return;
            State=state;
            target=target!=null?target:GetComponent<Image>();
            target.sprite=library.For(state)??library.idle;
            if(previous!=library)target.SetVerticesDirty();
        }
        public override void ModifyMesh(VertexHelper vertices)
        {
            if(!IsActive()||!Available||Mathf.Approximately(library.canvasScale,1f))return;
            // Grow only the rendered quad around the authored cell center. Layout,
            // collision/guard rectangles and the RectTransform remain unchanged.
            var center=(Vector3)((RectTransform)transform).rect.center;
            var vertex=new UIVertex();
            for(var i=0;i<vertices.currentVertCount;i++)
            {vertices.PopulateUIVertex(ref vertex,i);vertex.position=center+(vertex.position-center)*library.canvasScale;vertices.SetUIVertex(vertex,i);}
        }
    }
}
