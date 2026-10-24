using System;
using System.IO;
using System.Linq;
using DungeonSlash;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
internal static class CombatAnimationPreparation
{
    const string Art="Assets/Art/Arcade224/Animation/",Data="Assets/Data/Presentation/Animations/";
    const string States="Assets/Art/Arcade224/States/";
    [Serializable]sealed class Manifest{public Sheet[] sheets;}
    [Serializable]sealed class Sheet{public string file;public string[] actors;}
    static bool Complete(CombatSpriteAnimation p,bool hero)=>p!=null&&p.idle!=null&&p.attack!=null&&(hero?p.guard!=null:p.ready!=null&&p.dead!=null);
    static double next;
    static CombatAnimationPreparation()=>EditorApplication.update+=Poll;
    static void Poll()
    {
        if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+4;
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!Directory.Exists(Art))return;
        if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))return;
        var presentation=AssetDatabase.LoadAssetAtPath<ArcadePresentation>("Assets/Data/Presentation/Arcade224.asset");if(presentation==null||presentation.revision<4)return;
        var content=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/Data/Behemoth/GameContent.asset");
        var updated=false;
        foreach(var player in content.characters)
        {
            var path=Art+"Hero_"+player.character+".png";
            if(!File.Exists(path)||Complete(player.combatAnimation,true))continue;
            player.combatAnimation=Build(path,"Hero_"+player.character,true);EditorUtility.SetDirty(player);updated=true;
        }
        var manifest=File.Exists(States+"PoseSheets.json")?JsonUtility.FromJson<Manifest>(File.ReadAllText(States+"PoseSheets.json")):null;
        foreach(var guid in AssetDatabase.FindAssets("t:MonsterData",new[]{"Assets/Data/Behemoth","Assets/Data/DungeonSlashPrototype"}))
        {
            var monster=AssetDatabase.LoadAssetAtPath<MonsterData>(AssetDatabase.GUIDToAssetPath(guid));
            var key=Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(monster));key=key.Substring(key.LastIndexOf('_')+1);
            if(key is "TrainingMedium" or "TrainingHeavy")key="TrainingLight";
            var path=Art+"Enemy_"+key+".png";
            if(Complete(monster.combatAnimation,false))continue;
            var poses=AssetDatabase.LoadAssetAtPath<CombatSpriteAnimation>(Data+"Enemy_"+key+".asset");
            if(!Complete(poses,false))
            {
                if(File.Exists(path))poses=Build(path,"Enemy_"+key,false);
                else{var sheet=manifest?.sheets?.FirstOrDefault(s=>s.actors.Contains(key));if(sheet==null||!File.Exists(States+sheet.file))continue;BuildGroup(sheet);poses=AssetDatabase.LoadAssetAtPath<CombatSpriteAnimation>(Data+"Enemy_"+key+".asset");}
            }
            monster.combatAnimation=poses;EditorUtility.SetDirty(monster);updated=true;
        }
        if(File.Exists(States+"CompanionDog.png"))foreach(var guid in AssetDatabase.FindAssets("t:CompanionData",new[]{"Assets/Data"}))
        {
            var dog=AssetDatabase.LoadAssetAtPath<CompanionData>(AssetDatabase.GUIDToAssetPath(guid));if(dog.combatAnimation?.idle!=null&&dog.combatAnimation.attack!=null)continue;
            dog.combatAnimation=Save("CompanionDog",Import(States+"CompanionDog.png",2,1,new[]{("Idle",0),("Attack",1)}));EditorUtility.SetDirty(dog);updated=true;
        }
        if(presentation.revision<5&&File.Exists(States+"SpecialStates.png")&&content.characters.All(c=>Complete(c.combatAnimation,true))&&AssetDatabase.FindAssets("t:MonsterData",new[]{"Assets/Data/Behemoth","Assets/Data/DungeonSlashPrototype"}).All(g=>Complete(AssetDatabase.LoadAssetAtPath<MonsterData>(AssetDatabase.GUIDToAssetPath(g)).combatAnimation,false)))
        {
            BuildGroup(new Sheet{file="SpecialStates.png",actors=new[]{"IceNeedle","StormOrb","TrainingLight"}});
            void Correct(string target,string file,string source)
            {
                var sprites=AssetDatabase.LoadAllAssetsAtPath(States+file).OfType<Sprite>().ToArray();var prefix=Path.GetFileNameWithoutExtension(file)+"_"+source+"_";
                Save("Enemy_"+target,new[]{"Idle","Ready","Attack","Dead"}.ToDictionary(s=>s,s=>sprites.Single(p=>p.name==prefix+s)));
            }
            Correct("DeepTrialGuardian","EnemyStates_04.png","ElderLeftHeart");Correct("EclipseBeast","EnemyStates_03.png","DeepTrialGuardian");
            Correct("ElderLeftHeart","EnemyStates_05.png","LeftHeart");Correct("ElderRightHeart","EnemyStates_06.png","RightHeart");
            presentation.revision=5;EditorUtility.SetDirty(presentation);updated=true;
        }
        if(!updated)return;
        PrepareLayout(presentation);ApplyPrefabStyle(presentation.arcade);AssetDatabase.SaveAssets();
        File.WriteAllText("Logs/Arcade224-Animation-Preparation.txt","ONE representative image per state; no frame playback.\n"+string.Join("\n",content.characters.Select(p=>p.character+": "+Complete(p.combatAnimation,true)))+"\nPose sets: "+AssetDatabase.FindAssets("t:CombatSpriteAnimation",new[]{Data}).Length);
    }
    static CombatSpriteAnimation Build(string path,string key,bool hero)
    {
        return Save(key,Import(path,hero?3:4,3,hero?new[]{("Idle",0),("Attack",4),("Guard",7)}:new[]{("Idle",0),("Ready",5),("Attack",6),("Dead",11)}));
    }
    static System.Collections.Generic.Dictionary<string,Sprite> Import(string path,int cols,int rows,(string name,int cell)[] picks)
    {
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.npotScale=TextureImporterNPOTScale.None;importer.alphaIsTransparency=true;importer.maxTextureSize=512;
        importer.GetSourceTextureWidthAndHeight(out var w,out var h);
        var metadata=picks.Select(p=>new SpriteMetaData{name=Path.GetFileNameWithoutExtension(path)+"_"+p.name,rect=new Rect(p.cell%cols*w/(float)cols,(rows-1-p.cell/cols)*h/(float)rows,w/(float)cols,h/(float)rows),alignment=(int)SpriteAlignment.Center,pivot=Vector2.one*.5f}).ToArray();
#pragma warning disable CS0618
        importer.spritesheet=metadata;
#pragma warning restore CS0618
        importer.SaveAndReimport();var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();return picks.ToDictionary(p=>p.name,p=>sprites.Single(s=>s.name==Path.GetFileNameWithoutExtension(path)+"_"+p.name));
    }
    static CombatSpriteAnimation Save(string key,System.Collections.Generic.Dictionary<string,Sprite> sprites)
    {
        Directory.CreateDirectory(Data);var path=Data+key+".asset";var asset=AssetDatabase.LoadAssetAtPath<CombatSpriteAnimation>(path);if(asset==null){asset=ScriptableObject.CreateInstance<CombatSpriteAnimation>();AssetDatabase.CreateAsset(asset,path);}
        Sprite Get(string state)=>sprites.TryGetValue(state,out var s)?s:null;
        asset.idle=Get("Idle");asset.attack=Get("Attack");asset.guard=Get("Guard");asset.ready=Get("Ready");asset.dead=Get("Dead");EditorUtility.SetDirty(asset);return asset;
    }
    static void BuildGroup(Sheet sheet)
    {
        var names=new[]{"Idle","Ready","Attack","Dead"};var all=Import(States+sheet.file,4,sheet.actors.Length,sheet.actors.SelectMany((actor,row)=>names.Select((state,col)=>(actor+"_"+state,row*4+col))).ToArray());
        foreach(var actor in sheet.actors)Save("Enemy_"+actor,names.ToDictionary(s=>s,s=>all[actor+"_"+s]));
    }
    internal static void ApplyPrefabStyle(bool arcade)
    {
        var presentation=AssetDatabase.LoadAssetAtPath<ArcadePresentation>("Assets/Data/Presentation/Arcade224.asset");
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);var root=PrefabUtility.LoadPrefabContents(path);var player=root.GetComponent<PlayerView>();var monster=root.GetComponent<MonsterView>();var dog=root.GetComponent<CompanionView>();
            var font=arcade?AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Art/Arcade224/ArcadeFont.asset"):AssetDatabase.LoadAssetAtPath<ArtReferenceArchive>("Assets/Editor/Arcade-ArtReferences.asset")?.geometryFont;
            var texts=root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            if(font!=null&&texts.Length>0){foreach(var text in texts)text.font=font;PrefabUtility.SaveAsPrefabAsset(root,path);}
            if(player!=null)player.presentation=presentation;if(monster!=null)monster.presentation=presentation;if(dog!=null){dog.presentation=presentation;PrefabUtility.SaveAsPrefabAsset(root,path);}
            if(player!=null||monster!=null)
            {
                var owner=(UnityEngine.Object)player??monster;var so=new SerializedObject(owner);var image=(Image)so.FindProperty("body").objectReferenceValue;
                image.sprite=arcade?AssetDatabase.LoadAssetAtPath<CombatSpriteAnimation>(Data+(player!=null?"Hero_Warrior":"Enemy_Skeleton")+".asset")?.idle:null;
                image.rectTransform.sizeDelta=player!=null?(arcade?new Vector2(640,680):new Vector2(108,146)):image.rectTransform.sizeDelta;
                if(player!=null)image.rectTransform.anchoredPosition=arcade?new Vector2(0,16):Vector2.zero;
                var head=player!=null?so.FindProperty("head").objectReferenceValue as Image:null;if(head!=null)head.enabled=!arcade;
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
    static void Place(Transform t,float x,float y,float w,float h){var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=new Vector2(x,y)*4;r.sizeDelta=new Vector2(w,h)*4;}
    internal static void RefreshLayout(){var p=AssetDatabase.LoadAssetAtPath<ArcadePresentation>("Assets/Data/Presentation/Arcade224.asset");PrepareLayout(p);ApplyPrefabStyle(p.arcade);Feedback224Preparation.Prepare();AssetDatabase.SaveAssets();}
    static void PrepareLayout(ArcadePresentation presentation)
    {
        Arcade224Preparation.RefreshText();
        var path="Assets/Scenes/MainGameScene.unity";var scene=SceneManager.GetSceneByPath(path);var open=scene.IsValid()&&scene.isLoaded;if(!open)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        var roots=scene.GetRootGameObjects();var expansion=roots.SelectMany(r=>r.GetComponentsInChildren<RunExpansionView>(true)).First();Place(expansion.encounterHint.transform,0,55,350,28);
        foreach(var nav in roots.SelectMany(r=>r.GetComponentsInChildren<NavigationChoiceView>(true)))nav.SetVisible(false);
        if(expansion.characterStatus!=null)expansion.characterStatus.gameObject.SetActive(false);
        for(var i=0;i<4;i++){Place(expansion.spellButtons[i].transform,new[]{-162,-112,112,162}[i],-60,38,28);Place(expansion.spellLabels[i].transform,7,-3,22,24);}
        var mana=roots.SelectMany(r=>r.GetComponentsInChildren<ManaHudView>(true)).First();mana.transform.SetAsLastSibling();Place(mana.gauge.transform,20,174,92,4);Place(mana.spellbook.transform,115,-24,48,18);Place(mana.spellbook.GetComponentInChildren<TextMeshProUGUI>().transform,0,0,46,16);
        var reward=roots.SelectMany(r=>r.GetComponentsInChildren<RewardPopupView>(true)).First();var rewardData=new SerializedObject(reward);var confirm=(Button)rewardData.FindProperty("confirmButton").objectReferenceValue;Place(confirm.GetComponentInChildren<TextMeshProUGUI>().transform,0,0,176,20);
        var shop=roots.SelectMany(r=>r.GetComponentsInChildren<ShopView>(true)).First();var shopData=new SerializedObject(shop);var panel=(GameObject)shopData.FindProperty("panel").objectReferenceValue;
        var cards=panel.GetComponentsInChildren<ShopItemView>(true);
        for(var i=0;i<cards.Length;i++){Place(cards[i].transform,-88+i%2*176,58-i/2*28,168,24);Place(cards[i].transform.Find("ItemTitle"),-7,0,110,24);}
        Place(panel.transform.Find("Title"),-128,80,74,16);Place(panel.transform.Find("Wallet"),70,80,160,16);Place(panel.transform.Find("Leave/Label"),0,0,68,20);
        var result=roots.SelectMany(r=>r.GetComponentsInChildren<ResultView>(true)).First();var resultData=new SerializedObject(result);var restart=(Button)resultData.FindProperty("restart").objectReferenceValue;Place(restart.GetComponentInChildren<TextMeshProUGUI>().transform,0,0,176,20);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(!open)EditorSceneManager.CloseScene(scene,true);
    }
}
