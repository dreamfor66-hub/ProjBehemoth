"""Independent audit of editable textures, native mesh preservation and removed GUIDs."""
import json
import re
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
plan = json.loads((HERE / 'Plan.json').read_text(encoding='utf-8'))
errors, source_guids = [], {}
texture_guid = re.compile(r'\btexture:\s*\{fileID: 2800000, guid: ([0-9a-f]{32})')
buffers = re.compile(r'(?:m_IndexBuffer|_typelessdata): ([0-9a-f]+)')
for entry in plan['entries']:
    path = ROOT / entry['sprite']
    original = (HERE / 'Before' / entry['sprite']).read_text(encoding='utf-8-sig')
    current = path.read_text(encoding='utf-8-sig')
    source_guids[entry['source']] = texture_guid.search(original).group(1)
    png = ROOT / entry['png']
    guid = re.search(r'^guid: ([0-9a-f]{32})$', png.with_name(png.name + '.meta').read_text(), re.M).group(1)
    native_guid = re.search(r'^guid: ([0-9a-f]{32})$', path.with_name(path.name + '.meta').read_text(), re.M).group(1)
    if native_guid != entry['guid'] or set(texture_guid.findall(current)) != {guid}:
        errors.append('Reference mismatch: ' + entry['sprite'])
    if buffers.findall(original) != buffers.findall(current):
        errors.append('Changed original native mesh/index data: ' + entry['sprite'])
deleted = {source_guids[path] for path in plan['removedSources']}
pattern = re.compile(rb'\bguid:\s*([0-9a-f]{32})')
scanned = 0
for path in (ROOT / 'Assets').rglob('*'):
    if path.suffix not in ('.asset', '.meta', '.prefab', '.unity', '.mat', '.anim', '.controller', '.spriteatlas'):
        continue
    scanned += 1
    if {g.decode('ascii') for g in pattern.findall(path.read_bytes())} & deleted:
        errors.append('Reference to removed source GUID: ' + path.relative_to(ROOT).as_posix())
for path in plan['removedSources']:
    file = ROOT / path
    if file.exists() or file.with_name(file.name + '.meta').exists():
        errors.append('Unused former source remains: ' + path)
report = {'editableTexturesCreated': len(plan['entries']), 'unusedFormerSourcesRemoved': len(plan['removedSources']),
          'higherResolutionSourcesPreserved': len(plan.get('resizedSources', [])),
          'originalNativeMeshesAndGuidsVerified': len(plan['entries']), 'serializedFilesScanned': scanned, 'errors': errors}
(HERE / 'DiskAudit.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(report, ensure_ascii=True, indent=2))
if errors:
    raise SystemExit(1)
