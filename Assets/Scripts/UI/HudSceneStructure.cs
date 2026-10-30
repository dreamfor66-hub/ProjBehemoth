using UnityEngine;
namespace DungeonSlash
{
    // Serialized ownership map for designers. Runtime systems keep their existing direct references.
    public sealed class HudSceneStructure : MonoBehaviour
    {
        [HideInInspector] public int revision;
        public RectTransform world,combat,drawers,messages,decisions,hud,itemOverlays,developerTools;
        public RectTransform playerStatusGauges,playerGuard,pointerFeedback,spellControls;
    }
}
