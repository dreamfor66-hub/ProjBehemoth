# 회수견 전방 후면과 플레이어 옆 배치

사용자 이미지6의 문제는 현재 성견이 화면 가장자리에서 옆모습을 전시하고 있어, 큰 상반신이 잘린 플레이어와 다른 거리의 독립 오브젝트처럼 보이는 점이다. 실제 활성 참조는 옛 귀여운 TrailDog PNG가 아닌 `Art/Combat/Lore01/T_Sprite_Character_길동무_{Idle,Attack}.png` 및 별도 좌향 세트였다. 강아지 이벤트/적 그림의 정체성은 그대로 두고 **동료가 보는 카메라와 배치**만 교정했다.

## 원화와 반려

실제 활성 성견·Warrior navigation 원화·사용자 이미지6·사용자 DD 조련사 그림을 직접 열었다. 성견의 마른 작업견 체형, 긴 주둥이, 거친 회갈색 털, 탁한 붉은 목줄, 왼쪽 앞다리 붕대는 정체성으로 유지했다. 기존 귀여운 TrailDog 후면은 뒤쪽 머리/몸 시점만 참고했고 갈색·흰색 털이나 어린 인상은 복사하지 않았다. 외부 사진 검색의 뒤쪽 개 사진은 뒷발/꼬리/먼 앞발의 가림을 대조하는 보조 자료였으며 의학적 결론을 내리지 않았다: [후면 사진](https://cdn.p-nest.co.jp/c/wanchan.jp/pro/resize/700x500/100/4/00d816915f90273f9150216230c912a5.jpg). 개별 사진의 breed나 건강 상태를 제작 사실로 주장하지 않는다.

내장 imagegen으로 직접 편집했다. Idle은 뒤쪽 머리와 척추가 모두 전방으로 향하고, 눈/옆 주둥이는 보이지 않는다. 네 발이 지지되고 가까운 뒷발과 더 먼 앞발의 크기/높이가 구분된다. 넓은 목줄·몸통 아래 먹 덩어리를 유지하면서 작은 털 긋기·목줄 얼룩을 실제로 줄였다. Attack은 낮게 전진하는 어깨/목과 벌어진 뒷발의 지지로 별도 작화했다. 바뀐 붕대 쪽과 사람처럼 들어 올린 앞발이 나온 공격 초안 두 장은 반려했다. 자세/방향/큰 면을 같은 크기의 `PoseReview.jpg`와 생성 원본에서 직접 확인했다.

프롬프트는 `IdlePrompt.json`, `IdleCleanPrompt.json`, `AttackPrompt.json`, `AttackV2Prompt.json`, `AttackV3Prompt.json`에 보존했다. `Sources.json`은 선택/반려 원본과 사유, `InstalledPaintings.json`은 원본·생성·생산·메타 SHA 및 기계적인 단일 균등 배율/오프셋이다. 두 PNG 모두 기존1536×1024와 GUID/Importer metadata를 유지했다. Python은 색·붓질·표정을 수정하지 않았고 캔버스 배치/비교판만 만들었다. 생산 목록은 `DogAssetFiles.json`이다.

## 표시와 설치

`CompanionData.forwardAnimation`은 플레이어와 같은 전방 카메라를 쓰는 authored 세트를 별도 명시한다. 기존 좌향 세트가 남아 있더라도 화면 위치 때문에 옆모습으로 자동 바뀌지 않는다. 일반 다른 동료의 기존 앞/반대편 세트 fallback은 유지한다.

동료 크기/지지점은 방 코너에서 벗어나 실제 `PlayerView` painted Image의 월드 프레임을 읽어 계산한다. 좌우 flank는 그 너비의46%, 높이 배율은 현재 player 프레임 너비/기준640이다. 플레이어의 전투 카메라 확대와 같이 커진다. 탐험 기준 지지면은 확대 전 프레임 아래쪽을 복원하여 계산하고, 실제 플레이어 카메라의 하강량을 그대로 더한다. 전투에서도 지지면을 화면 아래6%에 고정했던 첫 배치는 실제 화면에서 개의 머리가 Warrior 투구보다 높아져 반려했다. 보정 후에는 Idle/Attack 중 더 짧은 원화의 높이로 공통 하한을 계산하여 상부30%가 남도록만 제한한다. 작은 전신을 억지로 넣는 대신 아래 다리가 프레임 밖으로 일부 잘릴 수 있다. Idle/Attack의 픽셀 배율과 지지면은 동일하며, 현재 표시 자세에 따라 위치를 바꾸지 않는다. `PlayerView.CameraFramingZoom`과 `CameraFramingDisplacement`는 기존 카메라 상태에서 계산되는 읽기 전용 값이다. 근처 사물과의 겹침은 방마다 한 번 좌/우 중 덜 겹치는 flank를 고른다. 좁은 후면 그림의 투명 여백이 넓은 보이지 않는 버튼이 되지 않도록 PNG에서 도출한 실제 알파 bounds만 클릭을 받는다.

명시적 설치는 global namespace의 `PaletteCompanionPreparation.Install()`이다. 자동 실행하지 않는다. 실제 활성 회수견 데이터에 `forwardAnimation=combatAnimation`, 편집 가능한 displayHeight360과 PNG에서 도출한 geometry를 저장한다. 옛 Depth/Lore preparer는 각 기존 Installed marker 때문에 현재 자동 재설치되지 않는 것을 확인했다.

실제 Unity Play·Editor hook는 이 에이전트가 호출하지 않았다. Root가 마지막에 한 Editor로 실행하는 scoped probe의 B1/B2/B3 navigation/Idle/Attack·내레이션 클릭 캡처에서 최종 거리·부분 잘림·실제 조작을 확인한다. 원화판을 실제 화면 완료로 대신하지 않는다.
