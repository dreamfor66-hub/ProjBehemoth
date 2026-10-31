using System;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

[InitializeOnLoad]
internal static class IllustratedEventPreparation
{
    internal const string Folder="Docs/Production/EventIllustrations-2026-10-29/";
    static double next;
    static IllustratedEventPreparation()
    {
        next=EditorApplication.timeSinceStartup+5;
        EditorApplication.playModeStateChanged+=_=>next=EditorApplication.timeSinceStartup+5;
        EditorApplication.update+=Poll;
    }
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.timeSinceStartup<next)return;
        next=EditorApplication.timeSinceStartup+4;
        if(File.Exists(Folder+"Installed.txt"))return;
        if(!File.Exists("Assets/Art/UI/Events/T_UI_Event_Mirror.png")||!File.Exists("Assets/Art/UI/Events/T_UI_Event_Pilgrim.png"))return;
        try { Prepare();File.WriteAllText(Folder+"Installed.txt","portrait-events-1\n"); }
        catch(Exception e){File.WriteAllText(Folder+"PreparationError.txt",e.ToString());next=EditorApplication.timeSinceStartup+60;Debug.LogException(e);}
    }
    static void Prepare()
    {
        const string path="Assets/Scenes/MainGameScene.unity";
        var scene=SceneManager.GetSceneByPath(path);var loaded=scene.isLoaded;
        if(loaded&&scene.isDirty)throw new InvalidOperationException("Unsaved MainGameScene edits preserved; preparation pending.");
        foreach(var id in new[]{"Mirror","Pilgrim"})
        {
            var art="Assets/Art/UI/Events/"+id+".png";
            AssetDatabase.ImportAsset(art,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(art);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.filterMode=FilterMode.Bilinear;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;
            importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
            var story=DungeonSlash.DesignerAssetPaths.Load<RandomEventData>("Assets/Data/Dungeon/Events/"+id+".asset");
            story.illustration=DungeonSlash.DesignerAssetPaths.Load<Sprite>(art);story.illustratedPopup=true;
            foreach(var page in story.pages)page.illustration=null;
            EditorUtility.SetDirty(story);
        }
        if(!loaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            var view=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RunExpansionView>(true)).Single();
            view.portraitChoices=new ExpansionChoiceButton[4];
            for(var i=0;i<4;i++)
            {
                var source=view.choices[0];var old=source.button.transform.parent.Find("Illustrated Event Choice "+i);
                var go=old!=null?old.gameObject:Object.Instantiate(source.button.gameObject,source.button.transform.parent);
                go.name="Illustrated Event Choice "+i;
                var original=source.button.GetComponentsInChildren<Transform>(true);var copied=go.GetComponentsInChildren<Transform>(true);
                T Corresponding<T>(T component)where T:Component=>copied[Array.IndexOf(original,component.transform)].GetComponent<T>();
                view.portraitChoices[i]=new ExpansionChoiceButton{button=Corresponding(source.button),title=Corresponding(source.title),description=Corresponding(source.description),icon=Corresponding(source.icon)};
                var rect=(RectTransform)go.transform;rect.anchoredPosition=new Vector2(300,-120-i*100);rect.sizeDelta=new Vector2(980,88);
                go.SetActive(false);
            }
            view.eventIllustrationAspect=.75f;EditorUtility.SetDirty(view);EditorSceneManager.SaveScene(scene);
        }
        finally {if(!loaded)EditorSceneManager.CloseScene(scene,true);}
        AssetDatabase.SaveAssets();
    }
}
