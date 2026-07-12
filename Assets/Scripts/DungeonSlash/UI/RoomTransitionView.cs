using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    /// <summary>First-person walking beat: floor markers advance toward the camera instead of blanking the screen.</summary>
    public sealed class RoomTransitionView : MonoBehaviour
    {
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform walkBackdrop;
        [SerializeField] private List<RectTransform> footfallMarkers = new();
        [SerializeField] private Image fade;
        [SerializeField, Min(.1f)] private float walkDuration = .78f;
        [SerializeField, Min(1)] private int stepCount = 2;
        [SerializeField] private float verticalTravel = 22f;
        [SerializeField] private float zoomAmount = .055f;

        public void Configure(RectTransform newBackground, RectTransform newWalkBackdrop, IEnumerable<RectTransform> newFootfallMarkers)
        {
            background = newBackground;
            walkBackdrop = newWalkBackdrop;
            footfallMarkers = newFootfallMarkers == null ? new List<RectTransform>() : new List<RectTransform>(newFootfallMarkers);
            ResetVisuals();
        }

        public IEnumerator Play()
        {
            if (background == null) yield break;
            if (walkBackdrop != null) walkBackdrop.gameObject.SetActive(true);
            if (fade != null) fade.gameObject.SetActive(false);
            background.anchoredPosition = Vector2.zero;
            background.localScale = Vector3.one;

            var elapsed = 0f;
            while (elapsed < walkDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / walkDuration);
                AnimateWalk(progress);
                yield return null;
            }
            AnimateWalk(1f);
            ResetVisuals();
        }

        private void AnimateWalk(float progress)
        {
            var stride = Mathf.Sin(progress * Mathf.PI * stepCount);
            background.anchoredPosition = Vector2.up * (stride * verticalTravel);
            background.localScale = Vector3.one * Mathf.Lerp(1f, 1f + zoomAmount, progress);

            for (var index = 0; index < footfallMarkers.Count; index++)
            {
                var marker = footfallMarkers[index];
                if (marker == null) continue;
                var phase = Mathf.Repeat(progress * stepCount + index / (float)Mathf.Max(1, footfallMarkers.Count), 1f);
                var depth = Mathf.SmoothStep(0f, 1f, phase);
                marker.anchoredPosition = new Vector2(0f, Mathf.Lerp(280f, -360f, depth));
                marker.sizeDelta = new Vector2(Mathf.Lerp(56f, 700f, depth), Mathf.Lerp(3f, 34f, depth));
                marker.localScale = Vector3.one;
                var image = marker.GetComponent<Image>();
                if (image == null) continue;
                var color = image.color;
                color.a = Mathf.Lerp(.12f, 0f, depth);
                image.color = color;
            }
        }

        private void ResetVisuals()
        {
            if (background != null)
            {
                background.anchoredPosition = Vector2.zero;
                background.localScale = Vector3.one;
            }
            if (walkBackdrop != null) walkBackdrop.gameObject.SetActive(false);
            if (fade != null) fade.gameObject.SetActive(false);
        }
    }
}
