using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace DungeonSlash
{
    [DefaultExecutionOrder(-20000), RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class GameTextBinding : MonoBehaviour
    {
        public string key;
        private void Awake() => Refresh();
        public void Refresh() { if (!string.IsNullOrEmpty(key)) GetComponent<TextMeshProUGUI>().text = GameText.Get(key); }
    }
}
