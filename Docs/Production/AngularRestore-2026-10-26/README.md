# 승인된 구도 복원 · 2026-10-26

- 워리어: 몸통과 같은 정면을 향한 후두부로 수정했다. 투구/목 주변 영역만 합성했고, 그 밖 몸통·방패·검·팔은 기존 PNG와 픽셀이 동일하다.
- 바바리안: 곰 머리 가죽을 뒤집어쓴 인간, 넓은 어깨, 양손으로 잡은 큰 도끼의 별도 시안을 만들었다. 기존 리자드와 승인된 정면 6종은 보존했다. `Gallery.html`에서 기존 리자드와 비교하고, 곰가죽 시안을 넣은 6종 배열을 볼 수 있다.
- 1층 배경: 사용자 4번 이미지의 실제 원본 `Assets/Art/Trials/Angular01/SourceHall.png`를 다시 사용했다. 중앙/왼쪽 문 패널의 벽 상태만 편집하고, 중앙 통로는 원본 픽셀 그대로 사용했다. 옆문은 기존 벽의 좁은 투시에 맞춰 편집했다. 문 패널 밖 기둥·바닥·천장·소실점·명암은 원본과 픽셀이 동일하다. B2/B3의 별도 배경은 유지한다.
- 크기와 배치: 6종의 전투 그림에 승인된 시안의 실제 canvasScale `1.1292576`을 복원했다. 원래 씬의 캐릭터 기준점·크기·입력 판정 사각형을 바꾸지 않는다. 배경은 확대하지 않고 Y=-24 논리 좌표로 조금 내렸다. 문 레이어도 함께 이동한다. 뒤로 가기는 원래 화면처럼 왼쪽 아래다. 당시 UI 색상 팔레트도 별도 저장본으로 복원했다.

## 최종 자산

- `Assets/Art/World/Angular/Heroes/Warrior-Rear.png`
- `Assets/Art/Concepts/BarbarianBearPelt01.png`
- `Assets/Art/World/Angular/Restoration01/B1-{Walls,Forward,Left,Right,AllOpen,LeftClosed}.png`
- `Assets/Data/Presentation/Angular/B1.asset`: 배경/출입구/위치
- `Assets/Data/Presentation/Angular/Animations/Hero-*.asset`: 당시 확대값
- `Assets/Data/Presentation/Angular/RestoredPalette.asset`: 당시 UI 팔레트
- `Assets/Scenes/MainGameScene.unity`: 저장된 배경·문·길 버튼 배치

내장 `image_gen`을 사용했다. 생성/편집 입력과 프롬프트는 [PromptSet.json](PromptSet.json), 원본에 국소 합성하는 재현 스크립트는 [Assemble.ps1](Assemble.ps1)에 있다. 그림을 다시 그리는 대신 문/머리 외부는 원본을 그대로 합성했다. 회색/어두운 배경의 6종 배열로 스타일와 알파를 확인했다. 바바리안의 얼굴은 기존 캐릭터가 아닌 별도 인간 시안이며 기존 로그 얼굴을 변경하지 않았다.

## 확인

- [Pixel-Invariants.json](Pixel-Invariants.json): 배경 문 패널 밖 변경 0픽셀, 워리어 머리 밖 변경 0픽셀.
- [Live.txt](Live.txt): 기존 한 개 Unity Editor의 실제 MainGameScene에서 확인했다. 승인 시안과 같은 확대값, 원래 1672×941 배경 크기, 배경 Y=-24, 실제 스와이프 타격/드래그 유지 방어, 4방향×8문 조합, 복원한 길 버튼 4개의 위치와 가려지지 않은 클릭 대상을 검증했다.
- `Screenshots/`: 실제 렌더링한 전체 문/벽/정면+우측 및 공격/방어 화면.
- 컴파일 오류 없음. 게임 밸런스·DPS 전체 회귀, 실기기 터치 검증은 이번 국소 아트 수정 범위에서 실행하지 않았다.

[비교 보드 열기](Gallery.html)
