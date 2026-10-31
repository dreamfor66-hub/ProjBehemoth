from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
folder=Path(__file__).resolve().parent
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',18)
def board(names,out):
 width,height=480,270
 image=Image.new('RGB',(width*3,(height+34)*((len(names)+2)//3)),'#202020');draw=ImageDraw.Draw(image)
 for i,name in enumerate(names):
  source=Image.open(folder/(name+'.png')).convert('RGB');source.thumbnail((width,height));x=i%3*width;y=i//3*(height+34);image.paste(source,(x,y+34));draw.text((x+8,y+6),name,font=font,fill='white')
 image.save(folder/out,quality=92)
board(['HUD-Ready','HUD-Recovering','HUD-GuardHeld','HUD-GuardBroken','HUD-WizardSpells','HUD-RocGroup'],'HUD-Review.jpg')
board(['Caption-Data_Monster_B1_Minion_Bat','Caption-Data_Monster_B1_Minion_Skeleton','Caption-LampPriest','Caption-Data_Monster_B3_Boss_SoulEater','VFX-Blocked','VFX-ActualHit','VFX-ActualHeavy','WeakPoint-Break','Caption-Long'],'Combat-Review.jpg')
board(['Death-Reveal','Death-TitleOnly','Returned-ToTitle'],'Death-Review.jpg')
