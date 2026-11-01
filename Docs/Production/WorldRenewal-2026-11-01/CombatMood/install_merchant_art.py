"""Mechanical installation only: preserve source canvases, asset metadata and existing portrait crops.

No repainting, color processing, filtering for style, or generated pose transforms.
The sole permitted framing compensation is one static uniform fit into the original PNG canvas.
"""
from pathlib import Path
import hashlib
import json
import shutil
import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
PROJECT = HERE.parents[3]
BEFORE = HERE.parent / 'Before'

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def alpha_bounds(image):
    return Image.fromarray((np.asarray(image)[:, :, 3] > 32).astype('uint8') * 255).getbbox()

def backup(path):
    relative = path.relative_to(PROJECT)
    for source in (path, path.with_suffix('.png.meta'), path.with_suffix('.asset'), path.with_suffix('.asset.meta')):
        if not source.exists():
            continue
        target = BEFORE / source.relative_to(PROJECT)
        target.parent.mkdir(parents=True, exist_ok=True)
        if not target.exists():
            shutil.copy2(source, target)
    return BEFORE / relative

def fit_on_source_canvas(generated, original):
    old = alpha_bounds(original)
    new = alpha_bounds(generated)
    ow, oh = old[2] - old[0], old[3] - old[1]
    nw, nh = new[2] - new[0], new[3] - new[1]
    scale = min(ow / nw, oh / nh)
    resized = generated.resize((round(generated.width * scale), round(generated.height * scale)), Image.Resampling.LANCZOS)
    x = round((old[0] + old[2]) / 2 - (new[0] + new[2]) / 2 * scale)
    y = round(old[3] - new[3] * scale)
    canvas = Image.new('RGBA', original.size)
    canvas.alpha_composite(resized, (x, y))
    return canvas, {'uniformScale': scale, 'offset': [x, y], 'originalAlphaBounds': old, 'generatedAlphaBounds': new}

def read_cv(path):
    return cv2.imdecode(np.fromfile(path, dtype=np.uint8), cv2.IMREAD_GRAYSCALE)

selected = json.loads((HERE / 'MerchantSelectedSources.json').read_text(encoding='utf-8-sig'))
manifest = []
for item in selected:
    target = PROJECT / item['production']
    original_path = backup(target)
    original = Image.open(original_path).convert('RGBA')
    generated_path = Path(item['generated'])
    generated = Image.open(generated_path).convert('RGBA')
    unchanged = {str(p.relative_to(PROJECT)): sha(p) for p in (target.with_suffix('.png.meta'), target.with_suffix('.asset'), target.with_suffix('.asset.meta')) if p.exists()}
    output, framing = fit_on_source_canvas(generated, original)
    output.save(target)
    assert output.size == original.size
    for p, digest in unchanged.items():
        assert sha(PROJECT / p) == digest, p + ' metadata changed'
    manifest.append({**item, 'sourceBackup': str(original_path), 'originalSha256': sha(original_path), 'generatedSha256': sha(generated_path), 'productionSha256': sha(target), 'canvas': list(original.size), 'framing': framing, 'preservedMetadata': unchanged})

for actor in dict.fromkeys(item['actor'] for item in selected):
    idle = PROJECT / next(item['production'] for item in selected if item['actor'] == actor and item['pose'] == 'Idle')
    original_idle = BEFORE / idle.relative_to(PROJECT)
    portrait = PROJECT / 'Assets/Art/Enemies/Portraits/Catalog' / f'T_Sprite_Monster_{actor}_Portrait.png'
    original_portrait = backup(portrait)
    unchanged = {str(p.relative_to(PROJECT)): sha(p) for p in (portrait.with_suffix('.png.meta'), portrait.with_suffix('.asset'), portrait.with_suffix('.asset.meta')) if p.exists()}
    match = cv2.matchTemplate(read_cv(original_idle), read_cv(original_portrait), cv2.TM_CCOEFF_NORMED)
    _, score, _, pos = cv2.minMaxLoc(match)
    assert score > .999, (actor, score)
    source = Image.open(idle).convert('RGBA')
    size = Image.open(original_portrait).size
    output = source.crop((pos[0], pos[1], pos[0] + size[0], pos[1] + size[1]))
    output.save(portrait)
    for p, digest in unchanged.items():
        assert sha(PROJECT / p) == digest, p + ' metadata changed'
    manifest.append({'actor': actor, 'pose': 'Portrait', 'production': str(portrait.relative_to(PROJECT)).replace('\\', '/'), 'sourceBackup': str(original_portrait), 'originalSha256': sha(original_portrait), 'productionSha256': sha(portrait), 'sourceIdle': str(idle), 'exactOriginalCrop': [*pos, *size], 'originalCropMatch': score, 'canvas': list(size), 'preservedMetadata': unchanged})

(HERE / 'MerchantAssetFiles.json').write_text(json.dumps([item['production'] for item in manifest], ensure_ascii=False, indent=2), encoding='utf-8')
(HERE / 'MerchantInstallation.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')

font_path = Path('C:/Windows/Fonts/malgun.ttf')
font = ImageFont.truetype(str(font_path), 24)
small = ImageFont.truetype(str(font_path), 18)
board = Image.new('RGB', (1800, 1330), (28, 29, 27))
draw = ImageDraw.Draw(board)
for row, actor in enumerate(dict.fromkeys(item['actor'] for item in selected)):
    y = row * 440
    draw.text((14, y + 8), actor, font=font, fill=(224, 218, 194))
    draw.text((14, y + 42), '이전 / 교정 / 독립 자세', font=small, fill=(175, 174, 158))
    poses = [item for item in manifest if item['actor'] == actor and item['pose'] != 'Portrait']
    for index, item in enumerate([{'production': poses[0]['production'], 'pose': 'Before', 'sourceBackup': poses[0]['sourceBackup']}, *poses]):
        image = Image.open(item['sourceBackup'] if index == 0 else PROJECT / item['production']).convert('RGBA')
        image.thumbnail((270, 335))
        x = 14 + index * 294
        board.paste(image, (x + (270-image.width)//2, y + 84 + 335-image.height), image)
        draw.text((x + 8, y + 60), item['pose'], font=small, fill=(218, 215, 196))
board.save(HERE / 'MerchantArtReview.jpg', quality=94)
print(f'Installed {len(selected)} independent paintings and 3 exact portrait crops; all source canvases and native metadata preserved.')
