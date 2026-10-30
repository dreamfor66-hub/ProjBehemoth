from pathlib import Path
import json,re,math
folder=Path('Docs/Production/MonsterAuthoring-2026-10-30')
records={}
for row in json.loads((folder/'Baseline.json').read_text(encoding='utf-8-sig'))['monsters']:
 guid=re.search(r'^guid: (\w+)',Path(row['path']+'.meta').read_text(encoding='utf-8-sig'),re.M)[1]
 fields=json.loads(row['json'])['MonoBehaviour']
 records[guid]=(row['oldHalfHeight'],min(c['center']['y']-c['radius'] for c in row['hitBoxes']),fields['combatScale'])
def members(s):
 return [(m[0],float(m[1]),float(m[2]),float(m[3])) for m in re.findall(r'- monster: \{fileID: \d+, guid: (\w+), type: \d+\}\s+offset: \{x: ([^,]+), y: ([^}]+)\}\s+(?:scale: [^\n]+\s+)?actionDelay: ([^\n]+)',s)]
checks=failures=0;log=[]
for backup in (folder/'Before/Assets/Data').rglob('*.asset'):
 before=backup.read_text(encoding='utf-8-sig')
 if '\n  members:' not in before:continue
 path=Path(str(backup.relative_to(folder/'Before')))
 old,new=members(before),members(path.read_text(encoding='utf-8-sig'))
 assert len(old)==len(new),(path,len(old),len(new))
 for a,b in zip(old,new):
  half,bottom,scale=records.get(a[0],(0,0,1))
  # The authored physical footprint moves; the actor's actual root does not.
  ok=a[0]==b[0] and abs(a[1]-b[1])<.001 and abs(a[3]-b[3])<.001 and abs((a[2]+half*scale)-(b[2]-bottom*scale))<.001
  checks+=1;failures+=not ok;log.append(('PASS ' if ok else 'FAIL ')+str(path)+' same actor origin and action delay')
log.append(f'RESULT {checks} encounter members, {failures} failures')
(folder/'EncounterVerification.txt').write_text('\n'.join(log)+'\n')
print(log[-1]);assert failures==0
