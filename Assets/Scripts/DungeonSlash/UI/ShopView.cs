using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class ShopView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private List<ShopItemView> items = new();
        [SerializeField] private Button leaveButton;

        public void Configure(GameObject newPanel, IEnumerable<ShopItemView> newItems, Button newLeaveButton)
        {
            panel = newPanel;
            items = newItems.ToList();
            leaveButton = newLeaveButton;
        }

        public void Show(IReadOnlyList<EquipmentData> equipment, RunState run, ShopSystem shop, Action<EquipmentData> buy, Action leave)
        {
            panel.SetActive(true);
            var canBuyAnything = false;
            for (var index = 0; index < items.Count; index++)
            {
                var visible = index < equipment.Count;
                items[index].gameObject.SetActive(visible);
                if (!visible) continue;
                var item = equipment[index];
                var affordable = shop != null ? shop.CanPurchase(run, item) : (!run.EquippedItems.Contains(item) && run.Gold >= item.price);
                canBuyAnything |= affordable;
                items[index].Bind(item, affordable, buy);
            }
            leaveButton.onClick.RemoveAllListeners();
            leaveButton.onClick.AddListener(() => leave());
            if (!canBuyAnything) leave();
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }
    }
}
