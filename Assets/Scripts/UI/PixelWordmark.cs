using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace DungeonSlash
{
    // Integer native pixels, with no scaled raster lettering or compressed bevel texture.
    [RequireComponent(typeof(CanvasRenderer))] public sealed class PixelWordmark:MaskableGraphic
    {
        public string word="DEEP SIX";
        public ArcadePresentation presentation;
        public int pixelSize=20;
        static readonly Dictionary<char,string[]> Glyphs=new()
        {
            ['D']=new[]{"11110","11011","11001","11001","11001","11011","11110"},
            ['E']=new[]{"11111","11000","11000","11110","11000","11000","11111"},
            ['P']=new[]{"11110","11011","11011","11110","11000","11000","11000"},
            ['S']=new[]{"01111","11000","11000","01110","00011","00011","11110"},
            ['I']=new[]{"11111","00100","00100","00100","00100","00100","11111"},
            ['X']=new[]{"11011","11011","01110","00100","01110","11011","11011"}
        };
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(presentation!=null&&!presentation.arcade)return;var unit=Mathf.Max(4,pixelSize/4*4);var left=Mathf.Round(-(word.Length*6-1)*unit*.5f/4)*4;var top=Mathf.Round(7*unit*.5f/4)*4;
            void Box(float x,float y,float width,float height,Color tint){var i=vh.currentVertCount;vh.AddVert(new Vector3(x,y),tint,Vector2.zero);vh.AddVert(new Vector3(x,y+height),tint,Vector2.zero);vh.AddVert(new Vector3(x+width,y+height),tint,Vector2.zero);vh.AddVert(new Vector3(x+width,y),tint,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);}
            for(var pass=0;pass<2;pass++)for(var n=0;n<word.Length;n++)if(Glyphs.TryGetValue(word[n],out var glyph))for(var y=0;y<7;y++)for(var x=0;x<5;x++)if(glyph[y][x]=='1')
            {var px=left+(n*6+x)*unit;var py=top-(y+1)*unit;if(pass==0)Box(px-4,py-8,unit+8,unit+12,new Color32(11,12,22,255));else Box(px,py,unit,unit,y<3?new Color32(243,230,191,255):new Color32(216,187,98,255));}
        }
    }
}
