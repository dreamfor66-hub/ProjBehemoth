using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class SpellbookView : MonoBehaviour
    {
        public Button[] slots;
        public TextMeshProUGUI[] slotLabels;
        public Image[] slotIcons, slotFills;
        public Button[] knownButtons;
        public TextMeshProUGUI[] knownLabels;
        public TextMeshProUGUI heading, description, preparedName;
        public Button equip,close;
        public TextMeshProUGUI equipLabel;
        public ScrollRect knownScroll;
        public RunExpansionView owner;
        private RunState state;
        private SwipeDirection direction;
        private SpellData selected;
        public SpellData Selected => selected;
        public SwipeDirection SelectedDirection => direction;

        private void Awake()
        {
            for(var i=0;i<slots.Length;i++){var slot=(SwipeDirection)i;slots[i].onClick.AddListener(()=>SelectSlot(slot));}
            close.onClick.AddListener(()=>owner.HideModal());
            equip.onClick.AddListener(()=>{if(state?.EquipKnownSpell(selected)==true){Render();owner.RefreshSpellbookHud();}});
        }
        public void Open(RunState value)
        {
            state=value;gameObject.SetActive(true);transform.parent.gameObject.SetActive(true);transform.parent.SetAsLastSibling();transform.SetAsLastSibling();
            SelectSlot(System.Enum.GetValues(typeof(SwipeDirection)).Cast<SwipeDirection>().FirstOrDefault(d=>state.Slot(d)!=null));
        }
        public void SelectSlot(SwipeDirection value)
        {
            direction=value;selected=state.Slot(value)??state.Spellbook.FirstOrDefault(s=>s.Data.defaultSlot==value)?.Data;
            knownScroll.verticalNormalizedPosition=1;Render();
        }
        public void Preview(SpellData spell){selected=spell;Render();}
        private void Update(){if(Input.GetKeyDown(KeyCode.Escape))owner.HideModal();}
        private void Render()
        {
            for(var i=0;i<slots.Length;i++)
            {
                var spell=state.Slot((SwipeDirection)i);var runtime=spell!=null?state.RuntimeFor(spell):null;
                slotLabels[i].text=SwipeDirectionUtility.Label((SwipeDirection)i)+(spell==null?"—":runtime.Cooldown>0?Mathf.CeilToInt(runtime.Cooldown).ToString():spell.mpCost.ToString("0"));
                slotIcons[i].sprite=spell?.icon;slotIcons[i].enabled=spell?.icon!=null;
                slotLabels[i].color=spell==null?new Color32(113,107,117,255):spell.Color;
                slots[i].image.color=i==(int)direction?new Color32(75,74,96,255):new Color32(25,27,45,255);
                var amount=spell==null?0:runtime.Cooldown<=0?1:1-runtime.Cooldown/Mathf.Max(.01f,spell.cooldownSeconds*state.Player.SpellCooldownFor(spell));
                slotFills[i].rectTransform.anchorMax=new Vector2(Mathf.Clamp01(amount),1);slotFills[i].color=spell?.Color??Color.clear;
            }
            preparedName.text=state.Slot(direction)?.displayName??GameText.Get("ui.spellbook.empty");
            heading.text=selected?.displayName??GameText.Get("ui.spellbook.empty");
            description.text=selected!=null?GameText.Format("ui.spellbook.metrics",selected.mpCost,selected.castingSeconds*state.Player.SpellCastingFor(selected),selected.cooldownSeconds*state.Player.SpellCooldownFor(selected))+"\n\n"+selected.description:GameText.Get("ui.spellbook.empty.body");
            var list=state.Spellbook.Where(s=>s.Data.defaultSlot==direction).Select(s=>s.Data).ToArray();
            for(var i=0;i<knownButtons.Length;i++)
            {
                knownButtons[i].gameObject.SetActive(i<list.Length);if(i>=list.Length)continue;
                var spell=list[i];knownLabels[i].text=spell.displayName;knownLabels[i].color=selected==spell?new Color32(216,187,98,255):new Color32(222,211,175,255);
                knownButtons[i].onClick.RemoveAllListeners();knownButtons[i].onClick.AddListener(()=>Preview(spell));
            }
            knownScroll.content.sizeDelta=new Vector2(knownScroll.content.sizeDelta.x,Mathf.Max(36,list.Length*20)*4);
            equip.interactable=selected!=null&&state.Slot(direction)!=selected;
            equipLabel.text=GameText.Get(equip.interactable?"ui.spellbook.prepare":"ui.spellbook.prepared");
        }
    }
}
