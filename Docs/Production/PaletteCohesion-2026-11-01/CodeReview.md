# 최종 변경 코드 읽기 검수

2026-11-01. **이번 수정에서 추가 수정이 필요한 확정 결함은 찾지 못했다.** 코드·데이터·그림·Unity 상태를 변경하거나 테스트를 추가 실행하지 않았다. Root가 공유한 최근 실제 검증395 checks/0 failures 및 props153/0 이후의 좁은 읽기 검수다.

|범위|읽어 확인한 직접 영향|
|---|---|
|`CompanionView`의 카메라 공유|실제 PlayerView 그림 Rect와 계산된 zoom/displacement를 같은 stage 좌표로 변환한다. PlayerView/그림을 찾지 못하면 stage 비율을 사용한다. Idle/Attack은 공통 ground와 기준 그림 스케일을 공유하며 새 포즈별 이동 튜닝을 추가하지 않는다.|
|동료 raycast filter|현재 그림의 PNG 파생 bounds를 body의 로컬 Rect로 변환한다. 투명 캔버스의 큰 빈 영역을 클릭 영역에서 빼며 전투/플레이 코드 검사 중에는 body raycast와 talk를 해제한다. 실제 회수견 데이터의 geometry/Idle/Attack 연결을 확인했다.|
|`PlayerView` 계산 속성 / body raycast|framingRoot/parent가 없으면 displacement는0이다. 그림의 raycast 해제는 전용 입력을 제거하지 않는다. `CombatInputController`는 직접 포인터를 읽으며 논리 몸체 판정으로 guard/charge를 처리한다.|
|`CompanionData.forwardAnimation`|미지정 시 기존 opposite/combat library로 내려간다. 현재 생산 회수견1종은 forward=combat이며 유효한 Idle/Attack을 보유한다. pose 미지정은 기존 `CombatSpriteAnimator`의 Unity-null 비교 및 Idle 대체 경로를 따른다.|
|`RoomWallMarkLayer.Prepare`|캐시된3장일 때 조기 반환하기 **전에** 첫 sibling 규칙을 실행한다. 저장 씬의 마지막 mark 계층도 첫 `Show()`에서 배경 위·문/사물 아래로 복구한다. 부모 Background 자체보다 뒤로 보내는 처리가 아니다.|
|`WorldRenewalPreparation`|서약실은 B1/B2/B3 전용 그림과 중복 tint 없는 바인딩을 사용한다. art revision은 CompanionData의 재귀 의존 PNG도 수집하므로 World 밖의 편집 가능한 동료 포즈를 누락하지 않는다. 기존 정렬/중복제거/파일 해시 처리에 연결된다.|

검수는 요청된 최신 경로와 직접 호출부·현재 생산 데이터·저장 씬에 한정했다. `CompanionView`의 전체 Git diff에는 이전 배치/그림/발견 작업도 포함되고 `RoomWallMarkLayer`·`WorldRenewalPreparation`은 현재 untracked이므로, 넓은 작업 전체를 이번 신규 변경으로 간주하지 않았다. 벽계층·서약실·revision 경로는 별도 읽기 검수자와도 대조했다.

현재 유효한 데이터에서의 null 대체를 확인했으며 임의로 손상된 animation/Prefab, 모든 화면 비율·입력 기기 또는 전체 전투 회귀까지 검증했다고 주장하지 않는다. 추가 Unity/전체 DPS·밸런스·회귀 suite는 실행하지 않았다.
