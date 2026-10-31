using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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

    public sealed class MapRoomIcon : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image image;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private TextMeshProUGUI facingArrow;
        [SerializeField] private Image interior;
        [SerializeField] private Image hitTarget;
        [SerializeField] private Image northRoute;
        [SerializeField] private Image eastRoute;
        [SerializeField] private Image southRoute;
        [SerializeField] private Image westRoute;
        private Color baseColor;
        private bool pulse;
        private bool highlighted;
        private bool current;
        private bool cleared;
        public DungeonRoom Room { get; private set; }
        public bool compact;
        private System.Action selected;
        public void SetSelection(System.Action action)
        {
            selected = action;
            if (image != null) image.raycastTarget = hitTarget == null && action != null;
            if (hitTarget != null)
            {
                hitTarget.raycastTarget = action != null;
                hitTarget.rectTransform.sizeDelta = Vector2.one * Mathf.Max(44, ((RectTransform)transform).rect.width);
            }
        }
        public void OnPointerClick(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left && !data.dragging) selected?.Invoke();
        }
        public void OnPointerEnter(PointerEventData data) { if (selected != null) { highlighted = true; ApplyColor(); } }
        public void OnPointerExit(PointerEventData data) { if (selected != null) { highlighted = false; ApplyColor(); } }

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
            Room = room;
            highlighted = isHighlighted;
            pulse = style is MapRoomIconStyle.UnknownReachable or MapRoomIconStyle.ClearedExit;
            current = style == MapRoomIconStyle.Current;
            cleared = style is MapRoomIconStyle.ClearedVisited or MapRoomIconStyle.ClearedExit;
            var large = ((RectTransform)transform).rect.width >= 40;
            baseColor = style switch
            {
                MapRoomIconStyle.Current => new Color(.86f, .70f, .40f, 1f),
                MapRoomIconStyle.UnknownReachable => new Color(.62f, .59f, .50f, 1f),
                MapRoomIconStyle.ClearedVisited => new Color(.30f, .30f, .28f, 1f),
                MapRoomIconStyle.ClearedExit => new Color(.48f, .45f, .37f, 1f),
                _ => room.Type == RoomEncounterType.Boss ? new Color(.70f, .31f, .23f)
                    : room.Type == RoomEncounterType.Elite ? new Color(.70f, .53f, .27f)
                    : new Color(.56f, .54f, .46f)
            };
            if (image != null)
            {
                image.enabled = true;
                ApplyColor();
            }

            if (label != null)
            {
                label.enabled = !current && (!cleared || large);
                label.raycastTarget = false;
                label.enableAutoSizing = false;
                label.fontSize = large ? 20 : 17;
                label.color = cleared ? new Color(.49f, .48f, .43f) : new Color(.92f, .87f, .73f);
                label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.anchoredPosition = Vector2.zero; label.rectTransform.sizeDelta = new Vector2(-4, -4);
                label.text = style switch
                {
                    MapRoomIconStyle.UnknownReachable => "?",
                    _ when large || !compact => RoomLabel(room.Type),
                    MapRoomIconStyle.KnownRoom => room.Type switch
                    {
                        RoomEncounterType.Combat => GameText.Get("ui.map.marker.combat"),
                        RoomEncounterType.Fountain or RoomEncounterType.PoisonFountain => "~",
                        RoomEncounterType.Reward => "+",
                        RoomEncounterType.Empty => string.Empty,
                        _ => "?"
                    },
                    _ => string.Empty
                };
            }

            if (facingArrow != null)
            {
                facingArrow.enabled = current;
                if (current)
                {
                    facingArrow.text = "\u25B2";
                    facingArrow.raycastTarget = false;
                    facingArrow.fontSize = large ? 27 : 20;
                    facingArrow.color = new Color(1f, .88f, .55f);
                    facingArrow.rectTransform.sizeDelta = Vector2.one * (large ? 38 : 26);
                    facingArrow.rectTransform.anchoredPosition = Vector2.zero;
                    facingArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, ArrowRotation(facing));
                }
            }

            var routeColor = highlighted ? new Color(.17f, .14f, .10f, 1f) : new Color(.73f, .61f, .40f, 1f);
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
            if (interior != null)
                interior.color = highlighted ? new Color(.30f, .25f, .14f) : current ? new Color(.28f, .23f, .13f)
                    : cleared ? new Color(.065f, .065f, .06f) : new Color(.11f, .115f, .11f);
            if (highlighted)
            {
                image.color = new Color(1f, .82f, .40f, 1f);
                return;
            }
            if (!pulse)
            {
                image.color = baseColor;
                return;
            }

            var phase = (Mathf.Sin(Time.unscaledTime * 6f) + 1f) * .5f;
            image.color = Color.Lerp(baseColor, new Color(.78f, .67f, .43f, 1f), phase * .55f);
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
            RoomEncounterType.Camp => GameText.Get("ui.map.marker.camp"),
            _ => "?"
        };
    }
}
