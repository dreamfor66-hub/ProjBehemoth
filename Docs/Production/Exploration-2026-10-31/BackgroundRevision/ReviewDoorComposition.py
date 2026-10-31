"""QA composites only, mirrors native UI image placement; does not rewrite production PNGs."""
from pathlib import Path
import json
import numpy as np
from PIL import Image,ImageDraw,ImageFont
root=Path(__file__).parent
data=json.loads((root/'DoorSources.json').read_text(encoding='utf-8'))
walls={s['act']:s['path'] for s in json.loads((root/'WallSources.json').read_text())}
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',18)
sheet=Image.new('RGB',(1672,1045),(30,30,28));draw=ImageDraw.Draw(sheet)
report=[]
for n,act in enumerate([2,3]):
 bg=Image.open(walls[act]).convert('RGBA');W,H=bg.size
 for i,side in enumerate(['Forward','Left','Right']):
  item=next(j for j in data['jobs'] if j['act']==act and j['side']==side)
  src=Image.open(item['path']).convert('RGBA');p=np.array(src);h,w=p.shape[:2]
  ys,xs=np.where(p[:,:,3]>=180);bounds=(int(xs.min()),int(ys.min()),int(xs.max()+1),int(ys.max()+1))
  x,y,width,height=data['placements'][str(act)][i]
  dest=(round(W*x),round(H*(1-y-height)),round(W*width),round(H*height))
  room=np.array(bg)[dest[1]:dest[1]+dest[3],dest[0]:dest[0]+dest[2],:3].max(axis=2)/255
  values=p[:,:,:3].max(axis=2)/255;paint=values[(p[:,:,3]>=180)&(values>.12)&(values<.65)]
  room=room[(room>.12)&(room<.65)]
  exposure=float(np.clip(np.median(room)/np.median(paint),.45,1))
  cropped=src.crop(bounds).resize((dest[2],dest[3]),Image.Resampling.LANCZOS)
  pixels=np.array(cropped);pixels[:,:,:3]=(pixels[:,:,:3]*exposure).astype('uint8')
  bg.alpha_composite(Image.fromarray(pixels),dest[:2])
  report.append({'act':act,'side':side,'sourceBounds':bounds,'placement':data['placements'][str(act)][i],'exposure':exposure})
 bg.convert('RGB').save(root/f'B{act}-AllOpen-Composite.jpg',quality=95)
 thumb=bg.convert('RGB').resize((836,471));sheet.paste(thumb,(0,n*522+35));draw.text((10,n*522+5),f'B{act} QA layer composite',font=font,fill='white')
 # Right half at full scale, to inspect joins rather than merely a thumbnail.
 sheet.paste(bg.convert('RGB').crop((1080,100,1672,620)),(900,n*522))
sheet.save(root/'Door-Composition-Review.jpg',quality=95)
(root/'DoorPlacement-QA.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
