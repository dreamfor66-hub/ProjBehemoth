using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class WeakPointView : MonoBehaviour
    {
        private sealed class HitSegment
        {
            public RectTransform Root { get; }
            public Image Background { get; }
            public Image Fill { get; }
            public bool IsBreaking { get; set; }

            public HitSegment(RectTransform root, Image background, Image fill)
            {
                Root = root;
                Background = background;
                Fill = fill;
            }
        }

        private const float SegmentWidth = 18f;
        private const float SegmentHeight = 7f;
        private const float SegmentGap = 3f;
        private const float SegmentBreakDuration = .16f;
        private static readonly Color SegmentBackgroundColor = new(.035f, .06f, .09f, .96f);
        private static readonly Color SegmentReadyColor = new(1f, .7f, .08f, 1f);
        private static readonly Color SegmentBrokenColor = new(.34f, .12f, .1f, .9f);

        [SerializeField] private RectTransform root;
        [SerializeField] private Image body;
        [SerializeField] private Text remainingHits;
        private RectTransform hitSegmentRoot;
        private readonly List<HitSegment> hitSegments = new();
        private int displayedRequiredHits = -1;
        private int displayedRemainingHits = -1;

        public void Configure(RectTransform newRoot, Image newBody, Text newRemainingHits)
        {
            root = newRoot;
            body = newBody;
            remainingHits = newRemainingHits;
        }

        public void Bind(WeakPointRuntime runtime)
        {
            if (runtime == null) return;
            if (root != null)
            {
                root.anchoredPosition = runtime.HitShape.Center;
                var diameter = runtime.HitShape.Radius * 2f;
                root.sizeDelta = new Vector2(diameter, diameter);
            }
            if (body != null)
            {
                body.enabled = runtime.IsActive && !runtime.IsDestroyed;
                body.color = Color.white;
            }
            if (remainingHits != null) remainingHits.enabled = false;

            EnsureHitSegments(runtime.RequiredHits);
            if (runtime.RequiredHits > 1 && (displayedRemainingHits < 0 || displayedRequiredHits != runtime.RequiredHits || displayedRemainingHits != runtime.RemainingHits))
                SetRemainingSegments(runtime.RemainingHits);
        }

        public void PlayChargeHit(int previousRemainingHits, int currentRemainingHits)
        {
            EnsureHitSegments(Mathf.Max(displayedRequiredHits, previousRemainingHits));
            if (displayedRequiredHits <= 1 || hitSegments.Count == 0) return;

            var segmentIndex = Mathf.Clamp(currentRemainingHits, 0, hitSegments.Count - 1);
            displayedRemainingHits = Mathf.Clamp(currentRemainingHits, 0, hitSegments.Count);
            StartCoroutine(BreakSegment(hitSegments[segmentIndex]));
        }

        private void EnsureHitSegments(int requiredHits)
        {
            requiredHits = Mathf.Max(1, requiredHits);
            if (displayedRequiredHits == requiredHits && hitSegments.Count == requiredHits) return;

            var segmentRoot = GetHitSegmentRoot();
            if (requiredHits <= 1)
            {
                segmentRoot.gameObject.SetActive(false);
                displayedRequiredHits = requiredHits;
                displayedRemainingHits = -1;
                return;
            }

            segmentRoot.gameObject.SetActive(true);
            for (var index = segmentRoot.childCount - 1; index >= 0; index--)
                Destroy(segmentRoot.GetChild(index).gameObject);
            hitSegments.Clear();

            var totalWidth = requiredHits * SegmentWidth + Mathf.Max(0, requiredHits - 1) * SegmentGap;
            segmentRoot.sizeDelta = new Vector2(totalWidth, SegmentHeight);
            for (var index = 0; index < requiredHits; index++)
                hitSegments.Add(CreateSegment(segmentRoot, index, totalWidth));

            displayedRequiredHits = requiredHits;
            displayedRemainingHits = -1;
        }

        private RectTransform GetHitSegmentRoot()
        {
            if (hitSegmentRoot != null) return hitSegmentRoot;

            var parent = remainingHits != null ? remainingHits.rectTransform : root;
            if (remainingHits != null)
            {
                parent.anchorMin = Vector2.zero;
                parent.anchorMax = Vector2.one;
                parent.offsetMin = Vector2.zero;
                parent.offsetMax = Vector2.zero;
            }
            var bar = new GameObject("HitSegments", typeof(RectTransform));
            hitSegmentRoot = bar.GetComponent<RectTransform>();
            hitSegmentRoot.SetParent(parent, false);
            hitSegmentRoot.anchorMin = new Vector2(.5f, 0f);
            hitSegmentRoot.anchorMax = new Vector2(.5f, 0f);
            hitSegmentRoot.pivot = new Vector2(.5f, 1f);
            hitSegmentRoot.anchoredPosition = new Vector2(0f, -2f);
            return hitSegmentRoot;
        }

        private static HitSegment CreateSegment(RectTransform parent, int index, float totalWidth)
        {
            var segmentObject = new GameObject($"HitSegment_{index + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            var segmentRoot = segmentObject.GetComponent<RectTransform>();
            segmentRoot.SetParent(parent, false);
            segmentRoot.anchorMin = new Vector2(0f, .5f);
            segmentRoot.anchorMax = new Vector2(0f, .5f);
            segmentRoot.pivot = new Vector2(0f, .5f);
            segmentRoot.anchoredPosition = new Vector2(index * (SegmentWidth + SegmentGap), 0f);
            segmentRoot.sizeDelta = new Vector2(SegmentWidth, SegmentHeight);
            var background = segmentObject.GetComponent<Image>();
            background.color = SegmentBackgroundColor;
            var outline = segmentObject.GetComponent<Outline>();
            outline.effectColor = new Color(1f, .72f, .12f, .9f);
            outline.effectDistance = new Vector2(1f, -1f);

            var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var fill = fillObject.GetComponent<Image>();
            var fillRect = fill.rectTransform;
            fillRect.SetParent(segmentRoot, false);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            fill.color = SegmentReadyColor;
            return new HitSegment(segmentRoot, background, fill);
        }

        private void SetRemainingSegments(int remainingHits)
        {
            displayedRemainingHits = Mathf.Clamp(remainingHits, 0, hitSegments.Count);
            for (var index = 0; index < hitSegments.Count; index++)
            {
                var segment = hitSegments[index];
                segment.IsBreaking = false;
                segment.Root.localScale = Vector3.one;
                segment.Root.localRotation = Quaternion.identity;
                segment.Background.color = index < displayedRemainingHits ? SegmentBackgroundColor : SegmentBrokenColor;
                segment.Fill.enabled = index < displayedRemainingHits;
                segment.Fill.color = SegmentReadyColor;
            }
        }

        private IEnumerator BreakSegment(HitSegment segment)
        {
            if (segment == null || segment.IsBreaking) yield break;
            segment.IsBreaking = true;
            var fillColor = segment.Fill.color;
            var elapsed = 0f;
            while (elapsed < SegmentBreakDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / SegmentBreakDuration);
                segment.Root.localScale = Vector3.one * Mathf.Lerp(1f, .35f, progress);
                segment.Root.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 28f, progress));
                fillColor.a = 1f - progress;
                segment.Fill.color = fillColor;
                yield return null;
            }
            segment.Fill.enabled = false;
            segment.Fill.color = SegmentReadyColor;
            segment.Root.localScale = Vector3.one;
            segment.Root.localRotation = Quaternion.identity;
            segment.Background.color = SegmentBrokenColor;
            segment.IsBreaking = false;
        }
    }
}
