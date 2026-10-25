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
        public Sprite For(CombatVisualState state)=>state switch
        {CombatVisualState.Attack=>attack,CombatVisualState.Guard=>guard,CombatVisualState.Ready=>ready,CombatVisualState.Dead=>dead,_=>idle};
    }
}
