using System;
using UnityEngine;

namespace DungeonSlash
{
    [CreateAssetMenu(menuName="Dungeon Slash/Presentation/Floor Artwork")]
    public sealed class FloorArtwork : ScriptableObject
    {
        // Shared act-one camera. Art is drawn to this projection; individual acts author only composition sockets.
        public static readonly Vector2 RoomVanishingPoint = new(.5f, .435f);
        public Sprite walls;
        [Tooltip("Complete dead-end camping room, preserving this floor's camera and architecture.")]
        public Sprite campInterior, campUsedInterior;
        [HideInInspector, Tooltip("Legacy import offset; no longer applied. Set the Background RectTransform directly.")]
        public Vector2 imageOffset;
        [Tooltip("Transparent full-canvas overlays, ordered Forward / Left / Right. Each image shares the wall canvas and perspective.")]
        public Sprite[] openings = new Sprite[3];
        [Tooltip("Optional painted doorway placement, Forward / Left / Right. Zero size preserves an existing full-canvas layer. Source transparent padding is measured automatically.")]
        public Rect[] openingPlacement = new Rect[3];
        [Header("Room composition (normalized screen coordinates)")]
        [Tooltip("Authored clear floor locations, clockwise from the far-left corner. Shared perspective determines size automatically.")]
        public Vector2[] floorObjectCorners = { new(.315f, .235f), new(.70f, .235f), new(.88f, .12f), new(.315f, .17f) };
        [Tooltip("Physical wall/floor attachment points: Forward / Left / Right. Adjust to each room's architecture, not its camera.")]
        public Vector2[] wallObjectSupports = { new(.50f, .35f), new(.24f, .29f), new(.81f, .29f) };
        [Serializable]
        public sealed class OpeningContour
        {
            public Vector2[] left, right;
            public Rect bounds;
            public Vector2 canvasScale = Vector2.one;
            public Vector2 canvasOffset;
            public float exposure = 1;
        }
        // Image-derived presentation, rebuilt after source PNG edits; not duplicate designer settings.
        [HideInInspector] public OpeningContour[] openingContours;
        [HideInInspector] public Color objectIllumination = Color.white;
    }
}
