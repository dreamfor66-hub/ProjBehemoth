using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class RunHudIconView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private Text stackLabel;

        private ItemTooltipView tooltip;
        private string itemName;
        private string itemDescription;
        private bool hasItem;

        public void Configure(Button newButton, Image newIconImage, Text newStackLabel)
        {
            button = newButton;
            iconImage = newIconImage;
            stackLabel = newStackLabel;
        }

        public void Bind(PerkData perk, int stacks, ItemTooltipView newTooltip)
        {
            Bind(perk == null ? null : perk.icon, perk?.displayName, perk?.description, stacks, null, newTooltip);
        }

        public void Bind(EquipmentData item, ItemTooltipView newTooltip, Action clicked = null)
        {
            Bind(item == null ? null : item.icon, item?.displayName, item?.description, 1, clicked, newTooltip);
        }

        public void BindEmpty(ItemTooltipView newTooltip)
        {
            Bind(null, null, null, 0, null, newTooltip);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (hasItem) tooltip?.Show(itemName, itemDescription, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData) => tooltip?.Hide();
        private void OnDisable() => tooltip?.Hide();

        private void Bind(Sprite icon, string newItemName, string newItemDescription, int stacks, Action clicked, ItemTooltipView newTooltip)
        {
            tooltip?.Hide();
            tooltip = newTooltip;
            itemName = newItemName;
            itemDescription = newItemDescription;
            hasItem = !string.IsNullOrEmpty(itemName);

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
