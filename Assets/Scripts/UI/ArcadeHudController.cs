using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class ArcadeHudController : MonoBehaviour
    {
        public GameObject mapPanel, inventoryPanel;
        public Button mapButton, inventoryButton, closeMap, closeInventory;
        public TextMeshProUGUI hint;
        public Vector2 explorationHintPosition=new(-88,-408),combatHintPosition=new(0,-64);
        public CombatController combat;
        public RunExpansionView expansion;
        public RunController run;
        private float hintUntil;
        private bool drawerWasOpen;
        private bool sawCombat;
        private bool showedCombatHint;
        private int hintAction;
        private void Awake()
        {
            mapButton.onClick.AddListener(()=>Toggle(mapPanel));inventoryButton.onClick.AddListener(()=>Toggle(inventoryPanel));
            closeMap.onClick.AddListener(Close);closeInventory.onClick.AddListener(Close);Close();
        }
        public void StartHint(){sawCombat=showedCombatHint=false;hintUntil=Time.unscaledTime+5;hint.text=GameText.Get("ui.arcade.explore.hint");}
        private void Toggle(GameObject panel)
        {
            if(expansion.IsDecisionOpen)return;
            var show=!panel.activeSelf;Close();panel.SetActive(show);panel.transform.SetAsLastSibling();
            combat.InputPaused=show;drawerWasOpen=show;
        }
        public void Close(){mapPanel.SetActive(false);inventoryPanel.SetActive(false);if(combat!=null)combat.InputPaused=expansion!=null&&expansion.IsDecisionOpen;drawerWasOpen=false;}
        private void Update()
        {
            if(expansion.IsDecisionOpen&&drawerWasOpen)Close();
            if(hint!=null)hint.rectTransform.anchoredPosition=combat.IsActive?combatHintPosition:explorationHintPosition;
            if(Input.GetKeyDown(KeyCode.Tab))Toggle(mapPanel);
            if(Input.GetKeyDown(KeyCode.I))Toggle(inventoryPanel);
            if(Input.GetKeyDown(KeyCode.Escape))Close();
            if(combat.IsActive&&!sawCombat){sawCombat=true;if(!showedCombatHint){showedCombatHint=true;hintAction=run.State.ActionSerial;hint.text=GameText.Get("ui.arcade.combat.hint");hintUntil=Time.unscaledTime+6;}}
            if(combat.IsActive&&(run.State.ActionSerial!=hintAction||combat.Player.IsGuarding))hintUntil=0;
            if(!combat.IsActive)sawCombat=false;
            hint.gameObject.SetActive(Time.unscaledTime<hintUntil&&!expansion.IsDecisionOpen&&!drawerWasOpen&&!(expansion.encounterHint!=null&&expansion.encounterHint.gameObject.activeSelf));
            mapButton.interactable=!combat.IsActive&&!expansion.IsDecisionOpen;
            inventoryButton.interactable=!expansion.IsDecisionOpen;
        }
    }
}
