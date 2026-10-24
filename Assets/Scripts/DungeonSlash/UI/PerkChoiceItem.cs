using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class PerkChoiceItem : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Image icon;

        public void Configure(Button newButton, TextMeshProUGUI newLabel, Image newIcon = null)
        {
            button = newButton;
            label = newLabel;
            icon = newIcon;
        }

        public void Bind(PerkData perk, Action<PerkData> selected, bool compact=false)
        {
            label.text = compact?perk.displayName:GameText.Format("ui.perkchoiceitem.69655f06", perk.displayName, perk.description);
            BindIcon(perk.icon);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => selected(perk));
        }
        public void Bind(EquipmentData relic, Action<EquipmentData> selected, bool compact=false)
        {
            label.text = compact?relic.displayName:GameText.Format("ui.perkchoiceitem.69655f06", relic.displayName, relic.description);
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
        public void SetFocused(bool focused)
        {
            button.image.color=focused?new Color32(75,74,96,255):new Color32(25,27,45,255);
            label.color=focused?new Color32(243,230,191,255):new Color32(164,154,145,255);
        }
    }
}
