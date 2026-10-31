using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace DungeonSlash
{
    /// <summary>A painted leaf inside the actual room aperture; the original masonry remains visible.</summary>
    public sealed class RoomOpeningPainting : MaskableGraphic
    {
        private Sprite sprite;
        private FloorArtwork.OpeningContour contour;
        private Rect source;
        private bool mirror;
        private bool sidePlane;
        public override Texture mainTexture => sprite != null ? sprite.texture : Texture2D.whiteTexture;
        public void Set(Sprite value, Rect opaqueBounds, FloorArtwork.OpeningContour opening, bool flipped, bool side)
        { sprite = value; source = opaqueBounds; contour = opening; mirror = flipped; sidePlane = side; raycastTarget = false; SetAllDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (sprite == null || contour?.left == null || contour.left.Length < 2) return;
            var uv = DataUtility.GetOuterUV(sprite); var box = contour.bounds; var rect = rectTransform.rect;
            var vanishing = FloorArtwork.RoomVanishingPoint;
            var tint = (Color32)color;
            var bottom = float.MaxValue; var top = float.MinValue;
            if (sidePlane) for (var i = 0; i < contour.left.Length; i++)
            {
                var l = PlaneY(contour.left[i]); var r = PlaneY(contour.right[i]);
                bottom = Mathf.Min(bottom, l, r); top = Mathf.Max(top, l, r);
            }
            for (var i = 0; i < contour.left.Length; i++)
            {
                Add(contour.left[i]); Add(contour.right[i]);
                if (i > 0) { var n = i * 2; vh.AddTriangle(n - 2, n - 1, n); vh.AddTriangle(n, n - 1, n + 1); }
            }
            void Add(Vector2 p)
            {
                var x = (p.x - box.xMin) / box.width; var y = (p.y - box.yMin) / box.height;
                var tx = x; var ty = y;
                if (sidePlane)
                {
                    // Horizontal boards/straps recede to the room's shared vanishing point.
                    var near = 1 / Mathf.Abs(box.xMin - vanishing.x); var far = 1 / Mathf.Abs(box.xMax - vanishing.x);
                    tx = (1 / Mathf.Abs(p.x - vanishing.x) - near) / (far - near);
                    ty = (PlaneY(p) - bottom) / Mathf.Max(.01f, top - bottom);
                }
                // The tall planar leaf is clipped by the arch, never stretched into a second arch frame.
                var u = mirror ? 1 - tx : tx;
                u = Mathf.Lerp(source.xMin, source.xMax, u); var v = Mathf.Lerp(source.yMin, source.yMax, ty);
                vh.AddVert(new Vector3(rect.xMin + x * rect.width, rect.yMin + y * rect.height), tint,
                    new Vector2(Mathf.Lerp(uv.x, uv.z, u), Mathf.Lerp(uv.y, uv.w, v)));
            }
            float PlaneY(Vector2 p) => vanishing.y + (p.y - vanishing.y) * Mathf.Abs(box.center.x - vanishing.x) / Mathf.Max(.01f, Mathf.Abs(p.x - vanishing.x));
        }
    }
}
