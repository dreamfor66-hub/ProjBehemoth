"""Mechanical frame isolation/padding only. No painted pixel recolor, resampling or relighting."""
from pathlib import Path
import json, shutil
import cv2
import numpy as np
from PIL import Image

OUT=Path(__file__).parent
ROOT=OUT.parents[2]
WORLD=ROOT/'Assets/Art/World/Exploration'
COMBAT=ROOT/'Assets/Art/Combat/Lore01'
COMBAT.mkdir(parents=True,exist_ok=True)
entries=[]
for f in (OUT/'Prompts').glob('*.json'):
    e=json.loads(f.read_text(encoding='utf-8'));e['manifest']=f.name
    if e.get('accepted',True):entries.append(e)

def save(image,path):
    path.parent.mkdir(parents=True,exist_ok=True)
    if path.exists():
        before=OUT/'Before'/path.relative_to(ROOT)
        if not before.exists():before.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(path,before)
    image.save(path)
    return path.relative_to(ROOT).as_posix()

def frames(e,cols,rows):
    src=Image.open(e['source']).convert('RGBA');rgba=np.asarray(src);alpha=rgba[:,:,3]
    _,labels,stats,centres=cv2.connectedComponentsWithStats((alpha>32).astype('uint8'))
    major=[]
    for row in range(rows):
        for col in range(cols):
            candidates=[i for i in range(1,len(stats)) if min(cols-1,int(centres[i,0]/src.width*cols))==col and min(rows-1,int(centres[i,1]/src.height*rows))==row]
            major.append(max(candidates,key=lambda i:stats[i,4]))
    # Detached sword/satchel parts follow the nearest principal figure, not a bounding-box crop.
    groups={i:min(range(len(major)),key=lambda j:np.linalg.norm((centres[i]-centres[major[j]])/np.array([src.width/cols,src.height/rows]))) for i in range(1,len(stats))}
    _,nearest=cv2.distanceTransformWithLabels((alpha<=32).astype('uint8'),cv2.DIST_L2,5,labelType=cv2.DIST_LABEL_CCOMP)
    owners=np.zeros(int(nearest.max())+1,dtype='uint8')
    for i,group in groups.items():owners[np.unique(nearest[labels==i])]=group+1
    result=[]
    for index in range(cols*rows):
        core=np.isin(labels,[i for i,g in groups.items() if g==index]);ys,xs=np.where(core)
        box=(max(0,int(xs.min())-10),max(0,int(ys.min())-10),min(src.width,int(xs.max())+11),min(src.height,int(ys.max())+11))
        pixels=rgba.copy();pixels[:,:,3]=np.where(owners[nearest]==index+1,alpha,0)
        result.append(Image.fromarray(pixels).crop(box))
    return result

def combat_canvas(im):
    # A fixed transparent, centred square per PNG; UI framing uses the authored painting,
    # never a pose-dependent keyed transform. Source painted pixels stay unchanged.
    size=int(max(im.size)*1.10)+2
    out=Image.new('RGBA',(size,size),(0,0,0,0));out.paste(im,((size-im.width)//2,size-im.height-12))
    return out

art=[];interiors=[];doors=[];combat=[];companion=None
for e in entries:
    name=e.get('installName',e['name']);kind=e['kind']
    if kind=='directions':
        views=frames(e,3,1);files={}
        for view,im in zip(['front','side','rear'],views):files[view]=save(im,WORLD/f'T_Sprite_Character_{name}_{view.title()}.png')
        art.append(dict(name=name,height=340 if name=='버려진호위용병' else 400,**files))
    elif kind=='door':
        path=save(Image.open(e['source']),WORLD/f'T_UI_World_{name}.png');doors.append(dict(name=name,path=path))
    elif kind in ['interior','eventInterior']:
        path=save(Image.open(e['source']),WORLD/f'T_UI_Room_{e["name"]}.png')
        interiors.append(dict(act=e['act'],event=e.get('event',''),path=path))
    elif kind=='combatDirections':
        views=frames(e,3,2)
        files={}
        for view,im in zip(['front','side','rear'],views[:3]):files[view]=save(im,WORLD/f'T_Sprite_Monster_{name}_{view.title()}.png')
        dead=save(views[5],WORLD/f'T_Sprite_Monster_{name}_Dead.png')
        art.append(dict(name=name,height=400,usedFront=dead,usedSide=dead,usedRear=dead,**files))
        poses={}
        for pose,im in zip(['Idle','Ready','Attack','Dead'],[views[0],views[3],views[4],views[5]]):poses[pose.lower()]=save(combat_canvas(im),COMBAT/f'T_Sprite_Monster_{name}_{pose}.png')
        combat.append(dict(name=name,**poses))
    elif kind=='combat4':
        poses={}
        for pose,im in zip(['Idle','Ready','Attack','Dead'],frames(e,2,2)):poses[pose.lower()]=save(combat_canvas(im),COMBAT/f'T_Sprite_Monster_{name}_{pose}.png')
        combat.append(dict(name=name,**poses))
    elif kind=='companion':
        companion={}
        for pose,im in zip(['Idle','Attack'],frames(e,2,1)):companion[pose.lower()]=save(combat_canvas(im),COMBAT/f'T_Sprite_Character_길동무_{pose}.png')
# Single-pose corrections always replace only their selected state after sheet extraction.
for e in entries:
    if e['kind']=='combatSingle':
        im=Image.open(e['source']).convert('RGBA');alpha=np.asarray(im.getchannel('A'));ys,xs=np.where(alpha>32)
        im=im.crop((max(0,int(xs.min())-10),max(0,int(ys.min())-10),min(im.width,int(xs.max())+11),min(im.height,int(ys.max())+11)))
        name=e.get('installName',e['name']);pose=e['state'];path=save(combat_canvas(im),COMBAT/f'T_Sprite_Monster_{name}_{pose}.png')
        next(c for c in combat if c['name']==name)[pose.lower()]=path
data=dict(art=art,interiors=interiors,doors=doors,combat=combat,companion=companion)
(OUT/'ArtReady.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
print('Saved',len(art),'world subjects,',len(interiors),'interiors,',len(doors),'entrances,',len(combat),'combat identities, companion poses.')
