using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public enum MapRoomIconStyle
    {
        Current,
        UnknownReachable,
        KnownRoom,
        ClearedVisited,
        ClearedExit
    }

    public sealed class MapRoomIcon : MonoBehaviour
    {
        [SerializeField] private Image image;
        [SerializeField] private Text label;
        [SerializeField] private Text facingArrow;
        [SerializeField] private Image northRoute;
        [SerializeField] private Image eastRoute;
        [SerializeField] private Image southRoute;
        [SerializeField] private Image westRoute;
        private Color baseColor;
        private bool pulse;
        private bool highlighted;

        public void Configure(Image newImage, Text newLabel) => Configure(newImage, newLabel, null, null, null, null, null);

        public void Configure(Image newImage, Text newLabel, Text newFacingArrow) => Configure(newImage, newLabel, newFacingArrow, null, null, null, null);

        public void Configure(Image newImage, Text newLabel, Text newFacingArrow, Image newNorthRoute, Image newEastRoute, Image newSouthRoute, Image newWestRoute)
        {
            image = newImage;
            label = newLabel;
            facingArrow = newFacingArrow;
            northRoute = newNorthRoute;
            eastRoute = newEastRoute;
            southRoute = newSouthRoute;
            westRoute = newWestRoute;
        }

        public void Bind(DungeonRoom room, bool current, FacingDirection facing)
        {
            Bind(room, current ? MapRoomIconStyle.Current : MapRoomIconStyle.KnownRoom, facing, false, null);
        }

        public void Bind(DungeonRoom room, MapRoomIconStyle style, FacingDirection facing, bool isHighlighted, ISet<FacingDirection> routes = null)
        {
            highlighted = isHighlighted;
            pulse = style is MapRoomIconStyle.UnknownReachable or MapRoomIconStyle.ClearedExit;
            var current = style == MapRoomIconStyle.Current;
            baseColor = style switch
            {
                MapRoomIconStyle.Current => new Color(.08f, .42f, .62f, 1f),
                MapRoomIconStyle.UnknownReachable => new Color(.16f, .32f, .46f, 1f),
                MapRoomIconStyle.ClearedVisited => new Color(.14f, .29f, .42f, 1f),
                MapRoomIconStyle.ClearedExit => new Color(.14f, .29f, .42f, 1f),
                _ => new Color(.16f, .31f, .45f, 1f)
            };
            if (image != null)
            {
                image.enabled = true;
                ApplyColor();
            }

            if (label != null)
            {
                label.enabled = style is not MapRoomIconStyle.Current and not MapRoomIconStyle.ClearedVisited and not MapRoomIconStyle.ClearedExit;
                label.text = style switch
                {
                    MapRoomIconStyle.UnknownReachable => "?",
                    MapRoomIconStyle.KnownRoom => RoomLabel(room.Type),
                    _ => string.Empty
                };
            }

            if (facingArrow != null)
            {
                facingArrow.enabled = current;
                if (current)
                {
                    facingArrow.text = "\u25B2";
                    var arrowDistance = image == null ? 12f : Mathf.Max(9f, image.rectTransform.rect.width * .3f);
                    facingArrow.rectTransform.anchoredPosition = ArrowOffset(facing, arrowDistance);
                    facingArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, ArrowRotation(facing));
                }
            }

            var routeColor = highlighted ? new Color(.08f, .13f, .2f, 1f) : new Color(.42f, .9f, 1f, 1f);
            SetRoute(northRoute, routes?.Contains(FacingDirection.North) == true, routeColor);
            SetRoute(eastRoute, routes?.Contains(FacingDirection.East) == true, routeColor);
            SetRoute(southRoute, routes?.Contains(FacingDirection.South) == true, routeColor);
            SetRoute(westRoute, routes?.Contains(FacingDirection.West) == true, routeColor);
        }

        private void Update()
        {
            if (pulse && !highlighted) ApplyColor();
        }

        private void ApplyColor()
        {
            if (image == null) return;
            if (highlighted)
            {
                image.color = new Color(1f, .76f, .18f, 1f);
                return;
            }
            if (!pulse)
            {
                image.color = baseColor;
                return;
            }

            var phase = (Mathf.Sin(Time.unscaledTime * 6f) + 1f) * .5f;
            image.color = Color.Lerp(baseColor, new Color(.22f, .48f, .64f, 1f), phase);
        }

        private static void SetRoute(Image route, bool visible, Color color)
        {
            if (route == null) return;
            route.enabled = visible;
            route.color = color;
        }

        private static Vector2 ArrowOffset(FacingDirection facing, float distance) => facing switch
        {
            FacingDirection.North => new Vector2(0f, distance),
            FacingDirection.East => new Vector2(distance, 0f),
            FacingDirection.South => new Vector2(0f, -distance),
            _ => new Vector2(-distance, 0f)
        };

        private static float ArrowRotation(FacingDirection facing) => facing switch
        {
            FacingDirection.North => 0f,
            FacingDirection.East => -90f,
            FacingDirection.South => 180f,
            _ => 90f
        };

        private static string RoomLabel(RoomEncounterType type) => type switch
        {
            RoomEncounterType.Start => "\uC2DC\uC791",
            RoomEncounterType.Empty => string.Empty,
            RoomEncounterType.Combat => "\uC804\uD22C",
            RoomEncounterType.Elite => "\uC815\uC608",
            RoomEncounterType.Boss => "\uBCF4\uC2A4",
            RoomEncounterType.Reward => "\uBCF4\uC0C1",
            RoomEncounterType.MajorReward => "\uC0C1\uC790",
            RoomEncounterType.Fountain => "\uBD84\uC218",
            RoomEncounterType.PoisonFountain => "\uBD84\uC218",
            RoomEncounterType.Chest => "\uC0C1\uC790",
            RoomEncounterType.Goddess => "\uC5EC\uC2E0",
            RoomEncounterType.Shop => "\uC0C1\uC810",
            _ => "?"
        };
    }
}
