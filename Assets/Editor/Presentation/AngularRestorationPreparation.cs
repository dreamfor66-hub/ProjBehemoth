using System;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Local art correction. Saved sources and placement remain inspectable before Play.
[InitializeOnLoad]
internal static class AngularRestorationPreparation
{
    const string Root="Assets/Art/World/Angular/Restoration01/";
    const string Stamp="Assets/Data/Presentation/Angular/RestorationInstalled.txt";
    const string Revision="original-composition-3";
    const string Report="Docs/Production/AngularRestore-2026-10-26/";
    static double next;static bool busy;
    static AngularRestorationPreparation()=>EditorApplication.update+=Poll;
    static void Poll()
    {
        if(busy||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.timeSinceStartup<next)return;
        next=EditorApplication.timeSinceStartup+4;
        if(File.Exists(Stamp)&&File.ReadAllText(Stamp)==Revision)return;
        if(new[]{"B1-Walls","B1-Forward","B1-Left","B1-Right"}.Any(n=>!File.Exists(Root+n+".png")))return;
        busy=true;
        try{Prepare();File.WriteAllText(Stamp,Revision);AssetDatabase.ImportAsset(Stamp);}
        catch(Exception e){File.WriteAllText(Report+"Preparation-Error.txt",e.ToString());Debug.LogException(e);next=EditorApplication.timeSinceStartup+60;}
        finally{busy=false;}
    }
    static void Import(string path)
    {
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.filterMode=FilterMode.Bilinear;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.npotScale=TextureImporterNPOTScale.None;
        var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
    }
    internal static void ApplySavedComposition()
    {
        // Read the exact accepted trial's padding compensation, rather than guessing a new zoom.
        var baseline=AssetDatabase.LoadAssetAtPath<CombatSpriteAnimation>("Assets/Data/Presentation/Trials/Angular01_WarriorAnimation.asset");
        foreach(var name in Enum.GetNames(typeof(CharacterKind)))
        {
            var animation=AssetDatabase.LoadAssetAtPath<CombatSpriteAnimation>("Assets/Data/Presentation/Angular/Animations/Hero-"+name+".asset");
            if(animation==null)continue;
            animation.canvasScale=baseline.canvasScale;EditorUtility.SetDirty(animation);
        }
        var floor=AssetDatabase.LoadAssetAtPath<FloorArtwork>("Assets/Data/Presentation/Angular/B1.asset");
        floor.walls=DungeonSlash.DesignerAssetPaths.Load<Sprite>(Root+"B1-Walls.png");
        floor.openings=new[]{"Forward","Left","Right"}.Select(n=>DungeonSlash.DesignerAssetPaths.Load<Sprite>(Root+"B1-"+n+".png")).ToArray();
        floor.imageOffset=new Vector2(0,-24);EditorUtility.SetDirty(floor);
        var accepted=AssetDatabase.LoadAssetAtPath<ArcadePresentation>("Assets/Data/Presentation/Trials/Angular01_Profile.asset");
        const string palettePath="Assets/Data/Presentation/Angular/RestoredPalette.asset";
        var palette=AssetDatabase.LoadAssetAtPath<DarkPaletteStyle>(palettePath);
        if(palette==null){palette=Object.Instantiate(accepted.darkPalette);AssetDatabase.CreateAsset(palette,palettePath);}
        var profile=AssetDatabase.LoadAssetAtPath<ArcadePresentation>("Assets/Data/Presentation/Angular/WorldArtwork.asset");
        profile.darkPalette=palette;EditorUtility.SetDirty(profile);
    }
    static void Prepare()
    {
        foreach(var name in new[]{"Walls","Forward","Left","Right","AllOpen","LeftClosed"})Import(Root+"B1-"+name+".png");
        Import("Assets/Art/World/Angular/Heroes/Warrior-Rear.png");Import("Assets/Art/Concepts/BarbarianBearPelt01.png");
        ApplySavedComposition();
        const string path="Assets/Scenes/MainGameScene.unity";
        var scene=SceneManager.GetSceneByPath(path);var loaded=scene.isLoaded;
        if(loaded&&scene.isDirty)throw new InvalidOperationException("Unsaved designer scene: "+path);
        if(!loaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            var roots=scene.GetRootGameObjects();
            var backdrop=roots.SelectMany(r=>r.GetComponentsInChildren<StageBackdropView>(true)).Single();
            var floor=AssetDatabase.LoadAssetAtPath<FloorArtwork>("Assets/Data/Presentation/Angular/B1.asset");
            backdrop.artwork[0]=floor;backdrop.stages[0]=floor.walls;
            backdrop.background.sprite=floor.walls;
            for(var i=0;i<3;i++){backdrop.doorLayers[i].sprite=floor.openings[i];backdrop.doorLayers[i].enabled=false;}
            var navigation=roots.SelectMany(r=>r.GetComponentsInChildren<NavigationChoiceView>(true)).Single();
            navigation.artworkPositions=new[]{new Vector2(0,124),new Vector2(-440,-36),new Vector2(440,-36),new Vector2(-530,-265)};
            // Recover only the accepted navigation header's quiet olive backing.
            var header=roots.SelectMany(r=>r.GetComponentsInChildren<Image>(true)).FirstOrDefault(i=>i.name=="LeftPanel");
            if(header!=null)header.color=new Color32(35,39,32,255);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        finally{if(!loaded)EditorSceneManager.CloseScene(scene,true);}
        AssetDatabase.SaveAssets();File.WriteAllText(Report+"Installed.txt","Original SourceHall camera/pixels outside two doorway panels restored. Hero canvas scale read from accepted trial; background Y=-24 logical units. Original Back navigation lower-left. Bear-pelt concept saved separately; existing character identity data retained.\n");
    }
}
