using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
namespace DungeonSlash
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class WeakPointReticle : MaskableGraphic
    {
        public ArcadePresentation presentation;
        public WeakPointVisualStyle style;
        public WeakPointVisualStyle Style => style != null ? style : WeakPointVisualStyle.Default;
        readonly List<WeakPointVisualStyle.Pixel> pixels = new();
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (presentation != null && !presentation.arcade) return;
            Style.BuildPixels(Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)/4,pixels);
            foreach(var pixel in pixels)
            {
                var p=pixel.position*4;var tint=pixel.color*color;
                var i = vh.currentVertCount;
                vh.AddVert(p+new Vector2(-2,-2),tint,Vector2.zero);
                vh.AddVert(p+new Vector2(-2,2),tint,Vector2.zero);
                vh.AddVert(p+new Vector2(2,2),tint,Vector2.zero);
                vh.AddVert(p+new Vector2(2,-2),tint,Vector2.zero);
                vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
            }
        }
    }
}
