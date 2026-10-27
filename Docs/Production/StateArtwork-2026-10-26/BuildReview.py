from pathlib import Path
import json,hashlib,html
from PIL import Image,ImageDraw

folder=Path(__file__).resolve().parent
manifest=json.loads((folder/'Manifest.json').read_text('utf-8'))['actors']
actors=json.loads((folder/'Actors.json').read_text('utf-8'))
names={e['id']:e['name'] for e in actors['monsters']}
names.update(dict(zip(['Warrior','Rogue','Barbarian','Samurai','Monk','Wizard'],['워리어','로그','바바리안','사무라이','몽크','위자드'])))
names['TrailDog']='동행견'
labels={'Idle':'대기','Ready':'준비','Attack':'공격','Guard':'방어','Dead':'사망'}
esc=lambda x:html.escape(str(x),quote=True)
parts=[];installed=0;generated=0;expected=sum(len(a['poses'])+bool(a.get('idleSource')) for a in manifest)
for role,title in [('Player','캐릭터 · Idle / Attack / Guard'),('Monster','몬스터 · Idle / Ready / Attack / Dead'),('Companion','동행견 · Idle / Attack')]:
    parts.append(f'<section id="{role}"><h2>{title}</h2>')
    for actor in [a for a in manifest if a['kind']==role]:
        key=actor['id'];name=names.get(key.split('-',1)[1],key)
        done=(folder/'Installed'/(key+'.txt')).exists();installed+=done
        cells=[]
        idle_copy=folder/'Idle'/(key+'.png')
        if not idle_copy.exists() and actor.get('idleSource'):
            idle_copy=folder.parent.parent.parent/actor['idleSource']
        images=[('Idle',idle_copy)]+[(s['state'],folder/'Outputs'/Path(s['source']).name) for s in actor['poses']]
        for state,path in images:
            if path.exists():
                if state!='Idle' or actor.get('idleSource'):generated+=1
                rev=hashlib.sha256(path.read_bytes()).hexdigest()[:10]
                src=path.relative_to(folder).as_posix()
                image=f'<img loading="lazy" src="{src}?v={rev}" alt="{esc(name)} {state}">'
            else:image='<div class="pending">제작 중</div>'
            cells.append(f'<figure>{image}<figcaption>{labels[state]} · {state}</figcaption></figure>')
        verified=(folder/'Verified'/(key+'.txt')).exists() and done and (folder/'Verified'/(key+'.txt')).read_text()==(folder/'Installed'/(key+'.txt')).read_text()
        status='게임 반영 · 실행 검증 완료' if verified else '게임 반영 완료' if done else '제작·검수 중'
        parts.append(f'<article id="{key}"><header><h3>{esc(name)}</h3><span class="status">{status}</span></header><div class="states" style="--count:{len(images)}">'+''.join(cells)+'</div></article>')
    parts.append('</section>')
page='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>정규 상태 이미지 · 제작과 연결</title><style>*{box-sizing:border-box}body{margin:0;background:#232720;color:#eee5ce;font:15px/1.5 system-ui}main{max-width:1640px;margin:auto;padding:22px}h1{font-size:26px}h2{font-size:22px;margin-top:38px}h3{margin:0;font-size:18px}a{color:#edc784}nav{display:flex;gap:18px;flex-wrap:wrap;margin:20px 0}article{border:1px solid #626f57;background:#333b2d;margin:20px 0}header{background:#434f38;padding:12px 16px;display:flex;justify-content:space-between;align-items:center}.status{font-size:13px;color:#d1dabf}.states{display:grid;grid-template-columns:repeat(var(--count),1fr)}figure{margin:0;padding:12px;text-align:center;border-right:1px solid #56614b}figure:last-child{border:0}img{width:100%;height:310px;object-fit:contain;display:block}.pending{height:310px;display:grid;place-items:center;color:#9ba68d}figcaption{padding-top:10px;color:#cbd4bf}.note{color:#c7d0ba}@media(max-width:720px){main{padding:12px}figure{padding:6px}img,.pending{height:190px}.states{overflow-x:auto;grid-template-columns:repeat(var(--count),minmax(150px,1fr))}}</style><main><h1>정규 상태 이미지 · 제작과 연결</h1>'''+f'<p>{len(manifest)}개 개체 중 {installed}개 반영 · 새 상태 이미지 {generated}/{expected}장 제작</p>'+'''<p class="note">상태마다 대표 이미지 한 장을 전환하는 기존 규격입니다. 승인된 Idle과 선택창은 유지합니다. 제작·검수 중인 세트는 완성 후 연결하며, 상태별 포즈와 장비 구조를 대조합니다.</p><nav><a href="#Player">캐릭터</a><a href="#Monster">몬스터</a><a href="#Companion">동행견</a><a href="Requirements.md">정규 규격</a><a href="Verification.md">실행 검증</a><a href="FinalPromptSet.json">최종 제작 프롬프트</a><a href="../MonsterMood-2026-10-26/Gallery.html">최근 네 몬스터 Idle</a></nav>'''+''.join(parts)+'</main></html>'
(folder/'Gallery.html').write_text(page,encoding='utf-8')
(folder/'Progress.json').write_text(json.dumps({'actors':len(manifest),'installed':installed,'generated':generated,'expectedNewStateImages':expected},indent=2),encoding='utf-8')
print(f'State art review: {installed}/{len(manifest)} installed, {generated}/{expected} new images.')
