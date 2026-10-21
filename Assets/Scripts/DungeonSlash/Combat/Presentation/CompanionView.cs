using UnityEngine;
namespace DungeonSlash
{
    public sealed class CompanionView : MonoBehaviour
    {
        public RectTransform body, tail;
        private CompanionData data;
        private float pulse;
        public void Configure(CompanionData definition) { data = definition; ((RectTransform)transform).anchoredPosition = data.playerOffset; }
        public void Attack(Vector2 target) { pulse = 1; }
        private void Update()
        {
            if (data == null || body == null) return;
            pulse = Mathf.MoveTowards(pulse, 0, Time.unscaledDeltaTime * 4);
            body.anchoredPosition = new Vector2(Mathf.Sin(pulse * Mathf.PI) * data.attackLungeDistance, Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3)) * data.idleBobHeight);
            if (tail != null) tail.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.unscaledTime * 9) * 20);
        }
    }
}
