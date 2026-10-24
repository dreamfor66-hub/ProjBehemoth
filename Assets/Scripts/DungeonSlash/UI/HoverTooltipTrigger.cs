using UnityEngine;
using UnityEngine.EventSystems;

namespace DungeonSlash
{
    /// <summary>Reusable anchor for item, perk, spell or localized UI descriptions.</summary>
    public sealed class HoverTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public ItemTooltipView view;
        public RectTransform anchor;
        public string titleKey, descriptionKey;
        [SerializeField] private string heading;
        [SerializeField, TextArea] private string body;
        private bool hovered, focused;

        public void Bind(EquipmentData item) => Bind(item != null ? item.displayName : "", item != null ? item.description : "");
        public void Bind(PerkData perk) => Bind(perk != null ? perk.displayName : "", perk != null ? perk.description : "");
        public void Bind(string title, string description)
        {
            heading=title;body=description;
            if(hovered||focused)Show();else view?.Hide(this);
        }
        private void Show()
        {
            var title=string.IsNullOrEmpty(titleKey)?heading:GameText.Get(titleKey);
            var description=string.IsNullOrEmpty(descriptionKey)?body:GameText.Get(descriptionKey);
            if(string.IsNullOrWhiteSpace(description)){view?.Hide(this);return;}
            view?.ShowBeside(this,title,description,anchor!=null?anchor:(RectTransform)transform);
        }
        public void OnPointerEnter(PointerEventData e){hovered=true;Show();}
        public void OnPointerExit(PointerEventData e){hovered=false;if(!focused)view?.Hide(this);}
        public void OnSelect(BaseEventData e){focused=true;Show();}
        public void OnDeselect(BaseEventData e){focused=false;if(!hovered)view?.Hide(this);}
        private void OnDisable(){hovered=focused=false;view?.Hide(this);}
    }
}
