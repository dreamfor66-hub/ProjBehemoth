using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace DungeonSlash
{
    public sealed class OutGameView : MonoBehaviour
    {
        public GameContent content;
        public Button[] characterButtons;
        public TextMeshProUGUI[] characterLabels;
        public Image[] selectionMarks;
        public TextMeshProUGUI characterName;
        public TextMeshProUGUI characterDescription;
        public TextMeshProUGUI stats;
        public TextMeshProUGUI startingRelic;
        public Image portrait;
        public Image selectionSplash;
        public ArcadePresentation presentation;
        public Button startButton;
        public ScrollRect startingRelicScroll;
        public bool compact;
        public GameObject detailsPanel;
        public TextMeshProUGUI compactDescription;
        public TextMeshProUGUI selectionStats,selectionRelic;
        public Image selectionRelicIcon;
        public TextMeshProUGUI selectionStory;
        public HoverTooltipTrigger startingRelicHover;
        public Button detailsButton;
        public TextMeshProUGUI detailsButtonLabel;
        public TextMeshProUGUI abilityStats;
        public ClassSymbolGraphic selectedClassSymbol;
        public HoverTooltipTrigger extraStatsHover;
        public bool DetailsVisible => detailsPanel != null && detailsPanel.activeSelf;
        public UnityEngine.Events.UnityEvent<int> selectionChanged=new();
        public int SelectedIndex=>selected;
        [Header("Illustrated character selection")]
        public bool illustratedSelection;
        public Image[] characterArtwork;
        public Image[] characterFrames;
        public GameObject illustratedIntroduction;
        private int selected;
        private void Awake()
        {
            for (var i = 0; i < characterButtons.Length; i++)
            {
                var index = i;
                characterButtons[i].onClick.AddListener(() => Select(index));
                characterLabels[i].text = content.characters[i].displayName;
            }
            startButton.onClick.AddListener(() => { startButton.interactable = false; RunSession.Start(content.characters[selected]); });
            if(detailsButton!=null)detailsButton.onClick.AddListener(ToggleDetails);
            Select(Mathf.Max(0, content.characters.IndexOf(RunSession.SelectedCharacter)));
        }
        public void ToggleDetails() => SetDetailsVisible(!DetailsVisible);
        public void SetDetailsVisible(bool visible)
        {
            if(detailsPanel!=null)detailsPanel.SetActive(visible);
            if(illustratedIntroduction!=null)illustratedIntroduction.SetActive(!visible);
            if(abilityStats==null)return; // Legacy layouts keep their existing popup content.
            if(selectionStory!=null)selectionStory.gameObject.SetActive(!visible);
            if(selectionStats!=null)selectionStats.gameObject.SetActive(!visible);
            if(detailsButtonLabel!=null)detailsButtonLabel.text=GameText.Get(visible?"ui.selection.show.introduction":"ui.outgame.details");
            startingRelicHover?.view?.Hide();
        }
        public void Select(int index)
        {
            selected = Mathf.Clamp(index, 0, content.characters.Count - 1);
            var data = content.characters[selected];
            if(compact&&compactDescription!=null)compactDescription.text=string.IsNullOrEmpty(data.selectionAbility)?GameText.Get("ui.arcade.class."+selected):data.selectionAbility;
            if(selectionStory!=null)selectionStory.text=data.description;
            if(startingRelicHover!=null)startingRelicHover.Bind(data.startingRelic);
            if(selectionStats!=null)selectionStats.text=GameText.Format("ui.feedback.selection.stats",data.maxHp,data.baseAttackDamage);
            if(selectionRelic!=null)selectionRelic.text=GameText.Format("ui.feedback.selection.relic",data.startingRelic.displayName);
            if(selectionRelicIcon!=null){selectionRelicIcon.sprite=data.startingRelic.icon;selectionRelicIcon.color=Color.white;}
            characterName.text = data.displayName; characterName.color = presentation!=null&&presentation.darkPalette!=null?presentation.darkPalette.bone:data.characterColor;
            if(selectedClassSymbol!=null&&!illustratedSelection)
            {
                selectedClassSymbol.character=data.character;selectedClassSymbol.SetAllDirty();
                var nameWidth=Mathf.Min(96,characterName.GetPreferredValues(data.displayName).x/4);
                selectedClassSymbol.rectTransform.anchoredPosition=new Vector2((-176+nameWidth+10)*4,82*4);
                if(detailsButton!=null)((RectTransform)detailsButton.transform).anchoredPosition=new Vector2((nameWidth-108)*4,80*4);
            }
            if(abilityStats!=null)abilityStats.text=GameText.Format("ui.selection.ability.stats",data.maxHp,data.maxMp,data.shieldMax,data.baseAttackDamage,data.baseChargeDamage,data.attackCooldown);
            if(extraStatsHover!=null)extraStatsHover.Bind(GameText.Get("ui.selection.extra.title"),GameText.Format("ui.selection.ability.extra",data.chargeDuration,data.normalShieldRegen,data.guardDamageReduction));
            characterDescription.text = data.description;
            stats.text = GameText.Format("ui.selection.stats", data.maxHp, data.baseAttackDamage, data.attackCooldown, data.baseChargeDamage, data.chargeDuration, data.shieldMax, data.normalShieldRegen, data.guardDamageReduction);
            startingRelic.text = GameText.Format("ui.outgameview.c86889d0", data.startingRelic.displayName, data.startingRelic.description);
            var relicSize=startingRelic.GetComponent<TypographyRoleBinding>()?.designSize??44;
            startingRelic.enableAutoSizing = !compact&&startingRelicScroll == null; startingRelic.fontSizeMin = compact?relicSize:18; startingRelic.fontSizeMax = compact?relicSize:20;
            if (startingRelicScroll != null) { Canvas.ForceUpdateCanvases(); startingRelicScroll.verticalNormalizedPosition = 1; }
            portrait.sprite = data.portrait; portrait.color = Color.white; portrait.enabled = data.portrait != null;
            if(selectionSplash!=null)
            {
                var useArt=presentation==null||presentation.arcade;
                selectionSplash.sprite=useArt?data.selectionIllustration:null;
                selectionSplash.material=useArt?data.selectionMaterial:null;
                selectionSplash.color=useArt&&data.selectionIllustration!=null?Color.white:data.selectionBackdrop;
                portrait.gameObject.SetActive(!useArt||data.selectionIllustration==null);
                for(var i=0;i<characterLabels.Length;i++)
                {
                    characterLabels[i].text=content.characters[i].displayName;
                    characterLabels[i].gameObject.SetActive(illustratedSelection||!useArt);
                }
            }
            for (var i = 0; i < selectionMarks.Length; i++) selectionMarks[i].color = i == selected ? data.characterColor : new Color(.17f, .22f, .3f);
            selectionChanged.Invoke(selected);
        }
    }
}
