using TMPro;
using UnityEngine;

namespace DungeonSlash
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class CombatGaugeLabel:MonoBehaviour
    {
        public AttackRecoveryGauge gauge;
        TextMeshProUGUI label;
        void LateUpdate(){if(label==null)label=GetComponent<TextMeshProUGUI>();label.enabled=gauge!=null&&gauge.isActiveAndEnabled;}
    }
}
