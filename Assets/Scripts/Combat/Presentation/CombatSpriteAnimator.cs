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
        public SwordSwingDirection SwingDirection {get;private set;}
        private Image target;
        public bool Available=>presentation!=null&&presentation.arcade&&library!=null;
        protected override void Awake(){base.Awake();target=GetComponent<Image>();}
        public void Play(CombatSpriteAnimation source,CombatVisualState state,bool restart=false,SwordSwingDirection direction=SwordSwingDirection.RightToLeft)
        {
            library=source;
            if(!Available)return;
            State=state;
            SwingDirection=direction;
            target=target!=null?target:GetComponent<Image>();
            // Unity's destroyed/missing asset wrappers can be CLR-non-null. Use Unity's
            // null comparison rather than ?? so an unassigned pose never draws a white quad.
            var sprite=library.For(state,direction);
            if(sprite==null)sprite=library.idle;
            target.overrideSprite=null;
            target.sprite=sprite;
            target.enabled=sprite!=null;
            target.SetVerticesDirty();
        }
        public override void ModifyMesh(VertexHelper vertices)
        {
            if(!IsActive()||!Available)return;
            // Constant transparent-canvas compensation only. All poses use the same
            // scale; attack/ready/guard never rotate, move, squash or tween the mesh.
            var rect=((RectTransform)transform).rect;
            var center=(Vector3)rect.center;
            var scale=library.canvasScale;
            var offset=Vector3.zero;
            var padding=library.canvasPaddingPixels;
            target=target!=null?target:GetComponent<Image>();
            if(target.sprite!=null && padding!=Vector4.zero)
            {
                var expanded=target.sprite.rect.size;
                var original=expanded-new Vector2(padding.x+padding.z,padding.y+padding.w);
                if(original.x>0 && original.y>0 && rect.width>0 && rect.height>0)
                {
                    // Image.preserveAspect fits the expanded PNG. Restore the old pixel
                    // scale, then undo the center shift caused by asymmetric top padding.
                    var oldPixelScale=target.preserveAspect?Mathf.Min(rect.width/original.x,rect.height/original.y):1f;
                    var newPixelScale=target.preserveAspect?Mathf.Min(rect.width/expanded.x,rect.height/expanded.y):1f;
                    if(target.preserveAspect)
                    {
                        scale*=oldPixelScale/newPixelScale;
                        offset=new Vector3(padding.z-padding.x,padding.w-padding.y,0)*(.5f*oldPixelScale*library.canvasScale);
                    }
                }
            }
            if(Mathf.Approximately(scale,1f)&&offset==Vector3.zero)return;
            var vertex=new UIVertex();
            for(var i=0;i<vertices.currentVertCount;i++)
            {vertices.PopulateUIVertex(ref vertex,i);vertex.position=center+(vertex.position-center)*scale+offset;vertices.SetUIVertex(vertex,i);}
        }
    }
}
