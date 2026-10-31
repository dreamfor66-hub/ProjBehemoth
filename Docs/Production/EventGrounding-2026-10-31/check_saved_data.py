from pathlib import Path
import json
from audit_events import read, guid, ASSETS, OUT
before=json.loads((OUT/'BeforeEvents.json').read_text(encoding='utf-8'))
plan=json.loads((OUT/'Plan.json').read_text(encoding='utf-8'))['events']
mechanics=['goldCost','hpCost','maxHpCost','consumeHealingPotion','requiredPower','requiredRelic','requiredPerk','requiredFlag','supply','supplyCost','nextPage','failurePage','requirements','effects']
def choices(e):
    def normalized(o):
        result={k:o.get(k) for k in mechanics}
        result['effects']=[dict({'spellPool':[],'spellChoiceCount':3},**effect) for effect in (o.get('effects') or [])]
        return result
    return [[normalized(o) for o in e['options']]]+[[normalized(o) for o in p['options']] for p in e.get('pages',[])]
refs={guid(p):p for p in (ASSETS/'Data/Presentation/Exploration').glob('*.asset')}
checks=[];active=[];fronts=[]
current={read(p)['id']:read(p) for p in (ASSETS/'Data/Dungeon/Events').glob('*.asset')}
for old in before:
    data=current[old['id']]
    authored=next(p for p in plan if p['id']==data['id'])
    checks.append((data['displayName']==authored['displayName'],data['id']+' authored display name installed'))
    for pg in authored['pages']:
        override=next((f['value'] for f in authored['fields'] if f['path']==f"pages.Array.data[{pg['index']}].narrative"),pg['narrative'])
        checks.append((data['pages'][pg['index']]['narrative']==override,data['id']+f" page {pg['index']} authored narrative installed"))
    for terminal in authored['terminals']:
        option=(data['options'] if terminal['page']<0 else data['pages'][terminal['page']]['options'])[terminal['index']]
        checks.append((option['resultText']==terminal['text'] and bool(option.get('removeWorldObject'))==terminal['remove'] and bool(option.get('useWorldObject'))==terminal['used'],data['id']+f" terminal {terminal['page']}/{terminal['index']} matches actual authored aftermath"))
    checks.append((choices(old)==choices(data),old['id']+' authored costs, conditions, effects, branching unchanged'))
    for key in ['weight','minAct','maxAct','minNormalCombatsInAct','requiredFlag','forbiddenFlag','excludedFromRandomPool','entryRoutes']:
        checks.append((old.get(key)==data.get(key),old['id']+' '+key+' preserved'))
    if not data['excludedFromRandomPool']:
        obj=refs[data['worldObject']['guid']];art=read(obj);active.append(data['worldObject']['guid']);fronts.append(art['front']['guid'])
        checks.append((art.get('facePlayerOnFirstEncounter',0)==next(p['npc'] for p in plan if p['id']==data['id']),data['id']+' first-facing policy'))
        text=' '.join([data['narrative']]+[p['narrative'] for p in data.get('pages',[])])
        forbidden=['연금술실','연구실','강의실','훈련장에','검문소','지하 우물방','운반 계단','승강 장치','서고에','기록실 책상','의식실 바닥']
        checks.append((not any(t in text for t in forbidden),data['id']+' no unrepresented special-room premise'))
        options=data['options']+[o for p in data.get('pages',[]) for o in p['options']]
        checks.append((all(o.get('revisitTextKey') for o in options if o.get('nextPage',-1)<0),data['id']+' every terminal has revisit response'))
checks += [(len(active)==len(set(active))==19,'19 unique active subject data'),(len(fronts)==len(set(fronts))==19,'19 unique active front paintings')]
csv=(ASSETS/'Data/Localization/kr.csv').read_text(encoding='utf-8-sig')
checks.append(('길 위의 노마법사' in csv and '주인 잃은 깃발' in csv,'rewritten Korean CSV installed'))
report='\n'.join(('PASS ' if ok else 'FAIL ')+message for ok,message in checks)
report+=f'\n{len(checks)} checks; {sum(not ok for ok,_ in checks)} failures.\n'
(OUT/'SavedDataAudit.txt').write_text(report,encoding='utf-8')
print(report[-1200:])
