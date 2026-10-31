from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,math
folder=Path(__file__).resolve().parent
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',18)
def board(names,out,cols=3,width=480):
 rows=math.ceil(len(names)/cols);height=270
 result=Image.new('RGB',(width*cols,(height+34)*rows),'#202020');draw=ImageDraw.Draw(result)
 for i,(path,label) in enumerate(names):
  image=Image.open(path).convert('RGB');image.thumbnail((width,height))
  x=(i%cols)*width;y=(i//cols)*(height+34)
  result.paste(image,(x,y+34));draw.text((x+8,y+6),label,font=font,fill='white')
 result.save(folder/out,quality=90)
index=json.loads((folder/'DataNames.json').read_text(encoding='utf-8'))['entries']
def label(stem):
 return next((e['displayName'] for e in index if Path(e['oldPath']).stem==stem),stem)
board([(p,label(p.stem.removeprefix('Species-'))) for p in sorted(folder.glob('Species-*.png'))],'Species-Review.jpg')
poses=[]
for stem in ['Data_Monster_B2_Boss_FrostQueen','Data_Monster_B3_Boss_SoulEater','StormMatriarch']:
 for prefix in ['Species','Ready','Attack']:
  poses.append((folder/f'{prefix}-{stem}.png',label(stem)+' · '+prefix))
board(poses,'GiantRuntime-Review.jpg')
board([(folder/(p+'.png'),p) for p in ['ActualMonsterGuardImpact','ActualMonsterHit','ActualMonsterChargeImpact','GuardAndCooldown','LowHealth','DeathResult']],'Feedback-Review.jpg')
