"""Collect orphan presentation libraries as well as their obsolete artwork."""
import collections
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
rows = json.loads((HERE.parent / 'Inventory.json').read_text(encoding='utf-8'))['rows']
by = {row['guid']: row for row in rows}

def protected(path):
    return (path.startswith(('Assets/Art/References/', 'Assets/Art/Fonts/', 'Assets/Art/TextMeshPro/', 'Assets/Art/Shaders/'))
            or path.startswith('Assets/Art/Monsters/') and '/Sources/' in path
            or path == 'Assets/Art/Trials/Angular01/StyleCorrections03/T_Sprite_Character_Samurai_Selection.png')

def presentation(path):
    return path.startswith('Assets/Data/Presentation/') and path.endswith('.asset')

# Playable + saved authoring scenes, prefabs and gameplay data are roots. A presentation
# asset must have an actual incoming use; merely existing is not evidence of use.
roots = [r['guid'] for r in rows if protected(r['path']) or
         not r['path'].startswith('Assets/Art/') and not presentation(r['path'])]
used, todo = set(), roots[:]
while todo:
    guid = todo.pop()
    if guid in used or guid not in by:
        continue
    used.add(guid)
    todo.extend(by[guid]['dependencies'])
delete = [r for r in rows if r['guid'] not in used and
          (presentation(r['path']) or r['path'].startswith('Assets/Art/') and r['type'] in ('Texture2D', 'Sprite', 'Material'))]
delete_paths = {r['path'] for r in delete}
delete_guids = {r['guid'] for r in delete}
incoming = [(r['path'], by[g]['path']) for r in rows if r['path'] not in delete_paths for g in r['dependencies'] if g in delete_guids]
if incoming:
    raise ValueError('Retained references to deletion: ' + str(incoming))
moves = []
for row in rows:
    if row['path'].startswith('Assets/Art/Enemies/Combat/') and row['path'] not in delete_paths:
        # Remaining training paintings genuinely back three live native Sprite libraries.
        if '/경량훈련인형/' not in row['path']:
            raise ValueError('Unexpected live legacy dependency: ' + row['path'])
        moves.append({'oldPath': row['path'], 'path': 'Assets/Art/Monsters/경량훈련인형/' + Path(row['path']).name, 'guid': row['guid']})
plan = {'moves': sorted(moves, key=lambda r: r['oldPath']), 'delete': sorted(delete_paths)}
(HERE / 'Plan.json').write_text(json.dumps(plan, ensure_ascii=False, indent=2), encoding='utf-8')
(HERE / 'DeletionAudit.md').write_text('# 재정리 삭제 근거\n\n첫 정리에서 모든 데이터 에셋을 사용 루트로 잡아 고아 애니메이션 라이브러리가 구형 그림을 보존한 것이 오류였다. 이번에는 프레젠테이션 에셋 자체를 루트로 인정하지 않는다. 저장된 씬·프리팹·게임플레이 데이터에서 도달할 수 없는 프레젠테이션 라이브러리와 그림을 함께 제거한다. 보존한 에셋에서 삭제 대상으로 향하는 GUID 참조는 0개다.\n\n' + '\n'.join('- `' + p + '`' for p in sorted(delete_paths)) + '\n', encoding='utf-8')
summary = {'deletedArt': sum(r['path'].startswith('Assets/Art/') for r in delete), 'deletedPresentation': sum(presentation(r['path']) for r in delete),
           'activeTrainingImagesMoved': len(moves), 'retainedReferencesToDeleted': len(incoming),
           'deletedLegacyCombatImages': sum(r['path'].startswith('Assets/Art/Enemies/Combat/') for r in delete)}
(HERE / 'PlanSummary.json').write_text(json.dumps(summary, indent=2), encoding='utf-8')
print(json.dumps(summary, indent=2))
print('Obsolete presentation asset types:', dict(collections.Counter(r['type'] for r in delete if presentation(r['path']))))
