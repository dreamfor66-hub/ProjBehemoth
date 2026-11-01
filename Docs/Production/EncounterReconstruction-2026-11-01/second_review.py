"""Executed by build_plan after the first review, before serializing final data.

The first draft's unchanged-object reopen policy did not satisfy the user's
one-off event requirement. These are substantive physical endings, not tooltip
excuses. Kept separately so the rejected reasoning remains reviewable.
"""
well=events['Well'];well['displayName']='침하하는 급료고'
well['authoringLore']='급료고의 무너진 배수층 위에 남은 철제 금고. 기울어진 석판과 벌어진 바닥은 클릭 이전 환경에도 보인다. 접근의 하중 때문에 마지막 받침이 움직여 금고가 미끄러진다. 금화를 빼내려면 무너지는 판을 붙들어야 하고, 물러서면 금고를 잃는다. 이름뿐인 저주/매달린 주머니/알 수 없는 경고문은 전부 폐기한다. 어느 선택이든 금고는 아래로 떨어져 같은 대상에 재도전하지 못한다.'
well['pages']=[page('철띠로 둘러친 벽 아래, 낮은 철제 금고가 기울어진 바닥판 끝에 걸려 있다. 금고 아래로는 석판이 무너진 구멍이 벌어져 있다.\n\n발을 옮기자 받침돌 하나가 아래로 떨어진다. 금고가 손가락 한 마디만큼 미끄러진다. 뚜껑이 벌어지고 안의 금화가 부딪힌다.\n\n들어 올리기에는 너무 무겁다. 금화를 꺼내려면 손을 넣은 채 기울어지는 판을 붙들어야 한다.',
    opt('받침판을 버티며 금화를 꺼낸다.','체력 14 지불 · 골드 +110',hp=14,effects=[effect(5,110)],
        result='금고를 받치며 벌어진 틈으로 팔을 밀어 넣었다. 날카로운 석판이 옆구리를 긁었다. 금화를 쓸어 담고 손을 빼자 받침이 무너졌다. 금고가 어둠 속으로 굴러떨어지는 소리가 길게 이어졌다.',
        revisit='금고는 아래로 떨어졌다. 부서진 받침과 열린 배수층만 남아 있다.',remove=True),
    opt('손을 대지 않고 물러선다.',result='안전한 석판까지 물러났다. 금고가 다시 기울더니 아래로 굴러떨어졌다. 금화 부딪히는 소리는 한참 뒤에 멎었다.',
        revisit='금고는 아래로 떨어졌다. 벌어진 바닥 아래에서는 아무것도 닿지 않는다.',remove=True))]

fruit=events['AmberSeed']
fruit['authoringLore']='벽의 틈과 뼈 사이에서 자라는 혈근. 영양 저장 포낭을 내밀었다가 진동을 받으면 벽 깊은 틈으로 수축하는 미궁 생명체. 처음부터 수축과 움직임이 관찰되므로 기다리면 기회가 끝난다는 결론은 갑자기 붙인 규칙이 아니다. 이식/섭취로 저장기관을 소모하거나, 물러서는 사이 포낭이 틈 안으로 사라져 더 이상 손이 닿지 않는다. 화분/재배자/설명서는 없다.'
fruit['pages'][0]['narrative']='굵은 뿌리가 벽의 줄눈을 밀어 벌렸다. 뿌리 사이에는 반쯤 녹은 작은 뼈들이 엉겨 있다. 끝에 매달린 붉은 포낭 하나가 천천히 오므라든다.\n\n가까이 다가서자 뿌리들이 떨리며 벽의 좁은 틈으로 당겨진다. 가시 끝에 손가락이 긁혔다. 핏방울이 닿은 가는 뿌리 하나가 상처에 달라붙더니 갈라진 살을 덮는다.\n\n붉은 포낭은 점점 안으로 끌려간다. 안쪽에는 씨앗처럼 단단한 덩어리가 비친다.'
fruit['pages'][0]['options'][2]=opt('뿌리에서 물러선다.',result='피 묻은 손을 거두었다. 붉은 포낭이 좁은 틈 안으로 끌려들어갔다. 마지막으로 당겨진 뿌리가 석판 아래에 밀착됐다. 칼끝을 넣어 봐도 더는 걸리는 것이 없다.',
    revisit='포낭은 벽 깊은 틈으로 숨었다. 말라붙은 바깥 뿌리만 손에 닿는다.',used=True)

scholar=events['Scholar']
scholar['authoringLore']='폐성 수비대의 봉인 기록고. 표지를 만진 독자를 놓아주지 않는 일회용 주술 교범. 독자가 한 장을 읽으면 잉크가 몸으로 옮겨가며 책이 소진되고, 독서를 거부하려면 손을 감싼 종이를 찢어야 한다. 시작 시 붙잡힌 손과 찢을 수 있는 종이가 명확하다. 읽지 않고 둔 정상 책을 소진 처리하는 모순은 없다.'
scholar['pages']=[page('석조 독서대 위에 책 한 권이 떠 있다. 책등의 글씨를 살피려 표지를 건드리자 종이가 손목을 휘감는다.\n\n손을 당길수록 종이 끝이 살에 파고든다. 책등에 찍힌 글자가 손끝에서 붉어진다.\n\n“한 독자에게 한 장. 잉크를 살리려거든 피를 댈 것.”\n\n펼쳐진 두 장에는 검의 궤적과 빛바랜 영창이 보인다. 종이 틈은 무기 끝으로 찢을 수 있을 만큼 벌어져 있다.',
    deepcopy(scholar['pages'][1]['options'][0]),deepcopy(scholar['pages'][1]['options'][1]),
    opt('종이를 찢고 손을 빼낸다.','보상 없이 교범 파괴',
        result='무기 끝을 벌어진 종이 틈에 넣고 돌렸다. 손목을 감싼 장들이 찢어졌다. 종이에서 새어 나온 잉크가 바닥에 닿기도 전에 말랐다. 빈 표지까지 바스러져 먼지처럼 흩어졌다. 빈 독서대만 남았다.',
        revisit='책은 사라지고 빈 독서대만 남아 있다.',used=True))]
# The installed book painting also contains its stone pedestal. Preserve that
# physical support in every ending instead of making the whole prop disappear.
for o in scholar['pages'][0]['options']:
    o.update(removeWorldObject=False,useWorldObject=True,resultingWorldObject={},resultingObjectName=None,
             revisit='책은 사라지고 빈 독서대만 남아 있다.')
scholar['pages'][0]['options'][0]['resultText']='익숙한 동작을 도해와 맞춰 되짚었다. 마지막 궤적을 따라가자 잉크가 손등으로 스며들었다. 손목을 감싼 책장이 풀리더니 표지와 함께 먼지처럼 흩어졌다. 빈 독서대만 남았다.'
scholar['pages'][0]['options'][1]['resultText']='종이가 핏방울을 빨아들였다. 빛바랜 영창이 선명해지는 동안 손끝이 차갑게 저렸다. 마지막 음절을 되뇌자 종이는 검게 타고 표지까지 먼지처럼 흩어졌다. 배운 영창과 빈 독서대만 남았다.'

contract=events['Contract'];contract['displayName']='지워지지 않는 서약'
contract['authoringLore']='옛 수비대 서약소에서 지원자의 손목을 붙잡아 맹약을 새기던 주술 서약지. 손대는 순간 손목에 감긴 종이와 대가의 문구가 드러난다. 서명하면 맹약이 몸으로 옮겨 소진되며, 거부하려면 실제 벽 횃불에 종이를 태워 끊는다. 거절하고도 정상 계약서가 남는 모순과 공짜 금박 거래는 없다. 서명과 물리적 파기를 선택할 수 있다.'
contract['pages'][0]['narrative']='낮은 서명대의 붉은 종이를 들어 올리자 끝이 손목을 감아 붙는다. 떼어 내려고 잡아당겨도 종이보다 살이 먼저 당겨진다. 벽의 횃불 불꽃이 밀랍 위에서 흔들린다.\n\n종이에 새 글자가 떠오른다.\n\n“몸의 일부를 남기고, 남은 몸에 칼의 힘을 받는다. 맹약 뒤의 상처는 전처럼 아물지 않으리라.”\n\n맨 아래에는 이름을 넣는 빈칸이 있다. 손목을 감싼 바깥쪽 종이는 불꽃 가까이에서 검게 말린다.'
contract['pages'][0]['options'][1]=opt('횃불에 서약지를 태워 끊는다.','보상 없이 서약 파기',
    result='손목의 바깥쪽 종이를 벽의 불꽃에 댔다. 밀랍 냄새와 함께 글자 끝이 타들어 갔다. 불길이 이름 칸에 닿자 종이가 힘을 잃고 풀렸다. 끝까지 태워 버린 재를 바닥에 털었다.',
    revisit='서약지는 태워 버렸다. 손목에도 서명대에도 맹약은 남지 않았다.',remove=True)

# A paid delivery is collection rather than a contrived gamble. Receiving one's
# own goods does not require an artificial choice to leave them behind.
events['Delivery']['pages'][0]['options']=events['Delivery']['pages'][0]['options'][:1]

training['pages'][0]['narrative']='방패와 창을 보관하던 선반 사이에 훈련 인형이 고정되어 있다. 틀 앞의 발판을 밟자 구동축이 돌아가며 검을 쥔 팔이 구속구를 당긴다.\n\n운반틀의 금속판에 글자가 찍혀 있다.\n\n“입문: 끈 해제 / 검정: 철사 절단 / 완전가동: 사슬 해제. 인형 정지 후 해당 단계의 지급함 개방. 긴급정지 시 구동축 절단.”\n\n틀 아래에는 약제함과 도해함, 장비함이 걸쇠에 물려 있다. 옆의 긴급정지 레버에는 붉은 칠이 남아 있다.'
training['pages'][0]['options'][-1]=opt('긴급정지 레버를 당긴다.','보상 없이 훈련 종료',
    result='레버를 끝까지 당겼다. 구동축 안에서 금속이 끊어지는 소리가 났다. 인형의 팔이 늘어졌다. 세 지급함의 걸쇠는 잠긴 채다. 다시 발판을 밟아도 축은 돌아가지 않는다.',
    revisit='긴급정지로 구동축을 끊었다. 인형은 멎었고, 지급함도 열리지 않는다.',used=True)

banner['pages'][0]['options'][-1]=opt('깃발을 포기하고 물러선다.',
    result='짐승이 내 쪽을 노려보는 동안 길을 비켰다. 녀석은 꺾인 깃대를 입으로 물고 고삐를 끌었다. 내가 떠난 쪽과 반대 통로에서 발톱 소리가 멀어졌다.',
    revisit='짐승은 깃발을 끌고 다른 통로로 떠났다.',remove=True)

# Third critique: the second adhesive-paper event repeats Scholar without a new
# coherent one-off premise. Keep the authored prototype, retire it from play.
events['Contract']['excludedFromRandomPool']=True
events['Contract']['theme']=None

# Independent review caught a return-to-StS remainder: a delivery is not an ATM,
# nor does a porter give away somebody else's change. This parcel contains one
# seeded shared relic; the transport payment was already made on the previous act.
delivery=events['Delivery']
delivery['pages'][0]['narrative']='벽의 보관칸에 운반틀 하나가 기댄 채 놓여 있다. 그 안의 꾸러미에는 내가 짐꾼에게 맡긴 표식이 매듭으로 묶여 있다.\n\n끈 아래 접힌 쪽지에서 익숙한 필체가 보인다.\n\n“약속한 장비 한 점입니다. 운임은 받았고, 나는 다음 보관칸으로 갑니다.”\n\n꾸러미는 봉해져 있고, 다른 사람의 표식은 붙어 있지 않다.'
delivery['pages'][0]['options'][0].update(description='공용 유물 1개 획득',effects=[effect(18),effect(9,flag='DeliveryWaiting')],
    resultText='운반틀을 내리고 매듭을 풀었다. 포장 속 장비를 꺼내 몸에 맞춰 걸었다. 마지막으로 내 표식을 떼어 품에 넣었다. 빈 포장만 보관칸에 접어 두었다.')
events['StrayDog']['pages'][1]['options'][0]['description']='고기 1개 소모 · 회수견 합류'
events['StrayDog']['pages'][1]['options'][1]['description']='회복 포션 1개 소모 · 회수견 합류'

# A wary animal can stay wary. No invented departure solely to enforce one use.
banner['authoringLore']='군용 목줄과 끊어진 고삐를 단 가시등 짐승이 같은 문양의 깃발을 지킨다. 주인의 행방은 알 수 없다. 깃발을 얻으려면 실제 같은 짐승과 싸워야 한다. 비켜가면 짐승은 그대로 지키고, 다음에 다시 대치할 수 있다. 기회가 사라지는 이유를 억지로 만들지 않는 생물 조우다.'
banner['pages'][0]['options'][-1]=leave('짐승의 영역을 피해 지나간다.','깃발에서 손을 거뒀다. 짐승은 깃대를 다시 앞발로 누르고, 내가 멀어질 때까지 이빨을 드러냈다.')

# Observed action without a resource/reward is still an action. It must not be
# blanket-classified as a postponement just because its effect list is empty.
cutpurse=events['RecoveryCutpurse']['options'][2]
cutpurse.update(leaveUnresolved=False,removeWorldObject=True,useWorldObject=False,
    resultText='손끝에 마력을 모아 사내의 목 앞에 멈췄다. 사내가 밧줄을 놓고 두 손을 들었다. “다른 사람인 줄 알았습니다.” 그는 등을 보이지 않으려 옆걸음치다가 반대 통로로 사라졌다.',
    revisit='사내는 밧줄을 버리고 다른 통로로 달아났다.')

follow=events['MagicTeacherReturn']
follow['pages'][4]['narrative']='노인이 내 손끝과 교본의 다음 장을 번갈아 본다.\n\n“새 힘을 얹기 전에 지금 아는 주문부터 더 익혀야겠군요. 잘 안 된 동작을 기억해 두세요. 그걸 봐야 어디를 고칠지 압니다.”\n\n그는 교본을 접어 지도판 밑에 끼우고 분필을 꺼낸다. 아직 지우지 못한 길이 남아 있다.'
follow['pages'][4]['options'][0].update(title='주문을 더 익힌 뒤 다시 말 걸기로 한다.',resultText='손을 거뒀다. 노인은 짧게 고개를 끄덕이고 지도에 새 선을 긋기 시작했다.')
advanced=events['MagicTeacherAdvanced']
advanced['pages'][2]['narrative']='노인이 손끝에 모인 힘을 살피더니 교본을 덮는다.\n\n“아직은 그릇이 못 버팁니다. 같은 주문을 더 써 보거나 마력을 늘린 뒤에 봅시다.”\n\n그는 교본을 가방 위에 올려 두고 조사하던 지도판으로 몸을 돌린다.'
advanced['pages'][2]['options'][0].update(title='힘을 더 기른 뒤 다시 찾아오기로 한다.',resultText='교본에서 손을 뗐다. 노인은 가방 곁에 서서 지도의 표식을 고쳐 그렸다.')

# The bottle worker teaches lightning, not a random poison/healing lesson.
events['StormBottle']['pages'][0]['options'][1]['effects'][0]['spellPool']=[
    dict(fileID=11400000,guid='b5628e2176861b947869198ac7b4693b',type=2),
    dict(fileID=11400000,guid='ff9e3bbd8cd3ed04891c8b17b6464ca1',type=2)]

# One medium examination produces one concrete skill refinement, and cannot ask
# for another upgrade after the final authored stack was already consumed.
training['pages'][0]['options'][1]['effects']=training['pages'][0]['options'][1]['effects'][:2]
training['pages'][0]['options'][1]['description']='전투 · 보유 능력 하나 강화'
events['Shelter']['pages'][0]['narrative']+='\n\n용병이 허리의 납작한 약통을 밀어 놓는다. “붕대만 묶어 주면 남은 약은 가져가세요. 혼자 감으려니 자꾸 풀려서.”'
events['Shelter']['pages'][0]['options'][0]['resultText']='붕대를 다시 감고 용병이 일어설 때까지 통로를 지켰다. 그는 약통에 남은 연고를 덜어 내 상처에 발라 주었다. 빈 통을 닫고 검을 지팡이처럼 짚는다. “이번에는 제 발로 나가겠습니다.” 귀환할 쪽을 확인한 그가 천천히 길을 떠났다.'
events['Shelter']['pages'][0]['options'][2]['resultText']='회수견이 통로를 지켜보는 동안 붕대를 다시 묶었다. 용병은 남은 연고로 내 상처를 감싸고 품삯 일부를 내밀었다. 검을 짚어 일어난 그가 개에게 먼저 고개를 숙인다. 우리는 각자의 길로 향했다.'

# The treasury's broken floor is a persistent, directional physical aftermath.
for o in well['pages'][0]['options']:
    o['removeWorldObject']=False;o['useWorldObject']=True
    o['resultingObjectName']='급료고의빈배수구'
