using System;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

// Explicit scoped installation/review. Opening the project alone never reapplies art.
[InitializeOnLoad]
internal static class PaletteCohesionVerification
{
    const string Folder="Docs/Production/PaletteCohesion-2026-11-01/";
    const string Request="Temp/PaletteCohesion.request";
    [Serializable] sealed class Manifest { public Entry[] files; }
    [Serializable] sealed class Entry { public string path; }
    static bool playReady;
    static PaletteCohesionVerification()
    {
        EditorApplication.update+=Poll;
        EditorApplication.playModeStateChanged+=state=>playReady=state==PlayModeStateChange.EnteredPlayMode;
    }
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists(Request)&&!EditorApplication.isPlayingOrWillChangePlaymode)
        {
            var command=File.ReadAllText(Request).Trim();File.Delete(Request);
            try
            {
                if(command=="status")
                    File.WriteAllText(Folder+"EditorStatus.txt","playing="+EditorApplication.isPlaying+"\nscene="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().path+"\n"+DateTime.UtcNow.ToString("O"));
                else if(command=="install")Install();
                else if(command=="run"||command=="dog"||command=="props")
                {
                    var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                    if(scene.isDirty||scene.path!="Assets/Scenes/MainGameScene.unity")throw new InvalidOperationException("Preserve current scene edits; scoped review requires saved MainGameScene.");
                    SessionState.SetBool("PaletteCohesion.DogOnly",command=="dog");SessionState.SetBool("PaletteCohesion.PropsOnly",command=="props");SessionState.SetBool("PaletteCohesion.Review",true);EditorApplication.isPlaying=true;
                }
            }
            catch(Exception e){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"EditorError.txt",e.ToString());Debug.LogException(e);}
        }
        if(playReady&&EditorApplication.isPlaying&&SessionState.GetBool("PaletteCohesion.Review",false)&&UnityEngine.Object.FindFirstObjectByType<GameFlowController>()!=null)
        {
            SessionState.SetBool("PaletteCohesion.Review",false);
            new GameObject("Scoped palette cohesion review").AddComponent<ExplorationDepthLiveProbe>().RunPaletteCohesion(SessionState.GetBool("PaletteCohesion.DogOnly",false),SessionState.GetBool("PaletteCohesion.PropsOnly",false));
        }
    }
    static void Install()
    {
        var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Folder+"BackgroundArtInstalled.json"));
        foreach(var entry in manifest.files)
        {
            AssetDatabase.ImportAsset(entry.path,ImportAssetOptions.ForceSynchronousImport);
            var importer=AssetImporter.GetAtPath(entry.path) as TextureImporter;
            if(importer==null)throw new IOException(entry.path);
            // New act paintings follow the existing single-sprite background canvas.
            if(importer.textureType!=TextureImporterType.Sprite)
            {
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=100;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;
                importer.SaveAndReimport();
            }
        }
        const string path="Assets/Data/Presentation/RoomThemes/Data_RoomTheme_잊힌서약실.asset";
        var backup=Folder+"Before/"+path;Directory.CreateDirectory(Path.GetDirectoryName(backup));if(!File.Exists(backup))File.Copy(path,backup);
        var theme=AssetDatabase.LoadAssetAtPath<RoomThemeData>(path);
        var paintings=new[]{"공용_서약실","B2_서약실","B3_서약실"}.Select(n=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/World/Angular/RoomThemes/T_UI_Room_"+n+".png")).ToArray();
        if(paintings.Any(p=>p==null))throw new InvalidDataException("Missing act-specific oath painting.");
        for(var i=0;i<3;i++)theme.acts[i].walls=paintings[i];
        theme.useFloorLighting=false;EditorUtility.SetDirty(theme);
        // Derived support follows the changed editable PNG, never manual duplicate knobs.
        foreach(var name in new[]{"여신상","분수"})
        {
            var obj=AssetDatabase.LoadAssetAtPath<RoomObjectData>("Assets/Data/Presentation/Exploration/Data_RoomObject_"+name+".asset");
            obj.SetDerivedGeometry(new[]{obj.front,obj.side,obj.rear,obj.usedFront,obj.usedSide,obj.usedRear}.Where(s=>s!=null).Distinct().Select(ExplorationArtPreparation.Geometry).ToArray());EditorUtility.SetDirty(obj);
        }
        PaletteCompanionPreparation.Install();
        var goddess=AssetDatabase.LoadAssetAtPath<RoomObjectData>("Assets/Data/Presentation/Exploration/Data_RoomObject_여신상.asset");
        // Keep the authored wall statue below the HUD even at B3's higher side support.
        goddess.displayHeight=550;EditorUtility.SetDirty(goddess);
        WorldRenewalPreparation.UpdateArtRevision();AssetDatabase.SaveAssets();
        File.WriteAllText(Folder+"Installed.txt","Installed six background paintings and act-specific oath references without duplicate background tint. Refreshed changed prop support and art revision.\n"+DateTime.UtcNow.ToString("O"));
    }
}
