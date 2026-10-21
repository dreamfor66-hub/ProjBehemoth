using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    /// <summary>Shared hover-only description panel for the right-hand run inventory.</summary>
    public sealed class ItemTooltipView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI description;
        [SerializeField] private RectTransform positionRoot;

        public void Configure(GameObject newPanel, TextMeshProUGUI newTitle, TextMeshProUGUI newDescription, RectTransform newPositionRoot)
        {
            panel = newPanel;
            title = newTitle;
            description = newDescription;
            positionRoot = newPositionRoot;
            Hide();
        }

        public void Show(string itemName, string itemDescription, Vector2 screenPosition)
        {
            if (panel == null) return;
            if (title != null) title.text = itemName;
            if (description != null) description.text = itemDescription;
            panel.SetActive(true);
            if (description != null && panel.transform is RectTransform box)
            {
                var maxHeight = positionRoot != null ? positionRoot.rect.height - 16f : 700f;
                var height = Mathf.Clamp(description.preferredHeight + 74f, 140f, Mathf.Max(140f, maxHeight));
                box.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                var body = description.rectTransform;
                body.anchorMin = body.anchorMax = new Vector2(.5f, 1f); body.pivot = new Vector2(.5f, 1f); body.anchoredPosition = new Vector2(0, -57);
                body.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height - 65f);
                if (title != null) { var heading = title.rectTransform; heading.anchorMin = heading.anchorMax = new Vector2(.5f, 1f); heading.pivot = new Vector2(.5f, 1f); heading.anchoredPosition = new Vector2(0, -12); }
            }
            PositionAt(screenPosition);
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void PositionAt(Vector2 screenPosition)
        {
            if (positionRoot == null || panel == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(positionRoot, screenPosition, null, out var point)) return;
            var panelRect = panel.GetComponent<RectTransform>();
            if (panelRect == null) return;
            var size = panelRect.sizeDelta;
            var bounds = positionRoot.rect;
            point += new Vector2(118f, -72f);
            point.x = Mathf.Clamp(point.x, bounds.xMin + size.x * .5f + 8f, bounds.xMax - size.x * .5f - 8f);
            point.y = Mathf.Clamp(point.y, bounds.yMin + size.y * .5f + 8f, bounds.yMax - size.y * .5f - 8f);
            panelRect.anchoredPosition = point;
        }
    }
}
