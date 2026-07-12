using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class AttackLineView : MonoBehaviour
    {
        [SerializeField] private RectTransform line;
        [SerializeField] private Image image;
        [SerializeField] private float visibleSeconds = .55f;
        private float hideAt;

        public void Configure(RectTransform newLine, Image newImage)
        {
            line = newLine;
            image = newImage;
            Hide();
        }

        public void Show(AttackSegment segment, bool charged)
        {
            if (line == null || image == null) return;
            var delta = segment.End - segment.Start;
            line.anchoredPosition = (segment.Start + segment.End) * .5f;
            line.sizeDelta = new Vector2(delta.magnitude, Mathf.Max(3f, segment.Width));
            line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            image.color = charged ? new Color(1f, .77f, .16f, .9f) : new Color(.35f, .9f, 1f, .9f);
            image.enabled = true;
            hideAt = Time.unscaledTime + visibleSeconds;
        }

        /// <summary>Keeps the currently displayed hit path on screen for a final-impact beat.</summary>
        public void KeepVisibleFor(float seconds)
        {
            if (image == null || !image.enabled) return;
            hideAt = Mathf.Max(hideAt, Time.unscaledTime + Mathf.Max(0f, seconds));
        }

        public void Hide()
        {
            if (image != null) image.enabled = false;
        }

        private void Update()
        {
            if (image != null && image.enabled && Time.unscaledTime >= hideAt)
                image.enabled = false;
        }
    }
}
