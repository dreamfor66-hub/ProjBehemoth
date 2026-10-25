using DungeonSlash;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

public sealed class ArcadeRasterFeature : ScriptableRendererFeature
{
    private RasterPass pass;
    public override void Create(){pass=new RasterPass();}
    public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData data)
    {
        var raster=data.cameraData.camera.GetComponent<ArcadeRasterCamera>();
        if(raster==null||raster.presentation==null||!raster.presentation.arcade)return;
        raster.EnsureFrame();pass.raster=raster;renderer.EnqueuePass(pass);
    }
    protected override void Dispose(bool disposing){pass?.Dispose();}
    private sealed class RasterPass:ScriptableRenderPass
    {
        public ArcadeRasterCamera raster;
        private RTHandle imported;
        private RenderTexture previous;
        private Material textComposite;
        public RasterPass(){renderPassEvent=RenderPassEvent.AfterRenderingPostProcessing;requiresIntermediateTexture=true;}
        public void Dispose(){imported?.Release();imported=null;CoreUtils.Destroy(textComposite);}
        private sealed class TextPassData
        {
            public ArcadeTextRenderer text;
        }
        public override void RecordRenderGraph(RenderGraph graph,ContextContainer data)
        {
            if(raster.Frame==null)return;
            if(previous!=raster.Frame){imported?.Release();imported=RTHandles.Alloc(raster.Frame);previous=raster.Frame;}
            var resources=data.Get<UniversalResourceData>();var source=resources.activeColorTexture;
            var target=graph.ImportTexture(imported);
            if(raster.quantizeColors)
            {
                var material=raster.presentation.paletteMaterial;
                var style=raster.presentation.weakPointStyle!=null?raster.presentation.weakPointStyle:WeakPointVisualStyle.Default;
                material.SetVector("_WeakPointStroke",style.stroke);material.SetVector("_WeakPointShadow",style.shadow);material.SetVector("_WeakPointCore",style.core);
                graph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(source,target,material,0),"224p / 16 colors");
            }
            else graph.AddBlitPass(source,target,Vector2.one,Vector2.zero,filterMode:RenderGraphUtils.BlitFilterMode.ClampNearest,passName:"224p / original art colors");
            graph.AddBlitPass(target,source,Vector2.one,Vector2.zero,filterMode:RenderGraphUtils.BlitFilterMode.ClampNearest,passName:"Integer pixel presentation");
            var text=raster.TextRenderer;
            if(text==null||text.Draws.Count==0||raster.textCompositeShader==null)return;
            var descriptor=graph.GetTextureDesc(source);
            descriptor.name="Native resolution TMP text";
            descriptor.colorFormat=UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB;
            descriptor.clearBuffer=true;descriptor.clearColor=Color.clear;
            descriptor.msaaSamples=MSAASamples.None;
            var textTexture=graph.CreateTexture(descriptor);
            var depthDescriptor=descriptor;depthDescriptor.name="Native text stencil";
            depthDescriptor.colorFormat=UnityEngine.Experimental.Rendering.GraphicsFormat.None;
            depthDescriptor.depthBufferBits=DepthBits.Depth24;
            var textDepth=graph.CreateTexture(depthDescriptor);
            using(var builder=graph.AddRasterRenderPass<TextPassData>("Native text / authored UI occlusion",out var passData))
            {
                passData.text=text;
                builder.SetRenderAttachment(textTexture,0);
                builder.SetRenderAttachmentDepth(textDepth,AccessFlags.Write);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc((TextPassData p,RasterGraphContext context)=>
                {
                    context.cmd.SetViewProjectionMatrices(p.text.View,p.text.Projection);
                    foreach(var draw in p.text.Draws)
                    {
                        if(draw.clip.HasValue)
                        {
                            if(draw.clip.Value.width<=0||draw.clip.Value.height<=0)continue;
                            context.cmd.EnableScissorRect(draw.clip.Value);
                        }
                        context.cmd.DrawMesh(draw.mesh,draw.matrix,draw.material,0,0,draw.properties);
                        if(draw.clip.HasValue)context.cmd.DisableScissorRect();
                    }
                });
            }
            if(textComposite==null)textComposite=CoreUtils.CreateEngineMaterial(raster.textCompositeShader);
            graph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(textTexture,source,textComposite,0),"Native TMP text over pixel artwork");
        }
    }
}
