using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class AttackLineView : MonoBehaviour
    {
        private sealed class AuxiliaryLine
        {
            public RectTransform Rect;
            public Image Image;
            public float HideAt;
        }

        [SerializeField] private RectTransform line;
        [SerializeField] private Image image;
        [SerializeField] private float visibleSeconds = .55f;
        private float hideAt;
        private readonly System.Collections.Generic.List<AuxiliaryLine> auxiliaryLines = new();

        public void Configure(RectTransform newLine, Image newImage)
        {
            line = newLine;
            image = newImage;
            Hide();
        }

        public void Show(AttackSegment segment, bool charged)
        {
            if (line == null || image == null) return;
            ConfigureLine(line, image, segment, charged ? new Color(1f, .77f, .16f, .9f) : new Color(.35f, .9f, 1f, .9f));
            image.enabled = true;
            hideAt = Time.unscaledTime + visibleSeconds;
        }

        /// <summary>Shows an additional real hit path without replacing the primary slash line.</summary>
        public void ShowAdditional(AttackSegment segment, bool charged, float alpha = .78f)
        {
            if (line == null || image == null || line.parent == null) return;
            var root = new GameObject("AuxiliaryAttackLine", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(line.parent, false);
            var auxiliary = new AuxiliaryLine { Rect = root.GetComponent<RectTransform>(), Image = root.GetComponent<Image>(), HideAt = Time.unscaledTime + visibleSeconds };
            auxiliary.Image.sprite = image.sprite;
            auxiliary.Image.type = image.type;
            auxiliary.Image.raycastTarget = false;
            var color = charged ? new Color(1f, .77f, .16f, alpha) : new Color(.35f, .9f, 1f, alpha);
            ConfigureLine(auxiliary.Rect, auxiliary.Image, segment, color);
            auxiliary.Image.enabled = true;
            auxiliaryLines.Add(auxiliary);
        }

        /// <summary>Keeps the currently displayed hit path on screen for a final-impact beat.</summary>
        public void KeepVisibleFor(float seconds)
        {
            if (image == null || !image.enabled) return;
            hideAt = Mathf.Max(hideAt, Time.unscaledTime + Mathf.Max(0f, seconds));
            foreach (var auxiliary in auxiliaryLines)
                auxiliary.HideAt = Mathf.Max(auxiliary.HideAt, Time.unscaledTime + Mathf.Max(0f, seconds));
        }

        public void Hide()
        {
            if (image != null) image.enabled = false;
            foreach (var auxiliary in auxiliaryLines)
                if (auxiliary?.Rect != null) Destroy(auxiliary.Rect.gameObject);
            auxiliaryLines.Clear();
        }

        private void Update()
        {
            if (image != null && image.enabled && Time.unscaledTime >= hideAt)
                image.enabled = false;
            for (var index = auxiliaryLines.Count - 1; index >= 0; index--)
            {
                var auxiliary = auxiliaryLines[index];
                if (auxiliary?.Rect == null || Time.unscaledTime < auxiliary.HideAt) continue;
                Destroy(auxiliary.Rect.gameObject);
                auxiliaryLines.RemoveAt(index);
            }
        }

        private static void ConfigureLine(RectTransform target, Image targetImage, AttackSegment segment, Color color)
        {
            var delta = segment.End - segment.Start;
            target.anchoredPosition = (segment.Start + segment.End) * .5f;
            target.sizeDelta = new Vector2(delta.magnitude, Mathf.Max(3f, segment.Width));
            target.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            targetImage.color = color;
        }
    }
}
