"""Mechanical file installation only. Pixels/alpha made by built-in imagegen."""
import json,re,shutil,os
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).parent
PROJECT=ROOT.parents[2]
ASSETS=PROJECT/'Assets'
prompts=ROOT/'Prompts'
metas={}
for m in (ASSETS/'Art').rglob('*.meta'):
    match=re.search(r'^guid: (\w+)',m.read_text(encoding='utf-8-sig'),re.M)
    if match:metas[match.group(1)]=m.with_suffix('')

def load(name):return Path(json.loads((prompts/(name+'.json')).read_text(encoding='utf-8-sig'))['source'])
def install(name,target):
    target=PROJECT/target
    if target.exists():
        old=ROOT/'BeforeAssets'/target.relative_to(PROJECT)
        if not old.exists():old.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(target,old)
    target.parent.mkdir(parents=True,exist_ok=True)
    source=load(name)
    if not target.exists() or target.read_bytes()!=source.read_bytes():
        staged=target.with_suffix(target.suffix+'.staging')
        shutil.copy2(source,staged)
        os.replace(staged,target)
    im=Image.open(target)
    if im.mode!='RGBA' or im.getchannel('A').getextrema()[0]!=0:raise ValueError('Not transparent: '+str(target))
    return target.relative_to(PROJECT).as_posix()

def painting(object_name,field,name):
    path=ASSETS/'Data/Presentation/Exploration'/('Data_RoomObject_'+object_name+'.asset')
    guid=re.search(r'^  '+field+r': .*guid: (\w+)',path.read_text(encoding='utf-8-sig'),re.M) if path.exists() else None
    target=metas[guid[1]].relative_to(PROJECT).as_posix() if guid else None
    if target is None and field.startswith('used') and path.exists():
        front=re.search(r'^  front: .*guid: (\w+)',path.read_text(encoding='utf-8-sig'),re.M)
        if front:
            original=metas[front[1]]
            target=original.with_name(original.stem.rsplit('_',1)[0]+'_'+field[0].upper()+field[1:]+'.png').relative_to(PROJECT).as_posix()
    if target is None:target='Assets/Art/World/Exploration/T_UI_World_'+object_name+'_'+field+'.png'
    return install(name,target)

def obj(name,height,fields,wall=False):
    return dict(name=name,height=height,wall=wall,**{field:painting(name,field,source) for field,source in fields.items()})

objects=[
    obj('저주받은금고',290,dict(front='safePitFront',side='safePitSide',rear='safePitRear')),
    obj('유리덮개',400,dict(front='fruitFront',side='fruitSide',rear='fruitRear',usedFront='fruitUsedFront',usedSide='fruitUsedSide',usedRear='fruitUsedRear'),wall=True),
    obj('떠있는책',300,dict(front='bookFront',side='bookSide',rear='bookRear',usedFront='bookUsedFront',usedSide='bookUsedSide',usedRear='bookUsedRear')),
    obj('표식붙은두꾸러미',180,dict(front='deliveryFront',side='deliverySide',rear='deliveryRear',usedFront='deliveryUsedFront',usedSide='deliveryUsedSide',usedRear='deliveryUsedRear')),
    obj('급료고의빈배수구',0,dict(front='pitFront',side='pitSide',rear='pitRear')),
]
seed=install('seedIcon','Assets/Art/UI/Icons/T_UI_Icon_혈근의씨.png')
(ROOT/'ArtReady.json').write_text(json.dumps(dict(objects=objects,seedIcon=seed),ensure_ascii=False,indent=2),encoding='utf-8')
rows=['# Saved production PNGs','','Made with built-in imagegen; independent editable PNGs. Existing PNG GUID/meta files preserved.','','|Data subject|Field|Production PNG|','|---|---|---|']
for o in objects:
    for key,path in o.items():
        if key in ['front','side','rear','usedFront','usedSide','usedRear']:rows.append('|'+o['name']+'|'+key+'|'+path+'|')
rows+=['','Relic icon: '+seed,'','Source/prompt: Prompts/*.json. BeforeAssets contains pre-task overwritten files. No artistic pixel modifications or procedural shading were used.']
(ROOT/'SavedAssets.md').write_text('\n'.join(rows),encoding='utf-8')
print('Installed',sum(sum(k in o for k in ['front','side','rear','usedFront','usedSide','usedRear']) for o in objects)+1,'editable PNGs; wrote ArtReady.json')
