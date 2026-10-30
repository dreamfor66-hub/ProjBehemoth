using System;
using System.IO;
using DungeonSlash;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Prepares matching, inspectable target/burst prefabs automatically, without touching combat data.
[InitializeOnLoad]
internal static class WeakPointVisualPreparation
{
    internal const string StylePath="Assets/Data/Presentation/WeakPointStyle.asset";
    static double next;
    static WeakPointVisualPreparation()=>EditorApplication.update+=Poll;
    static void Poll()
    {
        if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+4;
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        var fx=AssetDatabase.LoadAssetAtPath<WeakPointBreakFxView>("Assets/Prefabs/UI/WeakPointBreakFxView.prefab");
        var target=AssetDatabase.LoadAssetAtPath<WeakPointView>("Assets/Prefabs/Combat/WeakPointView.prefab");
        var profile=AssetDatabase.LoadAssetAtPath<ArcadePresentation>("Assets/Data/Presentation/Arcade224.asset");
        if(fx==null||target==null||target.reticle==null)return;
        if(fx.shatter!=null&&target.reticle.style!=null&&target.reticle.style.material!=null&&profile!=null&&profile.weakPointStyle==target.reticle.style)return;
        try{Prepare();}catch(Exception e){next=double.MaxValue;Debug.LogException(e);}
    }
    internal static void Prepare()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var style=AssetDatabase.LoadAssetAtPath<WeakPointVisualStyle>(StylePath);
        if(style==null){style=ScriptableObject.CreateInstance<WeakPointVisualStyle>();AssetDatabase.CreateAsset(style,StylePath);}
        style.material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/InkUi.mat");EditorUtility.SetDirty(style);
        var presentation=AssetDatabase.LoadAssetAtPath<ArcadePresentation>("Assets/Data/Presentation/Arcade224.asset");
        presentation.weakPointStyle=style;EditorUtility.SetDirty(presentation);
        Edit("Assets/Prefabs/Combat/WeakPointView.prefab",prefab=>
        {
            var view=prefab.GetComponent<WeakPointView>();view.reticle.style=style;view.reticle.raycastTarget=false;view.reticle.SetAllDirty();
            var body=new SerializedObject(view).FindProperty("body").objectReferenceValue as Image;
            if(body!=null)body.enabled=false;
        });
        Edit("Assets/Prefabs/UI/WeakPointBreakFxView.prefab",prefab=>
        {
            // Disable the previous six gold rectangles. They remain recoverable for old authored tooling.
            foreach(var image in prefab.GetComponentsInChildren<Image>(true)){image.raycastTarget=false;image.gameObject.SetActive(false);}
            var child=prefab.transform.Find("ReticleShatter");
            if(child==null){var go=new GameObject("ReticleShatter",typeof(RectTransform),typeof(CanvasRenderer),typeof(WeakPointShatterGraphic));go.transform.SetParent(prefab.transform,false);child=go.transform;}
            var graphic=child.GetComponent<WeakPointShatterGraphic>();graphic.style=style;graphic.diameter=96;graphic.progress=0;graphic.raycastTarget=false;
            var rect=graphic.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;rect.sizeDelta=new Vector2(96,96);rect.anchoredPosition=Vector2.zero;
            prefab.GetComponent<WeakPointBreakFxView>().shatter=graphic;
        });
        AssetDatabase.SaveAssets();Directory.CreateDirectory("Logs");File.WriteAllText("Logs/WeakPointVisual-Preparation.txt","Shared crimson broken reticle, warm central core, no white band. Break graphic fragments the same silhouette and inherits the actual target style/size.\n");
    }
    static void Edit(string path,Action<GameObject> edit)
    {var prefab=PrefabUtility.LoadPrefabContents(path);try{edit(prefab);PrefabUtility.SaveAsPrefabAsset(prefab,path);}finally{PrefabUtility.UnloadPrefabContents(prefab);}}
}
