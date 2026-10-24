using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    [RequireComponent(typeof(Image))]
    public sealed class CombatSpriteAnimator : MonoBehaviour
    {
        public ArcadePresentation presentation;
        public CombatSpriteAnimation library;
        public CombatVisualState State {get;private set;}
        private Image target;
        public bool Available=>presentation!=null&&presentation.arcade&&library!=null;
        private void Awake()=>target=GetComponent<Image>();
        public void Play(CombatSpriteAnimation source,CombatVisualState state,bool restart=false)
        {
            library=source;
            if(!Available)return;
            State=state;
            target=target!=null?target:GetComponent<Image>();
            target.sprite=library.For(state)??library.idle;
        }
    }
}
