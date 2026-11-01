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
        public Button previousPage, nextPage;
        public TextMeshProUGUI pageLabel;
        private int page;
        private IReadOnlyList<EquipmentData> lastStock;
        private IReadOnlyList<ShopOffer> lastOffers;
        private ShopOffer focusedOffer;

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
            if (!ReferenceEquals(lastStock, equipment)) { lastStock = equipment; page = 0; focused = null; }
            var pages = Mathf.Max(1, Mathf.CeilToInt(equipment.Count / (float)Mathf.Max(1, items.Count)));
            page = Mathf.Clamp(page, 0, pages - 1);
            if (pageLabel != null) { pageLabel.gameObject.SetActive(pages > 1); pageLabel.text = GameText.Format("ui.shop.page", page + 1, pages); }
            if (previousPage != null)
            { previousPage.gameObject.SetActive(pages > 1); previousPage.interactable = page > 0; previousPage.onClick.RemoveAllListeners(); previousPage.onClick.AddListener(() => { page--; focused = null; Show(equipment, run, shop, buy, leave); }); }
            if (nextPage != null)
            { nextPage.gameObject.SetActive(pages > 1); nextPage.interactable = page + 1 < pages; nextPage.onClick.RemoveAllListeners(); nextPage.onClick.AddListener(() => { page++; focused = null; Show(equipment, run, shop, buy, leave); }); }
            if (goldLabel != null) goldLabel.text = GameText.Format("ui.hud.gold", run.Gold);
            for (var index = 0; index < items.Count; index++)
            {
                var stockIndex = page * items.Count + index;
                var visible = stockIndex < equipment.Count;
                items[index].gameObject.SetActive(visible);
                if (!visible) continue;
                var item = equipment[stockIndex];
                var affordable = shop != null ? shop.CanPurchase(run, item) : (!run.EquippedItems.Contains(item) && run.Gold >= item.price);
                if(compact)items[index].BindCompact(item,p=>Focus(p,run,shop,buy));
                else items[index].Bind(item, affordable, buy, shop?.UnavailableReason(run, item));
            }
            if(compact&&equipment.Count>0)Focus(focused!=null&&equipment.Skip(page*items.Count).Take(items.Count).Contains(focused)?focused:equipment[page*items.Count],run,shop,buy);
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
        public void Show(IReadOnlyList<ShopOffer> stock, RunState run, ShopSystem shop, Action<ShopOffer> buy, Action leave)
        {
            panel.SetActive(true); panel.transform.SetAsLastSibling();
            if (!ReferenceEquals(lastOffers, stock)) { lastOffers = stock; page = 0; focusedOffer = null; }
            var pages = Mathf.Max(1, Mathf.CeilToInt(stock.Count / (float)Mathf.Max(1, items.Count)));
            page = Mathf.Clamp(page, 0, pages - 1);
            if (pageLabel != null) { pageLabel.gameObject.SetActive(pages > 1); pageLabel.text = GameText.Format("ui.shop.page", page + 1, pages); }
            if (previousPage != null) { previousPage.gameObject.SetActive(pages > 1); previousPage.interactable = page > 0; previousPage.onClick.RemoveAllListeners(); previousPage.onClick.AddListener(() => { page--; focusedOffer = null; Show(stock, run, shop, buy, leave); }); }
            if (nextPage != null) { nextPage.gameObject.SetActive(pages > 1); nextPage.interactable = page + 1 < pages; nextPage.onClick.RemoveAllListeners(); nextPage.onClick.AddListener(() => { page++; focusedOffer = null; Show(stock, run, shop, buy, leave); }); }
            if (goldLabel != null) goldLabel.text = GameText.Format("ui.hud.gold", run.Gold);
            for (var index = 0; index < items.Count; index++)
            {
                var slot = page * items.Count + index; items[index].gameObject.SetActive(slot < stock.Count);
                if (slot >= stock.Count) continue;
                var offer = stock[slot];
                if (compact) items[index].BindCompact(offer, value => Focus(value, run, shop, buy));
                else items[index].Bind(offer, shop.CanPurchase(run, offer), buy, shop.UnavailableReason(run, offer));
            }
            if (compact && stock.Count > 0) Focus(focusedOffer != null && stock.Skip(page * items.Count).Take(items.Count).Contains(focusedOffer) ? focusedOffer : stock[page * items.Count], run, shop, buy);
            else if (compact) { if (focusedTitle != null) focusedTitle.text = string.Empty; if (focusedDescription != null) focusedDescription.text = GameText.Get("ui.shop.soldout"); if (purchase != null) purchase.interactable = false; }
            leaveButton.onClick.RemoveAllListeners(); leaveButton.onClick.AddListener(() => leave());
        }
        private void Focus(ShopOffer offer, RunState run, ShopSystem shop, Action<ShopOffer> buy)
        {
            foreach (var card in items) card.SetFocused(card.Offer == offer);
            focusedOffer = offer;
            if (focusedTitle != null) focusedTitle.text = offer.DisplayName;
            if (focusedDescription != null) focusedDescription.text = focusedTitle != null ? offer.Description : offer.DisplayName + "\n" + offer.Description;
            focusedTitle?.GetComponent<TypographyRoleBinding>()?.Apply(); focusedDescription?.GetComponent<TypographyRoleBinding>()?.Apply();
            if (purchase != null) { purchase.interactable = shop.CanPurchase(run, offer); purchase.onClick.RemoveAllListeners(); purchase.onClick.AddListener(() => buy(offer)); }
            if (purchaseLabel != null) purchaseLabel.text = shop.CanPurchase(run, offer) ? GameText.Format("ui.arcade.buy", offer.Price) : shop.UnavailableReason(run, offer);
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }
    }
}
