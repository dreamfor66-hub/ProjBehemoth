"""Read-only raster checks; writes only the JSON report."""
import json
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
MANIFEST = ROOT / 'Assets/Art/DarkSeven/Import.json'
INKS = {(0,0,0),(25,21,31),(51,44,59),(100,88,107),(163,152,170),(238,229,213),(239,101,72)}
rows = []
for entry in json.loads(MANIFEST.read_text(encoding='utf-8-sig'))['entries']:
    if '/CharacterRedesign/' not in entry['source']:
        continue
    image = Image.open(ROOT / entry['target']).convert('RGBA')
    colors = set(image.getdata())
    rows.append(dict(path=entry['target'], size=list(image.size),
                     dimensionsPreserved=image.size == (entry['width'],entry['height']),
                     colors=len({p[:3] for p in colors if p[3]}),
                     outside=sum(1 for p in colors if p[3] and p[:3] not in INKS),
                     binaryAlpha=all(p[3] in (0,255) for p in colors),
                     baked=entry['hash'].startswith('native-regions-1/')))
Path(__file__).with_name('AssetAudit.json').write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8')
assert len(rows)==66 and all(r['dimensionsPreserved'] and r['colors']<=7 and r['outside']==0 and r['binaryAlpha'] and r['baked'] for r in rows)
print('PASS: 66 native-size character PNGs; <=7 inks; binary alpha; current source hashes.')
