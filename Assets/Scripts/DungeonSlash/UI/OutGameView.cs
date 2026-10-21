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
        public Button startButton;
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
            Select(Mathf.Max(0, content.characters.IndexOf(RunSession.SelectedCharacter)));
        }
        public void Select(int index)
        {
            selected = Mathf.Clamp(index, 0, content.characters.Count - 1);
            var data = content.characters[selected];
            characterName.text = data.displayName; characterName.color = data.characterColor;
            characterDescription.text = data.description;
            stats.text = GameText.Format("ui.outgameview.2e282e20", data.maxHp, data.baseAttackDamage, data.attackCooldown, data.baseChargeDamage, data.chargeDuration, data.shieldMax, data.normalShieldRegen);
            startingRelic.text = GameText.Format("ui.outgameview.c86889d0", data.startingRelic.displayName, data.startingRelic.description);
            startingRelic.enableAutoSizing = true; startingRelic.fontSizeMin = 18; startingRelic.fontSizeMax = 23;
            portrait.sprite = data.portrait; portrait.color = Color.white; portrait.enabled = data.portrait != null;
            for (var i = 0; i < selectionMarks.Length; i++) selectionMarks[i].color = i == selected ? data.characterColor : new Color(.17f, .22f, .3f);
        }
    }
}
