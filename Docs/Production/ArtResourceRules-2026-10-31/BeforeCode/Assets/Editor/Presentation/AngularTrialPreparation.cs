using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonSlash;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// A scoped, saved two-screen art trial. Rebuild is automatic when its own source art changes.
[InitializeOnLoad]
internal static class AngularTrialPreparation
{
    const string Art="Assets/Art/Trials/Angular01/",Data="Assets/Data/Presentation/Trials/",Scenes="Assets/Scenes/Trials/";
    static readonly string[] Names={"Warrior","Rogue","Barbarian","Samurai","Monk","Wizard","Hall","Rear"};
    static double next;static bool busy;
    static PlayerCombatData previousSelection;static bool trialPlay;
    static Color32 C(string s){ColorUtility.TryParseHtmlString("#"+s,out var c);return c;}
    static AngularTrialPreparation()
    {
        EditorApplication.update+=Poll;
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.ExitingEditMode)
            {
                trialPlay=SceneManager.GetActiveScene().path.StartsWith(Scenes+"Angular01_");
                if(trialPlay)previousSelection=RunSession.SelectedCharacter;
            }
            if(state==PlayModeStateChange.EnteredEditMode&&trialPlay){RunSession.SelectedCharacter=previousSelection;trialPlay=false;}
        };
    }
    static void Poll()
    {
        if(busy||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        const string request="Temp/AngularTrial.request";
        if(File.Exists(request))
        {
            var command=File.ReadAllText(request).Trim();File.Delete(request);
            if(command=="selection"&&!EditorApplication.isPlaying)EditorSceneManager.OpenScene(Scenes+"Angular01_Characters.unity");
            if(command=="main"&&!EditorApplication.isPlaying)EditorSceneManager.OpenScene(Scenes+"Angular01_Main.unity");
            if(command=="review"&&EditorApplication.isPlaying)Object.FindFirstObjectByType<AngularArtTrial>()?.Review();
            if(command=="start"&&EditorApplication.isPlaying)Object.FindFirstObjectByType<OutGameMenuView>()?.selection.startButton.onClick.Invoke();
            if(command=="continue"&&EditorApplication.isPlaying)Object.FindFirstObjectByType<AngularArtTrial>()?.ContinueFirst();
            if(command.StartsWith("capture:")&&EditorApplication.isPlaying)Object.FindFirstObjectByType<AngularArtTrial>()?.Capture(command.Substring(8));
        }
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.timeSinceStartup<next)return;
        next=EditorApplication.timeSinceStartup+3;
        if(Names.Any(n=>!File.Exists(Art+"Source"+n+".png")))return;
        var signature="angular-trial-5/"+string.Join("/",Names.Select(n=>File.GetLastWriteTimeUtc(Art+"Source"+n+".png").Ticks));
        var stamp="Temp/AngularTrial.signature";
        if(File.Exists(stamp)&&File.ReadAllText(stamp)==signature)return;
        busy=true;
        try{Prepare();File.WriteAllText(stamp,signature);}
        catch(Exception e){Debug.LogException(e);}
        finally{busy=false;}
    }
    static void Prepare()
    {
        Directory.CreateDirectory(Art);Directory.CreateDirectory(Data);Directory.CreateDirectory(Scenes);
        foreach(var n in Names)Bake(n);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach(var n in Names)Import(Art+n+".png");
        var selections=new Dictionary<CharacterKind,Sprite>();var portraits=new Dictionary<CharacterKind,Sprite>();
        var boxes=new[]{new RectInt(300,20,490,530),new RectInt(340,0,530,520),new RectInt(300,20,450,490),new RectInt(250,0,520,540),new RectInt(330,0,450,440),new RectInt(240,0,550,600)};
        for(var i=0;i<6;i++)
        {
            var n=Names[i];selections[(CharacterKind)i]=AssetDatabase.LoadAssetAtPath<Sprite>(Art+n+".png");
            CropPortrait(n,boxes[i]);Import(Art+n+"Portrait.png");portraits[(CharacterKind)i]=AssetDatabase.LoadAssetAtPath<Sprite>(Art+n+"Portrait.png");
        }
        var originalContent=AssetDatabase.FindAssets("t:GameContent").Select(AssetDatabase.GUIDToAssetPath).Where(p=>!p.Contains("/Trials/")).Select(DungeonSlash.DesignerAssetPaths.Load<GameContent>).First();
        var content=Clone(originalContent,Data+"Angular01_Content.asset");
        content.characters=new List<PlayerCombatData>();
        var rear=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"Rear.png");
        foreach(var original in originalContent.characters)
        {
            var hero=Clone(original,Data+"Angular01_"+original.character+".asset");
            hero.portrait=portraits[hero.character];hero.selectionIllustration=selections[hero.character];hero.selectionMaterial=null;
            if(hero.character==CharacterKind.Warrior)
            {
                hero.combatSprite=rear;
                var animation=Clone(original.combatAnimation,Data+"Angular01_WarriorAnimation.asset");
                animation.idle=rear;animation.attack=rear;animation.guard=rear;
                EditorUtility.SetDirty(animation);hero.combatAnimation=animation;
            }
            content.characters.Add(hero);EditorUtility.SetDirty(hero);
        }
        EditorUtility.SetDirty(content);
        var profile=Clone(AssetDatabase.LoadAssetAtPath<ArcadePresentation>("Assets/Data/Presentation/Arcade224.asset"),Data+"Angular01_Profile.asset");
        // The new reference is line art. Keep the trial at native presentation size
        // so the user can judge shape language without a coarse framebuffer rewrite.
        profile.width=1920;profile.height=1080;
        var palette=Clone(profile.darkPalette,Data+"Angular01_Palette.asset");
        palette.ink=C("151715");palette.shadow=C("272922");palette.stone=C("3c4035");palette.ash=C("5b604b");palette.silver=C("aaa991");palette.bone=C("e4dbc5");palette.accent=C("c3a15d");
        profile.darkPalette=palette;EditorUtility.SetDirty(profile);EditorUtility.SetDirty(palette);
        const string playerPath="Assets/Prefabs/Trials/Angular01_Player.prefab";
        var prefab=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Combat/PlayerView.prefab");
        try
        {
            foreach(var image in prefab.GetComponentsInChildren<Image>(true))image.material=null;
            prefab.GetComponent<PlayerView>().presentation=profile;
            PrefabUtility.SaveAsPrefabAsset(prefab,playerPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        var faces=ScriptableObject.CreateInstance<FatigueFaces>();faces.heroes=content.characters.Select(c=>new FatigueFaces.Hero{character=c.character,expressions=Enumerable.Repeat(c.portrait,4).ToArray()}).ToArray();
        var faceAsset=AssetDatabase.LoadAssetAtPath<FatigueFaces>(Data+"Angular01_Faces.asset");
        if(faceAsset==null){AssetDatabase.CreateAsset(faces,Data+"Angular01_Faces.asset");faceAsset=faces;}else{EditorUtility.CopySerialized(faces,faceAsset);Object.DestroyImmediate(faces);EditorUtility.SetDirty(faceAsset);}
        AssetDatabase.SaveAssets();
        BuildScene("OutGameScene","Angular01_Characters",content,profile,faceAsset,true);
        BuildScene("MainGameScene","Angular01_Main",content,profile,faceAsset,false);
        Directory.CreateDirectory(AngularArtTrial.Report);
        File.WriteAllText(AngularArtTrial.Report+"Prepared.txt","Two saved isolated trial scenes. Six selection cutouts, six first-state portraits, one hall, one Warrior rear idle. Original scenes, character assets, tuning and global render profile untouched. Attack/guard/fatigue expansion deliberately pending art-direction approval.\n");
    }
    static T Clone<T>(T original,string path)where T:ScriptableObject
    {
        var asset=AssetDatabase.LoadAssetAtPath<T>(path);
        if(asset==null){asset=Object.Instantiate(original);asset.name=Path.GetFileNameWithoutExtension(path);AssetDatabase.CreateAsset(asset,path);}
        else{EditorUtility.CopySerialized(original,asset);EditorUtility.SetDirty(asset);}
        return asset;
    }
    static void Bake(string name)
    {
        var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.LoadImage(File.ReadAllBytes(Art+"Source"+name+".png"));
        var input=texture.GetPixels32();
        // Preserve the illustrator's coherent color planes in this style trial.
        // Threshold only the cutout alpha; global palette projection caused false
        // speckled material boundaries in the rejected direction.
        for(var i=0;i<input.Length;i++)
        {
            var p=input[i];input[i]=p.a<220?new Color32(0,0,0,0):new Color32(p.r,p.g,p.b,255);
        }
        texture.SetPixels32(input);texture.Apply();Write(Art+name+".png",texture.EncodeToPNG());Object.DestroyImmediate(texture);
    }
    static void CropPortrait(string n,RectInt box)
    {
        var source=new Texture2D(2,2,TextureFormat.RGBA32,false);source.LoadImage(File.ReadAllBytes(Art+n+".png"));
        var result=new Texture2D(box.width,box.height,TextureFormat.RGBA32,false);
        result.SetPixels(source.GetPixels(box.x,source.height-box.y-box.height,box.width,box.height));result.Apply();Write(Art+n+"Portrait.png",result.EncodeToPNG());Object.DestroyImmediate(source);Object.DestroyImmediate(result);
    }
    static void Write(string path,byte[] bytes){var temp=path+".trialtmp";File.WriteAllBytes(temp,bytes);if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}
    static void Import(string path)
    {
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=100;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();
    }
    static void BuildScene(string sourceName,string trialName,GameContent content,ArcadePresentation profile,FatigueFaces faces,bool selection)
    {
        var target=Scenes+trialName+".unity";
        if(!File.Exists(target))AssetDatabase.CopyAsset("Assets/Scenes/"+sourceName+".unity",target);
        var scene=SceneManager.GetSceneByPath(target);var wasLoaded=scene.isLoaded;
        if(wasLoaded&&scene.isDirty)throw new InvalidOperationException("Trial scene contains unsaved designer edits: "+target);
        if(!wasLoaded)scene=EditorSceneManager.OpenScene(target,OpenSceneMode.Additive);
        var roots=scene.GetRootGameObjects();
        foreach(var component in roots.SelectMany(r=>r.GetComponentsInChildren<MonoBehaviour>(true)))
        {
            if(component==null)continue;
            var so=new SerializedObject(component);var p=so.GetIterator();
            while(p.Next(true))if(p.propertyType==SerializedPropertyType.ObjectReference)
            {
                if(p.objectReferenceValue is GameContent)p.objectReferenceValue=content;
                else if(p.objectReferenceValue is ArcadePresentation)p.objectReferenceValue=profile;
                else if(p.objectReferenceValue is FatigueFaces)p.objectReferenceValue=faces;
                else if(p.objectReferenceValue is PlayerCombatData hero)p.objectReferenceValue=content.characters.First(c=>c.character==hero.character);
                else if(p.objectReferenceValue is PlayerView&&AssetDatabase.Contains(p.objectReferenceValue))p.objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Trials/Angular01_Player.prefab").GetComponent<PlayerView>();
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach(var image in roots.SelectMany(r=>r.GetComponentsInChildren<Image>(true)))
        {
            image.material=null;
            var c=image.color;
            if(c.a>.95f&&c.r<.3f&&c.g<.3f&&c.b<.4f)image.color=new Color32(35,39,32,255);
        }
        foreach(var raster in roots.SelectMany(r=>r.GetComponentsInChildren<ArcadeRasterCamera>(true)))raster.quantizeColors=false;
        foreach(var button in roots.SelectMany(r=>r.GetComponentsInChildren<Button>(true)))
        {
            var colors=button.colors;colors.normalColor=C("272922");colors.highlightedColor=C("5b604b");colors.pressedColor=C("82846b");colors.selectedColor=C("3c4035");button.colors=colors;
        }
        foreach(var body in roots.SelectMany(r=>r.GetComponentsInChildren<StageBackdropView>(true)))
        {body.stages=new[]{AssetDatabase.LoadAssetAtPath<Sprite>(Art+"Hall.png")};body.background.sprite=body.stages[0];body.background.color=Color.white;}
        var trial=roots.SelectMany(r=>r.GetComponentsInChildren<AngularArtTrial>(true)).FirstOrDefault();
        if(trial==null){var go=new GameObject("Angular art direction — first-screen trial");SceneManager.MoveGameObjectToScene(go,scene);trial=go.AddComponent<AngularArtTrial>();}
        trial.content=content;trial.characterPage=selection;
        var nav=roots.SelectMany(r=>r.GetComponentsInChildren<NavigationChoiceView>(true)).FirstOrDefault();
        if(nav!=null)
        {
            var choices=new SerializedObject(nav).FindProperty("choices");trial.navigation=new RoomChoiceButton[choices.arraySize];
            for(var i=0;i<choices.arraySize;i++)trial.navigation[i]=choices.GetArrayElementAtIndex(i).objectReferenceValue as RoomChoiceButton;
            for(var i=0;i<trial.navigation.Length;i++)if(trial.navigation[i]!=null)((RectTransform)trial.navigation[i].transform).anchoredPosition=trial.navigationPositions[i];
        }
        if(selection)
        {
            var menu=roots.SelectMany(r=>r.GetComponentsInChildren<OutGameMenuView>(true)).First();var view=menu.selection;
            // Selection colors are authored by OutGameMenuView.RefreshSelection.
            foreach(var button in view.characterButtons)button.transition=Selectable.Transition.None;
            // The shared title backdrop sits outside the page root. Hide it only
            // in this isolated character-page scene, where the new figures need
            // quiet negative space instead of the unrelated old pixel painting.
            foreach(var image in roots.SelectMany(r=>r.GetComponentsInChildren<Image>(true)))
                if(!image.transform.IsChildOf(menu.characterPage.transform))image.enabled=false;
            foreach(var camera in roots.SelectMany(r=>r.GetComponentsInChildren<Camera>(true)))camera.backgroundColor=C("151715");
            var art=view.selectionSplash.rectTransform;
            art.anchorMin=new Vector2(.54f,.19f);art.anchorMax=new Vector2(.98f,.98f);art.offsetMin=art.offsetMax=Vector2.zero;
            view.selectionSplash.preserveAspect=true;view.selectionSplash.sprite=content.characters[0].selectionIllustration;view.selectionSplash.material=null;
            var bg=menu.characterPage.GetComponent<Image>();if(bg==null)bg=menu.characterPage.AddComponent<Image>();bg.color=C("151715");bg.raycastTarget=false;
            menu.titlePage.SetActive(false);menu.optionsPage.SetActive(false);menu.characterPage.SetActive(true);
            var pedestal=menu.characterPage.transform.Find("Trial figure panel");
            if(pedestal==null)
            {
                var go=new GameObject("Trial figure panel",typeof(RectTransform),typeof(Image));go.transform.SetParent(menu.characterPage.transform,false);go.transform.SetAsFirstSibling();
                var r=(RectTransform)go.transform;r.anchorMin=new Vector2(.54f,.17f);r.anchorMax=new Vector2(1,1);r.offsetMin=r.offsetMax=Vector2.zero;
                go.GetComponent<Image>().color=C("272922");go.GetComponent<Image>().raycastTarget=false;
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        if(!wasLoaded)EditorSceneManager.CloseScene(scene,true);
    }
}
