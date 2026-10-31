from pathlib import Path
import json,re,ast
root=Path(__file__).resolve().parents[3]
folder=Path(__file__).resolve().parent
index=json.loads((folder/'DataNames.json').read_text(encoding='utf-8'))['entries']
counts={'guids':0,'ids':0,'monsterCombatFields':0,'attackCombatFields':0,'weakPointRequirements':0}
fail=[]
def check(ok,label):
 if not ok:fail.append(label)
def field(text,key):
 m=re.search(r'^  '+re.escape(key)+r': (.*)$',text,re.M)
 return m[1] if m else None
for row in index:
 current=root/row['path'];before=folder/'Before'/row['oldPath']
 check(current.exists(),row['path']+' exists')
 if not before.exists():fail.append('No before baseline '+row['oldPath']);continue
 now=current.read_text(encoding='utf-8-sig');old=before.read_text(encoding='utf-8-sig')
 check(re.search(r'^guid: (.*)$',Path(str(current)+'.meta').read_text(),re.M)[1]==row['guid'],row['path']+' GUID');counts['guids']+=1
 check(field(old,'id')==field(now,'id'),row['path']+' stable id');counts['ids']+=1
 old_keys=re.findall(r'^    key: (.*)$',old,re.M);new_keys=re.findall(r'^    key: (.*)$',now,re.M)
 check(old_keys==new_keys,row['path']+' stable localization keys')
 if row['type']=='MonsterData':
  for key in ['maxHp','normalAttackInterval','chargeInterval','stunDuration','stunDamageMultiplier','experienceReward','goldReward','useOrbit','orbitRadius','orbitSpeed','supportId','diesWithLeader']:
   check(field(old,key)==field(now,key),row['path']+' preserved '+key);counts['monsterCombatFields']+=1
 if row['type']=='MonsterAttackData':
  for key in ['damage','shieldDamage','windupDuration','recoveryDuration','guardable','chargeTimeLimit','requiredSupportId']:
   check(field(old,key)==field(now,key),row['path']+' preserved '+key);counts['attackCombatFields']+=1
  for key in ['hitRadius','requiredChargeHits','optional']:
   check(re.findall(r'^    '+key+r': (.*)$',old,re.M)==re.findall(r'^    '+key+r': (.*)$',now,re.M),row['path']+' preserved '+key);counts['weakPointRequirements']+=1
  check(old[old.index('  combatMechanics:'):]==now[now.index('  combatMechanics:'):],row['path']+' preserved all attack mechanics')
report={'counts':counts,'failures':fail,'assetCount':len(index)}
(folder/'DataPreservation.json').write_text(json.dumps(report,indent=2,ensure_ascii=False),encoding='utf-8')
print(json.dumps({'assetCount':len(index),'checks':counts,'failures':len(fail)},ensure_ascii=False))
if fail:print('\n'.join(fail[:12]))
