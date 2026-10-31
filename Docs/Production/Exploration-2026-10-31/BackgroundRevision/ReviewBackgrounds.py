"""Read-only production-image QA. Only writes review sheets / measured line reports."""
from pathlib import Path
import json, math
import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFont

root=Path(__file__).parent
sources=json.loads((root/'WallSources.json').read_text(encoding='utf-8'))
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',18)
sheet=Image.new('RGB',(1800,1190),(30,30,28)); draw=ImageDraw.Draw(sheet)
report=[]
for i,item in enumerate(sources):
 im=Image.open(item['path']).convert('RGB'); w,h=im.size
 edges=cv2.Canny(np.array(im),60,130)
 edges[:int(h*.64),:]=0
 lines=cv2.HoughLinesP(edges,1,np.pi/180,80,minLineLength=150,maxLineGap=18)
 left=[];right=[]
 for line in ([] if lines is None else lines[:,0]):
  x1,y1,x2,y2=map(float,line)
  if abs(x2-x1)<80:continue
  m=(y2-y1)/(x2-x1); b=y1-m*x1; mx=(x1+x2)/2
  if .18<abs(m)<1.8:
   if m<0 and mx<w*.46:left.append((m,b,line))
   if m>0 and mx>w*.54:right.append((m,b,line))
 points=[]
 for m,b,_ in left:
  for n,c,_ in right:
   x=(c-b)/(m-n);y=m*x+b
   if .40<x/w<.60 and .42<y/h<.62: points.append((x/w,y/h))
 vp=np.median(points,axis=0).tolist() if points else None
 report.append({'act':item['act'],'imageSize':[w,h],'measuredFloorLineIntersection':vp,'linePairCount':len(points),
  'rearFloorJoinApprox':[.50,.618], 'method':'Canny/Hough long floor edges below y=.64. Median intersections from opposite-slope pairs near center; QA estimate, not editable gameplay data.'})
 thumb=im.resize((888,500));x=(i%2)*900;y=(i//2)*590
 sheet.paste(thumb,(x,y+35));draw.text((x+12,y+5),f"B{item['act']} / same camera and floor reference",font=font,fill='white')
 guide=ImageDraw.Draw(sheet)
 guide.line((x,y+35+500*.618,x+888,y+35+500*.618),fill=(230,170,65),width=2)
 if vp:
  px=x+vp[0]*888;py=y+35+vp[1]*500
  guide.ellipse((px-5,py-5,px+5,py+5),fill=(235,100,80))
  draw.text((x+12,y+543),f"floor-edge VP estimate: {vp[0]:.3f}, {vp[1]:.3f}",font=font,fill=(220,210,195))
sheet.save(root/'Background-Projection-Review.jpg',quality=95)
(root/'ProjectionMeasurements.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
