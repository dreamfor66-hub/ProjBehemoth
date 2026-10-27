from pathlib import Path
import json,hashlib,re

folder=Path(__file__).resolve().parent
project=folder.parents[2]
actors=json.loads((folder/'Manifest.json').read_text('utf-8'))['actors']
results=[]
for a in actors:
    current=(project/a['animationPath']).read_text('utf-8-sig')
    role={'Player':['idle','attack','guard'],'Monster':['idle','ready','attack','dead'],'Companion':['idle','attack']}[a['kind']]
    refs={k:re.search(r'^  '+k+r': (.+)$',current,re.M).group(1) for k in ['idle','attack','guard','ready','dead']}
    complete=all(refs[k]!='{fileID: 0}' for k in role) and len({refs[k] for k in role})==len(role) and all(v=='{fileID: 0}' for k,v in refs.items() if k not in role)
    idle_ok=hashlib.sha256((project/(a.get('idleSource') or a['idle'])).read_bytes()).hexdigest().upper()==a['idleHash']
    before=(folder/'Before'/a['dataPath']).read_text('utf-8-sig')
    after=(project/a['dataPath']).read_text('utf-8-sig')
    strip=lambda t:re.sub(r'^  (combatAnimation|combatSprite): .*\n','',t,flags=re.M).replace('\r','')
    data_ok=strip(before)==strip(after)
    before_profile=(folder/'Before'/a['animationPath']).read_text('utf-8-sig')
    preserved_fields=['canvasScale','combatPositionOffset','labelHeight']
    def field(t,key):
        found=re.search(r'^  '+key+r': (.*)$',t,re.M)
        defaults={'canvasScale':'1','combatPositionOffset':'{x: 0, y: 0}','labelHeight':'116'}
        return found.group(1) if found else defaults.get(key)
    layout_ok=all(field(current,k)==field(before_profile,k) for k in preserved_fields)
    installed=(folder/'Installed'/(a['id']+'.txt'))
    verified=(folder/'Verified'/(a['id']+'.txt'))
    verified_ok=installed.exists() and verified.exists() and installed.read_text()==verified.read_text()
    results.append({'id':a['id'],'exactStateContract':complete,'idleUnchanged':idle_ok,'gameplayDataUnchanged':data_ok,'layoutUnchanged':layout_ok,'separatePaintedStates':field(current,'illustratedMotion')=='0' and field(current,'illustratedLayout')=='1','liveVerified':verified_ok})
(folder/'Audit.json').write_text(json.dumps(results,indent=2),encoding='utf-8')
print(json.dumps({'actors':len(results),'passedAll':sum(all(v for k,v in r.items() if k!='id') for r in results),'pending':[r for r in results if not all(v for k,v in r.items() if k!='id')]},indent=2))
