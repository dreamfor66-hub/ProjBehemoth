"""Read-only palette samples and contact sheets; never repaints source art."""
from pathlib import Path
import json, math, argparse
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[3]
OUT=Path(__file__).resolve().parent
parser=argparse.ArgumentParser();parser.add_argument('--tag',default='');args=parser.parse_args()
prefix=args.tag+'-' if args.tag else ''
ART=ROOT/'Assets/Art/World'
BASE=[ART/'Angular/Restoration01/B1-Walls.png',ART/'Angular/Backdrops/B2-Walls.png',ART/'Angular/Backdrops/B3-Walls.png']
paths=BASE+sorted((ART/'Angular/RoomThemes').glob('*.png'))
paths += sorted((ART/'Exploration').glob('T_UI_Room_B*.png'))
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',16)
thumb=(560,315)
records=[]
for p in paths:
    if '장식_' in p.name: continue
    a=np.asarray(Image.open(p).convert('RGB'),dtype=float)
    h,w,_=a.shape
    # Same approximate plain floor/back wall regions. Occupied event rooms must
    # be judged visually; these numbers are diagnostic, not an acceptance test.
    samples={}
    for name,rect in {'back':(.43,.40,.57,.57),'floor':(.38,.76,.62,.88),'near_stone':(.12,.25,.16,.40)}.items():
        x0,y0,x1,y1=rect
        q=a[int(y0*h):int(y1*h),int(x0*w):int(x1*w)].reshape(-1,3)
        med=np.median(q,axis=0)
        samples[name]={'rgb_median':med.round(1).tolist(),'r_minus_b':round(float(med[0]-med[2]),1),'luma':round(float(med@np.array([.2126,.7152,.0722])),1)}
    records.append({'path':str(p.relative_to(ROOT)).replace('\\','/'),'size':[w,h],'samples':samples})
pages=[]
for n in range(0,len(records),9):
    subset=records[n:n+9];sheet=Image.new('RGB',(thumb[0]*3,355*math.ceil(len(subset)/3)),(24,24,24));d=ImageDraw.Draw(sheet)
    for k,rec in enumerate(subset):
        p=ROOT/rec['path'];im=Image.open(p).convert('RGB');im.thumbnail(thumb)
        x=(k%3)*thumb[0];y=(k//3)*355
        sheet.paste(im,(x,y));d.text((x+6,y+318),p.stem,font=font,fill='white')
    path=OUT/f'{prefix}BackgroundAudit-{n//9+1}.jpg';sheet.save(path,quality=94);pages.append(str(path))
(OUT/(prefix+'BackgroundPaletteSamples.json')).write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'count':len(records),'boards':pages},ensure_ascii=False))
