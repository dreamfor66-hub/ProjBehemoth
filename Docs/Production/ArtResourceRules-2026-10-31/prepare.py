"""Historical first-pass plan. Superseded by Correction/prepare.py: orphan data is not a usage root."""
import collections
import json
import re
import sys
from pathlib import Path

if __name__ == '__main__' and sys.argv[-1] != 'inventory':
    raise SystemExit('Superseded: run Correction/prepare.py. Orphan presentation assets must not be treated as use roots.')

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
inventory = json.loads((HERE / 'Inventory.json').read_text(encoding='utf-8'))
rows = inventory['rows']
byguid = {r['guid']: r for r in rows}
bypath = {r['path']: r for r in rows}
art = [r for r in rows if r['path'].startswith('Assets/Art/')]

def closure(roots):
    found, todo = set(), list(roots)
    while todo:
        guid = todo.pop()
        if guid in found or guid not in byguid:
            continue
        found.add(guid)
        todo.extend(byguid[guid]['dependencies'])
    return found

def protected(path):
    return (path.startswith(('Assets/Art/References/', 'Assets/Art/Fonts/',
                             'Assets/Art/TextMeshPro/', 'Assets/Art/Shaders/'))
            or '/Sources/' in path and path.startswith('Assets/Art/Monsters/')
            or path == 'Assets/Art/Trials/Angular01/StyleCorrections03/Samurai.png')

roots = {r['guid'] for r in rows if not r['path'].startswith('Assets/Art/') or protected(r['path'])}
used = closure(roots)

if sys.argv[-1] == 'inventory':
    print('Playing:', inventory['playing'], 'Assets:', len(rows), 'Art:', len(art))
    for folder in sorted({str(Path(r['path']).parent) for r in art}):
        group = [r for r in art if str(Path(r['path']).parent) == folder]
        print(folder, len(group), 'used', sum(r['guid'] in used for r in group),
              sorted({r['type'] for r in group}))
    (HERE / 'Unreachable.txt').write_text('\n'.join(r['path'] for r in art if r['guid'] not in used), encoding='utf-8')
    sys.exit()

def token(value):
    return re.sub(r'[^\w가-힣]+', '', value).replace('_', '')

def name_for(row):
    p = Path(row['path'])
    path, stem = row['path'], p.stem
    if path.startswith(('Assets/Art/References/', 'Assets/Art/TextMeshPro/', 'Assets/Art/Fonts/')):
        return None
    if row['type'] not in ('Texture2D', 'Sprite'):
        return None
    if stem.startswith(('T_Sprite_', 'T_UI_Icon_', 'T_UI_Common_Rect_')):
        return None
    # Multi-character state sheets are one authored texture, not one monster pose.
    if '/Sheets/' in path:
        return 'T_Sprite_Monster_Shared_' + token(stem)
    if '/Monsters/' in path and '/Sources/' in path:
        monster = p.parent.parent.name
        return 'T_Sprite_Monster_' + token(monster) + '_' + ('BreathSource' if '숨결' in stem else 'StateSheetSource')
    if ('/States/Monsters/' in path or '/NativeMonsterFrames/' in path or '/Archive/PreviousPoses/' in path
            or '/World/ActOne/' in path or path.startswith('Assets/Art/Monsters/')):
        parts = stem.rsplit('-', 1)
        return 'T_Sprite_Monster_' + token(parts[0]) + '_' + token(parts[1] if len(parts) > 1 else 'Idle')
    if '/Enemies/Combat/' in path:
        owner = p.parent.name
        state = stem.removeprefix(owner + '-')
        return 'T_Sprite_Monster_' + token(owner) + '_' + token(state)
    if '/World/Angular/Monsters/' in path:
        return 'T_Sprite_Monster_' + token(stem) + '_Idle'
    if '/Enemies/Portraits/' in path:
        return 'T_Sprite_Monster_' + token(stem) + '_Portrait'
    if '/States/Heroes/' in path or '/States/Companions/' in path:
        owner, state = stem.split('-', 1)
        return 'T_Sprite_Character_' + token(owner) + '_' + token(state)
    if '/Characters/Combat/' in path:
        return 'T_Sprite_Character_' + token(p.parent.name) + '_' + token(stem)
    if '/Characters/Companions/' in path:
        owner = p.parent.name if p.parent.name != 'Companions' else stem
        state = stem if p.parent.name != 'Companions' else 'Portrait'
        return 'T_Sprite_Character_' + token(owner) + '_' + token(state)
    if '/Characters/Portraits/' in path:
        return 'T_Sprite_Character_' + token(p.parent.name) + '_' + token(stem)
    if '/World/Angular/Heroes/' in path:
        owner, state = stem.split('-', 1)
        return 'T_Sprite_Character_' + token(owner) + '_' + token(state)
    if '/Trials/Angular01/' in path and re.fullmatch(r'(Warrior|Rogue|Barbarian|Samurai|Monk|Wizard)(Portrait)?', stem):
        owner = stem.removesuffix('Portrait')
        return 'T_Sprite_Character_' + owner + '_' + ('Portrait' if stem.endswith('Portrait') else 'Selection')
    if path == 'Assets/Art/Trials/Angular01/Rear.png':
        return 'T_Sprite_Character_Warrior_Rear'
    if '/DarkSeven/' in path:
        match = re.fullmatch(r'(Face|Hero|Selection|Enemy)(.+)', stem)
        if match:
            kind, owner = match.groups()
            category = 'Monster' if kind == 'Enemy' else 'Character'
            return 'T_Sprite_' + category + '_' + token(owner) + '_' + {'Face': 'Portrait', 'Hero': 'Idle', 'Selection': 'Selection', 'Enemy': 'Idle'}[kind]
        if stem == 'Logo':
            return 'T_UI_Title_Logo'
    if path.startswith('Assets/Art/UI/'):
        stem = stem.removeprefix('T_UI_')
        if '/Icons/' in path or '/Items2D/' in path or stem.endswith('Icon'):
            return 'T_UI_Icon_' + token(stem.removesuffix('Icon'))
        if '/Portraits/' in path:
            return 'T_UI_Portrait_' + token(stem)
        if '/Outgame/' in path:
            if '-' in stem:
                owner, role = stem.split('-', 1)
                return 'T_UI_' + ('CharacterSelection' if role == 'Illustration' else 'CharacterButton') + '_' + token(owner)
            return 'T_UI_Title_' + token(stem.removeprefix('Title'))
        if '/Events/' in path:
            return 'T_UI_Event_' + token(stem)
        if '/Title/' in path:
            return 'T_UI_Title_' + token(stem)
        if stem in ('Frame', 'PanelFrame'):
            return 'T_UI_Common_Rect_' + token(stem)
        return 'T_UI_Common_' + token(stem)
    return None

deletions = [r['path'] for r in art if r['guid'] not in used
             and (r['type'] in ('Texture2D', 'Sprite', 'Material', 'PaletteData') or r['path'].endswith('/Import.json'))]
moves = []
for row in art:
    if row['path'] in deletions:
        continue
    name = name_for(row)
    if name:
        p = Path(row['path'])
        target = p.with_name(name + p.suffix).as_posix()
        if target != row['path']:
            moves.append({'oldPath': row['path'], 'path': target, 'guid': row['guid']})

existing = set(bypath)
targets = collections.Counter(m['path'].lower() for m in moves)
collisions = [m for m in moves if targets[m['path'].lower()] > 1 or m['path'] in existing]
if collisions:
    raise ValueError('Naming collision: ' + str(collisions))
plan = {'moves': sorted(moves, key=lambda m: m['oldPath']), 'delete': sorted(deletions)}
(HERE / 'Plan.json').write_text(json.dumps(plan, ensure_ascii=False, indent=2), encoding='utf-8')
(HERE / 'DeletionAudit.md').write_text('# 미사용 아트 삭제 근거\n\n모든 Assets의 비아트 에셋(씬·프리팹·데이터 등)을 루트로 Unity의 실제 GUID 의존성 폐쇄를 계산했다. 화풍/사용자 제공 참조 원본, 사용 중인 그림의 편집 소스와 명시된 사무라이 원본은 보존했다. 아래 파일은 그 어느 루트에서도 도달하지 않는다. 동적 로딩의 별도 점검은 CodeLoaders.txt 및 Verification.md에 기록한다.\n\n' + '\n'.join('- `' + p + '`' for p in sorted(deletions)) + '\n', encoding='utf-8')
print('Renames', len(moves), 'Deletes', len(deletions))
print('Deleted per folder:', dict(collections.Counter('/'.join(p.split('/')[:4]) for p in deletions)))
print('Retained unclassified:', '\n'.join(r['path'] for r in art if r['path'] not in deletions and r['type'] in ('Texture2D','Sprite') and name_for(r) is None and not protected(r['path'])))
