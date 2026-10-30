현재 워리어 Ready는 후속 [반작용 관계 수정 기록](../WarriorCounterSwing-2026-10-28/Review.md)을 따른다. 아래 워리어 행과 프롬프트는 이전 버전 제작 이력이다.

# Ready 원화 및 이미지 전환 검수

범위: 전투 자세의 키 모션 제거와 플레이어 공격 대기 흰 박스 수정. 성장 모델과 전투 수치는 이번에 변경하지 않았다.

## 제작 근거

사용자 제공 원본 `Assets/Art/References/AngularCharacterStyle/Images/03-armored-hood-fighter.jpg`, `04-brawler-back.jpg` 및 STYLE_GUIDE를 직접 확인했다. 각 캐릭터 원래 Rear를 수정 대상으로 사용하고 원본 Attack/Guard와 비교했다. 외곽선, 큰 그림자 면, 기존 의상·색과 후면 시점을 유지하며 손과 무기의 위치로 대기를 구분했다. 기존 그림의 잔면 밀도까지 전체 재설계한 작업은 아니다.

모드: 내장 image_gen, 원본 이미지 편집, 투명 배경. 전체 공통/개별 프롬프트와 폐기 사유는 [Prompts.json](Prompts.json)에 보존했다. 생성 이미지는 후처리로 늘이거나 회전시키지 않고 프로젝트에 복사했다.

## 선택 결과

|캐릭터|대기 그림과 구조 검수|저장 경로|
|---|---|---|
|Warrior|검을 어깨 뒤로 당긴 대기. 후면 투구와 방패 손잡이/손 보존.|E:/Github/Proj_Behemoth/Assets/Art/World/Angular/States/Heroes/Warrior-Ready.png|
|Rogue|양팔을 접고 역수 단검을 당김. 후드 후면이며 얼굴 수정 없음.|E:/Github/Proj_Behemoth/Assets/Art/World/Angular/States/Heroes/Rogue-Ready.png|
|Barbarian|양손으로 도끼를 머리 위로 듦. 양손 사이 자루가 연결됨, 곰가죽 유지.|E:/Github/Proj_Behemoth/Assets/Art/World/Angular/States/Heroes/Barbarian-Ready.png|
|Samurai|발도 전 몸을 틀고 팔꿈치를 들어 준비. 칼집과 손잡이 한 축, 3단 견갑·머리 유지. 최초 중복 손잡이는 폐기하고 원본에서 재생성.|E:/Github/Proj_Behemoth/Assets/Art/World/Angular/States/Heroes/Samurai-Ready.png|
|Monk|앞손은 펴고 뒷주먹은 당김. 붕대·염주·안대 후면 유지.|E:/Github/Proj_Behemoth/Assets/Art/World/Angular/States/Heroes/Monk-Ready.png|
|Wizard|지팡이를 어깨 위로 당겨 주문 준비. 자루 연결, 부엉이와 후면 머리/복장 보존.|E:/Github/Proj_Behemoth/Assets/Art/World/Angular/States/Heroes/Wizard-Ready.png|

원본 Idle/Attack/Guard 파일은 덮어쓰지 않았다. 사무라이 실패본은 Rejected/Samurai-DuplicateHilt.png에 별도 보관하고 플레이 에셋에는 연결하지 않았다.

## 라이브 확인

기존 Unity 6000.0.59f2 MainGameScene에서 수정 범위만 실행했다. [Before.txt](Before.txt)는 6종 모두 Ready 스프라이트 누락/흰 박스 재현, [After.txt](After.txt)는 168 PASS / 0 FAIL로 완료했다. 플레이어 6종 실제 Begin/Move/End 입력, 차지 유지, 놓아 공격, Idle 복귀, 누락 및 파괴된 Unity 이미지 fallback, 비어 있는 전체 라이브러리의 흰 사각형 방지를 확인했다. 가드와 공격 접점을 동시에 유지하면서 각각 놓고 다시 누르는 동작을 6종에서 실행했고 Warrior는 전체 해제 없이 3회 반복했다. 실제 모바일 기기 접촉은 미검증이다.

1막 12종의 Idle/Ready/Attack 그림 구분 및 실제 MonsterView 전환/고정 메시 처리를 확인했다. 시간·상태별 회전, 이동, 스케일 및 찌그러짐 코드를 제거했다. 모든 상태에 공통인 투명 캔버스 보정과 전투 입장 프레이밍은 유지했다.

Screenshots/After-{캐릭터}-Ready.png, -Charged.png, -Attack.png에 6종 실게임 화면을 저장했다. 6종 대기와 Warrior/Barbarian/Samurai 공격을 직접 열어 확인했다. 흰 박스가 사라졌고 후면 그림이 유지된다.

## 다음 단계

사용자 OK 전까지 성장 모델 보정 및 1막 전체 난이도/성장 밸런싱은 보류한다. 이번 결과는 전투 밸런스 완료 판정이 아니다.

몬스터 화면 기록 보완: 첫 촬영은 주문서 패널이 화면을 가려, 수치나 전투를 돌리지 않는 MonsterView 전환만 다시 실행했다. [MonstersAfter.txt](MonstersAfter.txt) 60 PASS / 0 FAIL. 미궁 스핑크스 Ready/Attack을 패널 없이 다시 촬영하고 직접 비교했다. 이 화면은 개별 뷰 검사용 배치이며 전투 배치/난도 검증이 아니다. 마지막에 Play 모드 종료 및 대기 요청 파일 없음도 확인했다.
