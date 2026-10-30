# 미궁 스핑크스: 큰 몬스터 구도

## 기준과 제작

- 사용자 구도: Sphinx-UserComposition.png 오른쪽. 화면을 차지하는 몸의 부피·키·거리감이 핵심이며, 가로 비율 자체가 목표가 아니다.
- 정체성 수정 대상: Assets/Art/World/Angular/Monsters/StoneSphinx.png. 석재 사자 얼굴, 아이보리 갈기, 회색 몸, 어두운 날개, 황토색 가슴 문양 유지.
- 직접 확인한 사용자 원본: Art/References/AngularMonsterStyle/Images/04.jpg (EV GANIN 스핑크스), 09.jpg (타라스크), AngularCharacterStyle/Images/03-armored-hood-fighter.jpg. 날개 연결, 큰 앞발, 압축된 몸통, 카메라를 보는 머리, 큰 색/명암 면의 근거.
- 원본 스핑크스 자료가 있어 관련 이미지 근거는 충분하다. 작가 이름만으로 스타일을 지시하지 않았다.
- built-in image_gen 사용. 최종 전체 프롬프트는 Sphinx-Prompts.json. 세로로 세 프레임을 넣은 첫 출력은 몸이 납작해져 폐기. 독립 PNG 세 장으로 다시 제작했다.

## 소비 자산

- Assets/Art/World/ActOne/StoneSphinx-Idle.png
- Assets/Art/World/ActOne/StoneSphinx-Ready.png
- Assets/Art/World/ActOne/StoneSphinx-Attack.png
- 기존 MonsterData/CombatSpriteAnimation에 연결하며 새 몬스터 종류나 출현 기회는 추가하지 않는다. 지네 시안은 Docs의 RejectedCentipede에만 남긴다.

## 그림 검토

단일 포즈 수정본은 기존 석재 사자의 정체성과 큰 외곽 덩어리를 유지한다. Idle은 낮은 자세, Ready는 머리와 몸통을 세우고 앞발 간격 확대, Attack은 포효와 앞으로 뻗는 큰 발로 구분한다. 날개와 몸을 동일한 비율로 찌그러뜨리지 않는다.

현재 원화의 내부 면 분할과 윤곽이 사용자 원본보다 다소 매끈한 점은 남아 있다. 기존 자산과의 정체성 유지가 우선이며 이것을 전 아트 최종 스타일 완료로 판단하지 않는다. 실제 어두운 미궁 배경에서 표시 크기·대비·약점 위치를 검토하고 아래 런타임 기록에 결과를 추가한다.

## 런타임 검토

기존 Unity Editor에서 세 포즈와 충전 약점 표시를 캡처했다. 첫 실행에서 Stretch 앵커에 sizeDelta를 중복 적용해 화면 밖으로 커진 문제를 찾아 고정 앵커로 수정했다. 높이 500의 비례 맞춤으로 날개 끝과 앞발이 화면 안에 보이고 전투 영역 폭의 약 65%를 차지한다. 네 약점은 양 날개와 각 앞발에 놓인다. 부위별 몸 판정·바깥쪽 빗나감·네 약점의 입력 가능 범위 확인. 짧은 중앙 베기는 약점을 남기고 두 차례의 긴 날개/앞발 베기는 강타를 중단하고 기절시킨다. 단독/그룹에서 크기가 같다. SphinxRuntime.txt 및 Screenshots/Sphinx-*.png 참조.

입력 검증은 생산 코드 Begin/Move/End 경로를 기존 에디터 런타임에서 호출했다. 사람 손의 체감 및 실기기 제스처 검증은 남아 있다. 이 기록은 보스 HP/성장 밸런스 확정 근거가 아니다.
