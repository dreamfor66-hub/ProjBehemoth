using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class WeakPointView : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private Image body;
        [SerializeField] private Text remainingHits;

        public void Configure(RectTransform newRoot, Image newBody, Text newRemainingHits)
        {
            root = newRoot;
            body = newBody;
            remainingHits = newRemainingHits;
        }

        public void Bind(WeakPointRuntime runtime)
        {
            if (runtime == null) return;
            if (root != null) root.anchoredPosition = runtime.HitShape.Center;
            if (body != null)
            {
                body.enabled = runtime.IsActive && !runtime.IsDestroyed;
                body.color = Color.white;
            }
            if (remainingHits != null) remainingHits.enabled = false;
        }
    }
}
