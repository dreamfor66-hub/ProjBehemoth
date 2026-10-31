using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonSlash
{
    public enum OutGamePage { Title, Characters, Options }

    // Page roots, controls and portraits are serialized and editable in OutGameScene.
    public sealed class OutGameMenuView : MonoBehaviour
    {
        public OutGameView selection;
        public GameObject titlePage,characterPage,optionsPage;
        public Button newGame,options,quit,back,optionsBack;
        public TextMeshProUGUI detailHeading,volumeValue,fullscreenValue,scaleValue,shakeValue;
        public Slider volume;
        public Button fullscreen,windowScale,screenShake;
        public Image selectionShade;
        public OutGamePage Page { get; private set; }

        private void Awake()
        {
            GamePreferences.Apply();
            newGame.onClick.AddListener(ShowCharacters);options.onClick.AddListener(ShowOptions);quit.onClick.AddListener(Quit);
            back.onClick.AddListener(ShowTitle);optionsBack.onClick.AddListener(ShowTitle);
            selection.selectionChanged.AddListener(RefreshSelection);
            volume.SetValueWithoutNotify(GamePreferences.Volume);
            volume.onValueChanged.AddListener(value=>{GamePreferences.SetVolume(value);RefreshOptions();});
            fullscreen.onClick.AddListener(()=>{GamePreferences.SetFullscreen(!GamePreferences.Fullscreen);RefreshOptions();});
            windowScale.onClick.AddListener(()=>{GamePreferences.SetWindowScale(GamePreferences.WindowScale>=4?2:GamePreferences.WindowScale+1);RefreshOptions();});
            screenShake.onClick.AddListener(()=>{GamePreferences.SetScreenShake(!GamePreferences.ScreenShake);RefreshOptions();});
            ShowTitle();
        }
        public void ShowTitle(){SetPage(OutGamePage.Title);Focus(newGame);}
        public void ShowCharacters(){SetPage(OutGamePage.Characters);selection.Select(selection.SelectedIndex);Focus(selection.characterButtons[selection.SelectedIndex]);}
        public void ShowOptions(){SetPage(OutGamePage.Options);volume.SetValueWithoutNotify(GamePreferences.Volume);RefreshOptions();Focus(volume);}
        private void SetPage(OutGamePage value)
        {
            Page=value;selection.SetDetailsVisible(false);
            titlePage.SetActive(value==OutGamePage.Title);characterPage.SetActive(value==OutGamePage.Characters);optionsPage.SetActive(value==OutGamePage.Options);
            if(selectionShade!=null)selectionShade.gameObject.SetActive(value==OutGamePage.Options);
            GamePreferences.Save();
        }
        private void RefreshSelection(int index)
        {
            var palette=selection.presentation!=null?selection.presentation.darkPalette:null;
            var ink=palette!=null?palette.ink:(Color)new Color32(11,12,22,255);
            var light=palette!=null?palette.bone:(Color)new Color32(222,211,175,255);
            var accent=palette!=null?palette.accent:(Color)new Color32(216,187,98,255);
            for(var i=0;i<selection.characterButtons.Length;i++)
            {
                var button=selection.characterButtons[i];button.gameObject.SetActive(true);
                if(selection.illustratedSelection)
                {
                    button.image.color=i==index?new Color32(83,29,23,255):new Color32(12,13,12,255);
                    selection.characterLabels[i].color=light;
                    if(selection.characterArtwork!=null&&i<selection.characterArtwork.Length&&selection.characterArtwork[i]!=null)
                        selection.characterArtwork[i].color=i==index?Color.white:new Color(.66f,.66f,.66f,1);
                    if(selection.characterFrames!=null&&i<selection.characterFrames.Length&&selection.characterFrames[i]!=null)
                        selection.characterFrames[i].color=i==index?accent:new Color32(70,65,52,255);
                    selection.selectionMarks[i].gameObject.SetActive(i==index);
                    continue;
                }
                button.image.color=i==index?accent:ink;
                selection.characterLabels[i].color=i==index?ink:light;
                selection.selectionMarks[i].gameObject.SetActive(false);
                var symbol=button.GetComponentInChildren<ClassSymbolGraphic>(true);
                if(symbol!=null){symbol.color=i==index?ink:light;symbol.SetAllDirty();}
            }
            detailHeading.text=selection.content.characters[index].displayName;
        }
        private void RefreshOptions()
        {
            volumeValue.text=GameText.Format("ui.outgame.volume.value",Mathf.RoundToInt(GamePreferences.Volume*100));
            fullscreenValue.text=GameText.Get(GamePreferences.Fullscreen?"ui.outgame.on":"ui.outgame.off");
            scaleValue.text=GameText.Format("ui.outgame.scale.value",GamePreferences.WindowScale);
            shakeValue.text=GameText.Get(GamePreferences.ScreenShake?"ui.outgame.on":"ui.outgame.off");
        }
        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                if(selection.DetailsVisible)selection.SetDetailsVisible(false);
                else if(Page!=OutGamePage.Title)ShowTitle();
            }
            if(Page!=OutGamePage.Characters)return;
            var count=selection.characterButtons.Length;
            if(Input.GetKeyDown(KeyCode.LeftArrow)){selection.Select((selection.SelectedIndex+count-1)%count);Focus(selection.characterButtons[selection.SelectedIndex]);}
            if(Input.GetKeyDown(KeyCode.RightArrow)){selection.Select((selection.SelectedIndex+1)%count);Focus(selection.characterButtons[selection.SelectedIndex]);}
        }
        private static void Focus(Selectable target){if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(target.gameObject);}
        public void Quit()
        {
            GamePreferences.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
    }
}
