from pathlib import Path
from PIL import Image
import json, shutil, hashlib
HERE=Path(__file__).resolve().parent
PROJECT=HERE.parents[3]
OUT=HERE/'WizardInputs'
OUT.mkdir(exist_ok=True)
paths=['Assets/Art/UI/Outgame/T_UI_CharacterSelection_Wizard.png','Assets/Art/UI/Outgame/T_UI_CharacterButton_Wizard.png','Assets/Art/World/Angular/Heroes/T_Sprite_Character_Wizard_Portrait.png']+[f'Assets/Art/World/Angular/States/Heroes/T_Sprite_Character_Wizard_{p}.png' for p in ['Idle','Ready','Attack','Guard']]
records=[]
for rel in paths:
    source=PROJECT/rel
    backup=HERE.parent/'Before'/rel
    backup.parent.mkdir(parents=True,exist_ok=True)
    for p in [source,source.with_suffix('.png.meta'),source.with_suffix('.asset'),source.with_suffix('.asset.meta')]:
        b=HERE.parent/'Before'/p.relative_to(PROJECT)
        if p.exists() and not b.exists():shutil.copy2(p,b)
    im=Image.open(backup).convert('RGBA')
    bounds=im.getchannel('A').point(lambda v:255 if v>32 else 0).getbbox()
    if '/States/' in rel:
        cropped=OUT/source.name
        im.crop(bounds).save(cropped)
    else:cropped=backup
    records.append({'production':rel,'backup':str(backup),'canvas':im.size,'alphaBounds':bounds,'editTarget':str(cropped),'sha256':hashlib.sha256(backup.read_bytes()).hexdigest()})
(HERE/'WizardOriginals.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(records,ensure_ascii=False,indent=2))
