from pathlib import Path
import json, re, yaml
ROOT = Path(__file__).resolve().parents[3]
ASSETS = ROOT / 'Assets'
OUT = Path(__file__).parent
def read(path):
    text = path.read_text(encoding='utf-8-sig')
    text = re.sub(r'^%.*\n', '', text, flags=re.M)
    text = re.sub(r'^--- !u!\d+ &\d+.*$', '---', text, flags=re.M)
    return yaml.safe_load(text)['MonoBehaviour']
def guid(path):
    return re.search(r'^guid: (.+)$', path.with_suffix(path.suffix+'.meta').read_text(), re.M)[1]
if __name__ == '__main__':
    objects = {guid(p):p.stem for p in (ASSETS/'Data/Presentation/Exploration').glob('*.asset')}
    events=[]
    for p in sorted((ASSETS/'Data/Dungeon/Events').glob('*.asset')):
        event=read(p)
        for key in ('textKeys', 'illustration'): event.pop(key,None)
        event['path']=p.relative_to(ROOT).as_posix()
        event['worldObjectName']=objects.get(event.get('worldObject',{}).get('guid'),'NONE')
        events.append(event)
    (OUT/'BeforeEvents.json').write_text(json.dumps(events,ensure_ascii=False,indent=2),encoding='utf-8')
    for e in events:
        print('\n'+e['id']+' | '+e['displayName']+' | '+e['worldObjectName']+' | excluded='+str(e.get('excludedFromRandomPool')))
        for i,p in enumerate(e.get('pages',[])):
            print(str(i)+': '+p['narrative'].replace('\r\n',' / ').replace('\n',' / '))
            for j,o in enumerate(p.get('options',[])):
                print('  '+str(j)+' '+o['title']+' ->'+str(o.get('nextPage'))+' '+(o.get('resultText') or '').replace('\n',' / '))
