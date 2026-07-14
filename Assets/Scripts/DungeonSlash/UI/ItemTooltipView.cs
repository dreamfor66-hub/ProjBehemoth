using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    /// <summary>Shared hover-only description panel for the right-hand run inventory.</summary>
    public sealed class ItemTooltipView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text title;
        [SerializeField] private Text description;
        [SerializeField] private RectTransform positionRoot;

        public void Configure(GameObject newPanel, Text newTitle, Text newDescription, RectTransform newPositionRoot)
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
