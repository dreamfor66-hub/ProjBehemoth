using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class RoomObjectSlot : MonoBehaviour
    {
        public Image painting;
        public Button button;
        public RectTransform hitArea;
        public TextMeshProUGUI caption;
        private Action<string> hover;
        private string label;
        [SerializeField] private RoomContactShadow contact;
        [SerializeField] private RoomOpeningPainting inlay;
        public float Depth { get; private set; }
        public RoomObjectData BoundData { get; private set; }
        public void Bind(RoomObjectData data, int view, bool used, Vector2 anchor, float height, Color illumination,
            FloorArtwork.OpeningContour aperture, Action clicked, Action<string> onHover)
        {
            BoundData = data;
            label = data == null ? "" : GameText.Get(used && !string.IsNullOrEmpty(data.usedLabelKey) ? data.usedLabelKey : data.labelKey); hover = onHover;
            if (used && data != null && data.usedForm != null && data.usedForm != data)
            { height *= data.usedForm.displayHeight / Mathf.Max(1, data.displayHeight); data = data.usedForm; used = false; }
            var sprite = data != null ? data.Painting(view, used) : null;
            gameObject.SetActive(sprite != null);
            if (sprite == null) return;
            var rect = (RectTransform)transform;
            rect.anchorMin = rect.anchorMax = anchor; rect.pivot = new Vector2(.5f, 0);
            rect.anchoredPosition = Vector2.zero;
            Depth = data.fitsDoorway ? 1 : anchor.y;
            var normal = data.Painting(view, false);
            var reference = data.Geometry(normal); var current = data.Geometry(sprite);
            var pixelsPerUnit = height / Mathf.Max(1, normal.rect.height * reference.bounds.height);
            // A lid, empty bowl, or collected item changes silhouette without moving/scaling its support.
            if (used && normal != sprite)
                pixelsPerUnit *= normal.rect.width * reference.footprint.width / Mathf.Max(1, sprite.rect.width * current.footprint.width);
            var fullSize = sprite.rect.size * pixelsPerUnit;
            rect.sizeDelta = Vector2.Scale(fullSize, current.bounds.size);
            var supportX = (current.footprint.center.x - current.bounds.xMin) / current.bounds.width;
            rect.pivot = new Vector2(view == 3 ? 1 - supportX : supportX, (current.footprint.yMin - current.bounds.yMin) / current.bounds.height);
            var paintRect = painting.rectTransform;
            paintRect.anchorMin = paintRect.anchorMax = new Vector2(0, 0); paintRect.pivot = Vector2.zero;
            paintRect.sizeDelta = fullSize;
            paintRect.anchoredPosition = -Vector2.Scale(fullSize, current.bounds.position);
            painting.sprite = sprite; painting.preserveAspect = false;
            painting.rectTransform.localScale = new Vector3(view == 3 ? -1 : 1, 1, 1);
            // Mirroring is around the physical painting center, independent of transparent margins.
            if (view == 3) paintRect.anchoredPosition = new Vector2(rect.rect.width + fullSize.x * current.bounds.xMin, paintRect.anchoredPosition.y);
            painting.color = illumination;
            ConfigureContact(data, current, fullSize);
            painting.enabled = !data.fitsDoorway || aperture == null;
            if (data.fitsDoorway && aperture != null)
            {
                rect.anchorMin = rect.anchorMax = aperture.bounds.center;
                rect.pivot = new Vector2(.5f, .5f);
                var parent = (RectTransform)rect.parent;
                rect.sizeDelta = Vector2.Scale(parent.rect.size, aperture.bounds.size);
                PreparePresentation();
                inlay.transform.SetParent(transform, false);
                var surface = inlay.rectTransform; surface.anchorMin = Vector2.zero; surface.anchorMax = Vector2.one; surface.offsetMin = surface.offsetMax = Vector2.zero;
                inlay.gameObject.SetActive(true); inlay.color = illumination; inlay.Set(sprite, current.bounds, aperture, view == 3, view != 0);
            }
            else if (inlay != null) inlay.gameObject.SetActive(false);
            hitArea.gameObject.SetActive(true);
            SyncHitArea();
            button.onClick.RemoveAllListeners(); button.onClick.AddListener(() => { hover?.Invoke(null); clicked?.Invoke(); });
            if (caption != null) caption.gameObject.SetActive(false);
        }
        public void OnPointerEnter(PointerEventData data) => hover?.Invoke(label);
        public void OnPointerExit(PointerEventData data) => hover?.Invoke(null);
        public void OnPointerDown(PointerEventData data) { }
        public void OnPointerUp(PointerEventData data) { }
        private void ConfigureContact(RoomObjectData data, RoomObjectData.PaintingGeometry geometry, Vector2 fullSize)
        {
            if (data.fitsDoorway || data.wallSuspended) { if (contact != null) contact.gameObject.SetActive(false); return; }
            PreparePresentation();
            contact.transform.SetParent(transform, false); contact.transform.SetAsFirstSibling();
            contact.gameObject.SetActive(true); contact.raycastTarget = false; contact.color = new Color(.025f, .025f, .022f, .58f);
            var rect = contact.rectTransform; rect.anchorMin = rect.anchorMax = ((RectTransform)transform).pivot; rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(0, 1);
            var width = geometry.footprint.width * fullSize.x;
            rect.sizeDelta = new Vector2(width * 1.18f, Mathf.Clamp(width * .15f, 7, 38));
        }
        public void PreparePresentation()
        {
            if (contact == null)
            {
                contact = new GameObject("Ground contact", typeof(RectTransform), typeof(CanvasRenderer), typeof(RoomContactShadow)).GetComponent<RoomContactShadow>();
                contact.transform.SetParent(transform, false); contact.transform.SetAsFirstSibling(); contact.raycastTarget = false; contact.gameObject.SetActive(false);
            }
            if (inlay == null)
            {
                inlay = new GameObject("Door leaf inside original masonry", typeof(RectTransform), typeof(CanvasRenderer), typeof(RoomOpeningPainting)).GetComponent<RoomOpeningPainting>();
                inlay.transform.SetParent(transform, false); inlay.raycastTarget = false; inlay.gameObject.SetActive(false);
            }
        }
        // Paintings sit behind actors. Pointer targets live above them and follow the
        // actual painting rectangle across different parent coordinate systems.
        private void LateUpdate() => SyncHitArea();
        private void SyncHitArea()
        {
            if (hitArea == null || BoundData == null) return;
            var rect = (RectTransform)transform; var parent = (RectTransform)hitArea.parent;
            var origin = (Vector2)parent.InverseTransformPoint(rect.position);
            var size = parent.InverseTransformVector(rect.TransformVector(rect.rect.size));
            var padding = BoundData.hitPadding;
            hitArea.anchorMin = hitArea.anchorMax = new Vector2(.5f, .5f);
            hitArea.pivot = rect.pivot;
            hitArea.anchoredPosition = origin - parent.rect.center + (rect.pivot * 2 - Vector2.one) * padding;
            hitArea.sizeDelta = new Vector2(Mathf.Max(64, Mathf.Abs(size.x)) + padding * 2, Mathf.Max(64, Mathf.Abs(size.y)) + padding * 2);
        }
        public void Hide() { gameObject.SetActive(false); if (hitArea != null) hitArea.gameObject.SetActive(false); }
        public void UseInteriorPainting()
        { painting.enabled = false; if (contact != null) contact.gameObject.SetActive(false); }
        private void OnDisable() { hover?.Invoke(null); hover = null; if (hitArea != null) hitArea.gameObject.SetActive(false); }
    }
}
