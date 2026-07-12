using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class CombatHudView : MonoBehaviour
    {
        [SerializeField] private Image playerHp;
        [SerializeField] private Image shield;
        [SerializeField] private Image monsterHp;
        [SerializeField] private Text playerHpText;
        [SerializeField] private Text shieldText;
        [SerializeField] private Text playerStats;
        [SerializeField] private Text monsterStats;
        [SerializeField] private RectTransform chargeGaugeRoot;
        [SerializeField] private ArcGaugeGraphic chargeGaugeTrack;
        [SerializeField] private ArcGaugeGraphic chargeGaugeFill;
        [SerializeField] private Vector2 chargePointerOffset = new(0f, 90f);

        public void Configure(Image newPlayerHp, Image newShield, Image newMonsterHp, Text newPlayerStats, Text newMonsterStats, RectTransform newChargeGaugeRoot, ArcGaugeGraphic newChargeGaugeTrack, ArcGaugeGraphic newChargeGaugeFill, Text newPlayerHpText = null, Text newShieldText = null)
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
            chargePointerOffset = new Vector2(0f, 90f);
        }

        public void Bind(PlayerCombatRuntime player, MonsterRuntime monster, PointerGestureState gesture, float chargeProgress, Vector2 chargeOrigin)
        {
            var hasPlayer = player != null;
            SetBarVisible(playerHp, hasPlayer);
            SetBarVisible(shield, hasPlayer);
            if (hasPlayer)
            {
                SetGauge(playerHp, player.CurrentHp / player.MaxHp);
                SetGauge(shield, player.ShieldMax <= 0f ? 0f : player.ShieldCurrent / player.ShieldMax);
                SetValueText(playerHpText, $"HP {player.CurrentHp:0} / {player.MaxHp:0}");
                SetValueText(shieldText, $"SHIELD {player.ShieldCurrent:0} / {player.ShieldMax:0}");
                SetShieldAvailability(player);
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

            var showCharge = gesture == PointerGestureState.Charging || gesture == PointerGestureState.Charged;
            if (chargeGaugeRoot != null) chargeGaugeRoot.gameObject.SetActive(showCharge);
            if (showCharge)
            {
                if (chargeGaugeRoot != null) chargeGaugeRoot.anchoredPosition = chargeOrigin + chargePointerOffset;
                if (chargeGaugeFill != null) chargeGaugeFill.SetFillAmount(chargeProgress);
            }
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

        private static void SetValueText(Text label, string value)
        {
            if (label != null) label.text = value;
        }

        private void SetShieldAvailability(PlayerCombatRuntime player)
        {
            var guardAvailable = player.IsAlive && player.ShieldState != ShieldState.Broken && player.ShieldCurrent > 0f;
            if (shield != null)
                shield.color = guardAvailable ? new Color(.96f, .98f, 1f, 1f) : new Color(.34f, .44f, .53f, .9f);
            if (shieldText != null)
                shieldText.color = guardAvailable ? new Color(.06f, .08f, .12f, 1f) : new Color(.86f, .9f, .94f, 1f);
        }
    }
}
