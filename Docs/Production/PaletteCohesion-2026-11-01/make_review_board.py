"""Read-only QA layout of evidence. Never changes any production painting."""
from pathlib import Path
from PIL import Image,ImageOps,ImageDraw
folder=Path(__file__).resolve().parent
root=folder.parents[2]
rows=[
 ('USER-REJECTED YELLOW / CLEAN ROOM',root/'Assets/Art/References/DarkestDungeonVisual/ProjectContext/B1-04-RejectedOathRoom.png',
  'REVISED B1 ROOM / ACTUAL UNITY',folder/'Screens/Final-B1-Data_RoomTheme_잊힌서약실-North.jpg'),
 ('USER-REPORTED COMPANION FRAMING',Path('C:/Users/user/AppData/Local/Temp/codex-clipboard-fe0d5b72-f4a7-4f61-97dd-7052366a0995.png'),
  'REVISED COMPANION / ACTUAL UNITY',folder/'Screens/Final-B1-Dog-Navigation.jpg')
]
board=Image.new('RGB',(1600,980),(18,18,18));draw=ImageDraw.Draw(board)
for y,row in enumerate(rows):
 for x,(label,path) in enumerate([(row[0],row[1]),(row[2],row[3])]):
  draw.text((x*800+12,y*490+12),label,fill=(220,220,210))
  picture=ImageOps.contain(Image.open(path).convert('RGB'),(790,445))
  board.paste(picture,(x*800+(800-picture.width)//2,y*490+38+(445-picture.height)//2))
board.save(folder/'FinalComparison.jpg',quality=94)
print(folder/'FinalComparison.jpg')
