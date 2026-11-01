using UnityEngine;
namespace DungeonSlash
{
    [CreateAssetMenu(menuName = "Dungeon Slash/Companion")]
    public sealed class CompanionData : LocalizedData
    {
        public string displayName;
        public CompanionView prefab;
        public Sprite combatSprite;
        public CombatSpriteAnimation combatAnimation;
        [Tooltip("Optional authored forward-facing set for the same over-shoulder camera as the player. Used on either flank without mirroring.")]
        public CombatSpriteAnimation forwardAnimation;
        [Tooltip("Separately painted opposite-side poses when standing on the player's right. Not a mirrored copy.")]
        public CombatSpriteAnimation oppositeAnimation;
        [Min(40)] public float displayHeight = 245;
        public string narrationKey;
        [SerializeField, HideInInspector] private RoomObjectData.PaintingGeometry[] geometry;
        public RoomObjectData.PaintingGeometry Geometry(Sprite sprite)
        {
            if (geometry != null) foreach (var item in geometry) if (item.sprite == sprite) return item;
            return new RoomObjectData.PaintingGeometry { sprite = sprite, bounds = new Rect(0,0,1,1), footprint = new Rect(0,0,1,.2f) };
        }
#if UNITY_EDITOR
        public void SetDerivedGeometry(RoomObjectData.PaintingGeometry[] value) => geometry = value;
#endif
        [HideInInspector] public Vector2 playerOffset = new(-200, 70);
        [HideInInspector] public float idleBobHeight = 3;
        [HideInInspector] public float attackLungeDistance = 32;
    }
}
