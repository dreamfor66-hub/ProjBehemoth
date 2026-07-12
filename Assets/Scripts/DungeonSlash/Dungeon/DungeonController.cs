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
        public DungeonRunState State { get; private set; }
        public event Action<DungeonRoom> RoomEntered;
        public void Configure(DungeonMapView map, NavigationChoiceView choices, DungeonGenerationSettings settings) { mapView = map; navigation = choices; generationSettings = settings; }
        public void BeginRun(int seed)
        {
            State = new DungeonGenerator(generationSettings).Generate(seed);
            RefreshNavigation();
        }
        public void RefreshNavigation()
        {
            mapView.Render(State, hoveredDirection, true);
            navigation.SetVisible(true);
            navigation.Bind(State, Move, SetHoveredDirection);
        }
        public void HideNavigation()
        {
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
            var target = State.Graph.At(State.Player.CurrentRoom.Position + DirectionUtility.Offset(absolute));
            if (target == null || !State.Player.CurrentRoom.Connections.Contains(target.RoomId)) return;
            State.RevealConnection(State.Player.CurrentRoom, target);
            State.Player.MoveTo(target); mapView.Render(State, null, false); RoomEntered?.Invoke(target);
        }
    }
}
