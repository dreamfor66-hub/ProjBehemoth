# 1막 몬스터 기절 원화 — 2026-10-30

## 요구 체크리스트

- [x] Maze 스테이지의 일반/정예/보스와 그 조우에 포함된 몬스터 12종을 실제 GUID 참조에서 추출 (`Roster.json`).
- [x] 기존 원화와 사용자 제공 원본을 실제로 열어 형태·색면·먹선 확인.
- [x] `Stunned` 전용 이미지 필드와 표시 상태 추가. 기존 enum 값은 유지하고 끝에 추가.
- [x] 공격 잔상보다 기절 우선, 사망은 기절보다 우선. 기절에 진입하면 남아 있던 공격 표시 타이머 해제.
- [x] 전용 기절 그림은 원래 색으로 표시. 미배정 몬스터는 기존 Idle 및 기절 색상으로 안전하게 대체.
- [x] 12종 개별 원화 제작 및 축소 비교 검수.
- [x] 원화 PNG 및 Sprite를 프로젝트에 저장하고 각 애니메이션 데이터에 연결.
- [x] 기존 Unity Editor에서 12종 기절 진입/유지/회복/사망과 표시 크기 확인.

## 아트 근거와 제작 방식

- 원본 스타일 근거: `Assets/Art/References/AngularCharacterStyle/Images/03-armored-hood-fighter.jpg`, `05-monkey-staff-fighter.jpg` (사용자 제공 EV GANIN 원본).
- 각 종의 현행 Idle PNG를 첫 입력으로 제공. 스핑크스는 교체된 `Assets/Art/World/ActOne/StoneSphinx-Idle.png`가 기준이며 구버전 States 이미지를 정체성 기준으로 쓰지 않는다.
- 설치된 `Assets/Art/World/Angular/Backdrops/B1-Forward.png`도 입력. 배경 자체는 출력에 포함하지 않는다.
- 내장 image_gen으로 종별 한 장씩 실제 투명 PNG 생성. 모델 출력을 포즈 회전/찌그러뜨리기나 기존 Dead 복제로 대체하지 않는다.
- 기절은 살아 있는 회복 가능한 붕괴 자세다. 처진 머리·팔·귀·날개, 굽힌 무릎, 느슨한 인형 실 등 종별 특징을 쓴다. 만화 별이나 효과 아이콘은 원화에 굽지 않는다.
- 굵은 외곽선, 넓은 내부 먹 그림자, 소수의 큰 색면, 기존 장비와 절제된 색상 유지. 잔균열·천 조각·다각형 명암을 늘리지 않는다.

## 설치/검증 범위

기절 그림은 `Assets/Art/World/ActOne/Stunned/`, Unity Sprite는 기존 `Assets/Art/Combat/NativeMonsterFrames/`에 저장한다. `MonsterStunPreparation`은 검수된 Install.json과 Ready.txt가 있을 때 한 번만 연결하며 Installed.txt 이후 디자이너 변경을 덮어쓰지 않는다. 수동 메뉴 조립은 없다. PNG별 투명 경계와 기존 원화의 픽셀 스케일로 프레임을 만들고, 발밑은 현재 히트박스의 최하단에서 계산한다. 추가 런타임 자세 이동은 없다.

검증은 현재 Editor 한 인스턴스에서 바뀐 그림 전환만 대상으로 한다. 테스트용 복제 데이터 외 체력/공격/기절 시간/히트박스/약점은 변경하지 않는다. DPS 또는 막 전체 밸런스 검증은 이번 범위가 아니다.

## 완료 / 검증 결과

- 기존 Unity 6000.0.59f2 Editor에서 실행. 새 Unity 인스턴스나 백그라운드 밸런스 스위트는 실행하지 않았다.
- [Live.txt](Live.txt): 12종 × 11개 전환/프레임/크기/색상 점검 + 미배정 대체 1개 = **133개 통과, 실패 0**.
- [AssetsVerification.txt](AssetsVerification.txt): 새 native Sprite 12개 유효성 + MonsterData 원본 바이트 보존 12개 = **24개 통과**.
- [실제 게임 내 12종 비교](LiveContactSheet.jpg), [어두운 배경 원화 비교](PoseReview-Dark.jpg), [밝은 배경 비교](PoseReview-Light.jpg), [아트 검수](ArtReview.md).
- 실제 테스트는 원본 데이터의 runtime-only 복제본으로 진행했다. 기절 전환 검증 후 Play 모드를 종료했으며 테스트용 씬/오브젝트를 저장하지 않았다.
- 초기 설치에서 JsonUtility가 Rect의 x/y/width/height 프로퍼티를 읽지 않아 빈 프레임이 저장됐다. 명시적인 직렬화용 Region 필드로 수정하고 12개 Sprite를 모두 다시 저장했다. FramesReady.txt 및 native geometry/pivot 검사로 수정 결과 확인.
- 프롬프트 원문: [1그룹](Prompts.json), [2그룹](Prompts-Group2.json), [3그룹](Prompts-Group3.json), [거미 최종 수정](BoneScorpion-Correction.json). 제작은 내장 image_gen 사용.
- 최종 원화: [Assets/Art/World/ActOne/Stunned](../../../Assets/Art/World/ActOne/Stunned/). 소스 PNG의 원래 해상도와 알파를 유지한다.

개별 무기나 약점 타격, DPS 수치, 전체 막 난도 및 실기기 터치 감각은 변경하지 않았고 추가 검증 범위에도 포함하지 않았다.
