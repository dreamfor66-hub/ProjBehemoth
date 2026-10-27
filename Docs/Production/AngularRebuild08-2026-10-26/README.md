# 08 · 바바리안/위자드 수정, 워리어 롤백

후속 수정: 바바리안 후면08의 가려진 왼팔 길이에 대한 사용자 지적을 반영해 후면09로 교체했다. 현재 비교판은 [후면09](../BarbarianRear09-2026-10-26/Gallery.html)를 표시한다. 이 문서 아래08의 검수/파일 기록은 당시 기록이며 최신 바바리안 검수는09 폴더를 참조한다.

[현재 적용본](Gallery.html) · [검수 기록](Review.md) · [체크리스트](Checklist.md)

- **워리어:** 사용자 최신 지시에 따라 초기 앞/뒷면 팔과 방패로 롤백하고 마무리. 생성한08/08b/08c 방패/팔은 사용하지 않는다. `Restored/Warrior-{Front,Rear}.png`와 현재 자산 파일이 동일하다. 투구 등 기존 몸 디자인은 복원 기준 파일 그대로 유지.
- **바바리안:** 승인 앞모습 파일 유지. 뒷면은 허리 위, 두 손으로 대각선 도끼를 받치는 준비 자세. 도끼 심/소켓과 손 위/아래로 노출된 자루 축을 원본 확대 및 파란 축선 오버레이에서 대조했다. 원본 앞모습의 비대칭 양날 형상/재질과 비교했다.
- **위자드:** 앞모습을 부엉이와 눈맞춤하던05 파일로 복원, HUD와 선택창도 이 파일에서 다시 크롭. 뒷면의 책은 왼팔로 몸 앞에서 지지하는 위치라 몸/망토에 가려진다. 오른손 스태프와 어깨 파트너는 유지.

## 파일

프로젝트 루트 기준 현재 소비 자산:

- `Assets/Art/World/Angular/Heroes/Warrior-Front.png`, `Warrior-Rear.png` — 롤백
- `Assets/Art/World/Angular/Heroes/Barbarian-Rear.png` — 새 양손 준비 자세
- `Assets/Art/World/Angular/Heroes/Wizard-Front.png` —05 복원
- `Assets/Art/World/Angular/Heroes/Wizard-Rear.png` — 새 후면 구도
- `Assets/Art/World/Angular/Heroes/Wizard-Portrait.png` — 복원본의 HUD 크롭
- `Assets/Art/Characters/Selection/SelectionWarrior.png`, `SelectionWizard.png` — 해당 앞모습의 선택창 크롭

`Reviewed/`는 채택된 최종 파일, `Sources/`는 내장 `image_gen` 생성 출력, `Restored/`는 복원 원본이다. [채택 프롬프트](FinalPromptSet.json). 워리어 중단 시안은 프롬프트/소스로만 남고 설치 스크립트에는 포함되지 않는다.

`PrepareReviewed.ps1`는 바바리안 후면 캔버스2px 여백/하단1px 정리 및 투명영역/하단 색 노이즈 정리를 한다. 장비를 별도로 변형/합성하지 않는다. `ApplyFinal.ps1`는 채택 파일만 복사하고 필요한 크롭을 갱신한다.

## 실제 검증

- [복원·유지 파일 검증](Applied-Invariants.json): 워리어 전후면과 위자드 앞모습은 복원본 그대로. 바바리안 승인 앞모습/로그 등 보호 파일 유지.
- 원본 확대, 앞/뒷면 디자인 비교, [도끼 축선](Axe-Axis-Review.png), 회색/어두운 [6종 지면 배열](GroundLineup-Dark.png) 확인. 무기 높이를 신장으로 세지 않는다. 비교 배율은 설정 확정치가 아닌 표시용 가정이며 게임 배율을 변경하지 않는다.
- 기존 Unity Editor 한 개의 Play 모드에서 [SelectionPage 15개 확인](Live-Selection.txt) 통과. 워리어/바바리안/위자드 전투 캡처 및 위자드 선택창을 육안 확인했다. 캡처는 `Screenshots/`.
- 바바리안 도끼날/상단 그립, 위자드 스태프를 쥔 손과 복원된 선택창의 실제 표시를 확인했다. 바바리안 하단 그립은 원본 확대에서 대조했고 전투 화면에서는 아래쪽이 일부 잘린다. 기존 회색 가드 게이지는 유지했다.
- 검증 후 `MainGameScene` Edit 모드로 복귀. 신규 오류 로그 없음. 게임 UX·배경/캐릭터 배율·전투 수치를 수정하지 않았다. 밸런스/전체 회귀 검증은 실행하지 않았다.
