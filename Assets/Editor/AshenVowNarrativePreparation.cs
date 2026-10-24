using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class AshenVowNarrativePreparation
{
    private static GameContent content;
    private static Sprite[] illustrations;
    static AshenVowNarrativePreparation() => EditorApplication.delayCall += Prepare;
    internal static void Prepare()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.delayCall += Prepare; return; }
        content = AssetDatabase.LoadAssetAtPath<GameContent>("Assets/Data/Behemoth/GameContent.asset");
        if (content == null || content.narrativeRevision >= 2) return;
        if (content.artRevision < 2) { EditorApplication.delayCall += Prepare; return; }
        try
        {
            illustrations = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/AshenVow/Atlases/EventAtlas.png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();
            var source=File.ReadAllText("Docs/Production/EventSequences-Castle.md");
            var sections=Regex.Split(source,@"(?m)^## \d+\. ").Skip(1).ToArray();
            var ids=new[]{"StrayDog","Symbiosis","Mirror","Contract","Pilgrim","LostBeliever","Hourglass","StormBottle","AmberSeed","Bell","Banner","Tooth","Training","Scholar","Forge","Well","Investment","Delivery","MasterTrial","Shelter"};
            for(var i=0;i<ids.Length;i++)
            {
                var story=S(ids[i]);var intro=sections[i].Split(new[]{"\n- **","\r\n- **"},StringSplitOptions.None)[0];
                var lines=intro.Split('\n').Where(l=>l.StartsWith("> ")).Select(l=>l.Substring(2).Trim());
                story.narrative=string.Join("\n\n",lines);story.illustration=illustrations[i];
                story.pages=new(){Page(story.narrative,story.options.Select(o=>Copy(o)).Append(Leave()).ToArray())};story.entryRoutes.Clear();
                story.referenceExperience += "\n서사/연출: Docs/Production/EventSequences-Castle.md";EditorUtility.SetDirty(story);
            }
            BuildDog();BuildMirror();BuildContract();BuildLostBeliever();BuildHourglass();BuildBell();BuildTooth();BuildScholar();BuildForge();BuildWell();BuildTrial();BuildTeacher();
            var table=BehemothTextEditor.Table;
            foreach(var pair in new Dictionary<string,string>{["ui.event.continue"]="계속",["ui.event.leave"]="탐험으로 돌아간다",["ui.event.supplymissing"]="필요한 물품이 없다",["ui.event.notready"]="아직 배울 준비가 되지 않았다",["ui.event.learnedspell"]="{0}을 배웠다!",["ui.event.lessondeclined"]="이번 수업은 받지 않고 선생과 헤어졌다."})table.Set(pair.Key,pair.Value,"AshenVow");
            content.narrativeRevision=2;EditorUtility.SetDirty(content);BehemothTextEditor.SyncAll();BehemothTextEditor.ExportDefault();AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/AshenVow-NarrativePreparation.txt","20 castle event sequences + three teacher encounters; variable depth, paragraphs, supplies and conditional lessons.");
        }
        catch(Exception error){Debug.LogException(error);}
    }
    private static RandomEventData S(string id)=>content.events.First(s=>s.id==id);
    private static EventPage Page(string text,params EventOption[] options)=>new(){narrative=text,options=options.ToList()};
    private static EventOption Go(string title,int page)=>new(){title=title,nextPage=page};
    private static EventOption Leave(string text="그대로 자리를 떠났다.")=>new(){title="떠난다.",resultText=text};
    private static EventOption Copy(EventOption old,string title=null,string result=null)
    {
        return new(){title=title??old.title,description=Summary(old.effects),goldCost=old.goldCost,hpCost=old.hpCost,maxHpCost=old.maxHpCost,consumeHealingPotion=old.consumeHealingPotion,requiredRelic=old.requiredRelic,requiredPerk=old.requiredPerk,requiredFlag=old.requiredFlag,effects=new(old.effects),resultText=result??"장비를 챙기고 다시 복도로 나섰다."};
    }
    private static string Summary(IEnumerable<EventEffect> effects)=>string.Join(" · ",effects.Select(e=>e.kind switch
    {
        EventEffectKind.Relic=>e.relic?.displayName+" 획득",EventEffectKind.Gold=>"골드 +"+e.amount,EventEffectKind.Heal=>"체력 +"+e.amount,EventEffectKind.Stat=>e.stat==ModifierKind.MaxHp?"최대·현재 체력 +"+e.amount:e.stat==ModifierKind.ShieldMax?"최대 실드 +"+e.amount:"능력치 +"+e.amount,
        EventEffectKind.PerkChoice=>"새 퍽 선택",EventEffectKind.RemovePerk=>"퍽 하나 제거",EventEffectKind.UpgradePerk=>"보유 퍽 하나 강화",EventEffectKind.SpellChoice=>"주문 하나 습득",EventEffectKind.SharedRelicChoice=>"공용 유물 선택",EventEffectKind.ExclusiveChoice=>"전용 유물 선택",EventEffectKind.SacrificeRelic=>"공용 유물 하나 제거",EventEffectKind.Potion=>"포션 1개",EventEffectKind.Fight=>"전투",_=>null
    }).Where(s=>!string.IsNullOrEmpty(s)));
    private static EventOption O(RandomEventData story,int index,string title=null,string result=null)=>Copy(story.options[index],title,result);
    private static EventRequirement Req(EventRequirementKind kind,float amount=0,SpellData spell=null,string flag=null)=>new(){kind=kind,amount=amount,spell=spell,flag=flag};
    private static EventEffect Flag(string flag)=>new(){kind=EventEffectKind.SetFlag,flag=flag};
    private static void BuildDog()
    {
        var story=S("StrayDog");var supply=AshenVowGameplayPreparation.Asset<SupplyData>("Assets/Data/Behemoth/Supplies/Meat.asset");supply.displayName="고기";supply.icon=AssetDatabase.LoadAllAssetsAtPath("Assets/Art/AshenVow/Atlases/PerkAtlas.png").OfType<Sprite>().First(s=>s.name.EndsWith("_56"));EditorUtility.SetDirty(supply);
        foreach(var character in content.characters){if(character.startingSupplies.All(s=>s.supply!=supply))character.startingSupplies.Add(new(){supply=supply,count=1});EditorUtility.SetDirty(character);}
        var food=O(story,0,"먹을 것을 준다.","강아지가 허겁지겁 먹이를 먹었다. 자리를 뜨려 하자 내 곁으로 다가왔다.\n\n강아지가 동료로 합류했다!");food.consumeHealingPotion=false;food.supply=supply;food.supplyCost=1;
        var potion=O(story,0,"다친 부위를 치료해 준다.","약을 바르는 동안 강아지는 얌전히 기다렸다. 치료가 끝나자 내 손을 핥았다.\n\n강아지가 동료로 합류했다!");
        story.pages=new(){Page(story.narrative,Go("목줄을 풀어 준다.",1),Leave("난 갈 길이 바쁘다. 불쌍한 강아지를 뒤로했다.")),Page("강아지가 끼잉거리며 나를 올려다본다.",food,potion,Leave("강아지는 잠시 나를 바라보다 복도 너머로 사라졌다."))};story.pages[1].illustration=illustrations[23];EditorUtility.SetDirty(story);
    }
    private static void BuildMirror()
    {
        var s=S("Mirror");s.pages=new(){Page(s.narrative,Go("손을 들어 움직임을 확인한다.",1),Go("거울을 뒤집는다.",3),Leave("거울을 등지자 유리를 긁는 소리가 들렸다.")),Page("거울 속의 내가 무기를 천천히 휘두른다.\n\n내가 자주 쓰는 동작이다. 하지만 발을 딛는 위치가 조금 다르다.",O(s,1,"거리를 두고 동작을 따라 한다.","몇 번을 따라 하자 자세의 잘못된 부분을 알 것 같다. 거울 속의 내가 무기를 내렸다."),Go("유리 밖으로 나온 무기를 붙잡는다.",2),Leave()),Page("차가운 날이 손을 스친다. 더 당기면 유리 한 조각과 함께 떨어질 것 같다.",O(s,0,"상처를 감수하고 떼어 낸다.","팔에 긴 상처가 생겼다. 떼어 낸 조각 안에서는 아직 내 무기가 움직이고 있다."),Leave("날이 거울 안으로 들어갔다. 더 손대지 않고 물러났다.")),Page("뒷면의 은이 두껍게 벗겨져 있다.",O(s,2,"은을 긁어 챙긴다."),Leave())};EditorUtility.SetDirty(s);
    }
    private static void BuildContract()
    {
        var s=S("Contract");EventOption Sell()=>O(s,1,"계약서를 판다.","행상은 빈 서명란을 확인하고 돈을 건넸다.");EventOption Sign()=>O(s,0,"서명한다.","서명을 마치자 숨이 잠시 막혔다. 몸을 일으키니 이전보다 무기가 가볍게 느껴진다.");
        s.pages=new(){Page(s.narrative,Sell(),Go("무슨 계약인지 묻는다.",1),Leave()),Page("서명하면 힘을 얻습니다. 이 성에서 오래 버티고 싶으면 쓸 만하죠.",Go("대가를 묻는다.",2),Sell(),Leave()),Page("남은 수명의 일부입니다. 다친 상처처럼 다시 채울 수 있는 건 아니고요.",Go("효과를 확인할 수 있는지 묻는다.",3),Sign(),Sell()),Page("행상이 작은 추를 계약서 위에 올린다.\n\n봉인에서 뻗은 붉은 빛이 추를 감싸자, 무거운 쇳덩이가 손쉽게 들어 올려진다.\n\n사람에게 쓰면 수명이 빠져나갑니다. 결정하시죠.",Sign(),Sell(),Leave())};EditorUtility.SetDirty(s);
    }
    private static void BuildLostBeliever()
    {
        var s=S("LostBeliever");s.pages=new(){Page("소년 하나가 무너진 회랑 앞에서 서성이고 있다.\n\n아버지의 유품을 전해 주러 온 분을 만나야 해요. 그런데 길이 막혔어요.",Go("다른 길을 찾아본다.",2),O(s,2,"안전하게 돌아갈 길을 그려 준다."),Leave()),Page("소년이 내가 지닌 표식을 알아보고 다가온다.\n\n그 표식, 아버지의 유품을 가져오신 분께 받으셨나요?",O(s,0,"순례자가 향한 통로를 알려 준다."),O(s,2,"집으로 돌아갈 길만 그려 준다."),Leave()),Page("벽의 점검구 너머로 우회할 수 있겠다. 좁은 통로에는 날카로운 철제 잔해가 가득하다.",O(s,1,"상처를 감수하고 함께 지난다."),O(s,2,"안내도를 그려 돌아가게 한다."))};
        s.entryRoutes=new(){new(){page=1,requirements=new(){Req(EventRequirementKind.Flag,flag:"PilgrimMet")}}};EditorUtility.SetDirty(s);
    }
    private static void BuildHourglass()
    {var s=S("Hourglass");s.pages=new(){Page(s.narrative,Go("모래시계를 뒤집는다.",1),O(s,2,"주변의 깨진 유리만 챙긴다."),Leave()),Page("몸이 가벼워진다. 내가 움직이는 동안 떨어지던 종이 한 장이 공중에 멈춰 있다.\n\n유리에 금이 하나 더 생겼다.\n\n기록을 더 읽어 보니, 시간을 빌린 만큼 수명이 줄어든다고 한다.",O(s,0,"수명을 내어 주고 가져간다."),O(s,1,"기술을 익히기 전으로 시간을 되돌린다."),Leave("주변의 움직임이 돌아왔다. 더 시간을 빌리지는 않았다."))};EditorUtility.SetDirty(s);}
    private static void BuildBell()
    {var s=S("Bell");s.pages=new(){Page(s.narrative,Go("종을 꺼낼 방법을 살핀다.",1),O(s,2,"바닥의 청동 조각만 챙긴다."),Leave()),Page("들보를 조금만 들어도 종을 빼낼 수 있겠다.\n\n다만 받침이 없으면 내 어깨로 무게를 버텨야 한다.",O(s,1,"어깨로 들보를 들어 올린다."),Go("주변에서 받침을 찾는다.",2),Leave()),Page("부서진 의자와 촛대를 넣어 보았지만 무게를 견디지 못하고 휘어졌다.\n\n가지고 있는 유물이라면 버틸 수 있겠다. 대신 돌 아래에 남겨야 한다.",O(s,0,"유물 하나를 받침으로 쓴다."),O(s,2,"청동 조각만 챙긴다."),Leave())};EditorUtility.SetDirty(s);}
    private static void BuildTooth()
    {var s=S("Tooth");s.pages=new(){Page(s.narrative,O(s,0,"내 생환에 돈을 건다."),Go("당장 돈을 벌 다른 내기가 있는지 묻는다.",1),O(s,2,"장부 정리만 돕는다."),Leave()),Page("상인이 칼을 꺼내 탁자 위에 놓는다.\n\n날을 쥐고 버티는 겁니다. 먼저 손을 놓지만 않으면 돼요.",O(s,1,"칼날을 쥐고 버틴다.","피가 흘렀지만 손을 놓지 않았다. 상인이 먼저 그만하라며 돈을 꺼냈다."),Leave("상인이 칼을 치웠다. 돈을 걸지 않고 자리를 떠났다."))};EditorUtility.SetDirty(s);}
    private static void BuildScholar()
    {var s=S("Scholar");s.pages=new(){Page(s.narrative,Go("책을 건진다.",1),Leave("물이 떨어지는 소리를 뒤로하고 서고를 나왔다.")),Page("책을 펼치자 페이지가 손가락을 감싼다.\n\n한 줄은 읽고 가.\n\n기술, 영창, 약제법이 적힌 장이 차례로 펼쳐진다.",O(s,1,"이미 아는 기술을 읽는다."),Go("새로운 영창을 읽는다.",2),O(s,2,"약제법을 따라 한다."),Leave("빈 표지 쪽으로 손을 빼자 책이 다시 물 위에 떠올랐다.")),Page("영창 부분은 잉크가 흐려 읽을 수 없다.\n\n책 귀퉁이의 설명에는 피를 대면 감춰진 글자를 읽을 수 있다고 적혀 있다.",O(s,0,"피를 떨어뜨려 읽는다."),Go("다른 장을 읽는다.",1),Leave())};EditorUtility.SetDirty(s);}
    private static void BuildForge()
    {var s=S("Forge");s.pages=new(){Page(s.narrative,O(s,2,"방패만 수리한다."),Go("싸우는 자세를 봐 달라고 한다.",1),Leave()),Page("한 번 휘둘러 봐.\n\n몇 번 움직이자 장인이 발과 손목의 위치를 짚어 준다.\n\n지금 기술을 다듬을까, 나쁜 버릇 하나를 버리고 새로 배울까?",O(s,0,"익숙한 기술을 다듬는다."),O(s,1,"기술 하나를 버리고 새로 배운다."),Leave("장인은 급할수록 발밑부터 보라는 말을 남겼다."))};EditorUtility.SetDirty(s);}
    private static void BuildWell()
    {var s=S("Well");s.pages=new(){Page(s.narrative,O(s,0,"작은 주머니만 건진다."),Go("금고의 밧줄도 당겨 본다.",1),Leave()),Page("밧줄을 당기자 방패를 든 팔이 갑자기 무거워진다.\n\n금고가 올라오며 벽을 가린 물때가 벗겨졌다. 그 아래에 경고문이 있다.\n\n금고를 가져가는 자는 그 무게도 함께 짊어진다.",O(s,1,"저주를 감수하고 금고를 가져간다."),Leave("금고를 원래 자리로 돌려놓자 팔이 다시 가벼워졌다."))};EditorUtility.SetDirty(s);}
    private static void BuildTrial()
    {var s=S("MasterTrial");s.pages=new(){Page(s.narrative,Go("이름을 바로 알려 준다.",1),Go("옆 벽에 새겨진 글을 읽는다.",2),Leave("문은 틀린 이름을 한 번 더 불렀다. 걸음을 멈추지 않았다.")),Page("그 이름을 쓸 자격이 있는지 증명하라.\n\n문 안쪽에서 무거운 발소리가 다가온다.",O(s,0,"시험을 받는다."),Go("준비할 방법이 없는지 묻는다.",2),Leave()),Page("오래된 기술과 호흡법이 새겨져 있다.",O(s,1,"기술 하나를 버리고 비문의 기술을 익힌다."),O(s,2,"호흡법을 익힌다."),Leave())};EditorUtility.SetDirty(s);}
    private static SpellData Spell(string id)=>content.spells.First(s=>s.id==id);
    private static RandomEventData Teacher(string id,string title,int act,string flag,int illustration)
    {
        var s=AshenVowGameplayPreparation.Asset<RandomEventData>("Assets/Data/Behemoth/Events/"+id+".asset");s.id=id;s.displayName=title;s.minAct=act;s.maxAct=3;s.requiredFlag=flag;s.weight=1.5f;s.illustration=illustrations[illustration];s.referenceUrl="codex://threads/01a165fb-89c0-7853-894b-62442f48a155";s.referenceExperience="사용자 고전학파 마법선생 연속 사건. Docs/Production/EventSequences-Castle.md";s.options=new(){Leave()};if(!content.events.Contains(s))content.events.Add(s);return s;
    }
    private static EventOption Lesson(string title,SpellData spell,string flag,int failure=-1,params EventRequirement[] requirements)
    {var o=new EventOption{title=title,description=spell.description,effects=new(){new(){kind=EventEffectKind.LearnSpell,spell=spell},Flag(flag)},resultText="선생이 마지막 영창을 고쳐 주고 책을 덮었다.",failurePage=failure,requirements=requirements.ToList()};var unknown=Req(EventRequirementKind.KnowsSpell,spell:spell);unknown.invert=true;o.requirements.Add(unknown);return o;}
    private static void BuildTeacher()
    {
        var first=Teacher("MagicTeacherFirst","칠판 앞의 노인",1,null,20);
        var gold=new EventOption{title="마법보다는 여비가 필요하다고 한다.",effects=new(){new(){kind=EventEffectKind.Gold,amount=20}},resultText="그렇다면 이걸 받으세요. 다음에는 뒤도 좀 보면서 다니겠습니다."};
        first.pages=new(){Page("빈 강의실에서 노인이 벽에 무언가를 적고 있다.\n\n옆문 뒤에서 괴물이 기어 나오는데도 눈치채지 못한 모양이다.",Go("노인을 덮치려는 괴물을 막는다.",1),Leave("강의실에서 멀어지자 뒤늦게 노인의 호통과 폭발음이 들렸다.")),Page("괴물을 밀어내자 노인이 뒤를 돌아본다.\n\n아, 고맙습니다. 설명할 부분만 생각하다 보면 주변을 잘 못 보거든요.\n\n고전학파에서 마법을 가르쳤다고 한다. 지금은 성에 남은 기록을 조사하고 있다는 모양이다.\n\n마법에는 관심 없으세요? 도와주신 보답은 하고 싶은데.",Go("배워 보고 싶다고 한다.",2),gold),Page("노인이 손끝에 작은 불꽃을 만든다.\n\n파이어볼트부터 하죠. 작다고 얕보면 안 됩니다. 빨리 쓸 수 있는 주문이 오래 쓸 주문이니까요.\n\n가르쳐 준 영창을 따라 하자 불꽃이 날아가 빈 화로에 꽂힌다.\n\n좋아요. 큰 마법도 배우고 싶다면 마력을 담을 그릇부터 키워 두세요.",Lesson("파이어볼트를 배운다.",Spell("Ember"),"TeacherLesson1")),Page("그 불꽃은 이미 익혔군요. 그렇다면 다른 기초를 봅시다.\n\n마력의 흐름을 차갑게 만드는 법과 무기에 불꽃을 싣는 법, 어느 쪽이 궁금하세요?",Lesson("서리 파동을 배운다.",Spell("FrostRing"),"TeacherLesson1"),Lesson("인챈트 파이어를 배운다.",Spell("EnchantFire"),"TeacherLesson1"),gold)};
        first.pages[2].illustration=illustrations[21];first.pages[3].illustration=illustrations[21];
        // Route at the offer, after the rescue, so knowing Firebolt never skips the scene.
        first.pages[1].options[0].nextPage=4;first.pages.Add(Page("먼저 손끝에 마력을 모아 보세요.",Go("영창을 따라 한다.",2)));
        first.pages[4].routes=new(){new(){page=3,requirements=new(){Req(EventRequirementKind.KnowsSpell,spell:Spell("Ember"))}},new(){page=2}};
        first.entryRoutes.Clear();EditorUtility.SetDirty(first);
        var second=Teacher("MagicTeacherReturn","성장한 제자",2,"TeacherLesson1",21);
        second.pages=new(){Page("연구실 안에서 익숙한 목소리가 들린다.\n\n지난번에 가르쳐 드린 분이군요. 잠깐, 손을 좀 펴 보시겠어요?\n\n벌써 이만큼 익혔어요? 이렇게 빠른 분은 드문데.\n\n다른 것도 배워 볼 생각 있으세요?",Go("더 배워 보고 싶다.",1),Leave("노인에게 성 안의 막힌 통로와 옛 강의실 이야기를 들었다. 헤어질 때까지 수업 이야기는 다시 나오지 않았다.")),Page("작은 불꽃을 익혔으니, 이번에는 불이 퍼지는 방식을 배워도 되겠네요.\n\n다른 속성이나 무기에 마력을 싣는 쪽이 궁금하면 그것도 말씀하시고요.",Lesson("불을 한꺼번에 터뜨리는 법을 배운다.",Spell("FireBloom"),"TeacherLesson2",3,Req(EventRequirementKind.KnowsSpell,spell:Spell("Ember"))),Lesson("다른 속성의 기초를 배운다.",Spell("ChainLightning"),"TeacherLesson2"),Go("무기에 마력을 싣는 법을 묻는다.",2),Go("아주 큰 마법을 배우고 싶다고 한다.",3)),Page("무기는 마력이 머무를 자리를 만들어 줍니다. 어느 속성을 익힐까요?",Lesson("인챈트 파이어",Spell("EnchantFire"),"TeacherLesson2"),Lesson("인챈트 아이스",Spell("EnchantIce"),"TeacherLesson2"),Lesson("인챈트 라이트닝",Spell("EnchantLightning"),"TeacherLesson2"),Go("다른 수업을 묻는다.",1)),Page("음… 그거까지는 아직 자질이 안 보이는데요.\n\n지금은 마력을 더 담는 연습부터 하는 게 좋겠습니다. 배울 수 있는 것부터 해 볼까요?",Go("가능한 다른 수업을 고른다.",1),Leave()),Page("아, 당신. 아직 살아 있었군요?\n\n노인이 반가운 얼굴로 자리를 비켜 준다.\n\n지난번 마법은 좀 써 보셨어요? 손끝을 보니 아직 익숙하지 않으신 모양이네요.\n\n오늘은 저도 기록을 정리해야 해서요. 잠깐 앉아 쉬었다 가세요.\n\n차가 식기 전에 미궁의 옛 구조에 관한 이야기를 들었다. 노인은 책들을 챙겨 다른 연구실로 떠났다.",Leave())};
        second.entryRoutes=new(){new(){page=0,requirements=new(){Req(EventRequirementKind.SpellCasts,3),Req(EventRequirementKind.MaxMp,33)}},new(){page=4}};EditorUtility.SetDirty(second);
        var advanced=Teacher("MagicTeacherAdvanced","의식실의 수업",3,"TeacherLesson2",22);
        advanced.pages=new(){Page("의식실 바닥에 커다란 마법진이 그려져 있다.\n\n선생이 내 마력을 살피더니 이번에는 먼저 책을 펼친다.\n\n이제는 더 큰 주문을 버틸 수 있겠네요. 어느 쪽을 배워 볼까요?",Lesson("한곳에 큰 불덩이를 떨어뜨리는 법.",Spell("Meteor"),"TeacherMastered",1,Req(EventRequirementKind.KnowsSpell,spell:Spell("FireBloom"))),Lesson("번개를 한곳에 모으는 법.",Spell("VortexLightning"),"TeacherMastered",1,Req(EventRequirementKind.KnowsSpell,spell:Spell("ChainLightning"))),Lesson("얼음 창을 쏟아붓는 법.",Spell("IcicleShower"),"TeacherMastered",1,Req(EventRequirementKind.KnowsSpell,spell:Spell("FrostRing"))),Lesson("꺼지지 않는 홍염을 펼치는 법.",Spell("CrimsonInferno"),"TeacherMastered",1,Req(EventRequirementKind.KnowsSpell,spell:Spell("FireBloom")))),Page("음… 그 속성까지는 아직 자질이 안 보이는데요. 익힌 마법에서 더 나아가는 게 좋겠습니다.",Go("다른 수업을 고른다.",0),Leave()),Page("선생은 의식실의 마법진을 덮고 기록을 정리하고 있다.\n\n아직 큰 주문을 버틸 만큼 마력이 모이지 않았군요. 오늘은 성의 기록을 좀 읽고 가세요.\n\n함께 오래된 기록을 읽고 의식실을 나왔다.",Leave())};
        advanced.entryRoutes=new(){new(){page=0,requirements=new(){Req(EventRequirementKind.MaxMp,36),Req(EventRequirementKind.SpellCasts,12),Req(EventRequirementKind.LearnedSpells,3)}},new(){page=2}};EditorUtility.SetDirty(advanced);
    }
}
