# 편집 가능한 Texture 정리

프로젝트 AGENTS.md와 Assets/Art/AGENTS.md에 지속 규칙을 추가했다. 새 그림은 일반 PNG와 TextureImporter Sprite를 기본으로 사용한다. 기존 GUID/저작된 메시를 보존해야 하는 native Sprite는 같은 폴더에 같은 이름의 PNG를 두고 그 Texture에 직접 연결한다. 먼 공유 이미지나 시트에만 연결된 단독 Sprite를 새로 납품하지 않는다.

## 편집 위치

- 몬스터 초상화: `Assets/Art/Enemies/Portraits/Catalog/T_Sprite_Monster_이름_Portrait.png`
- 기존 몬스터 전투 프레임: `Assets/Art/Combat/NativeMonsterFrames/T_Sprite_Monster_이름_상태.png`
- 재제작 보스: 기존대로 `Assets/Art/Monsters/이름/T_Sprite_Monster_이름_상태.png`

이 PNG 파일의 그림을 수정하고 Unity로 돌아오면 기존 Sprite에 반영된다. 기존 .asset은 데이터 연결과 저작된 메시 호환을 위해 유지한다. 새 PNG가 각 Sprite의 실제 Texture이며, 별도의 갱신/굽기 메뉴는 필요하지 않다.

## 결과

- 초상화 44개와 전투 프레임 176개, 총 **220개**에 편집 가능한 PNG를 연결했다. 기존 12개 쌍을 포함한 native Sprite 232개 모두 동일 이름 PNG를 직접 참조하며 단독 Sprite는 **0개**다.
- 공유 시트와 먼 이미지에서 해당 영역을 분리했다. 분수 픽셀 크롭은 가장자리 보간용 여백을 포함하고 Sprite의 원래 크롭 크기·피벗·PPU·테두리·메시를 유지했다.
- 참조가 없어진 이전 PNG **161개**와 .meta를 삭제했다. 기존 임포트에서 축소되던 원화 **7개**는 현재 표시 해상도를 유지한 PNG를 추출하고, 큰 원본은 훈련 인형 및 ActOne의 Sources에 보존했다.
- 초상화 한 개와 전투 프레임 한 개의 PNG 픽셀을 임시로 바꾸고 실제 재임포트한 뒤, 기존 Sprite의 Texture에 변경이 도달함을 확인했다. 시험 후 원래 PNG 바이트를 정확히 복구했다. 결과는 EditProbe.txt에 있다.
- 독립 디스크 점검에서 변경한 220개 native Sprite의 GUID와 원래 메시/인덱스 버퍼를 확인하고 직렬화 파일 **2,957개**를 스캔해 삭제한 Texture GUID 참조 **0개**를 확인했다. DiskAudit.json에 결과가 있다.
- 기존 Unity Editor의 실제 실행 검증은 **55 PASS / 0 FAIL**이다. 몬스터 44종, 캐릭터 6종, UI 아이콘 160개와 실제 전투 그림 연결을 확인했다. 새 PNG를 사용하는 거미 Ready 전투 화면도 직접 검토했다. Live.txt 및 EditableSpider.png에 결과를 보존했다. 종료 후 Play 모드는 해제했다.

## 이행 중 교정

일부 원화의 임포트 해상도가 실제 파일과 달라 별도 처리했다. 또한 Sprite.Create/OverrideGeometry가 기존 메시를 재계산하는 차이를 검증에서 포착해, 원래 직렬화된 메시를 그대로 보존하고 Texture 좌표만 옮기는 방식으로 교정했다. 최종 검증은 교정 후 수행했다. 중간 진단은 ResolvedGeometryMigrationError.txt에 보존했다.
