using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonSlash
{
    public sealed class NavigationChoiceView : MonoBehaviour
    {
        [SerializeField] private List<RoomChoiceButton> choices = new();
        [Tooltip("Optional saved illustration slots, ordered Forward / Left / Right / Back. Empty preserves the existing default layout.")]
        public Vector2[] artworkPositions = Array.Empty<Vector2>();

        public void Configure(IEnumerable<RoomChoiceButton> newChoices) => choices = newChoices.ToList();

        public void Bind(DungeonRunState state, Action<RelativeDirection> selected, Action<FacingDirection?> hovered = null)
        {
            foreach (RelativeDirection relative in Enum.GetValues(typeof(RelativeDirection)))
            {
                var absolute = DirectionUtility.ToAbsolute(state.Player.Facing, relative);
                var target = state.Graph.At(state.Player.CurrentRoom.Position + DirectionUtility.Offset(absolute));
                var available = target != null && state.Player.CurrentRoom.Connections.Contains(target.RoomId)
                    && state.CanTravel(state.Player.CurrentRoom, target);
                var index=(int)relative;
                Vector2? artworkPosition=artworkPositions!=null&&index<artworkPositions.Length?artworkPositions[index]:null;
                choices[index].Bind(relative, absolute, available, selected, hovered, artworkPosition);
            }
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);
    }
}
