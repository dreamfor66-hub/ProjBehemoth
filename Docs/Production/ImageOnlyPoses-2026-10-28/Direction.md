# 이미지 전환만 사용하는 전투 자세

2026-10-28 사용자 후속 지시가 이전 ActOneExperience의 메시 변형 모션 기준보다 우선한다.

- [x] 공격 대기 흰 사각형을 6종 모두 기존 Unity 런타임에서 재현하고 Before 캡처 보존.
- [x] 상태·시간에 따른 회전/이동/스케일/찌그러짐 제거 코드 작성. static canvasScale은 투명 패딩 보정만 유지.
- [x] 기존 애니메이션 데이터의 키 모션 플래그와 수치를 저장 정리.
- [x] 플레이어 6종 Ready 원화 및 실제 참조 연결. 기존 idle/attack/guard 원본은 보존.
- [x] Idle→누름 Ready→해제 Attack→Idle, 취소/가드 동시 입력, 누락 fallback을 런타임 검증.
- [x] 원화 비교와 최종 화면 기록.

성장 모델·1막 난도 조정은 이번 범위에 포함하지 않으며, 사용자 OK 이후에만 다시 진행한다.

그림 근거: 사용자 원본 AngularCharacterStyle/Images/03-armored-hood-fighter.jpg, 04-brawler-back.jpg를 직접 확인. 현재 6종 rear/attack 그림은 정체성 보존용 수정 대상이다. 과한 잔면을 추가하지 않고 실루엣과 손/무기 구조로 준비 자세를 구분한다. 내장 image_gen을 사용한다.
