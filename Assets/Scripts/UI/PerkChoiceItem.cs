using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DungeonSlash
{
    public sealed class PerkChoiceItem : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Image icon;
        public TextMeshProUGUI effect;
        public Image selectedMark;
        public ScrollRect effectScroll;

        public void Configure(Button newButton, TextMeshProUGUI newLabel, Image newIcon = null)
        {
            button = newButton;
            label = newLabel;
            icon = newIcon;
        }

        public void Bind(PerkData perk, Action<PerkData> selected, bool compact=false)
        {
            label.text = compact?perk.displayName:GameText.Format("ui.perkchoiceitem.69655f06", perk.displayName, perk.description);
            BindEffect(perk.description);
            BindIcon(perk.icon);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => selected(perk));
        }
        public void Bind(EquipmentData relic, Action<EquipmentData> selected, bool compact=false)
        {
            label.text = compact?relic.displayName:GameText.Format("ui.perkchoiceitem.69655f06", relic.displayName, relic.description);
            BindEffect(relic.description);
            BindIcon(relic.icon);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => selected(relic));
        }

        private void BindIcon(Sprite sprite)
        {
            if (icon == null) return;
            icon.sprite = sprite;
            icon.color = Color.white;
            icon.enabled = sprite != null;
        }
        private void BindEffect(string description)
        {
            if(effect==null)return;effect.text=description;
            if(effectScroll==null)return;
            Canvas.ForceUpdateCanvases();
            var height=effect.GetPreferredValues(description,effect.rectTransform.rect.width,Mathf.Infinity).y;
            effect.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(effectScroll.viewport.rect.height,height));
            effectScroll.StopMovement();effectScroll.verticalNormalizedPosition=1;
        }
        public void SetFocused(bool focused)
        {
            if(focused&&EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(button.gameObject);
            button.image.color=focused?new Color32(75,74,96,255):new Color32(25,27,45,255);
            label.color=focused?new Color32(243,230,191,255):new Color32(164,154,145,255);
            if(effect!=null)effect.color=focused?new Color32(224,212,182,255):new Color32(192,185,168,255);
            if(selectedMark!=null)selectedMark.enabled=focused;
        }
    }
}
