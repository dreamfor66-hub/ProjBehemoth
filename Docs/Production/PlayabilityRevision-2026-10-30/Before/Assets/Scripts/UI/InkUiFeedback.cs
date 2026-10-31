using UnityEngine;
using UnityEngine.EventSystems;

namespace DungeonSlash
{
    // On the original Button object so raycasts and touch IDs are not rerouted by the skin.
    public sealed class InkUiFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public InkUiSurface surface;
        bool over, down;
        void Refresh() { if (surface != null) surface.Feedback(over, down); }
        public void OnPointerEnter(PointerEventData e) { over = true; Refresh(); }
        public void OnPointerExit(PointerEventData e) { over = false; Refresh(); }
        public void OnPointerDown(PointerEventData e) { down = true; Refresh(); }
        public void OnPointerUp(PointerEventData e) { down = false; Refresh(); }
        void OnDisable() { over = down = false; Refresh(); }
    }
}
