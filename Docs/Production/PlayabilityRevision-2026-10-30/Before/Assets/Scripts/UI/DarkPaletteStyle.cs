using UnityEngine;

namespace DungeonSlash
{
    /// <summary>One seven-ink art direction shared by artwork, UI and the final text composite.</summary>
    [CreateAssetMenu(menuName="Dungeon Slash/Presentation/Dark Palette")]
    public sealed class DarkPaletteStyle : ScriptableObject
    {
        public Color ink = Color.black;
        public Color shadow = new Color32(25,21,31,255);
        public Color stone = new Color32(51,44,59,255);
        public Color ash = new Color32(100,88,107,255);
        public Color silver = new Color32(163,152,170,255);
        public Color bone = new Color32(238,229,213,255);
        public Color accent = new Color32(239,101,72,255);
        [Range(.1f,1)] public float backgroundValue = .58f;
        [Range(.5f,2)] public float figureValue = 1.12f;
        [Tooltip("Source luminance boundaries for the five darker inks. No interpolated colors.")]
        public Vector4 valueSteps = new Vector4(.075f,.19f,.36f,.57f);
        [Range(.6f,1)] public float highlightStep = .81f;

        public Color[] Colors => new[]{ink,shadow,stone,ash,silver,bone,accent};
        public void Apply()
        {
            var colors=Colors;
            // Vectors deliberately contain sRGB values: conversion happens exactly once in the shaders.
            for(var i=0;i<colors.Length;i++)Shader.SetGlobalVector("_BehemothInk"+i,(Vector4)colors[i]);
            Shader.SetGlobalVector("_BehemothValueSteps",valueSteps);
            Shader.SetGlobalVector("_BehemothValues",new Vector4(backgroundValue,figureValue,highlightStep,0));
        }
    }
}
