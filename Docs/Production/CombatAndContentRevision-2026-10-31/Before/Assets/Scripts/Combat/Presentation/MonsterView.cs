using TMPro;
using System;
using System.Collections;
using System.Linq;
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
        private float attackUntil;
        private MonsterData boundData;
        private float authoredNameWidth,authoredTimeWidth;
        public void PlayAttack(float seconds=.4f){attackUntil=Time.time+seconds;if(animator!=null)animator.Play(animator.library,CombatVisualState.Attack,true);}
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
            // Species size affects the body and hitboxes; captions keep a readable shared screen size.
            var uiScale=1f/Mathf.Max(.1f,runtime.VisualScale);
            foreach(var label in new[]{stateLabel,attackNameLabel,chargeTimeLabel,directionalGuardLabel})
                if(label!=null)label.rectTransform.localScale=Vector3.one*uiScale;
            foreach(var graphic in new[]{attackNameBackdrop,chargeTimeBackdrop})
                if(graphic!=null)graphic.rectTransform.localScale=Vector3.one*uiScale;
            if(hpFill!=null&&hpFill.transform.parent is RectTransform hpRoot)hpRoot.localScale=Vector3.one*uiScale;
            if(directionalGuardFill!=null&&directionalGuardFill.transform.parent is RectTransform guardRoot)guardRoot.localScale=Vector3.one*uiScale;
            var library = runtime.Data.combatAnimation;
            // A break interrupts even an attack painting that has not timed out yet.
            if (runtime.IsStunned) attackUntil = 0f;
            var pose = !runtime.IsAlive ? CombatVisualState.Dead : runtime.IsStunned ? CombatVisualState.Stunned : Time.time < attackUntil ? CombatVisualState.Attack
                : runtime.State is MonsterState.Charging or MonsterState.Attacking ? CombatVisualState.Ready : CombatVisualState.Idle;
            var sprite = library != null ? library.For(pose) : runtime.Data.portrait;
            if (sprite == null && library != null) sprite = library.idle;
            if (sprite == null) sprite = runtime.Data.portrait;
            if (body != null)
            {
                body.overrideSprite = null;
                body.sprite = sprite;
                body.enabled = sprite != null;
                body.color = runtime.IsStunned && (library == null || library.stunned == null) ? new Color(1f, .89f, .65f) : Color.white;
                body.preserveAspect = true;
                body.useSpriteMesh = false;
                body.rectTransform.anchorMin = body.rectTransform.anchorMax = new Vector2(.5f, .5f);
                body.rectTransform.pivot = new Vector2(.5f, .5f);
                body.rectTransform.localScale = Vector3.one;
                if (sprite != null)
                {
                    var pixelSize = MonsterData.ArtworkUnitsPerWorldUnit / sprite.pixelsPerUnit;
                    var size = sprite.rect.size * pixelSize;
                    root.sizeDelta = size;
                    body.rectTransform.sizeDelta = size;
                    body.rectTransform.anchoredPosition = (sprite.rect.size * .5f - sprite.pivot) * pixelSize;
                }
                Animate(runtime.Data, pose);
            }
            var cropped = library != null && library.tightPoseFrames;
            var bounds = runtime.Data.ArtworkBounds;
            if (cropped && library != null)
                foreach (var frame in new[] { library.idle, library.ready, library.attack })
                    if (frame != null)
                    {
                        var b = MonsterData.ArtworkBoundsOf(frame);
                        bounds.yMax = Mathf.Max(bounds.yMax, b.yMax);
                        bounds.xMin = Mathf.Min(bounds.xMin, b.xMin);
                        bounds.xMax = Mathf.Max(bounds.xMax, b.xMax);
                    }
            var top = bounds.yMax + (cropped ? 36 : 65);
            var scene = GetComponentInParent<CombatSceneView>();
            if (cropped && scene != null)
                top = Mathf.Min(top, scene.LogicalArea.rect.yMax - 130 - scene.LogicalArea.InverseTransformPoint(root.position).y);
            if (stateLabel != null)
            {
                stateLabel.rectTransform.anchoredPosition = new Vector2(0, top);
                stateLabel.fontSize=28;
                stateLabel.rectTransform.sizeDelta=new Vector2(Mathf.Clamp(bounds.width*.4f,300,500),40);
                if (cropped)
                {
                    stateLabel.rectTransform.sizeDelta = new Vector2(Mathf.Max(300, bounds.width * .4f), 40);
                    stateLabel.fontSize = 28;
                }
            }
            if (hpFill != null && hpFill.transform.parent is RectTransform bar)
                bar.anchoredPosition = new Vector2(0, top - (cropped ? 38 : library != null && library.UsesIllustratedLayout ? 43 : 52));
            if (cropped && directionalGuardLabel != null)
            {
                directionalGuardLabel.rectTransform.anchoredPosition = new Vector2(0, top - 73);
                directionalGuardLabel.rectTransform.sizeDelta = new Vector2(310, 32);
                directionalGuardLabel.fontSize = 22;
                directionalGuardLabel.outlineColor = Color.black;
                directionalGuardLabel.outlineWidth = .18f;
            }
            if (cropped && directionalGuardFill != null && directionalGuardFill.transform.parent is RectTransform guardBar)
                guardBar.anchoredPosition = new Vector2(0, top - 98);
            if (cropped && scene != null && bounds.width > scene.LogicalArea.rect.width * .6f)
            {
                var origin=(Vector2)scene.LogicalArea.InverseTransformPoint(root.position);
                var actionPosition = (new Vector2(500,190)-origin)/runtime.VisualScale;
                var timerPosition = (new Vector2(500,125)-origin)/runtime.VisualScale;
                if (attackNameLabel != null) { attackNameLabel.rectTransform.anchoredPosition = actionPosition; attackNameLabel.rectTransform.sizeDelta = new Vector2(350, 50); attackNameLabel.fontSize = 30; }
                if (attackNameBackdrop != null) { attackNameBackdrop.rectTransform.anchoredPosition = actionPosition; attackNameBackdrop.rectTransform.sizeDelta = new Vector2(370, 54); }
                if (chargeTimeLabel != null) { chargeTimeLabel.rectTransform.anchoredPosition = timerPosition; chargeTimeLabel.rectTransform.sizeDelta = new Vector2(240, 56); chargeTimeLabel.fontSize = 40; }
                if (chargeTimeBackdrop != null) { chargeTimeBackdrop.rectTransform.anchoredPosition = timerPosition; chargeTimeBackdrop.rectTransform.sizeDelta = new Vector2(260, 60); }
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
                if (charging)
                {
                    chargeTimeLabel.text = runtime.StateTimer.ToString("0.0")+"<size=60%>"+GameText.Get("ui.feedback.seconds")+"</size>";
                }
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
            FitAttackCaption();
            if(scene!=null)
            {
                var origin=(Vector2)scene.LogicalArea.InverseTransformPoint(root.position);var scale=Mathf.Max(.1f,runtime.VisualScale);
                void ClampCaption(RectTransform caption)
                {if(caption==null)return;var p=caption.anchoredPosition;var x=origin.x+p.x*scale;var half=caption.rect.width*.5f;x=Mathf.Clamp(x,scene.LogicalArea.rect.xMin+half+20,scene.LogicalArea.rect.xMax-half-20);p.x=(x-origin.x)/scale;var y=origin.y+p.y*scale;y=Mathf.Clamp(y,scene.LogicalArea.rect.yMin+140+caption.rect.height*.5f,scene.LogicalArea.rect.yMax-135-caption.rect.height*.5f);p.y=(y-origin.y)/scale;caption.anchoredPosition=p;}
                ClampCaption(stateLabel?.rectTransform);ClampCaption(hpFill!=null?hpFill.transform.parent as RectTransform:null);
                ClampCaption(attackNameLabel?.rectTransform);ClampCaption(attackNameBackdrop?.rectTransform);ClampCaption(chargeTimeLabel?.rectTransform);ClampCaption(chargeTimeBackdrop?.rectTransform);
            }
        }

        public void ShowAttackName(string attackName, float seconds = 1.05f)
        {
            if (attackNameLabel == null) return;
            attackNameLabel.text = attackName;
            attackNameLabel.enabled = !string.IsNullOrWhiteSpace(attackName);
            SetVisible(attackNameBackdrop, attackNameLabel.enabled);
            attackNameHideAt = Time.unscaledTime + seconds;
            FitAttackCaption();
        }

        private void FitAttackCaption()
        {
            if(authoredNameWidth<=0&&attackNameBackdrop!=null)authoredNameWidth=attackNameBackdrop.rectTransform.rect.width;
            if(authoredTimeWidth<=0&&chargeTimeBackdrop!=null)authoredTimeWidth=chargeTimeBackdrop.rectTransform.rect.width;
            // Short names retain a generous brush border. Shared rules derive the size from the text.
            void Fit(TextMeshProUGUI label,Image backdrop,float original,float minimum,float padding)
            {
                if(label==null||backdrop==null||!label.enabled)return;
                if(original<=0)original=backdrop.rectTransform.rect.width;
                var natural=label.GetPreferredValues(label.text).x;
                var width=Mathf.Max(natural+padding,Mathf.Min(original,Mathf.Max(original*.76f,minimum)));
                var labelSize=label.rectTransform.sizeDelta;labelSize.x=width-28;label.rectTransform.sizeDelta=labelSize;
                var size=backdrop.rectTransform.sizeDelta;size.x=width;backdrop.rectTransform.sizeDelta=size;
            }
            Fit(attackNameLabel,attackNameBackdrop,authoredNameWidth,220,84);
            Fit(chargeTimeLabel,chargeTimeBackdrop,authoredTimeWidth,142,56);
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
