using System.Linq;
using DungeonSlash;
using UnityEditor;

internal static partial class BehemothContentBuilder
{
    private static EventEffect Burden(string kind, int battles) => Fx(EventEffectKind.Burden, battles, flag: kind);
    private static void Revise(string id, string title, string narrative, params EventOption[] options)
    {
        var story = content.events.Single(item => item.id == id);
        story.displayName = title; story.narrative = narrative; story.options = options.ToList(); EditorUtility.SetDirty(story);
    }
    private static void ReviseEvents()
    {
        Revise("StrayDog", "이 녀석, 놓질 않는다", "목줄을 풀어 주자 다친 강아지가 옷자락을 덥석 문다.\n\n한 발짝. 또 한 발짝. 절뚝거리면서도 기어이 따라온다.\n\n…데리고 가려면 뭔가 해 줘야겠다.",
            Cost(Opt("내 약을 나누자", "회복 포션 1개 소모. 강아지가 합류한다.\n4초마다 체력이 낮은 적을 협공한다.", Fx(EventEffectKind.Relic, power: RelicPower.StrayDog)), potion: true),
            Cost(Opt("우선 배부터 채우자", "35골드 소모. 강아지가 합류한다.", Fx(EventEffectKind.Relic, power: RelicPower.StrayDog)), gold: 35),
            Opt("걸을 수 있을 때까진 업고 가자", "강아지가 합류한다. 다음 2전투 공격 쿨타임 +20%.\n등 위에서 꼬리가 바쁘게 흔들린다.", Burden("Fatigue", 2), Fx(EventEffectKind.Relic, power: RelicPower.StrayDog)));
        Revise("Symbiosis", "두 번째 맥박", "씨앗을 떼려 하자 심장 쪽에서 통증이 느껴진다.\n\n뿌리는 아직 피부를 뚫지 않았는데.",
            Cost(Opt("네 몫까지 싸우지", "최대 체력 -12. 공생하는 씨앗 획득:\n일반 피해 +40%, 차지 피해 +30%.", Fx(EventEffectKind.Relic, power: RelicPower.Parasite)), maxHp: 12),
            Opt("뿌리를 끊는 법을 배운다", "퍽 하나 선택. 다음 2전투 모든 피해 -20%.\n잘라낸 뿌리가 손 안에서 아직도 움직인다.", Burden("BluntEdge", 2), Fx(EventEffectKind.PerkChoice)));
        Revise("Mirror", "거울이 먼저 벤다", "거울 속의 당신이 검을 든다. 당신은 아직 손도 움직이지 않았다.\n\n유리 너머의 입이 웃는다.\n“이번엔 네가 따라 해.”",
            Cost(Opt("한 번 베이고, 한 조각 챙긴다", "체력 -16. 기억하는 거울 조각 획득:\n매 4번째 무기 공격에 피해의 40% 추가.", Fx(EventEffectKind.Relic, power: RelicPower.MirrorShard)), hp: 16),
            Opt("끝까지 동작을 따라 한다", "퍽 하나 선택. 다음 3전투 공격 쿨타임 +20%.\n거울은 깨졌지만 팔이 자꾸 남의 속도로 움직인다.", Burden("Fatigue", 3), Fx(EventEffectKind.PerkChoice)));
        Revise("Contract", "서명은 이미 되어 있다", "계약서 끝에 당신 이름이 적혀 있다. 서명까지 똑같다.\n\n행상은 두 장을 내민다.\n“수명으로 내셔도 되고, 할부도 됩니다.”",
            Cost(Opt("짧고 굵게", "최대 체력 -18. 붉은 계약서 획득: 모든 피해 +35%.", Fx(EventEffectKind.Relic, power: RelicPower.BloodContract)), maxHp: 18),
            Opt("…할부로 합시다", "붉은 계약서 획득. 골드 빚 100.\n승리마다 최대 15골드를 갚는다.", Fx(EventEffectKind.GoldDebt, 100), Fx(EventEffectKind.Relic, power: RelicPower.BloodContract)));
        Revise("Pilgrim", "짐 하나만 부탁하네", "순례자의 짐 안에는 작은 돌들이 빼곡하다.\n\n“돌마다 돌아오지 못한 사람 이름이 있네. 버리라는 말은 하지 말아 주게.”",
            Cost(Opt("짐꾼을 구해 주자", "25골드 소모. 순례자의 서약 획득:\n가드 3회마다 게이지 3 회복. 후속 사건의 인연을 맺는다.", Fx(EventEffectKind.Relic, power: RelicPower.PilgrimOath), Fx(EventEffectKind.SetFlag, flag: "PilgrimMet")), gold: 25),
            Opt("돌 몇 개는 내가 들지", "같은 서약과 인연을 얻는다. 다음 2전투 최대 가드 -8.", Burden("CrackedGuard", 2), Fx(EventEffectKind.Relic, power: RelicPower.PilgrimOath), Fx(EventEffectKind.SetFlag, flag: "PilgrimMet")),
            Need(Opt("얘가 길을 찾을 거야", "강아지가 지름길을 찾는다. 서약 획득, 체력 12 회복.", Fx(EventEffectKind.Relic, power: RelicPower.PilgrimOath), Fx(EventEffectKind.Heal, 12), Fx(EventEffectKind.SetFlag, flag: "PilgrimMet")), RelicPower.StrayDog));
        Revise("LostBeliever", "그 돌에 적힌 이름", "신도가 쥔 돌에 익숙한 이름이 적혀 있다. 순례자의 짐에서 본 이름이다.\n\n“아버지가 이쪽으로 가셨다는데…”\n그의 등불 안에서 작은 빛이 깜빡인다.",
            Need(Opt("서약의 돌을 맞춰 본다", "길잡이의 등불 획득. 위습이 주문과 차지에 추가 피해 10.\n신도는 돌을 내려놓고 불빛을 당신에게 맡긴다.", Fx(EventEffectKind.Relic, power: RelicPower.GuidingWisp)), RelicPower.PilgrimOath),
            Opt("함께 돌아갈 길을 찾는다", "같은 등불 획득. 다음 2전투 공격 쿨타임 +20%.", Burden("Fatigue", 2), Fx(EventEffectKind.Relic, power: RelicPower.GuidingWisp)));
        Revise("Hourglass", "내일의 한숨", "모래시계를 뒤집자 내일 쉬어야 할 숨이 지금 폐 안으로 밀려든다.\n\n몸은 가벼워졌다. 유리에는 금이 하나 더 늘었다.",
            Cost(Opt("내일을 조금 잘라 낸다", "최대 체력 -8. 깨어진 모래시계 획득:\n주문 쿨타임과 차지 준비시간 -15%.", Fx(EventEffectKind.Relic, power: RelicPower.Hourglass)), maxHp: 8),
            Opt("빚진 숨은 걸으며 갚는다", "같은 모래시계 획득. 다음 3전투 공격 쿨타임 +20%.", Burden("Fatigue", 3), Fx(EventEffectKind.Relic, power: RelicPower.Hourglass)));
        Revise("StormBottle", "뚜껑을 열어 보셨군요", "병을 여는 순간 번개가 소매 안으로 들어간다.\n\n“아, 시음은 없는데. 병을 사시거나 내보내는 법을 배우셔야겠네요.”",
            Cost(Opt("병째로 산다", "45골드 소모. 병 속의 폭풍 획득:\n무기 차지 적중에 번개 피해 20 추가.", Fx(EventEffectKind.Relic, power: RelicPower.StormBottle)), gold: 45),
            Cost(Opt("내보내는 법을 배운다", "25골드 소모. 주문 하나 선택.", Fx(EventEffectKind.SpellChoice)), gold: 25),
            Opt("일단 방패에 가둔다", "주문 하나 선택. 다음 2전투 최대 가드 -8.\n방패 안쪽에서 지지직 소리가 난다.", Burden("CrackedGuard", 2), Fx(EventEffectKind.SpellChoice)));
        Revise("AmberSeed", "딱 하나 남은 열매", "배고픈 뿌리가 발목을 붙든다. 가지 끝에는 열매 하나, 그 안에는 씨앗 하나.\n\n지금 먹을 것인가. 앞으로 먹여 살릴 것인가.",
            Cost(Opt("내 피로 싹을 틔운다", "체력 -10. 호박빛 싹 획득:\n전투 3승마다 최대·현재 체력 +4.", Fx(EventEffectKind.Relic, power: RelicPower.AmberSeed)), hp: 10),
            Opt("씨앗째 씹어 삼킨다", "체력 20 회복. 다음 1전투 공격 쿨타임 +20%.\n배는 부른데 발걸음이 무겁다.", Fx(EventEffectKind.Heal, 20), Burden("Fatigue", 1)));
        Revise("Bell", "소리 나는 쪽을 받쳐라", "잔해 속 종은 당신이 숨을 쉴 때마다 울린다.\n\n꺼내려면 무언가 받쳐야 한다. 튼튼한 물건. 혹은 당신의 방패.",
            Opt("가져온 유물로 받친다", "공용 유물 하나 제거. 수호의 종 획득:\n최대 가드 +10, 전투 시작 3초 받는 피해 -20%.", Fx(EventEffectKind.SacrificeRelic), Fx(EventEffectKind.Relic, power: RelicPower.GuardianBell)),
            Cost(Opt("어깨로 들어 올린다", "체력 -20. 수호의 종 획득.", Fx(EventEffectKind.Relic, power: RelicPower.GuardianBell)), hp: 20),
            Opt("방패를 쐐기로 쓴다", "수호의 종 획득. 다음 3전투 최대 가드 -8.", Burden("CrackedGuard", 3), Fx(EventEffectKind.Relic, power: RelicPower.GuardianBell)));
        Revise("Banner", "아직 교대 시간이 아니다", "깃발 아래 빈 갑옷이 당신을 막아선다.\n“교대자. 자격을 증명하라.”\n\n시선을 피하자 보급 상자를 내민다.\n“그럼 짐이라도 들어라.”",
            Opt("내가 교대한다", "수호자와 전투. 승리 시 이름 없는 깃발 획득:\n살아 있는 적 하나당 무기 피해 +12%.", Fx(EventEffectKind.Fight, fight: "Trial"), Fx(EventEffectKind.Relic, power: RelicPower.RaggedBanner)),
            Opt("보급 상자를 짊어진다", "공용 유물 하나 선택. 다음 3전투 공격 쿨타임 +20%.", Burden("Fatigue", 3), Fx(EventEffectKind.SharedRelicChoice)));
        Revise("Tooth", "당신이 죽는 쪽에 걸었는데", "금니의 상인이 장부를 덮는다.\n\n“계속 살아 계시니 손해가 막심해서. 이젠 같은 편 먹읍시다.”",
            Cost(Opt("내 돈도 건다", "40골드 소모. 금빛 이빨 획득:\n전투 승리마다 골드 +10.", Fx(EventEffectKind.Relic, power: RelicPower.GoldenTooth)), gold: 40),
            Opt("먼저 따고 갚지", "금빛 이빨 획득. 골드 빚 65. 승리마다 최대 15골드 상환.", Fx(EventEffectKind.GoldDebt, 65), Fx(EventEffectKind.Relic, power: RelicPower.GoldenTooth)));
        Revise("Training", "인형이 자세를 고쳐 준다", "인형을 한 번 치자 나무 팔이 움직여 팔꿈치를 툭 친다.\n\n두 번. 또 툭.\n\n…전혀 봐줄 생각이 없어 보인다.",
            Cost(Opt("맞으면서 익힌다", "체력 -8. 퍽 하나 선택.", Fx(EventEffectKind.PerkChoice)), hp: 8),
            Opt("오늘은 한쪽 팔만 쓴다", "퍽 하나 선택. 다음 2전투 모든 피해 -20%.", Burden("BluntEdge", 2), Fx(EventEffectKind.PerkChoice)),
            Opt("선생님 실력부터 봅시다", "수호자와 전투. 승리 후 공용 유물 하나 선택.", Fx(EventEffectKind.Fight, fight: "Trial"), Fx(EventEffectKind.SharedRelicChoice)));
        Revise("Scholar", "책은 독자를 놓아주지 않는다", "물을 털고 책을 펼쳤다. 페이지가 손가락을 감싼다.\n\n“한 줄은 읽고 가.”\n책장이 스스로 넘어간다.",
            Cost(Opt("피로 밑줄을 긋는다", "체력 -9. 주문 하나 선택.", Fx(EventEffectKind.SpellChoice)), hp: 9),
            Cost(Opt("필사 비용을 낸다", "30골드 소모. 주문 하나 선택.", Fx(EventEffectKind.SpellChoice)), gold: 30),
            Opt("방패에 적어 외운다", "주문 하나 선택. 다음 2전투 최대 가드 -8.", Burden("CrackedGuard", 2), Fx(EventEffectKind.SpellChoice)));
        Revise("Forge", "날이 아니라 버릇을 간다", "장인은 무기를 보지도 않는다.\n“한 번 휘둘러 봐.”\n불꽃 속에서 지난 전투의 버릇이 도드라진다.\n“이걸 살릴까, 갈아 없앨까?”",
            Cost(Opt("익숙한 기술을 벼린다", "30골드 소모. 보유 퍽 하나를 1단계 강화.", Fx(EventEffectKind.UpgradePerk)), gold: 30),
            Opt("하나를 버리고 두 개를 배운다", "보유 퍽 하나 제거. 새 퍽을 두 번 선택.", Fx(EventEffectKind.RemovePerk), Fx(EventEffectKind.PerkChoice), Fx(EventEffectKind.PerkChoice)),
            Opt("기초부터 다시 친다", "퍽 하나 선택. 다음 2전투 공격 쿨타임 +20%.", Burden("Fatigue", 2), Fx(EventEffectKind.PerkChoice)));
        Revise("Well", "아래에서 당신을 부른다", "우물 속에서 당신 목소리가 들린다.\n“던져. 내가 받아 줄게.”\n\n무언가 올라오고 있다. 빈손은 아닌 것 같다.",
            Cost(Opt("밧줄을 타고 내려간다", "체력 -18. 공용 유물 하나 선택.", Fx(EventEffectKind.SharedRelicChoice)), hp: 18),
            Cost(Opt("동전으로 값을 치른다", "55골드 소모. 공용 유물 하나 선택.", Fx(EventEffectKind.SharedRelicChoice)), gold: 55),
            Opt("미래의 나에게 맡긴다", "공용 유물 하나 선택. 골드 빚 75. 승리마다 최대 15골드 상환.", Fx(EventEffectKind.GoldDebt, 75), Fx(EventEffectKind.SharedRelicChoice)));
        Revise("Investment", "배달지는 다음 막", "짐꾼이 행선지를 듣더니 상자에 이름을 쓴다.\n\n“일단 예약했습니다. 보증금을 주실래요, 직접 들고 가실래요?”",
            Cost(Opt("도착해서 받는다", "25골드 소모. 다음 막의 배달에서 유물과 골드를 받는다.", Fx(EventEffectKind.SetFlag, flag: "DeliveryWaiting")), gold: 25),
            Opt("상자는 내가 들겠다", "다음 3전투 공격 쿨타임 +20%. 같은 배달을 예약한다.", Burden("Fatigue", 3), Fx(EventEffectKind.SetFlag, flag: "DeliveryWaiting")));
        Revise("Delivery", "상자가 두 개다", "상자 하나에는 당신 이름이, 다른 하나에는 아직 태어나지 않은 당신 이름이 적혀 있다.\n\n“그쪽 건 추가 요금인데…”",
            Opt("내 이름만 가져간다", "공용 유물 하나 선택, 골드 +45. 배달을 마친다.", Fx(EventEffectKind.SharedRelicChoice), Fx(EventEffectKind.Gold, 45), Fx(EventEffectKind.RemoveFlag, flag: "DeliveryWaiting")),
            Opt("미래의 내 것도 뜯는다", "전용 유물 하나 선택. 골드 빚 80. 배달을 마친다.", Fx(EventEffectKind.GoldDebt, 80), Fx(EventEffectKind.ExclusiveChoice), Fx(EventEffectKind.RemoveFlag, flag: "DeliveryWaiting")));
        Revise("MasterTrial", "문이 이름을 틀리게 부른다", "문에 새겨진 입이 당신 이름을 부른다. 한 글자가 틀렸다.\n\n정정하자 안쪽에서 무거운 발소리가 난다.\n“그럼 직접 증명해라.”",
            Opt("정확히 기억하게 해 주지", "심층 수호자와 전투. 승리 후 전용 유물 하나 선택.", Fx(EventEffectKind.Fight, fight: "DeepTrial"), Fx(EventEffectKind.ExclusiveChoice)),
            Opt("틀린 이름으로 기술을 배운다", "퍽을 두 번 선택. 다음 3전투 모든 피해 -20%.", Burden("BluntEdge", 3), Fx(EventEffectKind.PerkChoice), Fx(EventEffectKind.PerkChoice)));
        Revise("Shelter", "불침번을 정하자", "불을 피우자 어둠 속의 눈들이 한꺼번에 감긴다.\n\n불이 꺼지면 다시 뜨겠지.\n잠을 잘 사람과 불을 지킬 사람을 정해야 한다.",
            Opt("불을 지키며 쉰다", "체력 +24. 다음 1전투 공격 쿨타임 +20%.", Fx(EventEffectKind.Heal, 24), Burden("Fatigue", 1)),
            Cost(Opt("밤새 몸을 단련한다", "체력 -12 후 최대·현재 체력 +8.", Fx(EventEffectKind.Stat, 8, stat: ModifierKind.MaxHp)), hp: 12),
            Need(Opt("너 먼저 자. 다음은 내 차례야", "강아지와 번갈아 망을 본다. 체력 +28, 골드 +12.\n밤새 불은 꺼지지 않았다.", Fx(EventEffectKind.Heal, 28), Fx(EventEffectKind.Gold, 12)), RelicPower.StrayDog));
    }
}
