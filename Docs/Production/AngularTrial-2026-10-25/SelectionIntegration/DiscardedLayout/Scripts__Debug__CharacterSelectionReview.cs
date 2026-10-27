#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Scoped selection UI verification only; never starts combat or changes balance data.
public sealed class CharacterSelectionReview : MonoBehaviour
{
    const string Report = "Docs/Production/AngularTrial-2026-10-25/SelectionIntegration/";
    public void Run() => StartCoroutine(Matrix());
    void Check(bool pass, string name)
    {
        File.AppendAllText(Report + "Live.txt", (pass ? "PASS " : "FAIL ") + name + "\n");
        if (!pass) Debug.LogError("Selection UI: " + name);
    }
    IEnumerator Capture(string name)
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        var image = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Report + name + ".png", image.EncodeToPNG());
        Destroy(image);
    }
    IEnumerator Matrix()
    {
        Directory.CreateDirectory(Report);
        File.WriteAllText(Report + "Live.txt", "Production OutGameScene / scoped character selection matrix\n");
        var menu = FindFirstObjectByType<OutGameMenuView>();
        var view = menu.selection; var layout = menu.characterLayout;
        menu.ShowCharacters();
        yield return null;
        Check(!layout.raster.presentation.arcade, "Selection renders natively without low-resolution palette pass");
        Check(view.characterButtons.Length == 6 && view.characterButtons.All(b => b.gameObject.activeInHierarchy), "All six choices visible");
        for (var i = 0; i < 6; i++)
        {
            view.characterButtons[i].onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();
            var data = view.content.characters[i];
            var art = layout.artwork.Single(a => a.character == data.character);
            Check(view.SelectedIndex == i && layout.figure.texture == art.texture, data.character + " click selects approved artwork");
            Check(view.characterName.text == data.displayName && layout.names[i].text == data.displayName, data.character + " name and card synchronized");
            Check(layout.indicators.Count(mark => mark.gameObject.activeSelf) == 1 && layout.indicators[i].gameObject.activeSelf, data.character + " one selected card");
            Check(art.texture.width == 1024 && art.texture.height == 1536, data.character + " original canvas retained");
            foreach (var label in new[] { view.characterName, view.compactDescription, view.selectionStory, view.selectionStats, view.selectionRelic })
            {
                label.ForceMeshUpdate();
                Check(!label.isTextOverflowing, data.character + " text fits: " + label.name);
            }
            yield return Capture("Selection-" + data.character);
            view.detailsButton.onClick.Invoke(); yield return null;
            Check(view.DetailsVisible && !view.selectionStory.gameObject.activeSelf, data.character + " details opens and hides introduction");
            view.abilityStats.ForceMeshUpdate();
            Check(!view.abilityStats.isTextOverflowing, data.character + " ability values fit");
            if (i == 0) yield return Capture("Selection-Details");
            view.detailsButton.onClick.Invoke(); yield return null;
            Check(!view.DetailsVisible && view.selectionStory.gameObject.activeSelf, data.character + " introduction restores");
        }
        view.Select(0);
        view.startingRelicHover.OnPointerEnter(new PointerEventData(EventSystem.current));
        yield return null;
        Check(view.startingRelicHover.view.IsVisible, "Starting equipment tooltip opens");
        yield return Capture("Selection-Equipment");
        view.startingRelicHover.OnPointerExit(new PointerEventData(EventSystem.current));
        Check(!view.startingRelicHover.view.IsVisible, "Starting equipment tooltip closes");
        menu.back.onClick.Invoke(); yield return null;
        Check(menu.Page == OutGamePage.Title && layout.raster.presentation == layout.normalPresentation, "Back returns to existing title rendering");
        menu.newGame.onClick.Invoke(); yield return null;
        Check(menu.Page == OutGamePage.Characters && layout.raster.presentation == layout.nativePresentation, "Title reopens native selection");
        Check(view.startButton.interactable, "Departure action enabled");
        yield return Capture("Selection-Final");
        File.AppendAllText(Report + "Live.txt", "MATRIX COMPLETE\n");
    }
}
#endif
