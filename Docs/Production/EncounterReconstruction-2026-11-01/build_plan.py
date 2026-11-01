"""Complete, idempotent event authoring payload. Never writes Unity assets itself."""
from copy import deepcopy
import json
import re
import yaml
from pathlib import Path

ROOT = Path(__file__).parent
before = {e['id']: e for e in json.loads((ROOT/'BeforeEvents.json').read_text(encoding='utf-8'))}
table_path=ROOT.parents[2]/'Assets/Data/Localization/Resources/BehemothKr.asset'
table=yaml.safe_load(re.sub(r'^--- !u!\d+ &\d+','---',table_path.read_text(encoding='utf-8-sig'),flags=re.M))['MonoBehaviour']
texts={x['key']:x['kr'] for x in table['entries']}
for e in before.values():
    for p in e['pages'] or [dict(options=e['options'])]:
        for o in p['options']:o['revisit']=texts.get(o.get('revisitTextKey',''),'')
events = deepcopy(before)

def effect(kind, amount=0, relic=None, stat=4, flag=None):
    return dict(kind=kind, amount=amount, relic=dict(guid=relic) if relic else {}, stat=stat,
                flag=flag, spellPool=[], spellChoiceCount=3)

def opt(title, description='', effects=None, result='', revisit='', gold=0, hp=0, maxhp=0,
        potion=False, next=-1, remove=False, used=False, leave=False, requirements=None):
    return dict(title=title,description=description,effects=effects or [],resultText=result,revisit=revisit,
        goldCost=gold,hpCost=hp,maxHpCost=maxhp,consumeHealingPotion=potion,nextPage=next,failurePage=-1,
        removeWorldObject=remove,useWorldObject=used,leaveUnresolved=leave,requirements=requirements or [],
        supplyCost=0)

def source(id,p,i,**changes):
    pages=before[id]['pages']
    o=deepcopy((pages[p]['options'] if pages else before[id]['options'])[i])
    o.update(changes)
    o.pop('revisitTextKey',None)
    return o

def leave(text='아무것도 건드리지 않고 물러선다.',result='손을 거두고 길을 비켰다. 남겨 둔 것은 그대로다.'):
    return opt(text,result=result,leave=True)

def page(text,*options):return dict(narrative=text,options=list(options),routes=[])

def revise(id,name,lore,pages,theme=None):
    e=events[id];e.update(displayName=name,authoringLore=lore,pages=pages,theme=theme)
    e['narrative']=pages[0]['narrative'];e['options']=deepcopy(pages[0]['options'])
    e['entryRoutes']=[]
    return e

# An actual treasury relic, not a bait purse arranged for an arbitrary small/large payout.
revise('Well','수호자의 급료고',
    '폐성 수비대의 급료 보관고. 원래 금고를 지키던 서약은 수비대가 사라져도 금화에 남아 있다. '
    '손잡이의 금속 경고띠와 금고를 향해 끌린 바닥 자국이 지불해야 할 짐을 미리 드러낸다. '
    '소액 미끼 주머니와 보이지 않는 설치자는 폐기한다. 내용물은 한 번만 꺼낼 수 있다.',[
    page('낮은 철제 금고가 바닥에 박혀 있다. 금고를 중심으로 석판이 길게 긁혀 있고, 손잡이에는 굵은 쇠띠가 감겨 있다.\n\n'
         '쇠띠의 글자는 아직 읽힌다.\n\n“급료를 반출하는 자, 남은 수호자의 짐을 짊어질 것.”\n\n'
         '뚜껑 틈에 금화가 보인다. 가까이 손을 내밀자 팔의 방패가 아래로 묵직하게 당겨진다.',
         opt('서약의 짐을 감수하고 금고를 연다.','골드 +150 · 최대 가드 게이지 −8',
             effects=[effect(5,150),effect(0,relic='82475a42d9f7a0646b91885be0c2caac')],
             result='손잡이를 당기자 쇠띠가 갈라졌다. 금화를 쓸어 담는 동안 팔이 점점 무거워졌다. 금고는 텅 비었지만, 손을 놓고서도 그 무게는 따라왔다.',
             revisit='금화는 모두 가져갔다. 갈라진 서약의 쇠띠만 빈 금고 안에 남아 있다.',used=True),
         leave('쇠띠를 건드리지 않고 물러선다.','팔을 거두자 방패의 무게가 돌아왔다. 금고는 열지 않았다.'))
],theme='폐성급료고')

revise('AmberSeed','돌틈의 핏덩이',
    '석회질 벽의 균열과 뼈 사이에서 자라는 혈근. 지하의 수분과 피에 반응하는 미궁 생명체다. '
    '단 하나의 부풀어 오른 포낭이 영양과 번식핵을 함께 품는다. 포낭을 먹거나 뿌리를 떼어 이식하면 '
    '둘 모두 같은 영양 저장소를 소모하여 그 자리에 재차 채집할 대상이 없다. 재배자/화분/설명서는 없다.',[
    page('굵은 뿌리가 벽의 줄눈을 밀어 벌렸다. 뿌리 사이에는 반쯤 녹은 작은 뼈들이 엉겨 있다. 끝에 매달린 붉은 포낭 하나가 천천히 오므라든다.\n\n'
         '가까운 뿌리를 건드리자 끝의 가시가 손가락을 긁는다. 핏방울에 닿은 가는 뿌리가 꿈틀거리며 상처를 덮는다. 갈라진 살이 잠깐 아물다가, 뿌리를 떼자 다시 따끔거린다.\n\n'
         '포낭 속에서는 씨앗처럼 단단한 덩어리가 만져진다.',
         opt('살아 있는 뿌리를 손목에 붙인다.','체력 10 지불 · 혈근의 씨 획득',hp=10,
             effects=[effect(0,relic='2900393ef75c6e542881f42f9ee141ec')],
             result='가느다란 뿌리가 손목을 파고들었다. 포낭에서 빠져나온 단단한 씨가 뿌리와 함께 살밑에 붙는다. 벽에 남은 뿌리는 수분을 잃고 늘어졌다. 내 맥박 사이에 낯선 고동이 끼었다.',
             revisit='포낭이 쪼그라들었다. 벽에 남은 뿌리 끝은 바싹 말라 있다.',used=True),
         opt('포낭을 잘라 내용물을 삼킨다.','체력 20 회복',effects=[effect(6,20)],
             result='얇은 껍질을 가르자 끈끈한 과육이 흘렀다. 이를 악물고 삼켰다. 따뜻한 것이 상처를 타고 번지는 동안 벽의 뿌리는 검게 오그라들었다. 단단한 핵까지 씹혀 더는 남은 것이 없다.',
             revisit='내용물을 모두 먹었다. 빈 포낭과 말라붙은 뿌리만 남았다.',used=True),
         leave('뿌리에서 손을 떼고 떠난다.','상처를 닦고 뿌리에서 물러났다. 붉은 포낭은 벽에 붙은 채 오므라든다.'))
],theme='혈근번식실')

revise('Scholar','봉인된 교범',
    '폐성 수비대의 봉인 기록고. 독자에게 한 번의 내용을 넘기는 주술 교범을 보관하던 곳이다. '
    '표지의 사용 조건과 살아 움직이는 종이가 물건 자체의 마력을 보여 준다. 읽기를 마치면 글자와 '
    '종이가 소진되고 겉장은 무너진다. 약제 재료 없이 포션을 만들어 내는 선택과 내용 있는 책의 재보상 금지는 폐기한다.',[
    page('기록 묶음이 눌린 서가 사이에서 책 한 권만 떠 있다. 표지를 묶은 가죽끈 아래로 종잇장이 들썩인다.\n\n'
         '책등에는 눌러 찍은 글자가 있다.\n\n“한 독자에게 한 장. 잉크를 살리려거든 피를 댈 것.”\n\n'
         '주변 바닥에는 같은 크기의 빈 책등들이 재처럼 부서져 있다.',
         opt('가죽끈을 풀어 내용을 살핀다.',next=1),leave('책을 묶어 둔 채 떠난다.')),
    page('가죽끈을 늦추자 책장이 손끝을 따라 열린다. 한쪽에는 검의 궤적과 발디딤이, 다른 쪽에는 빛바랜 영창이 빽빽하다.\n\n'
         '글자를 따라 손을 옮기면 다른 장의 잉크가 옅어진다. 끝까지 읽을 수 있는 것은 한 장뿐인 모양이다.',
         opt('몸에 익힌 기술과 검술 도해를 맞춰 본다.','보유 능력 하나 강화',effects=[effect(13)],
             result='익숙한 동작을 도해와 맞춰 되짚었다. 마지막 궤적을 따라가자 잉크가 손등으로 스며들었다. 책장이 바스러지고 표지도 무게를 잃었다. 빈 가죽만 서가 아래로 떨어졌다.',
             revisit='교범은 부서져 사라졌다. 바닥에는 글자 없는 가죽 조각만 남아 있다.',remove=True),
         opt('피를 떨어뜨려 영창을 읽는다.','체력 9 지불 · 주문 하나 습득',hp=9,effects=[effect(4)],
             result='종이가 핏방울을 빨아들였다. 빛바랜 영창이 선명해지는 동안 손끝이 차갑게 저렸다. 마지막 음절을 되뇌자 종이는 검게 타고 표지가 바스러졌다. 배운 영창만 머릿속에 남았다.',
             revisit='교범은 부서져 사라졌다. 바닥에는 타버린 표지 조각만 남아 있다.',remove=True),
         leave('가죽끈을 다시 묶고 떠난다.','읽던 장을 덮고 끈을 묶었다. 책은 처음처럼 가볍게 들썩인다.'))
],theme='봉인기록고')

revise('Contract','마지막 복무 서약',
    '옛 수비대가 목숨을 담보로 힘을 받던 서약소. 방에 새겨진 같은 문장과 희미한 손자국들이 '
    '용도와 대가를 설명한다. 계약을 위해 동전이나 금박을 공짜로 나누는 주체는 없다. '
    '서약에 손대지 않으면 언제든 돌아와 결정할 수 있고, 서명하면 서명지가 손목에 타 들어가 물건은 소진된다.',[
    page('낮은 석제 서명대에 붉은 밀랍의 서약지가 눌려 있다. 벽의 닳은 손자국 아래로 같은 문장이 여러 줄 새겨져 있다.\n\n'
         '“몸의 일부를 남기고, 남은 몸에 칼의 힘을 받는다. 맹약 뒤의 상처는 전처럼 아물지 않으리라.”\n\n'
         '서약지 맨 아래에 이름을 넣는 빈칸이 있다. 종이를 가까이 대자 손목의 맥박에 맞춰 글자가 붉어진다.',
         opt('내 이름을 쓰고 맹약한다.','최대 체력 18 지불 · 모든 피해 +25% · 받는 회복량 −40%',maxhp=18,
             effects=[effect(0,relic='79f116f127bf73f4db9a2a581e0bce84')],
             result='이름을 마치자 붉은 글자가 손목으로 타 들어왔다. 숨을 몰아쉬며 대에서 손을 뗐다. 무기는 가벼워졌지만 상처 가장자리는 차갑게 굳었다. 서약지는 검은 재가 되어 손가락 사이로 흩어졌다.',
             revisit='서약지는 이미 내 손목에 새겨졌다. 석제 대에는 밀랍이 눌린 자리만 남아 있다.',remove=True),
         leave('이름을 쓰지 않고 물러선다.','손을 거두자 글자의 붉은빛이 옅어졌다. 서약지는 빈칸을 남긴 채 대 위에 붙어 있다.'))
],theme='서약소')

delivery=before['Delivery']['pages'][0]['options'][0]
revise('Delivery','예약자의 표식',
    '앞서 선불 운임을 받은 계약 짐꾼이 실제 물품을 놓은 보관칸. 같은 표식과 필체가 인과를 이어 준다. '
    '꾸러미 속 물건은 전부 받을 수 있다. 기술 하나를 잊으면 돈이 나오는 금고/반납할 이유 없는 양자택일은 폐기한다. '
    '보관물을 건드리지 않으면 예약은 유지된다.',[
    page('벽의 보관칸에 운반틀 하나가 기댄 채 놓여 있다. 그 안의 꾸러미에는 내가 짐꾼에게 맡긴 표식이 매듭으로 묶여 있다.\n\n'
         '끈 아래 접힌 쪽지에서 익숙한 필체가 보인다.\n\n“운임은 받았습니다. 약속한 장비와 지난 손님의 잔금까지 넣었습니다. 나는 다음 보관칸으로 갑니다.”\n\n'
         '꾸러미는 봉해져 있고, 다른 사람의 표식은 붙어 있지 않다.',
         opt('표식을 확인하고 꾸러미를 푼다.','공용 유물 선택 · 골드 +45',effects=deepcopy(delivery['effects']),
             result='운반틀을 내리고 매듭을 풀었다. 장비를 골라 몸에 맞춰 걸고 동전을 세었다. 마지막으로 내 표식을 떼어 품에 넣었다. 빈 포장만 보관칸에 접어 두었다.',
             revisit='예약한 물건은 모두 받았다. 보관칸에는 빈 포장과 운반틀만 남아 있다.',used=True),
         leave('봉인을 풀지 않고 길을 계속 간다.','내 표식을 확인하고 다시 내려놓았다. 물건은 보관칸에 그대로 두었다.'))
],theme='물자보관칸')

# The training encounter already has a real room and opponent. Tighten the machine's
# visible operating rules rather than inventing another person to hand out a reward.
training=events['Training'];training['theme']='훈련보관실'
training['authoringLore']='폐성 수비대가 전투 검정과 장비 불출을 함께 하던 자동 훈련고. 구속끈/철사/사슬의 검정 단계가 실제 인형의 강도와 보상 수납함을 구분한다. 표의 문구를 읽기 전에는 기능을 단정하지 않는다. 도전을 시작하면 해당 검정의 일회용 태엽이 부서져 같은 인형을 재가동하지 못한다.'
training['pages'][0]['narrative']='방패와 창을 보관하던 선반 사이에 훈련 인형이 고정되어 있다. 검과 방패를 쥔 팔이 구속구를 당길 때마다 마룻대가 울린다.\n\n운반틀의 금속판에 글자가 찍혀 있다.\n\n“입문: 끈 해제 / 검정: 철사 절단 / 완전가동: 사슬 해제. 인형 정지 후 해당 단계의 지급함 개방. 태엽은 재사용하지 말 것.”\n\n틀 아래에는 작은 약제함, 검술 도해함, 장비함이 각각 걸쇠에 물려 있다.'
for i,o in enumerate(training['pages'][0]['options'][:3]):
    o['title']=['입문용 끈을 푼다.','검정용 철사를 끊는다.','완전가동 사슬을 푼다.'][i]
    o['resultText']=['인형이 멎자 약제함의 걸쇠가 풀렸다. 약병을 챙기는 동안 태엽이 끊어지는 소리가 났다. 인형의 팔은 늘어진 채 움직이지 않는다.',
                     '인형의 빈틈을 끝까지 따라붙었다. 검정을 마치자 도해함이 열렸다. 몸에 남은 움직임을 도해와 맞춰 익혔다. 태엽이 끊어지고 구속구가 바닥에 떨어졌다.',
                     '인형이 마지막 일격 뒤 멈췄다. 장비함의 걸쇠가 풀리고, 부서진 태엽 조각이 운반틀 아래로 쏟아졌다. 남은 장비를 챙겼다.'][i]
    o['useWorldObject']=True;o['removeWorldObject']=False
    o['revisit']='태엽이 끊어진 훈련 인형이다. 지급함도 비었다. 다시 가동할 수 없다.'
training['pages'][0]['options'][-1]=leave('구속구를 건드리지 않고 떠난다.','검정판을 읽고 물러섰다. 인형은 구속구 안에서 느리게 팔을 움직인다.')

# One actual opponent and one guarded relic. The free purse branch was arbitrary.
banner=events['Banner'];banner['displayName']='깃발을 지키는 짐승'
banner['authoringLore']='군용 목줄을 찬 가시등 짐승이 깃대와 끊어진 고삐를 지킨다. 버려진 군용 짐승이라는 추측은 형태로부터 나오며 화자가 주인의 행방을 알지는 못한다. 지키는 깃발을 가지려면 실제 같은 짐승과 싸운다. 짐승을 무시하면 그대로 남고 다시 대치할 수 있다. 편리하게 경계 밖에 놓인 소액 주머니는 폐기한다.'
banner['pages'][0]['narrative']='가시 돋은 짐승이 꺾인 깃대 위에 앞발을 올려놓고 있다. 목줄에는 끊어진 고삐가 매달려 있다. 닳은 군용 표식의 문양과 깃발의 문양이 같다.\n\n깃발에 손을 뻗자 짐승이 일어선다. 고삐가 바닥을 긁는다. 뒤로 물러나자 더 쫓아오지는 않고 깃대 곁을 맴돈다.'
banner['pages'][0]['options']=[source('Banner',0,0,resultText='짐승이 쓰러졌다. 끊어진 고삐를 밟고 깃대를 들어 올렸다. 날이 닳은 군용 표식이 깃발과 부딪혔다.',useWorldObject=True,removeWorldObject=False),leave('짐승의 영역을 피해 지나간다.','깃발에서 손을 거뒀다. 짐승은 내 발소리가 멀어질 때까지 이빨을 드러냈다.')]
banner['pages'][0]['options'][0]['revisit']='깃발은 내가 챙겼다. 끊어진 고삐와 쓰러진 짐승만 남았다.'

# Trade with a real worn broker; remove the arbitrary hand-cutting-for-cash stunt.
tooth=events['Tooth'];tooth['displayName']='금니패를 파는 중개인'
tooth['authoringLore']='생환 채권과 원정대 유품을 중개하는 현장 금융상. 성 바깥보다 수수료가 높은 위험 시장을 찾아 들어왔다. 금니 모양의 부적과 전당금을 실제 물품으로 거래한다. 이유 없이 상처 내는 내기는 폐기하고, 가져온 회복 포션을 현금화할 수 있게 한다. 한 번 거래하면 남은 재고를 닫고 계산에 몰두하므로 재거래가 안 되는 이유가 드러난다.'
tooth['pages']=[page('금니를 한 사내가 장부의 이름 위에 금을 긋는다. 외투 안쪽에는 회수표와 작은 전당품들이 실로 꿰여 있다.\n\n“탑에서 안 내려온 사람들 겁니다. 물건은 여기까지 데려왔으니 값을 받아야죠.”\n\n그가 금니 모양의 작은 패를 내민다.\n\n“동전 냄새를 잘 맡는 부적입니다. 약병도 삽니다. 내려올 사람은 많은데, 돌아갈 약은 늘 모자라거든.”',
    source('Tooth',0,0,title='금니 모양의 부적을 산다.',resultText='대금을 건네고 부적을 받았다. 사내는 거래에 쓴 장부를 덮고 외투의 전당품을 다시 묶었다. “오늘 내놓을 물건은 이겁니다.” 그는 남은 이름을 세기 시작했다.',revisit='중개인은 장부를 품 안에 닫았다. “거래는 끝났습니다.”'),
    opt('회복 포션 한 병을 판다.','회복 포션 1개 소모 · 골드 +35',potion=True,effects=[effect(5,35)],
        result='병마개를 확인한 사내가 동전을 세어 내밀었다. 약병을 외투 깊이 넣고 장부를 닫는다. “값은 치렀습니다. 더 팔 물건은 다음에 보죠.”',revisit='중개인은 병을 품 안에 챙기고 더 흥정하지 않는다.'),
    source('Tooth',0,2,title='장부를 짓누르고 여비를 내놓게 한다.',resultText='장부를 짓누르고 빈손을 내밀었다. 사내가 품의 동전을 밀어 놓는다. 나는 돈을 챙겼다. 금니가 잠깐 드러났다가 사라졌다. “당신 이름은 기억하죠.”',revisit='중개인은 장부를 움켜쥔 채 나를 노려본다. 더 말할 생각이 없다.'),
    leave('거래하지 않고 물러선다.','금니패를 돌려주었다. 사내는 장부로 시선을 내렸다.'))]
tooth['entryRoutes']=[]

# Remove omniscient claims and impossible one-time declines from all remaining NPCs.
for id,e in events.items():
    if id in ['Well','AmberSeed','Scholar','Contract','Delivery','Training','Banner','Tooth']:continue
    ps=e['pages'] or [dict(narrative=e['narrative'],options=e['options'])]
    for p in ps:
        for o in p['options']:
            if o['nextPage']>=0:continue
            has_cost=any(o.get(k,0) for k in ['goldCost','hpCost','maxHpCost','consumeHealingPotion','supplyCost'])
            if not o['effects'] and not has_cost:
                # This is a deliberate postponement, not an interaction that changed the NPC.
                o.update(removeWorldObject=False,useWorldObject=False,resultingWorldObject={},leaveUnresolved=True)
                o['resultText']='더 다가가지 않고 길을 비켰다. 아직 결정을 내리지는 않았다.'
                o['revisit']=''

# A specific initial greeting that only asks about an observable route, not unseen
# knots the player is expected to remember from an unpainted corridor.
mage=events['MagicTeacherFirst']
mage['pages'][0]['narrative']='노인이 지도판의 지워진 선을 분필로 덧그린다. 외투에는 조사용 나침반과 짧은 표식 끈이 매달려 있다. 판 아래에는 회수소의 납 표식이 묶여 있다.\n\n그가 내 발뒤꿈치의 먼지를 보고 고개를 든다.\n\n“그쪽에서 오셨군요. 이 지도에는 막혔다고 적혀 있어서요. 제가 놓친 길이 있나 봅니다.”'
mage['pages'][0]['options'][0]['title']='방금 지나온 방향을 짚어 준다.'
mage['pages'][1]['narrative']='내가 들어온 통로를 짚자 노인이 지도의 선 하나를 고친다.\n\n“바깥 회수소에 넘길 지도입니다. 지난달 지도만 믿고 사람을 보낼 수는 없으니까요.”\n\n그가 지도판 밑에서 닳은 교본을 꺼낸다.\n\n“답례를 하지요. 내려가실 거라면 손끝에 힘을 남기는 법이 도움이 될 겁니다.”'
mage['pages'][1]['options'][1]['resultText']='교본을 내미는 손을 밀어 놓고 지도판을 눌렀다. 노인이 가방의 매입금을 꺼내 놓았다. 돈을 거두자 그가 판을 들어 가슴에 안는다. “다음에는 길을 달리 합시다.” 그는 짐을 챙겨 다른 통로로 떠났다.'
mage['pages'][1]['options'][1]['revisit']='측량사는 이곳을 떠났다.'

lost=events['LostBeliever']
lost['pages'][0]['narrative']='소년이 낡은 회수표를 손바닥으로 문지른다. 표식의 이름과 종이에 베낀 이름이 같다.\n\n“저는 회수소 심부름을 합니다. 이 표에 적힌 사람은… 제 아버지예요. 누가 가져온 건지 물으러 내려왔습니다.”\n\n회수소의 배달 완장이 소년의 손목에서 헐겁게 돈다. 그는 내가 온 통로와 쥐고 있는 표식을 번갈아 본다.\n\n“표식을 달고 다니는 노인을 못 보셨나요?”'
lost['pages'][2]['narrative']='소년이 손바닥을 펴 보인다. 녹슨 회수표 가장자리가 손을 긁었다. 뒷면에는 날카로운 금속 테가 말려 있다.\n\n“이걸 떼서 집에 가져가고 싶어요. 이름만 있으면 되는데.”\n\n맨손으로 테를 펴면 손이 깊이 베일 것 같다. 소년은 쥔 표를 놓지 않는다.'
lost['pages'][0]['options'][3]['resultText']='소년의 손을 제치고 회수표의 금속 테를 끊었다. 안쪽의 작은 표장을 가져가자 소년이 이름 적힌 종이를 움켜쥔다. 나는 더 머물지 않았다. 그가 내 뒤를 피해 통로로 물러났다.'
lost['pages'][0]['options'][1]['resultText']='노인이 있는지는 모르지만 내가 온 방향은 지나갈 수 있다고 알려 주었다. 소년은 손에 남은 약을 덜어 내 상처에 발라 주었다. 표식을 소매 안에 넣고 그 길로 조심스레 떠났다.'
lost['pages'][1]['options'][1]['resultText']=lost['pages'][0]['options'][1]['resultText']
lost['pages'][2]['options'][1]['resultText']=lost['pages'][0]['options'][1]['resultText']

# QuietPrayer was specifically praised: keep its premise, make refusal a real return
# and treatment exhausting the finite herbs visible in the narrative.
quiet=events['QuietPrayer']
quiet['options'][0]['resultText']='성직자는 남은 약초를 갈아 내 상처를 감쌌다. 빈 약초주머니를 접어 무릎 아래 넣는다. “돌아가는 길을 찾으면… 저 바깥에도 아직 아침이 오는지 알려 주세요.” 손에는 더 쥔 것이 없었다.'
quiet['options'][0]['revisit']='성직자는 빈 약초주머니를 만진다. “남은 약은 다 썼어요. 지금은 곁에 잠깐 있어 주셔도 돼요.”'
quiet['options'][1]['resultText']='그녀의 손을 밀어내고 약초를 거두었다. 내 상처에 눌러 바르는 동안 성직자는 빈손을 무릎 아래 감췄다. 더는 내 쪽을 보지 않았다.'
quiet['options'][1]['revisit']='성직자는 내 시선을 피한다. 비어 있는 약초주머니만 무릎에 놓여 있다.'

shelter=events['Shelter']
shelter['pages'][0]['options'][1]['resultText']='검자루를 잡은 손을 밀어 놓고 허리의 품삯을 거뒀다. 용병은 피 묻은 붕대를 누른 채 나를 노려본다. 내가 손을 놓아도 칼에서는 손을 떼지 않았다.'
shelter['pages'][0]['options'][1]['revisit']='용병은 칼을 당겨 쥐고 있다. 더 가까이 오지 말라는 눈이다.'

investment=events['Investment'];investment['displayName']='운반틀의 저당'
investment['pages'][0]['narrative']='운반틀을 멘 짐꾼이 어깨끈을 당겨 조인다. 틀에는 상점의 납품표가 겹쳐 묶여 있고, 철제 버클에는 붉은 저당인이 찍혀 있다.\n\n“아직 두 곳 더 내려가야 합니다. 같은 쪽이면 장비를 맡아 드리죠. 선금은 운임이고, 보관 표식은 손님이 정합니다.”\n\n그가 접은 포장지를 들어 보인다.\n\n“정해 둔 보관칸에만 놓습니다. 길 한복판에 풀어 두면 주인이 생기니까요.”'
investment['pages'][0]['options'][0]['title']='운임을 내고 장비를 예약한다.'
investment['pages'][0]['options'][0]['description']='골드 25 지불 · 다음 막에서 예약 물품 발견 가능'
investment['pages'][0]['options'][0]['resultText']='표식을 정하고 선불 운임을 냈다. 짐꾼이 같은 매듭을 운반틀에 묶는다. “다음 구역 보관칸에서 이 매듭을 찾으세요.” 그는 어깨끈을 고쳐 메고 길을 떠났다.'
investment['pages'][0]['options'][1]['resultText']='운반틀의 약 꾸러미를 움켜쥐고 끈을 끊었다. 짐꾼이 균형을 되찾기 전에 병을 챙겼다. 그는 남은 짐을 끌어안고 다른 통로로 물러났다.'

storm=events['StormBottle']
storm['pages'][0]['narrative']='여인이 두꺼운 장갑으로 병마개를 조인다. 유리 안쪽에서 흰 실금 같은 번개가 모였다 흩어지고, 마개 틈에서 작은 천둥이 난다.\n\n“새 병은 비쌉니다. 성 안에서 채우고, 밖에서 팔아야 병값이 나와요.”\n\n납품표 아래에는 지운 빚의 액수가 줄지어 적혀 있다. 그녀가 밀랍을 다시 누른다.\n\n“봉인된 한 병을 살 수도 있고, 손끝에 남기는 법을 배울 수도 있어요. 제 공방이 생기면 이런 데까지 안 와도 되겠죠.”'
storm['pages'][0]['options'][2]['resultText']='여인의 병 주머니를 낚아채 입구를 쥐었다. 유리가 부딪치자 그녀가 봉인된 병을 품에 숨긴다. 나는 빈 병 밑에 모인 잔돈을 털어 냈다. 그녀는 남은 짐을 챙겨 다른 통로로 사라졌다.'

pilgrim=events['Pilgrim']
pilgrim['pages'][0]['narrative']='노인이 돌자루를 내려놓고 허리를 편다. 자루 안의 돌마다 이름과 날짜가 짧게 새겨져 있다. 작은 유품들은 천에 따로 싸였다.\n\n“몸을 못 빼내면 이름이라도 가져가야지. 밖에서는 이걸 보고 귀환금을 내주거든.”\n\n그가 목의 닳은 회수표를 보여 준다.\n\n“이번엔 짐이 무겁군. 여비를 좀 보태 주겠나. 이 표장은 대신 맡겨도 좋네. 나는 다음 짐에 새로 받으면 돼.”'
pilgrim['pages'][0]['options'][1]['resultText']='노인이 돌자루를 고쳐 잡는 사이 허리의 여비주머니를 거뒀다. 빈 허리를 만진 그가 나를 봤다. “그 사람들 몫은 손대지 말게.” 그는 돌자루만 끌어안고 내 곁을 피해 떠났다.'
pilgrim['pages'][0]['options'][2]['title']='회수견에게 안전한 쪽 냄새를 맡게 한다.'
pilgrim['pages'][0]['options'][2]['resultText']='회수견이 끊긴 고삐와 통로 바닥을 번갈아 맡았다. 노인은 녀석이 택한 쪽의 먼지와 발자국을 살피고 고개를 끄덕인다. 내 상처에 남은 연고를 나눠 바르고 회수표장을 건넸다. 개를 내 곁에 돌려놓고, 그가 자루를 메고 천천히 떠났다.'

# No intermediate physical release that silently resets when the player returns.
dog=events['StrayDog']
dog['pages'][0]['options'][0]['title']='거리를 두고 목줄과 다친 발을 살핀다.'
dog['pages'][1]['narrative']='쇠줄은 부러진 고리에 걸려 있고, 고리 밑의 털에는 피가 굳었다. 오른 앞발을 감싼 붕대도 검게 젖어 있다.\n\n개가 내 손과 통로를 번갈아 본다. 손을 낮추자 으르렁거림이 잦아들지만, 줄을 당기면 다시 이빨을 드러낸다.'
dog['pages'][1]['options'][0]['title']='말린 고기로 주의를 돌리고 쇠줄을 푼다.'
dog['pages'][1]['options'][0]['resultText']='고기를 멀찍이 놓자 개가 줄을 늦췄다. 고리를 풀고 물러섰다. 먹이를 삼킨 개는 코를 바닥에 댄 채 내 뒤를 따라왔다. 앞발을 절면서도 내가 멈추면 먼저 통로를 살폈다.'
dog['pages'][1]['options'][1]['title']='포션으로 상처를 씻고 쇠줄을 푼다.'
dog['pages'][1]['options'][1]['resultText']='목줄의 고리를 잡고 다친 발에 약을 흘렸다. 개는 이를 드러냈지만 물지 않았다. 붕대를 고쳐 묶고 쇠줄을 풀었다. 한동안 멀리 서 있던 개가 내 걸음에 맞춰 상처 난 발을 디뎠다.'

exec((ROOT/'second_review.py').read_text(encoding='utf-8-sig'))

# Previous teaching assets contained genuinely empty captions. Materialize the
# actual spell name from the referenced asset rather than shipping blank buttons.
assets=ROOT.parents[2]/'Assets'
guid_paths={}
for meta in (assets/'Data').rglob('*.meta'):
    found=re.search(r'^guid: (\w+)',meta.read_text(encoding='utf-8-sig'),re.M)
    if found:guid_paths[found.group(1)]=meta.with_suffix('')
for e in events.values():
    for p in e['pages'] or [dict(options=e['options'])]:
        for o in p['options']:
            if o.get('title'):continue
            spell=next((x for x in o['effects'] if x['kind']==17),None)
            if spell:
                path=guid_paths[spell['spell']['guid']]
                data=yaml.safe_load(re.sub(r'^--- !u!\d+ &\d+','---',path.read_text(encoding='utf-8-sig'),flags=re.M))['MonoBehaviour']
                o['title']=data['displayName']+' 교습을 받는다.'
            elif any(x['kind']==4 for x in o['effects']):o['title']='교본에서 배울 주문을 고른다.'
            elif not o['effects']:o['title']='수업을 미루고 길을 계속 간다.'
            else:raise ValueError('Uncaptioned option: '+e['id'])

# Make every return use the same existing subject and genuine unresolved state.
# All data here is authored, not per-ID gameplay behavior.
output=[]
for id,e in events.items():
    if not e['pages']:
        e['pages']=[page(e['narrative'],*e['options'])]
    # A single canonical root option list also supports the draw availability test.
    e['narrative']=e['pages'][0]['narrative']
    for pi,p in enumerate(e['pages']):
        for oi,o in enumerate(p['options']):
            o.setdefault('leaveUnresolved',False)
            o.setdefault('revisit','')
            if o['nextPage']<0 and not o['leaveUnresolved'] and not o.get('revisit'):
                if o.get('removeWorldObject'):o['revisit']='그는 이미 이곳을 떠났다.'
                elif o.get('useWorldObject'):o['revisit']='쓸 수 있는 것은 모두 챙겼다. 남은 것은 비어 있다.'
                else:raise ValueError(f'Unexplained persistent ending: {id} {pi}.{oi}')
    e['options']=deepcopy(e['pages'][0]['options'])
    output.append(e)

def refs(obj):
    if isinstance(obj,list):return [refs(x) for x in obj]
    if not isinstance(obj,dict):return obj
    if not obj:return ''
    if 'fileID' in obj or set(obj)=={'guid'}:
        return obj.get('guid','')
    result={}
    for k,v in obj.items():
        if k in ['textKeys','textOwnerGuid','m_Script','m_Name','m_EditorClassIdentifier'] or k.startswith('m_'):continue
        result[k]=bool(v) if k in ['excludedFromRandomPool','removeWorldObject','useWorldObject','consumeHealingPotion','leaveUnresolved','invert'] else refs(v)
    return result

payload=refs(dict(events=output))
(ROOT/'Plan.json').write_text(json.dumps(payload,ensure_ascii=False,indent=2),encoding='utf-8')
print('Authored',len(output),'events; wrote Plan.json')

