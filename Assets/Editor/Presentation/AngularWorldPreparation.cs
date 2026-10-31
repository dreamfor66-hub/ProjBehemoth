using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DungeonSlash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Installs saved production assets once all sources exist. No menu/bake step.
[InitializeOnLoad]
internal static class AngularWorldPreparation
{
    internal const string Report="Docs/Production/AngularWorld-2026-10-26/";
    const string Art="Assets/Art/World/Angular/",Data="Assets/Data/Presentation/Angular/";
    const string Stamp=Data+"Installed.txt";
    const string Revision="angular-world-3";
    [Serializable] internal sealed class Entry
    {
        public string baseline,id,name,subject,hint;
        public int floor;
        public string[] normalLabels;
        public float[] windupFactors;
    }
    [Serializable] sealed class Roster {public Entry[] items;}
    static double next;static bool busy;
    static AngularWorldPreparation(){EditorApplication.update+=Poll;}
    internal static Entry[] Entries()=>JsonUtility.FromJson<Roster>("{\"items\":"+File.ReadAllText(Report+"Roster.json").TrimStart('\uFEFF')+"}").items;
    static void Poll()
    {
        if(busy||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.timeSinceStartup<next)return;
        next=EditorApplication.timeSinceStartup+5;
        if(!File.Exists(Report+"Roster.json")||(File.Exists(Stamp)&&File.ReadAllText(Stamp)==Revision))return;
        var entries=Entries();
        if(entries.Any(e=>!File.Exists(Art+"Monsters/"+e.id+".png")))return;
        if(Enum.GetNames(typeof(CharacterKind)).Any(n=>!File.Exists(Art+"Heroes/"+n+"-Rear.png")))return;
        for(var f=1;f<=3;f++)foreach(var part in new[]{"Walls","Forward","Left","Right"})if(!File.Exists(Art+"Backdrops/B"+f+"-"+part+".png"))return;
        busy=true;
        try{Prepare(entries);File.WriteAllText(Stamp,Revision);AssetDatabase.ImportAsset(Stamp);}
        catch(Exception e){File.WriteAllText(Report+"Preparation-Error.txt",e.ToString());Debug.LogException(e);next=EditorApplication.timeSinceStartup+60;}
        finally{busy=false;}
    }
    static T Asset<T>(string path)where T:Object=>DungeonSlash.DesignerAssetPaths.Load<T>(path);
    static void Import(string path)
    {
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=AssetImporter.GetAtPath(path)as TextureImporter;
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=100;
        var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
        importer.filterMode=FilterMode.Bilinear;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;
        importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();
    }
    static T Create<T>(string path,Func<T> factory)where T:ScriptableObject
    {var asset=Asset<T>(path);if(asset==null){asset=factory();AssetDatabase.CreateAsset(asset,path);}return asset;}
    static readonly HashSet<LocalizedData> ChangedText=new();
    static void Text(LocalizedData data){EditorUtility.SetDirty(data);BehemothTextEditor.SyncAsset(data);ChangedText.Add(data);}
    static void Prepare(Entry[] entries)
    {
        Directory.CreateDirectory(Data+"Animations");Directory.CreateDirectory(Data+"Attacks");AssetDatabase.Refresh();
        var audit=new StringBuilder("Production art installation. Original numeric pacing preserved.\n");
        foreach(var e in entries)
        {
            var path=Art+"Monsters/"+e.id+".png";Import(path);var sprite=Asset<Sprite>(path);
            var monster=Asset<MonsterData>("Assets/Data/Combat/Monsters/"+e.baseline+".asset");
            if(monster==null)throw new InvalidDataException(e.baseline);
            var originalActions=monster.attacks.ToArray();
            monster.displayName=e.name;monster.encounterHint=e.hint;monster.portrait=sprite;
            var animation=Create(Data+"Animations/"+e.id+".asset",ScriptableObject.CreateInstance<CombatSpriteAnimation>);
            animation.idle=sprite;animation.guard=null;
            animation.canvasScale=1;animation.illustratedLayout=true;animation.illustratedMotion=false;EditorUtility.SetDirty(animation);monster.combatAnimation=animation;
            var bounds=OpaqueBounds(path);
            var floating=new[]{"StringRevenant","Threadling","QuillSprite","VeilOracle","MirrorMoth","MaskedChoir","TempestRoc","StormJelly","AshGargoyle","HollowEye","AshDragon","SunIdol","MoonIdol"};
            animation.combatPositionOffset=new Vector2(0,floating.Contains(e.id)?0:-220f+190f*1.65f*(bounds.y-.5f));
            var actions=new List<MonsterAttackData>();
            var chargeIndex=0;
            for(var i=0;i<originalActions.Length;i++)
            {
                var source=originalActions[i];if(source==null)continue;
                var action=Create(Data+"Attacks/"+e.id+"-"+i+".asset",()=>Object.Instantiate(source));
                action.displayName=source.type==MonsterAttackType.Normal?e.name+"의 견제":e.name+"의 강타 "+(++chargeIndex);
                Text(action);actions.Add(action);
            }
            monster.attacks=actions;monster.normalAttackSequence=new List<MonsterAttackData>();
            // Art replacement preserves the designer-authored normal action and timing.
            // A concept/name change does not authorize a fast/slow alternating sequence.
            Text(monster);
            audit.AppendLine($"{e.floor} {e.id}: {e.baseline}; HP={monster.maxHp}; normal={monster.normalAttackInterval}; charge={monster.chargeInterval}; sequence={monster.normalAttackSequence.Count}");
        }
        // Keep encounter composition, timing offsets, IDs and rewards; name the new occupants.
        foreach(var guid in AssetDatabase.FindAssets("t:EncounterData",new[]{"Assets/Data/Dungeon/Encounters"}))
        {
            var encounter=Asset<EncounterData>(AssetDatabase.GUIDToAssetPath(guid));
            if(!encounter.members.Any(m=>m.monster!=null&&entries.Any(e=>m.monster.name==e.baseline)))continue;
            var occupants=encounter.members.Where(m=>m.monster!=null).Select(m=>m.monster).Distinct().ToArray();
            encounter.displayName=string.Join(" · ",occupants.Select(m=>m.displayName));
            encounter.hint=string.Join("\n",occupants.Select(m=>m.encounterHint).Where(h=>!string.IsNullOrEmpty(h)));
            Text(encounter);
        }
        var floors=new FloorArtwork[3];
        for(var f=1;f<=3;f++)
        {
            foreach(var part in new[]{"Walls","Forward","Left","Right"})Import(Art+"Backdrops/B"+f+"-"+part+".png");
            floors[f-1]=Create(Data+"B"+f+".asset",ScriptableObject.CreateInstance<FloorArtwork>);
            floors[f-1].walls=Asset<Sprite>(Art+"Backdrops/B"+f+"-Walls.png");
            floors[f-1].openings=new[]{"Forward","Left","Right"}.Select(p=>Asset<Sprite>(Art+"Backdrops/B"+f+"-"+p+".png")).ToArray();
            EditorUtility.SetDirty(floors[f-1]);
        }
        var native=Create(Data+"WorldArtwork.asset",()=>Object.Instantiate(Asset<ArcadePresentation>("Assets/Data/Presentation/Arcade224.asset")));
        native.width=1920;native.height=1080;native.arcade=true;native.preserveIllustrationColors=true;EditorUtility.SetDirty(native);
        var heroes=Enum.GetNames(typeof(CharacterKind)).Select(n=>Asset<PlayerCombatData>("Assets/Data/Characters/"+n+".asset")).ToArray();
        foreach(var hero in heroes)
        {
            var path=Art+"Heroes/"+hero.character+"-Rear.png";Import(path);
            hero.combatSprite=Asset<Sprite>(path);
            var animation=Create(Data+"Animations/Hero-"+hero.character+".asset",ScriptableObject.CreateInstance<CombatSpriteAnimation>);
            animation.idle=hero.combatSprite;animation.ready=animation.dead=null;
            animation.illustratedLayout=true;animation.illustratedMotion=false;animation.canvasScale=1;
            hero.combatAnimation=animation;
            var portrait=Art+"Heroes/"+hero.character+"-Portrait.png";Import(portrait);hero.portrait=Asset<Sprite>(portrait);
            EditorUtility.SetDirty(hero);EditorUtility.SetDirty(animation);
        }
        var faces=Create(Data+"HeroFaces.asset",ScriptableObject.CreateInstance<FatigueFaces>);
        faces.heroes=heroes.Select(h=>new FatigueFaces.Hero{character=h.character,expressions=Enumerable.Repeat(Asset<Sprite>("Assets/Art/UI/Portraits/Angular/"+h.character+".png")??h.portrait,4).ToArray()}).ToArray();faces.useStageTint=true;EditorUtility.SetDirty(faces);
        if(File.Exists("Assets/Data/Presentation/Angular/RestorationInstalled.txt"))AngularRestorationPreparation.ApplySavedComposition();
        EditScene("Assets/Scenes/OutGameScene.unity",roots=>
        {
            var menu=roots.SelectMany(r=>r.GetComponentsInChildren<OutGameMenuView>(true)).Single();
            menu.characterPage.name="SelectionPage";
        });
        EditScene("Assets/Scenes/MainGameScene.unity",roots=>
        {
            var backdrop=roots.SelectMany(r=>r.GetComponentsInChildren<StageBackdropView>(true)).Single();
            backdrop.dungeon=roots.SelectMany(r=>r.GetComponentsInChildren<DungeonController>(true)).Single();
            backdrop.artwork=floors;backdrop.stages=floors.Select(f=>f.walls).ToArray();
            backdrop.background.sprite=floors[0].walls;backdrop.background.material=null;backdrop.background.color=Color.white;
            backdrop.doorLayers=new Image[3];
            for(var i=0;i<3;i++)
            {
                var name="Door artwork "+(RelativeDirection)i;var existing=backdrop.background.transform.Find(name);
                var image=existing!=null?existing.GetComponent<Image>():null;
                if(image==null){var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(backdrop.background.transform,false);image=go.GetComponent<Image>();}
                var rect=image.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                image.sprite=floors[0].openings[i];image.color=Color.white;image.raycastTarget=false;image.enabled=false;
                backdrop.doorLayers[i]=image;
            }
            foreach(var raster in roots.SelectMany(r=>r.GetComponentsInChildren<ArcadeRasterCamera>(true))){raster.presentation=native;raster.quantizeColors=false;}
            foreach(var hud in roots.SelectMany(r=>r.GetComponentsInChildren<FatigueHudView>(true))){hud.faces=faces;hud.face.material=null;}
            foreach(var image in roots.SelectMany(r=>r.GetComponentsInChildren<Image>(true)))
                if(image.material!=null&&image.material.shader.name=="UI/Behemoth/InkFigure")image.material=null;
        });
        var prefab=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Combat/MonsterView.prefab");
        try
        {
            prefab.GetComponent<MonsterView>().presentation=native;
            var body=prefab.GetComponentsInChildren<Image>(true).Single(i=>i.name=="Art");body.material=null;
            PrefabUtility.SaveAsPrefabAsset(prefab,"Assets/Prefabs/Combat/MonsterView.prefab");
        }
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        prefab=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Combat/PlayerView.prefab");
        try
        {
            prefab.GetComponent<PlayerView>().presentation=native;
            foreach(var image in prefab.GetComponentsInChildren<Image>(true))image.material=null;
            PrefabUtility.SaveAsPrefabAsset(prefab,"Assets/Prefabs/Combat/PlayerView.prefab");
        }
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        MergeCsv();AssetDatabase.SaveAssets();File.WriteAllText(Report+"Installed-Audit.txt",audit.ToString());
    }
    // Read source alpha rather than assuming equal square canvases imply equal silhouettes.
    static Vector2 OpaqueBounds(string path)
    {
        var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.LoadImage(File.ReadAllBytes(path));
        var pixels=texture.GetPixels32();var min=texture.height;var max=0;
        for(var y=0;y<texture.height;y++)for(var x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>64){min=Mathf.Min(min,y);max=Mathf.Max(max,y);}
        var bounds=new Vector2(1f-(max+1f)/texture.height,1f-(float)min/texture.height);Object.DestroyImmediate(texture);return bounds;
    }
    static void EditScene(string path,Action<GameObject[]> edit)
    {
        var scene=SceneManager.GetSceneByPath(path);var loaded=scene.isLoaded;
        if(loaded&&scene.isDirty)throw new InvalidOperationException("Unsaved designer scene: "+path);
        if(!loaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try{edit(scene.GetRootGameObjects());EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
        finally{if(!loaded)EditorSceneManager.CloseScene(scene,true);}
    }
    static void MergeCsv()
    {
        var csv=ScriptableObject.CreateInstance<GameTextDatabase>();csv.ImportCsv(File.ReadAllText(BehemothTextEditor.CsvPath));
        foreach(var pair in new[]{new[]{"ui.combat.support.cancelled","보조 개체 파괴 · 공격 차단"},new[]{"ui.combat.support.broken","두 우상 파괴 · 본체 기절"}})
        {csv.Set(pair[0],pair[1],"Assets/Scripts/Combat/Systems/CombatExpansion.cs");BehemothTextEditor.Table.Set(pair[0],pair[1],"Assets/Scripts/Combat/Systems/CombatExpansion.cs");}
        EditorUtility.SetDirty(BehemothTextEditor.Table);
        foreach(var data in ChangedText)foreach(var binding in data.textKeys)
            csv.Set(binding.key,BehemothTextEditor.Table.Get(binding.key),AssetDatabase.GetAssetPath(data)+":"+binding.propertyPath);
        File.WriteAllText(BehemothTextEditor.CsvPath,csv.ExportCsv(),new UTF8Encoding(true));Object.DestroyImmediate(csv);ChangedText.Clear();
        AssetDatabase.ImportAsset(BehemothTextEditor.CsvPath);
    }
}
