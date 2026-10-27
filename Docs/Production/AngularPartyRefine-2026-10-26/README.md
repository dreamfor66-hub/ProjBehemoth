# 캐릭터 장비·포즈 수정 / 07 검수

[비교판](Gallery.html) · [반복 검수 기록](Review07/Gates.md) · [요구 사항](Checklist.md)

최신 요청의 책 구조, 도끼 축, 워리어 스타일을 다시 수정했다. 실패한06과 부분 합성 시안은 최종 이미지로 사용하지 않았다.

- 워리어: 원래 몸/팔/주먹에 새 방패를 배치하고 두 가죽띠로 연결. 원래 팔 형태 유지. 방패 명암·면 수·선 굵기도 원본에 맞춰 재조정.
- 바바리안: 승인된 앞모습은 파일 그대로 유지. 뒷모습은 허리 위 구도, 소켓과 자루가 한 축으로 이어지는 도끼.
- 로그: 가까운 손을 든 역수 단검. 원래 얼굴/앞머리/후드 복원.
- 위자드: 닫힌 책을 자연스럽게 지지하고 부엉이와 눈맞춤. 전투 뒷면은 오른손에 스태프.

## 저장 파일

프로젝트 루트 기준 `Assets/Art/World/Angular/Heroes/`:

- `Warrior-Front.png`, `Warrior-Rear.png`
- `Barbarian-Front.png` (승인 파일 그대로), `Barbarian-Rear.png`
- `Rogue-Front.png`
- `Wizard-Front.png`, `Wizard-Rear.png`, `Wizard-Portrait.png`

소비 크롭: `Assets/Art/Characters/Selection/Selection{Warrior,Rogue,Barbarian,Wizard}.png`.
원본 6종과 사무라이/몽크 아트, 게임 UX·수치·배경/캐릭터 표시 배율은 수정하지 않았다.

생성은 내장 `image_gen` 모드로 진행했다. [최종 프롬프트 모음](FinalPromptSet.json), `Sources/`의 생성본과 `ApplyArt.ps1`/`ComposeFixedArms.ps1`에 재현 입력 및 합성 과정이 있다. 워리어는 새로 생성된 팔 대신 `ArmLocked/`의 원래 팔을 사용한다.

## 검증

- [워리어 팔 비교](Arm-Invariants07.json): 가죽띠로 가려지는 곳 밖 원본 팔 픽셀 변경 0.
- [정체성 비교](Identity-Invariants.json): 로그 얼굴 픽셀 변경 0, 승인 바바리안 앞모습 SHA 동일.
- [도끼 축 확인](Review07/Axe-Axis-Overlay.png), [노출 자루 측정](Review07/Axe-Axis.json).
- [지면 정렬](GroundAlignment.txt): 여섯 실제 발 최저점 y590. 비교 배율은 확정 신장 설정이 없는 상태의 가정이며 게임 배율을 바꾸지 않는다.
- 기존 Unity Editor 한 개에서 실제 Play 모드 검증. [SelectionPage 15개 확인](Live-Selection.txt) 통과. 실제 선택창 4종 및 전투 3종 캡처는 `Screenshots/`.
- 전투 화면의 기존 가드 게이지는 유지했다. 책/손 접촉은 원본 확대와 실제 표시를 함께 확인했다.
- 검증 후 Play 종료, `MainGameScene` Edit 상태 복귀. 로그 파일에 새 오류가 기록되지 않았다. 아트 변경에 맞춘 표시 검증만 수행했으며 밸런스/전체 회귀 검증은 실행하지 않았다.
