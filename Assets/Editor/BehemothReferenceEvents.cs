using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

internal static partial class BehemothContentBuilder
{
    private static void RebuildReferenceEvents()
    {
        // References describe the decision structure, not borrowed story/dialogue.
        void Event(string id, string title, string narrative, string reference, string experience, params EventOption[] choices)
        {
            Revise(id, title, narrative, choices);
            var story = content.events.Single(item => item.id == id);
            story.referenceUrl = "https://op.gg/ko/slay-the-spire2/events/" + reference;
            story.referenceExperience = experience;
            story.minNormalCombatsInAct = choices.Any(option => option.effects.Any(effect => effect.kind == EventEffectKind.SpellChoice)) ? 4 : 0;
            foreach (var option in choices)
            {
                if (option.requiredPower != RelicPower.None) option.requiredRelic = relics[option.requiredPower];
                option.requiredPower = RelicPower.None;
            }
            EditorUtility.SetDirty(story);
        }
        Event("StrayDog", "젖은 발자국", "문틈에서 작은 코가 나온다. 뒤따라 나온 발 하나에는 철사가 감겨 있다.\n\n철사를 끊어 주자 강아지가 절뚝이며 따라온다. 약이나 먹을 것이 있다면 더 멀리 함께 걸을 수 있겠다.", "TheLegendsWereTrue", "미래의 동행을 얻는 투자와 당장 챙길 수 있는 작은 회복. 동료 서사는 이 게임 고유 내용.",
            Cost(Opt("내 약을 나눈다", "회복 포션 1개 소모. 길동무 합류: 4초마다 약한 적을 협공한다.", Fx(EventEffectKind.Relic, power: RelicPower.StrayDog)), potion: true),
            Cost(Opt("식량 한 꾸러미", "35골드 소모. 길동무가 합류한다.", Fx(EventEffectKind.Relic, power: RelicPower.StrayDog)), gold: 35),
            Opt("길가의 약초로 감싸 준다", "강아지는 안전한 야영지에 남는다. 남은 약초로 내 체력 8 회복. 동료는 얻지 않는다.", Fx(EventEffectKind.Heal, 8)));
        Event("Symbiosis", "두 번째 맥박", "씨앗을 쥔 손에서 맥박이 두 번 뛴다. 하나는 당신 것, 다른 하나는 아직 자리를 찾지 못했다.\n\n옆에는 뿌리를 태울 만한 화로가 남아 있다.", "Symbiote", "강한 공격 강화와 기존 성장 하나를 변환하는 선택.",
            Cost(Opt("뿌리내릴 자리를 준다", "최대 체력 -12. 공생하는 씨앗: 일반 무기 피해 +25%, 차지 피해 +20%.", Fx(EventEffectKind.Relic, power: RelicPower.Parasite)), maxHp: 12),
            Opt("뿌리와 함께 버릇 하나를 태운다", "보유 퍽 하나 제거 후 새 퍽을 선택한다.", Fx(EventEffectKind.RemovePerk), Fx(EventEffectKind.PerkChoice)),
            Opt("껍질을 약재로 쓴다", "체력 10 회복. 씨앗의 힘은 남긴다.", Fx(EventEffectKind.Heal, 10)));
        Event("Mirror", "거울이 먼저 벤다", "거울 속의 당신이 먼저 검을 든다. 날이 유리 밖으로 조금 튀어나온다.\n\n그대로 따라 배워도 좋겠지만, 저 움직임을 조각째 떼어 갈 수도 있겠다.", "DoorsOfLightAndDark", "즉시 얻는 특이한 힘과 이미 가진 성장의 강화.",
            Cost(Opt("한 번 베이고 한 조각 챙긴다", "체력 -16. 거울 조각: 매 4번째 무기 적중에 피해 40% 추가.", Fx(EventEffectKind.Relic, power: RelicPower.MirrorShard)), hp: 16),
            Opt("내 동작을 고쳐 배운다", "보유 퍽 하나를 1단계 강화한다.", Fx(EventEffectKind.UpgradePerk)),
            Opt("거울 뒷면의 은을 긁어낸다", "골드 +12. 거울은 더 이상 움직이지 않는다.", Fx(EventEffectKind.Gold, 12)));
        Event("Contract", "붉은 서명", "계약서에는 힘을 빌려 준다는 한 줄만 있다. 서명란은 이상하리만큼 길다.\n\n행상이 빈 잉크병을 들어 보인다.\n“그만큼의 피가 아니라, 그만큼의 미래입니다.”", "Symbiote", "최대 체력이라는 영구 대가와 강한 성장, 또는 다른 소액 보상.",
            Cost(Opt("짧고 굵게", "최대 체력 -18. 붉은 계약서: 모든 피해 +25%.", Fx(EventEffectKind.Relic, power: RelicPower.BloodContract)), maxHp: 18),
            Opt("백지 계약서를 되판다", "골드 +25. 계약의 힘은 얻지 않는다.", Fx(EventEffectKind.Gold, 25)));
        Event("Pilgrim", "짐 하나만 부탁하네", "짐 안에는 이름이 새겨진 돌들이 가득하다.\n\n“사람 대신 돌아가는 이름들이네. 여비가 모자라 이 돌을 두고 갈 뻔했어.”", "TheLegendsWereTrue", "현재 자원을 미래 인연에 투자하거나 당장 회복을 선택.",
            Cost(Opt("여비를 보탠다", "25골드 소모. 순례자의 서약 획득. 가드 3회마다 게이지 +3. 인연을 남긴다.", Fx(EventEffectKind.Relic, power: RelicPower.PilgrimOath), Fx(EventEffectKind.SetFlag, flag: "PilgrimMet")), gold: 25),
            Opt("샘의 위치를 서로 나눈다", "체력 12 회복. 서약은 얻지 않는다.", Fx(EventEffectKind.Heal, 12)),
            Need(Opt("얘가 지름길을 찾을 거야", "강아지가 길을 찾는다. 서약 획득, 체력 12 회복, 인연을 남긴다.", Fx(EventEffectKind.Relic, power: RelicPower.PilgrimOath), Fx(EventEffectKind.Heal, 12), Fx(EventEffectKind.SetFlag, flag: "PilgrimMet")), RelicPower.StrayDog));
        Event("LostBeliever", "그 돌에 적힌 이름", "소년이 쥔 돌에는 익숙한 이름이 있다. 순례자의 짐에서 본 이름이다.\n\n등불은 소년의 손을 떠나 당신 쪽으로 조금씩 기운다.", "TheLegendsWereTrue", "이전 사건의 인연으로 특별 보상, 없으면 현재 체력으로 돕거나 다른 회복 선택.",
            Need(Opt("서약의 돌을 맞춰 본다", "길잡이의 등불 획득: 차지 적중과 주문 발동에 피해 6 추가.", Fx(EventEffectKind.Relic, power: RelicPower.GuidingWisp)), RelicPower.PilgrimOath),
            Cost(Opt("위험한 샛길을 함께 걷는다", "체력 -18. 길잡이의 등불 획득.", Fx(EventEffectKind.Relic, power: RelicPower.GuidingWisp)), hp: 18),
            Opt("돌아갈 길을 그려 준다", "답례의 약초로 체력 15 회복. 등불은 소년에게 남는다.", Fx(EventEffectKind.Heal, 15)));
        Event("Hourglass", "내일의 한숨", "모래시계를 뒤집자 몸이 가벼워진다. 유리에는 금이 하나 늘었다.\n\n멈춰 선 모래알을 세다 보니, 오래된 동작 하나가 새삼 떠오른다.", "DoorsOfLightAndDark", "유용한 유물과 보유 성장 재정비 사이의 선택.",
            Cost(Opt("내일을 조금 잘라 낸다", "최대 체력 -8. 깨어진 모래시계: 차지 준비시간과 주문 쿨타임 -12%.", Fx(EventEffectKind.Relic, power: RelicPower.Hourglass)), maxHp: 8),
            Opt("오래된 동작을 버리고 다시 익힌다", "보유 퍽 하나 제거 후 새 퍽을 두 번 선택한다.", Fx(EventEffectKind.RemovePerk), Fx(EventEffectKind.PerkChoice), Fx(EventEffectKind.PerkChoice)),
            Opt("금 간 유리를 모은다", "골드 +10. 시간의 힘은 얻지 않는다.", Fx(EventEffectKind.Gold, 10)));
        Event("StormBottle", "번개를 파는 행상", "병마다 서로 다른 천둥소리가 난다. 행상은 병 하나를 귀에 대고 고개를 젓는다.\n\n“이건 뚜껑을 따면 검으로 옮겨 갑니다. 주문이 필요하시면 옆 책을 보세요.”", "RelicTrader", "서로 다른 보상의 거래. 돈이 없다고 같은 물건을 외상으로 주지 않음.",
            Cost(Opt("폭풍 한 병", "45골드 소모. 차지 적중에 번개 피해 8을 더하는 유물 획득.", Fx(EventEffectKind.Relic, power: RelicPower.StormBottle)), gold: 45),
            Cost(Opt("번개를 읽는 법", "25골드 소모. 주문 하나 선택.", Fx(EventEffectKind.SpellChoice)), gold: 25),
            Opt("빈 병 수거를 돕는다", "골드 +12. 유물과 주문은 사지 않는다.", Fx(EventEffectKind.Gold, 12)));
        Event("AmberSeed", "딱 하나 남은 열매", "빛나는 열매 안에 작은 씨앗이 있다. 가지에는 피를 먹고 자란다는 표식이 남아 있다.\n\n지금 배를 채울 수도, 오래 품고 갈 수도 있겠다.", "TheLegendsWereTrue", "지금 회복과 이후 누적 성장의 선택.",
            Cost(Opt("내 피로 싹을 틔운다", "체력 -10. 호박빛 싹: 전투 3승마다 최대·현재 체력 +4.", Fx(EventEffectKind.Relic, power: RelicPower.AmberSeed)), hp: 10),
            Opt("씨앗째 씹어 삼킨다", "체력 20 회복. 씨앗은 사라진다.", Fx(EventEffectKind.Heal, 20)));
        Event("Bell", "소리 나는 쪽을 받쳐라", "종 아래에 끼어 있던 물건들이 오래전에 부서졌다. 하나를 밀어 넣으면 종을 꺼낼 수 있겠다.\n\n종을 포기하고 청동 조각만 떼어 가는 방법도 있다.", "RelicTrader", "보유 유물 선택 교환과 체력 대가, 또는 작은 고정 보상.",
            Opt("가져온 유물로 받친다", "공용 유물 하나 선택해 제거. 수호의 종: 최대 가드 +10, 전투 시작 3초 체력 피해 -20%.", Fx(EventEffectKind.SacrificeRelic), Fx(EventEffectKind.Relic, power: RelicPower.GuardianBell)),
            Cost(Opt("어깨로 들어 올린다", "체력 -20. 수호의 종 획득.", Fx(EventEffectKind.Relic, power: RelicPower.GuardianBell)), hp: 20),
            Opt("가장자리의 청동만 떼어 간다", "골드 +20. 종은 꺼내지 못한다.", Fx(EventEffectKind.Gold, 20)));
        Event("Banner", "아직 교대 시간이 아니다", "빈 갑옷이 깃발 아래 서 있다.\n“교대자. 자격을 증명하라.”\n\n뒤쪽 보급함에는 마지막 급료가 남아 있다.", "BattlewornDummy", "추가 강적 전투로 특수 보상, 또는 작은 확정 이익.",
            Opt("내가 교대한다", "수호자와 전투. 승리 시 깃발 획득: 살아 있는 적 하나당 무기 피해 +8%.", Fx(EventEffectKind.Fight, fight: "Trial"), Fx(EventEffectKind.Relic, power: RelicPower.RaggedBanner)),
            Opt("남겨진 급료를 챙긴다", "골드 +15. 깃발은 지키는 자에게 남긴다.", Fx(EventEffectKind.Gold, 15)));
        Event("Tooth", "금빛 이빨의 내기", "금니의 상인이 장부를 내민다.\n“이번엔 살아 돌아오시는 쪽에 걸죠.”\n\n탁자 아래에는 결투용 칼과 작은 금고가 있다.", "SunkenTreasury", "안전한 소액 보상과 위험한 큰 보상, 장기 투자.",
            Cost(Opt("내 돈도 건다", "40골드 소모. 금빛 이빨: 전투 승리마다 골드 +10.", Fx(EventEffectKind.Relic, power: RelicPower.GoldenTooth)), gold: 40),
            Cost(Opt("내가 이기는지 직접 보시지", "체력 -8. 골드 +65. 유물은 얻지 않는다.", Fx(EventEffectKind.Gold, 65)), hp: 8),
            Opt("내기는 됐고 품삯이나 주시오", "골드 +8.", Fx(EventEffectKind.Gold, 8)));
        BuildTrainingTrials();
        Event("Training", "오늘의 상대를 고르시오", "나무 인형 세 개가 나란히 선다. 첫째는 느슨한 끈, 둘째는 철사, 셋째는 사슬로 묶여 있다.\n\n“성적에 맞는 상은 준비되어 있습니다.”", "BattlewornDummy", "체력이 다른 세 훈련 상대와 서로 다른 보상을 직접 고르는 전투.",
            Opt("끈을 푼다", "연습 인형 체력 75. 승리 후 포션 1개. 포션이 가득하면 체력 8 회복.", Fx(EventEffectKind.Fight, fight: "TrainingLight"), Fx(EventEffectKind.Potion)),
            Opt("철사를 끊는다", "연습 인형 체력 140. 승리 후 보유 퍽을 두 번 강화.", Fx(EventEffectKind.Fight, fight: "TrainingMedium"), Fx(EventEffectKind.UpgradePerk), Fx(EventEffectKind.UpgradePerk)),
            Opt("사슬을 푼다", "연습 인형 체력 240. 승리 후 공용 유물 하나 선택.", Fx(EventEffectKind.Fight, fight: "TrainingHeavy"), Fx(EventEffectKind.SharedRelicChoice)));
        Event("Scholar", "끝까지 읽는 법", "물에서 건진 책이 손가락을 붙든다.\n“한 줄은 읽고 가.”\n\n공격을 다루는 장, 방어를 다루는 장, 주문을 다루는 장이 저절로 펼쳐진다.", "SelfHelpBook", "보유 성장 강화와 새 주문 습득을 서로 다른 선택으로 제공.",
            Cost(Opt("새 영창에 피로 밑줄을 긋는다", "체력 -9. 주문 하나 선택.", Fx(EventEffectKind.SpellChoice)), hp: 9),
            Opt("이미 아는 구절을 다시 읽는다", "보유 퍽 하나를 강화한다.", Fx(EventEffectKind.UpgradePerk)),
            Opt("약제법 한 쪽을 베낀다", "포션 1개. 포션이 가득하면 체력 8 회복.", Fx(EventEffectKind.Potion)));
        Event("Forge", "날이 아니라 버릇을 간다", "장인은 검을 보지도 않는다.\n“한 번 휘둘러 봐.”\n\n불꽃 속에서 지난 전투의 버릇이 도드라진다.\n“남길까, 갈아 없앨까?”", "DoorsOfLightAndDark", "이미 가진 성장의 강화와 하나를 버리는 재구성.",
            Cost(Opt("익숙한 기술을 벼린다", "30골드 소모. 보유 퍽 하나를 강화.", Fx(EventEffectKind.UpgradePerk)), gold: 30),
            Opt("하나를 버리고 두 개를 배운다", "보유 퍽 하나 제거 후 새 퍽 두 번 선택.", Fx(EventEffectKind.RemovePerk), Fx(EventEffectKind.PerkChoice), Fx(EventEffectKind.PerkChoice)),
            Opt("방패의 찌그러진 곳만 편다", "최대 가드게이지 +3. 퍽은 바뀌지 않는다.", Fx(EventEffectKind.Stat, 3, stat: ModifierKind.ShieldMax)));
        var curse = Asset<EquipmentData>(Root + "/Relics/WellCurse.asset"); curse.id = "WellCurse"; curse.displayName = "우물 밑의 무게"; curse.description = "최대 가드게이지 -8. 검은 우물에서 가져온 금의 무게다."; curse.scope = RelicScope.Event; curse.icon = relics[RelicPower.Hourglass].icon; curse.abilities.Clear(); Add(curse, TriggerType.OnAcquire, TargetType.Owner, Stat(ModifierKind.ShieldMax, -8)); if (!content.equipment.Contains(curse)) content.equipment.Add(curse); EditorUtility.SetDirty(curse);
        Event("Well", "밑바닥의 금고", "우물 벽의 선반에 작은 금 주머니가 놓여 있다. 그 아래에는 묵직한 금고.\n\n밧줄을 당길 때마다 방패를 짓누르는 듯한 무게가 느껴진다.", "SunkenTreasury", "작은 안전 보상과 큰 보상에 붙는 영구 저주의 선택.",
            Opt("선반의 주머니만 건진다", "골드 +30.", Fx(EventEffectKind.Gold, 30)),
            Opt("바닥의 금고까지 끌어 올린다", "골드 +150. 저주 유물 획득: 최대 가드게이지 -8.", Fx(EventEffectKind.Gold, 150), new EventEffect { kind = EventEffectKind.Relic, relic = curse }));
        Event("Investment", "배달지는 다음 막", "짐꾼이 목적지를 듣더니 상자에 이름을 쓴다.\n“당장 쓸 약이 필요하신가요, 다음 막의 물건이 필요하신가요?”", "TheLegendsWereTrue", "뒤에 받는 성장과 즉시 자원의 선택.",
            Cost(Opt("도착해서 받는다", "25골드 소모. 다음 막의 배달 사건에서 유물과 골드 수령.", Fx(EventEffectKind.SetFlag, flag: "DeliveryWaiting")), gold: 25),
            Opt("지금 쓸 약을 받는다", "포션 1개. 배달 예약은 하지 않는다.", Fx(EventEffectKind.Potion)));
        Event("Delivery", "상자가 두 개다", "상자에는 당신 이름이 하나씩 적혀 있다. 하나는 지난번의 당신, 다른 하나는 아직 만나지 못한 당신에게 왔다.\n\n짐꾼은 하나만 고르라는 듯 손을 편다.", "DoorsOfLightAndDark", "약속한 유물 성장과 퍽 하나를 버리는 자금 전환.",
            Opt("맡겨 둔 물건을 받는다", "공용 유물 하나 선택, 골드 +45.", Fx(EventEffectKind.SharedRelicChoice), Fx(EventEffectKind.Gold, 45), Fx(EventEffectKind.RemoveFlag, flag: "DeliveryWaiting")),
            Opt("장비 대신 여비를 받는다", "보유 퍽 하나 제거. 골드 +100.", Fx(EventEffectKind.RemovePerk), Fx(EventEffectKind.Gold, 100), Fx(EventEffectKind.RemoveFlag, flag: "DeliveryWaiting")));
        Event("MasterTrial", "문이 이름을 틀리게 부른다", "문에 새겨진 입이 당신 이름을 한 글자 틀리게 부른다.\n\n정정하자 무거운 발소리가 들린다.\n“직접 증명하라.”\n문 옆에는 오래된 가르침이 새겨져 있다.", "BattlewornDummy", "특별한 성장으로 이어지는 강적과 비교적 안전한 빌드 재정비.",
            Opt("정확히 기억하게 해 주지", "심층 수호자와 전투. 승리 후 전용 유물 하나 선택.", Fx(EventEffectKind.Fight, fight: "DeepTrial"), Fx(EventEffectKind.ExclusiveChoice)),
            Opt("새 이름에 맞는 기술을 배운다", "보유 퍽 하나 제거 후 새 퍽 두 번 선택.", Fx(EventEffectKind.RemovePerk), Fx(EventEffectKind.PerkChoice), Fx(EventEffectKind.PerkChoice)),
            Opt("벽의 호흡법을 익힌다", "최대·현재 체력 +4.", Fx(EventEffectKind.Stat, 4, stat: ModifierKind.MaxHp)));
        Event("Shelter", "불침번을 정하자", "불을 피우자 어둠 속의 눈들이 감긴다. 장작은 하룻밤을 버틸 만큼 남아 있다.\n\n푹 잘까. 잠을 줄이고 몸을 단련할까.", "DoorsOfLightAndDark", "현재 회복과 영구 체력 성장, 보유 동료가 여는 조건부 좋은 선택.",
            Opt("불을 지키며 쉰다", "체력 24 회복.", Fx(EventEffectKind.Heal, 24)),
            Opt("밤새 몸을 단련한다", "최대·현재 체력 +5.", Fx(EventEffectKind.Stat, 5, stat: ModifierKind.MaxHp)),
            Need(Opt("너 먼저 자. 다음은 내 차례야", "강아지와 번갈아 망을 본다. 체력 28 회복, 골드 +12.", Fx(EventEffectKind.Heal, 28), Fx(EventEffectKind.Gold, 12)), RelicPower.StrayDog));
    }
    private static void BuildTrainingTrials()
    {
        var source = encounters["Trial"].members[0].monster;
        foreach (var spec in new[] { ("TrainingLight", 75f), ("TrainingMedium", 140f), ("TrainingHeavy", 240f) })
        {
            var monster = Asset<MonsterData>(Root + "/Monsters/" + spec.Item1 + ".asset");
            EditorUtility.CopySerialized(source, monster); monster.name = spec.Item1; monster.displayName = "훈련 인형"; monster.maxHp = spec.Item2; monster.experienceReward = monster.goldReward = 0;
            monster.textKeys.Clear(); monster.textOwnerGuid = ""; EditorUtility.SetDirty(monster);
            E(spec.Item1, "훈련 인형 · " + spec.Item2, RoomEncounterType.Elite, monster);
        }
    }
}
