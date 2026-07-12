using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    /// <summary>Visited rooms retain every exit they offered; only navigation displays unvisited destination tiles.</summary>
    public sealed class DungeonMapView : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private MapRoomIcon iconPrefab;
        [SerializeField] private Image connectionPrefab;
        [SerializeField] private float spacing = 42f;
        private readonly List<MapRoomIcon> activeIcons = new();
        private DungeonPosition focusPosition;

        public void Configure(RectTransform newContent, MapRoomIcon prefab, Image connection)
        {
            content = newContent;
            iconPrefab = prefab;
            connectionPrefab = connection;
        }

        public void Render(DungeonRunState state, FacingDirection? hoveredDirection = null, bool showSelectionTargets = true)
        {
            if (state == null || content == null || iconPrefab == null) return;
            ClearActiveViews();

            var current = state.Player.CurrentRoom;
            focusPosition = current.Position;
            var currentNeighbours = state.Graph.Neighbors(current).ToList();
            var routes = CollectRoutes(state, current, currentNeighbours, showSelectionTargets);
            var visibleRooms = CollectVisibleRooms(state, currentNeighbours, showSelectionTargets);
            var mapSpacing = GetFittedSpacing(visibleRooms);

            CreateCurrentIcon(current, state.Player.Facing, mapSpacing, routes);
            if (showSelectionTargets)
            {
                foreach (var neighbour in currentNeighbours)
                    CreateReachableIcon(current, neighbour, state.Player.Facing, hoveredDirection, mapSpacing, routes);
            }

            foreach (var room in state.Graph.Rooms.Where(room => room.IsVisited && room != current && (!showSelectionTargets || !currentNeighbours.Contains(room))))
                CreateKnownIcon(room, state.Player.Facing, mapSpacing, routes);
        }

        private static Dictionary<int, HashSet<FacingDirection>> CollectRoutes(DungeonRunState state, DungeonRoom current, IEnumerable<DungeonRoom> currentNeighbours, bool showSelectionTargets)
        {
            var routes = new Dictionary<int, HashSet<FacingDirection>>();
            foreach (var room in state.Graph.Rooms.Where(room => room.IsVisited))
            {
                foreach (var neighbour in state.Graph.Neighbors(room))
                    Add(room.RoomId, DirectionTo(room, neighbour));
            }

            if (showSelectionTargets)
            {
                foreach (var neighbour in currentNeighbours.Where(room => !room.IsVisited))
                    Add(neighbour.RoomId, Opposite(DirectionTo(current, neighbour)));
            }
            return routes;

            void Add(int roomId, FacingDirection direction)
            {
                if (!routes.TryGetValue(roomId, out var directions))
                {
                    directions = new HashSet<FacingDirection>();
                    routes.Add(roomId, directions);
                }
                directions.Add(direction);
            }
        }

        private static List<DungeonRoom> CollectVisibleRooms(DungeonRunState state, IEnumerable<DungeonRoom> currentNeighbours, bool showSelectionTargets)
        {
            var rooms = state.Graph.Rooms.Where(room => room.IsVisited).ToList();
            if (showSelectionTargets)
                rooms.AddRange(currentNeighbours.Where(room => !room.IsVisited));
            return rooms;
        }

        private float GetFittedSpacing(IEnumerable<DungeonRoom> rooms)
        {
            // The viewport clips a long route. It must never shrink the room grid to fit it.
            return spacing;
        }

        private void CreateCurrentIcon(DungeonRoom current, FacingDirection facing, float mapSpacing, Dictionary<int, HashSet<FacingDirection>> routes)
        {
            var icon = Instantiate(iconPrefab, content);
            PlaceIcon(icon, current.Position, mapSpacing);
            icon.Bind(current, MapRoomIconStyle.Current, facing, false, RoutesFor(current, routes));
            activeIcons.Add(icon);
        }

        private void CreateReachableIcon(DungeonRoom current, DungeonRoom target, FacingDirection facing, FacingDirection? hoveredDirection, float mapSpacing, Dictionary<int, HashSet<FacingDirection>> routes)
        {
            var icon = Instantiate(iconPrefab, content);
            PlaceIcon(icon, target.Position, mapSpacing);
            var targetDirection = DirectionTo(current, target);
            var highlighted = hoveredDirection == targetDirection;
            var style = target.IsVisited && target.IsCleared ? MapRoomIconStyle.ClearedExit : target.IsVisited ? MapRoomIconStyle.KnownRoom : MapRoomIconStyle.UnknownReachable;
            icon.Bind(target, style, facing, highlighted, RoutesFor(target, routes));
            activeIcons.Add(icon);
        }

        private void CreateKnownIcon(DungeonRoom room, FacingDirection facing, float mapSpacing, Dictionary<int, HashSet<FacingDirection>> routes)
        {
            var icon = Instantiate(iconPrefab, content);
            PlaceIcon(icon, room.Position, mapSpacing);
            icon.Bind(room, room.IsCleared ? MapRoomIconStyle.ClearedVisited : MapRoomIconStyle.KnownRoom, facing, false, RoutesFor(room, routes));
            activeIcons.Add(icon);
        }

        private static FacingDirection DirectionTo(DungeonRoom from, DungeonRoom to)
        {
            return DirectionUtility.FromDelta(new DungeonPosition(to.Position.X - from.Position.X, to.Position.Y - from.Position.Y));
        }

        private static FacingDirection Opposite(FacingDirection direction) => (FacingDirection)(((int)direction + 2) % 4);

        private static ISet<FacingDirection> RoutesFor(DungeonRoom room, IReadOnlyDictionary<int, HashSet<FacingDirection>> routes)
        {
            return routes.TryGetValue(room.RoomId, out var directions) ? directions : null;
        }

        private Vector2 ToMapPosition(DungeonPosition position, float mapSpacing)
        {
            return new Vector2((position.X - focusPosition.X) * mapSpacing, (position.Y - focusPosition.Y) * mapSpacing);
        }

        private void PlaceIcon(MapRoomIcon icon, DungeonPosition position, float mapSpacing)
        {
            var rect = icon.GetComponent<RectTransform>();
            rect.anchoredPosition = ToMapPosition(position, mapSpacing);
            rect.sizeDelta = Vector2.one * TileSizeFor(mapSpacing);
        }

        private static float TileSizeFor(float mapSpacing) => Mathf.Clamp(mapSpacing * .95f, 24f, 40f);

        private void ClearActiveViews()
        {
            foreach (var icon in activeIcons)
            {
                if (icon == null) continue;
                if (Application.isPlaying) Destroy(icon.gameObject);
                else DestroyImmediate(icon.gameObject);
            }
            activeIcons.Clear();
        }
    }
}
