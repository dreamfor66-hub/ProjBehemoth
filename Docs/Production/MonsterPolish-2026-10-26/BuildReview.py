from pathlib import Path
import json,html,hashlib
from PIL import Image,ImageDraw

folder=Path(__file__).resolve().parent
rows=json.loads((folder/'Manifest.json').read_text('utf-8'))['items']
prior={e['id']:e for e in json.loads((folder/'Roster-Before.json').read_text('utf-8'))}
prompts={e['id']:e for e in json.loads((folder/'PromptSet.json').read_text('utf-8'))}
esc=lambda x:html.escape(str(x),quote=True)
cards=[]
for row in rows:
    key=row['id']; ref=prompts[key]['reference']
    rev=hashlib.sha256((folder/'Generated'/f'{key}.png').read_bytes()).hexdigest()[:10]
    name=esc(row['name']); before=esc(prior[key]['name'])
    pending=prompts[key].get('status')=='pending_generation'
    status='<p class="pending">생성 도구 차단으로 수정 보류 · 기존 이미지 유지</p>' if pending else ''
    change=f'{before} → {name}' if before!=name else name
    cards.append(f'<article id="{key}"><h2>{change}</h2>{status}<div class="pair"><figure><img loading="lazy" src="Before/{key}.png"><figcaption>수정 전</figcaption></figure><figure><img loading="lazy" src="Generated/{key}.png?v={rev}"><figcaption>수정 후</figcaption></figure></div><details><summary>대조한 레퍼런스</summary><a href="References/{ref}.jpg"><img class="ref" loading="lazy" src="References/{ref}.jpg"></a></details></article>')
page='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>몬스터 아트 수정 전후</title><style>*{box-sizing:border-box}body{background:#232720;color:#eee5ce;font:15px/1.5 system-ui;margin:0}main{max-width:1500px;margin:auto;padding:22px}h1{font-size:25px}h2{font-size:18px;margin:0;padding:14px 16px;background:#414c37}a{color:#edc784}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(420px,1fr));gap:18px}article{border:1px solid #69755c;background:#343b2f;overflow:hidden}.pair{display:grid;grid-template-columns:1fr 1fr}figure{margin:0;padding:12px;text-align:center}figure:first-child{border-right:1px solid #59624f}figure img{display:block;width:100%;height:285px;object-fit:contain}figcaption{color:#c5cdbb;padding:6px}details{border-top:1px solid #59624f;padding:12px 16px}summary{cursor:pointer}.ref{max-height:380px;max-width:100%;display:block;margin:12px auto}.intro{margin-bottom:20px;color:#cbd3c0}@media(max-width:480px){.grid{grid-template-columns:1fr}figure img{height:220px}}</style><main><h1>몬스터 아트 수정 전후 · 40종 수정 / 1종 보류</h1><p class="intro">40종은 수정 완료했고, 결정 성게는 생성 도구 차단으로 기존 이미지를 유지했습니다. 큰 면과 읽기 쉬운 실루엣을 기준으로 잔돌기·과도한 찢김·표면 조각을 줄였습니다. 지정한 다섯 종은 컨셉과 이름을 바꿨으며, 전투 패턴과 공격 시간은 유지했습니다.<br><a href="../MonsterComparison-2026-10-26/Gallery.html">전체 몬스터·주요 효과 표</a> · <a href="PromptSet.json">작업 프롬프트</a></p><div class="grid">'''+''.join(cards)+'''</div></main></html>'''
(folder/'Gallery.html').write_text(page,encoding='utf-8')
for start in range(0,len(rows),9):
    sheet=Image.new('RGB',(1200,1200),'#353b30');draw=ImageDraw.Draw(sheet)
    for j,row in enumerate(rows[start:start+9]):
        im=Image.open(folder/'Generated'/(row['id']+'.png')).convert('RGBA');im.thumbnail((380,350))
        x=j%3*400+(400-im.width)//2;y=j//3*400;sheet.paste(im,(x,y),im);draw.text((j%3*400+8,y+365),row['id'],fill='white')
    sheet.save(folder/'Review'/f'After-{start//9+1}.jpg')
print('Built 41 art comparisons and five review sheets.')
