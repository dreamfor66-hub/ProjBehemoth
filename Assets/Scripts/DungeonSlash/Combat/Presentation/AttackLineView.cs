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
            public RectTransform CastRect;
            public CapsuleCastGraphic Cast;
            public float HideAt;
        }

        [SerializeField] private RectTransform line;
        [SerializeField] private Image image;
        [SerializeField] private CapsuleCastGraphic hitCast;
        [SerializeField] private float visibleSeconds = .55f;
        private float hideAt;
        private readonly System.Collections.Generic.List<AuxiliaryLine> auxiliaryLines = new();

        public void Configure(RectTransform newLine, Image newImage)
        {
            line = newLine;
            image = newImage;
            EnsureHitCast();
            Hide();
        }

        public void Show(AttackSegment segment, bool charged)
        {
            if (line == null || image == null) return;
            EnsureHitCast();
            ConfigureLine(line, image, segment, charged ? new Color(1f, .77f, .16f, .9f) : new Color(.35f, .9f, 1f, .9f));
            ConfigureCast(hitCast.rectTransform, hitCast, segment);
            image.enabled = true;
            hitCast.enabled = true;
            hideAt = Time.unscaledTime + visibleSeconds;
        }

        /// <summary>Shows an additional real hit path without replacing the primary slash line.</summary>
        public void ShowAdditional(AttackSegment segment, bool charged, float alpha = .78f)
        {
            if (line == null || image == null || line.parent == null) return;
            var castObject = new GameObject("AuxiliaryAttackHitCast", typeof(RectTransform), typeof(CanvasRenderer), typeof(CapsuleCastGraphic));
            castObject.transform.SetParent(line.parent, false);
            var root = new GameObject("AuxiliaryAttackLine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(line.parent, false);
            var auxiliary = new AuxiliaryLine
            {
                Rect = root.GetComponent<RectTransform>(),
                Image = root.GetComponent<Image>(),
                CastRect = castObject.GetComponent<RectTransform>(),
                Cast = castObject.GetComponent<CapsuleCastGraphic>(),
                HideAt = Time.unscaledTime + visibleSeconds
            };
            auxiliary.Image.sprite = image.sprite;
            auxiliary.Image.type = image.type;
            auxiliary.Image.raycastTarget = false;
            var color = charged ? new Color(1f, .77f, .16f, alpha) : new Color(.35f, .9f, 1f, alpha);
            ConfigureCast(auxiliary.CastRect, auxiliary.Cast, segment);
            ConfigureLine(auxiliary.Rect, auxiliary.Image, segment, color);
            auxiliary.Cast.enabled = true;
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
            if (hitCast != null) hitCast.enabled = false;
            foreach (var auxiliary in auxiliaryLines)
            {
                if (auxiliary?.Rect != null) Destroy(auxiliary.Rect.gameObject);
                if (auxiliary?.CastRect != null) Destroy(auxiliary.CastRect.gameObject);
            }
            auxiliaryLines.Clear();
        }

        private void Update()
        {
            if (image != null && image.enabled && Time.unscaledTime >= hideAt)
            {
                image.enabled = false;
                if (hitCast != null) hitCast.enabled = false;
            }
            for (var index = auxiliaryLines.Count - 1; index >= 0; index--)
            {
                var auxiliary = auxiliaryLines[index];
                if (auxiliary?.Rect == null || Time.unscaledTime < auxiliary.HideAt) continue;
                Destroy(auxiliary.Rect.gameObject);
                if (auxiliary.CastRect != null) Destroy(auxiliary.CastRect.gameObject);
                auxiliaryLines.RemoveAt(index);
            }
        }

        private void EnsureHitCast()
        {
            if (hitCast != null || line == null || line.parent == null) return;
            var castObject = new GameObject("AttackHitCast", typeof(RectTransform), typeof(CanvasRenderer), typeof(CapsuleCastGraphic));
            castObject.transform.SetParent(line.parent, false);
            castObject.transform.SetSiblingIndex(line.GetSiblingIndex());
            hitCast = castObject.GetComponent<CapsuleCastGraphic>();
            hitCast.color = new Color(1f, .12f, .12f, .34f);
            hitCast.raycastTarget = false;
        }

        private static void ConfigureLine(RectTransform target, Image targetImage, AttackSegment segment, Color color)
        {
            var delta = segment.End - segment.Start;
            target.anchoredPosition = (segment.Start + segment.End) * .5f;
            target.sizeDelta = new Vector2(delta.magnitude, Mathf.Max(3f, segment.Width));
            target.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            targetImage.color = color;
        }

        private static void ConfigureCast(RectTransform target, CapsuleCastGraphic cast, AttackSegment segment)
        {
            var delta = segment.End - segment.Start;
            var diameter = Mathf.Max(3f, segment.Width);
            target.anchoredPosition = (segment.Start + segment.End) * .5f;
            target.sizeDelta = new Vector2(delta.magnitude + diameter, diameter);
            target.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            cast.color = new Color(1f, .12f, .12f, .34f);
            cast.raycastTarget = false;
        }
    }
}
