using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DungeonSlash
{
    public sealed class MenuChoiceFocus:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler
    {
        public TextMeshProUGUI label;public GameObject marker;
        private bool hovered,selected;
        public void OnPointerEnter(PointerEventData data){hovered=true;if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(gameObject);Refresh();}
        public void OnPointerExit(PointerEventData data){hovered=false;Refresh();}
        public void OnSelect(BaseEventData data){selected=true;Refresh();}
        public void OnDeselect(BaseEventData data){selected=false;Refresh();}
        private void OnDisable(){hovered=selected=false;Refresh();}
        private void Refresh(){var active=hovered||selected;if(marker!=null)marker.SetActive(active);if(label!=null)label.color=active?new Color32(216,187,98,255):new Color32(222,211,175,255);}
    }
}
