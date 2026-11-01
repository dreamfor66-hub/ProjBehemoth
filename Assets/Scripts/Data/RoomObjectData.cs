using System;
using UnityEngine;

namespace DungeonSlash
{
    [CreateAssetMenu(menuName = "Dungeon Slash/Room Object")]
    public sealed class RoomObjectData : ScriptableObject
    {
        public string labelKey;
        public string usedLabelKey;
        public string resolvedTextKey;
        public bool wallMounted;
        [Tooltip("The painting is a flat door leaf fitted inside the backdrop's existing aperture.")]
        public bool fitsDoorway;
        [Tooltip("Complete entrance painting including replacement jambs and lintel; projected onto the wall plane, covering the ordinary masonry frame.")]
        public bool includesDoorframe;
        [Tooltip("Mounted above the floor, e.g. a wall lever. No floor contact shadow.")]
        public bool wallSuspended;
        [Tooltip("A person or animal is placed facing the player on the first encounter. Its world position and facing are then preserved.")]
        public bool facePlayerOnFirstEncounter;
        [Tooltip("Optional authored response to being approached from behind on a later visit. No profile means no invented hostile outcome.")]
        public NpcApproachData rearApproach;
        [Tooltip("This subject is painted into a complete room interior; its sprite defines only the interaction area.")]
        public bool roomInterior;
        [Tooltip("Optional separately sized aftermath, e.g. luggage remaining after a person leaves.")]
        public RoomObjectData usedForm;
        [Tooltip("Front, side and rear are separate authored paintings. Symmetric side views may be mirrored.")]
        public Sprite front, side, rear;
        public Sprite usedFront, usedSide, usedRear;
        [Min(40)] public float displayHeight = 270;
        [Min(1)] public float hitPadding = 20;
        [Serializable]
        public struct PaintingGeometry
        {
            public Sprite sprite;
            public Rect bounds;
            public Rect footprint;
        }
        // Derived from PNG alpha by the editor. Transparent canvas padding is never a size/grounding input.
        [SerializeField, HideInInspector] private PaintingGeometry[] geometry;
        public PaintingGeometry Geometry(Sprite sprite)
        {
            if (geometry != null) foreach (var item in geometry) if (item.sprite == sprite) return item;
            return new PaintingGeometry { sprite = sprite, bounds = new Rect(0, 0, 1, 1), footprint = new Rect(0, 0, 1, .2f) };
        }
#if UNITY_EDITOR
        public void SetDerivedGeometry(PaintingGeometry[] value) => geometry = value;
#endif
        public Sprite Painting(int relativeView, bool used)
        {
            if (used && usedForm != null && usedForm != this) return usedForm.Painting(relativeView, false);
            var normal = relativeView == 0 ? front : relativeView == 2 ? rear : side;
            var consumed = relativeView == 0 ? usedFront : relativeView == 2 ? usedRear : usedSide;
            if (consumed == null) consumed = usedFront;
            return used && consumed != null ? consumed : normal != null ? normal : front;
        }
    }
}
