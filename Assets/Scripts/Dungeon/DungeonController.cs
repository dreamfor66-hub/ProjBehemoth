using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonSlash
{
    public sealed class DungeonController : MonoBehaviour
    {
        [SerializeField] private DungeonMapView mapView;
        [SerializeField] private NavigationChoiceView navigation;
        [SerializeField] private DungeonGenerationSettings generationSettings;
        private FacingDirection? hoveredDirection;
        private bool navigationVisible;
        public DungeonRunState State { get; private set; }
        public int Floor { get; private set; } = 1;
        public ExplorationSettings Exploration => generationSettings != null ? generationSettings.exploration : null;
        public event Action<DungeonRoom> RoomEntered;
        public void Configure(DungeonMapView map, NavigationChoiceView choices, DungeonGenerationSettings settings) { mapView = map; navigation = choices; generationSettings = settings; }
        public void BeginRun(int seed) => BeginFloor(seed, 1);
        public void BeginFloor(int seed, int floor)
        {
            Floor = Mathf.Clamp(floor, 1, 3);
            hoveredDirection = null;
            State = new DungeonGenerator(generationSettings).Generate(seed, Floor);
            RefreshNavigation();
        }
        public void BeginNextFloor()
        {
            var nextFloor = Floor + 1;
            var nextSeed = State == null ? nextFloor * 7919 : State.Seed + nextFloor * 7919;
            BeginFloor(nextSeed, nextFloor);
        }
        // Explicit F1 inspection room, separate from ordinary seeded generation.
        public void BeginDebugInspection(DungeonGraph graph)
        {
            hoveredDirection = null;
            State = new DungeonRunState(State?.Seed ?? 0, graph, Floor);
            RefreshNavigation();
        }
        public void RefreshNavigation()
        {
            navigationVisible = true;
            mapView.SetSelectionHandler(MoveAbsolute);
            mapView.Render(State, hoveredDirection, true);
            navigation.SetVisible(true);
            navigation.Bind(State, Move, SetHoveredDirection);
        }
        public void HideNavigation()
        {
            navigationVisible = false;
            mapView.SetSelectionHandler(null);
            hoveredDirection = null;
            navigation.SetVisible(false);
            if (State != null) mapView.Render(State, null, false);
        }

        private void SetHoveredDirection(FacingDirection? direction)
        {
            hoveredDirection = direction;
            if (State != null) mapView.Render(State, hoveredDirection, true);
        }
        private void Move(RelativeDirection relative)
        {
            var absolute = DirectionUtility.ToAbsolute(State.Player.Facing, relative);
            MoveAbsolute(absolute);
        }
        private void MoveAbsolute(FacingDirection absolute)
        {
            if (!navigationVisible || State == null) return;
            var target = State.Graph.At(State.Player.CurrentRoom.Position + DirectionUtility.Offset(absolute));
            if (target == null || !State.Player.CurrentRoom.Connections.Contains(target.RoomId)
                || !State.CanTravel(State.Player.CurrentRoom, target)) return;
            State.RevealConnection(State.Player.CurrentRoom, target);
            State.Player.MoveTo(target); mapView.Render(State, null, false); RoomEntered?.Invoke(target);
        }
        public void TravelThrough(FacingDirection absolute) => MoveAbsolute(absolute);
    }
}
