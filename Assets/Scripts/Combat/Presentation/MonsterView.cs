using TMPro;
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
        [SerializeField] private TextMeshProUGUI stateLabel;
        [SerializeField] private Image hpFill;
        [SerializeField] private TextMeshProUGUI attackNameLabel;
        [SerializeField] private TextMeshProUGUI chargeTimeLabel;
        [SerializeField] private Image attackNameBackdrop;
        [SerializeField] private Image chargeTimeBackdrop;
        [SerializeField] private Image directionalGuardFill;
        [SerializeField] private TextMeshProUGUI directionalGuardLabel;
        private float attackNameHideAt;
        public ArcadePresentation presentation;
        private CombatSpriteAnimator animator;
        public float arcadeArtScale=1.65f;
        private float attackUntil;
        private MonsterData boundData;
        public void PlayAttack(){attackUntil=Time.time+.3f;if(animator!=null)animator.Play(animator.library,CombatVisualState.Attack,true);}
        public void PlayDead(){if(boundData!=null)Animate(boundData,CombatVisualState.Dead);}
        private void Animate(MonsterData data,CombatVisualState state)
        {
            if(presentation==null||!presentation.arcade||data.combatAnimation==null||body==null)return;
            animator=animator!=null?animator:body.GetComponent<CombatSpriteAnimator>()??body.gameObject.AddComponent<CombatSpriteAnimator>();
            animator.presentation=presentation;animator.Play(data.combatAnimation,state);
        }

        public RectTransform Root => root;

        public void Configure(RectTransform newRoot, Image newBody, TextMeshProUGUI newStateLabel)
        {
            Configure(newRoot, newBody, newStateLabel, null, null, null);
        }

        public void Configure(RectTransform newRoot, Image newBody, TextMeshProUGUI newStateLabel, Image newHpFill, TextMeshProUGUI newAttackNameLabel, TextMeshProUGUI newChargeTimeLabel)
        {
            Configure(newRoot, newBody, newStateLabel, newHpFill, newAttackNameLabel, newChargeTimeLabel, null, null);
        }

        public void Configure(RectTransform newRoot, Image newBody, TextMeshProUGUI newStateLabel, Image newHpFill, TextMeshProUGUI newAttackNameLabel, TextMeshProUGUI newChargeTimeLabel, Image newAttackNameBackdrop, Image newChargeTimeBackdrop, Image newDirectionalGuardFill = null, TextMeshProUGUI newDirectionalGuardLabel = null)
        {
            root = newRoot;
            body = newBody;
            stateLabel = newStateLabel;
            hpFill = newHpFill;
            attackNameLabel = newAttackNameLabel;
            chargeTimeLabel = newChargeTimeLabel;
            attackNameBackdrop = newAttackNameBackdrop;
            chargeTimeBackdrop = newChargeTimeBackdrop;
            directionalGuardFill = newDirectionalGuardFill;
            directionalGuardLabel = newDirectionalGuardLabel;
        }

        public void Bind(MonsterRuntime runtime)
        {
            if (runtime == null || runtime.Data == null) return;
            boundData=runtime.Data;
            var artScale = presentation != null && presentation.arcade ? arcadeArtScale : 1f;
            float artOffsetY = 0f;
            float artHeight = root.rect.height;
            if (body != null && runtime.Data.combatAnimation is { UsesIllustratedLayout: true } groundLayout)
            {
                // Every state uses the same Idle contact line so attacks can lift feet naturally.
                var sprite = groundLayout.idle;
                if (sprite != null) artHeight = Mathf.Min(root.rect.height, root.rect.width * sprite.rect.height / sprite.rect.width);
                artHeight *= artScale * groundLayout.canvasScale;
                artOffsetY = -runtime.Data.bodyHitRadius + (.5f - groundLayout.groundContactY) * artHeight;
                body.rectTransform.anchoredPosition = new Vector2(0, artOffsetY);
            }
            if (presentation != null && presentation.arcade && runtime.Data.combatAnimation is { UsesIllustratedLayout: true } layout)
            {
                var nameHeight = artOffsetY + (layout.artworkTopY - .5f) * artHeight + 65f;
                if (stateLabel != null) stateLabel.rectTransform.anchoredPosition = new Vector2(0, nameHeight);
                if (hpFill != null && hpFill.transform.parent is RectTransform bar)
                    bar.anchoredPosition = new Vector2(0, nameHeight - 43f);
            }
            if (body != null)
            {
                body.sprite = runtime.Data.portrait;body.preserveAspect = runtime.Data.portrait != null;
                body.color = runtime.State == MonsterState.Stunned ? new Color(1f, .89f, .65f) : runtime.Data.portrait != null ? Color.white : runtime.Data.bodyColor;
                body.rectTransform.localScale=Vector3.one*(presentation!=null&&presentation.arcade?arcadeArtScale:1);
                Animate(runtime.Data,!runtime.IsAlive?CombatVisualState.Dead:Time.time<attackUntil?CombatVisualState.Attack:runtime.State is MonsterState.Charging or MonsterState.Attacking?CombatVisualState.Ready:CombatVisualState.Idle);
            }
            if (stateLabel != null)
            {
                stateLabel.text = (runtime.IsUnaware ? GameText.Get("ui.stealth.unaware")
                    + (runtime.Data.combatAnimation is { UsesIllustratedLayout: true } ? " · " : "\n") : "")
                    + runtime.Data.displayName
                    + (runtime.PoisonStacks > 0 ? GameText.Format("ui.monsterview.500afc29", runtime.PoisonStacks) : "")
                    + (runtime.SlowRemaining > 0 ? GameText.Get("ui.monsterview.95434960") : "")
                    + (runtime.State == MonsterState.Stunned ? GameText.Get("ui.monsterview.16148c80") : "");
                stateLabel.enabled = !string.IsNullOrEmpty(stateLabel.text);
            }
            SetGauge(hpFill, runtime.MaxHp <= 0f ? 0f : runtime.CurrentHp / runtime.MaxHp);
            if (chargeTimeLabel != null)
            {
                var charging = (runtime.State is MonsterState.Charging or MonsterState.Attacking) && runtime.StateTimer > 0f;
                chargeTimeLabel.enabled = charging;
                if (charging) chargeTimeLabel.text = runtime.StateTimer.ToString("0.0")+GameText.Get("ui.feedback.seconds");
                if(charging&&attackNameLabel!=null){attackNameLabel.text=(runtime.State==MonsterState.Charging?runtime.CurrentChargeAttack:runtime.NormalAttack)?.displayName;attackNameLabel.enabled=true;attackNameHideAt=Time.unscaledTime+.1f;SetVisible(attackNameBackdrop,true);}
                SetVisible(chargeTimeBackdrop, charging);
            }
            else SetVisible(chargeTimeBackdrop, false);

            var showDirectionalGuard = runtime.HasDirectionalGuard;
            SetGauge(directionalGuardFill, runtime.DirectionalGuardMax <= 0f ? 0f : runtime.DirectionalGuardCurrent / runtime.DirectionalGuardMax);
            if (directionalGuardFill != null)
                directionalGuardFill.color = runtime.IsDirectionalGuardActive ? new Color(.3f, .88f, 1f) : new Color(.32f, .52f, .65f);
            SetVisible(directionalGuardFill, showDirectionalGuard);
            SetVisible(directionalGuardFill != null ? directionalGuardFill.transform.parent.GetComponent<Image>() : null, showDirectionalGuard);
            if (directionalGuardLabel != null)
            {
                directionalGuardLabel.text = runtime.DirectionalGuardLabel;
                directionalGuardLabel.enabled = showDirectionalGuard;
            }

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
            SetAlpha(directionalGuardFill, alpha);
            SetAlpha(directionalGuardLabel, alpha);
            SetAlpha(directionalGuardFill != null ? directionalGuardFill.transform.parent.GetComponent<Image>() : null, alpha);
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
