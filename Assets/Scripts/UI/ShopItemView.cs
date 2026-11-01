using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class ShopItemView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI status;
        public EquipmentData Data {get;private set;}
        public ShopOffer Offer { get; private set; }

        public void Configure(Button newButton, TextMeshProUGUI newLabel, Image newIcon = null,
            TextMeshProUGUI newTitle = null, TextMeshProUGUI newStatus = null)
        { button = newButton; label = newLabel; icon = newIcon; title = newTitle; status = newStatus; }

        public void Bind(EquipmentData item, bool affordable, Action<EquipmentData> buy, string unavailableReason = null)
        {
            SetDescriptionVisible(true);
            label.text = title == null ? GameText.Format("ui.shopitemview.79a2a3", item.displayName, item.price, item.description) : item.description;
            if (title != null) title.text = item.displayName;
            if (icon != null) { icon.sprite = item.icon; icon.color = Color.white; icon.enabled = item.icon != null; }
            if (status != null)
            {
                status.text = affordable ? GameText.Format("ui.shop.buy", item.price)
                    : GameText.Format("ui.shop.unavailable", item.price, unavailableReason ?? GameText.Get("ui.shop.unavailablelabel"));
                status.color = affordable ? new Color(1f, .8f, .3f) : new Color(.78f, .8f, .85f);
            }
            button.interactable = affordable;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => { if (button.interactable) buy?.Invoke(item); });
        }
        public void BindCompact(EquipmentData item,Action<EquipmentData> focus)
        {
            Data=item;
            if(title!=null)title.text=item.displayName;
            SetDescriptionVisible(false);
            if(icon!=null){icon.sprite=item.icon;icon.color=Color.white;icon.enabled=item.icon!=null;}
            if(status!=null)status.text=GameText.Format("ui.arcade.price",item.price);
            button.interactable=true;button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>focus(item));
        }
        public void Bind(ShopOffer offer, bool affordable, Action<ShopOffer> buy, string unavailableReason = null)
        {
            Offer = offer; Data = offer.Item; SetDescriptionVisible(true);
            if (title != null) title.text = offer.DisplayName;
            label.text = title == null ? GameText.Format("ui.shopitemview.79a2a3", offer.DisplayName, offer.Price, offer.Description) : offer.Description;
            if (icon != null) { icon.sprite = offer.Icon; icon.color = Color.white; icon.enabled = offer.Icon != null; }
            if (status != null)
            {
                status.text = affordable ? GameText.Format("ui.shop.buy", offer.Price) : GameText.Format("ui.shop.unavailable", offer.Price, unavailableReason ?? GameText.Get("ui.shop.unavailablelabel"));
                status.color = affordable ? new Color(1f, .8f, .3f) : new Color(.78f, .8f, .85f);
            }
            button.interactable = affordable; button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => { if (button.interactable) buy?.Invoke(offer); });
        }
        public void BindCompact(ShopOffer offer, Action<ShopOffer> focus)
        {
            Offer = offer; Data = offer.Item;
            if (title != null) title.text = offer.DisplayName;
            SetDescriptionVisible(false);
            if (icon != null) { icon.sprite = offer.Icon; icon.color = Color.white; icon.enabled = offer.Icon != null; }
            if (status != null) status.text = offer.Purchased ? GameText.Get("ui.shop.soldout") : GameText.Format("ui.arcade.price", offer.Price);
            button.interactable = true; button.onClick.RemoveAllListeners(); button.onClick.AddListener(() => focus(offer));
        }
        public void SetFocused(bool focused)
        {
            button.image.color=focused?new Color32(75,74,96,255):new Color32(25,27,45,255);
            if(title!=null)title.color=focused?new Color32(243,230,191,255):new Color32(164,154,145,255);
        }
        private void SetDescriptionVisible(bool visible)
        {
            // Compact rows use the shared detail area; hide the old scroll surface as well as its text.
            var scroll=label.GetComponentInParent<ScrollRect>(true);
            if(scroll!=null&&scroll.transform.IsChildOf(transform))scroll.gameObject.SetActive(visible);
            label.gameObject.SetActive(visible);
        }
    }
}
