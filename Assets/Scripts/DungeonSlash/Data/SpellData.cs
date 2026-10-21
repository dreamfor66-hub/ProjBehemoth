using UnityEngine;
namespace DungeonSlash
{
    public enum SwipeDirection { Left, Right, Up, Down }
    public enum SpellElement { Fire, Ice, Lightning }
    public enum SpellShape { Pierce, Burst, Chain }
    [CreateAssetMenu(menuName = "Dungeon Slash/Spell")]
    public sealed class SpellData : LocalizedData
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public SpellElement element;
        public SpellShape shape;
        public SwipeDirection defaultSlot;
        [Min(.1f)] public float castingSeconds = .65f;
        [Min(.1f)] public float cooldownSeconds = 5f;
        [Min(0)] public float openingCooldownSeconds = 4f;
        [Min(0)] public float damage = 35f;
        [Min(1)] public float width = 24f;
        [Min(1)] public float radius = 130f;
        [Min(1)] public int chainTargets = 3;
        [Min(0)] public float slowSeconds;
        public Color Color => element switch { SpellElement.Fire => new(1f, .43f, .2f), SpellElement.Ice => new(.35f, .9f, 1f), _ => new(.9f, .8f, 1f) };
    }
}
