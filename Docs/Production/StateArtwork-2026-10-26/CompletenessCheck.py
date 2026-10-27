from pathlib import Path
import json,re,hashlib,collections,html
from PIL import Image

folder=Path(__file__).resolve().parent
root=folder.parents[2]
actors=json.loads((folder/'Manifest.json').read_text('utf-8'))['actors']
index={}
for branch in ['Data','Art']:
    for meta in (root/'Assets'/branch).rglob('*.meta'):
        m=re.search(r'^guid: (\w+)',meta.read_text('utf-8-sig',errors='replace'),re.M)
        if m:index[m[1]]=meta.with_suffix('')
def read(path):return path.read_text('utf-8-sig')
def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest().upper()
def binding(text,key):
    m=re.search(r'^  '+key+r': .*?guid: (\w+)',text,re.M)
    return index.get(m[1]) if m else None
def rel(path):return path.relative_to(root).as_posix() if path else None

problems=[];checked=[];expected_images=set()
spec={'Player':['Idle','Attack','Guard'],'Monster':['Idle','Ready','Attack','Dead'],'Companion':['Idle','Attack']}
for actor in actors:
    failures=[];data=read(root/actor['dataPath']);profile=root/actor['animationPath'];animation=read(profile)
    if binding(data,'combatAnimation')!=profile:failures.append('Data points to a different animation profile')
    expected={'Idle':actor['idle'],**{p['state']:p['output'] for p in actor['poses']}}
    actual={state:binding(animation,state.lower()) for state in ['Idle','Ready','Attack','Guard','Dead']}
    for state in spec[actor['kind']]:
        output=root/expected[state];expected_images.add(output)
        if actual[state]!=output:failures.append(state+' slot points to a different file')
        if not output.exists():failures.append(state+' deployed PNG missing');continue
        im=Image.open(output);im.verify()
        if state=='Idle':
            if digest(output)!=actor['idleHash']:failures.append('Deployed Idle hash changed')
        else:
            source=root/next(p['source'] for p in actor['poses'] if p['state']==state)
            if digest(source)!=digest(output):failures.append(state+' source/deployed bytes differ')
    for state in set(actual)-set(spec[actor['kind']]):
        if actual[state] is not None:failures.append('Disallowed state still bound: '+state)
    base=Image.open(root/actor['idle']);ratio=base.width/base.height
    for pose in actor['poses']:
        im=Image.open(root/pose['output'])
        if 'A' not in im.getbands() or im.getchannel('A').getextrema()[0]!=0:failures.append(pose['state']+' missing transparent alpha')
        if abs(im.width/im.height-ratio)>.005:failures.append(pose['state']+' canvas aspect changed')
    revision='|'.join('-'.join(re.findall('..',digest(root/p['source']))) for p in actor['poses'])
    for stage in ['Approved','Installed','Verified']:
        path=folder/stage/(actor['id']+'.txt')
        if not path.exists() or read(path).strip()!=revision:failures.append(stage+' record missing or stale')
    lines=[]
    for path in sorted(folder.glob('Live-*.txt'),key=lambda p:p.stat().st_mtime_ns):
        if path.name=='Live-Complete.txt':continue
        relevant=[s for s in read(path).splitlines() if (' '+actor['id']+' ') in s]
        if relevant:lines=relevant
    if not lines or any(s.startswith('FAIL ') for s in lines):failures.append('Latest per-actor runtime log missing or failing')
    if failures:problems.append({'id':actor['id'],'problems':failures})
    checked.append({'id':actor['id'],'stateCount':len(spec[actor['kind']]),'latestRuntimeChecks':len(lines),'problems':failures})

manifest_paths={a['dataPath'] for a in actors}
outside=[];monsters=[]
type_guids={}
for kind in ['Player','Companion']:
    sample=next(a for a in actors if a['kind']==kind)
    type_guids[kind]=re.search(r'm_Script: .*guid: (\w+)',read(root/sample['dataPath']))[1]
inventories={kind:[] for kind in type_guids}
for path in (root/'Assets/Data').rglob('*.asset'):
    text=read(path)
    for kind,guid in type_guids.items():
        if 'm_Script: {fileID: 11500000, guid: '+guid+',' in text:inventories[kind].append(rel(path))
    if 'm_Script: {fileID: 11500000, guid: b9f0f94ec7d31d445aac87fc5f063460,' not in text:continue
    monsters.append(path)
    if rel(path) in manifest_paths:continue
    profile=binding(text,'combatAnimation');states={s:binding(read(profile),s.lower()) for s in ['Idle','Ready','Attack','Dead']} if profile else {}
    owners=[]
    guid=re.search(r'^guid: (\w+)',read(Path(str(path)+'.meta')),re.M)[1]
    for encounter in (root/'Assets/Data/Dungeon/Encounters').glob('*.asset'):
        if guid in read(encounter):owners.append(rel(encounter))
    outside.append({'data':rel(path),'animation':rel(profile),'states':{k:rel(v) for k,v in states.items()},'stateContractValid':len(states)==4 and len(set(states.values()))==4 and all(p and p.exists() for p in states.values()),'encounters':owners})

idle_baseline=json.loads((folder/'Idle-Hashes.json').read_text('utf-8'))
changed_idle=[p for p,h in idle_baseline.items() if digest(root/p)!=h.upper()]
missing_gallery=[]
gallery=read(folder/'Gallery.html')
for src in re.findall(r'<img[^>]+src="([^"]+)"',gallery):
    if not (folder/html.unescape(src.split('?',1)[0])).exists():missing_gallery.append(src)
resolved_errors=[]
for path in (folder/'Outputs').glob('*-Error.json'):
    stem=path.name.removesuffix('-Error.json')
    resolved_errors.append({'job':stem,'currentOutputExists':(folder/'Outputs'/(stem+'.png')).exists()})
result={'actorsInStateMigration':len(actors),'monstersInCurrentProject':len(monsters),'otherCurrentInventories':inventories,'deployedStateSlots':sum(len(spec[a['kind']]) for a in actors),'uniqueDeployedImages':len(expected_images),'problems':problems,'changedApprovedIdles':changed_idle,'missingGalleryImages':missing_gallery,'actorsOutsideMigration':outside,'historicalGenerationErrors':resolved_errors,'actors':checked}
(folder/'CompletenessCheck.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k!='actors'},ensure_ascii=False,indent=2))
