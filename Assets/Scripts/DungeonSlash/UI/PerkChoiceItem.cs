using System;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class PerkChoiceItem : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Text label;
        [SerializeField] private Image icon;

        public void Configure(Button newButton, Text newLabel, Image newIcon = null)
        {
            button = newButton;
            label = newLabel;
            icon = newIcon;
        }

        public void Bind(PerkData perk, Action<PerkData> selected)
        {
            label.text = $"{perk.displayName}\n<size=13>{perk.description}</size>";
            BindIcon(perk.icon);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => selected(perk));
        }
        public void Bind(EquipmentData relic, Action<EquipmentData> selected)
        {
            label.text = $"{relic.displayName}\n<size=13>{relic.description}</size>";
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
