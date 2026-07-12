using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class MonsterView : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private Image body;
        [SerializeField] private Text stateLabel;
        [SerializeField] private Image hpFill;
        [SerializeField] private Text attackNameLabel;
        [SerializeField] private Text chargeTimeLabel;
        [SerializeField] private Image attackNameBackdrop;
        [SerializeField] private Image chargeTimeBackdrop;
        private float attackNameHideAt;

        public RectTransform Root => root;

        public void Configure(RectTransform newRoot, Image newBody, Text newStateLabel)
        {
            Configure(newRoot, newBody, newStateLabel, null, null, null);
        }

        public void Configure(RectTransform newRoot, Image newBody, Text newStateLabel, Image newHpFill, Text newAttackNameLabel, Text newChargeTimeLabel)
        {
            Configure(newRoot, newBody, newStateLabel, newHpFill, newAttackNameLabel, newChargeTimeLabel, null, null);
        }

        public void Configure(RectTransform newRoot, Image newBody, Text newStateLabel, Image newHpFill, Text newAttackNameLabel, Text newChargeTimeLabel, Image newAttackNameBackdrop, Image newChargeTimeBackdrop)
        {
            root = newRoot;
            body = newBody;
            stateLabel = newStateLabel;
            hpFill = newHpFill;
            attackNameLabel = newAttackNameLabel;
            chargeTimeLabel = newChargeTimeLabel;
            attackNameBackdrop = newAttackNameBackdrop;
            chargeTimeBackdrop = newChargeTimeBackdrop;
        }

        public void Bind(MonsterRuntime runtime)
        {
            if (runtime == null || runtime.Data == null) return;
            if (body != null) body.color = runtime.State == MonsterState.Stunned ? new Color(1f, .86f, .22f) : runtime.Data.bodyColor;
            if (stateLabel != null)
            {
                stateLabel.text = runtime.State switch
                {
                    MonsterState.Attacking => "Attacking",
                    MonsterState.Charging => "Charging",
                    _ => string.Empty
                };
                stateLabel.enabled = !string.IsNullOrEmpty(stateLabel.text);
            }
            SetGauge(hpFill, runtime.CurrentHp / runtime.Data.maxHp);
            if (chargeTimeLabel != null)
            {
                var charging = runtime.State == MonsterState.Charging && runtime.StateTimer > 0f;
                chargeTimeLabel.enabled = charging;
                if (charging) chargeTimeLabel.text = $"{runtime.StateTimer:0.0}";
                SetVisible(chargeTimeBackdrop, charging);
            }
            else SetVisible(chargeTimeBackdrop, false);

            if (attackNameLabel != null && attackNameLabel.enabled && Time.unscaledTime >= attackNameHideAt)
            {
                attackNameLabel.enabled = false;
                SetVisible(attackNameBackdrop, false);
            }
        }

        public void ShowAttackName(string attackName, float seconds = 1.05f)
        {
            if (attackNameLabel == null) return;
            attackNameLabel.text = attackName;
            attackNameLabel.enabled = !string.IsNullOrWhiteSpace(attackName);
            SetVisible(attackNameBackdrop, attackNameLabel.enabled);
            attackNameHideAt = Time.unscaledTime + seconds;
        }

        public void ResetOpacity() => SetOpacity(1f);

        public IEnumerator FadeOut(float seconds)
        {
            SetOpacity(1f);
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                SetOpacity(1f - Mathf.Clamp01(elapsed / seconds));
                yield return null;
            }
            SetOpacity(0f);
        }

        private static void SetGauge(Image fill, float value)
        {
            if (fill == null) return;
            value = Mathf.Clamp01(value);
            var rect = fill.rectTransform;
            if (rect.parent is not RectTransform parent) return;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(parent.rect.width * value, 0f);
        }

        private void SetOpacity(float alpha)
        {
            SetAlpha(body, alpha);
            SetAlpha(stateLabel, alpha);
            SetAlpha(hpFill, alpha);
            SetAlpha(attackNameLabel, alpha);
            SetAlpha(chargeTimeLabel, alpha);
            SetAlpha(attackNameBackdrop, alpha);
            SetAlpha(chargeTimeBackdrop, alpha);
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null) return;
            var color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }

        private static void SetVisible(Graphic graphic, bool visible)
        {
            if (graphic != null) graphic.enabled = visible;
        }
    }
}
