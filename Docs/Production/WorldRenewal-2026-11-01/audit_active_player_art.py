"""Read-only tracing of active main/outgame art; writes review artifacts only."""
from pathlib import Path
import re, json, subprocess, hashlib
import yaml
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
PROJECT = HERE.parents[2]
ASSETS = PROJECT / 'Assets'
guid_paths = {}
for name in subprocess.check_output(['rg', '--files', '-g', '*.meta'], cwd=ASSETS, text=True, encoding='utf-8').splitlines():
    meta = ASSETS / name
    with meta.open(encoding='utf-8-sig') as stream:
        start = stream.read(250)
    match = re.search(r'^guid: ([a-f0-9]+)', start, re.M)
    if match: guid_paths[match.group(1)] = meta.with_suffix('')

def docs(path):
    text = path.read_text(encoding='utf-8-sig')
    result = {}
    for match in re.finditer(r'^--- !u!(\d+) &(\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)', text, re.M|re.S):
        parsed = yaml.safe_load(match.group(3))
        result[int(match.group(2))] = {'type': int(match.group(1)), 'data': next(iter(parsed.values()))}
    return result

def data(path):
    return next(iter(docs(path).values()))['data']

def resolve(reference):
    return guid_paths.get((reference or {}).get('guid', ''))

def image_path(reference):
    path = resolve(reference)
    if path is None: return None
    if path.suffix.lower() in ('.png','.jpg','.jpeg','.webp'): return path
    if path.suffix == '.asset':
        sprite = data(path)
        if path.with_suffix('.png').exists(): return path.with_suffix('.png')
        return resolve((sprite.get('m_RD') or {}).get('texture'))
    return None

def relative(path):
    return str(path.relative_to(PROJECT)).replace('\\','/') if path is not None else None

out = docs(ASSETS / 'Scenes/OutGameScene.unity')
main = docs(ASSETS / 'Scenes/MainGameScene.unity')
out_controller = next(item['data'] for item in out.values() if 'characterArtwork' in item['data'] and 'content' in item['data'])
content_path = resolve(out_controller['content'])
assert content_path is not None
assert any(resolve(item['data'].get('content')) == content_path for item in main.values())
content = data(content_path)
rows = []
for index, reference in enumerate(content['characters']):
    path = resolve(reference); character = data(path)
    animation_path = resolve(character['combatAnimation']); animation = data(animation_path)
    card = out[out_controller['characterArtwork'][index]['fileID']]['data']
    paintings = {
        'Selection': image_path(character['selectionIllustration']),
        'Card': image_path(card['m_Sprite']),
        'Portrait / Front': image_path(character['portrait']),
        'CombatSprite / Rear': image_path(character['combatSprite']),
    }
    for pose in ('idle','ready','attack','guard','dead','readyLeftToRight','attackLeftToRight'):
        if animation.get(pose,{}).get('fileID',0): paintings[pose] = image_path(animation[pose])
    rows.append({'index': index, 'name': character['displayName'], 'characterData': relative(path), 'animationData': relative(animation_path), 'images': {key:relative(value) for key,value in paintings.items()}})

transforms = {item['data']['m_GameObject']['fileID']: item['data'] for item in out.values() if item['type'] in (4,224)}
transform_docs = {key:item['data'] for key,item in out.items() if item['type'] in (4,224)}
def hierarchy(game_object_id):
    result=[]
    while game_object_id:
        result.insert(0,out[game_object_id]['data'].get('m_Name',''))
        transform = transforms.get(game_object_id,{})
        parent = transform.get('m_Father',{}).get('fileID',0)
        game_object_id = transform_docs.get(parent,{}).get('m_GameObject',{}).get('fileID',0)
    return '/'.join(result)
def active_in_saved_hierarchy(game_object_id):
    while game_object_id:
        if not out[game_object_id]['data'].get('m_IsActive',1): return False
        transform=transforms.get(game_object_id,{})
        parent=transform.get('m_Father',{}).get('fileID',0)
        game_object_id=transform_docs.get(parent,{}).get('m_GameObject',{}).get('fileID',0)
    return True

title=[]
for item in out.values():
    component=item['data']
    if 'm_Sprite' not in component: continue
    path=image_path(component['m_Sprite'])
    if path is None:continue
    chain=hierarchy(component['m_GameObject']['fileID'])
    if '/TitlePage/' in chain and ('Title' in path.name or 'Logo' in path.name) or ('Background' in chain and 'Title' in path.name):
        title.append({'hierarchy':chain,'image':relative(path),'savedActiveSelf':bool(out[component['m_GameObject']['fileID']]['data'].get('m_IsActive',1)),'savedActiveInHierarchy':active_in_saved_hierarchy(component['m_GameObject']['fileID']),'material':relative(resolve(component.get('m_Material'))),'tint':component.get('m_Color')})

result={'outgameScene':'Assets/Scenes/OutGameScene.unity','mainScene':'Assets/Scenes/MainGameScene.unity','sameGameContent':relative(content_path),'illustratedSelection':out_controller.get('illustratedSelection'), 'characters':rows,'titleImages':title}
images=set(value for row in rows for value in row['images'].values() if value) | {item['image'] for item in title}
result['sourceHashes']={name:hashlib.sha256((PROJECT/name).read_bytes()).hexdigest() for name in sorted(images)}
(HERE/'GlobalPlayerOutgameAssets.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')

font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',22)
small=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',16)
columns=[('Selection',440),('Card',145),('Portrait / Front',220),('CombatSprite / Rear',210),('idle',200),('ready',200),('attack',210),('guard',190),('readyLeftToRight',200),('attackLeftToRight',210)]
width=sum(w for _,w in columns)+30
height=6*360+820
board=Image.new('RGB',(width,height),(24,26,25));draw=ImageDraw.Draw(board)
for row_index,row in enumerate(rows):
    y=row_index*360
    draw.text((12,y+4),row['name']+' · '+row['characterData'],font=font,fill=(226,220,198))
    x=12
    for key,w in columns:
        draw.text((x+4,y+38),key,font=small,fill=(177,177,160))
        name=row['images'].get(key)
        if name:
            image=Image.open(PROJECT/name).convert('RGBA')
            if key not in ('Selection','Card','Portrait / Front'):
                box=image.getchannel('A').point(lambda p:255 if p>64 else 0).getbbox()
                if box:image=image.crop(box)
            image.thumbnail((w-12,286))
            board.paste(image,(x+(w-image.width)//2,y+70+(286-image.height)//2),image)
        x+=w
draw.text((12,2170),'타이틀 소스/저장 활성상태 · 전투 이미지는 검토를 위해 투명 여백만 잘라 표시 · 실제 UI 배치 캡처가 아님',font=font,fill=(226,220,198))
for index,item in enumerate(title):
    x=12+(index%3)*740;y=2220+(index//3)*375
    image=Image.open(PROJECT/item['image']).convert('RGBA');image.thumbnail((715,300))
    board.paste(image,(x,y),image)
    draw.text((x,y+310),Path(item['image']).name,font=small,fill=(200,195,175))
    draw.text((x,y+337),'saved active='+str(item['savedActiveInHierarchy']),font=small,fill=(200,195,175))
board.save(HERE/'GlobalPlayerOutgameReview.jpg',quality=94)
print(json.dumps({'activeContent':result['sameGameContent'],'characters':[{'name':r['name'],'selection':r['images']['Selection'],'portrait':r['images']['Portrait / Front'],'combat':r['images']['idle']} for r in rows],'title':title},ensure_ascii=False,indent=2))
