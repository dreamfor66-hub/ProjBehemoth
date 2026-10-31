using UnityEngine;

namespace DungeonSlash
{
    [CreateAssetMenu(menuName="Dungeon Slash/Presentation")]
    public sealed class ArcadePresentation : ScriptableObject
    {
        public bool arcade = true;
        [Tooltip("Preserve authored illustration colors; skip legacy figure/background palette projection.")]
        public bool preserveIllustrationColors;
        public int revision; public int feedbackRevision; public int outgameRevision; public int mortalisRevision;
        public int readabilityRevision;
        public WeakPointVisualStyle weakPointStyle;
        public DarkPaletteStyle darkPalette;
        public int width = 384, height = 224;
        public Material paletteMaterial;
    }
}

