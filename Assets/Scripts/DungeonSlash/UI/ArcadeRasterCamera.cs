using UnityEngine;

namespace DungeonSlash
{
    // UI and world share the same low-resolution final framebuffer. Input remains in screen coordinates.
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public sealed class ArcadeRasterCamera : MonoBehaviour
    {
        public ArcadePresentation presentation;
        public bool quantizeColors=true;
        public RenderTexture Frame { get; private set; }
        private void OnEnable()=>EnsureFrame();
        public void EnsureFrame()
        {
            if(presentation == null || !presentation.arcade)return;
            if(Frame == null || Frame.width != presentation.width || Frame.height != presentation.height)
            {
                Release();Frame=new RenderTexture(presentation.width,presentation.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
                Frame.name="Arcade 384x224 indexed framebuffer";Frame.filterMode=FilterMode.Point;Frame.antiAliasing=1;Frame.Create();
            }
        }
        private void OnDisable()=>Release();
        private void Release(){if(Frame==null)return;Frame.Release();if(Application.isPlaying)Destroy(Frame);else DestroyImmediate(Frame);Frame=null;}
    }
}
