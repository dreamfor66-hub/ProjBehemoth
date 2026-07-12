using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class DamageNumberView : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private Text label;
        [SerializeField] private float lifetime = .8f;
        [SerializeField] private float riseSpeed = 52f;
        private float spawnedAt;
        private Color baseColor;

        public void Configure(RectTransform newRoot, Text newLabel)
        {
            root = newRoot;
            label = newLabel;
        }

        public void Show(Vector2 position, string message, Color color)
        {
            if (root != null) root.anchoredPosition = position;
            if (label != null)
            {
                label.text = message;
                label.color = color;
            }
            baseColor = color;
            spawnedAt = Time.unscaledTime;
        }

        private void Update()
        {
            var elapsed = Time.unscaledTime - spawnedAt;
            if (root != null) root.anchoredPosition += Vector2.up * riseSpeed * Time.unscaledDeltaTime;
            if (label != null)
            {
                var color = baseColor;
                color.a *= 1f - Mathf.Clamp01(elapsed / lifetime);
                label.color = color;
            }
            if (elapsed >= lifetime) Destroy(gameObject);
        }
    }
}
