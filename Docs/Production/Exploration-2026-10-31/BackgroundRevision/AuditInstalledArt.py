"""Read-only installed background audit; preserves all production images and metadata."""
from pathlib import Path
import json,hashlib,re
from PIL import Image
root=Path(__file__).parent
project=root.parents[3]
assets=project/'Assets'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def guid(p):return re.search(r'^guid: (\w+)',p.read_text(encoding='utf-8'),re.M)[1]
report=[]
for act in (2,3):
 for part in ('Walls','Forward','Left','Right'):
  p=assets/f'Art/World/Angular/Backdrops/B{act}-{part}.png'
  before=root/'Before'/p.name
  im=Image.open(p)
  entry={'path':str(p),'size':im.size,'mode':im.mode,'sha256':sha(p),'changed':sha(p)!=sha(before),
   'guid':guid(Path(str(p)+'.meta')),'guidPreserved':guid(Path(str(p)+'.meta'))==guid(Path(str(before)+'.meta'))}
  if 'A' in im.getbands():entry['alphaRange']=im.getchannel('A').getextrema()
  report.append(entry)
assert len(report)==8 and all(r['changed'] and r['guidPreserved'] for r in report)
(root/'InstalledArt.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print('8 replacement PNGs; all original Unity GUIDs preserved; editable PNG paintings.')
