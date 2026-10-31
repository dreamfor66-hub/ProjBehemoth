using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class DesignerDataNaming
{
    static double next=double.MaxValue;
    static bool busy;
    internal static bool IsBusy=>busy;
    static DesignerDataNaming(){EditorApplication.update+=Poll;Queue();}
    internal static void Queue()=>next=EditorApplication.timeSinceStartup+3;
    static void Poll()
    {
        if(busy||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.timeSinceStartup<next)return;
        if(!File.Exists(PlayabilityRevisionPreparation.Folder+"Installed.txt"))return;
        next=double.MaxValue;busy=true;
        try{Normalize();}catch(Exception e){Debug.LogException(e);File.WriteAllText(PlayabilityRevisionPreparation.Folder+"NamingError.txt",e.ToString());}finally{busy=false;}
    }
    static T[] All<T>()where T:UnityEngine.Object=>AssetDatabase.FindAssets("t:"+typeof(T).Name,new[]{"Assets/Data"}).Select(g=>AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g))).Where(a=>a!=null).ToArray();
    static string Slug(string value)=>Regex.Replace((value??"").Trim(),@"[^\p{L}\p{N}]+","_").Trim('_');
    internal static string MonsterName(string value)=>Regex.Replace(value??"",@"[^\p{L}\p{N}]+","");
    static string Display(ScriptableObject data)=>data.GetType().GetField("displayName")?.GetValue(data) as string??data.name;
    static string Id(ScriptableObject data)=>data.GetType().GetField("id")?.GetValue(data) as string??"";
    internal static void Normalize()
    {
        var previous=File.Exists(DesignerAssetPaths.IndexPath)?JsonUtility.FromJson<DesignerAssetPaths.Index>(File.ReadAllText(DesignerAssetPaths.IndexPath)):new DesignerAssetPaths.Index();
        var index=new DesignerAssetPaths.Index();
        var monsterContext=new Dictionary<MonsterData,string>();
        var attackOwner=new Dictionary<MonsterAttackData,List<MonsterData>>();
        foreach(var stage in All<StageSetData>())
        {
            void Register(IEnumerable<EncounterData> encounters,string role)
            {foreach(var encounter in encounters.Where(e=>e!=null))foreach(var member in encounter.members.Where(m=>m.monster!=null))monsterContext.TryAdd(member.monster,$"B{stage.act}_{role}");}
            Register(stage.earlyCombats,"Normal");Register(stage.hardCombats,"Normal");Register(stage.elites,"Elite");Register(stage.bosses,"Boss");
        }
        foreach(var monster in All<MonsterData>())foreach(var attack in monster.attacks.Where(a=>a!=null))
        {if(!attackOwner.TryGetValue(attack,out var owners))attackOwner[attack]=owners=new();owners.Add(monster);}
        var originalPaths=new Dictionary<int,string>();
        string DisplayFor(ScriptableObject data)
        {
            if(data is LocalizedData)return Display(data);
            if(!originalPaths.TryGetValue(data.GetInstanceID(),out var old))
            {old=previous.entries.FirstOrDefault(e=>e.guid==AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(data)))?.oldPath??AssetDatabase.GetAssetPath(data);originalPaths[data.GetInstanceID()]=old;}
            var stem=Path.GetFileNameWithoutExtension(old);
            return data switch
            {
                GameContent=>"게임 콘텐츠 목록",GameVariables=>"공용 게임 변수",FatigueSettings=>"피로도 규칙",RunBalanceSettings=>"레벨과 보상 규칙",DungeonGenerationSettings=>"미궁 생성 규칙",ActGrowthBudget budget=>$"{budget.act}막 성장 예산",PerkOfferGroup=>"마나 퍽 제안 묶음",
                BuffTagData=>stem switch{"Burn"=>"화상","DrawAttack"=>"발도 공격","EnchantFire"=>"화염 부여","EnchantIce"=>"빙결 부여","EnchantLightning"=>"뇌전 부여","LightningRod"=>"피뢰침","PerfectGuard"=>"완벽한 가드","Poison"=>"독","SheathParry"=>"납도 패리","Slow"=>"둔화","WeaponEnchantment"=>"무기 속성 부여",_=>stem},_=>stem
            };
        }
        string Name(ScriptableObject data)
        {
            var display=Slug(DisplayFor(data));if(string.IsNullOrEmpty(display))display=Slug(data.name);
            if(data is MonsterData monster)return "Data_Monster_"+(monsterContext.TryGetValue(monster,out var context)?context:"Special")+"_"+MonsterName(monster.displayName);
            if(data is MonsterAttackData attack)
            {var owner=attackOwner.TryGetValue(attack,out var owners)?MonsterName(owners.OrderByDescending(m=>monsterContext.ContainsKey(m)).First().displayName):"Common";return "Data_Attack_"+owner+"_"+attack.type+"_"+display;}
            if(data is EncounterData)
                foreach(var owner in monsterContext.Keys.OrderByDescending(m=>m.displayName.Length)) display=display.Replace(Slug(owner.displayName),MonsterName(owner.displayName));
            var type=data switch{EquipmentData item=>AssetDatabase.GetAssetPath(item).Contains("/PerkAbilities/")?"PerkAbility":item.IsPotion?"Potion":"Relic",PerkData=>"Perk",BuffData=>"Buff",PlayerCombatData=>"Character",SpellData=>"Spell",EncounterData=>"Encounter",StageSetData=>"Stage",RandomEventData=>"Event",CompanionData=>"Companion",_=>data.GetType().Name.Replace("Data","")};
            return "Data_"+type+"_"+display;
        }
        var dataSet=All<ScriptableObject>().Where(d=>d is LocalizedData or GameContent or GameVariables or FatigueSettings or RunBalanceSettings or DungeonGenerationSettings or ActGrowthBudget or BuffTagData or PerkOfferGroup).OrderBy(AssetDatabase.GetAssetPath).ToArray();
        var proposed=dataSet.ToDictionary(d=>d,d=>Path.GetDirectoryName(AssetDatabase.GetAssetPath(d)).Replace('\\','/')+"/"+Name(d)+".asset");
        foreach(var group in proposed.GroupBy(k=>k.Value,StringComparer.OrdinalIgnoreCase).Where(g=>g.Count()>1).ToArray())
            foreach(var pair in group)proposed[pair.Key]=Path.ChangeExtension(pair.Value,null)+"_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(pair.Key)).Substring(0,6)+".asset";
        foreach(var data in dataSet)
        {
            var old=AssetDatabase.GetAssetPath(data);var guid=AssetDatabase.AssetPathToGUID(old);var target=proposed[data];
            var original=previous.entries.FirstOrDefault(e=>e.guid==guid)?.oldPath??old;
            if(!string.Equals(old,target,StringComparison.Ordinal))
            {var error=AssetDatabase.MoveAsset(old,target);if(!string.IsNullOrEmpty(error))throw new IOException(error);data.name=Path.GetFileNameWithoutExtension(target);EditorUtility.SetDirty(data);}
            var prior=previous.entries.FirstOrDefault(e=>e.guid==guid);
            index.entries.Add(new(){oldPath=original,path=target,guid=guid,id=Id(data),displayName=DisplayFor(data),type=data.GetType().Name,aliases=(prior?.aliases??new()).Concat(new[]{old,original}).Distinct().ToList()});
            AssetDatabase.SetLabels(data,new[]{"DesignerData","종류_"+data.GetType().Name,"ID_"+Id(data)}.Concat(AssetDatabase.GetLabels(data).Where(l=>!l.StartsWith("ID_")&&!l.StartsWith("종류_"))).Distinct().ToArray());
        }
        index.entries.AddRange(previous.entries.Where(e=>index.entries.All(n=>n.guid!=e.guid)&&!string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(e.guid))));
        Directory.CreateDirectory(Path.GetDirectoryName(DesignerAssetPaths.IndexPath));DesignerAssetPaths.SaveIndex(index);
        // Keep CSV source locations usable without changing stable text keys.
        var table=BehemothTextEditor.Table;
        foreach(var entry in table.entries)
            foreach(var row in index.entries)
            {var alias=row.aliases.FirstOrDefault(p=>entry.source!=null&&entry.source.StartsWith(p+":",StringComparison.Ordinal));if(alias!=null){entry.source=row.path+entry.source.Substring(alias.Length);break;}}
        EditorUtility.SetDirty(table);AssetDatabase.SaveAssets();
        File.WriteAllText(BehemothTextEditor.CsvPath,table.ExportCsv(),new UTF8Encoding(true));
        var md=new StringBuilder("# 데이터 이름과 찾기\n\n파일명: `Data_종류_맥락_한국어명.asset`. 몬스터는 막/역할, 공격은 소유 몬스터/일반·강타를 포함한다. GUID, id, 텍스트 키는 유지한다. 표시명을 수정하고 저장하면 파일명도 자동 동기화된다.\n\nUnity 메뉴 `Behemoth/데이터 찾기`에서 한국어명, 기존 영문명, ID로 검색할 수 있다. 검색 결과를 선택하면 해당 데이터가 Inspector에 열린다.\n\n|종류|표시명|ID / 이전 이름|현재 경로|\n|---|---|---|---|\n");
        foreach(var row in index.entries)md.AppendLine($"|{row.type}|{row.displayName}|{row.id} / {Path.GetFileNameWithoutExtension(row.oldPath)}|{row.path}|");
        File.WriteAllText(PlayabilityRevisionPreparation.Folder+"DataIndex.md",md.ToString());
    }
}
internal sealed class DesignerDataSaveListener:AssetModificationProcessor
{static string[] OnWillSaveAssets(string[] paths){if(!DesignerDataNaming.IsBusy&&paths.Any(p=>p.StartsWith("Assets/Data/")&&p.EndsWith(".asset")))DesignerDataNaming.Queue();return paths;}}
internal sealed class DesignerDataBrowser:EditorWindow
{
    string search="";Vector2 scroll;DesignerAssetPaths.Index index;Font koreanFont;
    [MenuItem("Behemoth/데이터 찾기")]
    internal static void Open()=>GetWindow<DesignerDataBrowser>("데이터 찾기").Show();
    void OnEnable(){minSize=new Vector2(760,440);Reload();}
    void Reload(){if(File.Exists(DesignerAssetPaths.IndexPath))index=JsonUtility.FromJson<DesignerAssetPaths.Index>(File.ReadAllText(DesignerAssetPaths.IndexPath));}
    void OnGUI()
    {
        // Use an installed Korean font rather than Unity's dynamic Editor fallback atlas.
        if(koreanFont==null)koreanFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/DosStory/DOSIyagiMedium.ttf");
        var old=GUI.skin.font;GUI.skin.font=koreanFont;
        try{DrawGUI();}finally{GUI.skin.font=old;}
    }
    void DrawGUI()
    {
        using(new EditorGUILayout.HorizontalScope()){search=EditorGUILayout.TextField("이름 / ID / 이전 파일명",search);if(GUILayout.Button("새로고침",GUILayout.Width(80)))Reload();}
        if(index==null){EditorGUILayout.HelpBox("데이터 이름을 준비하고 있습니다.",MessageType.Info);return;}
        scroll=EditorGUILayout.BeginScrollView(scroll);
        foreach(var row in index.entries.Where(e=>string.IsNullOrWhiteSpace(search)||(e.displayName+e.id+e.oldPath+e.path+string.Join(" ",e.aliases??new())).IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0))
        {
            using(new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {if(GUILayout.Button(row.displayName+"  ·  "+row.type,EditorStyles.boldLabel)){Selection.activeObject=AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(row.guid));EditorGUIUtility.PingObject(Selection.activeObject);}EditorGUILayout.LabelField(row.path,EditorStyles.miniLabel);EditorGUILayout.LabelField("ID: "+row.id+"  /  이전: "+Path.GetFileNameWithoutExtension(row.oldPath),EditorStyles.miniLabel);}
        }
        EditorGUILayout.EndScrollView();
    }
}
