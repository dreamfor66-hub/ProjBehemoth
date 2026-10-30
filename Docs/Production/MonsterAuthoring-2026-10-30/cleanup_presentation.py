from pathlib import Path
import re
folder=Path('Docs/Production/MonsterAuthoring-2026-10-30')
p=Path('Assets/Scripts/Combat/Presentation/CombatSpriteAnimation.cs');s=p.read_text(encoding='utf-8-sig')
a=s.index('        [Range(0f, 1f), Tooltip("Idle ground-contact');b=s.index('        [HideInInspector] public float readyTilt;',a)
s=s[:a]+s[b:];p.write_text(s,encoding='utf-8')
p=Path('Assets/Scripts/Combat/Presentation/MonsterView.cs');s=p.read_text(encoding='utf-8-sig').replace('        public float arcadeArtScale=1.65f;\n','');p.write_text(s,encoding='utf-8')
p=Path('Assets/Editor/Presentation/AngularWorldPreparation.cs');s=p.read_text(encoding='utf-8-sig');s=re.sub(r'^.*animation\.labelHeight=.*\n','',s,flags=re.M);p.write_text(s,encoding='utf-8')
p=Path('Assets/Editor/Presentation/MonsterGroundingPreparation.cs');backup=folder/'Before'/p;backup.parent.mkdir(parents=True,exist_ok=True)
if not backup.exists():backup.write_bytes(p.read_bytes())
p.write_text('// Historical one-time artwork installer retired. Native Sprite geometry now supplies automatic artwork bounds.\n',encoding='utf-8')
for p in Path('Assets/Data/Presentation').rglob('*.asset'):
 s=p.read_text(encoding='utf-8-sig')
 if not re.search(r'^  (groundContactY|artworkTopY|labelHeight):',s,flags=re.M):continue
 backup=folder/'Before'/p;backup.parent.mkdir(parents=True,exist_ok=True)
 if not backup.exists():backup.write_bytes(p.read_bytes())
 s=re.sub(r'^  (groundContactY|artworkTopY|labelHeight):.*\n','',s,flags=re.M);p.write_text(s,encoding='utf-8')
