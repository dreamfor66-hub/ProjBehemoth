using TMPro;
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
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private TextMeshProUGUI facingArrow;
        [SerializeField] private Image northRoute;
        [SerializeField] private Image eastRoute;
        [SerializeField] private Image southRoute;
        [SerializeField] private Image westRoute;
        private Color baseColor;
        private bool pulse;
        private bool highlighted;

        public void Configure(Image newImage, TextMeshProUGUI newLabel) => Configure(newImage, newLabel, null, null, null, null, null);

        public void Configure(Image newImage, TextMeshProUGUI newLabel, TextMeshProUGUI newFacingArrow) => Configure(newImage, newLabel, newFacingArrow, null, null, null, null);

        public void Configure(Image newImage, TextMeshProUGUI newLabel, TextMeshProUGUI newFacingArrow, Image newNorthRoute, Image newEastRoute, Image newSouthRoute, Image newWestRoute)
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
            RoomEncounterType.Start => GameText.Get("ui.maproomicon.5cc146fe"),
            RoomEncounterType.Empty => string.Empty,
            RoomEncounterType.Combat => GameText.Get("ui.maproomicon.f1b4d56d"),
            RoomEncounterType.Elite => GameText.Get("ui.maproomicon.a492ce8"),
            RoomEncounterType.Boss => GameText.Get("ui.maproomicon.49257a85"),
            RoomEncounterType.Reward => GameText.Get("ui.maproomicon.a422e3c6"),
            RoomEncounterType.MajorReward => GameText.Get("ui.maproomicon.5b5ef854"),
            RoomEncounterType.Fountain => GameText.Get("ui.maproomicon.3938c211"),
            RoomEncounterType.PoisonFountain => GameText.Get("ui.maproomicon.3938c211"),
            RoomEncounterType.Chest => GameText.Get("ui.maproomicon.5b5ef854"),
            RoomEncounterType.Goddess => GameText.Get("ui.maproomicon.1bca6671"),
            RoomEncounterType.Shop => GameText.Get("ui.maproomicon.db564fd4"),
            RoomEncounterType.Event => "?",
            _ => "?"
        };
    }
}
