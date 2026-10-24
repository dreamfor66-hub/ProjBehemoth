using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

internal sealed class ArtReferenceArchive:ScriptableObject
{
    [Serializable]public sealed class Entry{public UnityEngine.Object asset;public string property;public UnityEngine.Object geometry,arcade;}
    public List<Entry> entries=new();
    public TMP_FontAsset geometryFont;
    [Serializable]public sealed class Tint{public UnityEngine.Object asset;public string property;public Color geometry,arcade;}
    public List<Tint> colors=new();
}
