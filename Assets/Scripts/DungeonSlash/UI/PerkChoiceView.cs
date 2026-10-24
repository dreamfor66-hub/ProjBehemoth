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
        public bool compact;
        public TextMeshProUGUI focusedDescription;
        public Button confirm;

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
            panel.transform.SetAsLastSibling();
            for (var i = 0; i < items.Count; i++) items[i].gameObject.SetActive(i < perks.Count);
            for (var i = 0; i < perks.Count; i++)
            {var data=perks[i];var card=items[i];card.Bind(data,compact?p=>Focus(p.displayName,p.description,()=>selected(p),card):selected,compact);}
            if(compact&&perks.Count>0)Focus(perks[0].displayName,perks[0].description,()=>selected(perks[0]),items[0]);
        }

        public void ShowRelics(IReadOnlyList<EquipmentData> relics, Action<EquipmentData> selected)
        {
            if (title != null) title.text = GameText.Get("ui.perkchoiceview.f79abb6d");
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            for (var i = 0; i < items.Count; i++) items[i].gameObject.SetActive(i < relics.Count);
            for (var i = 0; i < relics.Count; i++)
            {var data=relics[i];var card=items[i];card.Bind(data,compact?p=>Focus(p.displayName,p.description,()=>selected(p),card):selected,compact);}
            if(compact&&relics.Count>0)Focus(relics[0].displayName,relics[0].description,()=>selected(relics[0]),items[0]);
        }

        private void Focus(string name,string effect,Action selected,PerkChoiceItem chosen)
        {
            foreach(var item in items)item.SetFocused(item==chosen);
            focusedDescription.text=name+"\n"+effect;confirm.onClick.RemoveAllListeners();confirm.onClick.AddListener(()=>selected());
        }

        public void Hide() => panel.SetActive(false);
        public void SetTitle(string text) { if (title != null) title.text = text; }
    }
}
