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
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            CombatInkDrawing.Target(vh,Mathf.Min(rectTransform.rect.width,rectTransform.rect.height),Style,color);
        }
    }
}
