from pathlib import Path
import json, shutil
OUT=Path(__file__).parent; ROOT=OUT.parents[2]
data=json.loads((OUT/'ArtReady.staging.json').read_text(encoding='utf-8'))
for art in data['art']:
    if art['name']=='묶인훈련인형': art['aftermathHeight']=330
    if art['name']=='깃발곁의가시등짐승':
        path='Assets/Art/World/Exploration/T_Sprite_Monster_깃발곁의가시등짐승_UsedFront.png'
        shutil.copyfile(ROOT/'Assets/Art/Combat/NativeMonsterFrames/T_Sprite_Monster_가시등시련수_Dead.png',ROOT/path)
        art['usedFront']=art['usedSide']=art['usedRear']=path
data['camps']=[]
for camp in json.loads((OUT/'Camps.json').read_text(encoding='utf-8')):
    shutil.copyfile(camp['source'],ROOT/camp['path'])
    data['camps'].append({'act':camp['act'],'path':camp['path']})
# Preserve the pre-install data and scene state, including earlier designer changes.
paths=list((ROOT/'Assets/Data/Dungeon/Events').glob('*.asset'))
paths += [ROOT/p for p in ['Assets/Scenes/MainGameScene.unity','Assets/Scenes/Trials/Angular01_Main.unity','Assets/Data/Localization/kr.csv','Assets/Data/Localization/Resources/BehemothKr.asset']]
for path in paths:
    target=OUT/'Before'/path.relative_to(ROOT)
    if not target.exists(): target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(path,target)
(OUT/'ArtReady.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
print(len(data['art']),'directional subjects;',len(data['camps']),'camp interiors; ready for saved-asset installation')
