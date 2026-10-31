using UnityEngine;

namespace DungeonSlash
{
    [CreateAssetMenu(menuName="Dungeon Slash/Presentation/Floor Artwork")]
    public sealed class FloorArtwork : ScriptableObject
    {
        public Sprite walls;
        [HideInInspector, Tooltip("Legacy import offset; no longer applied. Set the Background RectTransform directly.")]
        public Vector2 imageOffset;
        [Tooltip("Transparent full-canvas overlays, ordered Forward / Left / Right. Each image shares the wall canvas and perspective.")]
        public Sprite[] openings = new Sprite[3];
    }
}
