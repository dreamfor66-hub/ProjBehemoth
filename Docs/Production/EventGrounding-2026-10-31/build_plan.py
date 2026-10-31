from pathlib import Path
import json
OUT=Path(__file__).parent
before=json.loads((OUT/'BeforeEvents.json').read_text(encoding='utf-8'))
events={e['id']:e for e in before}
patches=[]
def change(id, art, pages=None, title=None, npc=False, automatic=False):
    source=events[id]
    p={'id':id,'path':source['path'],'art':art,'npc':npc,'automatic':automatic,'displayName':title or source['displayName'],'pages':[],'fields':[],'terminals':[]}
    if pages:
        assert len(pages)==len(source['pages']),(id,len(pages),len(source['pages']))
        p['pages']=[{'index':i,'narrative':t} for i,t in enumerate(pages)]
    patches.append(p)
    return p
def field(p,path,value): p['fields'].append({'path':path,'value':value})
def ending(p,page,index,text,remove=False,used=False,revisit=None):
    p['terminals']=[t for t in p['terminals'] if (t['page'],t['index'])!=(page,index)]
    p['terminals'].append({'page':page,'index':index,'text':text,'remove':remove,'used':used,'revisit':revisit or '한 차례 살펴본 물건이다. 더 얻을 것은 없다.'})

p=change('Mirror','거울',[
'통로 가장자리에 커다란 거울이 기대어 있다.\n\n나는 멈춰 섰는데 거울 속의 나는 아직 움직이고 있다.',
'거울 속의 내가 무기를 천천히 휘두른다.\n\n내가 자주 쓰는 동작이다. 하지만 발을 딛는 위치가 조금 다르다.',
'차가운 날이 손을 스친다. 더 당기면 유리 한 조각과 함께 떨어질 것 같다.',
'거울을 돌려 보니 뒷면의 은이 두껍게 벗겨져 있다.'])
ending(p,1,0,'몇 번을 따라 하자 자세의 잘못된 부분을 알 것 같다. 거울 속의 내가 무기를 내렸다.',revisit='이제 거울 속의 나는 나와 같은 속도로 움직인다.')
ending(p,2,0,'팔에 긴 상처가 생겼다. 조각을 떼어 내자 나머지 거울과 받침대가 가루로 흩어졌다. 손에 남은 조각 안에서는 아직 내 무기가 움직이고 있다.',remove=True)
ending(p,3,0,'벗겨진 은을 챙겼다. 거울에는 더 이상 낯선 움직임이 비치지 않는다.',revisit='은이 벗겨진 거울이다. 흐릿한 내 모습만 비친다.')

p=change('LostBeliever','유품을찾는소년',[
'작은 보따리를 안은 소년이 길가에 앉아 있다.\n\n손에 쥔 돌에는 사람의 이름이 새겨져 있다.\n\n“아버지 이름이에요. 이걸 가져온 순례자를 만나야 하는데, 혼자서는 더 갈 수가 없어요.”',
'소년이 내가 지닌 표식을 알아보고 일어선다.\n\n“그 표식, 이름이 새겨진 돌을 들고 다니는 분께 받으셨나요?”',
'소년이 펼친 종이에 순례자의 이동 경로가 적혀 있다.\n\n그곳까지 데려다줄 수는 있겠다. 다만 날카로운 잔해를 지나는 동안 소년 대신 내가 앞을 막아야 한다.'],npc=True)
field(p,'pages.Array.data[0].options.Array.data[0].title','소년의 기록을 살펴본다.')
for i,page in enumerate(events['LostBeliever']['pages']):
    for j,o in enumerate(page['options']):
        if o['nextPage']<0: ending(p,i,j,'소년은 내가 알려 준 길을 종이에 옮겨 적었다. 보따리를 안고 걸음을 재촉하는 모습을 지켜봤다.',remove=True)
ending(p,2,0,'소년을 앞세워 잔해를 지났다. 내 팔에는 상처가 남았지만 소년은 무사하다. 소년이 건넨 등불을 들고 서로 다른 길로 향했다.',remove=True)

p=change('Tooth','금니의도박꾼',[
'금니가 번쩍이는 사내가 길가에서 무릎 위의 장부를 넘기고 있다.\n\n“아직 살아 계셨군요. 이번에는 살아 돌아오시는 쪽에 걸겠습니다.”\n\n사내는 이름을 적고, 돈을 보태면 승리할 때마다 배당을 주겠다고 한다.',
'사내가 짧은 칼을 꺼내 손잡이를 내민다.\n\n“제가 날을 잡겠습니다. 당신이 잡아당겨도 먼저 손을 놓지 않으면 됩니다. 해 보시겠어요?”'],npc=True)
# Preserve the authored HP cost: the player, not the gambler, holds the blade.
field(p,'pages.Array.data[1].narrative','사내가 짧은 칼의 손잡이를 단단히 쥔다.\n\n“당신이 날을 잡는 겁니다. 제가 당겨도 손을 놓지 않으면 이 돈을 드리죠.”')
ending(p,0,0,'사내가 장부에 표시를 남겼다. 앞으로의 승리에 내 몫도 걸렸다.',revisit='사내가 장부를 가리킨다. “새 내기는 없습니다. 살아 돌아오시기나 하세요.”')
ending(p,0,2,'흩어진 영수증을 정리해 주고 품삯을 받았다.',revisit='장부는 정리됐다. 사내는 계산을 이어 가고 있다.')
ending(p,1,0,'피가 흘렀지만 손을 놓지 않았다. 사내가 먼저 그만하라며 돈을 꺼냈다.',revisit='사내가 빈 돈주머니를 흔든다. “오늘 내기는 여기까지입니다.”')

p=change('QuietPrayer','성직자',npc=True)
for i,o in enumerate(events['QuietPrayer']['options']):
    ending(p,-1,i,o.get('resultText') or '작은 기도가 끝났다. 성직자는 다시 무릎을 끌어안았다.',revisit='성직자는 고개를 숙인 채 조용히 기도하고 있다. 더 부탁할 힘은 남지 않은 듯하다.')

p=change('Scholar','떠있는책',[
'길가에 떨어진 책 한 권이 바닥에서 조금 떠 있다.\n\n젖은 바닥에 닿지 않은 채 닫힌 표지가 들썩인다. 안쪽에서는 페이지 넘어가는 소리가 난다.',
'책을 펼치자 페이지가 손가락을 감싼다.\n\n“한 줄은 읽고 가.”\n\n기술, 영창, 약제법이 적힌 장이 차례로 펼쳐진다.',
'영창 부분은 잉크가 흐려 읽을 수 없다.\n\n책 귀퉁이에는 피를 대면 감춰진 글자를 읽을 수 있다고 적혀 있다.'])
ending(p,0,1,'책을 건드리지 않고 지나쳤다. 뒤에서도 페이지 넘어가는 소리가 이어진다.',revisit='책은 여전히 바닥 위에 떠 있다. 오늘은 더 읽지 않기로 했다.')
for i,j in [(1,0),(1,2),(2,0)]: ending(p,i,j,'마지막 줄을 읽자 책이 저절로 덮였다. 배운 내용을 되짚으며 손을 떼자 책은 다시 바닥 위로 떠올랐다.',used=True,revisit='다 읽은 책이 표지를 닫은 채 떠 있다. 다시 펼쳐지지는 않는다.')

p=change('AmberSeed','유리덮개',[
'누군가 두고 간 작은 화분에 열매 하나가 남아 있다.\n\n화분 손잡이에는 재배법이 매달려 있다. 피를 먹여 키우면 몸을 튼튼하게 해 주는 식물이라고 한다.\n\n열매를 바로 먹어도 상처에 좋지만, 씨앗까지 먹으면 다시 키울 수는 없다.'])
ending(p,0,0,'손가락의 피를 먹이자 작은 싹이 손목을 감았다. 싹을 조심스레 떼어 품에 넣었다. 빈 화분만 남았다.',used=True,revisit='열매가 사라진 빈 화분이다.')
ending(p,0,1,'열매를 씨앗째 씹어 삼켰다. 상처의 통증이 잦아들었다. 화분에는 마른 줄기만 남았다.',used=True,revisit='열매를 먹고 남긴 화분이다. 다시 열릴 싹은 없다.')

p=change('Well','저주받은금고',[
'작은 돈주머니가 매달린 금고가 길가에 놓여 있다.\n\n위쪽 주머니는 쉽게 풀 수 있지만, 금고의 손잡이에는 낡은 경고문이 감겨 있다.',
'손잡이를 당기자 방패를 든 팔이 갑자기 무거워진다.\n\n“금고를 여는 자는 그 무게도 함께 짊어진다.”\n\n손을 떼면 팔이 가벼워진다. 저주는 금고에 묶여 있는 모양이다.'],title='길가의 무거운 금고')
field(p,'pages.Array.data[0].options.Array.data[1].title','금고의 손잡이도 당겨 본다.')
ending(p,0,0,'작은 주머니의 동전만 꺼냈다. 빈 주머니를 매달아 둔 채 손을 떼자 금고의 봉인이 굳어졌다.',revisit='윗주머니는 비었고, 금고의 봉인은 굳어 버렸다.')
ending(p,1,0,'금고가 열리자 돈이 쏟아졌다. 돈을 챙긴 뒤에도 방패를 든 팔의 무게는 줄지 않았다.',used=True,revisit='텅 빈 금고다. 짊어진 무게를 돌려줄 수는 없다.')
ending(p,1,1,'손잡이를 놓자 팔이 다시 가벼워졌다. 봉인이 단단히 닫히는 소리가 났다.',revisit='다시 닫힌 봉인은 움직이지 않는다.')

p=change('Investment','예약받는짐꾼',[
'커다란 운반틀을 멘 짐꾼이 길가에서 꾸러미를 고쳐 묶고 있다.\n\n“더 깊이 내려가십니까? 저는 짐을 나르는 길을 압니다.”\n\n예약금을 내면 다음 구역의 통로에 내 표식이 붙은 장비를 남겨 주겠다고 한다.\n\n“지금 약이 필요하시면 여기서 드릴 수도 있습니다.”'],npc=True)
for j in range(3): ending(p,0,j,['짐꾼은 내 표식을 받아 장부에 붙였다. “다음 구역의 통로에서 찾아보세요.” 운반틀을 고쳐 멘 그가 먼저 길을 떠났다.','짐꾼에게 약을 받았다. 그는 꾸러미를 다시 묶고 다른 길로 향했다.','인사를 나누고 비켜섰다. 짐꾼은 운반틀을 메고 길을 떠났다.'][j],remove=True)

p=change('StormBottle','번개병장수',[
'뾰족한 모자를 쓴 여인이 길가에서 병 하나를 흔든다. 유리 안에서 작은 천둥이 울린다.\n\n“무기에 쓸 번개 필요하세요? 영창을 몰라도 됩니다.”\n\n병을 기울이자 번개가 유리벽을 따라 움직인다.\n\n“직접 주문을 쓰고 싶으면 이 두루마리를 보세요. 빈 병을 모아 주셔도 품삯은 드려요.”'],npc=True)
for j in range(3): ending(p,0,j,['번개가 갇힌 병을 받았다. 여인은 남은 병의 마개를 점검하기 시작했다.','여인이 펼친 두루마리를 읽었다. 영창을 되짚는 동안 그녀는 남은 병을 정리했다.','흩어진 빈 병을 모아 건네고 품삯을 받았다.'][j],revisit='여인은 남은 병의 마개를 점검하고 있다. “오늘 준비한 거래는 끝났어요.”')

p=change('Shelter','졸고있는경비병',[
'낡은 담요를 무릎에 덮은 경비병이 길가에 기대어 있다.\n\n나를 보자 졸린 눈을 비비며 한쪽 자리를 내준다.\n\n“잠깐 눈 좀 붙이려는데, 번갈아 망을 봐 주겠소? 아무도 안 봐 주면 불안해서 말이오.”'],title='잠깐의 교대',npc=True)
field(p,'pages.Array.data[0].options.Array.data[0].title','망을 번갈아 보며 쉰다.')
field(p,'pages.Array.data[0].options.Array.data[1].title','몸을 풀며 망을 본다.')
for j in range(3): ending(p,0,j,['경비병과 번갈아 망을 보며 잠시 쉬었다. 내 차례가 끝나자 그는 다시 담요를 끌어당겼다.','잠을 자는 대신 몸을 풀며 망을 봤다. 경비병이 일어나 고맙다며 자리를 정돈했다.','강아지가 귀를 세우고 주변을 살폈다. 경비병은 고맙다며 품삯을 건넸다.'][j],revisit='경비병은 담요를 무릎에 덮고 쉬고 있다. 오늘의 교대는 끝났다.')

p=change('Contract','붉은계약서',[
'붉은 밀랍으로 봉한 두루마리가 길가에 놓여 있다.\n\n손을 대자 종이에서 목소리가 흘러나온다.\n\n“서명하면 힘을 얻는다. 원치 않으면 금박 봉인만 가져가도 좋다.”',
'“서명자의 생명을 힘으로 바꾼다. 이 성에서 오래 버티고 싶다면 쓸 만한 거래다.”',
'“남은 수명의 일부다. 다친 상처처럼 다시 채울 수 있는 것은 아니다.”',
'종이에서 뻗은 붉은 빛이 내 무기를 감싼다. 잠깐이나마 무거운 무기가 가볍게 들린다.\n\n“서명하면 이 힘이 남는다. 생명이 줄어드는 일도 되돌릴 수 없다.”'])
for i,page in enumerate(events['Contract']['pages']):
    for j,o in enumerate(page['options']):
        if '판다' in o['title']:
            field(p,f'pages.Array.data[{i}].options.Array.data[{j}].title','금박 봉인만 떼어 낸다.')
            ending(p,i,j,'금박을 떼어 내자 계약서와 받침함이 함께 재로 무너졌다. 챙긴 금박은 돈으로 쓸 수 있겠다.',remove=True)
        elif o['title']=='서명한다.': ending(p,i,j,'서명을 마치자 숨이 잠시 막혔다. 몸을 일으키니 이전보다 무기가 가볍다. 계약서와 받침함이 함께 붉은 재가 되어 흩어졌다.',remove=True)

p=change('Delivery','표식붙은두꾸러미',[
'길가에 내 표식이 붙은 꾸러미 두 개가 놓여 있다.\n\n지난번 짐꾼의 쪽지가 끈에 매달려 있다.\n\n“예약하신 장비와 거스름돈입니다. 여비가 더 필요하면 옆의 금고를 쓰세요. 기록지에 기술 하나를 옮기면 장비 대신 돈을 드립니다.”\n\n금고의 작은 틈에서는 종이를 기다리는 듯 펜촉이 움직인다.'])
for j in range(2): ending(p,0,j,['꾸러미의 장비와 거스름돈을 챙겼다. 끈에 달린 표식이 저절로 떨어졌다.','기술을 옮겨 적자 머릿속에서 익숙한 동작 하나가 흐릿해졌다. 금고가 장비 꾸러미를 삼키고 돈을 내밀었다.'][j],used=True,revisit='표식이 떨어진 빈 꾸러미다. 맡긴 물건은 모두 받았다.')

p=change('MagicTeacherFirst','길위의노마법사',[
'잉크 묻은 손의 노인이 길가에서 두루마리를 붙잡고 씨름하고 있다.\n\n그의 손에서 흘러나온 작은 불꽃이 옷자락으로 옮겨붙으려 한다.',
'불씨를 털어 주자 노인이 허리를 편다.\n\n“아, 고맙습니다. 기록만 들여다보다 보면 옷 타는 줄도 모른다니까요.”\n\n노인은 한때 고전학파에서 마법을 가르쳤다고 한다. 지금은 성을 떠돌며 남은 기록을 찾는 중이다.\n\n“마법에는 관심 없으세요? 도와주신 보답은 하고 싶은데.”',
'노인이 무릎 위에 작은 책을 펼친다.\n\n“불꽃만 다루는 건 아닙니다. 적의 발을 묶거나 해로운 마력을 걷어 내는 주문도 있지요. 마음이 가는 쪽을 골라 보세요.”',
'노인이 무릎 위에 작은 책을 펼친다.\n\n“불꽃만 다루는 건 아닙니다. 적의 발을 묶거나 해로운 마력을 걷어 내는 주문도 있지요. 마음이 가는 쪽을 골라 보세요.”',
'노인이 손바닥을 펼쳐 보인다.\n\n“먼저 손끝에 마력을 모아 보세요.”'],title='길 위의 노마법사',npc=True)
field(p,'pages.Array.data[0].options.Array.data[0].title','옷자락의 불씨를 털어 준다.')
ending(p,0,1,'노인에게 옷자락을 가리켜 보였다. 그는 허둥지둥 불씨를 끄고 고맙다는 손짓을 하더니 짐을 챙겨 떠났다.',remove=True)
for i,page in enumerate(events['MagicTeacherFirst']['pages']):
    for j,o in enumerate(page['options']):
        if o['nextPage']<0 and not (i==0 and j==1): ending(p,i,j,'노인은 책을 가방에 넣고 자리에서 일어났다. “다음에 만나면 손이 얼마나 익었는지 봅시다.” 서로 인사를 나누고 길을 떠났다.',remove=True)

src=events['MagicTeacherReturn']; texts=[]
for i,page in enumerate(src['pages']):
    t=page['narrative'].replace('연구실 안에서 익숙한 목소리가 들린다.','큰 책을 안고 길가에 앉은 노인이 나를 알아본다.').replace('다른 연구실로 떠났다.','다른 통로로 떠났다.').replace('성 안의 막힌 통로와 옛 강의실','성 안의 길과 떠돌며 본 일들')
    if i==4: t='노인이 길가에서 가방을 고쳐 묶다 나를 알아본다.\n\n“아, 당신. 아직 살아 있었군요? 지난번 마법은 좀 써 보셨어요?”\n\n손끝을 살피더니 그는 웃으며 고개를 젓는다.\n\n“아직은 손에 익힐 시간이 더 필요하겠네요. 오늘은 길 이야기나 하고 갑시다.”'
    texts.append(t)
p=change('MagicTeacherReturn','노마법사의펼친책',texts,npc=True)
for i,page in enumerate(src['pages']):
    for j,o in enumerate(page['options']):
        if o['nextPage']<0: ending(p,i,j,'이야기를 마친 노인은 펼친 책을 가방에 넣었다. 다음 만남을 기약하고 서로 다른 통로로 향했다.',remove=True)

p=change('MagicTeacherAdvanced','노마법사의수정지팡이',[
'수정 지팡이를 짚은 노인이 길가에서 내 마력을 살핀다.\n\n지난번의 선생이다. 지팡이 끝의 빛이 내 손끝에 반응하자 그는 먼저 책을 꺼낸다.\n\n“이제는 더 큰 주문을 버틸 수 있겠네요. 어느 쪽을 배워 볼까요?”',
'“음… 그 속성까지는 아직 자질이 안 보이는데요. 익힌 마법에서 더 나아가는 게 좋겠습니다.”',
'노인이 지팡이의 빛과 내 손끝을 번갈아 살피더니 책을 도로 넣는다.\n\n“아직 큰 주문을 버틸 만큼 마력이 모이지 않았군요. 오늘은 여기까지 합시다.”\n\n잠깐 길 이야기를 나눈 뒤 서로의 짐을 챙겼다.'],title='지팡이 끝의 수업',npc=True)
for i,page in enumerate(events['MagicTeacherAdvanced']['pages']):
    for j,o in enumerate(page['options']):
        if o['nextPage']<0: ending(p,i,j,'선생은 지팡이의 빛을 거두고 책을 챙겼다. 서로 인사를 나눈 뒤 그는 다른 통로로 걸어갔다.',remove=True)

p=change('Banner','깃발곁의가시등짐승',[
'등에 가시 돋은 짐승이 낡은 깃발 곁에 웅크리고 있다.\n\n목줄에 묶인 깃발을 잡아당길 때마다 짐승이 낮게 으르렁거린다. 주인이 떠난 뒤에도 혼자 지키고 있는 모양이다.\n\n바깥쪽에 굴러 나온 작은 급료 주머니의 동전은 조용히 꺼낼 수 있겠다. 깃발까지 챙기려면 짐승을 물리쳐야 한다.'],title='주인 잃은 깃발',npc=True,automatic=True)
field(p,'pages.Array.data[0].options.Array.data[0].title','짐승을 물리치고 깃발을 챙긴다.')
ending(p,0,0,'짐승이 쓰러졌다. 목줄에서 풀어 낸 깃발을 챙겼다.',used=True,revisit='쓰러진 짐승이다. 곁에 있던 깃발은 챙겼다.')
ending(p,0,1,'급료 주머니의 동전만 조용히 꺼냈다. 짐승은 깃발 곁을 떠나지 않았다.',revisit='짐승이 여전히 깃발을 지키고 있다. 주머니는 비었으니 더 자극하지 않기로 했다.')

p=change('Training','묶인훈련인형',[
'운반틀에 묶인 훈련 인형이 길가에 버려져 있다. 회색 몸체에 주황색 띠가 둘러져 있고, 검과 둥근 방패가 함께 묶여 있다. 가까이 가자 둥근 머리가 움직인다.\n\n끈과 철사, 굵은 사슬이 차례로 관절을 붙잡고 있다.\n\n운반틀에는 경고가 붙어 있다.\n\n“구속을 많이 풀수록 강하게 움직임. 도전 보상은 내부 수납함에 보관.”'],title='어디까지 풀 것인가',automatic=True)
for j in range(3): ending(p,0,j,'인형의 움직임이 멎었다. 수납함에서 보상을 챙기자 남은 구속구도 바닥으로 떨어졌다.',used=True,revisit='힘을 잃고 처진 훈련 인형이다. 수납함은 비어 있다.')

p=change('StrayDog','다친강아지',npc=True)
ending(p,0,1,'목줄에 묶인 강아지를 뒤로했다.',revisit='강아지가 목줄에 묶인 채 웅크리고 있다. 오늘은 도와주지 못했다.')
for j in range(3): ending(p,1,j,['강아지가 허겁지겁 먹이를 먹었다. 자리를 뜨려 하자 내 곁으로 다가왔다.\n\n강아지가 동료로 합류했다!','치료가 끝나자 강아지가 내 손을 핥았다. 짐을 챙기는 나를 졸졸 따라왔다.\n\n강아지가 동료로 합류했다!','목줄에서 풀려난 강아지는 잠시 나를 바라보다 통로 너머로 사라졌다.'][j],remove=True)

p=change('Pilgrim','이름돌의순례자',[
'이름이 새겨진 돌을 자루에 담은 순례자가 길가에서 쉬고 있다.\n\n굴러온 돌 하나를 건네자 그가 자루를 벌려 보인다.\n\n“여기서 죽은 이들이네. 유족들에게 이름만이라도 전해 주려는 참이야.”\n\n자루가 무거워 짐꾼을 구했지만 여비가 모자란다고 한다. 아는 지름길도 없어 돌아갈 길이 막막한 모양이다.'],npc=True)
for j in range(3): ending(p,0,j,['순례자는 여비를 받아 품에 넣고 작은 표식을 건넸다. 돌자루를 고쳐 멘 그가 돌아갈 길로 향했다.','서로 알고 있는 샘의 위치를 종이에 표시했다. 순례자는 고맙다며 돌자루를 메고 길을 떠났다.','강아지가 냄새를 맡더니 순례자를 다른 통로로 이끌었다. 길을 확인한 순례자는 인사를 남기고 떠났고 강아지는 내게 돌아왔다.'][j],remove=True)

# Disabled legacy stories are still rewritten so reactivation cannot revive special-room premises.
legacy={
'Forge':['휴대 모루를 짊어진 장인이 길가에서 쉬고 있다. 내 방패를 보더니 찌그러진 부분을 가리킨다.\n\n“그건 금방 펴지지. 싸우는 자세도 봐 줄 수 있고.”','“한 번 휘둘러 봐.”\n\n몇 번 움직이자 장인이 발과 손목의 위치를 짚어 준다.\n\n“지금 기술을 다듬을까, 나쁜 버릇 하나를 버리고 새로 배울까?”'],
'Hourglass':['길가에 놓인 작은 운반 상자에서 모래시계 하나가 흔들리고 있다.\n\n손잡이에 묶인 쪽지에는 시간을 빌릴 수 있다고 적혀 있다.','몸이 가벼워진다. 떨어지던 종이 한 장이 공중에 멈춰 있다.\n\n유리에 금이 하나 더 생겼다.\n\n쪽지의 뒷면에는 시간을 빌린 만큼 수명이 줄어든다고 적혀 있다.'],
'Symbiosis':['길가의 깨진 약재 통에서 뿌리 돋은 씨앗이 보인다.\n\n통에 붙은 쪽지는 생명력을 먹고 숙주를 강화하는 식물이라고 설명한다. 뿌리를 태운 연기는 기억을 흐리게 하고 껍질은 약재로 쓴다고 한다.\n\n손을 가까이 대자 뿌리가 내 손목을 향해 움직인다.'],
'MasterTrial':['길가에 놓인 작은 돌가면의 입이 움직인다. 내 이름이다. 하지만 한 글자가 틀렸다.','“그 이름을 쓸 자격이 있는지 증명하라.”\n\n돌가면에서 묵직한 마력이 흘러나온다.','돌가면의 뒷면에 오래된 기술과 호흡법이 새겨져 있다.'],
'Bell':['작은 종이 쓰러진 운반 수레 아래에 끼어 있다. 바퀴가 흔들릴 때마다 맑은 소리가 난다.','수레를 조금만 들어도 종을 뺄 수 있겠다. 받침이 없으면 내 어깨로 무게를 버텨야 한다.','부러진 나뭇가지로는 무게를 버티지 못한다. 가지고 있는 유물이라면 버틸 수 있겠지만 수레 아래에 남겨야 한다.']}
for id,pages in legacy.items(): change(id,'',pages,npc=id=='Forge')

# Choice-specific endings retain what the player actually paid, received, or declined.
by_id={p['id']:p for p in patches}
p=by_id['LostBeliever']
for i,j in [(0,1),(1,1),(2,1)]:
    ending(p,i,j,'안전한 길을 그려 주자 소년이 보따리에서 약초를 꺼내 내 상처에 발라 주었다. 지도를 챙긴 소년은 돌아갈 길로 향했다.',remove=True)
ending(p,1,0,'표식을 보여 주고 순례자가 향한 통로를 짚어 주었다. 소년은 보답이라며 작은 등불을 건네고 보따리를 안아 들었다. 우리는 서로 다른 길로 향했다.',remove=True)
for i in [0,1]: ending(p,i,2,'도와줄 방법을 찾지 못한 채 비켜섰다. 소년은 보따리를 끌어안고 다시 길을 살폈다.',revisit='소년은 보따리를 안고 길을 살피고 있다. 더 해 줄 말을 찾지 못했다.')
p=by_id['Forge']
ending(p,0,0,'장인이 휴대 모루를 내려놓고 방패를 두드려 폈다. 고친 방패를 받아 들자 그는 도구를 다시 정리했다.',revisit='장인은 도구를 정리하고 있다. 부탁했던 정비는 끝났다.')
ending(p,1,0,'몇 차례 자세를 고쳐 휘둘렀다. 장인은 고개를 끄덕이며 모루를 챙겼다.',revisit='배운 자세를 되짚었다. 장인은 짐을 정리하고 있다.')
ending(p,1,1,'익숙한 동작을 버리고 장인이 짚어 준 두 가지 자세를 연습했다. 수업을 마치자 그가 모루를 챙겼다.',revisit='오늘 배운 자세부터 익히기로 했다. 장인은 짐을 정리하고 있다.')
p=by_id['Hourglass']
ending(p,0,1,'깨진 유리 조각만 골라 주머니에 넣었다. 모래시계는 손대지 않았다.',revisit='주변 유리는 챙겼다. 모래시계는 그대로 떠 있다.')
ending(p,1,0,'가슴속에서 무언가 빠져나갔다. 모래시계를 품에 넣자 주변의 움직임이 돌아왔다.',remove=True)
ending(p,1,1,'모래가 거꾸로 흐르자 몸에 밴 동작 하나가 사라졌다. 빈 기억에 기록지의 두 기술을 새기고 손을 뗐다.',revisit='시간을 되돌린 모래시계다. 더 시간을 빌리지는 않기로 했다.')
p=by_id['Symbiosis']
ending(p,0,0,'뿌리가 피부를 파고들었다. 욱신거리는 팔을 움직이니 전보다 힘이 들어간다. 빈 약재 통만 남았다.',remove=True)
ending(p,0,1,'뿌리를 태운 연기와 함께 익숙한 동작 하나가 흐려졌다. 통에 붙은 기록에서 새로운 기술을 익히고 재를 털었다.',remove=True)
ending(p,0,2,'껍질을 벗겨 상처에 문질렀다. 남은 씨앗은 말라 부스러졌다.',remove=True)
p=by_id['MasterTrial']
field(p,'pages.Array.data[0].options.Array.data[1].title','돌가면 뒷면의 글을 읽는다.')
ending(p,1,0,'돌가면이 불러낸 시련을 이겨 냈다. 가면의 입에서 내 몫의 보상이 떨어졌다.',revisit='돌가면은 입을 다물었다. 다시 이름을 부르지는 않는다.')
ending(p,2,0,'익숙한 동작 하나를 버리고 돌가면에 새겨진 두 기술을 몸에 익혔다.',revisit='돌가면에 새겨진 기술은 읽었다. 이제 직접 익힐 차례다.')
ending(p,2,1,'새겨진 호흡법을 따라 숨을 골랐다. 긴장을 늦추자 몸이 조금 편해졌다.',revisit='배운 호흡법을 되짚었다. 더 읽을 내용은 없다.')
p=by_id['Bell']
field(p,'pages.Array.data[1].options.Array.data[0].title','어깨로 수레를 들어 올린다.')
for i,j in [(0,1),(2,1)]: ending(p,i,j,'바닥에 흩어진 청동 조각만 챙겼다. 종은 수레 아래에 남겨 두었다.',revisit='청동 조각은 챙겼다. 종은 여전히 수레 아래에 있다.')
ending(p,1,0,'어깨로 수레를 버티며 종을 꺼냈다. 짐을 챙기고 수레를 조심스레 내려놓았다.',remove=True)
ending(p,2,0,'받침으로 넣은 유물을 수레 아래에 남기고 종을 꺼냈다. 수레를 다시 건드리지는 않기로 했다.',remove=True)

# Every terminal receives an authored revisit response; unchanged declines keep the subject present.
for p in patches:
    src=events[p['id']]; terminal={(t['page'],t['index']) for t in p['terminals']}
    sources=[(-1,src['options'])]+[(i,pg['options']) for i,pg in enumerate(src.get('pages',[]))]
    for page,options in sources:
        for j,o in enumerate(options):
            if o.get('nextPage',-1)>=0 or (page,j) in terminal: continue
            ending(p,page,j, '나는 내 길을 계속 가기로 했다.',revisit=('아까 만난 이다. 이야기는 마쳤으니 더 말을 걸지 않기로 했다.' if p['npc'] else '아까 살펴본 물건이다. 더 손대지 않기로 했다.'))

(OUT/'Plan.json').write_text(json.dumps({'events':patches},ensure_ascii=False,indent=2),encoding='utf-8')
print('Authored',len(patches),'events; active',sum(not e['excludedFromRandomPool'] for e in before))
