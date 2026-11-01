"""Record installed source identity; no raster changes or Unity mutations."""
from pathlib import Path
import hashlib,json,re
from PIL import Image
folder=Path(__file__).resolve().parent;root=folder.parents[2]
background=json.loads((folder/'BackgroundArtInstalled.json').read_text(encoding='utf-8'))['files']
dog=json.loads((folder/'Dog/InstalledPaintings.json').read_text(encoding='utf-8'))
refs=root/'Assets/Art/References/DarkestDungeonVisual'
reference=json.loads((refs/'Manifest.json').read_text(encoding='utf-8'))['files']
for entry in reference:
    assert hashlib.sha256((refs/entry['file']).read_bytes()).hexdigest()==entry['sha256'],entry['file']
paths=[entry['path'] for entry in background]+[entry['production'] for entry in dog]
props=['Goddess_Front','Goddess_Side','Fountain_Front','Fountain_Side','Fountain_UsedFront','Fountain_UsedSide','분수_rear','분수_usedRear']
paths += ['Assets/Art/World/Exploration/T_UI_World_'+name+'.png' for name in props]
doors=json.loads((folder/'DoorSeams/Installed.json').read_text(encoding='utf-8'))
paths += [Path(entry['path']).relative_to(root).as_posix() for entry in doors]
rows=[]
for path in paths:
    file=root/path;meta=Path(str(file)+'.meta');guid=re.search(r'^guid: (.+)$',meta.read_text(encoding='utf-8'),re.M).group(1)
    image=Image.open(file)
    rows.append({'path':path,'sha256':hashlib.sha256(file.read_bytes()).hexdigest(),'guid':guid,'canvas':list(image.size)})
for entry in background:
    assert hashlib.sha256((root/entry['path']).read_bytes()).hexdigest()==entry['sha256'],entry['path']
for entry in dog:
    assert hashlib.sha256((root/entry['production']).read_bytes()).hexdigest()==entry['sha256'],entry['production']
    assert hashlib.sha256((root/(entry['production']+'.meta')).read_bytes()).hexdigest()==entry['metaSha256'],entry['production']
for entry in doors:
    file=Path(entry['path'])
    assert hashlib.sha256(file.read_bytes()).hexdigest()==entry['sha256'],entry['path']
    assert hashlib.sha256(Path(str(file)+'.meta').read_bytes()).hexdigest()==entry['metaSha256'],entry['path']
for entry in json.loads((folder/'Props/ArtReady.json').read_text(encoding='utf-8'))['files']:
    assert hashlib.sha256((root/entry['path']).read_bytes()).digest()==hashlib.sha256(Path(entry['source']).read_bytes()).digest(),entry['path']
    assert (root/(entry['path']+'.meta')).read_bytes()==(folder/'Props/Before'/(entry['path']+'.meta')).read_bytes(),entry['path']
for entry in background:
    prior=folder/'Before'/(entry['path']+'.meta')
    if prior.exists():
        assert prior.read_bytes()==(root/(entry['path']+'.meta')).read_bytes(),entry['path']
report={'references':{'count':len(reference),'exactOriginalHashes':True},'installedPaintings':rows}
(folder/'InstalledArtManifest.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print('Verified',len(reference),'reference originals;',len(rows),'installed paintings recorded; generated source hashes and door meta match.')
