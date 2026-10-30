using UnityEngine;

namespace DungeonSlash
{
    /// <summary>Fit the saved F1 panel within its actual canvas.</summary>
    [ExecuteAlways, RequireComponent(typeof(RectTransform))]
    public sealed class DebugCatalogPanelLayout : MonoBehaviour
    {
        public float margin = 24;
        void OnEnable() => Fit();
        void LateUpdate() => Fit();
        void Fit()
        {
            var rect = (RectTransform)transform;
            if (rect.parent is not RectTransform parent || rect.rect.width <= 0 || rect.rect.height <= 0) return;
            var scale = Mathf.Min(1, (parent.rect.width - margin * 2) / rect.rect.width, (parent.rect.height - margin * 2) / rect.rect.height);
            rect.localScale = Vector3.one * Mathf.Max(.1f, scale);
            rect.anchoredPosition = Vector2.zero;
        }
    }
}
