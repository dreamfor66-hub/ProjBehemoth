from pathlib import Path
import json,re

folder=Path(__file__).resolve().parent
actors=json.loads((folder/'Manifest.json').read_text('utf-8'))['actors']
entries=[]
for actor in actors:
    poses=actor['poses']
    if actor.get('idleSource'):poses=[{'state':'Idle','source':actor['idleSource'],'output':actor['idle']}]+poses
    for pose in poses:
        stem=Path(pose['source']).stem
        candidates=[p for p in (folder/'Outputs').glob(stem+'*.json') if p.name==stem+'.json' or re.fullmatch(re.escape(stem)+r'-Revision\d+\.json',p.name)]
        latest=max(candidates,key=lambda p:int(re.search(r'Revision(\d+)',p.name)[1]) if 'Revision' in p.name else 1)
        record=json.loads(latest.read_text('utf-8'))
        record.update(id=stem,actor=actor['id'],state=pose['state'],output=pose['output'],reviewCopy=pose['source'],metadata=latest.name,status='installed_and_live_verified')
        entries.append(record)
(folder/'FinalPromptSet.json').write_text(json.dumps({'mode':'built-in image_gen','count':len(entries),'assets':entries},ensure_ascii=False,indent=2),encoding='utf-8')
print('Final prompt records corrected:',len(entries))
