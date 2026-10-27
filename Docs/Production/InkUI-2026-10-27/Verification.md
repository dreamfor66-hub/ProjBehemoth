# Ink UI 확인 기록

최종 확인: 2026-10-28, 기존 Unity 6000.0.59f2 Editor, DX11, Game View 1920×1080.
요청한 UI 변경과 직접 영향을 받는 상호작용만 확인했다. 별도 Unity 인스턴스나 DPS/전체 회귀 검증은 실행하지 않았다.

| 항목 | 방법 / 결과 | 증거 |
| --- | --- | --- |
| HP/MP/가드 최대 | 실제 연습 전투 100/100, 30/30, 35/35. 붉은 HP·청록 MP·뼈색 12칸 가드 표시 | After-Combat.png / .txt |
| 부분 잔량 | 실제 자원 40/100, 12/30, 14/35와 화면 약 40% 채움 대조 | After-Resources-40.png / .txt |
| 빈 잔량 | 0/100, 0/30, 0/35. HP/MP 채움 메시 정점 0, 가드 빈 홈 표시. 최대 잔량으로 복구 확인 | After-Resources-0.png / .txt |
| 충전 곡선 | HUD에 실제 충전 상태/진행도 50%, 100%를 전달해 홈·능선 표시 확인 | After-Charge-50.png, After-Charge-Full.png |
| 이벤트 선택지 | 실제 Hourglass 데이터. 마우스로 다음/이전 페이지 왕복 확인 | After-Event.png, After-Event-Page2.png |
| 기본/호버/눌림/비활성 | 기존 Button의 EventSystem 포인터 이벤트로 상태 전달. 눌림/호버 이미지 차이와 active/pressed 로그 대조 | After-Choice-Hover/Pressed/Disabled.png / .txt |
| 보유품·툴팁 | 원래 버튼 콜백으로 열기, 실제 장비 설명을 위에 표시. 텍스트·아이콘 가림/테두리 확인 | After-Inventory.png, After-Tooltip.png |
| 지도 | 정상 새 런의 지도 열기. 빈 제목판과 범례를 통과하던 이중 프레임 제거. 방/연결/방향 표시 유지 | After-Map.png / .txt |
| 보상·능력·상점 | 기존 View와 콘텐츠로 표시. 패널/자원 홈/선택 카드/비활성 구매 외관 확인 | After-Reward/Perks/Shop.png / .txt |
| 주문서 | 네 주문을 가진 연습 런으로 열기. 제목판 내부 배치 확인. 실제 마우스로 독안개→에너지 볼트 슬롯 전환, 설명/선택 테두리 갱신 확인 | After-Spellbook.png / .txt |
| 입력 차단 여부 | 모든 캡처의 새 InkUiSurface.raycastTarget=false. 원래 Button/콜백/입력 코드를 유지 | 각 .txt의 nonblocking=True |
| 저장·컴파일 | 자동 준비 후 씬/프리팹 저장, 건너뛴 dirty 씬 없음. 스타일 적용 revision 9. 최종 컴파일 및 확인 구간 예외 없음 | Preparation.txt, FinalLogAudit.txt |

자원 실험은 OctoberReviewLiveProbe가 만든 복제된 연습 전투에서만 실행했다. 적 행동 간격을 길게 하고 시뮬레이션을 멈춰 화면을 비교했으며, 원래 몬스터/전사 데이터와 밸런스는 바꾸지 않았다. 0 HP 가드는 사망 전환 대신 표시 스냅샷을 사용했다.

새 선/패널도 기존 TMP 합성 경로에서 원해상도로 그려져, 저해상도 확대 때문에 테두리가 굵어지거나 자원색이 다른 팔레트 색으로 치환되는 문제를 방지한다. RectMask2D/스텐실과 그리기 순서를 공유한다.

이번 확인에서 발견한 검증 보조 코드의 비활성 View 조회/요청 파일 경합 오류는 수정 후 재실행했다. 최종 확인 구간은 예외 없이 통과했다. 기존 RewardPopupView.animation의 CS0108 경고는 별도 잔존한다.

## 남은 범위

- 다른 화면 비율·해상도, 모바일 실기기의 읽기 크기/터치 느낌은 미검증.
- 입력 추적/제스처 로직은 바꾸지 않았다. 충전 게이지는 진행도 표시 확인이며 물리적 장시간 누르기/양손 멀티터치 재검증을 뜻하지 않는다.
- 긴 서사 스크롤의 모든 콘텐츠 조합, 비전투 캐릭터 선택 화면의 전체 조합은 별도 전체 회귀 대상으로 남긴다. 이번에는 표시한 패널의 마스크·겹침·읽기 상태를 확인했다.
- 공식 DD1의 프레임/색 위계를 Behemoth에 맞춰 적용한 결과다. DD1 HUD 배치·삽화·자원 의미를 그대로 옮긴 것은 아니다.

원본 참고와 해석: ReferenceAnalysis.md. 적용 전후: BeforeAfter.png. 공식 참조와 결과: ReferenceComparison.png.
