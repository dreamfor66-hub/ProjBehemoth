using UnityEngine;
namespace DungeonSlash
{
    /// <summary>Shared painted target palette and shatter direction.</summary>
    [CreateAssetMenu(menuName = "Dungeon Slash/Presentation/Weak Point Style")]
    public sealed class WeakPointVisualStyle : ScriptableObject
    {
        public Material material;
        public Color stroke = new Color32(239,65,36,255);
        public Color core = new Color32(255,241,188,255);
        public Color outline = Color.black;
        [Range(.75f,2)] public float ringThickness = 1.5f;
        [Range(6,24)] public float arcGap = 14;
        [Range(.5f,2)] public float coreRadius = 1.25f;
        [Min(.1f)] public float breakLifetime = .52f;
        [Min(1)] public float breakDistance = 90;
        static WeakPointVisualStyle fallback;
        public static WeakPointVisualStyle Default => fallback != null ? fallback : fallback = CreateFallback();
        static WeakPointVisualStyle CreateFallback()
        { var style=CreateInstance<WeakPointVisualStyle>();style.hideFlags=HideFlags.HideAndDontSave;return style; }
    }
}
