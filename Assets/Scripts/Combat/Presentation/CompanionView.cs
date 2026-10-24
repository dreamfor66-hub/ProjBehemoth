using UnityEngine;
namespace DungeonSlash
{
    public sealed class CompanionView : MonoBehaviour
    {
        public RectTransform body, tail;
        private CompanionData data;
        private float pulse;
        public ArcadePresentation presentation;
        public CombatVisualState State=>pulse>0?CombatVisualState.Attack:CombatVisualState.Idle;
        public void Configure(CompanionData definition)
        {
            pulse=0;
            data = definition; ((RectTransform)transform).anchoredPosition = presentation!=null&&presentation.arcade?new Vector2(-280,0):data.playerOffset;
            if (body != null && data.combatSprite != null)
            {
                var art=body.GetComponent<UnityEngine.UI.Image>();art.sprite=data.combatSprite;art.color=Color.white;art.preserveAspect=true;
                foreach(var graphic in GetComponentsInChildren<UnityEngine.UI.Graphic>(true))if(graphic!=art)graphic.enabled=false;
                body.sizeDelta=presentation!=null&&presentation.arcade?new Vector2(288,320):new Vector2(126,106);if(tail!=null)tail.gameObject.SetActive(false);
                if(presentation!=null&&presentation.arcade&&data.combatAnimation!=null)art.sprite=data.combatAnimation.idle;
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
                body.GetComponent<UnityEngine.UI.Image>().sprite=pulse>0?data.combatAnimation.attack:data.combatAnimation.idle;
                body.anchoredPosition=Vector2.zero;return;
            }
            body.anchoredPosition = new Vector2(Mathf.Sin(pulse * Mathf.PI) * data.attackLungeDistance, Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3)) * data.idleBobHeight);
            if (tail != null) tail.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.unscaledTime * 9) * 20);
        }
    }
}
