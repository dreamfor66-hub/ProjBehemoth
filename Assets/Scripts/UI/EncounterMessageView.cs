using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class EncounterMessageView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI message;
        [SerializeField, Min(.1f)] private float displaySeconds = 1.15f;

        public void Configure(GameObject newPanel, TextMeshProUGUI newMessage)
        {
            panel = newPanel;
            message = newMessage;
        }

        public IEnumerator Show(string text, System.Func<bool> presentationReady = null)
        {
            if (panel == null) yield break;
            panel.SetActive(true);
            if (message != null) message.text = text;
            yield return new WaitForSecondsRealtime(displaySeconds);
            // Keep the announcement visible if its configured duration is shorter than entry framing.
            while (presentationReady != null && !presentationReady()) yield return null;
            panel.SetActive(false);
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }
    }
}
