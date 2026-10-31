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
using Object=UnityEngine.Object;

[InitializeOnLoad]
internal static class PlayabilityRevisionPreparation
{
    internal const string Folder="Docs/Production/PlayabilityRevision-2026-10-30/";
    static bool busy,done;
    static PlayabilityRevisionPreparation()=>EditorApplication.update+=Poll;
    static void Poll()
    {
        if(done||busy||!File.Exists(Folder+"CodeReady.txt")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(File.Exists(Folder+"Installed.txt"))
        {
            if(!File.Exists(Folder+"FeedbackCanvasReady.txt"))
            {PrepareScene();File.WriteAllText(Folder+"FeedbackCanvasReady.txt","Saved feedback renderers installed.\n");}
            if(!File.Exists(Folder+"ResultGroupsInstalled.txt"))
            {PrepareScene();BehemothTextEditor.ExportDefault();File.WriteAllText(Folder+"ResultGroupsInstalled.txt","Unity-null-safe saved CanvasGroups installed.\n");}
            if(!File.Exists(Folder+"StatusSummaryInstalled.txt"))
            {PrepareScene();File.WriteAllText(Folder+"StatusSummaryInstalled.txt","Always-active HUD refresh keeps closed-inventory health summary current.\n");}
            if(!File.Exists(Folder+"SemanticNamesInstalled.txt"))
            {PrepareAttackNames();BehemothTextEditor.Table.Set("ui.camp.full.detail","피로도 100까지 · 체력 10 회복\n다음 이동부터 피로도가 다시 감소한다.\n살아 있는 일반 몬스터가 이동할 수 있다.");BehemothTextEditor.ExportDefault();File.WriteAllText(Folder+"SemanticNamesInstalled.txt","Elemental buffs, training variants and camp description clarified.\n");}
            if(!File.Exists(Folder+"AttackNamesInstalled.txt"))
            {PrepareAttackNames();AssetDatabase.SaveAssets();BehemothTextEditor.ExportDefault();File.WriteAllText(Folder+"AttackNamesInstalled.txt","Final localized skill names installed.\n");}
            if(!File.Exists(Folder+"AllAttackNamesInstalled.txt"))
            {PrepareAttackNames();AssetDatabase.SaveAssets();BehemothTextEditor.ExportDefault();DesignerDataNaming.Queue();File.WriteAllText(Folder+"AllAttackNamesInstalled.txt","Legacy presentation attacks now share meaningful authored skill names.\n");}
            done=true;return;
        }
        busy=true;
        try{Prepare();File.WriteAllText(Folder+"Installed.txt","Content, saved HUD/feedback/result, immediate fatigue and encounter growth rules installed.\n");done=true;}
        catch(Exception e){done=true;File.WriteAllText(Folder+"PreparationError.txt",e.ToString());Debug.LogException(e);}
        finally{busy=false;}
    }
    static T[] Assets<T>()where T:Object=>AssetDatabase.FindAssets("t:"+typeof(T).Name,new[]{"Assets/Data"}).Select(g=>AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g))).Where(a=>a!=null).ToArray();
    static T Ref<T>(Object owner,string field)where T:Object=>new SerializedObject(owner).FindProperty(field)?.objectReferenceValue as T;
    static void Text(LocalizedData data,string field,string value)
    {var so=new SerializedObject(data);so.FindProperty(field).stringValue=value;so.ApplyModifiedPropertiesWithoutUndo();BehemothTextEditor.SyncAsset(data);EditorUtility.SetDirty(data);}
    static EquipmentAbility Unit(TriggerType trigger,params EquipmentEffect[] effects)=>new(){trigger=new EquipmentTrigger{triggerType=trigger,triggerCount=1,chancePercent=100,triggerAttackTypeFilter=TriggerAttackTypeFilter.All,triggerAttackWayFilter=TriggerAttackWayFilter.All},target=TargetType.Owner,effects=effects.ToList()};
    static EquipmentEffect Stat(ModifierKind kind,float amount)=>new(){effectType=EffectType.StatIncrease,effectStatType=kind,effectMagnitude=amount,effectCount=1};
    static void Prepare()
    {
        Directory.CreateDirectory(Folder);
        var items=Assets<EquipmentData>();EquipmentData Item(string id)=>items.Single(x=>x.id==id);
        void Set(string id,string description,params EquipmentAbility[] units)
        {var item=Item(id);item.abilities=units.ToList();Text(item,"description",description);}
        Set("AzureTalon","일반 공격 피해 +15%, 공격 거리 +100.",Unit(TriggerType.OnAcquire,Stat(ModifierKind.NormalDamage,.15f),Stat(ModifierKind.AttackReach,100)));
        Set("DistanceMomentum","공격 회복 시간 -12%. 베기 거리에 비례해 피해가 증가한다 (100 거리마다 +6%).",Unit(TriggerType.OnAcquire,Stat(ModifierKind.AttackCooldownMultiplier,.12f),Stat(ModifierKind.DamagePerAttackDistance,.0006f)));
        Set("RaggedBanner","모든 피해 +12%. 살아 있는 적 한 마리마다 추가로 +4%.",Unit(TriggerType.Passive,new EquipmentEffect{effectType=EffectType.RuleModifier,rule=AbilityRule.DamageMultiplier,effectMagnitude=.12f,effectCount=1,valueVariables=new(){new EffectValueVariable{type=EffectScale.EnemyCount,multiplier=.04f}}}));
        Set("BloodContract","모든 피해 +25%. 받는 체력 회복량 -40%.",Unit(TriggerType.OnAcquire,Stat(ModifierKind.AllDamageMultiplier,.25f),Stat(ModifierKind.HealingReceived,-.4f)));
        var ember=Unit(TriggerType.OnDeath,Stat(ModifierKind.RevivalHealthFraction,.35f));ember.trigger.triggerMaxCount=1;
        Set("RevivalPotion","소지 중 사망하면 자동으로 소모되어 최대 체력의 35%로 부활한다. 마시거나 던질 수 없으며 버릴 수 있다.",ember);
        Text(Item("FatiguePotion"),"description","피로도 30 회복. 100을 넘지 않으며 다음 이동부터 다시 감소한다.");
        var table=BehemothTextEditor.Table;
        table.Set("ui.result.defeat","YOU DIED");table.Set("ui.result.victory","미궁을 넘어서");
        table.Set("ui.fatigue.potion.description","피로도 30 회복. 100을 넘지 않으며 다음 이동부터 다시 감소한다.");
        foreach(var entry in table.entries.Where(e=>e.key.StartsWith("ui.camp.")||e.key.StartsWith("ui.fatigue.")))
        {if(entry.kr!=null&&entry.kr.Contains("지치기까지"))entry.kr=entry.key=="ui.fatigue.grace"?"이동할 때마다 피로도가 감소한다.":entry.kr.Split(new[]{'\n'}).First();}
        table.Invalidate();EditorUtility.SetDirty(table);
        foreach(var stage in Assets<StageSetData>())
        {stage.minimumPackLevel=stage.act==1?4:1;if(stage.act==1){stage.easyCombatCount=6;stage.packChance=.4f;}EditorUtility.SetDirty(stage);}
        foreach(var ev in Assets<RandomEventData>())
        {
            var so=new SerializedObject(ev);var it=so.GetIterator();var changed=false;
            while(it.Next(true))if(it.propertyType==SerializedPropertyType.String&&!it.propertyPath.StartsWith("textKeys")&&it.stringValue.Contains("붉은 계약서: 모든 피해 +25%."))
            {it.stringValue=it.stringValue.Replace("붉은 계약서: 모든 피해 +25%.","붉은 계약서: 모든 피해 +25%, 받는 회복량 -40%.");changed=true;}
            if(changed){so.ApplyModifiedPropertiesWithoutUndo();BehemothTextEditor.SyncAsset(ev);EditorUtility.SetDirty(ev);}
        }
        var style=Assets<WeakPointVisualStyle>().Single();style.core=new Color32(255,248,218,255);style.stroke=new Color32(255,73,47,255);style.ringThickness=1.75f;EditorUtility.SetDirty(style);
        PrepareAttackNames();PrepareScene();AssetDatabase.SaveAssets();BehemothTextEditor.ExportDefault();
    }
    static RectTransform Node(Transform parent,string name)
    {var old=parent.Find(name) as RectTransform;if(old!=null)return old;var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;r.SetParent(parent,false);return r;}
    static T Ensure<T>(GameObject target)where T:Component
    {var component=target.GetComponent<T>();if(component==null)component=target.AddComponent<T>();return component;}
    static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;r.localScale=Vector3.one;}
    static void Place(RectTransform r,Vector2 position,Vector2 size){r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=position;r.sizeDelta=size;r.localScale=Vector3.one;}
    static void Font(TextMeshProUGUI label,float size){label.fontSize=size;label.enableAutoSizing=false;label.raycastTarget=false;var role=label.GetComponent<TypographyRoleBinding>();if(role!=null){role.designSize=size;role.Apply();}}
    static void PrepareScene()
    {
        const string path="Assets/Scenes/MainGameScene.unity";
        var scene=SceneManager.GetSceneByPath(path);var loaded=scene.isLoaded;
        if(!loaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            // Snapshot unsaved designer changes before touching the same open scene.
            if(scene.isDirty&&!File.Exists(Folder+"Before/UnsavedMainGameScene.unity"))
                if(!EditorSceneManager.SaveScene(scene,Folder+"Before/UnsavedMainGameScene.unity",true))throw new InvalidOperationException("Could not preserve unsaved scene snapshot.");
            T Find<T>()where T:Component=>scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).Single();
            var hud=Find<CombatHudView>();var structure=Find<HudSceneStructure>();var combat=Find<CombatController>();var sceneView=Find<CombatSceneView>();
            var quick=Find<QuickInventoryView>();quick.characterSummary=Find<RunHudView>();EditorUtility.SetDirty(quick);
            var guard=hud.guardGauge;guard.shieldSilhouette=true;
            var guardRoot=(RectTransform)Ref<Image>(hud,"shield").transform.parent;
            Place(guardRoot,new Vector2(-260,-285),new Vector2(300,58));
            Stretch(guard.rectTransform);guard.raycastTarget=false;
            var text=Ref<TextMeshProUGUI>(hud,"shieldText");Place(text.rectTransform,new Vector2(15,52),new Vector2(320,42));Font(text,30);text.color=new Color32(214,250,255,255);text.outlineColor=Color.black;text.outlineWidth=.25f;text.gameObject.SetActive(true);
            hud.attackRecovery.rectTransform.sizeDelta=new Vector2(170,38);
            var anchor=hud.attackRecovery.GetComponent<AttackCooldownAnchor>();anchor.placement=AttackCooldownPlacement.AbovePlayer;anchor.playerOffset=new Vector2(0,330);
            var feedbackRoot=Node(structure.combat,"CombatPressureFeedback");Stretch(feedbackRoot);feedbackRoot.SetAsLastSibling();
            var feedback=feedbackRoot.GetComponent<CombatPressureFeedback>()??feedbackRoot.gameObject.AddComponent<CombatPressureFeedback>();feedback.combat=combat;feedback.run=Find<RunController>();feedback.scene=sceneView;
            var impact=Node(feedbackRoot,"MonsterAttackImpact");Stretch(impact);if(impact.GetComponent<CanvasRenderer>()==null)impact.gameObject.AddComponent<CanvasRenderer>();feedback.impact=impact.GetComponent<MonsterImpactGraphic>()??impact.gameObject.AddComponent<MonsterImpactGraphic>();feedback.impact.raycastTarget=false;
            var danger=Node(feedbackRoot,"LowHealthAndDamageEdges");Stretch(danger);if(danger.GetComponent<CanvasRenderer>()==null)danger.gameObject.AddComponent<CanvasRenderer>();feedback.danger=danger.GetComponent<HealthDangerGraphic>()??danger.gameObject.AddComponent<HealthDangerGraphic>();feedback.danger.raycastTarget=false;
            combat.pressureFeedback=feedback;
            var result=Find<ResultView>();var panel=Ref<GameObject>(result,"panel");var pr=(RectTransform)panel.transform;
            Stretch(pr);pr.SetAsLastSibling();
            foreach(var surface in panel.GetComponents<InkUiSurface>())surface.enabled=false;
            var background=panel.GetComponent<Image>()??panel.AddComponent<Image>();background.color=new Color(0,0,0,.64f);background.raycastTarget=true;
            var veiled=Node(pr,"DeathVeil");Stretch(veiled);veiled.SetAsFirstSibling();var veilImage=veiled.GetComponent<Image>()??veiled.gameObject.AddComponent<Image>();veilImage.color=new Color(0,0,0,.35f);veilImage.raycastTarget=false;
            result.veil=Ensure<CanvasGroup>(veiled.gameObject);
            var band=Node(pr,"MourningBand");Place(band,Vector2.zero,new Vector2(1920,230));band.SetSiblingIndex(1);result.mourningBand=band.GetComponent<Image>()??band.gameObject.AddComponent<Image>();result.mourningBand.color=new Color(0,0,0,.66f);result.mourningBand.raycastTarget=false;
            result.headingGroup=Ensure<CanvasGroup>(band.gameObject);
            var title=Ref<TextMeshProUGUI>(result,"title");title.transform.SetParent(band,false);Place(title.rectTransform,Vector2.zero,new Vector2(1650,190));Font(title,100);title.text="YOU DIED";title.alignment=TextAlignmentOptions.Center;title.characterSpacing=12;
            var actions=Node(pr,"ResultActions");Place(actions,new Vector2(0,-215),new Vector2(1100,220));result.actions=Ensure<CanvasGroup>(actions.gameObject);
            var details=Ref<TextMeshProUGUI>(result,"details");details.transform.SetParent(actions,false);Place(details.rectTransform,new Vector2(0,40),new Vector2(1050,100));Font(details,30);details.alignment=TextAlignmentOptions.Center;details.color=new Color32(203,196,181,255);
            var restart=Ref<Button>(result,"restart");restart.transform.SetParent(actions,false);Place((RectTransform)restart.transform,new Vector2(0,-65),new Vector2(360,62));
            var returnLabel=restart.GetComponentInChildren<TextMeshProUGUI>(true);var returnKey="ui.result.return";BehemothTextEditor.Table.Set(returnKey,"캐릭터 선택으로");
            returnLabel.text="캐릭터 선택으로";Font(returnLabel,30);var binding=returnLabel.GetComponent<GameTextBinding>();if(binding!=null)binding.key=returnKey;
            EditorUtility.SetDirty(result);
            panel.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Scene save failed");
        }
        finally{if(!loaded)EditorSceneManager.CloseScene(scene,true);}
    }
    static void PrepareAttackNames()
    {
        // Authored skill names; number/steal claims are taken from actual mechanics.
        var names=new System.Collections.Generic.Dictionary<string,(string normal,string charge)>{
            ["TunnelRat"]=("독니 물기","거미 돌진"),["AlarmHound"]=("송곳니 물기","경종의 포효"),["ArmoredExecutioner"]=("낫 휘두르기","쌍낫 처형"),
            ["AshCrow"]=("부리 쪼기","재깃 폭풍"),["AshGeneral"]=("왕검 베기","잿불 단죄"),["BellKeeper"]=("종의 울림","진혼의 종소리"),
            ["BloodExecutioner"]=("핏빛 참격","선혈의 만찬"),["BroodKeeper"]=("실타래 채찍","꼭두각시 무도"),["CrystalSpider"]=("수정 다리 찌르기","수정창 난무"),
            ["DeepTrialGuardian"]=("봉인 타격","심연의 심판"),["IceMirror"]=("거울 파편","빙경 파열"),["IceNeedle"]=("서리침","빙침 폭풍"),
            ["LampPriest"]=("향로 타격","혼불 의식"),["MirrorKnight"]=("거울검 베기","반사의 칼날"),["OrbitBat"]=("급강하","회전 돌진"),
            ["PaleWisp"]=("창백한 불꽃","망자의 숨결"),["SnowBell"]=("서리의 울림","동결의 종소리"),["SoulLamp"]=("혼불 탄환","영혼 소각"),
            ["StormMatriarch"]=("날개 후려치기","폭풍 강림"),["StormOrb"]=("전격","뇌전 폭발"),["TrialGuardian"]=("중갑 돌진","대지 파쇄"),
            ["EclipseBeast"]=("키메라 할퀴기","일식 포효"),["ElderLeftHeart"]=("태양 광선","태양 폭발"),["ElderRightHeart"]=("달빛 광선","월식 파동")};
        var legacy=new System.Collections.Generic.Dictionary<string,string>{
            ["BatNormal"]="창 찌르기",["BatCharge"]="급소 찌르기",["LeechNormal"]="날개 스치기",["LeechCharge"]="검은 날개",
            ["SkeletonNormal"]="검 베기",["SkeletonCharge"]="골절 베기",["MinotaurNormal"]="몽둥이 휘두르기",["MinotaurCharge"]="대지 분쇄",
            ["BossNormal"]="앞발 강타",["BossCharge"]="미궁의 심판",["BossCrossCharge"]="사방 봉인",
            ["FrostBatNormal"]="깃바늘 쏘기",["FrostBatCharge"]="서리깃 폭풍",["IceWraithNormal"]="저주 손길",["IceWraithCharge"]="장막의 저주",
            ["YetiNormal"]="돌지팡이 타격",["YetiCharge"]="엄니 돌진",["FrostOgreNormal"]="뿔 들이받기",["FrostOgreCharge"]="쌍각 돌진",
            ["BossTwoNormal"]="발굽 차기",["BossTwoCharge"]="겨울의 관통",["AshHoundNormal"]="불씨 물기",["AshHoundCharge"]="용암 분사",
            ["ShadeLancerNormal"]="미늘창 찌르기",["ShadeLancerCharge"]="나선 관통",["VoidMawNormal"]="공허 탄환",["VoidMawCharge"]="심연의 응시",
            ["SoulKnightNormal"]="파계의 창",["SoulKnightCharge"]="전장의 돌진",["BossThreeNormal"]="발톱 가르기",["BossThreeCharge"]="잿불 숨결",
            ["MimicNormal"]="철니 물기",["MimicCharge"]="보물의 아귀",["MerchantMutantNormal"]="금자루 타격",["MerchantMutantCharge"]="탐욕의 폭식"};
        var index=File.Exists(DesignerAssetPaths.IndexPath)?JsonUtility.FromJson<DesignerAssetPaths.Index>(File.ReadAllText(DesignerAssetPaths.IndexPath)):null;
        // Old concept identifiers are migration aliases only, never runtime gameplay dispatch.
        var aliases=new System.Collections.Generic.Dictionary<string,string>{
            ["LanternImp"]="Bat",["BoneScorpion"]="TunnelRat",["StringRevenant"]="Skeleton",["CoffinSlime"]="Leech",["BellHound"]="AlarmHound",["EttinGatekeeper"]="Minotaur",["MirrorShell"]="MirrorKnight",["IronMantis"]="ArmoredExecutioner",["StoneSphinx"]="Boss",["OssuaryBell"]="BellKeeper",["WebDuchess"]="BroodKeeper",["Threadling"]="OrbitBat",["QuillSprite"]="FrostBat",["CrystalUrchin"]="IceNeedle",["VeilOracle"]="IceWraith",["TuskPilgrim"]="Yeti",["BellHare"]="SnowBell",["TwinAntlerStag"]="FrostOgre",["MirrorMoth"]="IceMirror",["MaskedChoir"]="PaleWisp",["WinterUnicorn"]="BossTwo",["PrismCrab"]="CrystalSpider",["TempestRoc"]="StormMatriarch",["StormJelly"]="StormOrb",["CoalSalamander"]="AshHound",["SerpentWarden"]="ShadeLancer",["AshGargoyle"]="AshCrow",["HollowEye"]="VoidMaw",["CenserRevenant"]="SoulLamp",["OathlessCentaur"]="SoulKnight",["CrimsonCount"]="BloodExecutioner",["AshHierophant"]="LampPriest",["AshDragon"]="BossThree",["EclipseChimera"]="EclipseBeast",["SunIdol"]="ElderLeftHeart",["MoonIdol"]="ElderRightHeart",["CinderLich"]="AshGeneral",["IronMimic"]="Mimic",["GildedToad"]="MerchantMutant",["TarrasqueWhelp"]="TrialGuardian",["CrownColossus"]="DeepTrialGuardian"};
        foreach(var attack in Assets<MonsterAttackData>())
        {
            var path=AssetDatabase.GetAssetPath(attack);var original=index?.entries.FirstOrDefault(e=>e.guid==AssetDatabase.AssetPathToGUID(path))?.oldPath??path;
            var stem=Path.GetFileNameWithoutExtension(original);var owner=stem.Split('_')[0];
            var artId=stem.Split('-')[0];
            if(aliases.TryGetValue(artId,out var alias)){owner=alias;stem=alias+(attack.type==MonsterAttackType.Charge?"Charge":"Normal");}
            if(legacy.TryGetValue(stem,out var fixedName))
            {Text(attack,"displayName",attack.combatMechanics.hitCount>1?"연속 "+fixedName:fixedName);continue;}
            if(!names.TryGetValue(owner,out var pair))continue;
            var name=attack.type==MonsterAttackType.Charge?pair.charge:pair.normal;
            if(stem=="AshGeneral_Charge_2")name="왕의 소각";
            if(attack.combatMechanics.hitCount>1&&attack.type==MonsterAttackType.Normal)name="연속 "+name;
            Text(attack,"displayName",name);
        }
        foreach(var buff in Assets<BuffData>())
        {
            var original=index?.entries.FirstOrDefault(e=>e.guid==AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(buff)))?.oldPath??AssetDatabase.GetAssetPath(buff);
            var explicitName=Path.GetFileNameWithoutExtension(original) switch{"SpellbladeFire"=>"마검 · 화염 방출","SpellbladeIce"=>"마검 · 빙결 방출","SpellbladeLightning"=>"마검 · 뇌전 방출",_=>null};
            if(explicitName!=null){Text(buff,"displayName",explicitName);continue;}
            var title=buff.displayName switch{"GuardianBell"=>"수호의 종 · 피해 감소","PainFrenzy"=>"고통의 광기 · 무기 강화","PerfectMending"=>"완전한 수복 · 가드 회복","SpellBlade"=>"마검 · 발도 강화","StaffConduit"=>buff.name.Contains("damage")?"지팡이 매개 · 피해 강화":"지팡이 매개 · 시전 강화",_=>null};
            if(title!=null)Text(buff,"displayName",title);
        }
        foreach(var monster in Assets<MonsterData>())
        {
            var original=index?.entries.FirstOrDefault(e=>e.guid==AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(monster)))?.oldPath??AssetDatabase.GetAssetPath(monster);
            var title=Path.GetFileNameWithoutExtension(original) switch{"TrainingLight"=>"경량 훈련 인형","TrainingMedium"=>"균형 훈련 인형","TrainingHeavy"=>"중량 훈련 인형",_=>null};if(title!=null)Text(monster,"displayName",title);
        }
    }
}
