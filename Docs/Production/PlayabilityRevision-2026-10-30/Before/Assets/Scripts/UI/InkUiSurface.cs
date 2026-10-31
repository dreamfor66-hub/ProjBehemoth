using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace DungeonSlash
{
    // A serialized, non-intercepting skin over the existing interactive Graphic.
    // The existing Button/Image and all gameplay bindings remain the interaction authority.
    [ExecuteAlways, RequireComponent(typeof(CanvasRenderer))]
    public sealed class InkUiSurface : MaskableGraphic
    {
        public InkUiStyle style;
        public Graphic source;
        public Selectable control;
        public bool trackAuthoredSelection;
        public Color authoredSelectedTint = new Color32(75,74,96,255);
        public InkSurfaceRole role;
        public Color resourceColor = Color.white;
        public InkResource resource;
        public bool titleBand;
        public TMP_Text titleLabel;
        public float titleBandHeight;
        Color previous;
        Color previousSource;
        int styleRevision=-1;
        bool previousEnabled, previousSelected, previousInteractable, previousTitleVisible;
        bool TitleVisible => titleLabel == null || titleLabel.isActiveAndEnabled && !string.IsNullOrWhiteSpace(titleLabel.text);
        bool hovered, pressed;
        float previousFill = -1;
        public bool IsHovered => hovered;
        public bool IsPressed => pressed;
        public override Texture mainTexture => Texture2D.whiteTexture;

        protected override void OnEnable() { base.OnEnable(); raycastTarget = false; }
        void LateUpdate()
        {
            if (source == null || style == null) return;
            var selected = control != null && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == control.gameObject;
            var enabledNow = source.isActiveAndEnabled;
            var interactable = control == null || control.IsInteractable();
            var tint = source.canvasRenderer.GetColor();
            var amount = source is Image image && image.type == Image.Type.Filled ? image.fillAmount : 1;
            if (previousTitleVisible != TitleVisible || amount != previousFill || tint != previous || source.color != previousSource || styleRevision != style.RenderRevision || previousEnabled != enabledNow || selected != previousSelected || interactable != previousInteractable)
            {
                previousTitleVisible = TitleVisible;
                previousFill = amount;
                previous = tint; previousSource = source.color; styleRevision = style.RenderRevision; previousEnabled = enabledNow; previousSelected = selected; previousInteractable = interactable;
                SetVerticesDirty();
            }
        }
        public void Feedback(bool over, bool down) { hovered = over; pressed = down; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (style == null || source == null || !source.isActiveAndEnabled) return;
            var selected = control != null && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == control.gameObject;
            var active = control == null || control.IsInteractable();
            var authoredFocus = control != null && source.color.r > .2f && source.color.r > source.color.b * 1.6f && source.color.g > source.color.b * 1.2f;
            var tintSelected = trackAuthoredSelection && Mathf.Abs(source.color.r-authoredSelectedTint.r)<.002f && Mathf.Abs(source.color.g-authoredSelectedTint.g)<.002f && Mathf.Abs(source.color.b-authoredSelectedTint.b)<.002f;
            var focus = active && (hovered || selected || authoredFocus || tintSelected);
            var alpha = source.color.a;
            var fill = resource switch { InkResource.Health=>style.hp, InkResource.Mana=>style.mana, InkResource.Guard=>style.guard, InkResource.Gold=>style.brass, _=>resourceColor };
            var rect = rectTransform.rect;
            if (role == InkSurfaceRole.GaugeFill && source is Image image && image.type == Image.Type.Filled)
            {
                if (image.fillMethod == Image.FillMethod.Vertical)
                {
                    var height = rect.height * image.fillAmount;
                    if (image.fillOrigin == 0) rect.height = height; else rect.yMin = rect.yMax - height;
                }
                else if (image.fillMethod == Image.FillMethod.Horizontal)
                {
                    var width = rect.width * image.fillAmount;
                    if (image.fillOrigin == 0) rect.width = width; else rect.xMin = rect.xMax - width;
                }
            }
            if(role is InkSurfaceRole.Brush or InkSurfaceRole.Countdown)
            {
                if(role==InkSurfaceRole.Countdown)CombatInkDrawing.CountdownShadow(vh,rect,alpha);
                else CombatInkDrawing.Brush(vh,rect,new Color(0,0,0,alpha));
                return;
            }
            InkUiDrawing.Surface(vh, rect, style, role, fill, titleBand && TitleVisible ? (titleBandHeight > 0 ? titleBandHeight : style.headerHeight) : 0, focus, pressed&&hovered&&active, active, alpha);
        }
    }
}
