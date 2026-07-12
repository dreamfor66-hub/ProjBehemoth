using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class EncounterMessageView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text message;
        [SerializeField, Min(.1f)] private float displaySeconds = 1.15f;

        public void Configure(GameObject newPanel, Text newMessage)
        {
            panel = newPanel;
            message = newMessage;
        }

        public IEnumerator Show(string text)
        {
            if (panel == null) yield break;
            panel.SetActive(true);
            if (message != null) message.text = text;
            yield return new WaitForSecondsRealtime(displaySeconds);
            panel.SetActive(false);
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }
    }
}
