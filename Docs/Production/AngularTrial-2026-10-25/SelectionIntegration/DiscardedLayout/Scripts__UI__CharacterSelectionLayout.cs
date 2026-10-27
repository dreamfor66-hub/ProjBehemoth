using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    // Presentation-only artwork and crops. Combat data and portraits are independent.
    public sealed class CharacterSelectionLayout : MonoBehaviour
    {
        [Serializable] public sealed class Artwork
        {
            public CharacterKind character;
            public Texture2D texture;
            public Rect figureUv, faceUv;
        }
        public Artwork[] artwork;
        public RawImage figure;
        public AspectRatioFitter figureAspect;
        public RawImage[] faces;
        public Image[] borders, indicators;
        public TextMeshProUGUI[] names;
        public TextMeshProUGUI pageNumber;
        public ArcadeRasterCamera raster;
        public ArcadePresentation normalPresentation, nativePresentation;
        public Color background, card, selectedCard, border, accent, text, muted;

        public void ShowPage(bool visible)
        {
            if (raster != null) raster.presentation = visible ? nativePresentation : normalPresentation;
        }

        public void Refresh(OutGameView view, int selected)
        {
            for (var i = 0; i < view.content.characters.Count; i++)
            {
                var data = view.content.characters[i];
                var art = Array.Find(artwork, entry => entry.character == data.character);
                var active = i == selected;
                view.characterButtons[i].image.color = active ? selectedCard : card;
                borders[i].color = active ? accent : border;
                indicators[i].gameObject.SetActive(active);
                names[i].gameObject.SetActive(true);
                names[i].text = data.displayName;
                names[i].color = active ? text : muted;
                faces[i].texture = art?.texture;
                faces[i].uvRect = art != null ? art.faceUv : new Rect(0, 0, 1, 1);
                if (!active || art == null) continue;
                figure.texture = art.texture;
                figure.uvRect = art.figureUv;
                figureAspect.aspectRatio = art.texture.width * art.figureUv.width /
                    (art.texture.height * art.figureUv.height);
            }
            if (pageNumber != null) pageNumber.text = GameText.Format("ui.outgame.page", selected + 1, view.content.characters.Count);
        }
    }
}
