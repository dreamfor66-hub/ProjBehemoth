"""Record reviewed PNGs and their native Sprite regions; never repaint source images."""
from pathlib import Path
import hashlib
import json
import shutil
from PIL import Image, ImageDraw

folder = Path('Docs/Production/MonsterStun-2026-10-30')
roster = json.loads((folder / 'Roster.json').read_text(encoding='utf-8'))
correction = json.loads((folder / 'BoneScorpion-Correction.json').read_text(encoding='utf-8'))
shutil.copy2(correction['file'], 'Assets/Art/World/ActOne/Stunned/BoneScorpion-Stunned.png')
entries, hashes = [], {}
for row in roster:
    name = Path(row['idle']).stem.replace('-Idle', '')
    path = Path('Assets/Art/World/ActOne/Stunned') / (name + '-Stunned.png')
    image = Image.open(path).convert('RGBA')
    bounds = image.getchannel('A').point(lambda value: 255 if value >= 16 else 0).getbbox()
    assert bounds and image.getchannel('A').getextrema()[0] == 0, path
    left, top, right, bottom = bounds
    left, top = max(0, left - 2), max(0, top - 2)
    right, bottom = min(image.width, right + 2), min(image.height, bottom + 2)
    entry = dict(monster='Assets/' + row['monster'], image=path.as_posix(),
                 rect=dict(x=left, y=image.height-bottom, width=right-left, height=bottom-top))
    # Reviewed head/helmet widths normalized to each source canvas. A folded pose
    # must not enlarge the character merely to consume the available canvas.
    if name == 'MirrorShell':
        entry.update(referenceFeatureFraction=165/1280, stunnedFeatureFraction=246/1280)
    if name == 'CoffinSlime':
        entry.update(referenceFeatureFraction=160/1280, stunnedFeatureFraction=210/1280)
    entries.append(entry)
    hashes[row['monster']] = hashlib.sha256(Path('Assets', row['monster']).read_bytes()).hexdigest()

(folder/'Install.json').write_text(json.dumps(dict(actors=entries), indent=2), encoding='utf-8')
(folder/'MonsterDataHashes.json').write_text(json.dumps(hashes, indent=2), encoding='utf-8')

for color, label in [((44,44,44),'Dark'), ((215,215,215),'Light')]:
    board = Image.new('RGB', (1440,1440), color)
    draw = ImageDraw.Draw(board)
    for index, row in enumerate(roster):
        name = Path(row['idle']).stem.replace('-Idle','')
        x, y = index % 3 * 480, index // 3 * 360
        for side, path in enumerate(['Assets/' + row['idle'], entries[index]['image']]):
            image = Image.open(path).convert('RGBA')
            image.thumbnail((230,310))
            board.paste(image, (x+side*240+(240-image.width)//2, y+35+(310-image.height)//2), image)
        draw.text((x+8,y+8), name+' | Idle / Stunned', fill='white' if label=='Dark' else 'black')
    board.save(folder / ('PoseReview-'+label+'.jpg'))

print('12 transparent paintings, native Sprite regions and source hashes recorded.')
