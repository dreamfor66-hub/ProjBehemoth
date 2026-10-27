# 투구·팔 고정 방패·부족식 바바리안

최신 지시를 반영한 최종본은 04다. 손잡이를 잡게 만든 03 방패와 찢어진 허리천 시안은 미채택이며 현재 적용본에서 제거했다.

- 워리어 뒷면 투구: 사용자 빨간 윤곽에 맞춰 하단 양옆을 안으로 꺾고 짧은 목 테두리가 벌어지도록 수정했다.
- 워리어 방패: 앞·뒷면 모두 수정 전 방패 방향, 팔과 주먹 위치를 복원했다. 원래 팔 위에 가죽 고정 띠만 국소 합성했다. 주먹은 손잡이를 쥐지 않는다. 손잡이용으로 꺾인 팔을 재사용하지 않았다. 앞면 원본·검·다리 및 투구 수정본의 몸통은 보존했다.
- 곰가죽 바바리안: 두 다리를 덮는 가죽 바지, 넓은 보강 가죽, 옆선의 가죽 끈, 털 부츠로 바꿨다. 좁힌 눈과 건달 같은 웃음을 유지하고 도끼를 어깨 위로 올렸다. 도끼 자루가 두 손과 도끼 머리 사이에서 곧게 이어지는지 확인했다.
- 비교판: 실제 불투명 발바닥 최저점을 y=590 지면에 정렬했다. 이미지를 늘이거나 찌그러뜨리지 않았다. 설정 신장 자료가 없어 배율은 비교용 가정으로 기록했다. 바바리안은 위로 든 도끼를 몸 높이 계산에서 제외했다.

## 최종 저장 파일

- `Assets/Art/World/Angular/Heroes/Warrior-Rear.png`: 수정 투구와 원래 팔 자세의 고정 띠
- `Assets/Art/World/Angular/Heroes/Warrior-Front.png`: 원래 팔·방패 자세의 고정 띠
- `Assets/Art/Characters/Selection/SelectionWarrior.png`: 같은 선택창 구도의 새 파생 이미지
- `Assets/Art/Concepts/BarbarianBearPelt04.png`: 곰가죽·가죽 바지·위로 든 도끼 전신 시안
- `Assets/Art/Concepts/BarbarianBearPelt04-Selection.png`: 동일 전신에서 자른 선택창 상반신 구도 미리보기
- `GroundLineup-Dark.png`, `GroundLineup-Gray.png`: 발 위치를 맞춘 6종 비교

곰가죽 인간 바바리안은 계속 별도 시안이다. 게임의 기존 리자드 원본과 데이터는 교체하지 않았다. 승인된 원본 6종, 특히 로그 얼굴과 사용자 선택 사무라이는 변경하지 않았다. 게임 UX·레이아웃·배경·크기 설정과 전투 수치는 이번 수정에서 변경하지 않았다.

## 생성과 스타일 검토

내장 `image_gen` 편집 모드를 사용했다. 최종 입력과 프롬프트는 [FinalPromptSet.json](FinalPromptSet.json)에 있다. 사용자 원본 두 장(후드 중갑 전사와 등 상처의 격투가), 현재 몽크·워리어·리자드를 함께 비교했다. 바바리안의 가죽 옆선과 곰가죽은 다른 캐릭터보다 묘사량이 많지만 주요 재질과 실루엣을 설명하는 큰 요소로 제한했다. 근육·털의 작은 면은 이전 시안보다 줄었다. 상반신 크롭에서 도끼날과 두 손이 함께 보이는 것을 확인했다. 어두운 배경과 회색 배경 모두 검사했다.

[ApplyArt04.ps1](ApplyArt04.ps1)은 생성본의 가죽 띠 영역만 원래 자세에 삽입한다. [Pixel-Invariants04.json](Pixel-Invariants04.json): 앞·뒷면 각각 띠 영역 밖의 보이는 픽셀 변경 0. 완전 투명 픽셀의 RGB만 0으로 정리했다. HUD 초상화는 수정 전 파일 그대로 복원했다. 투구 생성본은 이전 [ApplyArt.ps1](ApplyArt.ps1)의 머리 영역 합성으로 보존했다.

## 검증

- 기존 한 개 Unity Editor의 실제 MainGameScene에서 수정 투구와 팔 고정 띠가 표시되는 화면을 확인했다. 저장한 게임 화면은 `Screenshots/Warrior-Rear04.png`.
- 기존 선택창에서 워리어 파생 이미지를 확인한 결과와 화면은 `Live-Selection04.txt`, `Screenshots/Selection-Warrior04.png`에 기록한다.
- [Live-World04.txt](Live-World04.txt): 기존 승인 구도와 버튼 위치가 유지됨을 확인한 제한된 아트 표시 검증. DPS·밸런스 전체 회귀나 별도 Unity 인스턴스는 실행하지 않았다.

[최신 비교판](Gallery.html)
