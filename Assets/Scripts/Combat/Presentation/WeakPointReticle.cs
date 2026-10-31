using UnityEngine;
using UnityEngine.UI;
namespace DungeonSlash
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class WeakPointReticle : MaskableGraphic
    {
        public ArcadePresentation presentation;
        public WeakPointVisualStyle style;
        public WeakPointVisualStyle Style => style != null ? style : WeakPointVisualStyle.Default;
        public override Material defaultMaterial => Style.material != null ? Style.material : base.defaultMaterial;
        float hitAt = float.NegativeInfinity;
        public void PlayHit(){hitAt=Time.unscaledTime;SetVerticesDirty();}
        void Update(){if(Application.isPlaying)SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var pulse=Application.isPlaying?(1+Mathf.Sin(Time.unscaledTime*5.5f))*.5f:0;
            var hit=Application.isPlaying?Mathf.Clamp01(1-(Time.unscaledTime-hitAt)/.16f):0;
            CombatInkDrawing.Target(vh,Mathf.Min(rectTransform.rect.width,rectTransform.rect.height),Style,color,pulse,hit);
        }
    }
}
