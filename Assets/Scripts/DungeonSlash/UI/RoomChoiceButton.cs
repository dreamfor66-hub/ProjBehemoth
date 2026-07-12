using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class RoomChoiceButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Text label;
        [SerializeField] private Image icon;
        [SerializeField] private Sprite forwardIcon;
        [SerializeField] private Sprite sideIcon;
        [SerializeField] private Sprite backIcon;
        private FacingDirection absoluteDirection;
        private Action<FacingDirection?> hoverChanged;
        private bool hovered;

        public void Configure(Button newButton, Text newLabel, Image newIcon = null, Sprite newForwardIcon = null, Sprite newSideIcon = null, Sprite newBackIcon = null)
        {
            button = newButton;
            label = newLabel;
            icon = newIcon;
            forwardIcon = newForwardIcon;
            sideIcon = newSideIcon;
            backIcon = newBackIcon;
        }

        public void Bind(RelativeDirection relative, FacingDirection absolute, bool available, Action<RelativeDirection> selected, Action<FacingDirection?> newHoverChanged = null)
        {
            ClearHover();
            hoverChanged = newHoverChanged;
            absoluteDirection = absolute;
            gameObject.SetActive(available);
            if (!available) return;

            // Buttons stay in screen-relative slots; only their destination is converted from the player's Facing.
            GetComponent<RectTransform>().anchoredPosition = PositionFor(relative);
            BindDirectionIcon(relative);
            button.interactable = true;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => selected(relative));
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!gameObject.activeInHierarchy) return;
            hovered = true;
            hoverChanged?.Invoke(absoluteDirection);
        }

        public void OnPointerExit(PointerEventData eventData) => ClearHover();

        private void OnDisable() => ClearHover();

        private void ClearHover()
        {
            if (!hovered) return;
            hovered = false;
            hoverChanged?.Invoke(null);
        }

        private void BindDirectionIcon(RelativeDirection direction)
        {
            var sprite = direction switch
            {
                RelativeDirection.Forward => forwardIcon,
                RelativeDirection.Back => backIcon,
                _ => sideIcon
            };
            var hasSprite = icon != null && sprite != null;
            if (icon != null)
            {
                icon.enabled = hasSprite;
                if (hasSprite)
                {
                    icon.sprite = sprite;
                    icon.preserveAspect = true;
                    icon.rectTransform.localScale = direction == RelativeDirection.Left ? new Vector3(-1f, 1f, 1f) : Vector3.one;
                }
            }
            if (label == null) return;
            label.enabled = !hasSprite;
            label.text = direction switch
            {
                RelativeDirection.Forward => "\uC804\uC9C4",
                RelativeDirection.Left => "\uC67C\uCABD",
                RelativeDirection.Right => "\uC624\uB978\uCABD",
                _ => "\uD6C4\uC9C4"
            };
        }

        private static Vector2 PositionFor(RelativeDirection direction) => direction switch
        {
            RelativeDirection.Forward => new Vector2(0f, 180f),
            RelativeDirection.Right => new Vector2(190f, 8f),
            RelativeDirection.Back => new Vector2(0f, -112f),
            _ => new Vector2(-190f, 8f)
        };
    }
}
