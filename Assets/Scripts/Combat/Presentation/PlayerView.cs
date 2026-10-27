using System.Linq;
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
        [Header("Combat framing (canvas coordinates)")]
        [SerializeField] private Vector2 combatRootPosition = new Vector2(0, -532);
        [SerializeField, Min(1f)] private float combatImageSizeMultiplier = 1.5f;
        [SerializeField, Min(.01f)] private float framingDuration = .4f;
        private RectTransform framingRoot;
        private Vector2 framingRestPosition;
        private Vector2 bodyRestSize;
        private float framingProgress;
        private bool combatFraming;
        private float ImageSizeMultiplier => Mathf.Lerp(1f, combatImageSizeMultiplier, Mathf.SmoothStep(0f, 1f, framingProgress));
        private float cooldownFullWidth;
        private float cooldownAnchorY;
        public ArcadePresentation presentation;
        private CombatSpriteAnimator animator;
        private float attackUntil;
        public bool PreparingAttack { get; set; }
        public void PlayAttack(){attackUntil=Time.time+.3f;if(animator!=null)animator.Play(animator.library,CombatVisualState.Attack,true);}
        public Vector2 GuardBodySize=>animator!=null&&animator.Available?new Vector2(520,360)*ImageSizeMultiplier:root.rect.size;
        public Vector2 GuardBodyOffset=>animator!=null&&animator.Available?new Vector2(0,-20)*ImageSizeMultiplier:Vector2.zero;

        public RectTransform Root => root;
        public bool IsCombatFramingReady => combatFraming && framingProgress >= 1f;

        public void SetCombatFraming(RectTransform anchor, bool inCombat, bool immediate = false)
        {
            if (anchor == null) return;
            if (framingRoot != anchor)
            {
                RestoreFraming();
                framingRoot = anchor;
                framingRestPosition = anchor.anchoredPosition;
            }
            combatFraming = inCombat;
            if (immediate) framingProgress = inCombat ? 1f : 0f;
            ApplyFraming();
        }

        private void LateUpdate()
        {
            if (framingRoot == null) return;
            framingProgress = Mathf.MoveTowards(framingProgress, combatFraming ? 1f : 0f, Time.unscaledDeltaTime / Mathf.Max(.01f, framingDuration));
            ApplyFraming();
        }

        private void ApplyFraming()
        {
            if (framingRoot != null)
                framingRoot.anchoredPosition = Vector2.Lerp(framingRestPosition, combatRootPosition, Mathf.SmoothStep(0f, 1f, framingProgress));
            // Resize the image only; never scale the object or its UI children.
            if (body != null && bodyRestSize != Vector2.zero) body.rectTransform.sizeDelta = bodyRestSize * ImageSizeMultiplier;
            if (cooldown != null && presentation != null && presentation.arcade && framingRoot != null)
            {
                var position = cooldown.rectTransform.anchoredPosition;
                position.y = -160 + framingRestPosition.y - framingRoot.anchoredPosition.y;
                cooldown.rectTransform.anchoredPosition = position;
            }
        }

        private void RestoreFraming()
        {
            if (framingRoot != null) framingRoot.anchoredPosition = framingRestPosition;
            framingProgress = 0f;
            combatFraming = false;
            ApplyFraming();
        }

        private void OnDisable() => RestoreFraming();

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
            if (runtime.Data.combatSprite != null && body != null)
            {
                body.sprite = runtime.Data.combatSprite; body.preserveAspect = true;
                var animated=presentation!=null&&presentation.arcade&&runtime.Data.combatAnimation!=null;
                bodyRestSize = animated?new Vector2(640,680):new Vector2(190,225);
                body.rectTransform.anchoredPosition=animated?new Vector2(0,16):Vector2.zero;
                if(animated)
                {
                    animator=animator!=null?animator:body.GetComponent<CombatSpriteAnimator>()??body.gameObject.AddComponent<CombatSpriteAnimator>();
                    animator.presentation=presentation;
                    animator.Play(runtime.Data.combatAnimation,Time.time<attackUntil?CombatVisualState.Attack:runtime.IsGuarding?CombatVisualState.Guard:PreparingAttack?CombatVisualState.Ready:CombatVisualState.Idle);
                }
                if (head != null) head.enabled = false;
            }
            else if(body!=null){body.sprite=null;bodyRestSize=new Vector2(108,146);body.rectTransform.anchoredPosition=Vector2.zero;if(head!=null)head.enabled=true;}
            if (guardFx != null) guardFx.enabled = runtime.IsGuarding && !(presentation!=null&&presentation.arcade&&runtime.Data.combatAnimation!=null);
            if (cooldown != null)
            {
                var coolingDown = runtime.AttackCooldownRemaining > 0f && runtime.CurrentCooldown > 0f;
                var progress = coolingDown ? Mathf.Clamp01(1f - runtime.AttackCooldownRemaining / runtime.CurrentCooldown) : 0f;
                SetCooldownProgress(progress);
                cooldown.enabled = coolingDown;
            }

            var tint = runtime.Buffs?.Active.FirstOrDefault(buff => buff.Data.tintOwner);
            var bodyColor = runtime.IsAlive ? tint != null ? Color.Lerp(Color.white, tint.Data.presentationColor, .35f) : runtime.Data.combatSprite != null ? Color.white : runtime.Data.characterColor : Color.gray;
            if (body != null) body.color = bodyColor;
            if (head != null) head.color = runtime.IsAlive ? Color.Lerp(bodyColor, Color.white, .35f) : Color.gray;
            if (guardFx != null) guardFx.color = new Color(.3f, .75f, 1f, .55f);
            ApplyFraming();
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
            var arcade=presentation!=null&&presentation.arcade;
            rect.anchorMin = new Vector2(.5f, arcade?.5f:1f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(-cooldownFullWidth * .5f, arcade?-160:cooldownAnchorY);
            rect.sizeDelta = new Vector2(cooldownFullWidth * progress, rect.sizeDelta.y);
        }
    }
}
