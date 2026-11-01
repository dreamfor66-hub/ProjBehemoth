"""Mechanical canvas framing and production installation; no color/painting edits."""
from pathlib import Path
import json,hashlib,shutil,os,time
import numpy as np
from PIL import Image,ImageDraw,ImageFont
HERE=Path(__file__).resolve().parent
PROJECT=HERE.parents[3]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def bounds(im):return im.getchannel('A').point(lambda v:255 if v>32 else 0).getbbox()
source=json.loads((HERE/'WizardOriginals.json').read_text(encoding='utf-8'))
selected=json.loads((HERE/'WizardSelectedSources.json').read_text(encoding='utf-8'))
records=[]
for item in selected:
    original=next(x for x in source if x['production']==item['production'])
    target=PROJECT/item['production']
    old=Image.open(original['backup']).convert('RGBA')
    new=Image.open(item['generated']).convert('RGBA')
    metadata={str(p.relative_to(PROJECT)).replace('\\','/'):sha(p) for p in [target.with_suffix('.png.meta'),target.with_suffix('.asset'),target.with_suffix('.asset.meta')] if p.exists()}
    if item['framing']=='alpha':
        ob,nb=bounds(old),bounds(new)
        scale=min((ob[2]-ob[0])/(nb[2]-nb[0]),(ob[3]-ob[1])/(nb[3]-nb[1]))
        res=new.resize((round(new.width*scale),round(new.height*scale)),Image.Resampling.LANCZOS)
        offset=(round((ob[0]+ob[2])/2-(nb[0]+nb[2])/2*scale),round(ob[3]-nb[3]*scale))
        out=Image.new('RGBA',old.size)
        out.alpha_composite(res,offset)
    else:
        assert abs(new.width/new.height-old.width/old.height)<.002,('Image aspect changed',item['id'])
        scale=min(old.width/new.width,old.height/new.height)
        res=new.resize((round(new.width*scale),round(new.height*scale)),Image.Resampling.LANCZOS)
        offset=((old.width-res.width)//2,(old.height-res.height)//2)
        out=Image.new('RGBA',old.size)
        out.alpha_composite(res,offset)
    staging=HERE/'WizardStaging'
    staging.mkdir(exist_ok=True)
    temporary=staging/target.name
    out.save(temporary)
    assert target.resolve().is_relative_to((PROJECT/'Assets/Art').resolve())
    for attempt in range(4):
        try:
            os.replace(temporary,target)
            break
        except OSError:
            if attempt==3:raise
            time.sleep(.5)
    assert Image.open(target).size==old.size
    assert all(sha(PROJECT/p)==v for p,v in metadata.items())
    ma=np.array(old)[:,:,3]>32;mb=np.array(out)[:,:,3]>32
    iou=float((ma&mb).sum()/(ma|mb).sum())
    records.append({**item,'sourceBackup':original['backup'],'originalSha256':sha(Path(original['backup'])),'generatedSha256':sha(Path(item['generated'])),'productionSha256':sha(target),'canvas':old.size,'uniformScale':scale,'offset':offset,'originalAlphaBounds':bounds(old),'newAlphaBounds':bounds(out),'silhouetteIoU':iou,'preservedMetadata':metadata})
(HERE/'PlayerAssetFiles.json').write_text(json.dumps([x['production'] for x in records],ensure_ascii=False,indent=2),encoding='utf-8')
(HERE/'WizardInstallation.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',22)
small=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',17)
board=Image.new('RGB',(1800,1560),(29,30,28));d=ImageDraw.Draw(board)
d.text((15,8),'Wizard 원본 / 교정 — 실제 PNG · 구도/책/촛불/부엉이 관계 유지',font=font,fill=(230,224,204))
for idx,item in enumerate(records[:3]):
    y=48+idx*365
    d.text((15,y),item['id'],font=font,fill=(220,218,196))
    for col,p in enumerate([Path(item['sourceBackup']),PROJECT/item['production']]):
        im=Image.open(p).convert('RGBA');im.thumbnail((855,320))
        board.paste(im,(15+col*890+(855-im.width)//2,y+32+(320-im.height)//2),im)
for idx,item in enumerate(records[3:]):
    x=10+idx*447;y=1150
    d.text((x,y),item['id'],font=font,fill=(220,218,196))
    for col,p in enumerate([Path(item['sourceBackup']),PROJECT/item['production']]):
        im=Image.open(p).convert('RGBA');im=im.crop(bounds(im));im.thumbnail((216,345))
        board.paste(im,(x+col*220+(216-im.width)//2,y+34+345-im.height),im)
board.save(HERE/'WizardArtReview.jpg',quality=95)
print(json.dumps([{'id':x['id'],'size':x['canvas'],'silhouetteIoU':round(x['silhouetteIoU'],4),'alphaBounds':x['newAlphaBounds']} for x in records],indent=2))
