using TMPro;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    /// <summary>Modal presentation for non-combat room events. It owns the image, text, and optional choice buttons.</summary>
    public sealed class RoomEventView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Image illustration;
        [SerializeField] private TextMeshProUGUI message;
        [SerializeField] private Button yesButton;
        [SerializeField] private TextMeshProUGUI yesLabel;
        [SerializeField] private Button noButton;
        [SerializeField] private TextMeshProUGUI noLabel;

        public void Configure(GameObject newPanel, Image newIllustration, TextMeshProUGUI newMessage, Button newYesButton, TextMeshProUGUI newYesLabel, Button newNoButton, TextMeshProUGUI newNoLabel)
        {
            panel = newPanel;
            illustration = newIllustration;
            message = newMessage;
            yesButton = newYesButton;
            yesLabel = newYesLabel;
            noButton = newNoButton;
            noLabel = newNoLabel;
        }

        public IEnumerator ShowTimed(string text, Sprite sprite)
        {
            ShowBase(text, sprite);
            var confirmed=false;
            ConfigureButton(yesButton,yesLabel,GameText.Get("ui.common.confirm"),()=>confirmed=true);
            while(!confirmed && panel.activeSelf)yield return null;
            Hide();
        }

        public void ShowChoice(string text, Sprite sprite, string yes, string no, Action accepted, Action declined)
        {
            ShowBase(text, sprite);
            ConfigureButton(yesButton, yesLabel, yes, () => { Hide(); accepted?.Invoke(); });
            ConfigureButton(noButton, noLabel, no, () => { Hide(); declined?.Invoke(); });
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void ShowBase(string text, Sprite sprite)
        {
            if (panel == null) return;
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            if (message != null) message.text = text;
            if (illustration != null)
            {
                illustration.sprite = sprite;
                illustration.color = Color.white;
                illustration.enabled = sprite != null;
                illustration.preserveAspect = true;
            }
            SetButtonVisible(yesButton, false);
            SetButtonVisible(noButton, false);
        }

        private static void ConfigureButton(Button button, TextMeshProUGUI label, string text, Action clicked)
        {
            if (button == null) return;
            button.gameObject.SetActive(true);
            if (label != null) label.text = text;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => clicked());
        }

        private static void SetButtonVisible(Button button, bool visible)
        {
            if (button != null) button.gameObject.SetActive(visible);
        }
    }
}
