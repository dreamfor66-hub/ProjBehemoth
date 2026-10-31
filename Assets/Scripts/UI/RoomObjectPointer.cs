using UnityEngine;
using UnityEngine.EventSystems;

namespace DungeonSlash
{
    public sealed class RoomObjectPointer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public RoomObjectSlot slot;
        public void OnPointerEnter(PointerEventData data) => slot.OnPointerEnter(data);
        public void OnPointerExit(PointerEventData data) => slot.OnPointerExit(data);
        public void OnPointerDown(PointerEventData data) => slot.OnPointerDown(data);
        public void OnPointerUp(PointerEventData data) => slot.OnPointerUp(data);
    }
}
