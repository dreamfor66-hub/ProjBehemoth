# Buff 통합 및 격노 → 분노 전환

## 요구사항 체크리스트

- [x] Fury를 BuffData 중첩으로 표현하고 일반공격으로 12중첩 획득한다.
- [x] Fury 40 이상에서만 차지로 Rage에 진입한다.
- [x] `AddBuff(Rage)` 계산기: `BuffStacks / Fury × 1`. 이어서 `RemoveBuff(Fury)`.
- [x] 48을 쌓았다면 48로 시작한다. OnBuffStart의 100 재충전 효과를 제거했다.
- [x] 소수 중첩을 보존한다. 77.25의 전환과 0.1, 1.25 단위의 소모도 반올림하지 않는다.
- [x] 0중첩 부여는 버프를 만들지 않는다. 0까지 소모하면 해당 버프를 종료한다.
- [x] 분노 중 무기 +30%, 공격 대기 -20%, 체력 피해 -25%, 기본 초당25 소모를 유지한다.
- [x] 관련 유물의 격노 획득·소모·계산기 참조를 Fury/Rage 버프로 이관한다.
- [x] 몽크 입력 연계·추가 타격·가드 회복·긴 패턴 해금을 BuffData로 이관한다.
- [x] FormData, CombatResourceData, 런타임 자원 사전, 관련 효과·조건·계산기 선택지·캐릭터 필드를 삭제한다.
- [x] 기존 Form 자산 7개와 자원 Fury 원본을 Assets에서 제거한다. 이관된 버프는 기존 버프 GUID를 유지한다.
- [x] 새 상태·중첩 HUD와 유물 설명은 TextMeshPro 및 한국어 키/CSV를 사용한다.
- [x] 저장된 자산으로 준비되며 수동 Tool/메뉴 조립이 필요 없다.
- [x] 여섯 캐릭터의 기본 체력·가드·회복·공격·차지·대기 수치는 작업 전과 동일하다.

## 편집 위치

- `Assets/Data/Behemoth/Buffs/Fury.asset`, `Rage.asset`: 각각 격노·분노 중첩과 상태.
- `Assets/Data/Behemoth/Relics/Starter_Barbarian.asset`: 진입 조건, 획득, 전환, 비분노 소모.
- `Assets/Data/Behemoth/Buffs/Burst.asset`, `Multi.asset`, `Shelter.asset`, `Flow.asset`, `Tiger.asset`, `Dragon.asset`: 몽크의 입력 조건과 행동 Units.
- `BuffStack` 조건과 계산기 `BuffStacks`에서 참조 버프를 직접 선택할 수 있다. 참조를 비우면 현재 실행 중인 버프를 읽는다.

## 검증

- 단위 검증 138개 통과. Fury 39.9/40/48/77.25/100 경계, 중첩 이동 순서, 소수 소모, 종료·재획득, 임의 버프 간 계산기 참조, 기존 몽크·사무라이 기능 포함.
- Unity 6000.0.59f2 열린 에디터의 실제 Play 모드에서 홀드 차지·전환량·HUD·색상·소진 종료를 확인했다. 36중첩에서는 분노에 들어가지 않고, 4번째 일반공격 후 실제 차지로 현재 중첩만큼 전환했다.
- 전체 164개 중 163개 통과, 1개 실패. 실패는 이전부터 알려진 `Balance_SixCoreLoops_SustainedDpsProbe_AndVisibleDog`의 로그/워리어 지속 DPS 비율 기준(현재 약 0.696)이며, 이번 작업의 모든 기능 검증은 통과했다. 테스트 기준을 낮추거나 기본 전투 수치를 조정하지 않았다.
- 3막×6직업 엘리트 비교와 기존 몽크·사무라이·로그 은신·다중 접촉 입력·이벤트·UI 회귀도 완료했다. 결과: `Logs/Behemoth-BuffUnificationFullRegression.xml`, 행렬: `Logs/Behemoth-BuffUnificationFullMatrix.txt`, 비교: `Logs/Behemoth-BalanceMatrix.txt`.
- 최초 전환 입력 테스트의 무리 배치에서는 홀드 시작점이 전장 밖에 놓였다. 단일 몬스터 조우로 설정을 고치고 실제 차지 상태에 대한 검증을 추가한 뒤 통과했다. 게임 입력 규칙을 완화하지 않았다.
- 실기기 터치와 장기 빌드 밸런스는 별도 검증 대상이다. 이전부터 알려진 로그 지속 DPS 비교 기준 미달은 이번 통합과 별개로 유지한다.

## 복구 원본

`Docs/MigrationArchive/FormResource-v14`에 제거한 자산·타입·전환 코드 원본을 보관했다. 작업 직전 Scripts/Editor/Data/Tests 사본은 `Logs/BuffUnification-v14-Backup`에 있다. 원본을 Unity가 다시 불러오지 않도록 Assets 외부에 보관한다.
