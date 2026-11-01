"""Authoring payload only; no gameplay branches. Existing effects/costs/routes are preserved."""
import json
from pathlib import Path
root=Path(__file__).parent
before={e['id']:e for e in json.loads((root/'BeforeEvents.json').read_text(encoding='utf-8'))}
events=[]
def event(id, name=None, lore=None, art=None, clone=None):
    e=dict(id=id,name=name,lore=lore,art=art,clone=clone,pages=[],options=[])
    events.append(e);return e
def page(e,i,text):e['pages'].append(dict(index=i,text=text))
def option(e,p,i,title=None,result=None,revisit=None,used=False,remove=False,description=None):
    e['options'].append(dict(page=p,index=i,title=title,result=result,revisit=revisit,used=used,remove=remove,description=description))

trap=event('SalvageTrap','끊어진 회수선',
    '회수대가 운반로의 뒤를 막으려고 설치한 반복 작동식 쇠덫. 끊어진 회수선과 정비용 주머니가 남았다. 설치자는 보이지 않지만 누가 왜 이 물건을 가져왔는지 형태로 짐작할 수 있다. 장소 자체의 환상/차원이동이 필요하지 않다. 선택의 원형은 기존 Mirror의 기술 강화/체력 대가의 유물/소액 골드이며 효과와 비용은 보존한다.',
    '회수대의쇠덫',clone='Mirror')
page(trap,0,'펼쳐진 쇠덫에서 가는 태엽 소리가 난다. 끊어진 쇠줄 끝에는 회수대의 납 표식이 매달려 있다.\n\n턱 사이에 찢긴 장갑 한 조각이 끼어 있다. 장치 옆의 정비 주머니는 아직 묶여 있다.\n\n이 길을 지키던 사람은 보이지 않는다.')
page(trap,1,'무기 끝으로 턱을 눌렀다. 쇠날이 맞물리고, 잠시 뒤 다시 벌어진다.\n\n힘을 모으는 쪽과 풀리는 쪽이 번갈아 바뀐다. 걸쇠가 비는 짧은 틈이 있다. 움직임을 익힐 수도, 태엽을 빼낼 수도 있겠다.')
page(trap,2,'칼끝을 넣자 태엽 상자가 조금 들린다. 하지만 걸쇠를 잡을 손이 하나 더 필요하다.\n\n손을 넣으면 날을 피할 수는 없을 것이다. 장갑에 남은 찢김이 얼마나 깊은지 이제 알 것 같다.')
page(trap,3,'정비 주머니의 입구가 보인다. 작은 동전들이 쇠줄 고리 밑에 깔려 있다.\n\n쇠줄을 무기 끝으로 당기면 덫은 닫히겠지만, 주머니는 꺼낼 수 있겠다.')
option(trap,0,0,'무기 끝으로 걸쇠를 눌러 본다.')
option(trap,0,1,'옆의 정비 주머니를 살핀다.')
option(trap,1,0,'힘이 풀리는 간격을 몸에 익힌다.','쇠턱의 움직임에 맞춰 발을 옮겼다. 힘을 모으는 동안 생기는 빈틈이 눈에 들어온다. 마지막으로 무기 끝을 넣어 걸쇠를 부러뜨렸다. 쇠턱은 벌어진 채 멈췄다.','태엽이 풀려 있다. 벌어진 쇠턱은 더 움직이지 않는다.',used=True)
option(trap,1,1,'태엽 상자를 빼낼 준비를 한다.')
option(trap,2,0,'날을 감수하고 태엽을 빼낸다.','쇠날이 손을 물었다. 이를 악물고 태엽 상자를 뽑아냈다. 날을 움직이던 힘은 작고 무거운 상자 속에 남아 있다. 빈 덫은 벌어진 채 멈췄다.','태엽 상자는 내가 가져갔다. 빈 쇠덫만 바닥에 남았다.',used=True,description='체력 16 지불 · 반격 태엽 획득')
option(trap,3,0,'쇠줄을 당기고 주머니를 꺼낸다.','무기 끝으로 태엽 상자를 열고 걸쇠를 부러뜨렸다. 힘이 빠진 쇠턱을 확인한 뒤 주머니를 꺼내 동전을 셌다. 빈 주머니를 덫 옆에 내려놓았다.','빈 정비 주머니와 태엽이 풀린 쇠덫이다.',used=True)
for p,pg in enumerate(before['Mirror']['pages']):
    for i,o in enumerate(pg['options']):
        if o['nextPage']<0 and not any(x['page']==p and x['index']==i for x in trap['options']):
            option(trap,p,i,'건드리지 않고 물러선다.','쇠줄을 밟지 않도록 비켜 지나갔다. 등 뒤에서 태엽 소리가 이어진다.','아직 작동하는 쇠덫이다. 이쪽으로 지날 때는 쇠줄을 피하자.')

mage=event('MagicTeacherFirst','귀환로의 측량사',
    '회수소에 새 귀환 지도를 납품하는 현장 마법사. 옛 원정대의 길 표시를 지도로 옮기되 실제 통행 여부를 확인하며 전진한다. 만나는 원정객에게 지나온 표식을 확인하고, 답례와 현장 기록 교환으로 주문을 가르친다. 각 막의 다음 교습도 조사 여정 도중 다시 만나는 동일 인물이다. 무능한 불장난/공간 갑작스런 등장/후방 습격 전제는 없다.')
page(mage,0,'노인이 지도판 위의 두 갈래 선을 번갈아 짚는다. 가방에는 길 표시용 끈이 묶여 있고, 장갑의 손끝은 분필 가루로 희다.\n\n그가 나침반을 닫고 내 쪽을 본다.\n\n“지금 오신 길에 세 줄로 묶은 끈이 있던가요? 옛 지도에는 양쪽 다 귀환로라고 되어 있어서.”')
page(mage,1,'지나온 쪽을 짚어 주자 노인이 지도 한 귀퉁이를 접는다.\n\n“그쪽은 지워야겠군요. 이 지도를 믿고 내려올 사람이 또 있을 테니.”\n\n그는 분필을 넣고 내 손을 살핀다.\n\n“답례는 해야지요. 손끝에 힘을 모아 본 적 있습니까? 내려가실 거면 쓸 데가 있을 겁니다.”')
for p in (2,3):page(mage,p,'노인이 지도판을 가방에 넣고 얇은 교본을 펼친다.\n\n“첫 주문은 손에 맞는 걸로 고르세요. 종이에 적힌 대로 안 되는 곳도 있으니, 다음에 만나면 어디서 막혔는지 듣겠습니다.”')
page(mage,4,'노인이 장갑 한쪽을 벗고 손가락을 천천히 편다.\n\n“불씨를 내는 게 먼저가 아닙니다. 손끝 한곳에 힘을 남겨 두세요. 숨이 끊어져도 흩어지지 않게.”')
option(mage,0,0,'지나온 길의 표식을 짚어 준다.')
for p,pg in enumerate(before['MagicTeacherFirst']['pages']):
    for i,o in enumerate(pg['options']):
        if o['nextPage']>=0:continue
        if any(x['kind']==5 for x in o['effects']):
            option(mage,p,i,'답례 대신 지도 매입금을 내놓으라고 한다.','가방 끈을 붙들고 지도 매입금을 받아 냈다. 노인은 지도판을 겨드랑이에 끼웠다. “이 길에서 다시 만나도 모르는 사이로 합시다.” 그는 다른 통로로 걸어갔다.','측량사는 이곳을 떠났다.',remove=True)
        elif o['effects']:
            option(mage,p,i,result='몇 번을 되풀이한 뒤에야 손끝의 힘이 흩어지지 않았다. 노인이 내 손의 흔적을 교본 여백에 남겼다. “다음 길목에서 확인해 봅시다.” 그는 지도판을 챙겨 조사하던 쪽으로 떠났다.',revisit='측량사는 조사를 계속하러 떠났다.',remove=True)
        else:option(mage,p,i,'길을 계속 간다.','노인이 나침반을 접고 지도를 가방에 넣는다. 서로 길을 비켜 다른 통로로 향했다.','측량사는 이미 다른 통로로 떠났다.',remove=True)

for id in ['MagicTeacherReturn','MagicTeacherAdvanced']:
    e=event(id,lore=mage['lore'])
    for i,p in enumerate(before[id]['pages']):
        if i==0:
            page(e,i,'노인이 지도에 새 선을 긋다가 나를 알아본다. 분필이 짧아져 있다.\n\n“또 만났군요. 가르쳐 드린 주문은 어느 길에서 가장 잘 버텼습니까?”\n\n그는 지도판을 접고 교본을 편다. 내 손에 남은 흔적과 책의 주석을 비교한다.' if id=='MagicTeacherReturn' else '분필 자국이 묻은 지팡이가 길을 두드린다. 노인이 새 표식 끈을 가방에 넣으며 나를 알아본다.\n\n“여기까지 왔으면 그 주문은 손에 붙었겠군요.”\n\n그가 교본 뒷장을 펼친다. “이제 더 큰 힘을 어디에 남겨 둘지 정해 봅시다.”')
        elif id=='MagicTeacherReturn' and i>=5:
            last=p['narrative'].replace('\r','').split('\n\n')[-1]
            page(e,i,'노인이 교본의 접힌 귀퉁이를 편다. 지난번 주문 옆에 내가 지나온 길의 표식이 그려져 있다.\n\n'+last)
        elif id=='MagicTeacherReturn' and i==4:
            page(e,i,'노인이 지도에 내 귀환 표식을 덧그린다.\n\n“그때 손에 남긴 힘은 아직 익숙하지 않은 모양이군요. 억지로 더 얹으면 흩어집니다.”\n\n그는 교본을 덮고 새로 막힌 길을 알려 준다. 서로의 귀환로를 확인한 뒤 헤어졌다.')
    for p,pg in enumerate(before[id]['pages']):
        for i,o in enumerate(pg['options']):
            if o['nextPage']>=0:continue
            if any(x.get('flag')=='RobbedFieldScholar' for x in o['effects']):continue
            if o['removeWorldObject']:
                option(e,p,i,result='노인이 교본을 가방에 넣고 지도판을 고쳐 쥔다. “써 보고 다음 길목에서 확인합시다.” 서로 지나온 표식을 확인하고 각자의 통로로 향했다.',revisit='측량사는 새 귀환 표식을 확인하러 떠났다.',remove=True)

dog=event('StrayDog','회수표를 단 개',
    '회수대가 무너진 통로와 시신의 냄새를 찾는 데 쓰던 다 자란 추적견. 조련사의 회수표가 목줄에 남았다. 돌아오지 않은 조련사를 기다리다 끊어진 쇠줄에 걸렸고 오른 앞발을 다쳤다. 순종적인 애완동물이 아니라 먹이를 받아도 먼저 퇴로를 확인하는 작업견. 구조 후 함께 경계하며 싸우는 이유는 남은 작업 습성과 새 길잡이를 택한 판단이다.')
page(dog,0,'큰 회색 개가 앞발을 뻗고 누워 있다. 목줄의 짧은 쇠줄이 부러진 고리에 걸려 있다. 몸을 일으키려 할 때마다 오른 앞발이 꺾인다.\n\n낡은 가죽 목줄에는 회수대의 표식이 달려 있다. 개는 내 손을 보며 낮게 으르렁거린다. 도망갈 길부터 재는 눈이다.')
page(dog,1,'걸린 고리를 풀어 주었다. 개는 곧장 달아나지 않고 두 걸음 물러서서 나를 살핀다. 오른 앞발의 붕대가 검게 젖어 있다.\n\n쇠줄 끝에 남은 사람 냄새를 다시 맡고, 내가 온 통로로 코를 돌린다.')
option(dog,0,0,'무기를 내려놓고 걸린 쇠줄을 푼다.')
option(dog,0,1,'쇠줄을 건드리지 않고 떠난다.','개가 다친 앞발을 몸 아래 숨긴다. 거리를 두고 지나가자 낮은 울음이 잦아든다.','개는 걸린 쇠줄을 당기지 않으려 몸을 낮추고 있다.')
option(dog,1,0,'말린 고기를 놓고 한 걸음 물러선다.','개는 내 손보다 뒤쪽 통로를 먼저 확인했다. 고기를 삼키고 나서도 한동안 따라오지 않았다. 내가 모퉁이를 돌 때, 무거운 발소리가 곁에 붙었다.','회수견이 내 곁에서 경계하고 있다.',remove=True)
option(dog,1,1,'앞발의 붕대를 새로 묶는다.','목줄의 고리를 쥐고 천천히 붕대를 감았다. 개는 이를 드러냈지만 물지 않았다. 손을 놓자 세 발로 몸을 일으켰다. 잠시 뒤 내 걸음에 맞춰 상처 난 발을 디뎠다.','회수견이 내 곁에서 경계하고 있다.',remove=True)
option(dog,1,2,'더 다가가지 않고 떠난다.','개는 풀린 쇠줄을 끌고 가장자리로 물러섰다. 나를 보내고서야 앞발의 붕대를 핥기 시작했다.','회수견은 경계를 풀지 않고 상처를 핥고 있다.')

(root/'ContentPlan.json').write_text(json.dumps(dict(events=events),ensure_ascii=False,indent=2),encoding='utf-8')
