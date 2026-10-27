from pathlib import Path
import json
from PIL import Image,ImageDraw
p=Path(__file__).resolve().parent
actors=json.loads((p/'Manifest.json').read_text('utf-8'))['actors']
rows=[a for a in actors if a['kind']=='Monster' and not (p/'Approved'/(a['id']+'.txt')).exists() and all((p/'Outputs'/Path(x['source']).name).exists() for x in a['poses'])]
index=[]
for start in range(0,len(rows),3):
    batch=rows[start:start+3]; sheet=Image.new('RGB',(1440,len(batch)*395),(54,61,48));draw=ImageDraw.Draw(sheet)
    for row,a in enumerate(batch):
        draw.text((10,row*395+6),a['id'],fill=(239,230,210))
        imgs=[('Idle',p/'Idle'/(a['id']+'.png'))]+[(x['state'],p/'Outputs'/Path(x['source']).name) for x in a['poses']]
        for col,(state,src) in enumerate(imgs):
            im=Image.open(src).convert('RGBA');im.thumbnail((350,350));sheet.paste(im,(col*360+(360-im.width)//2,row*395+30+(350-im.height)//2),im)
            draw.text((col*360+12,row*395+375),state,fill=(233,224,205))
    dest=p/'Review'/('Pending-'+str(start//3+1)+'.jpg');sheet.save(dest,quality=92);index.append({'file':str(dest),'actors':[x['id'] for x in batch]})
(p/'Review'/'Pending.json').write_text(json.dumps(index,indent=2))
print(json.dumps(index,indent=2))
