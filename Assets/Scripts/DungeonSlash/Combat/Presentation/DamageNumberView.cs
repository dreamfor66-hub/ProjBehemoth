using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class DamageNumberView : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private float lifetime = .8f;
        [SerializeField] private float riseSpeed = 52f;
        [SerializeField] private TextMeshProUGUI[] outlineLabels;
        [SerializeField] private float criticalLifetime = 1.15f;
        [SerializeField] private float gravity = 1800f;
        [SerializeField] private Vector2 criticalLaunch = new(160, 320);
        private float spawnedAt;
        private Color baseColor;
        private bool bouncing;
        private float floor;
        private Vector2 velocity,position;
        private int bounces;
        private static int direction;
        public bool IsCritical => bouncing;
        public int BounceCount => bounces;

        public void Configure(RectTransform newRoot, TextMeshProUGUI newLabel)
        {
            root = newRoot;
            label = newLabel;
        }

        public void Show(Vector2 position, string message, Color color)
        {
            bouncing=false;
            if(outlineLabels!=null)foreach(var outline in outlineLabels)if(outline!=null)outline.gameObject.SetActive(false);
            if (root != null) root.anchoredPosition = position;
            if (label != null)
            {
                label.text = message;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                var width = Mathf.Max(110, Mathf.Ceil(label.GetPreferredValues(message).x / 4) * 4 + 16);
                label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                label.color = color;
            }
            baseColor = color;
            spawnedAt = Time.unscaledTime;
        }

        public void ConfigureOutline(TextMeshProUGUI[] labels) => outlineLabels=labels;
        public void ShowHit(Vector2 start,float amount,bool critical)
        {
            Show(start,amount.ToString("0")+(critical?"!!":""),critical?new Color32(230,155,80,255):Color.white);
            if(outlineLabels!=null)foreach(var outline in outlineLabels)
            {
                if(outline==null)continue;
                outline.gameObject.SetActive(true);outline.text=label.text;outline.color=Color.black;
                outline.rectTransform.sizeDelta=label.rectTransform.sizeDelta;
            }
            bouncing=critical;position=start;floor=start.y-96;bounces=0;
            velocity=new Vector2((++direction%2==0?1:-1)*criticalLaunch.x,criticalLaunch.y);
        }

        private void Update()
        {
            var elapsed = Time.unscaledTime - spawnedAt;
            var duration=bouncing?criticalLifetime:lifetime;
            if (root != null)
            {
                if(bouncing)
                {
                    var dt=Mathf.Min(Time.unscaledDeltaTime,.04f);
                    position+=velocity*dt;velocity.y-=gravity*dt;
                    if(position.y<floor)
                    {
                        position.y=floor;
                        if(bounces<2){velocity.y=bounces==0?220:100;bounces++;velocity.x*=.65f;}
                        else velocity=Vector2.zero;
                    }
                    root.anchoredPosition=new Vector2(Mathf.Round(position.x/4)*4,Mathf.Round(position.y/4)*4);
                    root.localScale=Vector3.one*Mathf.Lerp(1.25f,1,Mathf.Clamp01(elapsed/.1f));
                }
                else root.anchoredPosition += Vector2.up * riseSpeed * Time.unscaledDeltaTime;
            }
            if (label != null)
            {
                var color = baseColor;
                color.a *= 1f - Mathf.InverseLerp(duration*.7f,duration,elapsed);
                label.color = color;
                if(outlineLabels!=null)foreach(var outline in outlineLabels)if(outline!=null)outline.color=new Color(0,0,0,color.a);
            }
            if (elapsed >= duration) Destroy(gameObject);
        }
    }
}
