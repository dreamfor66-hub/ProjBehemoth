using UnityEngine;
namespace DungeonSlash
{
    public enum MonkFinisher { Burst, MultiHit, Shelter, Flow }
    [CreateAssetMenu(menuName = "Dungeon Slash/Monk Form")]
    public sealed class MonkFormData : LocalizedData
    {
        public string displayName;
        public SwipeDirection[] pattern = new SwipeDirection[2];
        public MonkFinisher finisher;
        public float damageMultiplier = 1f;
        public int hits = 1;
        [HideInInspector] public RelicPower requiredPower;
        public bool requiresUnlock;
        public System.Collections.Generic.List<EquipmentEffect> effects = new();
        public string PatternLabel => string.Join("", System.Array.ConvertAll(pattern, DirectionLabel));
        public static string DirectionLabel(SwipeDirection direction) => direction switch { SwipeDirection.Left => "←", SwipeDirection.Right => "→", SwipeDirection.Up => "↑", _ => "↓" };
    }
}
