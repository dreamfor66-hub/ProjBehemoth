using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DungeonSlash
{
    public enum TextRole { Base, Title, Heading, Narrative, Description, Button, HudValue, CompactValue, MicroValue, CombatNumber, Hint }
    public enum Typeface { Base, DosStory, Galmuri11, Galmuri11Condensed, Galmuri9, Galmuri7, Tangba, YiSunShin }

    [Serializable]
    public sealed class TypographyRule
    {
        public TextRole role;
        public Typeface face;
        public Typeface narrowFace=Typeface.Base;
        public bool useNarrowFaceWhenNeeded;
        public bool numericOnly;
        [Range(.5f,2)] public float sizeMultiplier=1;
    }

    [CreateAssetMenu(menuName="Dungeon Slash/Typography Profile")]
    public sealed class TypographyProfile : ScriptableObject
    {
        [Tooltip("Changes all roles assigned to Base together. Explicit role assignments can override it.")]
        public Typeface baseFace=Typeface.YiSunShin;
        public TMP_FontAsset dosStory,galmuri11,galmuri11Condensed,galmuri9,galmuri7;
        [Header("Temporary font trial")]
        [Tooltip("Turn off to restore Galmuri11 Condensed everywhere assigned to that face, without changing text roles.")]
        public bool useTangbaForCondensed=true;
        public TMP_FontAsset tangba;
        [Header("Smooth typeface")]
        public TMP_FontAsset yiSunShin;
        public bool snapToNativePixels=true;
        public List<TypographyRule> rules=new();

        public TypographyRule Rule(TextRole role)
        {
            foreach(var rule in rules)if(rule.role==role)return rule;
            return null;
        }
        public TMP_FontAsset Font(Typeface face)
        {
            if(face==Typeface.Base)face=baseFace==Typeface.Base?Typeface.DosStory:baseFace;
            return face switch
            {
                Typeface.DosStory=>dosStory,
                Typeface.Galmuri11=>galmuri11,
                Typeface.Galmuri11Condensed=>useTangbaForCondensed&&tangba!=null?tangba:galmuri11Condensed,
                Typeface.Galmuri9=>galmuri9,
                Typeface.Galmuri7=>galmuri7,
                Typeface.Tangba=>tangba,
                Typeface.YiSunShin=>yiSunShin,
                _=>dosStory
            };
        }
    }
}
