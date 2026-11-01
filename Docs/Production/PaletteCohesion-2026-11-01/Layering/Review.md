# 벽장식과 주요 사물의 렌더 계층

## 변경 전 조사

2026-11-01 최종 QA의 `Screens/Final-B3-Goddess-0-Ready.jpg`를 `view_image`로 직접 확인했다. 여신상 얼굴·손 중앙으로 벽장식 천이 앞에서 겹쳐 그려진다. 이 이미지는 `Before-B3-Goddess-0-Ready.jpg`로 보존한다. 생산 PNG를 합성하거나 색/배치를 바꾸어 해결하지 않는다.

`Scenes/MainGameScene.unity`의 Background RectTransform(fileID 175021868)의 직계 자식은 다음 순서다.

1. Door artwork Forward/Left/Right (325779053, 1816054260, 992304290)
2. Exploration Objects (1232760228)
3. Fixed room architecture marks (35350375)

Unity Canvas는 뒤쪽 형제를 앞에 그리므로, 나중에 생성된 장식 계층이 사물 위에 그려지는 것이 원인이다. `ExplorationView.Add()`는 RoomObjectSlot만 해당 objectLayer 내부에서 Depth 순으로 정렬한다. `RoomObjectSlot.Bind()`의 벽면/바닥 지지점 계산이나 그림의 Rect는 원인이 아니다. `StageBackdropView.Refresh()`는 background 아래 RoomWallMarkLayer를 찾아 Show()하고, Show()가 Prepare()를 호출하지만 기존 Prepare()에는 형제 순서 규칙이 없었다. `WorldRenewalPreparation.Install()` 역시 background의 마지막 자식으로 생성한 뒤 Prepare()했다.

## 최소 교정 규칙

별도의 장식 전용 `RoomWallMarkLayer`를 재사용하여, Prepare()에서 자기 계층을 background의 첫 자식으로 고정한다. 기존 paintings 배열이 있을 때도 적용되도록 조기 반환 전에 실행한다. background 그림 → 벽장식 → 문 그림/문틀 → 주요 사물 순서를 보장한다. 문/사물의 자체 정렬이나 actor/HUD/상호작용 계층은 이동하지 않는다. `SetAsLastSibling()`으로 거대한 사물 계층을 UI 위로 끌어올리지 않는다.

공유 규칙이므로 여신상 ID나 그림명 분기는 없고, 추가 편집 필드도 만들지 않는다. 기존 장식의 raycastTarget=false와 투시·색·좌표를 유지한다.

## 검증 상태

변경 전 보고서를 작성하고 근거 이미지·원본 소스를 보존한 뒤 `Scripts/UI/RoomWallMarkLayer.cs`만 수정했다. `Prepare()`의 조기 반환 전에 `if(transform.GetSiblingIndex()!=0)transform.SetAsFirstSibling();`을 넣었다. `StageBackdropView.Refresh()` → `RoomWallMarkLayer.Show()` → `Prepare()` 순서라 이미 설치된 paintings 배열에서도 적용된다. 현재 array 구성·데이터·사물 geometry·원화·씬은 변경하지 않았다. 원본과의 줄 비교로 변경이 이 규칙과 설명 주석뿐임을 확인했다.

교정 후 기존 Editor에서 컴파일하고 16:13:14–31 KST에 실제33장(세 막 여신상9+분수 네 방향×사용 전후24)을 다시 촬영했다. PropsReview.txt는 실제 장식/주요 사물 부모 계층과 형제 순서를 포함해153/0이다. 독립 검수자가33장을 직접 열어 B3 정면 얼굴/그릇, B2/B3 측면의 벽천 가림 해소 및 분수 레이어 회귀 없음을 확인했다. 전후 비교와 해시는 Props/ActualRuntimeReview.md, Actual-Goddess-Layer-Fixed-BeforeAfter.jpg를 따른다. 이 변경에서 새 반려 사유는 발견하지 않았다.
