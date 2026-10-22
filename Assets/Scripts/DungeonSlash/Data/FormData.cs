using UnityEngine;
using Sirenix.OdinInspector;
namespace DungeonSlash
{
    public enum FormFinisher { Burst, MultiHit, Shelter, Flow }
    [CreateAssetMenu(menuName = "Dungeon Slash/Form")]
    public sealed class FormData : LocalizedData
    {
        public string displayName;
        [Tooltip("빈 패턴은 상태형 Form입니다. 방향 목록이 있으면 해당 일반공격 연계 후 차지로 발동합니다.")] public SwipeDirection[] pattern = new SwipeDirection[2];
        [HideInInspector] public FormFinisher finisher; // Content classification, not a runtime dispatch key.
        [ShowIf(nameof(HasPattern))] public float damageMultiplier = 1f;
        [ShowIf(nameof(HasPattern)), Min(1)] public int hits = 1;
        [HideInInspector] public RelicPower requiredPower;
        public bool requiresUnlock;
        public BuffData buff;
        [HideInInspector] public System.Collections.Generic.List<EquipmentEffect> effects = new(); // Legacy migration input only.
        private bool HasPattern=>pattern!=null && pattern.Length>0;
        public string PatternLabel => string.Join("", System.Array.ConvertAll(pattern, DirectionLabel));
        public static string DirectionLabel(SwipeDirection direction) => direction switch { SwipeDirection.Left => "←", SwipeDirection.Right => "→", SwipeDirection.Up => "↑", _ => "↓" };
    }
}
