using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    /// <summary>A shallow ink contact patch, sized from the painting's physical footprint.</summary>
    public sealed class RoomContactShadow : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = rectTransform.rect;
            var center = (Color32)color; var edge = center; edge.a = 0;
            vh.AddVert(r.center, center, Vector2.zero);
            const int count = 16;
            for (var i = 0; i <= count; i++)
            {
                var a = i * Mathf.PI * 2 / count;
                vh.AddVert(r.center + new Vector2(Mathf.Cos(a) * r.width * .5f, Mathf.Sin(a) * r.height * .5f), edge, Vector2.zero);
                if (i > 0) vh.AddTriangle(0, i, i + 1);
            }
        }
    }
}
