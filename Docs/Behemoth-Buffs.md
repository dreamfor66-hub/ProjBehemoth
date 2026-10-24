# Buff 데이터 편집

기준 프로젝트는 **Smash-Dungeon**이다. `BuffData`, `BuffUnitData`, `BuffReleaseData`, `BuffDuplicationData`, `BuffManager`, `BuffUnit`, `BuffUnitDataPropertyDrawer`의 구조와 실행 단계를 비교했다. SuperNinjaDaisen의 버프도 비교했으나, 서로 다른 중복·예약 처리에는 Smash-Dungeon을 기준으로 삼았다. 3D 액션·탄환·사운드 등 그 프로젝트 고유 시스템까지 복사한 것은 아니다.

## 편집 위치

- `Assets/Data/Behemoth/Buffs`: 실제 상태와 동작.
- `Assets/Data/Behemoth/BuffTags`: 상태 그룹, 신호, 행동 표식.
- `Fury`, `Rage`, 몽크의 여섯 연계도 모두 `Buffs` 안의 BuffData다. FormData와 CombatResourceData는 삭제했다.
- `BuffData.attackPattern`: 선택적인 일반공격 연계 조건. 빈 목록은 일반 상태 버프다. 캐릭터의 `attackBuffs` 목록에 연계 버프를 연결한다.
- 기존 유물의 `Abilities`도 Buff와 같은 Unit 형식을 사용한다.

Unity 6000.0.59f2를 쓰는 `E:/Github/proj_collapse`에서 Odin을 복사했다. 한국어 이름·설명 편집과 CSV 가져오기/내보내기는 그대로 유지된다.

## Unit 읽는 순서

**조건 → 트리거 → 대상 → 순서대로 효과 실행**

조건 목록은 AND다. 반전으로 부정을 표현한다. 다음 Unit의 ‘조건 / 트리거 공유’를 켜면 이전 그룹의 조건 판정·주기·횟수·확률을 함께 사용하고, 대상과 효과 목록만 추가한다. 효과 순서는 목록 순서다.

- `OnHit`: 공격 종류·방향 필터가 나타난다.
- `OnTimer`: 초 주기만 나타난다. 공격 필터의 이전 값은 실행에서도 무시된다.
- `OnSignal`: 지정한 신호만 받는다.
- `OwnerStat`: Buff가 유지되는 동안의 스탯 / 규칙.
- 유물의 `Passive → OwnerStat`도 같은 계산을 쓴다. 조건을 공유한 여러 스탯, Buff가 유지되는 동안의 동료 동행·연계 버프 사용 허가(`UnlockBuff`)도 이 방식이다.
- `Action`: 공격 실행 전에 배율·추가 적중·차지 판정·행동 태그를 변경한다.
- `Hit`: 실제 피해 계산 전에 해당 적중의 보정을 변경한다. 후속 회복 등은 계산된 피해를 참조한다.
- `ThisBuff`: 현재 Buff 제거, 스택 변경, 내부 값 변경.

비율은 Behemoth의 기존 수치 단위인 **1 = 100%**를 유지한다. 예: 피해 보너스 0.3은 +30%, 배율 1.5는 ×1.5다. `chancePercent`만 0~100이며 0은 발동하지 않는다. 값 변수는 기본 값에 각 참조 값 × 계수를 더한다.

## 실행·중복·해제

1. `UpdateState`: 시간 진행 후 이전 단계에서 예약된 명령을 FIFO로 적용한다.
2. `Cast`: 시작 → 스택 알림 → 주기/조건 충족 → 종료를 처리한다.
3. 효과에서 추가·제거한 Buff는 **다음 상태 갱신**에 반영된다. 같은 프레임에 무한히 재귀 생성하지 않는다.

중복 방식은 별개 인스턴스 / 기존 유지 / 새 Buff로 교체 / 스택이다. **Smash-Dungeon과 마찬가지로 Buff 자산 이름이 같으면 동일 Buff로 취급한다.** 서로 다른 상태에는 서로 다른 자산 이름을 사용한다. 같은 태그를 중복 기준으로 삼을 수도 있다. 시간 갱신, 상한에서 재부여 시 갱신 여부도 데이터로 정한다. 후자는 기존 독의 사용감을 보존하기 위한 추가 옵션이다.

해제는 영구 / 즉시 / 시간 / 전투 수 / 실제 Unit 발동 횟수다. 횟수 해제는 병합한 효과 개수가 아닌 그룹 발동 횟수를 센다. 전투 종료 정리는 시간 만료와 달리 종료 효과를 재생하지 않는다. 영구 Buff와 여러 전투에 걸친 Buff는 유지한다.

## 연결 예

- `Sheathed`: 공격 전 발도 태그·배율 부여 → 공격 종료 후 자신 제거.
- `SheathParryWindow`: 짧은 시간 가드 비용 0 → 준비 Buff 추가 + 패리 신호.
- `PreparedDraw`: 다음 공격을 차지 판정으로 변경 → 공격 종료 후 자신 제거.
- `Fury`: 일반공격으로 12중첩 획득. 비분노 상태에서 초당 2중첩 감소.
- `Rage`: Fury 40중첩 이상에서 차지할 때 `AddBuff(Rage)`의 계산기로 `BuffStacks / Fury × 1`을 읽는다. 이어서 `RemoveBuff(Fury)`를 실행한다. 48을 모았다면 48로 시작하며 100으로 채우지 않는다.
- `Rage` 자체가 분노 상태와 남은 중첩이다. 무기·방어·쿨타임 보정은 버프의 Passive Unit, 초당 25 소모는 ChangeBuffStack Unit, 0중첩이면 종료한다.
- `Burst`, `Multi`, `Shelter`, `Flow`, `Tiger`, `Dragon`: 입력 패턴이 선택한 BuffData를 공격 준비 전에 적용한다. `ScaleActionDamage`, `ActionExtraHits` 등으로 공격을 만들고, 공격 종료 후 제거한다. 효과·해금·추가 타격도 버프 참조로 편집한다.
- `FlowAcceleration`(연환 가속), `ShelterWard`(호신 보호): 연계가 남기는 후속 상태 버프다.
- `Poison`: 적이 Buff를 보유하고, 2초 주기로 스택을 참조해 피해를 준다.
- `DelayedDamage`: 받은 피해를 Buff의 초기 값으로 저장 → 주기적으로 지불 → 정상 종료 시 잔액 지불. 승리 정리 시 잔액은 사라진다.

계산기의 `BuffStacks`, `BuffElapsed`, `BuffValue`는 참조 버프를 직접 지정할 수 있다. 비워두면 현재 실행 중인 버프를 참조한다. `BuffStack` 조건도 참조 버프·태그와 비교식을 지원한다. 중첩은 소수값을 보존하므로 0.1씩의 소모나 77.25중첩 전환도 반올림되지 않는다. 0중첩 부여는 버프를 만들지 않는다.

새 아이템 ID나 직업 전용 변수를 추가할 필요 없이 Unit과 Buff 참조를 조합한다. 캐릭터·유물과 기존 버프의 GUID, 편집된 입력 패턴과 기본 전투 수치는 유지했다. 삭제한 Form·Resource 원본은 `Docs/MigrationArchive/FormResource-v14`, 작업 직전 전체 데이터·코드 사본은 `Logs/BuffUnification-v14-Backup`에 보관했다.
