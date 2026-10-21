using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class PlayerView : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private Image body;
        [SerializeField] private Image head;
        [SerializeField] private Image guardFx;
        [SerializeField] private Image cooldown;
        private float cooldownFullWidth;
        private float cooldownAnchorY;

        public RectTransform Root => root;

        public void Configure(RectTransform newRoot, Image newBody, Image newHead, Image newGuardFx, Image newCooldown)
        {
            root = newRoot;
            body = newBody;
            head = newHead;
            guardFx = newGuardFx;
            cooldown = newCooldown;
        }

        public void Bind(PlayerCombatRuntime runtime)
        {
            if (runtime == null) return;
            if (guardFx != null) guardFx.enabled = runtime.IsGuarding;
            if (cooldown != null)
            {
                var coolingDown = runtime.AttackCooldownRemaining > 0f && runtime.CurrentCooldown > 0f;
                var progress = coolingDown ? Mathf.Clamp01(1f - runtime.AttackCooldownRemaining / runtime.CurrentCooldown) : 0f;
                SetCooldownProgress(progress);
                cooldown.enabled = coolingDown;
            }

            var bodyColor = runtime.IsAlive ? runtime.IsEnraged ? new Color(1f, .32f, .14f) : runtime.Data.characterColor : Color.gray;
            if (body != null) body.color = bodyColor;
            if (head != null) head.color = runtime.IsAlive ? Color.Lerp(bodyColor, Color.white, .35f) : Color.gray;
            if (guardFx != null) guardFx.color = runtime.IsPerfectWindow ? new Color(1f, .9f, .35f, .9f) : new Color(.3f, .75f, 1f, .55f);
        }

        private void SetCooldownProgress(float progress)
        {
            if (cooldown == null) return;
            var rect = cooldown.rectTransform;
            if (cooldownFullWidth <= 0f)
            {
                cooldownFullWidth = rect.sizeDelta.x;
                cooldownAnchorY = rect.anchoredPosition.y;
            }

            cooldown.type = Image.Type.Simple;
            cooldown.fillAmount = progress;
            rect.anchorMin = new Vector2(.5f, 1f);
            rect.anchorMax = new Vector2(.5f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(-cooldownFullWidth * .5f, cooldownAnchorY);
            rect.sizeDelta = new Vector2(cooldownFullWidth * progress, rect.sizeDelta.y);
        }
    }
}
