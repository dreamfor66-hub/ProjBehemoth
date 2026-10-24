using UnityEngine;

namespace DungeonSlash
{
    [CreateAssetMenu(menuName="Dungeon Slash/Presentation")]
    public sealed class ArcadePresentation : ScriptableObject
    {
        public bool arcade = true;
        public int revision; public int feedbackRevision; public int outgameRevision; public int mortalisRevision;
        public int readabilityRevision;
        public WeakPointVisualStyle weakPointStyle;
        public int width = 384, height = 224;
        public Material paletteMaterial;
    }
}

