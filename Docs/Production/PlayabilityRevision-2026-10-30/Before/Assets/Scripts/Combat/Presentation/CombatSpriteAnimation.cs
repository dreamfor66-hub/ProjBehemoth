using UnityEngine;

namespace DungeonSlash
{
    public enum CombatVisualState { Idle, Attack, Guard, Ready, Dead, Stunned }
    public enum SwordSwingDirection { RightToLeft, LeftToRight }

    [CreateAssetMenu(menuName="Dungeon Slash/Presentation/Combat State Images")]
    public sealed class CombatSpriteAnimation : ScriptableObject
    {
        public Sprite idle,attack,guard,ready,dead;
        [Tooltip("Distinct recoverable stun painting. Unassigned monsters retain their Idle painting.")]
        public Sprite stunned;
        [Min(.01f), Tooltip("How long the player's Attack painting stays visible before returning to the current pose. Does not change attack cooldown or damage timing.")]
        public float attackPoseSeconds = .3f;
        [Header("Optional left-to-right sword swing")]
        public Sprite readyLeftToRight, attackLeftToRight;
        public bool HasAlternateSwing=>readyLeftToRight!=null&&attackLeftToRight!=null;
        [Min(1f), Tooltip("Transparent canvas expansion shared by every pose. Drawing compensates this padding without changing gameplay bounds.")]
        public float canvasScale = 1f;
        [Tooltip("Added transparent PNG pixels: left, bottom, right, top. Fixed rendering compensation retains the original artwork size and position.")]
        public Vector4 canvasPaddingPixels;
        [HideInInspector, Tooltip("Legacy migration data. Keyed pose motion is not used at runtime.")]
        public bool illustratedMotion;
        [Tooltip("Use the painted-art label/layout without substituting mesh motion for state images.")]
        public bool illustratedLayout;
        public bool UsesIllustratedLayout => illustratedLayout || illustratedMotion;
        [HideInInspector, Tooltip("Legacy center placement offset, retained for migration only. Encounters now place hitbox bottoms.")]
        public Vector2 combatPositionOffset;
        [HideInInspector] public float readyTilt;
        [HideInInspector] public float attackTilt;
        [HideInInspector] public Vector2 attackOffset;
        [HideInInspector] public bool emphasizePoses;
        [HideInInspector] public Vector2 readyOffset;
        [HideInInspector] public Vector2 readyScale = Vector2.one;
        [HideInInspector] public Vector2 strikeScale = Vector2.one;
        [HideInInspector] public float strikeSeconds = .3f;
        [Tooltip("Tightly cropped pose rectangles share width and a ground baseline, rather than a square canvas.")]
        public bool tightPoseFrames;
        public Sprite For(CombatVisualState state)=>state switch
        {CombatVisualState.Attack=>attack,CombatVisualState.Guard=>guard,CombatVisualState.Ready=>ready,CombatVisualState.Dead=>dead,CombatVisualState.Stunned=>stunned!=null?stunned:idle,_=>idle};
        public Sprite For(CombatVisualState state, SwordSwingDirection direction)
        {
            if(direction==SwordSwingDirection.LeftToRight&&HasAlternateSwing)
            {
                var alternate=state==CombatVisualState.Ready?readyLeftToRight:state==CombatVisualState.Attack?attackLeftToRight:null;
                if(alternate!=null)return alternate;
            }
            return For(state);
        }
    }
}
