using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DungeonSlash
{
    public sealed class ShopView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private List<ShopItemView> items = new();
        [SerializeField] private Button leaveButton;
        [SerializeField] private TextMeshProUGUI goldLabel;
        public bool compact;
        public TextMeshProUGUI focusedDescription;
        public TextMeshProUGUI focusedTitle;
        public Button purchase;
        public TextMeshProUGUI purchaseLabel;
        private EquipmentData focused;

        public void Configure(GameObject newPanel, IEnumerable<ShopItemView> newItems, Button newLeaveButton, TextMeshProUGUI newGoldLabel = null)
        {
            panel = newPanel;
            items = newItems.ToList();
            leaveButton = newLeaveButton;
            goldLabel = newGoldLabel;
        }

        public void Show(IReadOnlyList<EquipmentData> equipment, RunState run, ShopSystem shop, Action<EquipmentData> buy, Action leave)
        {
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            if (goldLabel != null) goldLabel.text = GameText.Format("ui.hud.gold", run.Gold);
            for (var index = 0; index < items.Count; index++)
            {
                var visible = index < equipment.Count;
                items[index].gameObject.SetActive(visible);
                if (!visible) continue;
                var item = equipment[index];
                var affordable = shop != null ? shop.CanPurchase(run, item) : (!run.EquippedItems.Contains(item) && run.Gold >= item.price);
                if(compact)items[index].BindCompact(item,p=>Focus(p,run,shop,buy));
                else items[index].Bind(item, affordable, buy, shop?.UnavailableReason(run, item));
            }
            if(compact&&equipment.Count>0)Focus(focused!=null&&equipment.Contains(focused)?focused:equipment[0],run,shop,buy);
            leaveButton.onClick.RemoveAllListeners();
            leaveButton.onClick.AddListener(() => leave());
        }
        private void Focus(EquipmentData item,RunState run,ShopSystem shop,Action<EquipmentData> buy)
        {
            foreach(var card in items)card.SetFocused(card.Data==item);
            focused=item;
            if(focusedTitle!=null)focusedTitle.text=item.displayName;
            focusedDescription.text=focusedTitle!=null?item.description:item.displayName+"\n"+item.description;
            focusedTitle?.GetComponent<TypographyRoleBinding>()?.Apply();
            focusedDescription.GetComponent<TypographyRoleBinding>()?.Apply();
            purchase.interactable=shop.CanPurchase(run,item);purchaseLabel.text=purchase.interactable?GameText.Format("ui.arcade.buy",item.price):shop.UnavailableReason(run,item);
            purchase.onClick.RemoveAllListeners();purchase.onClick.AddListener(()=>buy(item));
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }
    }
}
