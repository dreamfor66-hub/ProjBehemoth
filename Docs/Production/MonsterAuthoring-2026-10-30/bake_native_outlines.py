"""One-time Sprite-native vertex migration; no pixels, UVs or frame rectangles change.

Unity 6000.0 refuses OverrideGeometry outside Play mode. Its serialized Sprite
vertex stream is explicit: stream 0=float3 positions, stream 1=float2 UVs.
Only stream 0's Y bounds are baked here; the production UI uses the full rect.
"""
from pathlib import Path
import json,re,struct
folder=Path('Docs/Production/MonsterAuthoring-2026-10-30')
assert (folder/'NativeRectanglesComplete.txt').exists()
count=0
for record in json.loads((folder/'Baseline.json').read_text(encoding='utf-8'))['monsters']:
 for frame in record['frames']:
  name=Path(record['path']).stem+'-'+frame['state']
  path=Path('Assets/Art/Combat/NativeMonsterFrames')/(name+'.asset')
  s=path.read_text(encoding='utf-8-sig')
  assert re.search(r'm_VertexCount: 4\b',s), name
  y0=max(-record['oldHalfHeight'],frame['renderedRect']['y'])/100
  y1=min(frame['labelY']-(36 if record['tight'] else 65),frame['renderedRect']['y']+frame['renderedRect']['height'])/100
  def bake(m):
   raw=bytearray.fromhex(m[1]); assert len(raw)==80,(name,len(raw))
   ys=[struct.unpack_from('<f',raw,i*12+4)[0] for i in range(4)]
   for i,y in enumerate(ys):
    struct.pack_into('<f',raw,i*12+4,y0 if y==min(ys) else y1)
    if record['tight']:
     x=struct.unpack_from('<f',raw,i*12)[0]
     struct.pack_into('<f',raw,i*12,(-1 if x<0 else 1)*record['oldArtSize']['x']/200)
   return '_typelessdata: '+raw.hex()
  s=re.sub(r'_typelessdata: ([0-9a-f]+)',bake,s)
  path.write_text(s,encoding='utf-8');count+=1
(folder/'NativeOutlinesComplete.txt').write_text(f'{count} native Sprite vertex streams baked; rect/pivot/PPU/UV/texture bytes preserved.\n')
