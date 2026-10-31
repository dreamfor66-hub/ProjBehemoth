"""Lossless sprite-frame extraction only: no repaint, relight, resize or alpha modification."""
from pathlib import Path
import json
from PIL import Image
import numpy as np
import cv2
OUT=Path(__file__).parent
ROOT=OUT.parents[2]
DEST=ROOT/'Assets/Art/World/Exploration'
manifest=json.loads((OUT/'GeneratedSheets.json').read_text(encoding='utf-8'))
art=[]
for entry in manifest:
    source=Path(entry['source']); image=Image.open(source).convert('RGBA')
    alpha=np.array(image.getchannel('A'))
    _,components,stats,centroids=cv2.connectedComponentsWithStats((alpha>128).astype('uint8'))
    rows=entry.get('rows',1);count=3*rows
    # Some silhouettes have overlapping rectangular bounds although no painted pixels touch.
    # Extract the three connected sprite frames without clipping boots or repainting pixels.
    _,nearest=cv2.distanceTransformWithLabels((alpha<=128).astype('uint8'),cv2.DIST_L2,5,labelType=cv2.DIST_LABEL_CCOMP)
    ownership=np.zeros(int(nearest.max())+1,dtype='uint8')
    for comp in range(1,len(stats)):
        ys,xs=np.where(components==comp)
        target=min(2,int(centroids[comp,0]/image.width*3))+3*min(rows-1,int(centroids[comp,1]/image.height*rows))
        ownership[np.unique(nearest[ys,xs])]=target+1
    rgba=np.array(image)
    prefix=(('T_Sprite_'+entry.get('category','Character')+'_') if entry['npc'] else 'T_UI_World_')+('행상_Seated' if entry['name']=='상인' else entry['name']+'_')
    files={}
    for index,view in enumerate(('Front','Side','Rear','UsedFront','UsedSide','UsedRear')[:count]):
        pixels=rgba.copy(); pixels[:,:,3]=np.where(ownership[nearest]==index+1,alpha,0)
        frame=Image.fromarray(pixels)
        bbox=frame.getbbox(); assert bbox is not None
        frame=frame.crop((max(0,bbox[0]-16),max(0,bbox[1]-16),min(image.width,bbox[2]+16),min(image.height,bbox[3]+16)))
        path=DEST/(prefix+view+'.png'); frame.save(path)
        files[view[0].lower()+view[1:]]=path.relative_to(ROOT).as_posix()
    art.append({'name':entry['name'],'npc':entry['npc'],'height':entry.get('height',350 if entry['npc'] else 260),**files})
    print(entry['name'],image.size,count,'lossless connected sprite frames')
(OUT/'ArtReady.staging.json').write_text(json.dumps({'art':art},ensure_ascii=False,indent=2),encoding='utf-8')
