"""QA contact sheets and source-manifest maintenance; never redraws production PNGs."""
from pathlib import Path
import hashlib, json, shutil
from PIL import Image, ImageDraw, ImageFont

root=Path(__file__).resolve().parents[3]
folder=Path(__file__).resolve().parent
old=root/'Docs/Production/StateArtwork-2026-10-26'
manifest_path=old/'Manifest.json'
manifest=json.loads(manifest_path.read_text(encoding='utf-8-sig'))
for actor in manifest['actors']:
    name={'Monster-StringRevenant':'Skeleton','Monster-BoneScorpion':'Spider'}.get(actor['id'])
    if not name: continue
    actor['idleHash']=hashlib.sha256((folder/f'Generated/{name}-Idle.png').read_bytes()).hexdigest().upper()
    for pose in actor['poses']:
        target=root/pose['source']
        backup=folder/'Before/StateSources'/target.name
        backup.parent.mkdir(parents=True,exist_ok=True)
        if not backup.exists(): shutil.copy2(target,backup)
        shutil.copy2(folder/f'Generated/{name}-{pose["state"]}.png',target)
    revision='|'.join('-'.join(f'{x:02X}' for x in hashlib.sha256((root/p['source']).read_bytes()).digest()) for p in actor['poses'])
    for sub in ['Approved','Installed']:
        (old/sub/f'{actor["id"]}.txt').write_text(revision,encoding='utf-8')
manifest_path.write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',22)
sheet=Image.new('RGB',(1600,1540),'#272925'); draw=ImageDraw.Draw(sheet)
def tile(path,box,label):
    x,y,w,h=box
    draw.rectangle((x,y,x+w,y+h),fill='#4a4c46')
    img=Image.open(path).convert('RGBA'); img.thumbnail((w-24,h-48),Image.Resampling.LANCZOS)
    sheet.paste(img,(x+(w-img.width)//2,y+32+(h-40-img.height)//2),img)
    draw.text((x+10,y+8),label,font=font,fill='#f1e5c6')
for i,num in enumerate(['14','02','10','05']):
    tile(root/f'Assets/Art/References/AngularMonsterStyle/Images/{num}.jpg',(i*400,0,394,310),f'Original reference {num}')
for row,name in enumerate(['Skeleton','Spider']):
    for col,state in enumerate(['Idle','Ready','Attack','Dead']):
        tile(folder/f'Generated/{name}-{state}.png',(col*400,320+row*390,394,380),f'{name} {state}')
for i,hero in enumerate(['Warrior','Barbarian','Monk','Rogue','Samurai','Wizard']):
    path=root/f'Assets/Art/World/Angular/Heroes/{hero}-Rear.png'
    tile(path,(i*266,1110,260,420),f'Current {hero}')
sheet.save(folder/'Review.png')
qa=[]
for path in sorted((folder/'Generated').glob('*.png')):
    if '-v' in path.stem: continue
    im=Image.open(path).convert('RGBA'); a=im.getchannel('A'); box=a.point(lambda v:255 if v>24 else 0).getbbox()
    qa.append(dict(file=path.name,size=im.size,alpha_extrema=a.getextrema(),visible_bounds=box,sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
(folder/'ImageQA.json').write_text(json.dumps(qa,indent=2),encoding='utf-8')
print('Reviewed',len(qa),'distinct RGBA images; contact sheet saved; source manifests updated.')
