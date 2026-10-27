# 몬스터 바닥 기준 배치 / 해골·거미 수정

2026-10-27. 기존 Unity Editor에서 구현·검증. Built-in `image_gen`으로 투명 PNG 제작.

## 요구사항과 결과

- [x] Encounter offset 및 기본 포메이션은 원형 몸체 히트박스의 **아래쪽**을 지정한다. 중심 = 배치 좌표 + `(0, bodyHitRadius × VisualScale)`. 높이 18, scale 1이면 중심은 배치점보다 9 높다.
- [x] 크기/스케일이 다른 몬스터도 같은 기준을 사용한다. 공중 몬스터는 EncounterMember.offset.y를 높이면 된다. 기존 배치 수치는 일괄 재조정하지 않았으며, 좌표의 의미가 아래쪽 기준으로 바뀐다.
- [x] 42개 몬스터 이미지 라이브러리에 Idle 알파 하단·상단을 직렬화했다. 투명 여백 때문에 발이 뜨는 문제를 보정하고 이름/HP도 이미지 위로 배치한다. 모든 자세는 Idle 접지선을 공유해 공격 동작의 발 들기는 유지한다. groundContactY는 Inspector에서 수정 가능하다.
- [x] 궤도 이동은 자신의 배치 중심을 유지한다. 소환은 부모의 아래쪽 기준에서 새 몬스터의 높이를 한 번만 더한다. 충돌·약점·스킬은 변환된 중심을 공유한다.
- [x] 해골 전사와 거미의 Idle / Ready / Attack / Dead 총 8개 PNG 교체. 기존 GUID, 능력치와 전투 템포는 유지.
- [x] 레퍼런스 조사 지침을 Assets/Art/AGENTS.md에 기록하여 이후 채팅에서도 적용.

## 레퍼런스와 그림 검토

사용자 제공 EV GANIN 원본 01–15를 직접 검토했다. 해골은 14의 단순한 쐐기형 두개골, 검은 눈구멍, 적은 치아 홈과 굵은 갈비뼈를 우선했다. 02의 굵은 뼈 구조도 참고했다. 거미는 10 전갈의 짧고 굵은 다리, 큰 관절, 넓은 어두운 그림자를 참고했다. 05의 정면 위협 시선과 09의 낮은 무게중심도 비교했다.

추가로 EV GANIN skeleton/spider 및 Pinterest 자료를 검색했다. [해골 검색 결과](https://id.pinterest.com/pin/507499451816207068/)와 [작가 Pinterest](https://ru.pinterest.com/evganinpin/)는 접근이 403으로 차단되어 실제 그림 근거로 사용하지 않았다. 관련 원본이 충분히 있는 프로젝트 내 이미지 14/02/10을 최종 근거로 사용했다. 검색 결과 제목만 보고 특정 그림체라고 판단하지 않았다.

초기 해골의 긴 몸통·작은 삼각 음영을 줄이고, 얼굴/검/방패를 플레이어 쪽으로 향하게 수정했다. 거미는 장식·잘게 쪼갠 면을 줄였고, 다리 수가 잘못된 중간 결과를 폐기한 뒤 4개씩 총 8개의 다리로 교정했다. 정면 시선과 물어뜯는 자세를 상태 이미지에도 유지했다. [원본·8개 상태·현재 6캐릭터 비교](Review.png).

## 최종 파일 / 프롬프트

- 해골 Idle: `Assets/Art/World/Angular/Monsters/StringRevenant.png`
- 거미 Idle: `Assets/Art/World/Angular/Monsters/BoneScorpion.png`
- 나머지 자세: `Assets/Art/World/Angular/States/Monsters/{StringRevenant,BoneScorpion}-{Ready,Attack,Dead}.png`
- 완성 원본 복사본: [Generated](Generated/). 변경 전 파일: [Before](Before/).
- 프롬프트: [초기 생성](monsterIdlePrompts.json), [스타일 수정](monsterIdleRevisionPrompts.json), [자세·다리 수정](AdditionalPrompts.json). 외부 CLI 사용 없음.
- PNG 알파/크기/해시: [ImageQA.json](ImageQA.json). 기존 상태 제작 manifest/승인 revision도 새 이미지로 동기화했다.

## 로컬 플레이테스트

[Live.txt](Live.txt): **32 PASS / 0 FAIL**, 검증 후 Play 종료.

| 확인 | 결과 |
|---|---|
| 서로 다른 반지름과 scale 1 / .75 / .5의 접지, 중심, 이미지 하단 | 통과 |
| 높이18, scale .5에서 아래점+4.5가 중심 | 통과 |
| 실제 일반 공격, 약점 화면 좌표, 차지 공격 적중 | 통과 |
| 두 몬스터 × 4개 상태 이미지 전환 | 통과 |
| Y=140 공중 배치 및 .6 scale | 통과 |
| 궤도 이동 중심 / 실제 실패차지 소환 경로 | 통과 |

스크린샷: [서로 다른 크기](01-Mixed-Size-Grounding.png), [공중 배치](02-Air-Placement.png), 각 몬스터 상태별 PNG. 이 화면은 좌표 검증용 런타임 복제 데이터다. 전체 밸런스 비교 및 실제 터치 기기는 이번 범위에서 실행하지 않았다.
