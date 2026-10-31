from pathlib import Path
import re,json
folder=Path(__file__).resolve().parent
root=folder.parents[2]
before=folder/'Before/UnsavedMainGameScene.unity'
if not before.exists():before=folder/'Before/Assets/Scenes/MainGameScene.unity'
def layouts(path):
 text=path.read_text(encoding='utf-8-sig');blocks={}
 for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)\n(.*?)(?=^--- !u!|\Z)',text,re.M|re.S):blocks[m[2]]=(int(m[1]),m[3])
 def ref(block,key):return re.search(r'^  '+key+r': \{fileID: (-?\d+)',block,re.M)[1]
 def transform(go):return next((key,b) for key,(kind,b) in blocks.items() if kind==224 and ref(b,'m_GameObject')==go)
 hud=next(b for _,b in blocks.values() if '\n  playerHp:' in b)
 hp=blocks[ref(hud,'playerHp')][1];hp_go=ref(hp,'m_GameObject');_,hp_transform=transform(hp_go);hp_root=blocks[ref(hp_transform,'m_Father')][1]
 mana=next(b for _,b in blocks.values() if '\n  spellbook:' in b and '\n  gauge:' in b and '\n  fill:' in b)
 _,mana_root=transform(ref(mana,'gauge'))
 keys=['m_AnchorMin','m_AnchorMax','m_Pivot','m_AnchoredPosition','m_SizeDelta','m_LocalScale']
 return {name:{key:re.search(r'^  '+key+r': (.+)$',block,re.M)[1] for key in keys} for name,block in [('HP',hp_root),('MP',mana_root)]}
original=layouts(before);current=layouts(root/'Assets/Scenes/MainGameScene.unity')
report={'baseline':str(before),'HP_unchanged':original['HP']==current['HP'],'MP_unchanged':original['MP']==current['MP'],'before':original,'after':current}
(folder/'LayoutPreservation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in report.items() if k not in ['before','after']},ensure_ascii=False))
assert report['HP_unchanged'] and report['MP_unchanged']
