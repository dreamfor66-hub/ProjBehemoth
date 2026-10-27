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
        public event Action<DungeonRoom> RoomEntered;
        public void Configure(DungeonMapView map, NavigationChoiceView choices, DungeonGenerationSettings settings) { mapView = map; navigation = choices; generationSettings = settings; }
        public void BeginRun(int seed)
        {
            Floor = 1;
            State = new DungeonGenerator(generationSettings).Generate(seed, Floor);
            RefreshNavigation();
        }
        public void BeginNextFloor()
        {
            Floor++;
            var nextSeed = State == null ? Floor * 7919 : State.Seed + Floor * 7919;
            State = new DungeonGenerator(generationSettings).Generate(nextSeed, Floor);
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
            if (target == null || !State.Player.CurrentRoom.Connections.Contains(target.RoomId)) return;
            State.RevealConnection(State.Player.CurrentRoom, target);
            State.Player.MoveTo(target); mapView.Render(State, null, false); RoomEntered?.Invoke(target);
        }
    }
}
