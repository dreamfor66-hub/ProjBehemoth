"""Update active code and editing guides; keep historical logs and GUID aliases intact."""
import json
import re
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
plan = json.loads((HERE / 'Plan.json').read_text(encoding='utf-8'))
mapping = {m['oldPath']: m['path'] for m in plan['moves']}
files = list((ROOT / 'Assets/Editor').rglob('*.cs')) + list((ROOT / 'Assets/Scripts').rglob('*.cs'))
files += [ROOT / 'Assets/Art/AGENTS.md', ROOT / 'Assets/Art/References/AngularCharacterStyle/STYLE_GUIDE.md',
          ROOT / 'Docs/Production/CombatAndContentRevision-2026-10-31/ArtGuide.md',
          ROOT / 'Docs/Production/CombatAndContentRevision-2026-10-31/ArtPlan.json']
changes = []
for path in files:
    if path.name in ('DesignerAssetPaths.cs', 'ArtResourceMaintenance.cs'):
        continue
    raw = path.read_bytes()
    old = raw.decode('utf-8-sig')
    text = old
    for source, target in mapping.items():
        text = text.replace(source, target)
    if path.suffix == '.cs':
        # Historical editor recipes also construct paths dynamically. Resolve those by GUID aliases.
        text = re.sub(r'(?<![\w.])(?:UnityEditor\.)?AssetDatabase\.LoadAssetAtPath<(Sprite|Texture2D)>',
                      r'DungeonSlash.DesignerAssetPaths.Load<\1>', text)
    if path.name == 'AGENTS.md':
        text = text.replace('Warrior-Idle.png', 'T_Sprite_Character_Warrior_Idle.png')
        text = text.replace('Current StyleCorrections03/Samurai.png', 'Current StyleCorrections03/T_Sprite_Character_Samurai_Selection.png')
    if text == old:
        continue
    backup = HERE / 'BeforeCode' / path.relative_to(ROOT)
    backup.parent.mkdir(parents=True, exist_ok=True)
    if not backup.exists():
        backup.write_bytes(raw)
    # Preserve existing line endings and BOM, including files containing designer edits.
    if b'\r\n' in raw:
        text = text.replace('\r\n', '\n').replace('\n', '\r\n')
    path.write_bytes((b'\xef\xbb\xbf' if raw.startswith(b'\xef\xbb\xbf') else b'') + text.encode('utf-8'))
    changes.append(path.relative_to(ROOT).as_posix())
(HERE / 'UpdatedCode.json').write_text(json.dumps(changes, ensure_ascii=False, indent=2), encoding='utf-8')
print('Updated', len(changes), 'code/guide files')
