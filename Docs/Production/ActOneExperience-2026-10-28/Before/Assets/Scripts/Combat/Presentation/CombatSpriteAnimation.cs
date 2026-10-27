using UnityEngine;

namespace DungeonSlash
{
    public enum CombatVisualState { Idle, Attack, Guard, Ready, Dead }

    [CreateAssetMenu(menuName="Dungeon Slash/Presentation/Combat State Images")]
    public sealed class CombatSpriteAnimation : ScriptableObject
    {
        public Sprite idle,attack,guard,ready,dead;
        [Min(1f), Tooltip("Transparent canvas expansion shared by every pose. Drawing compensates this padding without changing gameplay bounds.")]
        public float canvasScale = 1f;
        [Tooltip("Legacy single-illustration motion. Disabled for regular, separate state-image sets.")]
        public bool illustratedMotion;
        [Tooltip("Use the painted-art label/layout without substituting mesh motion for state images.")]
        public bool illustratedLayout;
        public bool UsesIllustratedLayout => illustratedLayout || illustratedMotion;
        [HideInInspector, Tooltip("Legacy center placement offset, retained for migration only. Encounters now place hitbox bottoms.")]
        public Vector2 combatPositionOffset;
        [Range(0f, 1f), Tooltip("Idle ground-contact pixel Y / canvas height. Aligns artwork feet to the hitbox lower edge; does not alter combat stats.")]
        public float groundContactY;
        [Range(0f, 1f), Tooltip("Top of opaque Idle artwork / canvas height. Keeps name and HP above differently sized illustrations.")]
        public float artworkTopY = 1f;
        [Tooltip("Name and HP sit above the opaque artwork, independently of sprite padding.")]
        public float labelHeight = 116f;
        public float readyTilt = -4f;
        public float attackTilt = 7f;
        public Vector2 attackOffset = new(0f, -18f);
        public Sprite For(CombatVisualState state)=>state switch
        {CombatVisualState.Attack=>attack,CombatVisualState.Guard=>guard,CombatVisualState.Ready=>ready,CombatVisualState.Dead=>dead,_=>idle};
    }
}
