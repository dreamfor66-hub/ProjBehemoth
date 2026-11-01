"""Diagnostic comparison only. Never changes production or generated paintings."""
from pathlib import Path
import json
from PIL import Image,ImageDraw,ImageFont
import numpy as np

HERE=Path(__file__).parent
PROJECT=HERE.parents[3]
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',18)
small=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',15)
rows=[]
for view in ['Front','Side','Rear']:
    paths=[('Closed authority',PROJECT/f'Assets/Art/World/Exploration/T_UI_World_Chest_{view}.png'),
           ('Installed open',PROJECT/f'Assets/Art/World/Exploration/T_UI_World_Chest_Used{view}.png')]
    for p in sorted((HERE/'Prompts').glob(view+'-Attempt*.json')):
        paths.append((p.stem,Path(json.loads(p.read_text(encoding='utf-8'))['source'])))
    width=384*len(paths)
    board=Image.new('RGB',(width,520),(34,36,38))
    draw=ImageDraw.Draw(board)
    measurements=[]
    for col,(label,path) in enumerate(paths):
        im=Image.open(path).convert('RGBA')
        draw.text((col*384+8,8),label,fill='white',font=font)
        board.paste(im.resize((384,256)),(col*384,35),im.resize((384,256)))
        cropped=im.crop((0,600,1536,1024)).resize((384,106))
        board.paste(cropped,(col*384,330),cropped)
        a=np.array(im)
        mask=(a[:,:,3]>=240)&(a[:,:,:3].max(2)>25)
        mask[:600]=False
        y,x=np.where(mask)
        bbox=[int(x.min()),int(y.min()),int(x.max()),int(y.max())]
        measurements.append(dict(label=label,path=str(path),lowerMaterialBBox=bbox))
        draw.text((col*384+8,450),'y >= 600 material bbox:',fill='white',font=small)
        draw.text((col*384+8,475),str(bbox),fill='white',font=small)
    board.save(HERE/f'{view}-Candidates.jpg',quality=95)
    rows.append(dict(view=view,candidates=measurements))
(HERE/'Candidates.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(rows,ensure_ascii=False,indent=2))
