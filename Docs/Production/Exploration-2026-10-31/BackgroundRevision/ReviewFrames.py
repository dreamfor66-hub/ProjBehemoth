"""QA only: contact sheets of actual Unity frames; never rewrites production art."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import re
root=Path(__file__).parent
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',16)
files=sorted((root/'Runtime').glob('*Data_RoomObject_*.jpg'))
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
 for mask in range(8):
  p=root/'Runtime'/f'B{act}-Room-Mask{mask}.jpg'
  if not p.exists():continue
  if mask==0:
   doors=Image.new('RGB',(1920,600),(28,28,25));labels=ImageDraw.Draw(doors)
  im=Image.open(p);im.thumbnail((480,270));x=(mask%4)*480;y=(mask//4)*300
  doors.paste(im,(x,y));labels.text((x+8,y+272),f'B{act} doors {mask:03b}',font=font,fill='white')
 if (root/'Runtime'/f'B{act}-Room-Mask7.jpg').exists():doors.save(root/f'B{act}-Door-Matrix.jpg',quality=95)
comparison=Image.new('RGB',(2880,1140),(28,28,25));labels=ImageDraw.Draw(comparison)
for act in range(1,4):
 for row,name in enumerate(['Room-Mask7','Data_RoomObject_분수-D1-Available']):
  p=root/'Runtime'/f'B{act}-{name}.jpg'
  if not p.exists():continue
  im=Image.open(p);im.thumbnail((960,540));x=(act-1)*960;y=row*570
  comparison.paste(im,(x,y));labels.text((x+10,y+542),f'B{act} / {name}',font=font,fill='white')
comparison.save(root/'Runtime-Act-Comparison.jpg',quality=95)
