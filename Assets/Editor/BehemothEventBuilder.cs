using System.Collections.Generic;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

internal static partial class BehemothContentBuilder
{
    private static EventEffect Fx(EventEffectKind kind, float amount = 0, string flag = null, RelicPower power = RelicPower.None, ModifierKind stat = ModifierKind.MaxHp, string fight = null)
        => new() { kind = kind, amount = amount, flag = flag, relic = power == RelicPower.None ? null : relics[power], stat = stat, encounter = fight == null ? null : encounters[fight] };
    private static EventOption Opt(string title, string description, params EventEffect[] effects)
        => new() { title = title, description = description, effects = new List<EventEffect>(effects) };
    private static EventOption Leave() => Opt("지나친다", "아무 대가도 지불하지 않고 탐험을 계속한다.");
    private static EventOption Cost(EventOption option, int gold = 0, float hp = 0, float maxHp = 0, bool potion = false)
    { option.goldCost = gold; option.hpCost = hp; option.maxHpCost = maxHp; option.consumeHealingPotion = potion; return option; }
    private static EventOption Need(EventOption option, RelicPower power = RelicPower.None, string flag = null, string perk = null)
    { option.requiredPower = power; option.requiredFlag = flag; option.requiredPerk = perk; return option; }
    private static RandomEventData Story(string id, string title, string text, string art, params EventOption[] options)
    {
        var story = Asset<RandomEventData>(Root + "/Events/" + id + ".asset");
        story.id = id; story.displayName = title; story.narrative = text;
        story.illustration = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/T_UI_Event" + art + ".png");
        story.options = new List<EventOption>(options); story.minAct = 1; story.maxAct = 3; story.weight = 1;
        story.requiredFlag = ""; story.forbiddenFlag = "";
        content.events.Add(story); EditorUtility.SetDirty(story); return story;
    }
    private static void BuildEvents()
    {
        content.events.Clear();
        Story("StrayDog", "젖은 발자국", "무너진 문 뒤에서 다친 강아지가 몸을 떤다. 다가가자 꼬리가 아주 조금 움직인다.", "Fountain",
            Cost(Opt("상처를 치료한다", "이벤트 전용 유물 '작은 발자국'. 강아지가 동료로 합류해 전투 중 협공한다.", Fx(EventEffectKind.Relic, power: RelicPower.StrayDog)), potion: true),
            Cost(Opt("먹을 것을 나눈다", "골드로 식량을 마련한다. 같은 동료 유물을 얻는다.", Fx(EventEffectKind.Relic, power: RelicPower.StrayDog)), gold: 35), Leave());
        Story("Symbiosis", "숨 쉬는 씨앗", "따뜻한 씨앗이 손바닥의 맥박에 맞춰 뛴다. 뿌리내릴 자리를 찾고 있다.", "Goddess",
            Cost(Opt("몸에 받아들인다", "공생하는 씨앗: 일반 피해 +40%, 차지 피해 +30%.", Fx(EventEffectKind.Relic, power: RelicPower.Parasite)), maxHp: 12),
            Opt("껍질만 챙긴다", "골드 18 획득.", Fx(EventEffectKind.Gold, 18)), Leave());
        Story("Mirror", "금 간 거울", "거울 속의 검이 당신보다 한 박자 늦게 움직인다. 조각을 떼어내면 이 잔상을 가지고 갈 수 있을 듯하다.", "Goddess",
            Cost(Opt("거울 조각을 꺼낸다", "기억하는 거울 조각: 매 4번째 무기 공격에 피해 40% 추가.", Fx(EventEffectKind.Relic, power: RelicPower.MirrorShard)), hp: 16),
            Opt("내 모습을 관찰한다", "퍽 후보 중 하나를 선택한다.", Fx(EventEffectKind.PerkChoice)), Leave());
        Story("Contract", "붉은 서명", "잉크가 마르지 않는 계약서. 힘을 빌려주겠다는 문장 아래, 당신의 수명이 빈칸으로 남아 있다.", "Merchant",
            Cost(Opt("피로 서명한다", "붉은 계약서: 모든 피해 +35%.", Fx(EventEffectKind.Relic, power: RelicPower.BloodContract)), maxHp: 18),
            Opt("계약서를 판다", "골드 25 획득. 계약의 힘은 얻지 않는다.", Fx(EventEffectKind.Gold, 25)), Leave());
        Story("Pilgrim", "갈림길의 순례자", "순례자는 무거운 짐을 내려놓고 기도한다. 동행하지 않더라도 서로에게 작은 약속은 남길 수 있다.", "Goddess",
            Cost(Opt("여비를 나눈다", "순례자의 서약: 퍼펙트가드 시 체력 2 회복. 후속 사건의 실마리.", Fx(EventEffectKind.Relic, power: RelicPower.PilgrimOath), Fx(EventEffectKind.SetFlag, flag: "PilgrimMet")), gold: 25),
            Opt("기도를 배운다", "현재 체력 12 회복.", Fx(EventEffectKind.Heal, 12)),
            Need(Opt("재생의 호흡을 나눈다", "'재생의 호흡' 퍽을 보유하면 체력 22 회복과 골드 12 획득.", Fx(EventEffectKind.Heal, 22), Fx(EventEffectKind.Gold, 12)), perk: "Recovery"), Leave());
        var lost = Story("LostBeliever", "길 잃은 신도", "낡은 표식을 쥔 신도가 순례자의 행방을 묻는다. 약속을 증명하거나, 길을 찾아줄 다른 방법이 필요하다.", "Goddess",
            Need(Opt("서약을 보여준다", "길잡이의 등불: 주문 발동과 차지 적중에 위습의 추가 공격.", Fx(EventEffectKind.Relic, power: RelicPower.GuidingWisp)), RelicPower.PilgrimOath),
            Cost(Opt("길을 함께 찾는다", "위험한 샛길에서 체력을 소비하고 같은 유물을 얻는다.", Fx(EventEffectKind.Relic, power: RelicPower.GuidingWisp)), hp: 18), Leave());
        lost.minAct = 2; lost.requiredFlag = "PilgrimMet";
        Story("Hourglass", "멈춘 모래", "모래알은 공중에 멈춰 있는데, 모래시계를 쥔 손만 빠르게 늙어 간다.", "Fountain",
            Cost(Opt("흐름을 가져온다", "깨어진 모래시계: 주문 쿨타임과 차지 준비시간 -15%.", Fx(EventEffectKind.Relic, power: RelicPower.Hourglass)), maxHp: 8),
            Cost(Opt("잠시만 시간을 빌린다", "차지 준비시간 -12%. 영구적인 이번 런 보정.", Fx(EventEffectKind.Stat, .12f, stat: ModifierKind.ChargeDurationMultiplier)), gold: 25), Leave());
        Story("StormBottle", "번개를 파는 행상", "유리병 안에서 번개가 작은 뱀처럼 몸을 튼다. 행상은 '영창을 몰라도 된다'며 병을 두드린다.", "Merchant",
            Cost(Opt("폭풍 한 병", "병 속의 폭풍: 차지 적중에 번개 피해 20 추가.", Fx(EventEffectKind.Relic, power: RelicPower.StormBottle)), gold: 45),
            Cost(Opt("주문을 배운다", "아직 모르는 주문 3종 중 하나를 습득.", Fx(EventEffectKind.SpellChoice)), gold: 25), Leave());
        Story("AmberSeed", "호박빛 정원", "빛나는 새싹은 오래 걸을 사람을 기다린다. 지금 먹을 열매를 남기면 훗날 더 깊이 뿌리내릴 것이다.", "Fountain",
            Cost(Opt("새싹을 가져간다", "호박빛 싹: 이후 전투 3승마다 최대·현재 체력 +4.", Fx(EventEffectKind.Relic, power: RelicPower.AmberSeed)), hp: 10),
            Opt("열매를 먹는다", "지금 체력 20 회복. 새싹은 남긴다.", Fx(EventEffectKind.Heal, 20)), Leave()).maxAct = 2;
        Story("Bell", "무너진 종탑", "작은 종이 돌무더기 아래에서 계속 울린다. 누군가의 물건을 남겨두면 대신 종을 꺼낼 수 있겠다.", "Goddess",
            Opt("유물로 받친다", "보유 공용 유물 하나를 선택해 잃는다. 수호의 종: 최대 가드 +10, 전투 시작 3초 피해 -20%.", Fx(EventEffectKind.SacrificeRelic), Fx(EventEffectKind.Relic, power: RelicPower.GuardianBell)),
            Cost(Opt("잔해를 들어 올린다", "체력을 소비하고 같은 종을 얻는다.", Fx(EventEffectKind.Relic, power: RelicPower.GuardianBell)), hp: 20), Leave());
        Story("Banner", "이름 없는 수비대", "돌아오지 못한 수비대의 깃발이 찢긴 채 남아 있다. 그 아래에는 아직 움직이는 갑옷이 서 있다.", "Gold",
            Opt("마지막 경계를 돌파한다", "고정 강적 편성과 전투. 승리 보상에 더해 이름 없는 깃발 획득: 적 하나당 무기 피해 +12%.", Fx(EventEffectKind.Fight, fight: "Trial"), Fx(EventEffectKind.Relic, power: RelicPower.RaggedBanner)),
            Opt("보급품만 찾는다", "골드 15 획득.", Fx(EventEffectKind.Gold, 15)), Leave());
        Story("Tooth", "금빛 이빨의 내기", "상인은 당신의 생환에 돈을 걸겠다고 한다. 보증금을 내면 매번 승리할 때마다 배당을 돌려준다고 한다.", "Merchant",
            Cost(Opt("계약금을 건다", "금빛 이빨: 이후 전투 승리마다 골드 +10.", Fx(EventEffectKind.Relic, power: RelicPower.GoldenTooth)), gold: 40),
            Opt("도박 대신 휴식", "체력 10 회복.", Fx(EventEffectKind.Heal, 10)), Leave()).maxAct = 2;
        Story("Training", "부러진 훈련 인형", "낡은 인형에는 서로 다른 세 종류의 무기 자국이 남아 있다. 약한 상대에게 배울 것인지, 진짜 시험을 치를 것인지.", "Gold",
            Cost(Opt("동작을 반복한다", "퍽 3개 중 하나 선택.", Fx(EventEffectKind.PerkChoice)), hp: 8),
            Opt("실전 시험을 신청한다", "강적과 전투 후 공용 유물 3개 중 하나 선택. 일반 전투 횟수에는 포함되지 않는다.", Fx(EventEffectKind.Fight, fight: "Trial"), Fx(EventEffectKind.SharedRelicChoice)), Leave());
        Story("Scholar", "젖지 않는 마도서", "물속의 책은 한 장도 젖지 않았다. 읽는 데 필요한 것은 마나가 아니라 잠깐의 집중과 대가다.", "Fountain",
            Cost(Opt("책을 펼친다", "모르는 주문 후보 3종 중 하나 습득. 모든 직업이 선택 가능.", Fx(EventEffectKind.SpellChoice)), hp: 9),
            Cost(Opt("필사본을 산다", "같은 주문 선택 기회.", Fx(EventEffectKind.SpellChoice)), gold: 30), Leave());
        Story("Forge", "잊힌 제련소", "장인은 무기 대신 당신의 습관을 벼리겠다고 한다. 이미 익힌 기술을 강화하거나, 하나를 포기하고 새 길을 고를 수 있다.", "Merchant",
            Cost(Opt("익숙한 기술을 벼린다", "중첩 가능한 보유 퍽 하나를 선택해 1단계 강화.", Fx(EventEffectKind.UpgradePerk)), gold: 30),
            Opt("습관을 내려놓는다", "보유 퍽 하나를 제거한 뒤 새 퍽 후보 중 선택.", Fx(EventEffectKind.RemovePerk), Fx(EventEffectKind.PerkChoice)), Leave());
        Story("Well", "검은 우물", "아래로 던진 동전 소리가 돌아오지 않는다. 대신 바닥에서 낯선 유물의 빛이 올라온다.", "Fountain",
            Cost(Opt("밧줄을 타고 내려간다", "공용 유물 3종 중 하나 선택.", Fx(EventEffectKind.SharedRelicChoice)), hp: 18),
            Cost(Opt("우물에 값을 지불한다", "공용 유물 3종 중 하나 선택.", Fx(EventEffectKind.SharedRelicChoice)), gold: 55), Leave());
        Story("Investment", "내일의 배달", "짐꾼은 다음 막까지 살아남는다면 지금보다 훨씬 값진 물건을 전해주겠다고 약속한다.", "Merchant",
            Cost(Opt("물건을 맡긴다", "다음 막에서 '늦은 배달' 사건이 등장할 수 있다. 보상은 공용 유물 선택과 골드 45.", Fx(EventEffectKind.SetFlag, flag: "DeliveryWaiting")), gold: 25), Leave()).maxAct = 1;
        var delivery = Story("Delivery", "늦은 배달", "지친 짐꾼이 당신을 알아보고 손을 흔든다. 약속했던 상자가 무사히 도착했다.", "Merchant",
            Opt("상자를 받는다", "공용 유물 선택과 골드 45. 맡긴 거래를 마친다.", Fx(EventEffectKind.SharedRelicChoice), Fx(EventEffectKind.Gold, 45), Fx(EventEffectKind.RemoveFlag, flag: "DeliveryWaiting")), Leave());
        delivery.minAct = 2; delivery.requiredFlag = "DeliveryWaiting"; delivery.weight = 5;
        var trial = Story("MasterTrial", "이름을 묻는 문", "문은 직업의 이름이 아니라 당신이 치를 수 있는 대가를 묻는다. 문 너머에는 동료를 거느린 수호자가 기다린다.", "Goddess",
            Opt("문 안의 시험에 도전", "고정 강적 편성과 전투 후, 현재 직업의 전용 유물 3종 중 하나 선택. 전용 유물 보유 한도 3개를 공유한다.", Fx(EventEffectKind.Fight, fight: "DeepTrial"), Fx(EventEffectKind.ExclusiveChoice)), Leave());
        trial.minAct = 2; trial.weight = .3f;
        Story("Shelter", "불 꺼진 야영지", "장작은 남아 있지만 먹을 것은 부족하다. 쉬어갈지, 앞으로의 체력을 위해 지금의 여유를 내어줄지 결정하자.", "Fountain",
            Opt("잔불을 피운다", "체력 18 회복.", Fx(EventEffectKind.Heal, 18)),
            Cost(Opt("밤새 몸을 단련한다", "최대·현재 체력 +8. 현재 체력 12를 먼저 지불한다.", Fx(EventEffectKind.Stat, 8, stat: ModifierKind.MaxHp)), hp: 12),
            Need(Opt("강아지와 번갈아 망을 본다", "동료가 있으면 안전하게 체력 28 회복과 골드 12 획득.", Fx(EventEffectKind.Heal, 28), Fx(EventEffectKind.Gold, 12)), RelicPower.StrayDog), Leave());
        ReviseEvents();
        foreach (var story in content.events) EditorUtility.SetDirty(story);
    }
}
