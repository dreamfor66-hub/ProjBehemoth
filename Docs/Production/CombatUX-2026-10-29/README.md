# Combat UX / 2026-10-29

요청한 UI·아이콘·이벤트 임시 그림·체급/간격·회복 중 hold 수정을 적용하고, 기존 Unity Editor에서 해당 범위를 검증했다. 마지막 상태는 MainGameScene, Play 종료, unsaved scene 변경 없음이다.

## 적용 결과
- 선택 카드마다 아이콘·이름·효과를 함께 표시한다. 긴 설명은 카드 안에서 스크롤되고 긴 이름은 28–36 범위로 맞춰진다. 선택 표식/테두리와 최종 획득 버튼을 유지한다. 유물 선택에도 동일 적용.
- 퍽41 / 유물96 / 포션7, 총144종을 각각 새로 그렸다. PerkAbilities39개를 포함한183개 데이터 참조를 교체했다. 원본PNG는1254² RGBA, Unity UI 수입은 max512. Icon-Provenance.md 및 IconSources에 제작 근거가 있다.
- 이벤트23개에 Pilgrim/Mirror 그림을 임시 배정했다. 페이지는 공통 그림을 상속하고 결과에도 유지한다. 이벤트별 전용 그림을 완성했다는 의미는 아니다. Event-Placeholders.txt에 임시 배정표가 있다.
- 2마리 기본 간격540→460, 작성된 조우는 기존 거리의80%를 목표로 몸통 여유를 확보했다. 큰 몸통이 겹치는 경우 줄임을 제한했다. 종별 크기는 인원수에 따라 줄이지 않는다.
- 해골 전사 시각 높이를 기준으로 고블린1.0, 거미0.48, 박쥐0.42, 작은 실인형0.34로 조정했다. artwork/body/weak-point layout도 함께 보정했다. HP·대미지·공격 주기·보상과 B2/B3 능력치는 변경하지 않았다.
- 회복 중 누른 공격은 Ready/차지를 표시하지 않는다. 계속 누르고 있으면 회복 직후0부터 차지한다. 텍스트 대신 HP 위 칼 실루엣이 어두운 상태→황동색 채움→상아색 사용 가능 상태를 표시한다. 회복 중 뗀 입력은 공격 예약으로 남지 않는다.
- 다수전의 추가 가드 점유와 공격 기회 손실을 독립적인 압력으로 평가하도록 root AGENTS.md 및 EncounterPressure.md에 저장했다. 임의 난이도 배수는 넣지 않았다.

## 검증 근거
- Runtime-FirstPass.txt: 카드,23개 이벤트 시작 화면, 회복/hold3회, 가드 접촉을 유지한 공격 lift/repress3회, 조기 release, 단독/2마리 크기, 약점 표시와 실제 hit center 일치.
- Runtime-Cards.txt: 실제 Camera를 사용한 viewport raycast/스크롤, 선택/확정 및 강조 상태 재검증6항목 통과. 첫 검사에서 Camera를 누락해 발생한 probe 좌표 오류도 수정했다.
- Runtime-Events.txt: 작성 페이지/결과 그림 상속75항목 통과. 결과/분기 효과의 전수 회귀가 아닌 변경한 그림 표시의 검증이다.
- Perks-Final.png, Relics-Final.png, Event-Temporary-Painting.png, Event-Temporary-Outcome.png, Recovery-HUD.png, Recovery-Ready-And-Charge.png, Pair-Scale-And-Spacing.png, Small-Species.png, Small-Species-WeakPoints.png를 직접 확인했다.
- Final-Data-Audit.json:144그림/183참조/23이벤트 연결 확인. 전체 DPS/성장 밸런스 검사는 실행하지 않았다. 입력 접촉 검증은 실제 Unity 프레임에서 합성 pointer 경로로 실행했으며, 물리 멀티터치 기기는 미검증이다.

## 보존 / 되돌리기
Before/Assets에 수정 직전 scene·script·data가 있다. 최신 사용자 수정과 다른 작업의 변경이 같은 파일에 있으므로 통째로 덮지 말고 해당 필드/변경만 비교하여 복원해야 한다. 새 아이콘은 Assets/Art/UI/Items2D에 따로 보존하며 기존 아이콘 파일은 삭제하지 않았다.

CombatUxPreparation은 저장된 scene/data를 한번 준비하는 Editor 코드다. LayoutInstalled/IconsInstalled/CardsPolishInstalled 마커가 재실행을 막는다. 특히 LayoutInstalled를 지워 전체 migration을 다시 돌리면 조우 간격을 반복 축소할 수 있으므로, 재조정 시 개별 필드만 수정한다. 검증 요청은 명시적인 Temp/CombatUx.request가 있을 때만 실행되며 별도 Unity 인스턴스를 만들지 않는다.
