using System;
using System.IO;
using DungeonSlash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Saved art roles, prepared once without a menu step. Dirty designer scenes are never saved.
[InitializeOnLoad]
internal static class DarkArtPreparation
{
    const string Profile="Assets/Data/Presentation/Arcade224.asset";
    const string FigureMaterial="Assets/Art/Materials/InkFigure.mat";
    static DarkArtPreparation(){EditorApplication.delayCall+=Prepare;EditorApplication.update+=Poll;}
    static void Prepare()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)
        {EditorApplication.delayCall+=Prepare;return;}
        var profile=AssetDatabase.LoadAssetAtPath<ArcadePresentation>(Profile);
        if(profile==null||profile.darkPalette==null)return;
        profile.darkPalette.Apply();
        var material=AssetDatabase.LoadAssetAtPath<Material>(FigureMaterial);
        if(material==null)
        {
            var shader=Shader.Find("UI/Behemoth/InkFigure");if(shader==null)return;
            material=new Material(shader);AssetDatabase.CreateAsset(material,FigureMaterial);
        }
        foreach(var path in new[]{"Assets/Prefabs/Combat/MonsterView.prefab","Assets/Prefabs/Combat/PlayerView.prefab"})
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var component=(Component)asset.GetComponent<MonsterView>()??asset.GetComponent<PlayerView>();
            var body=new SerializedObject(component).FindProperty("body").objectReferenceValue as Image;
            if(body==null||body.material==material)continue;
            var prefab=PrefabUtility.LoadPrefabContents(path);
            try
            {
                component=(Component)prefab.GetComponent<MonsterView>()??prefab.GetComponent<PlayerView>();
                body=new SerializedObject(component).FindProperty("body").objectReferenceValue as Image;
                body.material=material;PrefabUtility.SaveAsPrefabAsset(prefab,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
        }
        var background=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environments/LowColorBackground.mat");
        foreach(var path in new[]{"Assets/Scenes/OutGameScene.unity","Assets/Scenes/MainGameScene.unity"})
        {
            var scene=SceneManager.GetSceneByPath(path);var loaded=scene.isLoaded;
            if(loaded&&scene.isDirty)continue;
            if(!loaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var changed=false;
                foreach(var root in scene.GetRootGameObjects())
                {
                    foreach(var image in root.GetComponentsInChildren<Image>(true))
                    {
                        var spritePath=image.sprite!=null?AssetDatabase.GetAssetPath(image.sprite.texture):"";
                        var chosen=spritePath.StartsWith("Assets/Art/Environments/")?background:
                            spritePath.StartsWith("Assets/Art/Characters/")&&!spritePath.Contains("/Selection/")?material:null;
                        if(chosen!=null&&image.material!=chosen){image.material=chosen;changed=true;}
                    }
                    foreach(var raster in root.GetComponentsInChildren<ArcadeRasterCamera>(true))
                        if(!raster.quantizeColors){raster.quantizeColors=true;changed=true;}
                }
                if(changed)EditorSceneManager.SaveScene(scene);
            }
            finally{if(!loaded)EditorSceneManager.CloseScene(scene,true);}
        }
        AssetDatabase.SaveAssets();
    }
    static void Poll()
    {
        const string request="Temp/DarkArt.request";
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(request))return;
        string command;try{command=File.ReadAllText(request).Trim();File.Delete(request);}catch(IOException){return;}
        if(command=="prepare"&&!EditorApplication.isPlaying)Prepare();
        if(command=="capture"&&!EditorApplication.isPlaying){SceneView.RepaintAll();}
        if(command.StartsWith("live:")&&EditorApplication.isPlaying)
        {
            var probe=Object.FindFirstObjectByType<DarkArtLiveProbe>()??new GameObject("Scoped seven-ink art review").AddComponent<DarkArtLiveProbe>();
            probe.Run(command.Substring(5));
        }
    }
}
