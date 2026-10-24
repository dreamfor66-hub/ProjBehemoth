using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ArcGaugeGraphic : MaskableGraphic
    {
        [SerializeField, Range(1f, 180f)] private float arcDegrees = 100f;
        [SerializeField, Range(0f, 1f)] private float fillAmount = 1f;
        [SerializeField, Min(1f)] private float thickness = 12f;
        [SerializeField] private float startAngle = 150f;

        public float FillAmount => fillAmount;

        public void Configure(float newArcDegrees, float newFillAmount)
        {
            arcDegrees = newArcDegrees;
            fillAmount = Mathf.Clamp01(newFillAmount);
            raycastTarget = false;
            SetVerticesDirty();
        }

        public void SetFillAmount(float value)
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(fillAmount, value)) return;
            fillAmount = value;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (fillAmount <= 0f) return;

            var rect = rectTransform.rect;
            var outerRadius = Mathf.Min(rect.width * .45f, rect.height * .62f);
            var innerRadius = Mathf.Max(1f, outerRadius - thickness);
            var center = new Vector2(0f, rect.yMax - rect.height * .62f);
            var sweep = -arcDegrees * fillAmount;
            var segments = Mathf.Max(1, Mathf.CeilToInt(32f * fillAmount));

            for (var index = 0; index <= segments; index++)
            {
                var progress = index / (float)segments;
                var angle = (startAngle + sweep * progress) * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                AddVertex(vertexHelper, center + direction * outerRadius);
                AddVertex(vertexHelper, center + direction * innerRadius);
            }

            for (var index = 0; index < segments; index++)
            {
                var vertex = index * 2;
                vertexHelper.AddTriangle(vertex, vertex + 2, vertex + 1);
                vertexHelper.AddTriangle(vertex + 1, vertex + 2, vertex + 3);
            }
        }

        private void AddVertex(VertexHelper vertexHelper, Vector2 position)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = color;
            vertexHelper.AddVert(vertex);
        }
    }
}
