using System;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

internal static partial class ExplorationDepthPreparation
{
    [Serializable] class ArtPlan{public Art[] objects;public string dogIdle,dogAttack,dogOppositeIdle,dogOppositeAttack,dogIcon,trapIcon;}
    [Serializable] class Art{public string name,front,side,rear,usedFront,usedSide,usedRear;public float height;public bool wall,door,frame,face;}
    [Serializable] class ContentPlan{public StoryPatch[] events;}
    [Serializable] class StoryPatch{public string id,name,lore,art,clone;public PagePatch[] pages;public OptionPatch[] options;}
    [Serializable] class PagePatch{public int index;public string text;}
    [Serializable] class OptionPatch{public int page,index;public string title,result,revisit,description;public bool used,remove;}
    static T Load<T>(string path)where T:Object=>AssetDatabase.LoadAssetAtPath<T>(path);
    static Sprite Painting(string path)=>string.IsNullOrEmpty(path)?null:Load<Sprite>(path);
    static void ImportPainting(string path)
    {
        if(string.IsNullOrEmpty(path))return;Backup(path);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.spritePixelsPerUnit=100;importer.SaveAndReimport();
    }
    static partial void InstallContent()
    {
        var art=JsonUtility.FromJson<ArtPlan>(File.ReadAllText(Folder+"ArtReady.json"));
        var plan=JsonUtility.FromJson<ContentPlan>(File.ReadAllText(Folder+"ContentPlan.json"));
        var paths=art.objects.SelectMany(a=>new[]{a.front,a.side,a.rear,a.usedFront,a.usedSide,a.usedRear}).Concat(new[]{art.dogIdle,art.dogAttack,art.dogOppositeIdle,art.dogOppositeAttack,art.dogIcon,art.trapIcon}).Where(p=>!string.IsNullOrEmpty(p)).Distinct().ToArray();
        foreach(var path in paths)ImportPainting(path);
        var objects=AssetDatabase.FindAssets("t:RoomObjectData").Select(g=>Load<RoomObjectData>(AssetDatabase.GUIDToAssetPath(g))).ToDictionary(o=>o.name.Replace("Data_RoomObject_",""));
        foreach(var a in art.objects)
        {
            if(!objects.TryGetValue(a.name,out var obj))
            {obj=ScriptableObject.CreateInstance<RoomObjectData>();AssetDatabase.CreateAsset(obj,"Assets/Data/Presentation/Exploration/Data_RoomObject_"+a.name+".asset");objects.Add(a.name,obj);}
            else Backup(AssetDatabase.GetAssetPath(obj));
            obj.front=Painting(a.front);obj.side=Painting(a.side);obj.rear=Painting(a.rear);obj.usedFront=Painting(a.usedFront);obj.usedSide=Painting(a.usedSide);obj.usedRear=Painting(a.usedRear);
            obj.displayHeight=a.height;obj.wallMounted=a.wall;obj.fitsDoorway=a.door;obj.includesDoorframe=a.frame;obj.facePlayerOnFirstEncounter=a.face;
            obj.roomInterior=false;obj.usedForm=null;obj.wallSuspended=false;
            var sprites=new[]{obj.front,obj.side,obj.rear,obj.usedFront,obj.usedSide,obj.usedRear}.Where(s=>s!=null).Distinct();
            obj.SetDerivedGeometry(sprites.Select(ExplorationArtPreparation.Geometry).ToArray());EditorUtility.SetDirty(obj);
        }
        var settings=Load<ExplorationSettings>(ExplorationPreparation.SettingsPath);Backup(AssetDatabase.GetAssetPath(settings));settings.secretWallSearches=3;
        using(var hash=System.Security.Cryptography.SHA256.Create())
        {
            var signatures=paths.Select(p=>p+":"+BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(p)))).ToArray();
            settings.presentationRevision=BitConverter.ToString(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(string.Join("|",signatures)))).Replace("-","");
        }
        EditorUtility.SetDirty(settings);
        var dog=Load<CompanionData>(AssetDatabase.GUIDToAssetPath("c6d66f4a1e280544b805c94668f5f3e7"));Backup(AssetDatabase.GetAssetPath(dog));
        dog.combatSprite=Painting(art.dogIdle);dog.displayHeight=245;dog.narrationKey="ui.companion.recoveryhound";
        dog.SetDerivedGeometry(new[]{Painting(art.dogIdle),Painting(art.dogAttack),Painting(art.dogOppositeIdle),Painting(art.dogOppositeAttack)}.Where(s=>s!=null).Distinct().Select(ExplorationArtPreparation.Geometry).ToArray());
        var library=dog.combatAnimation;Backup(AssetDatabase.GetAssetPath(library));library.idle=dog.combatSprite;library.attack=Painting(art.dogAttack);EditorUtility.SetDirty(library);EditorUtility.SetDirty(dog);
        var opposite=Load<CombatSpriteAnimation>("Assets/Data/Presentation/Animations/Data_Animation_회수견_반대시점.asset");
        if(opposite==null){opposite=Object.Instantiate(library);AssetDatabase.CreateAsset(opposite,"Assets/Data/Presentation/Animations/Data_Animation_회수견_반대시점.asset");}
        opposite.idle=Painting(art.dogOppositeIdle);opposite.attack=Painting(art.dogOppositeAttack);EditorUtility.SetDirty(opposite);dog.oppositeAnimation=opposite;
        var dogPath=AssetDatabase.GetAssetPath(dog.prefab);Backup(dogPath);var prefab=PrefabUtility.LoadPrefabContents(dogPath);
        try{prefab.GetComponent<CompanionView>().Configure(dog);PrefabUtility.SaveAsPrefabAsset(prefab,dogPath);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
        var stories=AssetDatabase.FindAssets("t:RandomEventData").Select(g=>Load<RandomEventData>(AssetDatabase.GUIDToAssetPath(g))).ToDictionary(e=>e.id);
        var csv=ScriptableObject.CreateInstance<GameTextDatabase>();csv.ImportCsv(File.ReadAllText(BehemothTextEditor.CsvPath));
        void Text(string key,string value){csv.Set(key,value,"Exploration depth");BehemothTextEditor.Table.Set(key,value,"Exploration depth");}
        foreach(var p in plan.events)
        {
            if(!stories.TryGetValue(p.id,out var story))
            {
                story=Object.Instantiate(stories[p.clone]);story.id=p.id;story.excludedFromRandomPool=false;story.textKeys.Clear();story.textOwnerGuid="";story.options.Clear();
                AssetDatabase.CreateAsset(story,"Assets/Data/Dungeon/Events/Data_Event_끊어진_회수선.asset");stories.Add(story.id,story);
                var old=stories[p.clone];Backup(AssetDatabase.GetAssetPath(old));old.excludedFromRandomPool=true;EditorUtility.SetDirty(old);
                foreach(var guid in AssetDatabase.FindAssets("t:GameContent"))
                {var content=Load<GameContent>(AssetDatabase.GUIDToAssetPath(guid));Backup(AssetDatabase.GetAssetPath(content));if(!content.events.Contains(story))content.events.Add(story);EditorUtility.SetDirty(content);}
            }
            Backup(AssetDatabase.GetAssetPath(story));
            if(!string.IsNullOrEmpty(p.name))story.displayName=p.name;if(!string.IsNullOrEmpty(p.lore))story.authoringLore=p.lore;
            if(!string.IsNullOrEmpty(p.art))story.worldObject=objects[p.art];
            foreach(var page in p.pages)story.pages[page.index].narrative=page.text;
            foreach(var patch in p.options)
            {
                var option=patch.page<0?story.options[patch.index]:story.pages[patch.page].options[patch.index];
                if(patch.title!=null)option.title=patch.title;if(patch.description!=null)option.description=patch.description;
                if(patch.result!=null)option.resultText=patch.result;
                if(option.nextPage<0)
                {
                    option.removeWorldObject=patch.remove;option.useWorldObject=patch.used;option.resultingWorldObject=null;
                    if(patch.revisit!=null){option.revisitTextKey="ui.depth.revisit."+p.id+"."+patch.page+"."+patch.index;Text(option.revisitTextKey,patch.revisit);}
                }
            }
            if(story.pages.Count>0)story.narrative=story.pages[0].narrative;
            story.illustration=story.worldObject.front;foreach(var page in story.pages)page.illustration=story.illustration;
            story.worldObject.labelKey="ui.depth.object."+story.id;Text(story.worldObject.labelKey,story.displayName);EditorUtility.SetDirty(story.worldObject);
            if(!string.IsNullOrEmpty(p.clone))
            {
                if(!story.referenceExperience.Contains("2026-11-01: 일반 미궁의 거울 전제 폐기"))story.referenceExperience+="\n2026-11-01: 일반 미궁의 거울 전제 폐기. 회수대 쇠덫의 기계적 위험/관찰/정비 주머니로 원래 선택 비용과 효과를 재구성. 공간 이동 없음.";
                var relic=story.pages.SelectMany(pg=>pg.options).SelectMany(o=>o.effects).First(e=>e.kind==EventEffectKind.Relic).relic;
                Backup(AssetDatabase.GetAssetPath(relic));relic.displayName="반격 태엽";relic.icon=Painting(art.trapIcon);
                relic.description="매 4번째 무기 적중에 피해 40%를 추가합니다.";Sync(relic,csv);
            }
            if(story.id=="StrayDog")
            {
                foreach(var relic in story.pages.SelectMany(pg=>pg.options).SelectMany(o=>o.effects).Where(e=>e.kind==EventEffectKind.Relic).Select(e=>e.relic).Distinct())
                {Backup(AssetDatabase.GetAssetPath(relic));relic.icon=Painting(art.dogIcon);relic.displayName="회수견의 목줄";relic.description="회수견이 곁에서 동행한다. 4초마다 체력이 가장 낮은 적에게 7 + 현재 레벨만큼 피해.";Sync(relic,csv);}
            }
            Sync(story,csv);
        }
        dog.displayName="회수견";
        // These prototypes require an artificial mutual exclusion between physical loot
        // choices. Preserve their data for authors, but keep them out of ordinary play.
        foreach(var id in new[]{"Mirror","SalvageTrap"})if(stories.TryGetValue(id,out var retired)){retired.excludedFromRandomPool=true;EditorUtility.SetDirty(retired);}
        foreach(var story in stories.Values.Where(s=>!s.excludedFromRandomPool))
        {
            story.narrative=HoundText(story.narrative);
            foreach(var pg in story.pages){pg.narrative=HoundText(pg.narrative);foreach(var o in pg.options){o.title=HoundText(o.title);o.resultText=HoundText(o.resultText);}}
            foreach(var o in story.options){o.title=HoundText(o.title);o.resultText=HoundText(o.resultText);}Sync(story,csv);
        }
        Sync(dog,csv);
        foreach(var path in new[]{"Assets/Scenes/MainGameScene.unity","Assets/Scenes/Trials/Angular01_Main.unity"})
        {
            Backup(path);var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);var loaded=scene.isLoaded;if(!loaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var roots=scene.GetRootGameObjects();var overlay=roots.SelectMany(r=>r.GetComponentsInChildren<DebugCheatOverlay>(true)).Single();
                overlay.roomObjectCatalog=settings.roomObjects.Select(b=>b.artwork).Concat(stories.Values.Where(e=>!e.excludedFromRandomPool).Select(e=>e.worldObject))
                    .Concat(new[]{settings.key,settings.hiddenWall,settings.lockedDoor,settings.eliteEntrance,settings.bossEntrance}).Where(a=>a!=null).Distinct().OrderBy(a=>a.name).ToList();EditorUtility.SetDirty(overlay);
                var flow=roots.SelectMany(r=>r.GetComponentsInChildren<GameFlowController>(true)).Single();
                var serialized=new SerializedObject(flow);serialized.FindProperty("fountainEventSprite").objectReferenceValue=objects["분수"].front;serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            finally{if(!loaded)EditorSceneManager.CloseScene(scene,true);}
        }
        File.WriteAllText(BehemothTextEditor.CsvPath,csv.ExportCsv(),new System.Text.UTF8Encoding(true));Object.DestroyImmediate(csv);EditorUtility.SetDirty(BehemothTextEditor.Table);AssetDatabase.SaveAssets();
        File.WriteAllText(Folder+"Installed.txt","Saved adult recovery hound with both authored directions, pedestal fountains, complete entrance frames, surveying mage and localization; Mirror and SalvageTrap excluded. See Verification.md for current acceptance.\n");
    }
    static string HoundText(string value) => value?.Replace("강아지","회수견").Replace("회수견가","회수견이").Replace("회수견는","회수견은").Replace("회수견와","회수견과");
    static void Sync(LocalizedData data,GameTextDatabase csv)
    {EditorUtility.SetDirty(data);BehemothTextEditor.SyncAsset(data);foreach(var key in data.textKeys)csv.Set(key.key,BehemothTextEditor.Table.Get(key.key),"Exploration depth");}
}
