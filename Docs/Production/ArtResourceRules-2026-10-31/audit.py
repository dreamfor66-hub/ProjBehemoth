"""Independent disk, alias and serialized-reference audit after Unity maintenance."""
import json
import re
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
if '--correction' in sys.argv:
    HERE = HERE / 'Correction'
read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))
plan = read(HERE / 'Plan.json')
before = read(HERE / 'Before.json')['rows']
after = read(HERE / 'After.json')['rows']
current = {r['guid']: r for r in after}
deleted = {r['guid'] for r in before if r['path'] in plan['delete']}
index = read(ROOT / 'Docs/Production/PlayabilityRevision-2026-10-30/DataNames.json')['entries']
aliases = {alias: row['guid'] for row in index for alias in [row['oldPath'], row['path'], *(row.get('aliases') or [])]}
errors = []
for move in plan['moves']:
    path = ROOT / move['path']
    if not path.exists():
        errors.append('Missing moved file: ' + move['path'])
        continue
    meta = path.with_name(path.name + '.meta').read_text(encoding='utf-8-sig')
    if not re.search(r'^guid: ' + move['guid'] + '$', meta, re.M):
        errors.append('Moved file GUID mismatch: ' + move['path'])
    if aliases.get(move['oldPath']) != move['guid']:
        errors.append('Missing old-path alias: ' + move['oldPath'])
for path in plan['delete']:
    file = ROOT / path
    if file.exists() or file.with_name(file.name + '.meta').exists():
        errors.append('Deleted file or meta remains: ' + path)
for row in before:
    if row['guid'] in deleted:
        continue
    value = current.get(row['guid'])
    if not value or row['dependencies'] != value['dependencies'] or row['sprites'] != value['sprites']:
        errors.append('Retained dependency/geometry mismatch: ' + row['path'])
serialized = 0
guid_pattern = re.compile(rb'\bguid:\s*([0-9a-f]{32})')
for path in (ROOT / 'Assets').rglob('*'):
    if path.suffix not in ('.unity', '.prefab', '.asset', '.mat', '.controller', '.overrideController', '.anim', '.meta', '.spriteatlas'):
        continue
    serialized += 1
    references = {g.decode('ascii') for g in guid_pattern.findall(path.read_bytes())}
    if references & deleted:
        errors.append('Serialized reference to deleted GUID: ' + path.relative_to(ROOT).as_posix())
report = {'renamed': len(plan['moves']), 'deleted': len(plan['delete']),
          'retainedAssetsVerified': len(before) - len(deleted),
          'oldPathAliasesVerified': len(plan['moves']), 'serializedFilesScanned': serialized,
          'errors': errors}
(HERE / 'Audit.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(report, ensure_ascii=True, indent=2))
if errors:
    raise SystemExit(1)
