using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    /// <summary>Draws a horizontal capsule. The RectTransform supplies length, diameter, position, and rotation.</summary>
    public sealed class CapsuleCastGraphic : MaskableGraphic
    {
        private const int CapSegments = 12;

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            var rect = GetPixelAdjustedRect();
            var radius = Mathf.Max(0f, rect.height * .5f);
            if (radius <= Mathf.Epsilon) return;

            var leftCenter = rect.xMin + radius;
            var rightCenter = Mathf.Max(leftCenter, rect.xMax - radius);
            var vertices = new System.Collections.Generic.List<Vector2>(CapSegments * 2 + 2);

            for (var index = 0; index <= CapSegments; index++)
            {
                var angle = Mathf.PI * .5f + Mathf.PI * index / CapSegments;
                vertices.Add(new Vector2(leftCenter + Mathf.Cos(angle) * radius, rect.center.y + Mathf.Sin(angle) * radius));
            }
            for (var index = 0; index <= CapSegments; index++)
            {
                var angle = -Mathf.PI * .5f + Mathf.PI * index / CapSegments;
                vertices.Add(new Vector2(rightCenter + Mathf.Cos(angle) * radius, rect.center.y + Mathf.Sin(angle) * radius));
            }

            var centerIndex = 0;
            vertexHelper.AddVert(rect.center, color, Vector2.zero);
            foreach (var vertex in vertices)
                vertexHelper.AddVert(vertex, color, Vector2.zero);

            for (var index = 0; index < vertices.Count; index++)
            {
                var next = index == vertices.Count - 1 ? 0 : index + 1;
                vertexHelper.AddTriangle(centerIndex, index + 1, next + 1);
            }
        }
    }
}
