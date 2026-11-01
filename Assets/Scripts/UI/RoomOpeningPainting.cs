using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace DungeonSlash
{
    /// <summary>A wall-projected painting, either an inset leaf or a complete entrance facade.</summary>
    public sealed class RoomOpeningPainting : MaskableGraphic
    {
        private Sprite sprite;
        private FloorArtwork.OpeningContour contour;
        private Rect source;
        private bool mirror;
        private bool sidePlane;
        private bool fullFrame;
        private float frameWidthFactor=1.68f;
        private Vector2 frameGround;
        public override Texture mainTexture => sprite != null ? sprite.texture : Texture2D.whiteTexture;
        public void Set(Sprite value, Rect opaqueBounds, FloorArtwork.OpeningContour opening, bool flipped, bool side, bool frame = false, float frameWidth = 1.68f, Vector2? ground = null)
        { sprite = value; source = opaqueBounds; contour = opening; mirror = flipped; sidePlane = side; fullFrame=frame; frameWidthFactor=frameWidth; frameGround=ground??new Vector2(.5f,.35f); raycastTarget = false; SetAllDirty(); }
        public static Vector2[] FrameCorners(FloorArtwork.OpeningContour opening, bool side, float widthFactor = 1.68f, Vector2? ground = null)
        {
            var b=opening.bounds;var vp=FloorArtwork.RoomVanishingPoint;
            var x0=b.xMin-b.width*(widthFactor-1)*.5f;var x1=b.xMax+b.width*(widthFactor-1)*.5f;
            var support=ground??new Vector2(side?b.center.x:.5f,side ? .29f : .35f);
            var bottom=support.y-.005f;var top=b.yMax+b.height*.14f;
            if(!side)return new[]{new Vector2(x0,bottom),new Vector2(x1,bottom),new Vector2(x1,top),new Vector2(x0,top)};
            float Plane(Vector2 p)=>vp.y+(p.y-vp.y)*Mathf.Abs(b.center.x-vp.x)/Mathf.Max(.01f,Mathf.Abs(p.x-vp.x));
            var ys=new System.Collections.Generic.List<float>();foreach(var p in opening.left)ys.Add(Plane(p));foreach(var p in opening.right)ys.Add(Plane(p));
            var low=Plane(support)-.005f;var high=Mathf.Max(ys.ToArray());high+=(high-low)*.12f;
            float Y(float x,float y)=>vp.y+(y-vp.y)*Mathf.Abs(x-vp.x)/Mathf.Abs(b.center.x-vp.x);
            return new[]{new Vector2(x0,Y(x0,low)),new Vector2(x1,Y(x1,low)),new Vector2(x1,Y(x1,high)),new Vector2(x0,Y(x0,high))};
        }
        public static Rect FrameBounds(FloorArtwork.OpeningContour opening,bool side,float widthFactor = 1.68f, Vector2? ground = null)
        {
            var points=FrameCorners(opening,side,widthFactor,ground);var min=points[0];var max=points[0];foreach(var p in points){min=Vector2.Min(min,p);max=Vector2.Max(max,p);}return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (sprite == null || contour?.left == null || contour.left.Length < 2) return;
            if(fullFrame)
            {
                var points=FrameCorners(contour,sidePlane,frameWidthFactor,frameGround);
                var frameBox=FrameBounds(contour,sidePlane,frameWidthFactor,frameGround);var bounds=rectTransform.rect;
                var uvRect=DataUtility.GetOuterUV(sprite);var vp=FloorArtwork.RoomVanishingPoint;
                const int columns=12,rows=8;
                for(var row=0;row<=rows;row++)for(var column=0;column<=columns;column++)
                {
                    var tx=column/(float)columns;var ty=row/(float)rows;
                    var x=Mathf.Lerp(points[0].x,points[1].x,tx);
                    var low=Mathf.Lerp(points[0].y,points[1].y,tx);var high=Mathf.Lerp(points[3].y,points[2].y,tx);
                    if(sidePlane)
                    {
                        // A subdivided projective wall surface keeps iron bands continuous;
                        // a two-triangle affine quad bends them at its diagonal.
                        var a=points[0].x-vp.x;var b=points[1].x-vp.x;
                        x=vp.x+Mathf.Sign(a)/Mathf.Lerp(1/Mathf.Abs(a),1/Mathf.Abs(b),tx);
                        var ratio=Mathf.Abs((x-vp.x)/a);low=vp.y+(points[0].y-vp.y)*ratio;high=vp.y+(points[3].y-vp.y)*ratio;
                    }
                    var y=Mathf.Lerp(low,high,ty);var u=Mathf.Lerp(source.xMin,source.xMax,mirror?1-tx:tx);var v=Mathf.Lerp(source.yMin,source.yMax,ty);
                    vh.AddVert(new Vector3(bounds.xMin+(x-frameBox.xMin)/frameBox.width*bounds.width,bounds.yMin+(y-frameBox.yMin)/frameBox.height*bounds.height),color,
                        new Vector2(Mathf.Lerp(uvRect.x,uvRect.z,u),Mathf.Lerp(uvRect.y,uvRect.w,v)));
                    if(row>0&&column>0){var i=row*(columns+1)+column;vh.AddTriangle(i-columns-2,i-columns-1,i);vh.AddTriangle(i-columns-2,i,i-1);}
                }
                return;
            }
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
