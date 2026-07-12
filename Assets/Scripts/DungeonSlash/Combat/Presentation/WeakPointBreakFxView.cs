using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    /// <summary>Lightweight UI particle burst used when a weak point breaks.</summary>
    public sealed class WeakPointBreakFxView : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private List<Image> shards = new();
        [SerializeField, Min(.05f)] private float lifetime = .42f;
        [SerializeField] private float burstRadius = 78f;
        private float activeLifetime;

        public void Configure(RectTransform newRoot, IEnumerable<Image> newShards)
        {
            root = newRoot;
            shards = new List<Image>(newShards);
        }

        public void Play(Vector2 position, float minimumLifetime = 0f)
        {
            if (root != null) root.anchoredPosition = position;
            activeLifetime = Mathf.Max(lifetime, minimumLifetime);
            StartCoroutine(Burst());
        }

        private IEnumerator Burst()
        {
            var elapsed = 0f;
            while (elapsed < activeLifetime)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / activeLifetime);
                for (var index = 0; index < shards.Count; index++)
                {
                    var shard = shards[index];
                    if (shard == null) continue;
                    var angle = index * Mathf.PI * 2f / Mathf.Max(1, shards.Count) + .28f;
                    var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    shard.rectTransform.anchoredPosition = direction * (burstRadius * Mathf.SmoothStep(0f, 1f, progress));
                    shard.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg + progress * 220f);
                    var color = shard.color;
                    color.a = 1f - progress;
                    shard.color = color;
                }
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
