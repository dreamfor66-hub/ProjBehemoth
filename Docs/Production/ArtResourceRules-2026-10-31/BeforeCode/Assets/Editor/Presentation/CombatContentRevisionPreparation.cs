using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DungeonSlash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

// One-time, backed-up asset migration. Saved scenes and data remain fully designer-editable.
[InitializeOnLoad]
internal static class CombatContentRevisionPreparation
{
    internal const string Folder=CombatContentRevisionTools.Folder;
    static bool busy;
    [Serializable] class Frame {public string state,image;public float ppu;public Vector2 pivot;}
    [Serializable] class Painting {public string oldName,name;public Frame[] frames;public List<MonsterHitBox> hitBoxes;public List<Vector2> weakPoints;}
    [Serializable] class ArtPlan {public Painting[] monsters;}
    static CombatContentRevisionPreparation()=>EditorApplication.update+=Poll;
    static void Poll()
    {
        if(busy||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Folder+"Ready.txt")||File.Exists(Folder+"Installed.txt")||File.Exists(Folder+"InstallError.txt"))return;
        busy=true;
        try{Install();File.WriteAllText(Folder+"Installed.txt","Saved content, framing, authored poses, independent supports, spell choices and fatigue thresholds installed.\n");}
        catch(Exception e){File.WriteAllText(Folder+"InstallError.txt",e.ToString());Debug.LogException(e);}
        finally{busy=false;}
    }
    internal static void Backup(string path)
    {
        if(string.IsNullOrEmpty(path)||!File.Exists(path))return;
        var copy=Folder+"Before/"+path;
        if(File.Exists(copy))return;
        Directory.CreateDirectory(Path.GetDirectoryName(copy));File.Copy(path,copy);
        if(File.Exists(path+".meta"))File.Copy(path+".meta",copy+".meta",true);
    }
    static T[] All<T>()where T:Object=>AssetDatabase.FindAssets("t:"+typeof(T).Name,new[]{"Assets/Data"}).Select(g=>AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g))).Where(a=>a!=null).ToArray();
    static MonsterData Monster(string old)=>DesignerAssetPaths.Load<MonsterData>("Assets/Data/Combat/Monsters/"+old+".asset");
    static void Dirty(Object value){EditorUtility.SetDirty(value);}
    static void Install()
    {
        if(!File.Exists(Folder+"ContentSaved.txt"))
        {
        Backup(DesignerAssetPaths.IndexPath);
        foreach(var data in All<MonsterData>())
        {
            Backup(AssetDatabase.GetAssetPath(data));Backup(AssetDatabase.GetAssetPath(data.combatAnimation));
            foreach(var attack in data.attacks.Where(a=>a!=null))Backup(AssetDatabase.GetAssetPath(attack));
        }
        // Revert the previous enlargement choices for existing bosses; new paintings set their own native scale below.
        foreach(var old in new[]{"BellKeeper","CrystalSpider","AshGeneral","EclipseBeast"})
        {var data=Monster(old);data.combatScale=1;Dirty(data);}
        InstallPaintings();InstallSupports();InstallTeacher();InstallFatigue();InstallScenes();
        AssetDatabase.SaveAssets();File.WriteAllText(Folder+"ContentSaved.txt","Saved content before path migration.\n");
        }
        DesignerDataNaming.Normalize();NormalizeArtNames();
        BehemothTextEditor.ExportDefault();AssetDatabase.SaveAssets();
    }
    internal static void InstallPaintings()
    {
        var plan=JsonUtility.FromJson<ArtPlan>(File.ReadAllText(Folder+"ArtPlan.json"));
        foreach(var painting in plan.monsters)
        {
            var data=Monster(painting.oldName);var library=data.combatAnimation;
            foreach(var frame in painting.frames)
            {
                AssetDatabase.ImportAsset(frame.image,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(frame.image);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.maxTextureSize=4096;importer.npotScale=TextureImporterNPOTScale.None;importer.spritePixelsPerUnit=frame.ppu;importer.filterMode=FilterMode.Bilinear;
                importer.SaveAndReimport();var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(frame.image);
                var state=(CombatVisualState)Enum.Parse(typeof(CombatVisualState),frame.state);
                var existing=state==CombatVisualState.Stunned?null:library.For(state);
                var path="Assets/Art/Monsters/"+painting.name+"/"+painting.name+"-"+frame.state+".asset";
                var generated=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),frame.pivot,frame.ppu,0,SpriteMeshType.FullRect);
                if(existing!=null)
                {
                    Backup(AssetDatabase.GetAssetPath(existing));generated.name=existing.name;
                    EditorUtility.CopySerialized(generated,existing);Object.DestroyImmediate(generated);Dirty(existing);
                }
                else
                {
                    existing=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if(existing!=null){generated.name=existing.name;EditorUtility.CopySerialized(generated,existing);Object.DestroyImmediate(generated);Dirty(existing);}
                    else{existing=generated;existing.name=Path.GetFileNameWithoutExtension(path);AssetDatabase.CreateAsset(existing,path);}
                }
                if(state==CombatVisualState.Idle)library.idle=existing;
                else if(state==CombatVisualState.Ready)library.ready=existing;
                else if(state==CombatVisualState.Attack)library.attack=existing;
                else library.stunned=existing;
            }
            library.dead=library.idle;library.canvasScale=1;library.canvasPaddingPixels=Vector4.zero;library.tightPoseFrames=true;library.illustratedLayout=true;
            data.combatScale=1;data.hitBoxes=painting.hitBoxes;
            foreach(var attack in data.GetAttacks(MonsterAttackType.Charge))
            {
                if(attack.chargeWeakPoints.Count!=painting.weakPoints.Count)throw new InvalidDataException(data.displayName+" weak-point count changed");
                for(var n=0;n<attack.chargeWeakPoints.Count;n++)attack.chargeWeakPoints[n].localPosition=painting.weakPoints[n];
                Dirty(attack);
            }
            Dirty(library);Dirty(data);
        }
    }
    static void InstallSupports()
    {
        var report=new StringBuilder("Support recovery is independent of charge resolution; one missing slot per interval. Stun pauses recovery.\n");
        var encounters=All<EncounterData>();
        var bosses=All<StageSetData>().SelectMany(s=>s.bosses).Where(e=>e!=null).SelectMany(e=>e.members).Select(m=>m.monster).Where(m=>m!=null&&!m.diesWithLeader).Distinct().ToArray();
        foreach(var boss in bosses)
        {
            var roster=boss.GetAttacks(MonsterAttackType.Charge).SelectMany(a=>a.chargeSummonMechanics.openingSummons).Where(m=>m!=null).ToList();
            if(roster.Count==0)
            {
                var formation=encounters.FirstOrDefault(e=>e.members.Any(m=>m.monster==boss)&&e.members.Any(m=>m.monster!=boss&&m.monster!=null&&m.monster.diesWithLeader));
                if(formation!=null)roster=formation.members.Where(m=>m.monster!=boss&&m.monster!=null&&m.monster.diesWithLeader).Select(m=>m.monster).ToList();
            }
            if(roster.Count==0)continue;
            boss.supportRoster=roster;boss.reinforcementInterval=12;Dirty(boss);
            foreach(var attack in boss.GetAttacks(MonsterAttackType.Charge))
            {
                attack.requiredSupportId="";
                attack.chargeSummonMechanics.openingSummons.Clear();attack.chargeSummonMechanics.summonOnFailedCharge=null;attack.chargeSummonMechanics.healOnFailedChargeWhileSummonAlive=0;Dirty(attack);
            }
            report.AppendLine(boss.displayName+": "+string.Join(", ",roster.Select(m=>m.displayName))+"; restore one every 12s while a slot is empty, charge damage/timing/hits preserved.");
        }
        File.WriteAllText(Folder+"Supports.txt",report.ToString());
    }
    static void InstallTeacher()
    {
        var spells=All<SpellData>();SpellData Spell(string id)=>spells.Single(s=>s.id==id);
        var pool=new[]{"Ember","Spark","EnergyBolt","FrostRing","PoisonMist","Cleanse","EnchantIce","LightningMark"}.Select(Spell).ToList();
        var stories=All<RandomEventData>();var first=stories.Single(e=>e.id=="MagicTeacherFirst");var follow=stories.Single(e=>e.id=="MagicTeacherReturn");
        Backup(AssetDatabase.GetAssetPath(first));Backup(AssetDatabase.GetAssetPath(follow));
        EventOption Choice(string flag)=>new(){title="배울 주문을 고른다.",description="노인이 보여 주는 기초 주문 세 가지 중 하나를 배운다.",effects=new(){new(){kind=EventEffectKind.SpellChoice,spellPool=pool.ToList(),spellChoiceCount=3,flag=flag},new(){kind=EventEffectKind.SetFlag,flag=flag=="TeacherFirstSpell"?"TeacherLesson1":"TeacherLesson2"}}};
        EventOption Leave()=>new(){title="떠난다.",resultText="노인은 펼쳐 둔 책을 다시 챙겼다."};
        foreach(var index in new[]{2,3})
        {
            first.pages[index].narrative="노인은 낡은 책 몇 권을 책상 위에 펼친다.\n\n불꽃을 다루는 법만 있는 건 아닙니다. 적의 발을 묶거나 몸에 남은 해로운 마력을 걷어 내는 주문도 있지요.\n\n지금 가르칠 수 있는 것들을 보여 드리겠습니다. 마음이 가는 쪽을 골라 보세요.";
            first.pages[index].options=new(){Choice("TeacherFirstSpell"),Leave()};first.pages[index].routes.Clear();
        }
        first.pages[4].routes.Clear();
        follow.pages[1].narrative="지난번 주문을 손에 익혔다면, 이제 다른 쓰임새도 살펴봅시다.\n\n한 주문을 깊이 파고들어도 좋고, 빈틈을 메워 줄 기초를 더 배워도 됩니다.";
        follow.pages[1].options.Insert(0,Choice("TeacherSecondSpell"));
        var lessons=new Dictionary<string,string>{
            ["Ember"]="손끝의 불꽃이 전보다 안정됐군요. 이번에는 그 불이 퍼지는 방식을 살펴봅시다.",
            ["Spark"]="짧은 번개를 끊어 내는 솜씨가 좋아졌군요. 이제 마력이 이어지는 길도 읽어 봅시다.",
            ["EnergyBolt"]="마력탄의 흐름이 곧아졌군요. 힘을 모을 때와 흘려보낼 때를 구분할 차례입니다.",
            ["FrostRing"]="발밑에 서리를 두르는 법을 익혔군요. 거리를 지키는 주문을 더 살펴볼까요?",
            ["PoisonMist"]="안개가 머무는 곳을 고르는군요. 힘을 오래 남기는 법을 알아보면 좋겠습니다.",
            ["Cleanse"]="상처에 남은 마력을 읽는 손이 됐군요. 지우는 법을 배웠으니, 이번에는 마력을 남기는 법도 살펴봅시다.",
            ["EnchantIce"]="무기에 실은 냉기가 안정됐군요. 다른 속성을 머무르게 하는 법도 배워 볼까요?",
            ["LightningMark"]="번개를 부를 자리를 잘 고르는군요. 이제 흩어진 마력을 이어 보는 게 좋겠습니다."};
        foreach(var spell in pool)
        {
            var index=follow.pages.Count;
            follow.pages.Add(new EventPage{narrative="연구실 안에서 익숙한 목소리가 들린다.\n\n노인은 내가 지난번에 골랐던 주문을 떠올리며 손을 살핀다.\n\n"+lessons[spell.id],options=new(){new(){title="다음 수업을 부탁한다.",nextPage=spell.id=="EnchantIce"||spell.id=="Cleanse"?2:1},Leave()}});
            follow.pages[0].routes.Add(new(){requirements=new(){new(){kind=EventRequirementKind.ChosenSpell,flag="TeacherFirstSpell",spell=spell}},page=index});
        }
        BehemothTextEditor.SyncAsset(first);BehemothTextEditor.SyncAsset(follow);Dirty(first);Dirty(follow);
    }
    static void InstallFatigue()
    {
        foreach(var settings in All<FatigueSettings>())
        {
            Backup(AssetDatabase.GetAssetPath(settings));
            settings.attackSpeed=new(new Keyframe(0,.35f),new Keyframe(10,.55f),new Keyframe(30,.7f),new Keyframe(50,.86f),new Keyframe(60,.95f),new Keyframe(70,1),new Keyframe(100,1));
            settings.finalDamage=new(new Keyframe(0,.2f),new Keyframe(10,.65f),new Keyframe(30,.85f),new Keyframe(45,1),new Keyframe(100,1));
            Dirty(settings);
        }
        BehemothTextEditor.Table.Set("ui.fatigue.normal","여유");
    }
    internal static void InstallScenes()
    {
        foreach(var path in new[]{"Assets/Scenes/MainGameScene.unity","Assets/Scenes/Trials/Angular01_Main.unity"})
        {
            var scene=SceneManager.GetSceneByPath(path);var loaded=scene.isLoaded;
            if(!loaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            Backup(path);
            if(scene.isDirty)EditorSceneManager.SaveScene(scene,Folder+"Before/Unsaved-"+DateTime.UtcNow.Ticks+"-"+Path.GetFileName(path),true);
            foreach(var view in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CombatSceneView>(true)))
            {
                var safe=view.LogicalArea.Find("MonsterSafeArea") as RectTransform;
                if(safe==null){safe=(RectTransform)new GameObject("MonsterSafeArea",typeof(RectTransform)).transform;safe.SetParent(view.LogicalArea,false);}
                safe.anchorMin=new Vector2(.105f,.255f);safe.anchorMax=new Vector2(.865f,.892f);safe.offsetMin=safe.offsetMax=Vector2.zero;
                view.monsterSafeArea=safe;Dirty(view);
                var captions=view.LogicalArea.Find("MonsterCaptionOverlay") as RectTransform;
                if(captions==null){captions=(RectTransform)new GameObject("MonsterCaptionOverlay",typeof(RectTransform)).transform;captions.SetParent(view.LogicalArea,false);}
                captions.anchorMin=Vector2.zero;captions.anchorMax=Vector2.one;captions.offsetMin=captions.offsetMax=Vector2.zero;
                var actors=view.LogicalArea.Find("Actors");
                if(actors!=null)captions.SetSiblingIndex(actors.GetSiblingIndex()+1);else captions.SetAsLastSibling();
                view.monsterCaptionRoot=captions;Dirty(view);
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            if(!loaded)EditorSceneManager.CloseScene(scene,true);
        }
    }
    internal static void NormalizeArtNames()
    {
        var index=JsonUtility.FromJson<DesignerAssetPaths.Index>(File.ReadAllText(DesignerAssetPaths.IndexPath));
        var monsters=All<MonsterData>();
        var tokens=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        foreach(var monster in monsters)
        {
            var compact=DesignerDataNaming.MonsterName(monster.displayName);
            var row=index.entries.FirstOrDefault(e=>e.guid==AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(monster)));
            if(row!=null)foreach(var old in row.aliases.Concat(new[]{row.oldPath}))tokens[Path.GetFileNameWithoutExtension(old)]=compact;
            if(row!=null)
            {
                var original=Path.GetFileNameWithoutExtension(row.oldPath);
                var legacy=Regex.Match(original,@"[A-Za-z][A-Za-z0-9]*$").Value;
                if(!string.IsNullOrEmpty(legacy))foreach(var token in new[]{legacy,"Enemy_"+legacy,"Enemy"+legacy})tokens[token]=compact;
            }
            tokens[monster.displayName.Replace(' ','_')]=compact;
        }
        foreach(var entry in AngularWorldPreparation.Entries())tokens[entry.id]=DesignerDataNaming.MonsterName(entry.name);
        foreach(var folder in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/Art/Enemies/Combat/")&&AssetDatabase.IsValidFolder(p)).OrderByDescending(p=>p.Length).ToArray())
        {
            var leaf=Path.GetFileName(folder);if(!tokens.TryGetValue(leaf,out var compact)||leaf==compact)continue;
            var target=Path.GetDirectoryName(folder).Replace('\\','/')+"/"+compact;
            var error=AssetDatabase.MoveAsset(folder,target);if(!string.IsNullOrEmpty(error))throw new IOException(error);
            foreach(var row in index.entries.Where(e=>e.path.StartsWith(folder+"/")))
            {row.aliases??=new();row.aliases.Add(row.path);row.path=target+row.path.Substring(folder.Length);}
        }
        var moves=new List<(string old,string path)>();
        foreach(var path in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/Art/")||p.StartsWith("Assets/Data/Presentation/")).Where(p=>!AssetDatabase.IsValidFolder(p)).ToArray())
        {
            // Original style references are evidence, rather than project character production assets.
            if(path.StartsWith("Assets/Art/References/"))continue;
            var stem=Path.GetFileNameWithoutExtension(path);var updated=stem;
            var parent=Path.GetFileName(Path.GetDirectoryName(path));
            if(path.StartsWith("Assets/Art/Enemies/Combat/")&&monsters.Any(m=>DesignerDataNaming.MonsterName(m.displayName)==parent)&&Enum.TryParse<CombatVisualState>(stem,out _))updated=parent+"-"+stem;
            foreach(var token in tokens.OrderByDescending(t=>t.Key.Length))
            {
                if(string.IsNullOrEmpty(token.Key)||token.Key==token.Value)continue;
                if(stem==token.Key||stem.StartsWith(token.Key+"-",StringComparison.OrdinalIgnoreCase)||stem.StartsWith(token.Key+"_",StringComparison.OrdinalIgnoreCase))
                {updated=token.Value+stem.Substring(token.Key.Length);break;}
            }
            // Animation libraries use the species name even when their source was a former concept ID.
            var animation=AssetDatabase.LoadAssetAtPath<CombatSpriteAnimation>(path);
            var owner=animation!=null?monsters.FirstOrDefault(m=>m.combatAnimation==animation):null;
            if(owner!=null)updated="Data_MonsterAnimation_"+DesignerDataNaming.MonsterName(owner.displayName);
            if(updated==stem)continue;
            var target=Path.GetDirectoryName(path).Replace('\\','/')+"/"+updated+Path.GetExtension(path);
            if(File.Exists(target)&&target!=path)target=Path.GetDirectoryName(path).Replace('\\','/')+"/"+updated+"-"+AssetDatabase.AssetPathToGUID(path).Substring(0,6)+Path.GetExtension(path);
            moves.Add((path,target));
        }
        var report=new StringBuilder("# 몬스터 아트와 데이터 경로\n\n몬스터 이름의 공백은 파일명에서 제거한다. 원본·초상화·상태 프레임은 같은 한국어 이름으로 검색한다. 이전 경로는 GUID 별칭으로 보존한다.\n\n");
        foreach(var move in moves)
        {
            var guid=AssetDatabase.AssetPathToGUID(move.old);var error=AssetDatabase.MoveAsset(move.old,move.path);if(!string.IsNullOrEmpty(error))throw new IOException(error);
            var asset=AssetDatabase.LoadMainAssetAtPath(move.path);if(asset!=null&&!(asset is Texture2D)){asset.name=Path.GetFileNameWithoutExtension(move.path);Dirty(asset);}
            var row=index.entries.FirstOrDefault(e=>e.guid==guid);
            if(row==null){row=new(){guid=guid,oldPath=move.old,path=move.path,type=asset!=null?asset.GetType().Name:"Art",displayName=Path.GetFileNameWithoutExtension(move.path)};index.entries.Add(row);}
            row.aliases??=new();if(!row.aliases.Contains(move.old))row.aliases.Add(move.old);row.path=move.path;
            report.AppendLine("- `"+move.old+"` → `"+move.path+"`");
        }
        foreach(var row in index.entries)
        {var current=AssetDatabase.GUIDToAssetPath(row.guid);if(!string.IsNullOrEmpty(current)&&current!=row.path){row.aliases??=new();row.aliases.Add(row.path);row.path=current;}}
        DesignerAssetPaths.SaveIndex(index);
        File.AppendAllText(Folder+"ArtNames.md",report.ToString());
    }
    internal static void OrganizeCurrentPaintings()
    {
        Directory.CreateDirectory("Assets/Art/Archive/RejectedWide");Directory.CreateDirectory("Assets/Art/Archive/PreviousPoses");
        var plan=JsonUtility.FromJson<ArtPlan>(File.ReadAllText(Folder+"ArtPlan.json"));
        foreach(var painting in plan.monsters)Directory.CreateDirectory("Assets/Art/Monsters/"+painting.name+"/Sources");
        AssetDatabase.Refresh();
        var index=JsonUtility.FromJson<DesignerAssetPaths.Index>(File.ReadAllText(DesignerAssetPaths.IndexPath));
        void Move(string path,string target)
        {
            if(path==target||string.IsNullOrEmpty(path)||!File.Exists(path))return;
            var guid=AssetDatabase.AssetPathToGUID(path);var error=AssetDatabase.MoveAsset(path,target);if(!string.IsNullOrEmpty(error))throw new IOException(error);
            var row=index.entries.FirstOrDefault(e=>e.guid==guid);
            if(row==null){row=new(){guid=guid,oldPath=path,path=target,type="Art",displayName=Path.GetFileNameWithoutExtension(target)};index.entries.Add(row);}
            row.aliases??=new();row.aliases.Add(path);row.path=target;
        }
        var guide=new StringBuilder("# 현재 보스 그림 수정 위치\n\n아래 폴더의 `몬스터명-Idle / Ready / Attack / Stunned.png`가 실제 전투 그림이다. 같은 이름의 `.asset`은 이미지의 피벗과 픽셀 단위를 가진 Unity Sprite다. `Sources`는 생성 원본이다. 가로 확장 폐기안은 `Art/Archive/RejectedWide`, 이전 상태 그림은 `Art/Archive/PreviousPoses`로 보존한다. GUID와 기존 작업 이력은 유지한다.\n\n");
        foreach(var painting in plan.monsters)
        {
            var monster=Monster(painting.oldName);var library=monster.combatAnimation;
            foreach(var frame in painting.frames)
            {
                var state=(CombatVisualState)Enum.Parse(typeof(CombatVisualState),frame.state);var sprite=library.For(state);
                Move(AssetDatabase.GetAssetPath(sprite),"Assets/Art/Monsters/"+painting.name+"/"+painting.name+"-"+frame.state+".asset");
            }
            foreach(var state in new[]{"Idle","Ready","Attack","Dead","Stunned"})
            {
                Move("Assets/Art/World/GiantPoses/"+painting.name+"-"+state+".png","Assets/Art/Archive/RejectedWide/"+painting.name+"-"+state+".png");
                Move("Assets/Art/Combat/NativeMonsterFrames/"+painting.name+"-"+state+".asset","Assets/Art/Archive/PreviousPoses/"+painting.name+"-"+state+".asset");
                Move("Assets/Art/World/Angular/States/Monsters/"+painting.name+"-"+state+".png","Assets/Art/Archive/PreviousPoses/"+painting.name+"-"+state+".png");
            }
            Move("Assets/Art/Monsters/"+painting.name+"/"+painting.name+"-상태원화.png","Assets/Art/Monsters/"+painting.name+"/Sources/"+painting.name+"-상태원화.png");
            guide.AppendLine("- **"+monster.displayName+"**: `Assets/Art/Monsters/"+painting.name+"/`\n  - 전투 데이터: `"+AssetDatabase.GetAssetPath(monster)+"`\n  - 상태 연결: `"+AssetDatabase.GetAssetPath(library)+"`\n  - 초상화: `"+AssetDatabase.GetAssetPath(monster.portrait)+"`\n  - 초상화 원본: `"+AssetDatabase.GetAssetPath(monster.portrait.texture)+"`");
        }
        foreach(var row in index.entries)
        {var current=AssetDatabase.GUIDToAssetPath(row.guid);if(!string.IsNullOrEmpty(current)&&current!=row.path){row.aliases??=new();row.aliases.Add(row.path);row.path=current;}}
        DesignerAssetPaths.SaveIndex(index);
        File.WriteAllText(Folder+"ArtGuide.md",guide.ToString());AssetDatabase.SaveAssets();
    }
    internal static void MatchAttackNames()
    {
        void Rename(MonsterAttackData attack,string title)
        {attack.displayName=title;BehemothTextEditor.SyncAsset(attack);Dirty(attack);}
        Rename(Monster("StormMatriarch").GetAttack(MonsterAttackType.Normal),"연속 발톱찍기");
        Rename(Monster("Data_Monster_B3_Boss_SoulEater").GetAttack(MonsterAttackType.Normal),"불씨 분사");
        AssetDatabase.SaveAssets();DesignerDataNaming.Normalize();BehemothTextEditor.ExportDefault();
        File.WriteAllText(Folder+"AttackNamesMatched.txt","Roc normal: repeated talon strikes. Dragon normal: ember spit; charge: ember breath. Numeric attack data unchanged.\n");
    }
}
