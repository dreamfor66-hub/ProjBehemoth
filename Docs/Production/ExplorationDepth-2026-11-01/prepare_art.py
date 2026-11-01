"""Mechanical PNG installation/crops only; artwork is made by built-in image_gen."""
import json, pathlib, re, shutil
from PIL import Image,ImageOps
folder=pathlib.Path(__file__).resolve().parent
project=folder.parents[2]
assets=project/'Assets'
prompts=folder/'Prompts'
metas={}
for path in (assets/'Art').rglob('*.meta'):
    match=re.search(r'^guid: (\w+)',path.read_text(encoding='utf-8-sig'),re.M)
    if match:metas[match[1]]=path.with_suffix('')
def source(name):
    name='Hound-Idle' if name=='houndIdle' else name
    result=pathlib.Path(json.loads((prompts/(name+'.json')).read_text(encoding='utf-8'))['source'])
    if not result.is_absolute():result=pathlib.Path('C:/Users/user/.codex/generated_images/01a190b9-78ed-7b11-a7c7-21a97973500f')/result
    return result
def install(name,target):
    target=project/target
    if target.exists():
        backup=folder/'Before'/target.relative_to(project)
        if not backup.exists():backup.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(target,backup)
    target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source(name),target)
    return str(target.relative_to(project)).replace('\\','/')
def old_painting(name,field,default):
    text=(assets/'Data/Presentation/Exploration'/('Data_RoomObject_'+name+'.asset')).read_text(encoding='utf-8-sig')
    match=re.search(r'^  '+field+r': .*guid: (\w+)',text,re.M)
    return str(metas[match[1]].relative_to(project)).replace('\\','/') if match else default
def obj(name,variants,height,wall=False,door=False,frame=False,face=False):
    result=dict(name=name,height=height,wall=wall,door=door,frame=frame,face=face)
    for field,src in variants.items():
        default='Assets/Art/World/Exploration/T_UI_World_'+name+'_'+field+'.png'
        existing=assets/'Data/Presentation/Exploration'/('Data_RoomObject_'+name+'.asset')
        target=old_painting(name,field,default) if existing.exists() else default
        result[field]=install(src,target)
    return result
objects=[
    obj('다친강아지',dict(front='houndFront',side='houndSide',rear='houndRear'),185,face=True),
    obj('길위의노마법사',dict(front='mageFront',side='mageSide',rear='mageRear'),400,face=True),
    obj('분수',dict(front='fountainFront',side='fountainSide',rear='fountainRear',usedFront='fountainUsedFront',usedSide='fountainUsedSide2',usedRear='fountainUsedRear'),260),
    obj('엘리트입구',dict(front='eliteFrame',side='eliteFrame',rear='eliteFrame'),400,wall=True,door=True,frame=True),
    obj('보스입구',dict(front='bossFrame',side='bossFrame',rear='bossFrame'),400,wall=True,door=True,frame=True),
    obj('회수대의쇠덫',dict(front='trapFront',side='trapSide',rear='trapRear',usedFront='trapUsedFront',usedSide='trapUsedSide2',usedRear='trapUsedRear2'),115)
]
for name,front,side,height in [('예약받는짐꾼','porterFront','porterSide',400),('금니의도박꾼','brokerFront','brokerSide',350),('번개병장수','bottleFront','bottleSide',400)]:
    entry=obj(name,dict(front=front,side=side),height,face=True)
    entry['rear']=old_painting(name,'rear','')
    objects.append(entry)
idle=install('houndIdle','Assets/Art/Combat/Lore01/T_Sprite_Character_길동무_Idle.png')
attack=install('houndAttack','Assets/Art/Combat/Lore01/T_Sprite_Character_길동무_Attack.png')
opposite_idle=install('houndOppositeIdle','Assets/Art/Combat/Lore01/T_Sprite_Character_회수견_좌향_Idle.png')
opposite_attack=install('houndOppositeAttack','Assets/Art/Combat/Lore01/T_Sprite_Character_회수견_좌향_Attack.png')
def portrait(src,box,target):
    im=Image.open(source(src)).convert('RGBA').crop(box)
    im=ImageOps.contain(im,(512,512));out=Image.new('RGBA',(512,512));out.alpha_composite(im,((512-im.width)//2,(512-im.height)//2))
    path=project/target;path.parent.mkdir(parents=True,exist_ok=True);out.save(path);return target
dog_icon=portrait('houndIdle',(980,0,1510,530),'Assets/Art/UI/Icons/T_UI_Icon_회수견.png')
trap_icon=portrait('trapFront',(620,150,1150,680),'Assets/Art/UI/Icons/T_UI_Icon_반격태엽.png')
(folder/'ArtReady.json').write_text(json.dumps(dict(objects=objects,dogIdle=idle,dogAttack=attack,dogOppositeIdle=opposite_idle,dogOppositeAttack=opposite_attack,dogIcon=dog_icon,trapIcon=trap_icon),ensure_ascii=False,indent=2),encoding='utf-8')
