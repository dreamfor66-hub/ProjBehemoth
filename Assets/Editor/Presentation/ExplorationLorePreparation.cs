using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DungeonSlash;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
internal static class ExplorationLorePreparation
{
    internal const string Folder="Docs/Production/ExplorationLore-2026-11-01/";
    const string Objects="Assets/Data/Presentation/Exploration/";
    static double next;
    [Serializable] class ArtPlan { public Art[] art; public Interior[] interiors; public Door[] doors; public Combat[] combat; public Companion companion; }
    [Serializable] class Art { public string name,front,side,rear,usedFront,usedSide,usedRear; public float height; }
    [Serializable] class Interior { public int act; public string @event,path; }
    [Serializable] class Door { public string name,path; }
    [Serializable] class Combat { public string name,idle,ready,attack,dead; }
    [Serializable] class Companion { public string idle,attack; }
    [Serializable] class ContentPlan { public EventPatch[] events; }
    [Serializable] class EventPatch { public string id,displayName,narrative,lore,forbiddenFlag,art; public bool @new; public int kind,layout,minAct,maxAct; public float weight; public PagePatch[] pages; public OptionPatch[] options; }
    [Serializable] class PagePatch { public int index; public string text; }
    [Serializable] class Ref { public string guid; }
    [Serializable] class EffectPatch { public int kind,stat; public float amount; public string flag,asset; public Ref relic,spell,encounter; public Ref[] spellPool; public int spellChoiceCount=3; }
    [Serializable] class RequirementPatch { public int kind; public float amount; public string flag; public bool invert; }
    [Serializable] class BudgetBaselines { public BudgetBaseline[] budgets; }
    [Serializable] class BudgetBaseline { public int act; public float inherited,eventUnits; }
    [Serializable] class OptionPatch { public int page,index,goldCost=-1; public float hpCost=-1; public string title,description,resultText,revisit; public bool remove,used,add,terminal; public EffectPatch[] effects; public RequirementPatch[] requirements; }
    static ExplorationLorePreparation()=>EditorApplication.update+=Poll;
    static void Poll()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.timeSinceStartup<next)return;
        next=EditorApplication.timeSinceStartup+3;
        if(File.Exists(Folder+"Installed.txt")&&!File.Exists(Folder+"BudgetsUpdated.txt")){UpdateBudgets();return;}
        if(File.Exists(Folder+"Installed.txt")&&!File.Exists(Folder+"PresentationFinished.txt")){FinishPresentation();return;}
        if(File.Exists(Folder+"Installed.txt")||!File.Exists(Folder+"ArtReady.json")||!File.Exists(Folder+"ContentPlan.json"))return;
        if(Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Select(UnityEngine.SceneManagement.SceneManager.GetSceneAt).Any(s=>s.isDirty))return;
        try{Install();}catch(Exception e){File.WriteAllText(Folder+"PreparationError.txt",e.ToString());Debug.LogException(e);next+=60;}
    }
    static void Backup(string path)
    { var target=Folder+"Before/"+path;if(File.Exists(path)&&!File.Exists(target)){Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(path,target);} }
    static T Asset<T>(string path)where T:ScriptableObject
    {var value=AssetDatabase.LoadAssetAtPath<T>(path);if(value!=null){Backup(path);return value;}Directory.CreateDirectory(Path.GetDirectoryName(path));value=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(value,path);return value;}
    static Sprite Sprite(string path)=>string.IsNullOrEmpty(path)?null:AssetDatabase.LoadAssetAtPath<Sprite>(path);
    static T Refer<T>(Ref r)where T:Object=>r==null||string.IsNullOrEmpty(r.guid)?null:AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(r.guid));
    static void UpdateBudgets()
    {
        var generation=AssetDatabase.FindAssets("t:DungeonGenerationSettings").Select(g=>AssetDatabase.LoadAssetAtPath<DungeonGenerationSettings>(AssetDatabase.GUIDToAssetPath(g))).First(g=>g.exploration!=null);
        var baselines=JsonUtility.FromJson<BudgetBaselines>(File.ReadAllText(Folder+"BudgetBaseline.json"));float inheritedDelta=0;
        foreach(var budget in AssetDatabase.FindAssets("t:ActGrowthBudget").Select(g=>AssetDatabase.LoadAssetAtPath<ActGrowthBudget>(AssetDatabase.GUIDToAssetPath(g))).OrderBy(b=>b.act))
        {
            Backup(AssetDatabase.GetAssetPath(budget));
            var baseline=baselines.budgets.Single(b=>b.act==budget.act);
            var story=budget.opportunities.FirstOrDefault(o=>o.source=="이벤트 성장 선택");if(story!=null){story.opportunities=generation.eventRoomCount;story.assumption="실제 기본 이벤트방 2곳. 숨겨진 방은 필수 예산에 포함하지 않음. 수락률 35%와 획득당 1.5U는 이전 임시 가정이며 NPC 비중/선택 변경 이후 아직 재검증하지 않음.";}
            var shop=budget.opportunities.FirstOrDefault(o=>o.source=="상점 성장 구매");if(shop!=null)shop.assumption="층당 행상은 1명. 기존 1/2/3명분 재고를 6/12/18칸으로 모았으므로 행상 수가 아닌 재고 묶음 수를 기회로 계산. 구매율 30%와 2U는 미검증 임시 가정. 골드를 추가 지급하지 않음.";
            var previous=budget.opportunities.FirstOrDefault(o=>o.source=="이전 막의 비레벨업 성장");if(previous!=null)previous.unitsPerAcquisition=baseline.inherited+inheritedDelta;
            // Preserve the designer's calibrated level-up rows and existing inherited basis.
            // Only carry forward the delta of the event opportunity row changed here.
            if(story!=null)inheritedDelta+=story.ExpectedUnits-baseline.eventUnits;
            EditorUtility.SetDirty(budget);
        }
        AssetDatabase.SaveAssets();File.WriteAllText(Folder+"BudgetsUpdated.txt","Event opportunities derive from 2 base rooms; supplier opportunities mean merged stock batches. Previous unvalidated acquisition/value assumptions retained and labeled. Only changed event-opportunity deltas propagate into inherited budgets; calibrated B1 rows are preserved. No balance suite run.\n");
    }
    static void FinishPresentation()
    {
        var dog=Asset<CompanionData>("Assets/Data/Characters/Companions/Data_Companion_길동무.asset");dog.playerOffset=new Vector2(-430,240);EditorUtility.SetDirty(dog);
        var path=AssetDatabase.GetAssetPath(dog.prefab);Backup(path);var root=PrefabUtility.LoadPrefabContents(path);
        try{var view=root.GetComponent<CompanionView>();view.Configure(dog);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        var settings=AssetDatabase.LoadAssetAtPath<ExplorationSettings>(ExplorationPreparation.SettingsPath);
        var events=AssetDatabase.FindAssets("t:RandomEventData").Select(g=>AssetDatabase.LoadAssetAtPath<RandomEventData>(AssetDatabase.GUIDToAssetPath(g))).Where(e=>!e.excludedFromRandomPool);
        PrepareScenes(settings,events);
        AssetDatabase.SaveAssets();File.WriteAllText(Folder+"PresentationFinished.txt","Saved dog prefab/new art and authored offset clear of foreground hero. Companion pose framing uses one reference pixel scale. Event encounter monsters are available through the F1 monster catalog.\n");
    }
    internal static void FinishProduction(){UpdateBudgets();FinishPresentation();}
    static void Import(string path,float logicalWidth=0)
    {
        if(string.IsNullOrEmpty(path))return;AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var t=(TextureImporter)AssetImporter.GetAtPath(path);t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;
        t.mipmapEnabled=false;t.alphaIsTransparency=true;t.filterMode=FilterMode.Bilinear;t.textureCompression=TextureImporterCompression.Uncompressed;
        t.spritePixelsPerUnit=100;
        if(logicalWidth>0){t.GetSourceTextureWidthAndHeight(out var width,out _);t.spritePixelsPerUnit=width*100f/logicalWidth;}
        t.SaveAndReimport();
    }
    static void Install()
    {
        var art=JsonUtility.FromJson<ArtPlan>(File.ReadAllText(Folder+"ArtReady.json"));
        var contentPlan=JsonUtility.FromJson<ContentPlan>(File.ReadAllText(Folder+"ContentPlan.json"));
        var csv=ScriptableObject.CreateInstance<GameTextDatabase>();csv.ImportCsv(File.ReadAllText(BehemothTextEditor.CsvPath));
        void Text(string key,string text){csv.Set(key,text,"2026-11-01 exploration lore");BehemothTextEditor.Table.Set(key,text,"2026-11-01 exploration lore");}
        Text("ui.explore.wall.hollow","안쪽이 빈 듯한 소리가 난다.");Text("ui.explore.wall.loose","돌 사이의 모르타르가 조금씩 떨어진다.");Text("ui.explore.wall.opened","벽돌이 밀리며 좁은 입구가 드러났다.");
        Text("ui.explore.goddess.ask","여신상 앞에서 기도를 올릴까?");Text("ui.explore.goddess.pray","기도를 올린다");
        Text("ui.shop.page","재고 {0} / {1}");Text("ui.shop.previous","앞 재고");Text("ui.shop.next","뒤 재고");
        Text("ui.gameflowcontroller.5db06efc","후드를 쓴 행상이 자루의 매듭을 푼다. 가격표 뒷면에는 오래된 차용증이 겹쳐 붙어 있다.\n\n\"동전부터 보시죠. 밖에서는 이 값에 못 구합니다.\"\n\n물건을 살펴볼까?");
        var settings=AssetDatabase.LoadAssetAtPath<ExplorationSettings>(ExplorationPreparation.SettingsPath);Backup(AssetDatabase.GetAssetPath(settings));
        settings.secretWallSearches=5;settings.annexEventChance=.30f;
        foreach(var a in art.art)
        {
            foreach(var path in new[]{a.front,a.side,a.rear,a.usedFront,a.usedSide,a.usedRear}.Where(p=>!string.IsNullOrEmpty(p)).Distinct())Import(path);
            var obj=Asset<RoomObjectData>(Objects+"Data_RoomObject_"+a.name+".asset");obj.front=Sprite(a.front);obj.side=Sprite(a.side);obj.rear=Sprite(a.rear);
            obj.usedFront=Sprite(a.usedFront);obj.usedSide=Sprite(a.usedSide);obj.usedRear=Sprite(a.usedRear);obj.displayHeight=a.height;obj.facePlayerOnFirstEncounter=true;
            EditorUtility.SetDirty(obj);
        }
        foreach(var door in art.doors)
        {
            Import(door.path);var obj=Asset<RoomObjectData>(Objects+"Data_RoomObject_"+door.name+".asset");
            obj.front=obj.side=obj.rear=Sprite(door.path);obj.fitsDoorway=true;obj.wallMounted=true;obj.displayHeight=400;
            obj.labelKey=door.name=="보스입구"?"ui.explore.bossdoor":"ui.explore.elitedoor";
            Text(obj.labelKey,door.name=="보스입구"?"사자 문양의 겹문":"교차한 칼날의 철문");EditorUtility.SetDirty(obj);
            if(door.name=="보스입구")settings.bossEntrance=obj;else settings.eliteEntrance=obj;
        }
        var wall=Asset<RoomObjectData>(Objects+"Data_RoomObject_비밀벽.asset");
        wall.front=wall.side=wall.rear=AssetDatabase.LoadAssetAtPath<FloorArtwork>("Assets/Data/Presentation/Angular/B1.asset").walls;
        wall.fitsDoorway=true;wall.wallMounted=true;wall.labelKey="ui.explore.secretwall";Text(wall.labelKey,"돌벽");settings.hiddenWall=wall;EditorUtility.SetDirty(wall);EditorUtility.SetDirty(settings);
        foreach(var interior in art.interiors){Import(interior.path);if(string.IsNullOrEmpty(interior.@event)){var floor=Asset<FloorArtwork>("Assets/Data/Presentation/Angular/B"+interior.act+".asset");floor.bossInterior=Sprite(interior.path);EditorUtility.SetDirty(floor);}}
        var merchant=AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/Combat/Monsters/Data_Monster_Special_금먹는두꺼비.asset");
        var merchantWidth=merchant.ReferenceArtwork.rect.width*100f/merchant.ReferenceArtwork.pixelsPerUnit;
        CombatSpriteAnimation banditAnimation=null;
        foreach(var c in art.combat)
        {
            var logicalWidth=c.name=="후드행상변이"?merchantWidth:400;
            foreach(var path in new[]{c.idle,c.ready,c.attack,c.dead})Import(path,logicalWidth);
            var library=c.name=="후드행상변이"?merchant.combatAnimation:Asset<CombatSpriteAnimation>("Assets/Data/Presentation/Animations/Data_MonsterAnimation_회수꾼도적.asset");
            Backup(AssetDatabase.GetAssetPath(library));library.idle=Sprite(c.idle);library.ready=Sprite(c.ready);library.attack=Sprite(c.attack);library.dead=Sprite(c.dead);
            library.stunned=null;library.illustratedLayout=true;library.tightPoseFrames=true;library.canvasScale=1;library.canvasPaddingPixels=Vector4.zero;EditorUtility.SetDirty(library);
            if(c.name=="후드행상변이"){Backup(AssetDatabase.GetAssetPath(merchant));merchant.portrait=library.idle;EditorUtility.SetDirty(merchant);}else banditAnimation=library;
        }
        var bandit=PrepareBandit(banditAnimation);
        if(art.companion!=null)
        {
            Import(art.companion.idle);Import(art.companion.attack);
            var dog=Asset<CompanionData>("Assets/Data/Characters/Companions/Data_Companion_길동무.asset");
            dog.combatSprite=Sprite(art.companion.idle);var library=dog.combatAnimation;Backup(AssetDatabase.GetAssetPath(library));
            library.idle=dog.combatSprite;library.attack=Sprite(art.companion.attack);library.canvasScale=1;library.canvasPaddingPixels=Vector4.zero;library.illustratedLayout=true;
            EditorUtility.SetDirty(library);EditorUtility.SetDirty(dog);
        }
        var stories=AssetDatabase.FindAssets("t:RandomEventData",new[]{"Assets/Data/Dungeon/Events"}).Select(g=>AssetDatabase.LoadAssetAtPath<RandomEventData>(AssetDatabase.GUIDToAssetPath(g))).ToDictionary(e=>e.id);
        foreach(var p in contentPlan.events)
        {
            if(!stories.TryGetValue(p.id,out var story)){story=Asset<RandomEventData>("Assets/Data/Dungeon/Events/Data_Event_이름_없는_회수표.asset");story.id=p.id;story.minAct=p.minAct;story.maxAct=p.maxAct;story.discovery=EventDiscovery.Automatic;story.illustratedPopup=true;story.minNormalCombatsInAct=3;story.referenceExperience="2026-11-01 사용자 제안: 회수업을 가장해 원정객을 노리는 도적의 적대 조우. 전투/갈취 회피/주문 위협의 선택.";stories.Add(story.id,story);}
            Backup(AssetDatabase.GetAssetPath(story));story.displayName=p.displayName;story.encounterKind=(EventEncounterKind)p.kind;story.authoringLore=p.lore;story.weight=p.weight;story.roomLayout=(EventRoomLayout)p.layout;
            if(p.forbiddenFlag!=null)story.forbiddenFlag=p.forbiddenFlag;
            if(p.narrative!=null)story.narrative=p.narrative;
            foreach(var pg in p.pages??Array.Empty<PagePatch>())story.pages[pg.index].narrative=pg.text;
            if(story.pages.Count>0)story.narrative=story.pages[0].narrative;
            if(p.id=="Shelter")story.worldObject=AssetDatabase.LoadAssetAtPath<RoomObjectData>(Objects+"Data_RoomObject_버려진호위용병.asset");
            if(!string.IsNullOrEmpty(p.art))story.worldObject=AssetDatabase.LoadAssetAtPath<RoomObjectData>(Objects+"Data_RoomObject_"+p.art+".asset");
            story.worldObject.labelKey="ui.lore.object."+story.id;Text(story.worldObject.labelKey,story.displayName);EditorUtility.SetDirty(story.worldObject);
            if(story.encounterKind==EventEncounterKind.Person)story.worldObject.displayHeight=p.id=="Shelter"?340:p.id is "QuietPrayer" or "LostBeliever" or "Tooth"?story.worldObject.displayHeight:400;
            story.illustration=story.worldObject.front;foreach(var pg in story.pages)pg.illustration=story.illustration;
            story.roomInteriors=new Sprite[3];foreach(var entry in art.interiors.Where(i=>i.@event==story.id))story.roomInteriors[entry.act-1]=Sprite(entry.path);
            foreach(var patch in p.options??Array.Empty<OptionPatch>())
            {
                var list=patch.page<0?story.options:story.pages[patch.page].options;
                EventOption option;if(patch.add){option=list.FirstOrDefault(o=>o.title==patch.title);if(option==null){option=new EventOption();list.Add(option);}}else option=list[patch.index];
                option.title=patch.title;if(!string.IsNullOrEmpty(patch.description))option.description=patch.description;option.resultText=patch.resultText;
                if(patch.goldCost>=0)option.goldCost=patch.goldCost;if(patch.hpCost>=0)option.hpCost=patch.hpCost;
                if(patch.add||patch.terminal)option.nextPage=-1;
                option.removeWorldObject=patch.remove;option.useWorldObject=patch.used;option.resultingWorldObject=null;
                if(patch.effects!=null)option.effects=patch.effects.Select(Effect).ToList();
                if(patch.requirements!=null)option.requirements=patch.requirements.Select(r=>new EventRequirement{kind=(EventRequirementKind)r.kind,amount=r.amount,flag=r.flag,invert=r.invert}).ToList();
                option.revisitTextKey="ui.lore.revisit."+story.id+"."+patch.page+"."+list.IndexOf(option);
                Text(option.revisitTextKey,!string.IsNullOrEmpty(patch.revisit)?patch.revisit:patch.remove?"이곳을 떠난 뒤다.":"이야기는 끝났다. 더 말을 걸지 않기로 했다.");
            }
            EditorUtility.SetDirty(story);Sync(story,csv);
        }
        foreach(var guid in AssetDatabase.FindAssets("t:GameContent"))
        {
            var c=Asset<GameContent>(AssetDatabase.GUIDToAssetPath(guid));if(!c.events.Contains(stories["RecoveryCutpurse"]))c.events.Add(stories["RecoveryCutpurse"]);EditorUtility.SetDirty(c);
        }
        foreach(var guid in AssetDatabase.FindAssets("t:DungeonGenerationSettings"))
        {var generation=Asset<DungeonGenerationSettings>(AssetDatabase.GUIDToAssetPath(guid));if(generation.exploration==null)continue;generation.eventRoomCount=2;generation.minimumShopRooms=1;EditorUtility.SetDirty(generation);}
        Sync(bandit,csv);Sync(merchant,csv);
        foreach(var attack in bandit.attacks)Sync(attack,csv);
        Sync(AssetDatabase.LoadAssetAtPath<EncounterData>("Assets/Data/Dungeon/Encounters/Data_Encounter_회수꾼도적.asset"),csv);
        PrepareScenes(settings,stories.Values.Where(e=>!e.excludedFromRandomPool));
        File.WriteAllText(BehemothTextEditor.CsvPath,csv.ExportCsv(),new UTF8Encoding(true));Object.DestroyImmediate(csv);
        EditorUtility.SetDirty(BehemothTextEditor.Table);AssetDatabase.SaveAssets();
        File.WriteAllText(Folder+"Installed.txt","Saved 20 events/lore/choice aftermath, one expanded supplier per floor, separate secret masonry and locked doors, entrances and 9 room interiors, coherent dog/merchant combat art, saved F1 and shop page controls.\n");
        if(File.Exists(Folder+"PreparationError.txt"))File.Delete(Folder+"PreparationError.txt");
    }
    static EventEffect Effect(EffectPatch e)=>new(){kind=(EventEffectKind)e.kind,amount=e.amount,flag=e.flag,stat=(ModifierKind)e.stat,
        relic=string.IsNullOrEmpty(e.asset)?Refer<EquipmentData>(e.relic):(EventEffectKind)e.kind==EventEffectKind.Relic?AssetDatabase.LoadAssetAtPath<EquipmentData>(e.asset):null,
        encounter=string.IsNullOrEmpty(e.asset)?Refer<EncounterData>(e.encounter):(EventEffectKind)e.kind==EventEffectKind.Fight?AssetDatabase.LoadAssetAtPath<EncounterData>(e.asset):null,
        spell=string.IsNullOrEmpty(e.asset)?Refer<SpellData>(e.spell):(EventEffectKind)e.kind==EventEffectKind.LearnSpell?AssetDatabase.LoadAssetAtPath<SpellData>(e.asset):null,
        spellPool=(e.spellPool??Array.Empty<Ref>()).Select(Refer<SpellData>).Where(s=>s!=null).ToList(),spellChoiceCount=Mathf.Max(1,e.spellChoiceCount)};
    static void Sync(LocalizedData data,GameTextDatabase csv)
    {BehemothTextEditor.SyncAsset(data);foreach(var key in data.textKeys)csv.Set(key.key,BehemothTextEditor.Table.Get(key.key),"Exploration lore");}
    static MonsterData PrepareBandit(CombatSpriteAnimation library)
    {
        var path="Assets/Data/Combat/Monsters/Data_Monster_Special_회수꾼도적.asset";
        var monster=AssetDatabase.LoadAssetAtPath<MonsterData>(path);
        if(monster==null){monster=Object.Instantiate(AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/Combat/Monsters/Data_Monster_B1_Normal_해골전사.asset"));monster.textKeys.Clear();monster.textOwnerGuid="";AssetDatabase.CreateAsset(monster,path);}
        monster.displayName="회수꾼 도적";monster.encounterHint="칼을 당길 때 드러나는 약점을 노리자.";monster.combatAnimation=library;monster.portrait=library.idle;monster.combatScale=1;
        // A newly authored human body. Existing species' tuning and collision data are untouched.
        monster.hitBoxes=new(){new(){center=new Vector2(0,115),radius=43},new(){center=new Vector2(0,15),radius=70},new(){center=new Vector2(0,-92),radius=62}};
        monster.attacks=monster.attacks.Select((a,i)=>
        {
            var name=i==0?"그늘 베기":"급소 찌르기";var target="Assets/Data/Combat/Attacks/Data_Attack_회수꾼도적_"+(i==0?"Normal":"Charge")+".asset";
            var attack=AssetDatabase.LoadAssetAtPath<MonsterAttackData>(target);
            if(attack==null&&AssetDatabase.GetAssetPath(a).Contains("Data_Attack_회수꾼도적_"))attack=a;
            if(attack==null){attack=Object.Instantiate(a);attack.textKeys.Clear();attack.textOwnerGuid="";AssetDatabase.CreateAsset(attack,target);}
            attack.displayName=name;
            if(attack.type==MonsterAttackType.Charge)attack.chargeWeakPoints=new(){new(){id="head",localPosition=new Vector2(-20,112),hitRadius=30},new(){id="knife-hand",localPosition=new Vector2(90,12),hitRadius=30}};
            EditorUtility.SetDirty(attack);return attack;
        }).ToList();monster.normalAttackSequence.Clear();EditorUtility.SetDirty(monster);
        var encounter=Asset<EncounterData>("Assets/Data/Dungeon/Encounters/Data_Encounter_회수꾼도적.asset");encounter.id="RecoveryCutpurse";encounter.displayName="회수꾼도적";encounter.hint=monster.encounterHint;encounter.category=RoomEncounterType.Combat;encounter.members=new(){new(){monster=monster}};EditorUtility.SetDirty(encounter);
        BehemothTextEditor.SyncAsset(encounter);foreach(var a in monster.attacks)BehemothTextEditor.SyncAsset(a);return monster;
    }
    static T RefField<T>(object source,string name)=>(T)source.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(source);
    static void PrepareScenes(ExplorationSettings settings,IEnumerable<RandomEventData> events)
    {
        foreach(var path in new[]{"Assets/Scenes/MainGameScene.unity","Assets/Scenes/Trials/Angular01_Main.unity"})
        {
            Backup(path);var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);var loaded=scene.isLoaded;if(!loaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var roots=scene.GetRootGameObjects();
                var overlay=roots.SelectMany(r=>r.GetComponentsInChildren<DebugCheatOverlay>(true)).Single();
                overlay.roomObjectCatalog=settings.roomObjects.Select(b=>b.artwork).Concat(events.Select(e=>e.worldObject)).Concat(new[]{settings.key,settings.lockedDoor,settings.hiddenWall,settings.eliteEntrance,settings.bossEntrance}).Where(a=>a!=null).Distinct().OrderBy(a=>a.name).ToList();EditorUtility.SetDirty(overlay);
                foreach(var view in roots.SelectMany(r=>r.GetComponentsInChildren<ExplorationView>(true)))
                {
                    var slots=view.slots.ToList();while(slots.Count<8)
                    {
                        var rect=new GameObject("World Object "+(slots.Count+1),typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(view.objectLayer,false);
                        var slot=rect.gameObject.AddComponent<RoomObjectSlot>();var paint=new GameObject("Painting",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<Image>();paint.transform.SetParent(rect,false);paint.raycastTarget=false;slot.painting=paint;
                        var hit=new GameObject("Interaction Area",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button)).GetComponent<Image>();hit.transform.SetParent(view.interactionLayer,false);hit.color=new Color(0,0,0,.001f);slot.hitArea=hit.rectTransform;slot.button=hit.GetComponent<Button>();slot.button.targetGraphic=hit;slot.button.transition=Selectable.Transition.None;hit.gameObject.AddComponent<RoomObjectPointer>().slot=slot;slot.PreparePresentation();slot.Hide();slots.Add(slot);
                    }
                    view.slots=slots.ToArray();EditorUtility.SetDirty(view);
                }
                foreach(var flow in roots.SelectMany(r=>r.GetComponentsInChildren<GameFlowController>(true)))
                {var so=new SerializedObject(flow);so.FindProperty("merchantEventSprite").objectReferenceValue=settings.For(RoomEncounterType.Shop).front;so.ApplyModifiedPropertiesWithoutUndo();}
                foreach(var shop in roots.SelectMany(r=>r.GetComponentsInChildren<ShopView>(true)))
                {
                    var panel=RefField<GameObject>(shop,"panel");var template=RefField<Button>(shop,"leaveButton");
                    shop.previousPage=PageButton(panel.transform,template,"Previous Stock",-270,"ui.shop.previous");shop.nextPage=PageButton(panel.transform,template,"Next Stock",270,"ui.shop.next");
                    shop.pageLabel=PageText(panel.transform,template,"Stock Page",0,"ui.shop.page");EditorUtility.SetDirty(shop);
                }
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            finally{if(!loaded)EditorSceneManager.CloseScene(scene,true);}
        }
    }
    static TextMeshProUGUI PageText(Transform parent,Button template,string name,float x,string key)
    {
        var found=parent.Find(name);var label=found!=null?found.GetComponent<TextMeshProUGUI>():null;
        if(label==null){label=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();label.transform.SetParent(parent,false);}
        var src=template.GetComponentInChildren<TextMeshProUGUI>(true);label.font=src.font;label.fontSharedMaterial=src.fontSharedMaterial;label.fontSize=32;label.color=src.color;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;
        var rect=label.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(x,-80);rect.sizeDelta=new Vector2(200,64);
        var binding=label.GetComponent<GameTextBinding>()??label.gameObject.AddComponent<GameTextBinding>();binding.key=key;label.text=GameText.Get(key);return label;
    }
    static Button PageButton(Transform parent,Button template,string name,float x,string key)
    {
        var found=parent.Find(name);var button=found!=null?found.GetComponent<Button>():null;
        if(button==null){button=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button)).GetComponent<Button>();button.transform.SetParent(parent,false);}
        var image=button.GetComponent<Image>();image.color=Color.black;image.raycastTarget=true;button.targetGraphic=image;button.colors=template.colors;
        var rect=(RectTransform)button.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(x,-80);rect.sizeDelta=new Vector2(200,72);
        var label=PageText(button.transform,template,"Label",0,key);label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;
        var original=template.GetComponentInChildren<InkUiSurface>(true);if(original!=null){var skin=button.GetComponentInChildren<InkUiSurface>(true);if(skin==null){skin=new GameObject("Ink Surface",typeof(RectTransform),typeof(CanvasRenderer),typeof(InkUiSurface)).GetComponent<InkUiSurface>();skin.transform.SetParent(button.transform,false);skin.rectTransform.anchorMin=Vector2.zero;skin.rectTransform.anchorMax=Vector2.one;skin.rectTransform.offsetMin=skin.rectTransform.offsetMax=Vector2.zero;skin.transform.SetAsFirstSibling();}skin.style=original.style;skin.source=image;skin.control=button;skin.role=original.role;skin.raycastTarget=false;var feedback=button.GetComponent<InkUiFeedback>()??button.gameObject.AddComponent<InkUiFeedback>();feedback.surface=skin;}
        return button;
    }
}
