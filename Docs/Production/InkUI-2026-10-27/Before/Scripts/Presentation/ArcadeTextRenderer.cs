using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace DungeonSlash
{
    /// <summary>
    /// Replays the existing Canvas meshes into a transparent, native-resolution text layer.
    /// Later artwork erases covered text, preserving modal/tooltip ordering without moving
    /// authored text, changing fonts, or duplicating the interactive UI hierarchy.
    /// </summary>
    public sealed class ArcadeTextRenderer : System.IDisposable
    {
        public sealed class Draw
        {
            public Mesh mesh;
            public Matrix4x4 matrix;
            public Material material;
            public MaterialPropertyBlock properties=new();
            public Rect? clip;
        }
        readonly ArcadeRasterCamera owner;
        readonly List<Draw> draws=new();
        readonly List<Draw> drawPool=new();
        readonly List<Graphic> graphics=new(),children=new(),retiredGraphics=new();
        readonly List<Material> retiredMaterials=new();
        readonly List<RectMask2D> masks=new();
        readonly Dictionary<Graphic,Mesh> meshes=new();
        readonly List<Vector3> vertices=new(),normals=new();
        readonly List<Vector4> uv=new(),tangents=new();
        readonly List<Color32> colors=new();
        readonly List<int> indices=new();
        readonly Dictionary<Material,Material> occluders=new();
        readonly Vector3[] corners=new Vector3[4];
        int captures;
        public IReadOnlyList<Draw> Draws=>draws;
        public Matrix4x4 View { get; private set; }
        public Matrix4x4 Projection { get; private set; }
        public ArcadeTextRenderer(ArcadeRasterCamera owner)=>this.owner=owner;

        public void Capture(Camera camera)
        {
            draws.Clear();
            View=camera.worldToCameraMatrix;
            Projection=GL.GetGPUProjectionMatrix(camera.projectionMatrix,false);
            graphics.Clear();
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if(!canvas.isRootCanvas||!canvas.isActiveAndEnabled||canvas.renderMode==RenderMode.ScreenSpaceOverlay)continue;
                if(canvas.renderMode==RenderMode.ScreenSpaceCamera&&canvas.worldCamera!=camera)continue;
                if((camera.cullingMask&(1<<canvas.gameObject.layer))==0)continue;
                canvas.GetComponentsInChildren(false,children);
                foreach(var graphic in children)
                    if(graphic.isActiveAndEnabled&&graphic.depth>=0&&!graphic.canvasRenderer.cull)graphics.Add(graphic);
            }
            graphics.Sort((a,b)=>
            {
                var layer=SortingLayer.GetLayerValueFromID(a.canvas.sortingLayerID).CompareTo(SortingLayer.GetLayerValueFromID(b.canvas.sortingLayerID));
                if(layer!=0)return layer;
                var order=a.canvas.sortingOrder.CompareTo(b.canvas.sortingOrder);
                return order!=0?order:a.depth.CompareTo(b.depth);
            });
            var hasText=false;
            foreach(var graphic in graphics)
            {
                var isText=graphic is TMP_Text||graphic is TMP_SubMeshUI;
                if(!hasText&&!isText)continue;
                var renderer=graphic.canvasRenderer;
                var source=renderer.GetMesh();
                if(source==null||source.vertexCount==0||source.subMeshCount==0||source.GetIndexCount(0)==0)continue;
                // CanvasRenderer's native mesh is not an ordinary GPU Mesh. Copy its channels
                // into a reusable owned mesh so DrawMesh has an uploaded index buffer.
                if(!meshes.TryGetValue(graphic,out var mesh))
                {
                    mesh=new Mesh {name="Native TMP / "+graphic.name,hideFlags=HideFlags.HideAndDontSave};mesh.MarkDynamic();
                    meshes.Add(graphic,mesh);
                }
                mesh.Clear();
                source.GetVertices(vertices);mesh.SetVertices(vertices);
                source.GetColors(colors);mesh.SetColors(colors);
                source.GetNormals(normals);mesh.SetNormals(normals);
                source.GetTangents(tangents);mesh.SetTangents(tangents);
                for(var channel=0;channel<4;channel++){source.GetUVs(channel,uv);mesh.SetUVs(channel,uv);}
                source.GetIndices(indices,0);mesh.SetIndices(indices,MeshTopology.Triangles,0);
                var tint=renderer.GetColor();
                tint.a=renderer.GetInheritedAlpha();
                var material=renderer.GetMaterial();
                if(material==null)continue;
                if(drawPool.Count<=draws.Count)drawPool.Add(new Draw());
                var draw=drawPool[draws.Count];
                var block=draw.properties;block.Clear();
                block.SetTexture("_MainTex",isText?material.mainTexture:graphic.mainTexture);
                // RectMask2D clipping is normally injected by CanvasRenderer, not stored in its material.
                // Scissor the replay against the same intersected ancestor rectangles.
                Rect? clip=null;
                graphic.GetComponentsInParent(false,masks);
                foreach(var mask in masks)
                {
                    if(!mask.isActiveAndEnabled)continue;
                    mask.rectTransform.GetWorldCorners(corners);
                    var min=RectTransformUtility.WorldToScreenPoint(camera,corners[0]);
                    var max=RectTransformUtility.WorldToScreenPoint(camera,corners[2]);
                    var bounds=Rect.MinMaxRect(min.x,min.y,max.x,max.y);
                    if(clip.HasValue){var prior=clip.Value;bounds=Rect.MinMaxRect(Mathf.Max(prior.xMin,bounds.xMin),Mathf.Max(prior.yMin,bounds.yMin),Mathf.Min(prior.xMax,bounds.xMax),Mathf.Min(prior.yMax,bounds.yMax));}
                    clip=bounds;
                }
                if(!isText)
                {
                    if(!occluders.TryGetValue(material,out var eraser))
                    {
                        eraser=new Material(owner.textOcclusionShader){hideFlags=HideFlags.HideAndDontSave};
                        occluders.Add(material,eraser);
                    }
                    foreach(var property in StencilProperties)
                        if(material.HasProperty(property))eraser.SetFloat(property,material.GetFloat(property));
                    block.SetColor("_Color",(material.HasProperty("_Color")?material.GetColor("_Color"):Color.white)*tint);
                    material=eraser;
                }
                else
                {
                    block.SetFloat("_BehemothHideRasterText",0);
                    foreach(var property in TextColors)
                        if(material.HasProperty(property))block.SetColor(property,material.GetColor(property)*tint);
                    hasText=true;
                }
                draw.mesh=mesh;draw.matrix=graphic.transform.localToWorldMatrix;draw.material=material;draw.clip=clip;
                draws.Add(draw);
            }
            if(++captures%120==0)ReleaseRetiredGraphics();
        }
        static readonly string[] StencilProperties={"_Stencil","_StencilComp","_StencilOp","_StencilReadMask","_StencilWriteMask","_ColorMask"};
        static readonly string[] TextColors={"_FaceColor","_OutlineColor","_UnderlayColor","_Color"};
        void ReleaseRetiredGraphics()
        {
            retiredGraphics.Clear();retiredMaterials.Clear();
            foreach(var pair in meshes)if(pair.Key==null){CoreUtils.Destroy(pair.Value);retiredGraphics.Add(pair.Key);}
            foreach(var graphic in retiredGraphics)meshes.Remove(graphic);
            foreach(var pair in occluders)if(pair.Key==null){CoreUtils.Destroy(pair.Value);retiredMaterials.Add(pair.Key);}
            foreach(var material in retiredMaterials)occluders.Remove(material);
        }
        public void Dispose()
        {
            foreach(var mesh in meshes.Values)CoreUtils.Destroy(mesh);
            foreach(var material in occluders.Values)CoreUtils.Destroy(material);
            meshes.Clear();occluders.Clear();draws.Clear();
        }
    }
}
