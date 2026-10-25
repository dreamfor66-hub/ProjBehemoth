using UnityEngine;

namespace DungeonSlash
{
    // Artwork uses the low-resolution framebuffer; TMP text is composed afterwards at camera resolution.
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public sealed class ArcadeRasterCamera : MonoBehaviour
    {
        public ArcadePresentation presentation;
        public bool quantizeColors=true;
        public Shader textOcclusionShader;
        public Shader textCompositeShader;
        public ArcadeTextRenderer TextRenderer { get; private set; }
        public RenderTexture Frame { get; private set; }
        private void OnEnable()
        {
            if(textOcclusionShader==null)textOcclusionShader=Shader.Find("Hidden/Behemoth/Text Occlusion");
            if(textCompositeShader==null)textCompositeShader=Shader.Find("Hidden/Behemoth/Native Text Composite");
            EnsureFrame();
            TextRenderer=new ArcadeTextRenderer(this);
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering+=BeginCamera;
            UnityEngine.Rendering.RenderPipelineManager.endCameraRendering+=EndCamera;
        }
        private void BeginCamera(UnityEngine.Rendering.ScriptableRenderContext context,Camera camera)
        {
            if(camera==GetComponent<Camera>()&&presentation!=null&&presentation.arcade&&textOcclusionShader!=null&&textCompositeShader!=null)
            {
                TextRenderer.Capture(camera);
                Shader.SetGlobalFloat("_BehemothHideRasterText",1);
            }
        }
        private void EndCamera(UnityEngine.Rendering.ScriptableRenderContext context,Camera camera)
        {
            if(camera==GetComponent<Camera>())Shader.SetGlobalFloat("_BehemothHideRasterText",0);
        }
        public void EnsureFrame()
        {
            if(presentation == null || !presentation.arcade)return;
            if(Frame == null || Frame.width != presentation.width || Frame.height != presentation.height)
            {
                Release();Frame=new RenderTexture(presentation.width,presentation.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
                Frame.name="Arcade 384x224 indexed framebuffer";Frame.filterMode=FilterMode.Point;Frame.antiAliasing=1;Frame.Create();
            }
        }
        private void OnDisable()
        {
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering-=BeginCamera;
            UnityEngine.Rendering.RenderPipelineManager.endCameraRendering-=EndCamera;
            Shader.SetGlobalFloat("_BehemothHideRasterText",0);
            TextRenderer?.Dispose();TextRenderer=null;Release();
        }
        private void Release(){if(Frame==null)return;Frame.Release();if(Application.isPlaying)Destroy(Frame);else DestroyImmediate(Frame);Frame=null;}
    }
}
