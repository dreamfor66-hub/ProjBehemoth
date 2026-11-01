"""Unmodified imagegen PNG installation and diagnostic compositing only."""
from pathlib import Path
import json,re,shutil,os,sys,hashlib
import numpy as np
from PIL import Image,ImageDraw,ImageFont

HERE=Path(__file__).parent
PROJECT=HERE.parents[3]
manifest=json.loads((HERE/'ArtReady.json').read_text(encoding='utf-8'))
files=manifest['files']
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',22)

def geometry(path):
    im=Image.open(path).convert('RGBA')
    a=np.array(im)[::-1]
    m=a[:,:,3]>=180
    yy,xx=np.where(m)
    x0,x1,y0,y1=int(xx.min()),int(xx.max()),int(yy.min()),int(yy.max())
    material=m&(a[:,:,:3].max(2)>55)
    support=y1
    for y in range(y0,int(np.ceil(y0+(y1-y0)*.30))):
        if material[y,x0:x1+1].any():support=y;break
    if support==y1:support=y0
    fy,fx=np.where(material[support:int(np.ceil(support+(y1-y0)*.22)),x0:x1+1])
    left,right=(int(fx.min())+x0,int(fx.max())+x0) if len(fx) else (x0,x1)
    stone=m.copy();stone[:a.shape[0]-600]=False
    return dict(canvas=list(im.size),bounds=[x0,y0,x1+1,y1+1],support=[left,support,right+1],
                alphaMin=int(a[:,:,3].min()),alphaMax=int(a[:,:,3].max()))

for f in files:
    original=PROJECT/f['path']
    before=HERE/'Before'/f['path']
    if not before.exists():before.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(original,before)
    oldmeta=Path(str(original)+'.meta');beforemeta=Path(str(before)+'.meta')
    if not beforemeta.exists():shutil.copy2(oldmeta,beforemeta)
    f['before']=str(before)
    f['beforeGeometry']=geometry(before)
    f['afterGeometry']=geometry(f['source'])
    assert f['beforeGeometry']['canvas']==f['afterGeometry']['canvas'],f['key']
    f['guid']=re.search(r'^guid: (\w+)',oldmeta.read_text(encoding='utf-8'),re.M)[1]
    f['sourceSha256']=hashlib.sha256(Path(f['source']).read_bytes()).hexdigest()

bykey={f['key'].replace('Front2','Front'):f for f in files}
def place(canvas,path,normal,height,anchor,perspective,light):
    im=Image.open(path).convert('RGBA')
    ref=geometry(normal)
    ppu=height*perspective*canvas.height/1000/(ref['bounds'][3]-ref['bounds'][1])
    rgba=np.array(im)
    rgba[:,:,:3]=(rgba[:,:,:3].astype(float)*np.array(light)).clip(0,255).astype('uint8')
    paint=Image.fromarray(rgba).resize((round(im.width*ppu),round(im.height*ppu)),Image.Resampling.LANCZOS)
    sx=(ref['support'][0]+ref['support'][2])/2
    sy=im.height-ref['support'][1]
    x=round(anchor[0]*canvas.width-sx*ppu)
    y=round((1-anchor[1])*canvas.height-sy*ppu)
    canvas.alpha_composite(paint,(x,y))

for act in range(1,4):
    floor=(PROJECT/f'Assets/Data/Presentation/Angular/B{act}.asset').read_text(encoding='utf-8-sig')
    lights=re.search(r'objectIllumination: \{r: ([\d.]+), g: ([\d.]+), b: ([\d.]+)',floor)
    light=list(map(float,lights.groups()))
    bg=PROJECT/('Assets/Art/World/Angular/Restoration01/B1-Walls.png' if act==1 else f'Assets/Art/World/Angular/Backdrops/B{act}-Walls.png')
    board=Image.new('RGB',(1672,980),(25,27,29));draw=ImageDraw.Draw(board)
    for row,view in enumerate(['Front','Side']):
        for col,state in enumerate(['Before','After']):
            canvas=Image.open(bg).convert('RGBA')
            god=bykey['Goddess_'+view];fountain=bykey['Fountain_'+view]
            field='before' if state=='Before' else 'source'
            place(canvas,god[field],god[field],580,(.5,.35) if view=='Front' else(.24,.29),.8 if view=='Front' else .95,light)
            place(canvas,fountain[field],fountain[field],260,(.7,.235),.70+(.38-.235)*1.45,light)
            canvas=canvas.resize((836,470),Image.Resampling.LANCZOS)
            board.paste(canvas,(col*836,row*490+20))
            draw.text((col*836+10,row*490),f'B{act} {view} {state} | current scene illumination',fill='white',font=font)
    board.save(HERE/f'B{act}-BeforeAfter.jpg',quality=95)

for view in ['Front','Side','Rear']:
    normal=bykey['Fountain_'+view];used=bykey['Fountain_Used'+view]
    board=Image.new('RGB',(1324,670),(31,33,35));draw=ImageDraw.Draw(board)
    for col,f in enumerate([normal,used]):
        im=Image.open(f['source']).convert('RGBA').resize((662,594))
        board.paste(im,(col*662,40),im)
        draw.text((col*662+10,5),view+(' NORMAL' if col==0 else ' DRY | same canvas'),fill='white',font=font)
    board.save(HERE/f'Fountain-{view}-StateContact.jpg',quality=95)

(HERE/'GeometryAndCanvas.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
if '--install' in sys.argv:
    for f in files:
        target=PROJECT/f['path'];staged=Path(str(target)+'.palette-staging')
        shutil.copy2(f['source'],staged);os.replace(staged,target)
        assert hashlib.sha256(target.read_bytes()).hexdigest()==f['sourceSha256']
        assert re.search(r'^guid: (\w+)',Path(str(target)+'.meta').read_text(encoding='utf-8'),re.M)[1]==f['guid']
    (HERE/'Installed.txt').write_text('Installed8 exact unmodified built-in imagegen PNGs. Canvas and existing .meta/GUID preserved. Geometry must refresh from alpha before root live verification.\n',encoding='utf-8')
print('Prepared8 PNGs, 3-act alpha/lighting contact boards, 3 dry-state boards; production install='+str('--install' in sys.argv))
