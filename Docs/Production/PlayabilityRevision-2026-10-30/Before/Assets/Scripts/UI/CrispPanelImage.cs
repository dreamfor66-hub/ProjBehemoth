using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    /// <summary>One opaque panel with four equally thick edges, sized for the artwork's 4-unit pixel grid.</summary>
    [AddComponentMenu("UI/Crisp Panel Image")]
    public sealed class CrispPanelImage : Image
    {
        [Min(0)] public float borderThickness=4;
        public Color borderColor=new Color32(75,74,96,255);
        public override Texture mainTexture=>Texture2D.whiteTexture;

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            var rect=GetPixelAdjustedRect();
            var thickness=Mathf.Min(borderThickness,Mathf.Min(rect.width,rect.height)*.5f);
            void Quad(float left,float bottom,float right,float top,Color tint)
            {
                if(right<=left||top<=bottom)return;
                var start=vertices.currentVertCount;
                vertices.AddVert(new Vector3(left,bottom),tint,Vector2.zero);
                vertices.AddVert(new Vector3(left,top),tint,Vector2.zero);
                vertices.AddVert(new Vector3(right,top),tint,Vector2.zero);
                vertices.AddVert(new Vector3(right,bottom),tint,Vector2.zero);
                vertices.AddTriangle(start,start+1,start+2);vertices.AddTriangle(start,start+2,start+3);
            }
            Quad(rect.xMin,rect.yMin,rect.xMax,rect.yMax,color);
            var edge=borderColor;
            if(Mathf.Abs(edge.grayscale-color.grayscale)<.12f)edge=Color.Lerp(edge,Color.white,.25f);
            edge.a*=color.a;
            Quad(rect.xMin,rect.yMax-thickness,rect.xMax,rect.yMax,edge);
            Quad(rect.xMin,rect.yMin,rect.xMax,rect.yMin+thickness,edge);
            Quad(rect.xMin,rect.yMin+thickness,rect.xMin+thickness,rect.yMax-thickness,edge);
            Quad(rect.xMax-thickness,rect.yMin+thickness,rect.xMax,rect.yMax-thickness,edge);
        }
    }
}
