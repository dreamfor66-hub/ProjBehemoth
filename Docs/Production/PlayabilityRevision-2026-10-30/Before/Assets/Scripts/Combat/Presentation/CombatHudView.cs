using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class CombatHudView : MonoBehaviour
    {
        [SerializeField] private Image playerHp;
        [SerializeField] private Image shield;
        public SlantedGuardGauge guardGauge;
        public AttackRecoveryGauge attackRecovery;
        public ArcadePresentation presentation;
        [SerializeField] private Image monsterHp;
        [SerializeField] private TextMeshProUGUI playerHpText;
        [SerializeField] private TextMeshProUGUI shieldText;
        [SerializeField] private TextMeshProUGUI playerStats;
        [SerializeField] private TextMeshProUGUI monsterStats;
        [SerializeField] private RectTransform chargeGaugeRoot;
        [SerializeField] private ArcGaugeGraphic chargeGaugeTrack;
        [SerializeField] private ArcGaugeGraphic chargeGaugeFill;
        [SerializeField] private ArcGaugeGraphic chargeGaugeSecondFill;
        [SerializeField] private Vector2 chargePointerOffset = new(0f, 90f);

        public void Configure(Image newPlayerHp, Image newShield, Image newMonsterHp, TextMeshProUGUI newPlayerStats, TextMeshProUGUI newMonsterStats, RectTransform newChargeGaugeRoot, ArcGaugeGraphic newChargeGaugeTrack, ArcGaugeGraphic newChargeGaugeFill, TextMeshProUGUI newPlayerHpText = null, TextMeshProUGUI newShieldText = null, ArcGaugeGraphic newChargeGaugeSecondFill = null)
        {
            playerHp = newPlayerHp;
            shield = newShield;
            monsterHp = newMonsterHp;
            playerHpText = newPlayerHpText;
            shieldText = newShieldText;
            playerStats = newPlayerStats;
            monsterStats = newMonsterStats;
            chargeGaugeRoot = newChargeGaugeRoot;
            chargeGaugeTrack = newChargeGaugeTrack;
            chargeGaugeFill = newChargeGaugeFill;
            chargeGaugeSecondFill = newChargeGaugeSecondFill;
            chargePointerOffset = new Vector2(0f, 90f);
        }

        public void Bind(PlayerCombatRuntime player, MonsterRuntime monster, PointerGestureState gesture, float chargeProgress, Vector2 chargeOrigin, float secondChargeProgress = 0f)
        {
            var hasPlayer = player != null;
            if(attackRecovery!=null){attackRecovery.gameObject.SetActive(hasPlayer);if(hasPlayer)attackRecovery.Bind(player);}
            var slanted=guardGauge!=null&&(presentation==null||presentation.arcade);
            if(guardGauge!=null)guardGauge.gameObject.SetActive(hasPlayer&&slanted);
            if(shield!=null){shield.enabled=!slanted;var track=shield.transform.parent.GetComponent<Image>();if(track!=null)track.enabled=!slanted;}
            SetBarVisible(playerHp, hasPlayer);
            SetBarVisible(shield, hasPlayer);
            if (hasPlayer)
            {
                SetGauge(playerHp, player.CurrentHp / player.MaxHp);
                SetGauge(shield, player.ShieldMax <= 0f ? 0f : player.ShieldCurrent / player.ShieldMax);
                SetValueText(playerHpText, GameText.Format("ui.combathudview.dfe9f229", player.CurrentHp, player.MaxHp));
                SetValueText(shieldText, GameText.Format("ui.combathudview.82e84bd4", player.ShieldCurrent, player.ShieldMax));
                SetShieldAvailability(player);
                if(slanted)
                {
                    guardGauge.gameObject.SetActive(true);guardGauge.SetValue(player.ShieldMax>0?player.ShieldCurrent/player.ShieldMax:0,player.IsGuarding,player.ShieldState==ShieldState.Broken);
                    if(shieldText!=null){shieldText.gameObject.SetActive(true);shieldText.enabled=true;shieldText.color=player.ShieldState==ShieldState.Broken?new Color32(216,101,77,255):new Color32(221,208,169,255);}
                }
            }
            else
            {
                SetValueText(playerHpText, string.Empty);
                SetValueText(shieldText, string.Empty);
            }
            if (playerStats != null) playerStats.enabled = false;

            // Every monster owns a local HP gauge now; the legacy global bar stays hidden for multi-monster encounters.
            SetBarVisible(monsterHp, false);
            if (monsterStats != null)
            {
                monsterStats.text = string.Empty;
                monsterStats.enabled = false;
            }

            var showCharge = gesture is PointerGestureState.Charging or PointerGestureState.Charged or PointerGestureState.SecondCharging or PointerGestureState.SecondCharged;
            if (chargeGaugeRoot != null) chargeGaugeRoot.gameObject.SetActive(showCharge);
            if (showCharge)
            {
                if (chargeGaugeRoot != null) chargeGaugeRoot.anchoredPosition = chargeOrigin + chargePointerOffset;
                if (chargeGaugeFill != null){chargeGaugeFill.color=chargeProgress>=1?new Color32(124,167,163,255):new Color32(230,155,80,255);chargeGaugeFill.SetFillAmount(chargeProgress);}
                if (chargeGaugeSecondFill != null)chargeGaugeSecondFill.color=secondChargeProgress>=1?new Color32(243,230,191,255):new Color32(188,96,70,255);
                if (chargeGaugeSecondFill != null) chargeGaugeSecondFill.SetFillAmount(secondChargeProgress);
            }
            else if (chargeGaugeSecondFill != null) chargeGaugeSecondFill.SetFillAmount(0f);
        }

        private static void SetBarVisible(Image fill, bool visible)
        {
            if (fill == null) return;
            var root = fill.transform.parent;
            if (root != null) root.gameObject.SetActive(visible);
            else fill.enabled = visible;
        }

        private static void SetGauge(Image fill, float value)
        {
            if (fill == null) return;
            value = Mathf.Clamp01(value);
            if(fill.transform.parent is RectTransform vertical && vertical.rect.height>vertical.rect.width*2){fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Vertical;fill.fillOrigin=0;fill.fillAmount=value;return;}
            fill.fillAmount = value;
            fill.type = Image.Type.Simple;
            var rect = fill.rectTransform;
            if (rect.parent is not RectTransform parent) return;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(parent.rect.width * value, 0f);
        }

        private static void SetValueText(TextMeshProUGUI label, string value)
        {
            if (label != null) label.text = value;
        }

        private void SetShieldAvailability(PlayerCombatRuntime player)
        {
            var guardAvailable = player.IsAlive && player.ShieldState != ShieldState.Broken && player.ShieldCurrent > 0f;
            if (shield != null)
                shield.color = guardAvailable ? new Color32(147,171,182,255) : new Color32(75,74,96,255);
            if (shieldText != null)
                shieldText.color = guardAvailable ? new Color(.06f, .08f, .12f, 1f) : new Color(.86f, .9f, .94f, 1f);
        }
    }
}
