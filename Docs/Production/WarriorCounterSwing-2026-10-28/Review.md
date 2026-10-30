# 워리어 Ready / Attack 반작용 관계 수정

2026-10-28 사용자 지시: Ready와 Attack이 같은 바깥 공간을 쓰는 문제가 있어, 힘을 모으는 위치와 베어낸 위치가 반대가 되도록 워리어만 먼저 수정한다.

- [x] 기존 바깥쪽 Attack을 기준으로 안쪽에 모으는 Ready 선택.
- [x] Ready 원화 수정 및 기존 GUID를 유지한 실제 프로젝트 교체.
- [x] 실제 화면에서 누름/차지/해제 Attack/Idle 복귀 확인.
- [x] 가드와 공격 접점의 독립 유지/취소/재입력 3회 확인.
- [x] 다른 캐릭터 그림 및 기존 Attack/Guard 유지 확인.

## 현재 결과

현재 Ready: `E:/Github/Proj_Behemoth/Assets/Art/World/Angular/States/Heroes/Warrior-Ready.png`

고정 Attack: `E:/Github/Proj_Behemoth/Assets/Art/World/Angular/States/Heroes/Warrior-Attack.png`

검과 오른손은 Ready에서 왼쪽 어깨/몸 안쪽으로 모였다가 Attack에서 오른쪽 바깥으로 풀린다. 오른팔이 가슴 앞을 가로지르는 구간은 후면 카메라에서 몸에 가려지도록 그렸다. 손·칼자루·가드·칼날이 한 무기 축으로 이어지며, 손잡이나 팔을 중복 생성하지 않았다. 몸통의 안쪽 꼬임과 펼쳐진 Attack이 다른 실루엣을 사용한다. 런타임 키 회전/이동/스케일은 추가하지 않았다.

투구 후두부, 올리브 천/갑옷, 노란 스카프, 왼손 방패 정체성을 유지했다. 전후 몸통은 준비의 비틀림 때문에 완전히 같은 윤곽은 아니다. 기존 압축된 체형, 큰 색 면/명암 및 외곽선과 비교했으며 작은 삼각형 장식/재질을 추가하지 않았다. 원래 회색/검정 패널로 구성한 비교용 HTML은 저장했지만, 브라우저가 file 프로토콜을 차단해 렌더 검수하지 않았다. 원본 PNG와 실제 게임 화면을 직접 열어 검수했다.

## 근거와 생성

사용자 원본 `03-armored-hood-fighter.jpg`, `04-brawler-back.jpg`를 실제로 열어 형태 덩어리와 외곽선/명암을 비교했다. 기존 Warrior Attack은 디자인 고정용 편집 대상이다. 내장 image_gen의 이미지 편집 / 투명 배경 모드를 사용했다. 전체 최종 프롬프트·입력 역할·원본 출력·프로젝트 경로는 [Generation.json](Generation.json)에 저장했다. 이전 Ready는 [Before/Warrior-Ready.png](Before/Warrior-Ready.png)에 보존했다.

## 범위 검증

Before/After-Hashes.json: 18개 Ready/Attack/Guard 중 Warrior-Ready.png만 변경. 나머지 5종과 Warrior Attack/Guard는 바이트 동일. 에셋 메타 GUID 및 Hero-Warrior Ready 참조 유지 확인.

기존 Unity 6000.0.59f2 MainGameScene에서 워리어만 검증. [Runtime.txt](Runtime.txt): 23 PASS / 0 FAIL / COMPLETE. 차지 후에도 같은 Ready 그림 유지, 해제 시 기존 Attack 전환, Idle 복귀, 키 모션 미사용, 누락/파괴 이미지 fallback과 가드 동시 입력을 확인했다. Play 모드를 종료했고 잔여 요청 파일은 없다. 실제 모바일 기기 입력 및 밸런스 검증은 이번 범위가 아니다.

실제 캡처: `Docs/Production/ImageOnlyPoses-2026-10-28/Screenshots/WarriorCounterReady-Warrior-{Ready,Charged,Attack}.png`. 세 화면을 직접 열어 새 Ready/기존 Attack의 공간 대비와 흰 박스 없음 확인.

## 사용자 OK 이후에 이어갈 일

나머지 5종에도 힘을 모으는 준비와 반대 방향으로 풀리는 공격을 짝으로 검토·수정하는 일을 1막 전체 성장/난이도 밸런싱 작업의 요구사항에 포함한다. 현재는 그 이미지 수정과 성장·난도 보정을 시작하지 않는다.
