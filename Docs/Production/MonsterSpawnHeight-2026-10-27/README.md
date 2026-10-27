# 몬스터 배치 높이 -150

- `Assets/Scenes/MainGameScene.unity`의 `Canvas/CombatRoot/MonsterSpawnRoot`를 `(0,-150)`으로 저장했다. Inspector의 Pos Y로 이후 직접 조절 가능하다. 일회성 준비 revision이 적용 완료되면 디자이너 수정을 덮어쓰지 않는다.
- 히트박스 하단 기준, 이미지 접지 보정, 개별 Encounter offset/scale은 유지한다. 기본 3마리의 하단은 `(-370,-215),(0,-150),(370,-215)`, 2마리는 `(-270,-185),(270,-185)`다.
- 배경/플레이어/몬스터 능력치는 변경하지 않았다. 기존 열린 씬의 수정은 보존하여 저장했고, 변경 전 디스크 파일과 열린 씬을 이 폴더에 백업했다.
- 기존 Unity Editor에서 단일 해골, 3마리 배치, 플레이어 높이, 약점 화면 좌표 및 실제 차지 공격 판정만 확인했다. [Live.txt](Live.txt): **10 PASS / 0 FAIL**. 확인 후 Play 종료. 별도 밸런스 검증은 실행하지 않았다.
- 화면: [단일 해골](01-Skeleton-Lowered.png), [차지](02-Skeleton-Charge.png), [3마리](03-Trio-Lowered.png).
