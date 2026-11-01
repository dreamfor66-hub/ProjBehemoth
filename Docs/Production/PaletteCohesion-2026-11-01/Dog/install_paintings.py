"""Only uniform canvas framing and QA composition. All painting edits use built-in imagegen."""
from pathlib import Path
import json,hashlib,os
from PIL import Image,ImageDraw,ImageFont
HERE=Path(__file__).resolve().parent
PROJECT=HERE.parents[3]
sources=json.loads((HERE/'Sources.json').read_text(encoding='utf-8'))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def bounds(im):return im.getchannel('A').point(lambda v:255 if v>32 else 0).getbbox()
old=Image.open(HERE/'Before/Assets/Art/Combat/Lore01/T_Sprite_Character_길동무_Idle.png').convert('RGBA')
raw_idle=Image.open(sources['idle']).convert('RGBA')
ob=bounds(old);nb=bounds(raw_idle)
scale=(ob[3]-ob[1])/(nb[3]-nb[1])
records=[]
for pose in ['Idle','Attack']:
    path=PROJECT/f'Assets/Art/Combat/Lore01/T_Sprite_Character_길동무_{pose}.png'
    before=HERE/'Before'/path.relative_to(PROJECT)
    reference=Image.open(before).convert('RGBA')
    raw=Image.open(sources[pose.lower()]).convert('RGBA')
    old_bounds=bounds(reference);new_bounds=bounds(raw)
    res=raw.resize((round(raw.width*scale),round(raw.height*scale)),Image.Resampling.LANCZOS)
    offset=(round(reference.width/2-(new_bounds[0]+new_bounds[2])/2*scale),round(ob[3]-new_bounds[3]*scale))
    canvas=Image.new('RGBA',reference.size);canvas.alpha_composite(res,offset)
    meta=path.with_suffix('.png.meta');meta_before=sha(meta)
    temporary=HERE/(path.stem+'.staging.png');canvas.save(temporary)
    assert path.resolve().is_relative_to((PROJECT/'Assets/Art/Combat/Lore01').resolve())
    os.replace(temporary,path)
    assert sha(meta)==meta_before
    records.append({'pose':pose,'production':str(path.relative_to(PROJECT)).replace('\\','/'),'generated':sources[pose.lower()],'canvas':reference.size,'alphaBounds':bounds(canvas),'uniformScale':scale,'offset':offset,'beforeSha256':sha(before),'generatedSha256':sha(Path(sources[pose.lower()])),'sha256':sha(path),'metaSha256':sha(meta)})
(HERE/'InstalledPaintings.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
(HERE/'DogAssetFiles.json').write_text(json.dumps([x['production'] for x in records],ensure_ascii=False,indent=2),encoding='utf-8')
board=Image.new('RGB',(1400,850),(30,31,29));draw=ImageDraw.Draw(board)
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',22)
for i,(label,p) in enumerate([('Before / side showcase',HERE/'Before/Assets/Art/Combat/Lore01/T_Sprite_Character_길동무_Idle.png'),('Forward / Idle',PROJECT/records[0]['production']),('Forward / Attack',PROJECT/records[1]['production'])]):
    draw.text((20+i*460,15),label,font=font,fill=(220,214,193))
    im=Image.open(p).convert('RGBA');im=im.crop(bounds(im));im.thumbnail((430,760))
    board.paste(im,(20+i*460+(430-im.width)//2,70+760-im.height),im)
board.save(HERE/'PoseReview.jpg',quality=95)
print(json.dumps(records,ensure_ascii=False,indent=2))
