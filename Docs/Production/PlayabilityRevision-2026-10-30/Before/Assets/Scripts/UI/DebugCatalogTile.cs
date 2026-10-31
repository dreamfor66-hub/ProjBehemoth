using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonSlash
{
    /// <summary>An inspectable icon cell shared by every F1 catalog.</summary>
    public sealed class DebugCatalogTile : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public Button button;
        public Image icon;
        public GameObject selectedFrame;
        public TextMeshProUGUI fallback;
        public Sprite previewIcon;
        private ItemTooltipView tooltip;
        private RectTransform sideAnchor;
        private string heading, body;

        public void Bind(Sprite sprite, string title, string description, bool selected, ItemTooltipView view, RectTransform anchor, Action clicked)
        {
            tooltip?.Hide(this);
            tooltip = view; sideAnchor = anchor; heading = title; body = description;
            icon.sprite = sprite; icon.enabled = sprite != null;
            fallback.gameObject.SetActive(sprite == null);
            fallback.text = string.IsNullOrEmpty(title) ? "?" : title.Substring(0, 1);
            selectedFrame.SetActive(selected);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => clicked());
        }

        public void SetSelected(bool selected) => selectedFrame.SetActive(selected);
        private void Show() => tooltip?.ShowBeside(this, heading, body, sideAnchor != null ? sideAnchor : (RectTransform)transform);
        public void OnPointerEnter(PointerEventData e) => Show();
        public void OnPointerExit(PointerEventData e) => tooltip?.Hide(this);
        public void OnSelect(BaseEventData e) => Show();
        public void OnDeselect(BaseEventData e) => tooltip?.Hide(this);
        private void OnDisable() => tooltip?.Hide(this);
    }
}
