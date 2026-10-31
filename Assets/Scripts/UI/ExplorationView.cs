using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class ExplorationView : MonoBehaviour
    {
        public RectTransform objectLayer;
        public RectTransform interactionLayer;
        [Tooltip("The combat gesture surface yields pointer events while exploring.")]
        public Graphic combatInputSurface;
        public RoomObjectSlot[] slots;
        public CanvasGroup narration;
        public TextMeshProUGUI narrationText;
        public TextMeshProUGUI hoverText;
        public TextMeshProUGUI inventoryKeys;
        public void ShowKeys(int count)
        {
            if (inventoryKeys == null) return;
            inventoryKeys.text = count > 0 ? GameText.Format("ui.explore.inventory.key", count) : "";
        }
        private Coroutine toast;
        private int nextSlot;
        private StageBackdropView backdrop;
        private FloorArtwork Floor => backdrop != null && backdrop.run?.State != null && backdrop.artwork?.Length > 0
            ? backdrop.artwork[Mathf.Clamp(backdrop.run.State.CurrentAct - 1, 0, backdrop.artwork.Length - 1)] : null;
        public void SetBackdrop(StageBackdropView value) => backdrop = value;
        public int VisibleObjects => nextSlot;
        public void Clear()
        {
            nextSlot = 0;
            foreach (var slot in slots) if (slot != null) slot.Hide();
            Hover(null);
        }
        public void SetVisible(bool visible)
        {
            if (combatInputSurface != null) combatInputSurface.raycastTarget = !visible;
            if (objectLayer != null) objectLayer.gameObject.SetActive(visible);
            if (interactionLayer != null) interactionLayer.gameObject.SetActive(visible);
            if (!visible) { Hover(null); if (toast != null) StopCoroutine(toast); toast = null; if (narration != null) narration.alpha = 0; }
        }
        public void Add(RoomObjectData art, int view, bool used, Vector2 anchor, float perspective, bool subtle, Action clicked)
        {
            if (art == null || slots == null || nextSlot >= slots.Length) return;
            var slot = slots[nextSlot++];
            var scale = objectLayer != null ? objectLayer.rect.height / 1000f : 1;
            var height = art.displayHeight * perspective * scale;
            var floor = Floor;
            var illumination = floor != null ? floor.objectIllumination : Color.white;
            var aperture = art.fitsDoorway && floor?.openingContours?.Length >= 3 ? floor.openingContours[view == 0 ? 0 : view == 1 ? 1 : 2] : null;
            slot.Bind(art, view, used, anchor, height, illumination, aperture, clicked, Hover);
            if (art.roomInterior && Floor?.campInterior != null) slot.UseInteriorPainting();
            // Paint rearward props before nearer props, regardless of content registration order.
            for (var i = 0; i < nextSlot; i++) for (var j = i + 1; j < nextSlot; j++)
                if (slots[i].Depth < slots[j].Depth && slots[i].transform.GetSiblingIndex() < slots[j].transform.GetSiblingIndex())
                    slots[j].transform.SetSiblingIndex(slots[i].transform.GetSiblingIndex());
        }
        public void AddHiddenWall(RoomObjectData wall, int view, Vector2 anchor, Action clicked)
        {
            var before = nextSlot;
            Add(wall, view, false, anchor, 1, false, clicked);
            if (nextSlot > before) slots[before].UseHiddenWall();
        }
        public void Say(string key)
        {
            if (narration == null || narrationText == null) return;
            if (toast != null) StopCoroutine(toast);
            narrationText.text = GameText.Get(key); toast = StartCoroutine(FadeNarration());
        }
        private IEnumerator FadeNarration()
        {
            narration.alpha = 1;
            yield return new WaitForSecondsRealtime(2.2f);
            for (var t = 0f; t < .35f; t += Time.unscaledDeltaTime) { narration.alpha = 1 - t / .35f; yield return null; }
            narration.alpha = 0; toast = null;
        }
        private void Hover(string value) { if (hoverText != null) { hoverText.text = value ?? ""; hoverText.gameObject.SetActive(!string.IsNullOrEmpty(value)); } }
        public Vector2 Corner(int relative)
        {
            var index = (relative % 4 + 4) % 4;
            if (Floor?.floorObjectCorners?.Length == 4) return Floor.floorObjectCorners[index];
            return index switch { 0 => new(.315f, .235f), 1 => new(.70f, .235f), 2 => new(.88f, .12f), _ => new(.315f, .17f) };
        }
        // Choose once from the authored far-floor sockets. Painting geometry and the actual
        // navigation UI determine visibility; do not add per-NPC duplicate placement settings.
        public int FirstEncounterCorner(RoomObjectData art, int preferred)
        {
            var navigation = FindFirstObjectByType<NavigationChoiceView>(FindObjectsInactive.Include);
            if (objectLayer == null || art?.front == null || navigation == null) return preferred;
            var blockers = navigation.GetComponentsInChildren<RoomChoiceButton>(true);
            var corners = new Vector3[4];
            float CoveredArea(int candidate)
            {
                var anchor = Corner(candidate);
                var geometry = art.Geometry(art.front);
                var height = art.displayHeight * Perspective(anchor) * objectLayer.rect.height / 1000f;
                var width = height * art.front.rect.width * geometry.bounds.width
                    / Mathf.Max(1, art.front.rect.height * geometry.bounds.height);
                var support = new Vector2((geometry.footprint.center.x - geometry.bounds.xMin) / geometry.bounds.width,
                    (geometry.footprint.yMin - geometry.bounds.yMin) / geometry.bounds.height);
                var point = objectLayer.rect.min + Vector2.Scale(objectLayer.rect.size, anchor);
                var painting = new Rect(point - Vector2.Scale(new Vector2(width, height), support), new Vector2(width, height));
                var covered = 0f;
                foreach (var blocker in blockers)
                {
                    ((RectTransform)blocker.transform).GetWorldCorners(corners);
                    var min = (Vector2)objectLayer.InverseTransformPoint(corners[0]);
                    var max = (Vector2)objectLayer.InverseTransformPoint(corners[2]);
                    covered += Mathf.Max(0, Mathf.Min(painting.xMax, max.x) - Mathf.Max(painting.xMin, min.x))
                        * Mathf.Max(0, Mathf.Min(painting.yMax, max.y) - Mathf.Max(painting.yMin, min.y));
                }
                return covered;
            }
            var other = 1 - preferred;
            return CoveredArea(other) + .01f < CoveredArea(preferred) ? other : preferred;
        }
        public static float Perspective(Vector2 groundPoint) => .70f + (.38f - groundPoint.y) * 1.45f;
        public Vector2 KeyCorner(int relative)
        {
            var point = Corner(relative) + new Vector2(.025f, .015f);
            // Small ground clues need a clear silhouette outside the foreground sword/shield.
            point.x = point.x < .5f ? Mathf.Min(point.x, .30f) : Mathf.Max(point.x, .79f);
            return point;
        }
        public Vector2 Wall(RelativeDirection relative)
        {
            var index = relative == RelativeDirection.Forward ? 0 : relative == RelativeDirection.Left ? 1 : 2;
            if (Floor?.wallObjectSupports?.Length == 3) return Floor.wallObjectSupports[index];
            return index switch { 0 => new(.50f, .35f), 1 => new(.24f, .29f), _ => new(.81f, .29f) };
        }
        public Vector2 Switch(RelativeDirection relative) => Wall(relative) + new Vector2(relative == RelativeDirection.Forward ? -.07f : relative == RelativeDirection.Right ? .005f : -.005f, .14f);
        public Vector2 EntranceClue(RelativeDirection relative) => Wall(relative) + (relative switch
        { RelativeDirection.Forward => new Vector2(-.13f, -.115f), RelativeDirection.Left => new Vector2(-.02f, -.13f), _ => new Vector2(.01f, -.13f) });
    }
}
