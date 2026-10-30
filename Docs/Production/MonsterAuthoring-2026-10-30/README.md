# Monster authoring structure cleanup

## Requirements

- [x] Drag existing weak points in the Ready preview; keep numerical editing, Undo/Redo and runtime coordinates consistent.
- [x] Draw, select, move, resize and remove hitboxes in the same preview; separate hitbox visibility toggle.
- [x] Preserve the Ready preview and show Portrait separately at its assignment field.
- [x] Keep CombatScale; use one hitBoxes collision system.
- [x] Remove separate BodySize, BodyHitRadius, BodyLobes, ArtworkSize and WeakPointLayoutScale authoring controls through actual data migration.
- [x] Derive footprint from the lowest hitbox edge, never from another editable footprint value.
- [x] Gate orbit settings behind useOrbit; preserve the existing orbit motion.
- [x] Remove confirmed unused/duplicated nearby fields and report the reason for each.
- [x] Preserve current working-tree assets and staged changes, all actual combat geometry, timings, damage, rewards and visible artwork.
- [x] Compare saved pre-migration geometry for every affected monster; verify editor gestures and directly affected behavior in the existing Unity runtime.

No balance/DPS suite or additional Unity instance is authorized or required for this structural migration.

## 수정 항목과 이유

| 이전 항목 | 수정 및 이유 |
|---|---|
| BodySize / BodyLobes / BodyHitRadius / UseAuthoredFootprint | 실제 판정 원들의 합집합을 그대로 `hitBoxes`로 이관. 이중 판정/크기 기준 제거. `BodySize`는 호환용 읽기 전용 계산 속성만 남음. |
| Footprint | `min(hitBox.center.y - hitBox.radius)`로 자동 계산. 별도 저작 값 없음. |
| ArtworkSize | 기존 표시 크기와 기준점을 native Sprite의 PPU/pivot/frame geometry로 이관. 원본 PNG와 픽셀 crop은 그대로 유지. MonsterData에서 그림 보정 수치 제거. |
| WeakPointLayoutScale | 배율을 각 약점의 실제 좌표에 반영. 서로 다른 몬스터 배율이 적용되던 공유 공격은 별도 공격 에셋으로 분리하여 다른 몬스터에 영향 방지. |
| normalizedPosition | 실제 의미에 맞게 `localPosition`으로 변경. 기존 직렬화 키도 마이그레이션. |
| Pattern.Orbit | `useOrbit`으로 분리. 기존 공전 몬스터만 true. OrbitRadius(가로 반경), OrbitSpeed(각속도)는 true일 때만 편집 가능. 기존 세로 궤도 65와 운동식 유지. |
| CombatScale | 종별 크기/바리에이션 저작 값이므로 유지. 무리 수에 따라 크기를 변경하지 않음. |
| EncounterMember.scale | 실제 SpawnMonster가 무시하던 값 제거. 종별 CombatScale이 유일한 크기 기준. |
| SummonedMinionScale / SpawnMonster.visualScale 인자 | 이미 무시되던 상수/인자 제거. 소환수 실제 크기 유지. |
| BodyColor | 현재 MonsterView가 읽지 않는 저작 값 제거. |
| labelHeight | 실제 렌더링에서 읽지 않는 값 제거. |
| groundContactY / artworkTopY / arcadeArtScale | 기존 그림 기준점을 Sprite에 이관한 뒤 중복 보정/높이 입력 제거. 이름/HP 위치는 native frame geometry로 자동 계산. |

## 기존 결과 보존

- 변경 전 작업 트리에서 몬스터 44종, Idle/Ready/Attack/Dead 176개 표시 사각형, 원형 판정, 실제 약점 배율을 기록했다. HEAD나 오래된 원본으로 되돌리지 않았다.
- `Verification.txt`: 1,100 checks / 0 failures. 판정 원, 약점 좌표/반경/필요 타수, 모든 미이관 전투 수치, 그림 texture/crop/mesh quad, 이름/HP 위치 및 글자 크기 비교.
- `EncounterVerification.txt`: 저장된 배치 63개 멤버의 실제 actor origin/actionDelay 보존. 비대칭 Sphinx는 실제 최하단 히트박스에 맞춰 footprint가 바뀌므로 저장된 배치 y를 -15 이관하여 화면/판정 원점은 유지.
- 기존 캡슐 판정은 기존 엔진이 실제 생성하던 원들을 그대로 저장했다. 모양을 새로 근사하거나 밸런스를 재조정하지 않았다.
- `Gestures.txt`: 실제 Windows Unity Inspector에서 약점 이동/Undo/Redo, 히트박스 생성/이동/반지름/삭제/Undo, 표시 토글, Portrait 분리 표시, useOrbit 조건부 활성화 확인. 임시 테스트 에셋은 제거.
- `Live.txt`: 기존 Unity Play 모드에서 단일/3마리 배치, 종별 CombatScale 유지, 실제 일반 타격/약점 강타, 1.6배 바리에이션의 약점 좌표, 기존 회전 운동식, useOrbit=false 정지 확인. 11 checks / 0 failures.
- `PreviewRuntimeMatch.txt`: 실제 해골 전사와 Sphinx 전투의 Ready 그림/약점 UI 크기·중심·판정 반경이 Inspector와 일치. 화면 픽셀 스냅 전 그림 오차는 각각 0.000044 / 0.000103 logical units 이하. 변경 범위의 Play 검증 후 바로 종료.
- 재개 후 검증 코드의 baseline 역직렬화가 참조 에셋을 로딩하며 남기던 Console 오류도 수정했다. `VerificationHarness.txt`: 참조를 제거한 값 전용 역직렬화 44종 통과, 최종 Unity 컴파일 성공. 이 확인 과정에서는 디자이너가 편집한 현재 데이터를 복원하거나 덮어쓰지 않았다.
- `Baseline.json`과 `Before/`는 마이그레이션 직전 데이터 보존 기록이다. `bake_native_outlines.py`는 원본 픽셀/UV/rect를 변경하지 않고 Sprite의 원래 frame 기준만 직렬화했다. 변환기는 이관 후 자동 재실행되지 않도록 Editor에서 제거하여 이 문서 폴더에 보관했다.

## 사용

MonsterData 선택 → 위 Ready 영역에서 약점을 드래그. 히트박스 생성 탭은 중심에서 밖으로 드래그한다. 선택/이동 탭에서 원을 이동하고, 선택한 원 오른쪽 손잡이로 반지름을 조절한다. Delete 또는 삭제 버튼으로 제거한다. `화면 맞춤`은 편집 후 프레임을 다시 잡는다. 수치 편집과 Undo/Redo도 사용할 수 있다.
