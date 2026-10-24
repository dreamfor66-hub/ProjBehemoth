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
        public RasterPass(){renderPassEvent=RenderPassEvent.AfterRenderingPostProcessing;requiresIntermediateTexture=true;}
        public void Dispose(){imported?.Release();imported=null;}
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
        }
    }
}
