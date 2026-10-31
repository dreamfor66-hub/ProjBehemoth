"""QA only: contact sheets of actual Unity frames; never rewrites production art."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import re
root=Path(__file__).parent
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',16)
files=sorted((root/'Runtime').glob('*.jpg'))
for act in range(1,4):
 frames=[p for p in files if p.name.startswith(f'B{act}-')]
 for page in range((len(frames)+11)//12):
  sheet=Image.new('RGB',(1800,1155),(35,35,33));draw=ImageDraw.Draw(sheet)
  for n,p in enumerate(frames[page*12:page*12+12]):
   name=p.stem.split('Data_RoomObject_')[1]; direction=int(re.search(r'-D(\d)-',name)[1]); family=name.split('-D')[0]
   wall=family in ['분수','여신상','잠긴문','벽장치']
   if wall:
    crop=(675,125,1200,785) if direction==0 else (145,125,735,825) if direction==1 else (1250,125,1850,825)
   else:
    crop=(200,460,810,1020) if direction in [0,3] else (1240,460,1920,1080)
   image=Image.open(p).crop(crop);image.thumbnail((444,348))
   x=(n%4)*450; y=(n//4)*385
   sheet.paste(image,(x+(450-image.width)//2,y));draw.text((x+8,y+351),f'B{act} {name}',font=font,fill=(220,220,206))
  sheet.save(root/f'B{act}-Review-{page+1:02}.jpg',quality=92)
