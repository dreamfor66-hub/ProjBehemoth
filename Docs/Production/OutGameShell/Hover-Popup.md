# 재사용 사이드 설명 팝업

`HoverTooltipTrigger`는 유물 이름/아이콘을 감싼 RectTransform에 붙인다. `view`에는 같은 Canvas의 `ItemTooltipView`, `anchor`에는 설명이 붙을 영역을 연결한다. 클릭 동작과 독립적으로 포인터 진입/이탈과 키보드 선택/해제를 처리한다.

내용은 `Bind(EquipmentData)`, `Bind(PerkData)`, `Bind(title, description)`로 공급하거나 Inspector의 `titleKey`, `descriptionKey`로 한국어 텍스트 키를 연결한다. 주문 등 다른 데이터도 제목/설명 문자열을 전달하면 된다. 아이템 이름/ID 분기는 없다. 캐릭터 선택은 현재 캐릭터의 `startingRelic` 데이터를 직접 연결한다.

`ItemTooltipView.ShowBeside(owner, title, body, anchor)`는 앵커 양옆 여유를 비교하고 Canvas 내부로 위치를 제한한다. 팝업은 raycast를 받지 않아 가려진 버튼이나 원래 hover 영역을 방해하지 않는다. 소유자별 Hide를 통해 이전 영역의 이탈이 새 설명을 닫지 않는다. 호버 중 Bind가 바뀌면 즉시 갱신한다. 원래의 `Show(title, body, screenPosition)` API는 HUD/선택지에서 계속 사용할 수 있다.

OutGameScene의 `Canvas/SideDescription`은 편집기에서 확인 가능한 저장된 팝업이다. `expandToFit`은 긴 설명에 맞춰 폭을 늘리며 폰트 크기를 유지한다. 기본 폭 154, 최대 폭 240 native pixels. 폰트/색/테두리/최대 폭은 Inspector에서 조정한다. 컴포넌트의 panel/title/description/positionRoot 연결을 복제해 다른 Canvas에서도 사용할 수 있다.
