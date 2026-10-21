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

        public void Bind(PerkData perk, Action<PerkData> selected)
        {
            label.text = GameText.Format("ui.perkchoiceitem.69655f06", perk.displayName, perk.description);
            BindIcon(perk.icon);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => selected(perk));
        }
        public void Bind(EquipmentData relic, Action<EquipmentData> selected)
        {
            label.text = GameText.Format("ui.perkchoiceitem.69655f06", relic.displayName, relic.description);
            BindIcon(relic.icon);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => selected(relic));
        }

        private void BindIcon(Sprite sprite)
        {
            if (icon == null) return;
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }
    }
}
