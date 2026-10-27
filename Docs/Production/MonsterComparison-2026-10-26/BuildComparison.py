from pathlib import Path
import json,re,html,shutil,csv,collections,hashlib

folder=Path(__file__).resolve().parent
root=folder.parents[2]
history=json.loads((folder/'BeforeRollback/DataAudit.json').read_text('utf-8'))
previous={e['id']:e for e in history}
export=json.loads((folder/'CurrentData.json').read_text('utf-8-sig'))
entries={}
for e in export['monsters']:
    e['data']=json.loads(e['json'])
    for a in e['attacks']:a['data']=json.loads(a['json'])
    entries[e['id']]=e
names={k:e['data']['displayName'] for k,e in entries.items()}
supports={e['data']['supportId']:names[k] for k,e in entries.items() if e['data']['supportId']}
changed={e['id'] for e in history if e['newNormals']}
num=lambda n:format(float(n),'.3f').rstrip('0').rstrip('.')
esc=lambda s:html.escape(str(s),quote=True)

# The old image is resolved through the pre-art monster's actual animation binding.
assets={}
for branch in ('Data','Art'):
    for meta in (root/'Assets'/branch).rglob('*.meta'):
        match=re.search(r'^guid: (\w+)',meta.read_text('utf-8-sig',errors='replace'),re.M)
        if match:assets[match[1]]=meta.with_suffix('')
for side in ('Before','Current'):(folder/'Portraits'/side).mkdir(parents=True,exist_ok=True)
for key,e in entries.items():
    old=(root/'Docs/Production/AngularWorld-2026-10-26/Before/Monsters'/(e['baseline']+'.asset')).read_text('utf-8-sig')
    guid=re.search(r'^  combatAnimation: .*guid: (\w+)',old,re.M)[1]
    animation=assets[guid].read_text('utf-8-sig')
    guid=re.search(r'^  idle: .*guid: (\w+)',animation,re.M)[1]
    shutil.copyfile(assets[guid],folder/'Portraits/Before'/f'{key}.png')
    shutil.copyfile(root/e['sprite'],folder/'Portraits/Current'/f'{key}.png')

categories=collections.defaultdict(set)
companions=collections.defaultdict(list)
for encounter in export['encounters']:
    members=encounter['members']
    if not members:continue
    leader=members[0]['monster'];categories[leader].add(encounter['category'])
    grouped=collections.defaultdict(list)
    for member in members[1:]:grouped[member['monster']].append(member)
    for child,group in grouped.items():
        if child!=leader:companions[leader].append({'child':child,'count':len(group),'kind':'同伴','delays':[m['actionDelay'] for m in group],'encounter':encounter['id']})
for key,e in entries.items():
    # Only failed-charge spawning is an active summon in authored encounters.
    # openingSummons is not appended to a planned EncounterData's explicit members.
    for attack in e['attacks']:
        if attack.get('onFailure'):
            child=attack['onFailure']
            if not any(c['child']==child and c['kind']=='召喚' for c in companions[key]):
                companions[key].append({'child':child,'count':1,'kind':'召喚','delays':[.45],'encounter':'강타 저지 실패'})

def effects(key,context=None):
    e=entries[key];d=e['data'];out=[]
    if context:
        role='소환' if context['kind']=='召喚' else '동행'
        out.append(f"{names[context['parent']]}의 전용 {role} {context['count']}기.")
    else:
        groups=[]
        for child in companions[key]:
            if child['kind']=='同伴':
                text=f"{names[child['child']]} {child['count']}기"
                if text not in groups:groups.append(text)
        if groups:out.append('동행 구성(조우별): '+', '.join(groups)+'.')
    same_encounters=[x for x in export['encounters'] if x['members'] and x['members'][0]['monster']==key]
    for enc in same_encounters:
        count=sum(m['monster']==key for m in enc['members'])
        if count>1:
            out.append(f"동일 개체 {count}기가 함께 등장.")
    normals=[a for a in e['attacks'] if a['data']['type']==0]
    if not normals:out.append('직접 공격 없이 본체를 지원.')
    directions={1:'가로 베기',2:'올려 베기',4:'내려 베기',6:'세로 베기',7:'모든 방향 베기'}
    for a in e['attacks']:
        t=a['data'];m=t['combatMechanics'];charge=t['type']==1;label='강타' if charge else '일반'
        if charge:label+=' '+str(1+sum(b['data']['type']==1 for b in e['attacks'][:e['attacks'].index(a)]))
        count=m['hitCount']
        if not charge and count>1:out.append(f"일반 공격 {count}연타.")
        if charge:
            points=t['chargeWeakPoints'];required=[p for p in points if not p['optional']];optional=[p for p in points if p['optional']]
            def point_text(ps):
                groups=collections.Counter(p['requiredChargeHits'] for p in ps)
                return ' + '.join(f'{n}개(각 강타 {h}회)' for h,n in sorted(groups.items()))
            attack=f'{count}연타' if count>1 else '단타'
            out.append(f"{label}: {attack}. "+('필수 약점 '+point_text(required)+'.' if required else '필수 약점 없음.'))
            if optional:out.append('보조 약점 '+point_text(optional)+'; 파괴 시 스턴 시간 증가.')
            if t['requiredSupportId']:out.append(f"{supports.get(t['requiredSupportId'],t['requiredSupportId'])} 생존 시 선택 가능한 강타. 해당 보조 개체 파괴 시 이 강타 차단.")
        if m['healOnUnguardedHit']>0:out.append(f"흡혈: 플레이어에게 실제 체력 피해를 주면 자기 HP {num(m['healOnUnguardedHit'])} 회복.")
        dg=m['directionalGuard']
        if not charge and dg['enabled']:
            out.append(f"방향 방어: {directions[dg['breakByAttackWay']]}로 방어를 파괴해야 몸통 타격 가능. 파괴 후 시간이 지나면 방어 재생.")
        if a.get('onFailure'):
            heal=t['chargeSummonMechanics']['healOnFailedChargeWhileSummonAlive']
            out.append(f"강타 저지 실패: {names[a['onFailure']]} 1기 소환(해당 개체가 없을 때). 이미 생존 중이면 "+(f"자기 HP {num(heal)} 회복." if heal>0 else '추가 소환하지 않음.'))
    if d['pattern']==1:out.append('같은 방향의 무기 직접 베기를 연속 적중시키면 반격.')
    if d['pattern']==2:
        out.append('무기 직접 타격을 5회 받을 때마다 다음 일반 공격·강타가 빨라짐.')
    if d['pattern']==3:out.append('본체 주위를 공전하며 공격.')
    if d['pattern']==4:
        if d['supportId']:
            out.append(('해 우상: 본체의 5연타 강타를 유지.' if d['supportId']=='left_heart' else '달 우상: 본체의 연결 강타를 유지.'))
        else:out.append('해/달 우상 둘 다 사라지면 본체 스턴. 각 우상이 담당하는 강타만 차단되며, 보조 개체가 필요 없는 강타는 남음.')
    if d['diesWithLeader']:out.append('비종속 본체가 모두 사망하면 함께 소멸.')
    return out

def notes(key):
    e=entries[key];old=previous[key];out=['외형·명칭 교체.']
    polish=folder.parent/'MonsterPolish-2026-10-26'
    if (polish/'Installed.txt').exists():
        out=['레퍼런스에 맞춰 과한 돌기·찢김과 표면 조각을 줄임.']
        if key=='CrystalUrchin':out=['이미지 수정 보류: 생성 도구 차단으로 기존 이미지를 유지.']
        prior={r['id']:r['name'] for r in json.loads((polish/'Roster-Before.json').read_text('utf-8-sig'))}
        if prior[key]!=e['data']['displayName']:out.append(prior[key]+' → '+e['data']['displayName']+'. 컨셉·이름만 변경; 전투 패턴 유지.')
    if key in changed:
        values=' / '.join(num(a['windup']) for a in old['newNormals'])
        out.append(f"임의 추가했던 {values}초 교대 공격 제거 → 원래 준비 {num(old['oldNormalWindup'])}초의 일반 공격으로 롤백.")
    if key=='IronMantis':out.append('기존 안내의 “보조 약점 세 개”와 달리 실제는 보조 1개에 강타 3회 필요.')
    if key in {'MirrorShell','CoffinSlime','BoneScorpion','StringRevenant'} and (folder.parent/'MonsterMood-2026-10-26/Installed.txt').exists():
        out.insert(0,'레퍼런스의 무드·선·명암을 다시 맞춘 최신 Idle 적용.')
    return out

def rank(key):
    cats=categories[key]
    if 'Boss' in cats:return 2
    if 'Elite' in cats:return 1
    if 'Combat' in cats:return 0
    return 3

child_only={'Threadling','BellHare','StormJelly','SunIdol','MoonIdol'}
order={e['id']:i for i,e in enumerate(export['monsters'])}
mains=sorted((k for k in entries if k not in child_only),key=lambda k:(rank(k),entries[k]['floor'] or 9,order[k]))
rows=[]
listed=set()
for key in mains:
    rows.append((key,None,rank(key)))
    listed.add(key)
    for child in companions[key]:
        # Only dedicated companions get their own row beneath the leader.
        # Independently appearing monsters retain one main entry.
        if child['child'] in child_only and child['child'] not in listed:
            rows.append((child['child'],{**child,'parent':key},rank(key)))
            listed.add(child['child'])
assert {k for k,_,_ in rows}==set(entries), 'Every monster must appear'
assert len(rows)==len(entries)==len(listed), 'Every monster must appear exactly once'
assert len(entries)==41 and all(not e['normalSequence'] for e in entries.values())

def kind(key,context):
    if context:return ('쫄 · 소환' if context['kind']=='召喚' else '쫄 · 동행')+f" {context['count']}기"
    if key=='IronMimic':return '특수 · 상자'
    if key=='GildedToad':return '특수 · 상인 변이'
    if key in {'TarrasqueWhelp','CrownColossus'}:return '특수 · 시련(이벤트)'
    return ['쫄','엘리트','보스','특수'][rank(key)]

groups=['쫄','엘리트','보스','특수 조우 · 시련']
parts=[];markdown=['# 몬스터 전체 목록','','| 층 | 종류 | 이전 컨셉 | 현재 컨셉 | 주요 효과 | 비고 |','|---|---|---|---|---|---|'];csvrows=[]
active=-1
for key,context,group in rows:
    e=entries[key];d=e['data'];old=previous[key];floor='공통' if not e['floor'] else f"B{e['floor']}"
    if group!=active:parts.append(f'<tr class="section" id="group-{group}"><th colspan="6">{groups[group]}</th></tr>');active=group
    current=effects(key,context);remark=notes(key);typ=kind(key,context)
    parent=f'<small>↳ {esc(names[context["parent"]])} 전용 동행</small>' if context else ''
    before=f'<img loading="lazy" src="Portraits/Before/{key}.png" alt="{esc(old["oldName"])}"><strong>{esc(old["oldName"])}</strong>'
    revision=hashlib.sha256((folder/'Portraits/Current'/f'{key}.png').read_bytes()).hexdigest()[:10]
    after=f'<img loading="lazy" src="Portraits/Current/{key}.png?v={revision}" alt="{esc(d["displayName"])}"><strong>{esc(d["displayName"])}</strong>{parent}'
    cls=' class="child"' if context else ''
    parts.append(f'<tr{cls} data-monster="{key}"><td>{floor}</td><td>{esc(typ)}</td><td class="concept">{before}</td><td class="concept">{after}</td><td class="effects">'+''.join('<p>'+esc(x)+'</p>' for x in current)+'</td><td>'+''.join('<p>'+esc(x)+'</p>' for x in remark)+'</td></tr>')
    imagebase=folder/'Portraits/Current'/f'{key}.png'
    mdcurrent=f'![{d["displayName"]}]({imagebase.as_posix()})<br>{d["displayName"]}'
    markdown.append('| '+' | '.join([floor,typ,old['oldName'],mdcurrent,'<br>'.join(current),'<br>'.join(remark)])+' |')
    csvrows.append([floor,typ,old['oldName'],d['displayName'],'\n'.join(current),'\n'.join(remark)])

page='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>몬스터 전체 목록 · 현재 효과</title><style>*{box-sizing:border-box}body{margin:0;background:#232720;color:#eee5ce;font:15px/1.55 system-ui,sans-serif}main{max-width:1740px;margin:auto;padding:20px}h1{font-size:25px;margin:8px 0 14px}nav{display:flex;flex-wrap:wrap;gap:16px;margin:12px 0 20px}a{color:#edc784}.scroll{overflow:auto}table{border-collapse:collapse;width:100%;table-layout:fixed;min-width:1120px;background:#30362c}th,td{border:1px solid #626f57;padding:10px;vertical-align:top;overflow-wrap:anywhere}thead th{background:#46513c;position:sticky;top:0;z-index:2;text-align:left}.section th{background:#475637;text-align:left;font-size:19px;padding:15px}.concept{text-align:center}.concept img{display:block;width:100%;height:145px;object-fit:contain;margin:0 auto 9px}.concept strong{display:block}p{margin:0 0 8px}small{display:block;color:#b9c4ae;margin-top:8px}.child td{background:#293228}.child td:first-child{border-left:5px solid #90a06d}.effects p:nth-child(2n){color:#d9ddce}footer{margin-top:22px;color:#c4ccba;font-size:14px}</style><main><h1>몬스터 전체 목록</h1><nav><a href="#group-0">쫄</a><a href="#group-1">엘리트</a><a href="#group-2">보스</a><a href="#group-3">특수 · 시련</a><a href="Comparison.csv">표 다운로드</a><a href="Rollback-Live.txt">원래 공격 시간 복원 확인</a></nav><div class="scroll"><table><colgroup><col style="width:4%"><col style="width:9%"><col style="width:12%"><col style="width:14%"><col style="width:44%"><col style="width:17%"></colgroup><thead><tr><th>층</th><th>종류</th><th>이전 컨셉</th><th>현재 컨셉</th><th>주요 효과</th><th>비고</th></tr></thead><tbody>'''+''.join(parts)+'''</tbody></table></div><footer><p>종류는 현재 조우 데이터의 일반/엘리트/보스 구분을 따릅니다. 시련은 이벤트이므로 임의로 엘리트나 보스로 분류하지 않았습니다. 동행·소환은 본체 바로 아래에 배치했으며, 여러 조우가 공유하는 몬스터는 관계마다 다시 표시했습니다. 동일 개체 여러 기는 하나의 행에 수량을 적었습니다.</p><p>주요 효과에는 연타·약점·흡혈·반격·소환·동행 등 실제 패턴만 적었습니다. 모든 강타는 필수 약점을 모두 부수면 중단되고 스턴됩니다. 체력·피해량·공격 시간·보상 등 세부 수치는 생략했습니다. 이전 이미지는 교체 직전 연결돼 있던 기본 자세, 현재 이미지는 실제 적용된 아트입니다.</p><p>강타의 openingSummons 설정은 조우에 자동 추가되는 것으로 과장하지 않았습니다. 실제 계획 조우는 명시된 동행 목록을 사용합니다. 표의 소환은 강타 저지 실패 시 실행되는 소환입니다.</p><a href="CurrentData.json">현재 데이터</a> · <a href="DataAudit.json">원본 대조</a> · <a href="Rollback.txt">롤백 기록</a></footer></main></html>'''
for i,label in enumerate(['쫄','엘리트','보스','특수 · 시련']):
    page=page.replace(f'<a href="#group-{i}">{label}</a>',f'<button type="button" onclick="document.getElementById(\'group-{i}\').scrollIntoView()">{label}</button>')
page=page.replace('동행·소환은 본체 바로 아래에 배치했으며, 여러 조우가 공유하는 몬스터는 관계마다 다시 표시했습니다. 동일 개체 여러 기는 하나의 행에 수량을 적었습니다.', '모든 몬스터는 고유하게 한 번만 표시합니다. 종토끼·실인형·폭풍 해파리·해 우상·달 우상처럼 본체에 딸린 전용 개체만 본체 바로 아래에 배치했습니다. 독립적으로도 등장하는 몬스터는 중복 행을 만들지 않고 동행·소환 관계를 효과 칸에 적었습니다. 동일 개체 여러 기는 하나의 행에 수량을 적었습니다.')
page=page.replace('</style>','button{background:none;border:0;padding:0;color:#edc784;font:inherit;text-decoration:underline;cursor:pointer}</style>')
page=page.replace('<nav>','<nav><a href="../MonsterPolish-2026-10-26/Gallery.html">아트 수정 전후·레퍼런스</a>')
page=page.replace('<nav>','<nav><a href="../MonsterMood-2026-10-26/Gallery.html">최근 네 종 재작업</a>')
page=page.replace('.scroll{overflow:auto}', '.scroll{overflow:visible}tr.section{scroll-margin-top:50px}@media(max-width:1120px){.scroll{overflow-x:auto}}')
(folder/'Gallery.html').write_text(page,encoding='utf-8')
(folder/'Comparison.md').write_text('\n'.join(markdown)+'\n',encoding='utf-8')
with (folder/'Comparison.csv').open('w',encoding='utf-8-sig',newline='') as f:
    writer=csv.writer(f);writer.writerow(['층','종류','이전 컨셉','현재 컨셉','주요 효과','비고']);writer.writerows(csvrows)

# Recompute the old scalar/attack invariant checks against the saved, rolled-back data.
audit=[]
for key,e in entries.items():
    before=(root/'Docs/Production/AngularWorld-2026-10-26/Before/Monsters'/(e['baseline']+'.asset')).read_text('utf-8-sig')
    fields=['maxHp','normalAttackInterval','chargeInterval','stunDuration','stunDamageMultiplier','experienceReward','goldReward','bodyHitRadius','pattern','orbitRadius','orbitSpeed']
    def same(field):
        value=float(re.search(r'^  '+field+r': (\S+)',before,re.M)[1]);return abs(value-e['data'][field])<.0002
    attack_same=all((root/a['path']).read_text('utf-8-sig').split('  damage:')[1]==(root/p).read_text('utf-8-sig').split('  damage:')[1] for a,p in zip(e['attacks'],previous[key]['oldAttacks']))
    item={'id':key,'previous':previous[key]['oldName'],'current':e['data']['displayName'],'unchangedStats':all(same(x) for x in fields),'originalAttackMechanics':attack_same,'normalSequence':e['normalSequence'],'normalTimingRollback':key in changed}
    assert item['unchangedStats'] and attack_same and not e['normalSequence'],key
    audit.append(item)
(folder/'DataAudit.json').write_text(json.dumps(audit,ensure_ascii=False,indent=2),encoding='utf-8')
(folder/'List-Check.json').write_text(json.dumps({'uniqueMonsters':len(entries),'visibleRows':len(rows),'duplicateMonsterRows':len(rows)-len({k for k,_,_ in rows}),'dedicatedCompanionsUnderLeader':[k for k,c,g in rows if c],'requestedColumns':['층','종류','이전 컨셉','현재 컨셉','주요 효과','비고'],'allListed':len({k for k,_,_ in rows})==41,'portraitsPerRow':2,'allNormalTimingMatchesBefore':True,'arbitraryAlternatingSequencesActive':0},ensure_ascii=False,indent=2),encoding='utf-8')
print('Created full table:',len(entries),'unique monsters,',len(rows),'rows; no duplicates.')
