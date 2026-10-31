using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonSlash
{
    /// <summary>Large destination rooms and small corridor encounters share the actual navigation graph.</summary>
    public sealed class DungeonMapView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private MapRoomIcon iconPrefab;
        [SerializeField] private Image connectionPrefab;
        [SerializeField] private float spacing = 84f;
        [SerializeField] private DungeonMapStyle layout;
        [HideInInspector] public int layoutRevision;
        private readonly List<GameObject> activeViews = new();
        private DungeonRunState renderedState;
        private DungeonPosition focusPosition;
        private System.Action<FacingDirection> selected;
        private Vector2 minimum, maximum;
        private bool dragging;
        private int dragPointer;
        private float zoom = 1f;

        public DungeonMapStyle Layout => layout;
        public void SetSelectionHandler(System.Action<FacingDirection> handler) => selected = handler;
        public void Configure(RectTransform newContent, MapRoomIcon prefab, Image connection)
        { content = newContent; iconPrefab = prefab; connectionPrefab = connection; }

        private void OnEnable() => FocusCurrent();
        private void OnDisable() => dragging = false;
        public void FocusCurrent()
        {
            dragging = false;
            if (content == null) return;
            content.anchoredPosition = Vector2.zero;
            zoom = 1f; content.localScale = Vector3.one;
        }

        public void Render(DungeonRunState state, FacingDirection? hoveredDirection = null, bool showSelectionTargets = true)
        {
            if (state == null || content == null || iconPrefab == null) return;
            ClearActiveViews();
            var current = state.Player.CurrentRoom;
            if (renderedState != state || !focusPosition.Equals(current.Position)) FocusCurrent();
            renderedState = state; focusPosition = current.Position;
            var neighbours = state.Graph.Neighbors(current).ToHashSet();
            // Visits are permanent exploration memory. Generation-time IsRevealed marks a
            // known encounter identity, not permission to expose a distant, unvisited room.
            var visible = state.Graph.Rooms.Where(r => r.IsVisited || r == current
                || showSelectionTargets && neighbours.Contains(r)).ToList();
            var visibleIds = visible.Select(r => r.RoomId).ToHashSet();
            var drawn = new HashSet<string>();
            foreach (var room in visible.Where(r => r.IsVisited))
                foreach (var other in state.Graph.Neighbors(room))
                {
                    var key = room.RoomId < other.RoomId ? $"{room.RoomId}:{other.RoomId}" : $"{other.RoomId}:{room.RoomId}";
                    if (!drawn.Add(key)) continue;
                    var highlighted = room == current && hoveredDirection == DirectionTo(room, other)
                        || other == current && hoveredDirection == DirectionTo(other, room);
                    DrawCorridor(room, other, !visibleIds.Contains(other.RoomId), highlighted);
                }

            minimum = Vector2.zero; maximum = Vector2.zero;
            foreach (var room in visible)
            {
                var icon = Instantiate(iconPrefab, content);
                icon.name = "Map " + room.RoomId + " " + room.Type;
                activeViews.Add(icon.gameObject);
                var rect = (RectTransform)icon.transform;
                rect.anchoredPosition = ToMapPosition(room.Position);
                var size = SizeFor(room); rect.sizeDelta = Vector2.one * size;
                minimum = Vector2.Min(minimum, rect.anchoredPosition - Vector2.one * size * .5f);
                maximum = Vector2.Max(maximum, rect.anchoredPosition + Vector2.one * size * .5f);
                var reachable = showSelectionTargets && neighbours.Contains(room);
                var style = room == current ? MapRoomIconStyle.Current
                    : room.IsCleared ? reachable ? MapRoomIconStyle.ClearedExit : MapRoomIconStyle.ClearedVisited
                    : room.IsVisited || room.IsRevealed || room.IsMajorRoom ? MapRoomIconStyle.KnownRoom
                    : MapRoomIconStyle.UnknownReachable;
                var direction = DirectionTo(current, room);
                icon.Bind(room, style, state.Player.Facing, reachable && hoveredDirection == direction);
                icon.SetSelection(reachable && selected != null ? () => selected(direction) : null);
            }
            ClampPan();
        }

        private float SizeFor(DungeonRoom room) => layout != null ? layout.SizeFor(room) : room.IsMajorRoom || room.Type == RoomEncounterType.Start ? 50 : 24;
        private float CellSpacing => Mathf.Max(layout != null ? layout.cellSpacing : spacing, (layout != null ? layout.roomSize : 50) + 24);
        private Vector2 ToMapPosition(DungeonPosition position) => new((position.X - focusPosition.X) * CellSpacing, (position.Y - focusPosition.Y) * CellSpacing);

        private void DrawCorridor(DungeonRoom from, DungeonRoom to, bool unexploredExit, bool highlighted)
        {
            if (connectionPrefab == null) return;
            var a = ToMapPosition(from.Position); var b = ToMapPosition(to.Position);
            var direction = (b - a).normalized;
            var tileSize = layout != null ? layout.corridorTileSize : 10;
            var gap = layout != null ? layout.corridorTileGap : 4;
            var start = a + direction * (SizeFor(from) * .5f + gap + tileSize * .5f);
            var end = unexploredExit ? (a + b) * .5f : b - direction * (SizeFor(to) * .5f + gap + tileSize * .5f);
            var distance = Vector2.Dot(end - start, direction);
            if (distance < 0) return;
            var count = Mathf.Max(1, Mathf.FloorToInt(distance / (tileSize + gap)) + 1);
            for (var index = 0; index < count; index++)
            {
                var tile = Instantiate(connectionPrefab, content);
                tile.name = unexploredExit ? "Unexplored exit" : "Corridor";
                tile.raycastTarget = false;
                tile.rectTransform.anchoredPosition = count == 1 ? (start + end) * .5f : Vector2.Lerp(start, end, index / (float)(count - 1));
                tile.rectTransform.sizeDelta = Vector2.one * tileSize;
                tile.color = highlighted ? new Color(.85f, .69f, .38f) : unexploredExit
                    ? new Color(.25f, .25f, .24f, 1f - .6f * index / Mathf.Max(1, count - 1)) : new Color(.37f, .36f, .32f);
                activeViews.Add(tile.gameObject);
            }
        }

        private static FacingDirection DirectionTo(DungeonRoom from, DungeonRoom to) =>
            DirectionUtility.FromDelta(new DungeonPosition(to.Position.X - from.Position.X, to.Position.Y - from.Position.Y));

        public void OnBeginDrag(PointerEventData data)
        {
            if (dragging || data.button != PointerEventData.InputButton.Left) return;
            dragging = true; dragPointer = data.pointerId;
        }
        public void OnDrag(PointerEventData data)
        {
            if (!dragging || data.pointerId != dragPointer || content == null) return;
            var viewport = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, data.position, data.pressEventCamera, out var now)
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, data.position - data.delta, data.pressEventCamera, out var before)) return;
            content.anchoredPosition += now - before; ClampPan();
        }
        public void OnEndDrag(PointerEventData data) { if (data.pointerId == dragPointer) dragging = false; }
        public void OnScroll(PointerEventData data)
        {
            if (content == null) return;
            var next = Mathf.Clamp(zoom + data.scrollDelta.y * .1f, .65f, 1.5f);
            if (Mathf.Approximately(next, zoom)) return;
            var viewport = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, data.position, data.enterEventCamera, out var point);
            content.anchoredPosition = point - (point - content.anchoredPosition) * (next / zoom);
            zoom = next; content.localScale = Vector3.one * zoom; ClampPan();
        }
        private void ClampPan()
        {
            if (content == null) return;
            var half = ((RectTransform)transform).rect.size * .5f;
            var position = content.anchoredPosition;
            position.x = Mathf.Clamp(position.x, -maximum.x * zoom - half.x * .25f, -minimum.x * zoom + half.x * .25f);
            position.y = Mathf.Clamp(position.y, -maximum.y * zoom - half.y * .25f, -minimum.y * zoom + half.y * .25f);
            content.anchoredPosition = position;
        }
        private void ClearActiveViews()
        {
            foreach (var view in activeViews)
            {
                if (view == null) continue;
                view.SetActive(false);
                if (Application.isPlaying) Destroy(view); else DestroyImmediate(view);
            }
            activeViews.Clear();
        }
    }
}
