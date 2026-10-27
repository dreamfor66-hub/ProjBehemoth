from pathlib import Path
import json,re,html

root=Path(__file__).resolve().parents[3]
folder=Path(__file__).resolve().parent
rows=json.loads((folder/'DataAudit.json').read_text('utf-8'))
notes={
'LanternImp':'유지: 일반 공격 + 필수 약점 1개 강타.',
'BoneScorpion':'변경: 단일 일반 공격 → 집게 훑기/꼬리 찍기 교대. 준비 0.75초 → 0.45/1.05초. 강타는 유지.',
'StringRevenant':'유지: 일반 공격 + 필수 약점 2개 강타. 실을 끊는 별도 기믹은 추가되지 않음.',
'CoffinSlime':'유지: 가드하지 못한 일반 공격 적중 시 HP 18 회복. 강타 약점 2개.',
'BellHound':'유지: 강타 저지 실패 시 굴쥐 자리의 납골 전갈을 호출.',
'EttinGatekeeper':'변경: 왼머리 내려찍기/오른머리 휘두르기 교대. 기존 3연속 강타와 필수 약점 2개는 유지.',
'MirrorShell':'유지: 같은 베기 방향 반복 시 반격. 일반 2연타.',
'IronMantis':'변경: 낫 견제/교차 베기 교대. 일반 3연타와 필수 약점 1개 + 3회 강타가 필요한 보조 약점 1개 유지.',
'StoneSphinx':'유지: 세로 베기로 파괴하는 방향 방어. 강타별 필수 약점 3개/4개.',
'OssuaryBell':'유지: 일반 3연타 + 필수 약점 3개 강타.',
'WebDuchess':'유지: 본체와 주변 공전 개체 2기의 편대. 박쥐가 실인형으로 교체.',
'Threadling':'유지: 본체 주위를 공전하는 보조 적.',
'QuillSprite':'유지: 일반 공격 + 필수 약점 1개 강타.',
'CrystalUrchin':'유지: 일반 2연타 + 필수 약점 2개 강타.',
'VeilOracle':'유지: 일반 공격 + 필수 약점 2개 강타. 장막 개폐 판정은 별도 추가되지 않음.',
'TuskPilgrim':'유지: 종토끼와 시차를 두고 공격하는 2체 편대. 본체 강타 약점 2개.',
'BellHare':'유지: 엄니 순례자 옆에서 다른 박자로 공격하는 보조 적.',
'TwinAntlerStag':'변경: 앞발 견제/뿔 휘두르기 교대. 일반 2연타, 가로 베기 방향 방어, 강타 약점 2개 유지.',
'MirrorMoth':'유지: 같은 베기 방향 반복 시 반격 + 내려 베기로 파괴하는 방향 방어. 일반 2연타.',
'MaskedChoir':'유지: 3체 시차 편대. 개별 일반 공격은 2연타.',
'WinterUnicorn':'유지: 일반 공격 + 필수 약점 3개 강타. 별도 돌진 경로/충돌 판정은 추가되지 않음.',
'PrismCrab':'유지: 일반 4연타 + 내려 베기 방향 방어 + 필수 약점 3개 강타. 강타 저지 실패 시 향로 망령 소환/기존 소환수가 살아 있으면 HP 24 회복.',
'TempestRoc':'변경: 날개 바람/발톱 낚기 교대. 각 행동의 2연타와 폭풍 해파리 공전 편대는 유지.',
'StormJelly':'유지: 본체 주위 공전 + 일반 2연타.',
'CoalSalamander':'유지: 빠른 일반 공격 + 필수 약점 1개 강타.',
'SerpentWarden':'변경: 창 찌르기/꼬리 휩쓸기 교대. 기존 필수 약점 2개 강타 유지.',
'AshGargoyle':'유지: 일반 3연타 + 필수 약점 2개 강타.',
'HollowEye':'유지: 일반 공격 + 필수 약점 2개 강타. 별도 시선/광선 판정은 추가되지 않음.',
'CenserRevenant':'유지: 편대의 보조 적. 일반 공격 + 필수 약점 2개 강타.',
'OathlessCentaur':'유지: 일반 3연타(후속 간격 0.22초) + 필수 약점 3개 강타.',
'CrimsonCount':'유지: 무기 직접 타격 5회마다 공격 타이머를 0.5초 앞당김. 일반 2연타.',
'AshHierophant':'유지: 재 가고일 호위 편대. 강타 저지 실패 시 가고일 추가 호출.',
'AshDragon':'변경: 발톱 휘두르기/불씨 뿜기 교대. 필수 약점 4개 강타 유지. 별도 원거리 화염탄 판정은 추가되지 않음.',
'EclipseChimera':'유지: 해/달 보조 개체와 강타 연결. 해 파괴로 연타 강타 차단, 달 파괴로 연결 강타 차단, 둘 다 파괴하면 본체 기절.',
'SunIdol':'유지: 좌심장의 역할. 살아 있으면 본체의 5연타 강타를 허용.',
'MoonIdol':'유지: 우심장의 역할. 살아 있으면 본체의 연결 강타를 허용.',
'CinderLich':'유지: 일반 5연타. 소환 강타를 저지하지 못하면 향로 망령 합류, 소환/3연속 강타 순환.',
'IronMimic':'유지: 특수 조우의 일반 공격 + 필수 약점 2개 강타.',
'GildedToad':'유지: 상인 변이 조우. 빠른 일반 공격 + 필수 약점 3개 강타.',
'TarrasqueWhelp':'변경: 앞발 찍기/등껍질 들이밀기 교대. 각 행동의 2연타와 필수 약점 2개 강타 유지.',
'CrownColossus':'유지: 심층 시련의 일반 2연타 + 필수 약점 3개 강타.'}

lines=['# 몬스터 전후 비교 — 현재 적용 데이터 기준','',
'41종 중 **8종은 일반 공격 순서/준비 시간이 변경**, **33종은 외형·명칭이 바뀌고 전투 패턴은 유지**됐다. 표의 “유지”는 새로 추가한 기믹이라는 뜻이 아니라 기존 몬스터에서 이어받은 동작이다.','',
'비교 기준은 AngularWorld 작업 직전 Before/Monsters·Before/Attacks 백업과 현재 Assets/Data/Combat/Monsters 및 Data/Presentation/Angular/Attacks 자산이다.','',
'## 실제 패턴 변경 8종','',
'시간은 공격 주기가 아닌 **첫 타격 전 준비 시간**이다. 완료한 행동마다 다음 행동으로 넘어가며, 중단된 행동은 순서를 유지한다. 두 행동은 이름과 준비 시간만 다르고 피해량·타격 수·가드 가능 여부·후속 간격은 기존 일반 공격에서 복제했다. 꼬리/불씨 등의 이름이 별도의 범위 판정이나 투사체 구현을 뜻하지 않는다.','',
'| 층 | 기존 → 현재 | 이전 준비 | 현재 교대 행동 |',
'|---|---|---:|---|']
for e in rows:
 if not e['newNormals']: continue
 label='특수/시련' if not e['floor'] else f"B{e['floor']}"
 actions=' → '.join(f"{a['name']} **{float(a['windup']):.3f}초**" for a in e['newNormals'])
 lines.append(f"| {label} | {e['oldName']} → {e['name']} | {float(e['oldNormalWindup']):.3f}초 | {actions} |")
for f,title in [(1,'B1 · 미궁'),(2,'B2 · 서리층'),(3,'B3 · 잿불층'),(0,'특수 조우 / 시련')]:
 lines+=['',f'## {title}','','| 기존 몬스터 | 현재 몬스터 | 실제 전투 패턴의 전후 차이 |','|---|---|---|']
 for e in rows:
  if e['floor']==f: lines.append(f"| {e['oldName']} | {e['name']} | {notes[e['id']]} |")
lines+=['','## 확인 결과와 설명의 한계','',
'- 41종 모두 체력, 일반 공격 주기, 강타 주기, 기절 시간/배율, 보상, 몸통 판정 반경, 공전/지원 설정의 비교값은 동일했다.',
'- 기존 attacks의 피해량부터 하위 기믹 설정까지 41종 전부 복제 전후 동일했다. 추가된 일반 공격 순서 8종만 준비 시간이 0.6배/1.4배로 달라진다. 평균 준비 시간이 같다는 사실이 전투 체감/난이도 동등성을 보장하지는 않는다.',
'- 예: 해골 → 실매듭 망령은 외형/명칭 변경이며 새 실 절단 시스템은 없다. 굴쥐 → 납골 전갈은 같은 타격 기믹으로 준비 시간이 다른 두 행동을 교대한다.',
'- 기존 안내의 철갑 사마귀 “보조 약점 세 개” 문구는 실제 데이터와 다르다. 실제는 **보조 약점 1개에 강타 3회 필요**다. 이 보고서는 안내 문구보다 실제 자산 값을 우선했다.',
'- 일부 안내의 “가슴 매듭/중앙 눈/뿔”은 콘셉트 설명이며, 각각을 별도로 추적하는 부위 판정 시스템이 추가된 것은 아니다.',
'- 이번 작업은 데이터 대조/표 작성이며 게임 자산 수정, 추가 플레이 테스트, 밸런스 검증은 하지 않았다. 이전 작업의 실행 검증 기록은 AngularWorld-2026-10-26/Live.txt에 별도로 보존돼 있다.',
'','근거: [41종 데이터 대조](DataAudit.json) · [원본 대응표](../AngularWorld-2026-10-26/Roster.json) · [기존 수치 비교 기록](../AngularWorld-2026-10-26/Pacing-Invariants.json)']
(folder/'Comparison.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
# Static HTML tables, readable without any third-party dependencies.
body=[];table=False
for l in lines:
 if l.startswith('|'):
  if re.fullmatch(r'[| :\-]+',l):continue
  cells=[x.strip() for x in l.strip('|').split('|')]
  first=not table
  if first:body.append('<div class="table"><table>');table=True
  tag='th' if first else 'td'
  body.append('<tr>'+''.join(f'<{tag}>'+html.escape(c).replace('**','')+f'</{tag}>' for c in cells)+'</tr>')
 else:
  if table:body.append('</table></div>');table=False
  if l.startswith('# '):body.append('<h1>'+html.escape(l[2:])+'</h1>')
  elif l.startswith('## '):body.append('<h2>'+html.escape(l[3:])+'</h2>')
  elif l:body.append('<p>'+html.escape(l).replace('**','')+'</p>')
if table:body.append('</table></div>')
page='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>몬스터 전후 · 콘셉트와 패턴</title><style>body{margin:0;background:#232720;color:#eee5ce;font:16px/1.65 system-ui,sans-serif}main{max-width:1450px;margin:auto;padding:26px}h1{font-size:26px}h2{font-size:21px;margin-top:34px}p{color:#cbd0bf}.table{overflow:auto}table{border-collapse:collapse;width:100%;background:#30362c;margin:12px 0}th,td{text-align:left;vertical-align:top;padding:12px;border:1px solid #68725a}th{background:#454e3c;color:#f2dca1}td:first-child{min-width:120px}td:nth-child(2){min-width:145px}a{color:#edc784}</style><main><nav><a href="Comparison.md">표 다운로드</a> · <a href="DataAudit.json">데이터 대조</a></nav>'''+''.join(body)+'</main></html>'
(folder/'Gallery.html').write_text(page,encoding='utf-8')

