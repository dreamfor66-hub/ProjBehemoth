using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    /// <summary>A tapered ink edge follows the real attack segment; it never changes its hit shape.</summary>
    [RequireComponent(typeof(Image))]
    public sealed class SlashRibbonEffect : BaseMeshEffect
    {
        public Color edgeColor = new(.91f, .88f, .76f, 1f);
        public Color chargedColor = new(1f, .73f, .32f, 1f);
        [Range(.05f, .7f)] public float curve = .28f;
        [Range(.1f, 2f)] public float thickness = .65f;
        private bool charged;
        private float progress;

        public void SetStroke(bool isCharged, float age)
        { charged = isCharged; progress = Mathf.Clamp01(age); graphic?.SetVerticesDirty(); }

        public override void ModifyMesh(VertexHelper mesh)
        {
            if (!IsActive()) return;
            mesh.Clear();
            var rect = ((RectTransform)transform).rect;
            var fade = 1f - Mathf.SmoothStep(0f, 1f, progress);
            var edge = charged ? chargedColor : edgeColor;
            DrawBand(mesh, rect, new Color(.025f, .021f, .018f, fade * .85f), 1.65f, -rect.height * .12f);
            edge.a *= fade;
            DrawBand(mesh, rect, edge, 1f, 0);
            var core = Color.Lerp(edge, Color.white, .65f); core.a *= .85f;
            DrawBand(mesh, rect, core, .23f, rect.height * .03f);
        }

        private void DrawBand(VertexHelper mesh, Rect rect, Color tint, float width, float offset)
        {
            const int samples = 24;
            var start = mesh.currentVertCount;
            for (var i = 0; i <= samples; i++)
            {
                var t = (float)i / samples;
                var taper = Mathf.Pow(Mathf.Max(0, Mathf.Sin(t * Mathf.PI)), .7f);
                var x = Mathf.Lerp(rect.xMin, rect.xMax, t);
                var y = rect.center.y + Mathf.Sin(t * Mathf.PI) * rect.height * curve + offset;
                var half = rect.height * thickness * width * taper * .5f * (charged ? 1.2f : 1f);
                mesh.AddVert(new Vector3(x, y - half), tint, Vector2.zero);
                mesh.AddVert(new Vector3(x, y + half), tint, Vector2.one);
                if (i == 0) continue;
                var n = start + i * 2;
                mesh.AddTriangle(n - 2, n - 1, n); mesh.AddTriangle(n, n - 1, n + 1);
            }
        }
    }
}
