from pathlib import Path
import re
for root in ('Assets/Scripts','Assets/Editor','Assets/Tests'):
 for p in Path(root).rglob('*.cs'):
  s=p.read_text(encoding='utf-8-sig')
  n=re.sub(r'([\w.]+)\.HitBoxBounds\.extents\.y',r'(-\1.FootprintY)',s)
  if p.name in ('MonsterGroundingLiveProbe.cs','CombatFocusLiveProbe.cs'):
   n=re.sub(r',\s*scale\s*=\s*[\d.]+f?', '', n)
  if p.name=='MonsterGroundingLiveProbe.cs':
   a=n.index('        var renderedHeight =')
   b=n.index('        Check(Mathf.Abs(artBottom',a)
   n=n[:a]+'''        var artBottom = Center(index).y + data.ArtworkBounds.yMin * runtime.VisualScale;
'''+n[b:]
  if n!=s:p.write_text(n,encoding='utf-8')
