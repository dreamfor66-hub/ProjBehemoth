using UnityEngine;

namespace DungeonSlash
{
    public enum InkSurfaceRole { Panel, Button, Choice, Slot, GaugeTrack, GaugeFill, Header, Divider, Field }
    public enum InkResource { Custom, Health, Mana, Guard, Gold }

    [CreateAssetMenu(menuName="Dungeon Slash/Presentation/Ink UI Style")]
    public sealed class InkUiStyle : ScriptableObject
    {
        public int appliedRevision;
        public Material material;
        [Header("Observed DD1 hierarchy: black fields, ash rails, selective brass")]
        public Color paper = new Color32(9, 9, 8, 255);
        public Color recess = new Color32(3, 3, 3, 255);
        public Color iron = new Color32(61, 59, 53, 255);
        public Color edge = new Color32(100, 96, 80, 255);
        public Color brass = new Color32(169, 143, 78, 255);
        public Color bone = new Color32(221, 208, 169, 255);
        public Color header = new Color32(22, 31, 32, 255);
        [Header("Gameplay resources")]
        public Color hp = new Color32(161, 24, 22, 255);
        public Color hpRidge = new Color32(226, 49, 34, 255);
        public Color mana = new Color32(62, 111, 121, 255);
        public Color guard = new Color32(191, 180, 148, 255);
        public Color brokenGuard = new Color32(115, 42, 31, 255);
        [Header("Editable linework")]
        [Range(.5f, 5)] public float railWidth = 2;
        [Range(2, 16)] public float railGap = 7;
        [Range(0, 3)] public float wear = 1;
        [Range(4, 24)] public int guardCells = 12;
        [Range(64, 140)] public float headerHeight = 100;
        public int RenderRevision { get; private set; }
        void OnValidate() { RenderRevision++; }
    }
}
