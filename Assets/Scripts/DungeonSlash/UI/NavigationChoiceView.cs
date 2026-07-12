using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonSlash
{
    public sealed class NavigationChoiceView : MonoBehaviour
    {
        [SerializeField] private List<RoomChoiceButton> choices = new();

        public void Configure(IEnumerable<RoomChoiceButton> newChoices) => choices = newChoices.ToList();

        public void Bind(DungeonRunState state, Action<RelativeDirection> selected, Action<FacingDirection?> hovered = null)
        {
            foreach (RelativeDirection relative in Enum.GetValues(typeof(RelativeDirection)))
            {
                var absolute = DirectionUtility.ToAbsolute(state.Player.Facing, relative);
                var target = state.Graph.At(state.Player.CurrentRoom.Position + DirectionUtility.Offset(absolute));
                var available = target != null && state.Player.CurrentRoom.Connections.Contains(target.RoomId);
                choices[(int)relative].Bind(relative, absolute, available, selected, hovered);
            }
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);
    }
}
