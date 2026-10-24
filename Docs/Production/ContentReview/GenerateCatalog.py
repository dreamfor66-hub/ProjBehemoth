"""Render the read-only Unity ContentReviewExport snapshot into Korean review tables."""
import json
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parent
PROJECT = ROOT.parents[2]
snapshot = json.loads((ROOT / 'Snapshot.json').read_text(encoding='utf-8-sig'))
chars = {'Warrior': '워리어', 'Rogue': '로그', 'Barbarian': '바바리안', 'Samurai': '사무라이', 'Monk': '몽크', 'Wizard': '위자드'}
raw = lambda item: json.loads(item['raw'])
clean = lambda value: str(value).replace('\r', '').replace('\n', '<br>').replace('|', '\\|')
number = lambda value: f'{value:g}'
link = lambda item: f"[{item['name']}]({(PROJECT / item['path']).as_posix()})"
by_path = {item['path']: item for group in ('perks', 'equipment', 'spells') for item in snapshot[group]}
by_id = {item['id']: item for item in snapshot['equipment']}
spells = {item['id']: item for item in snapshot['spells']}
excluded = [story for story in snapshot['events'] if raw(story).get('excludedFromRandomPool')]


def table(headers, rows):
    return '\n'.join(['| ' + ' | '.join(headers) + ' |', '| ' + ' | '.join(['---'] * len(headers)) + ' |'] +
                     ['| ' + ' | '.join(clean(v) for v in row) + ' |' for row in rows]) + '\n'


def save(filename, text):
    (ROOT / filename).write_text(text, encoding='utf-8')


common_source = '유물 상자 / 상점 / 훈련 사건의 사슬 상대 승리 / 배달 사건의 유물 선택'
common_time = 'B1부터; 배달은 B2~B3'
exclusive_source = '해당 직업의 B1·B2 보스 승리 후 전용 유물 3택1'
exclusive_time = 'B1 보스 이후부터'
perk_source = '레벨업 퍽 3택1'
perk_time = 'B1 첫 레벨업부터'

# Perks: one acquisition's printed effect, rather than hypothetical capped build totals.
text = '# 퍽 목록 · 41종\n\n'
text += '기준: 2026-10-24 수정 후 저장된 게임 데이터. **공용 11종 + 직업별 5종 × 6직업**이며, 41종 모두 실제 플레이 퍽 목록에 등록되어 있다. 효과는 1회 획득 기준이다. 현재 해금 조건은 없다.\n\n'
text += '입수처는 모두 **레벨업 보상의 퍽 3택1**, 입수 시기는 **B1 첫 레벨업부터**다. 해당 직업 전용 퍽은 해당 직업에게만 나온다. 「오늘의 상대를 고르시오」 철사 상대 승리(2회)와 「거울이 먼저 벤다」·「끝까지 읽는 법」에서는 이미 가진 퍽의 중첩을 추가할 수 있다. 제거 후 새 퍽을 고르는 사건 5종은 출현에서 제외했다.\n\n'
for scope, kind in [('Shared', None)] + [('Exclusive', kind) for kind in chars]:
    group = [p for p in snapshot['perks'] if p['scope'] == scope and (kind is None or p['character'] == kind)]
    text += '## ' + ('공용' if kind is None else chars[kind]) + '\n\n'
    text += table(['이름', '효과 · 1회 획득', '최대 중첩', '입수처', '입수 시기'],
                  [(link(p), p['description'], p['maxStacks'], perk_source, perk_time) for p in group]) + '\n'
text += '「마력 그릇」과「마력 순환」은 합쳐서 일반 퍽 하나의 등장 비중을 쓰고, 같은 보상에 함께 등장하지 않는다. 「축약 영창」은 위자드 퍽과 위자드 유물에 같은 이름이 존재하므로 구분해서 봐야 한다.\n'
save('Perks.md', text)

# Relics include actual relics that also teach a spell; ordinary tomes are counted separately.
relics = [e for e in snapshot['equipment'] if raw(e)['itemKind'] == 0 and not e['id'].startswith('Tome_')]
books = [e for e in snapshot['equipment'] if e['id'].startswith('Tome_')]
potions = [e for e in snapshot['equipment'] if raw(e)['itemKind'] == 1]
assert len(relics) == 84 and len(books) == 12 and len(potions) == 6
available = snapshot['playableEquipment']

event_sources = {
    'StrayDog': ('「젖은 발자국」에서 고기 1개 또는 회복 포션 1개 제공', 'B1~B3'),
    'GuidingWisp': ('「그 돌에 적힌 이름」: 순례자의 서약 보유 선택 또는 체력 18 소모', 'B2~B3; 앞선 순례자 사건에서 인연을 남겨야 사건 출현'),
    'Parasite': ('현재 입수 불가 — 「두 번째 맥박」 출현 제외', '없음; 제외 전 B1~B3'),
    'MirrorShard': ('「거울이 먼저 벤다」에서 체력 16 소모', 'B1~B3'),
    'BloodContract': ('「붉은 서명」에서 최대 체력 18 소모', 'B1~B3'),
    'PilgrimOath': ('「짐 하나만 부탁하네」에서 25골드 지불 또는 강아지 보유 선택', 'B1~B3'),
    'Hourglass': ('현재 입수 불가 — 「내일의 한숨」 출현 제외', '없음; 제외 전 B1~B3'),
    'StormBottle': ('「번개를 파는 행상」에서 45골드 지불', 'B1~B3; B1에서는 일반전투 4회 완료 후'),
    'AmberSeed': ('「딱 하나 남은 열매」에서 체력 10 소모', 'B1~B2'),
    'GuardianBell': ('현재 입수 불가 — 「소리 나는 쪽을 받쳐라」 출현 제외', '없음; 제외 전 B1~B3'),
    'RaggedBanner': ('「아직 교대 시간이 아니다」에서 수호자 전투 승리', 'B1~B3'),
    'GoldenTooth': ('「금빛 이빨의 내기」에서 40골드 지불', 'B1~B2'),
    'WellCurse': ('「밑바닥의 금고」에서 큰 금고를 선택할 때 골드 +150과 함께 획득하는 저주', 'B1~B3'),
}

effect_overrides = {
    'HunterLantern': '표기: 몬스터 처치 시 HP 7 회복. **현재 데이터에는 회복 효과가 연결되어 있지 않음.**',
    'ThunderWire': '표기: 일반 베기 피해 +20%, 재사용 대기시간 -12%. **현재 데이터에는 해당 능력치 효과가 연결되어 있지 않음.** 번개 속성 피해도 제공하지 않음.',
    'Starter_Warrior': '표기: 전투 중 가드게이지 초당 2.5 회복. **현재 수호자의 방패 데이터는 회복 효과와 일치하지 않음 — 확인 메모 참조.**',
    'CrosswindCharm': '가로 베기 적중 시 중앙에서 90도 회전한 같은 길이의 추가 공격. 추가 공격 피해 계수 50%.',
    'EchoSpur': '내려베기 적중 시 같은 길이의 추가 공격. 추가 공격 피해 계수 55%.',
    'Technique_CoreHarvest': '약점 **파괴** 시 실드 +10. 설명의「약점 타격」과 실제 트리거가 다름.',
}


def relic_row(e):
    effect = effect_overrides.get(e['id'], e['description'])
    if e['spell']:
        effect += f" (습득 주문의 현재 이름: {by_path[e['spell']]['name']})"
    if e['scope'] == 'Starting':
        source, time = f"{chars[e['character']]} 시작 유물", '런 시작 시'
    elif e['scope'] == 'Exclusive':
        source, time = exclusive_source, exclusive_time
    elif e['scope'] == 'Event':
        source, time = event_sources[e['id']]
    elif e['path'] not in available:
        source, time = '현재 입수 불가 — 카탈로그에만 있고 플레이 보상 목록에 없음', '없음'
    else:
        source, time = common_source + f"; 상점 {e['price']}골드", common_time
    return link(e), effect, source, time


text = '# 유물 목록 · 84종 + 일반 주문서 12종\n\n'
text += '시작 6종 / 공용 17종 / 직업 전용 48종 / 이벤트 전용 13종 = **유물 84종**. 이 중 **현재 입수 가능 78종**, 사건 제외로 중단 3종, 보상 목록에 빠진 공용 3종이다. 일반 주문서 12종은 별도 표로 구분했다. 포션 6종은 유물에 포함하지 않는다.\n\n'
text += '현재 데이터에는 커먼·레어 같은 **희귀도 구분이 없다**. 아래의 공용/전용/이벤트는 입수 풀 구분이다. 같은 유물은 중복 소유할 수 없으며, 전용 유물 보유 한도는 3개다. 현재 일반 진행에서 전용 유물 선택은 B1·B2 보스 뒤에 있고, B3 보스는 승리 종료이므로 보상을 주지 않는다. 「문이 이름을 틀리게 부른다」의 추가 전용 유물 경로는 이번에 제외됐다.\n\n'
text += '공용 유물은 일반전투·엘리트 승리 때마다 주어지는 보상이 아니다. 아래 표의 **유물 상자·상점·훈련·배달 사건**이 실제 입수 경로다. 상점/보상은 무작위 후보에서 선택하며 특정 속성 슬롯을 보장하지 않는다.\n\n'
text += '## 시작 유물 · 6종\n\n' + table(['이름', '효과', '입수처', '입수 시기'], [relic_row(e) for e in relics if e['scope'] == 'Starting']) + '\n'
text += '## 공용 유물 · 17종\n\n' + table(['이름', '효과', '입수처', '입수 시기'], [relic_row(e) for e in relics if e['scope'] == 'Shared']) + '\n'
for kind, name in chars.items():
    text += f'## {name} 전용 유물 · 8종\n\n'
    text += table(['이름', '효과', '입수처', '입수 시기'], [relic_row(e) for e in relics if e['scope'] == 'Exclusive' and e['character'] == kind]) + '\n'
text += '## 이벤트 전용 · 13종\n\n' + table(['이름', '효과', '입수처', '입수 시기'], [relic_row(e) for e in relics if e['scope'] == 'Event']) + '\n'
text += '## 일반 주문서 · 12종\n\n게임 내부에서는 유물과 같은 장비 데이터지만, 역할은 해당 주문의 습득이다. 이름은 실제 상점에 표시되는 이름 그대로 적었다.\n\n'
text += table(['이름', '효과', '입수처', '입수 시기'], [(link(e), by_path[e['spell']]['name'] + ' 습득', common_source + '; 상점 38골드', common_time) for e in books])
text += '\n「잿불 화살의 마도서」는 **파이어볼트**,「화염 개화의 마도서」는 **화염폭발**을 가르친다. 주문서/일부 유물에 이전 주문명이 남아 있다. 주문 자체의 효과·MP·쿨타임·이벤트 경로는 주문 목록 참조.\n'
save('Relics.md', text)

teacher = {
    'Ember': ['칠판 앞의 노인(B1~B3): 첫 수업'],
    'FrostRing': ['칠판 앞의 노인(B1~B3): 파이어볼트를 이미 알 때 대체 수업'],
    'EnchantFire': ['칠판 앞의 노인(B1~B3): 파이어볼트를 이미 알 때 대체 수업', '성장한 제자(B2~B3): 재회 수업'],
    'EnchantIce': ['성장한 제자(B2~B3): 재회 수업'],
    'EnchantLightning': ['성장한 제자(B2~B3): 재회 수업'],
    'FireBloom': ['성장한 제자(B2~B3): 파이어볼트 보유 후 수업'],
    'ChainLightning': ['성장한 제자(B2~B3): 재회 수업'],
}
prerequisites = {'Meteor': '화염폭발 보유', 'VortexLightning': '연쇄 번개 보유', 'IcicleShower': '서리 파동 보유', 'CrimsonInferno': '화염폭발 보유'}
spell_effects = {
    'Ember': '그은 방향으로 관통하는 화염탄. 기본 화염 피해 36.',
    'EnergyBolt': '그은 방향으로 관통하는 에너지 볼트. 기본 **냉기** 피해 17.',
    'Spark': '긋기를 끝낸 지점에 번개. 기본 번개 피해 13.',
    'FireBloom': '체력 비율이 가장 낮은 적 주위에 폭발. 범위 화염 피해 20.',
    'FrostRing': '마지막 무기 적중 대상(없으면 높은 현재 체력 우선) 주위에 서리. 냉기 피해 12, 둔화 3초.',
    'ChainLightning': '긋기를 끝낸 지점에서 시작하는 번개 피해 14. 최대 **3개 대상**에게 연결.',
    'PoisonMist': '10초 동안 독안개. 2초마다 독 1중첩과 둔화를 갱신한다. 자체 직접 피해 0.',
    'EnchantFire': '10초 동안 무기 적중에 화염 피해 4와 화상 추가.',
    'EnchantIce': '10초 동안 무기 적중에 냉기 피해 4와 둔화 추가.',
    'EnchantLightning': '10초 동안 무기 적중에 번개 피해 4, 다른 적에게 번개 피해 2 추가.',
    'Cleanse': '자신의 해로운 효과 제거.',
    'LightningMark': '적에게 7초 표식. 다음 무기 적중 피해의 30%를 **다른 적**에게 번개로 전달. 1대1에서 전달 대상이 없음.',
    'Meteor': '대상 주위에 운석. 범위 화염 피해 120.',
    'VortexLightning': '대상 주위에 6초 동안 번개 소용돌이. 0.75초 간격으로 번개 피해 14(총 8회).',
    'IcicleShower': '대상 주위에 0.5초 간격으로 고드름 6회. 회당 냉기 피해 16, 둔화 1.5초.',
    'CrimsonInferno': '대상 주위에 8초 홍염. 1초 간격으로 화염 피해 12(총 8회), 화상 부여.',
}
spell_rows = []
for s in snapshot['spells']:
    r = raw(s)
    if r['tier'] < 2:
        source = ['해당 일반 주문서(유물 상자/상점/공용 유물 선택)', '번개를 파는 행상(25골드)', '끝까지 읽는 법(체력 9)']
        if s['id'] == 'EnergyBolt':
            source.insert(0, '위자드 시작 주문(집중의 수정)')
        source += teacher.get(s['id'], [])
        extra = [e for e in relics if e['scope'] == 'Exclusive' and e['spell'] == s['path']]
        source += [chars[e['character']] + ' 전용 유물「' + e['name'] + '」' for e in extra]
        time = ('위자드는 시작부터; 다른 경로는 ' if s['id'] == 'EnergyBolt' else '') + 'B1부터(일반 주문서); 행상/서고는 B1 일반전투 4회 후'
        if extra:
            time += '; 전용 유물은 B1 보스 후'
        if s['id'] in teacher:
            time += '; 선생 수업은 입수처에 표시한 층과 아래 조건 참조'
    else:
        source = ['의식실의 수업: 공통 자격 + ' + prerequisites[s['id']]]
        time = 'B3; 선생의 앞선 두 수업 완료 및 성장 조건 충족'
    spell_rows.append((link(s), spell_effects[s['id']], f"{number(r['mpCost'])} / {number(r['cooldownSeconds'])}초", ['←', '→', '↑', '↓'][r['defaultSlot']], '<br>'.join(source), time))
text = '# 주문 목록 · 16종\n\n'
text += '일반 주문 12종(화염폭발의 데이터 단계는 1, 나머지는 0) + 심화 주문 4종(단계 2). **모든 일반 주문서는 B1부터 입수 가능**하며 인챈트 선행 소유나 메타 해금 락은 없다. 심화 주문은 일반 상자·상점에 들어가지 않는다.\n\n'
text += '피해는 기본 데이터 수치로 직업/퍽/유물/치명타 보정 전이다. MP와 쿨타임도 기본값이다. 일반 주문의 기본 캐스팅은 0.4초, 심화는 0.6초. 시작 쿨타임은 전부 0초. 위자드는 시작 유물의 쿨타임 감소 등으로 실제 수치가 달라진다. **에너지 볼트는 현재 냉기 속성**, 독안개·정화도 주문 속성 분류는 냉기다.\n\n'
text += table(['이름', '효과', 'MP / 쿨타임', '슬롯', '입수처', '입수 시기'], spell_rows)
text += '\n## 선생 연속 사건 조건\n\n'
text += '- **칠판 앞의 노인**: B1~B3. 구조 후 한 수업을 받으면 첫 수업 완료. 파이어볼트를 모르면 파이어볼트 수업, 이미 알면 서리 파동/인챈트 파이어 대체 수업. 수업 대신 골드를 받거나 떠나면 이 연속 수업 경로를 열지 못한다.\n'
text += '- **성장한 제자**: B2~B3, 첫 수업 완료가 선행. 재회 시 **누적 주문 시전 3회 이상 + 최대 MP 33 이상**이어야 수업으로 진행. 조건 미달이면 대화 사건으로 소비된다. 수업 1개를 마치면 두 번째 수업 완료.\n'
text += '- **의식실의 수업**: B3, 두 번째 수업 완료가 선행. **최대 MP 36 이상 + 누적 주문 시전 12회 이상 + 배운 주문 3종 이상**, 그리고 해당 심화 주문의 선행 주문을 보유해야 한다. 네 가지 중 **한 주문을 추가 습득**한다. 기존 하위 주문을 지우거나 교체하지 않는다. 미달이면 기록을 읽는 사건으로 소비된다.\n'
text += '\n같은 주문을 이미 알고 있으면 일반 주문서 보상 후보에서 빠진다. 같은 방향의 주문을 새로 배울 때는 기존 준비 주문을 유지하고 새 주문을 보관할 수 있다. 슬롯은 방향별 고정이며 주문서에서 배운 대체 주문을 준비한다.\n'
save('Spells.md', text)

text = '# Dungeon Mortalis · 현행 콘텐츠 검토\n\n'
text += '2026-10-24, 번개 추가피해 수정과 교환 사건 제외 후의 저장 데이터 기준. 기획안이 아니라 현재 등록·입수 경로를 대조한 목록이다. 이름을 누르면 해당 Unity 데이터 자산으로 이동한다.\n\n'
text += '- [퍽 전체 목록 · 41종](Perks.md) — 이름 / 1회 획득 효과 / 중첩 상한 / 입수처 / 입수 시기\n'
text += '- [유물 전체 목록 · 84종 + 일반 주문서 12종](Relics.md) — 시작 / 공용 / 직업 전용 / 이벤트 전용 / 주문서\n'
text += '- [주문 전체 목록 · 16종](Spells.md) — 효과 / MP·쿨타임 / 슬롯 / 입수처 / 입수 시기 / 선생 조건\n'
text += '- [별도 확인 메모](Findings.md) — 목록 누락·설명 불일치·논의할 설계 요소\n\n'
text += '## 이번 반영\n\n'
text += '「병 속의 폭풍」「뇌성의 대검」의 차지 적중 번개 피해 8에 번개 피해 증가가 적용되도록 데이터 효과를 수정했다. 기본 피해와 트리거는 유지했다. 생성용 이전 레시피에도 같은 수정을 남겼다. 신규 속성 전환 유물이나 희귀도는 추가하지 않았다.\n\n'
text += '교환 선택지를 포함한 아래 **5개 사건 전체**를 무작위 출현에서 제외했다. 선택지·서사 자산은 향후 재설계를 위해 보존했고, Inspector의 excludedFromRandomPool로 관리한다. 다른 사건의 단순 강화나 퍽→골드 전환은 유지했다.\n\n'
text += table(['제외한 사건', '해당 선택', '함께 중단되는 보상 경로'], [
    ('두 번째 맥박', '퍽 1개 제거 → 새 퍽 선택', '공생하는 씨앗의 유일한 입수처'),
    ('내일의 한숨', '퍽 1개 제거 → 새 퍽 2회 선택', '깨어진 모래시계의 유일한 입수처'),
    ('소리 나는 쪽을 받쳐라', '공용 유물 제거 → 수호의 종', '수호의 종의 유일한 입수처'),
    ('날이 아니라 버릇을 간다', '퍽 1개 제거 → 새 퍽 2회 선택', '이 사건의 강화/최대 실드 증가 선택도 중단'),
    ('문이 이름을 틀리게 부른다', '퍽 1개 제거 → 새 퍽 2회 선택', '이 사건의 전용 유물 전투/체력 증가도 중단'),
])
text += '\n「상자가 두 개다」의 퍽 제거→100골드 선택은 새 퍽/유물 교환이 아니므로 유지했다. 「오늘의 상대를 고르시오」의 기존 퍽 강화도 유지했다.\n\n'
text += '## 검증과 범위\n\n'
text += '기존 Unity Editor에서 **20개 집중 검증 통과**. 두 유물 각각 일반타 미발동, 기본 8, 번개 퍽 1중첩 8.8 / 2중첩 9.6, 화염 퍽 비영향, 물리 차지 기본값 유지, 모든 피해 +25%와 번개 +20% 동시 적용 시 12를 확인했다. B1~B3 총 384회 사건 추첨에서 제외 사건 미출현을 확인했고, 제외 사건만 있는 풀에서는 사건이 나오지 않았다. [검증 로그](Runtime-Checks.txt). 전체 DPS/밸런스 검증은 실행하지 않았다.\n\n'
text += '범위: GameContent의 퍽 41 / 장비 102 / 주문 16 / 사건 23 및 MainGameScene의 실제 퍽 41 / 장비 98 목록을 대조했다. 102 장비는 유물 84 + 일반 주문서 12 + 포션 6이다. 84 유물 중 현재 경로가 있는 것은 78종이다. 파일만 남은 이전 프로토타입 퍽이나 PerkAbilities 내부 효과 자산은 현행 선택 가능한 별도 콘텐츠로 세지 않았다. 일반 해금은 없고, 시기는 최초 입수 가능한 구간이며 등장 보장은 아니다.\n'
save('Index.md', text)

print('Generated Perks41, Relics84, ordinary books12, Spells16. Excluded events:', [e['id'] for e in excluded])
assert len(snapshot['perks']) == 41 and len(snapshot['spells']) == 16 and len(excluded) == 5
assert set(snapshot['playablePerks']) == {e['path'] for e in snapshot['perks']}
assert all(e['path'] in available for e in books)
