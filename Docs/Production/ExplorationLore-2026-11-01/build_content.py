from pathlib import Path
import json
OUT=Path(__file__).parent
before={e['id']:e for e in json.loads((OUT/'BeforeEvents.json').read_text(encoding='utf-8'))}
people=['LostBeliever','Tooth','QuietPrayer','Investment','StormBottle','Shelter','MagicTeacherFirst','MagicTeacherReturn','MagicTeacherAdvanced','Pilgrim']
lore={
'LostBeliever':'성 밖 회수소에서 이름표를 대조하던 수습 심부름꾼. 아버지의 귀환 계약이 이름 돌 하나로 종결되려 하자 직접 원정 짐을 따라 들어왔다. 일행과 떨어져 기록과 유품을 붙들고 있다. 구하는 것은 부나 모험이 아니라 계약서에 적히지 않은 마지막 행적이다. 슬픔은 이 인물의 고유한 상황이며 전체 던전 주민의 기본 태도가 아니다.',
'Tooth':'귀환한 원정대의 배당을 정산하는 사설 생환 보험업자. 금니는 예전 호위 일의 상흔. 구두 내기는 장부에 실제 배당 채무로 남는다. 성문에서 기다리면 다른 채권자에게 고객을 빼앗기므로 원정객이 지나는 길목까지 들어왔다. 고개를 숙이는 것은 계산 중이기 때문이며, 겁먹고 살 의지를 잃은 인물이 아니다.',
'QuietPrayer':'부상자 후송을 맡은 야전 성직자. 마지막 후송 일행을 먼저 보내고 남았지만 자신의 발목과 믿음이 함께 꺾였다. 남은 약초로 타인을 돌보는 행위만은 포기하지 않았다. 앉아 있는 절망의 장면을 이 사건에 집중한다.',
'Investment':'원정대와 성 밖 상점 사이를 오가는 계약 짐꾼. 선불 운임으로 담보 잡힌 운반틀을 되찾으려 한다. 이미 익힌 실제 길을 되짚어 다음 구역에 물건을 놓는다. 공짜 호의가 아니라 신용과 납품 시간을 지키는 직업적 선택이다.',
'StormBottle':'유리공방의 선금을 받아 들어온 병입 마력 수집가. 돌아오지 않는 공급자를 기다리는 대신 직접 빈 병과 성 안의 잔류 번개를 수거한다. 온전한 병을 회수하면 빚을 갚고 자기 공방을 열 생각이다. 살아 있는 상인답게 병을 들고 봉인을 확인하며 손님과 흥정한다.',
'Shelter':'상단의 고용 표식을 단 젊은 호위 용병. 괴수에게 쫓기다 주인이 연결 밧줄을 끊고 도망쳐 부상을 입었다. 배신을 확인하려고 자신의 발로 나갈 생각이며, 절망해 영주하는 사람이 아니다. 부상으로 무릎을 꿇은 이유가 그림과 행동에 드러난다.',
'MagicTeacherFirst':'해체된 고전학파의 현장 연구자. 다른 원정대가 장비로 팔아 버리기 전에 흩어진 실증 기록을 모으려 들어왔다. 불씨에 정신이 팔린 첫 만남 이후 플레이어의 실제 주문 사용을 다음 판본의 증거로 삼는다. 수업과 시혜만 주는 사람이 아니라 연구에 필요한 제자를 찾는 능동적 목적이 있다.',
'MagicTeacherReturn':'첫 만남의 연구자. 플레이어의 주문 사용 흔적을 자신의 판본과 대조하려 다시 만난다. 책을 펼쳐 선 채로 질문하고 가르친다. 빼앗기거나 위협받으면 이 협력은 끝나고 후속 수업도 없다.',
'MagicTeacherAdvanced':'같은 연구자. 깊은 층에서 관측한 마력을 완성할 실험이 필요하다. 플레이어가 실제 익힌 속성을 따라 고급 주문을 가르친다. 준비가 없으면 수업을 거두며, 어떤 상대에게나 마지막 힘을 퍼 주는 절망한 노인이 아니다.',
'Pilgrim':'귀환 계약에 따라 신원과 유품을 회수하는 늙은 실무자. 살아 돌아오지 못한 이들의 이름이 보상 명부에서 사라지지 않도록 돌과 유품을 가족에게 가져간다. 시신을 빼낼 수 없었던 탓에 이번 짐은 특히 가볍고 마음은 무겁다. 돌아갈 운임과 안전한 길을 구하며, 이름표를 훔쳐 단 가짜 회수꾼과 대비된다.',
'StrayDog':'원정대의 운반견을 따라 들어온 어린 개. 주인은 사라지고 가죽 목줄과 다친 앞발이 남았다. 도움을 주면 발견 그림과 같은 갈색·흰색 무늬/붕대/목걸이를 유지한 동료로 합류한다.',
'Banner':'주인의 급료 주머니와 깃발을 목줄에 매단 시련 짐승. 오랫동안 배운 경계를 반복한다. 전투 상대는 실제 그림과 같은 가시등 시련수다.',
'Training':'폐성 원정에 쓰이던 운반식 훈련 인형. 구속 단계가 실제 전투의 강도를 바꾸고 보상은 수납함에 있다. 주변 운반 장비는 같은 카메라의 막다른 보관 공간을 이룬다.',
'Scholar':'길가에 쌓아 둔 회수 기록에 섞인 주술서. 기록 자체가 독자를 붙잡으므로 사람을 새로 만들어 낼 필요가 없다. 주변은 실제로 보이는 원정 기록 보관더미다.',
}
patches=[]
for id,e in before.items():
    if e['excludedFromRandomPool']:continue
    kind=1 if id in people else 2 if id=='StrayDog' else 3 if id in ['Banner','Training'] else 0
    weight=(.60 if id=='MagicTeacherFirst' else .80 if id in ['MagicTeacherReturn','MagicTeacherAdvanced'] else .35) if kind==1 else e['weight']
    p=dict(id=id,displayName=e['displayName'],kind=kind,weight=weight,lore=lore.get(id,'사람을 새로 등장시키지 않고, 실제 발견한 물건의 성질과 그 선택의 결과로 진행하는 우연한 발견.'),pages=[],options=[])
    patches.append(p)
byid={p['id']:p for p in patches}
def page(id,index,text):byid[id]['pages'].append(dict(index=index,text=text))
def option(id,p,i,title,text,description='',effects=None,remove=False,used=False,revisit='',goldCost=-1,hpCost=-1,add=False,requirements=None):
    d=dict(page=p,index=i,title=title,resultText=text,description=description,remove=remove,used=used,revisit=revisit,goldCost=goldCost,hpCost=hpCost,add=add)
    if effects is not None:d['effects']=effects
    if requirements is not None:d['requirements']=requirements
    byid[id]['options'].append(d)
def effect(kind,amount=0,flag='',asset=''):return dict(kind=kind,amount=amount,flag=flag,asset=asset)

page('StormBottle',0,'여인이 병을 불빛에 비춰 마개의 밀랍을 살핀다. 안쪽에서 작은 천둥이 울린다.\n\n“이 소리면 아직 샙니다. 빈 병은 버리지 마세요. 공방에 가져가면 이것도 제 몫이거든요.”\n\n허리의 납품표에는 갚은 금액이 여러 번 고쳐 적혀 있다. 여인은 내 무기를 보고 병 하나를 들어 보인다.\n\n“검에 묻힐까요, 다루는 법을 익힐까요?”')
option('StormBottle',0,2,'병 주머니를 낚아챈다.','병끼리 부딪치는 소리에 여인이 뒤로 물러났다. 나는 빈 병과 그 안의 잔돈을 챙겼다. 그녀는 봉인된 병을 품에 숨기고 통로를 빠져나갔다.','골드 +20',effects=[effect(5,20)],remove=True)
for i in (0,1):
    old=before['StormBottle']['pages'][0]['options'][i]
    option('StormBottle',0,i,old['title'],'거래를 마친 여인은 납품표에 줄을 하나 더 긋고 병을 다시 묶었다. “다음엔 제 이름으로 만든 병을 가져올게요.” 짐을 챙긴 그녀가 길을 떠났다.',remove=True)

page('Investment',0,'운반틀을 멘 짐꾼이 어깨끈을 당겨 조인다. 끈에는 서로 다른 상점의 납품표가 겹쳐 달려 있다.\n\n“더 내려가시죠? 지금 맡기시면 제가 먼저 물건을 놔두겠습니다. 표식은 본인이 고르세요.”\n\n운반틀 한쪽에는 붉게 지운 저당 표식이 남아 있다. 짐꾼은 내 시선을 알아채고 웃는다.\n\n“이번 운임까지 받으면 제 것이거든요. 늦게 오는 일은 없을 겁니다.”')
option('Investment',0,1,'약 꾸러미를 빼앗는다.','어깨끈을 잡아당겨 약 꾸러미를 낚아챘다. 무거운 운반틀에 발이 묶인 짐꾼은 쫓아오지 못했다. 끊어진 끈을 쥔 채 나를 노려보다 다른 통로로 사라졌다.','회복 포션 획득',remove=True)

page('Pilgrim',0,'노인이 무거운 돌자루를 고쳐 안는다. 돌마다 사람의 이름과 날짜가 짧게 새겨져 있다.\n\n“몸까지 데려올 수 없으면, 이름이라도 있어야 돈을 받지.”\n\n목에 걸린 회수 표식은 오래 닳았고, 자루 안에는 작은 유품들이 천에 싸여 있다.\n\n노인은 돌아갈 운임을 세다 동전을 다시 넣는다.\n\n“이 이름들을 기다리는 사람들이 있어. 지름길을 알면 알려 주겠나.”')
option('Pilgrim',0,1,'허리의 여비를 강제로 거둔다.','노인이 돌자루를 놓치지 않으려는 틈에 허리 주머니를 빼앗았다. “이건 그 사람들 몫이 아닌데.” 그는 빈 허리를 더듬다 돌자루를 고쳐 안고 걸어갔다.','골드 +18',effects=[effect(5,18)],remove=True)

byid['Shelter']['displayName']='끊어진 호위'
page('Shelter',0,'젊은 용병이 무릎을 꿇은 채 허벅지의 붕대를 누르고 있다. 부러진 고용 표식과 잘린 밧줄이 목 아래 흔들린다.\n\n“세 번 당기면 같이 뛰기로 했어요. 분명히 세 번을…”\n\n그는 밧줄 끝을 내려다보다 이를 악문다.\n\n“나가서 물어봐야죠. 왜 칼로 잘랐는지.”\n\n내가 다가가자 손이 검자루를 찾는다. 붕대만 다시 묶으면 몸을 일으킬 수 있을 것 같다.')
option('Shelter',0,0,'붕대를 묶어 주고 잠시 망을 본다.','용병이 붕대를 당겨 매는 동안 곁을 지켰다. 내 숨도 차츰 고르게 돌아왔다. 그는 검을 지팡이 삼아 일어나 돌아갈 방향을 확인했다. “여기서 끝낼 생각은 없습니다.”','체력 +24',remove=True)
option('Shelter',0,1,'칼을 뽑기 전에 품삯부터 빼앗는다.','검자루를 누르고 남은 품삯을 꺼냈다. 용병은 피 묻은 붕대를 놓지 못한 채 나를 노려봤다. 나는 돈을 챙겨 비켜섰다.','골드 +22',effects=[effect(5,22)],revisit='용병은 붕대를 누른 채 검을 쥐고 있다. 더 가까이 가는 것은 그만두자.')
option('Shelter',0,2,'강아지와 함께 망을 봐 준다.','강아지가 귀를 세우고 주위를 살피는 동안 붕대를 새로 묶었다. 용병은 남은 품삯을 내밀고 검을 짚어 일어났다. “이번에는 제 발로 나가겠습니다.”','체력 +28 · 골드 +12',remove=True)

page('Tooth',0,'금니가 번쩍이는 사내가 무릎 위의 장부를 넘긴다. 귀환한 원정대의 이름 옆에는 지급액과 검은 빗금이 번갈아 적혀 있다.\n\n“성문에서 기다리면 다른 장부에 이름이 올라가더라고요. 그래서 제가 찾아오는 겁니다.”\n\n사내가 빈 줄을 두드린다.\n\n“당신 이름도 남기시죠. 살아 돌아오는 쪽에 돈을 걸겠습니다.”')
option('Tooth',0,2,'장부를 눌러 놓고 돈을 요구한다.','칼끝으로 장부를 누르자 사내가 돈을 세어 밀었다. 금니가 잠깐 드러났다. “이름은 기억했습니다.” 나는 동전을 거두고 손을 뗐다.','골드 +20',effects=[effect(5,20)],revisit='사내가 장부를 덮는다. “정산은 끝났습니다.”')

byid['QuietPrayer']['narrative']='성직자가 무릎을 끌어안고 있다. 발목의 붕대는 검게 젖었고, 빈 들것의 어깨끈을 아직 쥐고 있다.\n\n“마지막 둘은 먼저 보냈어요. 여기까지 업고 온 건 헛일이 아니었겠죠.”\n\n손을 펴자 작은 성표가 드러난다.\n\n“제 것은 모르겠지만… 다른 상처는 아직 볼 수 있어요. 약초가 조금 남았거든요.”'
option('QuietPrayer',-1,1,'남은 약초를 강제로 챙긴다.','그녀의 손에서 약초를 빼내 내 상처에 눌렀다. 성직자는 빈 손을 천천히 무릎 아래 감췄다. 더는 내 쪽을 보지 않았다.','체력 12 회복',effects=[effect(6,12)],revisit='성직자는 빈 손을 감춘 채 고개를 숙이고 있다. 약초는 내가 가져갔다.')

page('LostBeliever',0,'소년이 낡은 외투 깃을 보따리에 밀어 넣는다. 옷깃의 이름과 손에 든 돌의 이름이 같다.\n\n“회수소에서는 여기 도장이 찍혔으니 끝났대요. 그런데 이 돌은 누가 가져왔는지도 몰라요.”\n\n심부름꾼의 납품끈이 소년의 손목에 남아 있다. 따라 들어온 일행을 놓친 모양이다.\n\n“그 아저씨한테 딱 한 가지만 물어보고 싶어요.”')
page('LostBeliever',2,'소년이 기록을 보여 주려다 움찔한다. 등불의 부러진 쇠고리가 보따리를 쥔 손 안쪽에 파고들어 있다.\n\n상처가 벌어질까 두려워 손을 펴지 못한 모양이다. 고리를 꺾어 주면 손을 뺄 수 있겠지만, 날카로운 끝은 내가 받아야 한다.')
option('LostBeliever',2,0,'부러진 고리를 맨손으로 꺾는다.','날카로운 쇠끝에 내 손이 찢어졌다. 소년은 손을 빼내고 등불을 내게 건넸다. 나는 순례자가 쓰는 표식을 종이에 그려 주었다. 보따리를 다시 묶은 소년이 인사를 하고 돌아갔다.','체력 18 지불 · 등불 유물 획득',remove=True)
option('LostBeliever',0,-1,'보따리를 붙든 손을 떼어 낸다.','소년의 손을 떼어 내고 작은 등불을 꺼냈다. 소년은 외투 깃만 끌어안은 채 물러섰다. 나는 빈 보따리를 돌려주고 길을 비켰다.','등불 유물 획득',effects=before['LostBeliever']['pages'][1]['options'][0]['effects'],remove=True,add=True)

page('MagicTeacherFirst',0,'노인이 선 채로 두루마리를 들여다본다. 모서리에 옮겨붙은 불씨도 알아채지 못한 채 글줄을 손으로 따라간다.\n\n“여기서 하나만 더 찾으면 판본이 맞는데…”\n\n가방 밖으로 내민 종이에는 팔아 넘긴 기록들의 인수 도장이 빼곡하다. 불씨가 손가락 쪽으로 번진다.')
page('MagicTeacherFirst',1,'불씨를 털어 주자 노인이 급히 두루마리를 접는다.\n\n“재보다 먼저 회수해야 하는데, 제가 먼저 태울 뻔했군요.”\n\n그는 내 손을 잠깐 살핀다.\n\n“마법을 쓰신다면 기록 하나만 남겨 주세요. 실제로 통했던 주문이 필요합니다. 대신 손에 맞게 가르쳐 드리지요.”')
for i in (2,3):page('MagicTeacherFirst',i,'노인이 가방에서 책을 꺼내 선 채로 펼친다.\n\n“불, 얼음, 번개. 종이에 적힌 것과 몸이 기억하는 것은 다릅니다. 써 보고 돌아오면 다음 장을 보죠.”')
option('MagicTeacherFirst',1,1,'책 한 권을 여비로 내놓으라고 한다.','가방 끈을 움켜쥐고 책을 빼냈다. 노인은 남은 두루마리를 품에 넣었다. “제 기록에는 당신 이름을 남기지 않겠습니다.” 그는 돌아서 걸어갔다.','골드 +25 · 이후 이 연구자의 수업을 받지 못함',effects=[effect(5,25),effect(8,flag='RobbedFieldScholar')],remove=True)

for id in ['MagicTeacherReturn','MagicTeacherAdvanced']:
    byid[id]['forbiddenFlag']='RobbedFieldScholar'
    for i,pg in enumerate(before[id]['pages']):
        t=pg['narrative'].replace('길가에 앉은','길가에 선').replace('무릎 위에','팔 위에').replace('자리에 앉아','책을 펼쳐').replace('자리에서 일어나','짐을 챙겨')
        if i==0:t=t.replace('큰 책을 안고 길가에 선 노인이 나를 알아본다.','큰 책을 펼쳐 든 노인이 내 손끝의 흔적을 알아본다.\n\n“찾았습니다. 제가 적은 것과 어디가 달랐는지 말해 주세요.”')
        page(id,i,t)
    entry_pages={r['page'] for r in before[id]['entryRoutes']}|{0}
    for i in sorted(entry_pages):
        option(id,i,-1,'펼친 기록을 낚아챈다.' if id=='MagicTeacherReturn' else '지팡이의 수정을 강제로 떼어 낸다.','노인이 남은 기록을 가방에 넣고 손을 거뒀다. 챙긴 물건은 내다 팔 수 있겠지만, 이 사람에게 다음 수업을 청할 수는 없을 것이다. 그는 아무 말 없이 다른 통로로 사라졌다.','골드 +35 · 이후 이 연구자의 수업을 받지 못함',effects=[effect(5,35),effect(8,flag='RobbedFieldScholar')],remove=True,add=True)

page('Scholar',0,'쌓아 둔 기록 사이에서 책 한 권이 바닥 위로 떠 있다.\n\n다른 책들은 끈에 묶인 채 잠잠한데, 이 책의 닫힌 표지만 들썩인다. 안쪽에서는 페이지 넘어가는 소리가 난다.')
page('Training',0,'낡은 운반 장비 사이에 훈련 인형 하나가 묶여 있다. 회색 몸체의 주황색 띠는 여러 번 새로 감은 흔적이 있다.\n\n원정 보급품에 쓰는 수납함이 운반틀 아래 붙어 있다.\n\n“구속을 많이 풀수록 강하게 움직임. 도전 보상은 내부 수납함에 보관.”\n\n머리가 움직인다. 검과 둥근 방패가 끈을 당기며 덜컥거린다.')

byid['Training']['layout']=1;byid['Scholar']['layout']=1
bandit=dict(id='RecoveryCutpurse',displayName='이름 없는 회수표',kind=3,weight=1.2,lore='희생자의 귀환금을 가로채기 위해 회수업자로 위장한 단독 도적. 빈 이름표와 새 밧줄을 들고 원정객을 물색한다. 정당한 회수업자의 닳은 표식과 대비된다. 첫 막의 사람형 적대 조우이며 대화 NPC의 증가로 계산하지 않는다.',pages=[],options=[],new=True,art='회수꾼도적',minAct=1,maxAct=1)
patches.append(bandit);byid[bandit['id']]=bandit
bandit['narrative']='후드를 쓴 사내가 밧줄을 손바닥에 감는다. 목에 걸린 회수표에는 이름이 하나도 없다.\n\n“돌아가실 때 짐이 무거우면 제가 들어 드리죠.”\n\n말과 달리 사내의 시선은 내 주머니에 머문다. 장갑 아래에서 짧은 칼이 드러난다.\n\n“지금 가볍게 해 드릴 수도 있고.”'
option('RecoveryCutpurse',-1,-1,'칼을 뽑은 손부터 벤다.','사내가 쓰러졌다. 이름표 안쪽에는 살아 있는 원정객의 짐값이 적혀 있었다. 주머니의 돈을 거두고 밧줄을 내려놓았다.','전투 · 승리 시 골드 +20',effects=[effect(10,asset='Assets/Data/Dungeon/Encounters/Data_Encounter_회수꾼도적.asset'),effect(5,20)],used=True,revisit='밧줄을 움켜쥔 도적의 시신이다. 회수할 이름은 남겨 놓지 않았다.',add=True)
option('RecoveryCutpurse',-1,-1,'동전을 던지고 거리를 벌린다.','사내가 동전을 줍는 동안 검을 겨눈 채 물러섰다. 그는 빈 이름표를 옷 안에 감추고 다른 길로 빠졌다.','골드 15 지불',effects=[],goldCost=15,remove=True,add=True)
option('RecoveryCutpurse',-1,-1,'손끝에 마력을 모아 목줄을 겨눈다.','사내가 손끝의 마력을 보고 밧줄을 놓았다. “다른 사람인 줄 알았습니다.” 등을 보이지 않으려 옆걸음치던 그가 통로 너머로 사라졌다.','실제로 주문을 사용한 경험 필요',effects=[],requirements=[dict(kind=1,amount=1)],remove=True,add=True)

(OUT/'ContentPlan.json').write_text(json.dumps(dict(events=patches),ensure_ascii=False,indent=2),encoding='utf-8')
print('Authored',len(patches),'active events, including one hostile recovery-cutpurse encounter.')
