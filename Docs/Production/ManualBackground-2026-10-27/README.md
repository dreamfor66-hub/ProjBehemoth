# Background 수동 제어 — 2026-10-27

- [x] RoomTransitionView의 배경 자동 확대, 위치 흔들림, 원점/Scale 1 초기화 제거.
- [x] StageBackdropView의 매 프레임 FloorArtwork.imageOffset 위치 덮어쓰기 제거.
- [x] 원화 준비 코드의 Background 위치 자동 설정 제거. 기존 imageOffset 데이터는 보존하되 Inspector에서 숨김.
- [x] 배경/문 스프라이트 교체와 발걸음 표식 연출은 유지.
- [x] 기존 Background RectTransform/씬 값은 변경하지 않음. Anchors/Width/Height/위치/Scale은 Inspector에서 직접 설정.

실행 검증은 기존 Unity의 Background에 임시 고정 anchor, 1234×730 크기, 위치 (37,-41), Scale (1.12,.94,1)을 적용하고 여러 LateUpdate, 세 층 전환, 이동 중/종료/초기화에서 유지 여부를 확인한다. 테스트값과 층은 종료 시 복원하며 자산에 저장하지 않는다. 초기 검증은 아직 run.State가 없는 Play 진입 상태에서 중단되어, State가 없는 경우에만 런 초기화를 하도록 검증 준비 코드를 보완했다. 최종 결과는 Live.txt에 기록한다. 전투/밸런스 테스트는 실행하지 않는다.

최종 실행 결과: 32개 항목 PASS, failures=0. 수동 크기·위치·Scale이 세 층 전환과 이동 시작/중간/종료 및 Configure/Reset에서 유지되며, 테스트 레이아웃과 층 복원을 확인했다.
