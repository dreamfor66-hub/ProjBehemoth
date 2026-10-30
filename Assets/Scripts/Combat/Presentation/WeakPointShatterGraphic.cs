using UnityEngine;
using UnityEngine.UI;
namespace DungeonSlash
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class WeakPointShatterGraphic : MaskableGraphic
    {
        public WeakPointVisualStyle style;
        [Min(16)] public float diameter=96;
        [Range(0,1)] public float progress;
        WeakPointVisualStyle Style => style!=null?style:WeakPointVisualStyle.Default;
        public override Material defaultMaterial => Style.material!=null?Style.material:base.defaultMaterial;
        public void Show(float value){progress=Mathf.Clamp01(value);SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(progress>=1)return;
            CombatInkDrawing.Target(vh,diameter,Style,color,progress);
        }
    }
}
