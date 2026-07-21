using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class RunHudIconView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private Text stackLabel;

        private ItemTooltipView tooltip;
        private string itemName;
        private string itemDescription;
        private bool hasItem;
        private Action removed;

        public void Configure(Button newButton, Image newIconImage, Text newStackLabel)
        {
            button = newButton;
            iconImage = newIconImage;
            stackLabel = newStackLabel;
        }

        public void Bind(PerkData perk, int stacks, ItemTooltipView newTooltip, Action remove = null)
        {
            Bind(perk == null ? null : perk.icon, perk?.displayName, perk?.description, stacks, null, remove, newTooltip);
        }

        public void Bind(EquipmentData item, ItemTooltipView newTooltip, Action clicked = null, Action remove = null)
        {
            Bind(item == null ? null : item.icon, item?.displayName, item?.description, 1, clicked, remove, newTooltip);
        }

        public void BindEmpty(ItemTooltipView newTooltip)
        {
            Bind(null, null, null, 0, null, null, newTooltip);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (hasItem) tooltip?.Show(itemName, itemDescription, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData) => tooltip?.Hide();
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right || !hasItem || removed == null) return;
            tooltip?.Hide();
            removed();
        }
        private void OnDisable() => tooltip?.Hide();

        private void Bind(Sprite icon, string newItemName, string newItemDescription, int stacks, Action clicked, Action remove, ItemTooltipView newTooltip)
        {
            tooltip?.Hide();
            tooltip = newTooltip;
            itemName = newItemName;
            itemDescription = newItemDescription;
            hasItem = !string.IsNullOrEmpty(itemName);
            removed = remove;

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }
            if (stackLabel != null)
            {
                stackLabel.enabled = stacks > 1;
                stackLabel.text = stacks > 1 ? $"x{stacks}" : string.Empty;
            }
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.interactable = hasItem;
            if (clicked != null) button.onClick.AddListener(() => clicked());
        }
    }
}
