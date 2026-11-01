from pathlib import Path
import hashlib, json, shutil
from PIL import Image
folder=Path(__file__).resolve().parent
root=folder.parents[2]
manifest=json.loads((folder/'BackgroundArtReady.json').read_text(encoding='utf-8'))
for entry in manifest['files']:
    src=Path(entry['source']);dst=root/entry['path'];oldsize=None
    if dst.exists():
        oldsize=Image.open(dst).size
        backup=folder/'Before'/entry['path'];backup.parent.mkdir(parents=True,exist_ok=True)
        if not backup.exists():shutil.copy2(dst,backup)
        meta=dst.with_suffix(dst.suffix+'.meta')
        if meta.exists() and not Path(str(backup)+'.meta').exists():shutil.copy2(meta,Path(str(backup)+'.meta'))
    generated=Image.open(src)
    assert generated.height==941 and abs(generated.width-1672)<=1, (src,generated.size)
    if oldsize is not None:assert oldsize==(1672,941), (dst,oldsize)
    dst.parent.mkdir(parents=True,exist_ok=True)
    if generated.size==(1672,941):shutil.copy2(src,dst)
    else:
        # One generator output is one pixel narrower. Pad the outermost dark
        # edge; do not rescale perspective or repaint any source pixel.
        fitted=Image.new(generated.mode,(1672,941));fitted.paste(generated,(0,0));fitted.save(dst)
        entry['mechanical_fit']='1671x941 to1672x941; one black pixel at right edge; no rescale'
    entry['sha256']=hashlib.sha256(dst.read_bytes()).hexdigest()
    entry['canvas']=list(Image.open(dst).size)
(folder/'BackgroundArtInstalled.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print('Installed',len(manifest['files']),'background PNGs without pixel repainting or canvas change.')
