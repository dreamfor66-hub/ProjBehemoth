using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class ManaHudView : MonoBehaviour
    {
        public RunController run;
        public Image fill;
        public TextMeshProUGUI label;
        public GameObject gauge;
        public Button spellbook;
        public CombatController combat;
        private RunExpansionView expansion;
        private void Awake()=>expansion=GetComponentInParent<Canvas>().GetComponentInChildren<RunExpansionView>(true);
        private void LateUpdate()
        {
            var state = run != null ? run.State : null;
            var visible = state != null;
            if (gauge != null) gauge.SetActive(visible && combat != null && combat.IsActive && (expansion==null || !expansion.IsDecisionOpen));
            if (spellbook != null) spellbook.gameObject.SetActive(visible && state.Spellbook.Count>0 && (combat == null || !combat.IsActive) && (expansion==null || !expansion.IsDecisionOpen));
            if (!visible) return;
            var player = state.Player;
            label.text = GameText.Format("ui.mana.gauge", player.CurrentMp, player.MaxMp);
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = new Vector2(player.MaxMp > 0 ? player.CurrentMp / player.MaxMp : 0, 1);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
        }
    }
}
