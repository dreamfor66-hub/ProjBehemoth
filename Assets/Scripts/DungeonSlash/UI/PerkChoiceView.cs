using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class PerkChoiceView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private List<PerkChoiceItem> items = new();
        [SerializeField] private TextMeshProUGUI title;

        public void Configure(GameObject newPanel, IEnumerable<PerkChoiceItem> newItems, TextMeshProUGUI newTitle = null)
        {
            panel = newPanel;
            items = newItems.ToList();
            title = newTitle;
        }

        public void Show(IReadOnlyList<PerkData> perks, Action<PerkData> selected)
        {
            if (title != null) title.text = GameText.Get("ui.perkchoiceview.62fc6faf");
            panel.SetActive(true);
            for (var i = 0; i < items.Count; i++) items[i].gameObject.SetActive(i < perks.Count);
            for (var i = 0; i < perks.Count; i++) items[i].Bind(perks[i], selected);
        }

        public void ShowRelics(IReadOnlyList<EquipmentData> relics, Action<EquipmentData> selected)
        {
            if (title != null) title.text = GameText.Get("ui.perkchoiceview.f79abb6d");
            panel.SetActive(true);
            for (var i = 0; i < items.Count; i++) items[i].gameObject.SetActive(i < relics.Count);
            for (var i = 0; i < relics.Count; i++) items[i].Bind(relics[i], selected);
        }

        public void Hide() => panel.SetActive(false);
        public void SetTitle(string text) { if (title != null) title.text = text; }
    }
}
