using UnityEngine;
using UnityEngine.UI;
namespace DungeonSlash
{
    /// <summary>A short-lived, accurate impact radius; presentation only, never a hit resolver.</summary>
    public sealed class SpellAreaGraphic : MaskableGraphic
    {
        private float started;
        private Color tint;
        public void Initialize(float radius, Color value)
        { rectTransform.sizeDelta = Vector2.one * radius * 2; tint = value; color = value; started = Time.unscaledTime; raycastTarget = false; SetVerticesDirty(); }
        private void Update()
        { var alpha = Mathf.Clamp01(1f - (Time.unscaledTime - started) / .45f); color = new Color(tint.r, tint.g, tint.b, alpha * .7f); }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); var radius = rectTransform.rect.width * .5f;
            for (var i = 0; i < 64; i++)
            {
                var a = i * Mathf.PI / 32; var b = (i + 1) * Mathf.PI / 32; var index = mesh.currentVertCount;
                var u = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); var v = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                mesh.AddVert(u * radius, color, Vector2.zero); mesh.AddVert(v * radius, color, Vector2.zero);
                mesh.AddVert(v * (radius - 4), color, Vector2.zero); mesh.AddVert(u * (radius - 4), color, Vector2.zero);
                mesh.AddTriangle(index, index + 1, index + 2); mesh.AddTriangle(index, index + 2, index + 3);
            }
        }
    }
}
