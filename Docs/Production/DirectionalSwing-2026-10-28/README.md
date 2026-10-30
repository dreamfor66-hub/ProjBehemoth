# 워리어 양방향 베기 프로토

## 동작
- 전투 화면의 왼쪽 절반을 누르면 좌→우 Ready/Attack, 오른쪽 절반(중앙 포함)을 누르면 기존 우→좌 Ready/Attack.
- 방향은 접촉 시작 시 정해진다. 중앙을 넘거나 손가락을 추가해도 이미 모으던 자세가 반대 Attack으로 바뀌지 않는다.
- 각 접촉이 방향을 따로 가진다. 가드/주문 접촉은 Ready 방향 선정에서 제외한다. 기본 공격과 차지의 판정·피해·쿨다운은 기존 경로를 사용한다.
- 연속 타격은 최초 공격의 그림 방향을 유지한다. 다른 캐릭터나 누락된 방향 세트는 기존 Ready/Attack을 사용한다. 대체 그림 두 장이 모두 있을 때만 새 세트를 사용한다.

## 작화와 저장
- 기존 우→좌 Ready/Attack, Idle/Guard 및 사용자의 편집 파일은 덮어쓰지 않았다.
- `Assets/Art/World/Angular/States/Heroes/Warrior-{Ready,Attack}-LeftToRight.png` 두 파일과 메타를 추가하고 실제 `Hero-Warrior.asset`에 지정했다.
- 새 그림은 2896×1304 투명 캔버스를 사용한다. 기존 PNG나 캐릭터 픽셀은 축소/재작성하지 않았다. 새 생성물만 공통 배율1.12로 원화 크기를 맞추고, 머리 기준선을 등록했다. 자세 전환에 트랜스폼 이동·회전·스케일 키는 사용하지 않는다. 정확한 배치/원본은 Registration.json, Ready-Source.png, Attack-Source.png에 기록했다.
- 새 Ready는 오른팔이 가슴 앞쪽으로 접혀 후면 시점에서 가려지며 검날이 왼쪽으로 나온다. Attack은 이전 바깥 베기 후보의 오른팔/정수 그립을 바탕으로 오른쪽으로 풀고 긴 검을 옆 여백까지 펼친다. 방패는 계속 왼팔, 2리벳 견갑은 오른쪽 어깨다. 좌우 반전본을 사용하지 않는다.
- 최초 생성물은 검/방패 팔이 뒤바뀌어 제외했다. 두 번째 Ready와 새 Attack을 원본 스타일/장비 및 비교판에서 확인했다. 실루엣을 큰 면으로 유지했으며, 새 Attack의 길어진 검과 Ready의 몸통 꼬임은 사용자 시각 검토 대상이다. 사용자 승인으로 표현하지 않는다.

## 레퍼런스
- 사용자 원본: `Assets/Art/References/AngularCharacterStyle/Images/03-armored-hood-fighter.jpg`, `04-brawler-back.jpg`. 실제 원본을 열어 굵은 윤곽, 큰 명암 덩어리, 제한된 올리브/철색, 압축된 체형과 비교했다.
- 현재6종: `Docs/Production/AngularTrial-2026-10-25/StyleReview/AllSix-Corrected.png`.
- 이전 Attack 후보: `Docs/Production/WarriorCounterSwing-2026-10-28/Before/Warrior-Attack-Outward.png`.
- 실제 한손검/방패 연속 동작: [Sword and Buckler, p13 fig25–27](https://therionarms.com/articles/Schwert_Buckler_v050606.pdf). 저장본은 `../WarriorActOne-2026-10-28/References/Sword-Buckler-Page-13.png`. 감긴 팔과 몸통이 풀리는 참고이며, 사진의 특정 팔을 좌우 반전해 캐릭터에 대입하지 않았다.
- 정확한 생성 프롬프트: Prompts.json. 첫 Ready 제외 사유를 포함한다.

## 범위와 검증
기존 Unity Editor의 MainGameScene에서 실제 입력 Begin/Move/End와 전투 공격 경로를 사용한다. 손가락 ID만 합성한다. 양방향 눌림/Ready, 중앙을 통과하는 베기/Attack, Idle 복귀, 접촉 독립 취소, 가드와 겹친 추가 손가락의 해제/재입력3회, 다른 클래스의 기본 그림 fallback을 확인한다. 결과는 Runtime.txt, 실제 화면은 방향별 Ready/Attack PNG에 남긴다. 실제 모바일 기기 감각은 미검증이다.

현재 저장된 전투 프레이밍은 캐릭터가 화면 하단에 깊게 걸쳐 있어 칼/몸통 일부가 아래로 잘린다. 이는 기존 우→좌에도 존재하며, 사용자가 위치를 직접 맞추겠다고 한 지시대로 씬/프리팹 위치와 전투 줌은 변경하지 않았다. Comparison.png는 공통 배율로 원화 전체를 보여 주며, 게임 스크린샷은 현재 크롭 상태 그대로다. 입력 연결 통과와 최종 화면 배치 완료를 구분한다.

최종 실행은 48 PASS / 0 FAIL / COMPLETE. 양방향 차지 유지와 차지 발동도 포함한다. 최종 코드와 원화가 Unity에 반영된 뒤 실행했고 Play 모드를 종료했다. 잔여 요청이나 중복 검증은 없다. 기기 터치 감각과 최종 화면 배치는 별도다.

성장 밸런스, 다른 클래스 원화, 기존 우→좌 원화는 이번 변경 범위에 포함하지 않았다.
