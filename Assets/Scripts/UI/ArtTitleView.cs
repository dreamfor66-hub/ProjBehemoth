using TMPro;
using UnityEngine;

namespace DungeonSlash
{
    [ExecuteAlways]
    public sealed class ArtTitleView:MonoBehaviour
    {
        public ArcadePresentation presentation;
        public GameObject illustration;
        public TextMeshProUGUI fallback;
        void OnEnable()=>Refresh();
        void Update()=>Refresh();
        public void Refresh()
        {
            var art=presentation!=null&&presentation.arcade;
            if(illustration!=null&&illustration.activeSelf!=art)illustration.SetActive(art);
            if(fallback!=null&&fallback.gameObject.activeSelf==art)fallback.gameObject.SetActive(!art);
        }
    }
}
