using UnityEngine;
namespace DungeonSlash
{
    public sealed class CompanionView : MonoBehaviour
    {
        public RectTransform body, tail;
        private CompanionData data;
        private float pulse;
        private CombatSpriteAnimator animator;
        public ArcadePresentation presentation;
        public CombatVisualState State=>pulse>0?CombatVisualState.Attack:CombatVisualState.Idle;
        public void Configure(CompanionData definition)
        {
            pulse=0;
            data = definition; ((RectTransform)transform).anchoredPosition = data.playerOffset;
            if (body != null && data.combatSprite != null)
            {
                var art=body.GetComponent<UnityEngine.UI.Image>();art.sprite=data.combatSprite;art.enabled=true;art.color=Color.white;art.preserveAspect=true;
                foreach(var graphic in GetComponentsInChildren<UnityEngine.UI.Graphic>(true))if(graphic!=art)graphic.enabled=false;
                body.sizeDelta=presentation!=null&&presentation.arcade?new Vector2(288,320):new Vector2(126,106);if(tail!=null)tail.gameObject.SetActive(false);
                if(presentation!=null&&presentation.arcade&&data.combatAnimation!=null)
                {
                    animator=art.GetComponent<CombatSpriteAnimator>()??art.gameObject.AddComponent<CombatSpriteAnimator>();
                    animator.presentation=presentation;animator.Play(data.combatAnimation,CombatVisualState.Idle);
                    FramePainting(art.sprite);
                }
            }
            else if(body!=null)
            {var art=body.GetComponent<UnityEngine.UI.Image>();art.sprite=null;art.color=new Color(.62f,.42f,.23f);body.sizeDelta=new Vector2(68,40);foreach(var graphic in GetComponentsInChildren<UnityEngine.UI.Graphic>(true))graphic.enabled=true;if(tail!=null)tail.gameObject.SetActive(true);}
        }
        public void Attack(Vector2 target) { pulse = 1; }
        private void Update()
        {
            if (data == null || body == null) return;
            pulse = Mathf.MoveTowards(pulse, 0, Time.unscaledDeltaTime * 4);
            if(presentation!=null&&presentation.arcade&&data.combatAnimation!=null)
            {
                if(animator!=null)animator.Play(data.combatAnimation,State);
                FramePainting(body.GetComponent<UnityEngine.UI.Image>().sprite);
                body.anchoredPosition=Vector2.zero;return;
            }
            body.anchoredPosition = new Vector2(Mathf.Sin(pulse * Mathf.PI) * data.attackLungeDistance, Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3)) * data.idleBobHeight);
            if (tail != null) tail.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.unscaledTime * 9) * 20);
        }
        private void FramePainting(Sprite painting)
        {
            if(painting==null||data.combatSprite==null)return;
            // Constant reference-pixel scale and ground baseline, regardless of transparent
            // frame padding. This does not move, rotate or distort an authored pose.
            body.anchorMin=body.anchorMax=new Vector2(.5f,0);body.pivot=new Vector2(.5f,0);
            body.sizeDelta=painting.rect.size*(320f/data.combatSprite.rect.height);
        }
    }
}
