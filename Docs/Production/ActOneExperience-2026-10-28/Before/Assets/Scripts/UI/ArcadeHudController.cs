using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;

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
        private bool drawerWasOpen;
        public bool CombatTutorialShown {get;private set;}
        private void Awake()
        {
            mapButton.onClick.AddListener(()=>Toggle(mapPanel));inventoryButton.onClick.AddListener(()=>Toggle(inventoryPanel));
            closeMap.onClick.AddListener(Close);closeInventory.onClick.AddListener(Close);Close();
        }
        public void StartHint()
        {
            CombatTutorialShown=false;
            if(hint!=null)hint.gameObject.SetActive(false);
            if(expansion==null)return;
            expansion.ShowMessage(GameText.Get("ui.tutorial.exploration.title"),GameText.Get("ui.tutorial.exploration.body"),null);
            FocusConfirmation();
        }
        public IEnumerator ShowCombatTutorial()
        {
            if(CombatTutorialShown||expansion==null)yield break;
            CombatTutorialShown=true;
            var acknowledged=false;
            var body=GameText.Get("ui.tutorial.combat.body");
            if(run?.State!=null&&run.State.Spellbook.Count>0)body+="\n\n"+GameText.Get("ui.tutorial.combat.magic");
            expansion.ShowMessage(GameText.Get("ui.tutorial.combat.title"),body,()=>acknowledged=true);
            FocusConfirmation();
            // GameFlow has prepared the encounter but has not started its clocks or accepted attacks.
            while(!acknowledged)yield return null;
            // The confirming click/key must not become the first combat gesture.
            while(Input.GetMouseButton(0)||Input.touchCount>0||Input.GetKey(KeyCode.Space)||Input.GetKey(KeyCode.Return)||Input.GetKey(KeyCode.KeypadEnter))yield return null;
        }
        private void FocusConfirmation()
        {
            var rows=expansion.ActiveChoices;
            if(EventSystem.current!=null&&rows!=null&&rows.Length>0)EventSystem.current.SetSelectedGameObject(rows[0].button.gameObject);
        }
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
            if(Input.GetKeyDown(KeyCode.Tab))Toggle(mapPanel);
            if(Input.GetKeyDown(KeyCode.I))Toggle(inventoryPanel);
            if(Input.GetKeyDown(KeyCode.Escape))Close();
            if(hint!=null&&hint.gameObject.activeSelf)hint.gameObject.SetActive(false);
            mapButton.interactable=!combat.IsActive&&!expansion.IsDecisionOpen;
            inventoryButton.interactable=!expansion.IsDecisionOpen;
        }
    }
}
