using System.Collections.Generic;
using UnityEngine;

namespace DungeonSlash
{
    /// <summary>The intact target and its shatter burst share this exact pixel silhouette and palette.</summary>
    [CreateAssetMenu(menuName = "Dungeon Slash/Presentation/Weak Point Style")]
    public sealed class WeakPointVisualStyle : ScriptableObject
    {
        public Color stroke = new Color32(239,101,72,255);
        public Color shadow = new Color32(100,88,107,255);
        public Color core = new Color32(238,229,213,255);
        public Color outline = Color.black;
        [Range(.75f,2)] public float ringThickness = 1.5f;
        [Range(.5f,3)] public float diagonalGap = 1.5f;
        [Range(1,4)] public float spokeLength = 2.5f;
        [Range(.5f,2)] public float coreRadius = 1.25f;
        [Min(.1f)] public float breakLifetime = .38f;
        [Min(1)] public float breakDistance = 17;
        static WeakPointVisualStyle fallback;
        public static WeakPointVisualStyle Default => fallback != null ? fallback : fallback = CreateFallback();
        static WeakPointVisualStyle CreateFallback()
        { var style=CreateInstance<WeakPointVisualStyle>();style.hideFlags=HideFlags.HideAndDontSave;return style; }

        public readonly struct Pixel
        {
            public readonly Vector2 position;
            public readonly Color color;
            public Pixel(Vector2 position,Color color){this.position=position;this.color=color;}
        }
        public void BuildPixels(float logicalDiameter,List<Pixel> pixels)
        {
            pixels.Clear();
            var radius=Mathf.Max(4,Mathf.Floor(logicalDiameter*.5f)-4.5f);
            var extent=Mathf.CeilToInt(radius+spokeLength+1);
            for(var y=-extent;y<=extent;y++)for(var x=-extent;x<=extent;x++)
            {
                var p=new Vector2(x,y);
                var foreground=Foreground(p,radius);
                if(!foreground)
                {
                    var backing=false;
                    for(var oy=-1;oy<=1&&!backing;oy++)for(var ox=-1;ox<=1;ox++)
                        if(Foreground(p+new Vector2(ox,oy),radius)){backing=true;break;}
                    if(!backing)continue;
                }
                var center=Mathf.Abs(x)+Mathf.Abs(y)<=coreRadius;
                pixels.Add(new Pixel(p, !foreground ? outline : center ? core : y<0 ? shadow : stroke));
            }
        }
        bool Foreground(Vector2 p,float radius)
        {
            var ax=Mathf.Abs(p.x);var ay=Mathf.Abs(p.y);var distance=p.magnitude;
            var center=ax+ay<=coreRadius;
            var arc=distance>=radius-ringThickness&&distance<=radius&&Mathf.Abs(ax-ay)>diagonalGap;
            var prong=(ax<.75f||ay<.75f)&&distance>=radius-2&&distance<=radius+spokeLength;
            return center||arc||prong;
        }
    }
}
