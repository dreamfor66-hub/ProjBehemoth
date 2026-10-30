# 선택창 크기·도끼 비율·색감 / 새 몽크 일러스트

## 최종 적용

- 바바리안: 두 날 크기를 키워 긴 자루와의 비율을 보정. 몸/포즈/수정된 어깨 운반 방향 유지. 사용자가 다시 첨부한 첫 그림을 색상 기준으로 피부/갈색 가죽·털/회색 구름/황색 석양을 복원.
- 몽크: 기존 수반·물결·지하 아치 그림은 교체했다. 목조 수도원 수행방에서 붕대를 감으며 출발을 준비하는 완전히 새로운 장면/카메라/행동. 이전 그림을 확대하는 대안은 최종안이 아니다. 안대·염주·올리브 한쪽 어깨 천·붉은 허리띠 정체성은 유지.
- 위자드: 도서관/책상/책/양초/부엉이 구성을 보존하며 소폭 넓게 잡은 프레이밍.

적용 자산은 Assets/Art/UI/Outgame/{Barbarian,Monk,Wizard}-Illustration.png. 기존 경로와 메타 GUID,1672×941 해상도를 유지했다. Warrior/Rogue/Samurai PNG와 모든 여섯 메타 파일은 변경 전 SHA256 일치. 원화/버튼/전투 아트 및 UI 배치 변경 없음.

## 미리보기와 검토

- Final-Preview.jpg: 실제 Unity 바바리안/몽크 선택창.
- Selection-Overview.jpg: 실제 여섯 선택창 전체 비교.
- Artwork-Before.jpg / Artwork-After.jpg: 같은 표시 크기의 여섯 그림 비교.
- Head-Comparison-Before.jpg / Head-Comparison-After.jpg: 원본 픽셀 크기를 유지한 머리 부근 비교. 같은 상자에 맞춰 확대/축소하지 않았다. 수동 랜드마크는 대략적인 관찰값이며, 투구·후드·머리카락·수염/시점 차이가 있어 객관적 신체 측정이나 프롬프트 수치 충족으로 주장하지 않는다.
- PaletteAndNewMonk/Palette-Comparison.jpg: 사용자 지정 원래 색상 / 주황색으로 누적된 수정본 / 복원본. 생성 편집으로 색조와 재료색을 시각적으로 맞췄으며 이전 원본의 픽셀 그대로 되돌린 것은 아니다.

직접 확인: 새 몽크는 수반/물/올려다보는 옆얼굴을 사용하지 않고, 정면에 가까운3/4하향 얼굴과 실제 참고 사진의 붕대 당기는 손-손목 연결을 사용한다. 새 그림의 첫 결과는 머리가 너무 커서 머리/목 연결만 다시 비례 조정했다. 최종 얼굴은 다른 다섯 선택창과 비교했고 실제 UI에서 얼굴·흰 천 장력·양손의 주요 접점이 보인다. 원본 스타일2장과 정면 원화를 대조해 큰 면/선/의상 정체성을 확인했다.

바바리안은 도끼머리/체결부/자루/쥔 손 방향을 유지하고 날 비율을 키웠다. 반복 생성 중 색감이 누적되어 최신 첨부의 원래 색감으로 별도 복원했다. 새 재료에 작은 삼각형 면/파편 디테일을 추가하지 않았다.

## Unity 검증

기존 Unity Editor1개만 사용. 여섯 선택 버튼의 실제 콜백으로 전환하고1920×1080실제 화면을 캡처, 여섯 파일이 모두 다른 화면인지 확인했다. 최종24PASS /0FAIL — Runtime.txt, Verification.json. 실제 여섯 화면과 바바리안/몽크 확대 화면을 직접 열어 크기/가림/글자 대비를 검토했다.

처음 이미 실행 중이던 Play 세션은 스크립트 갱신 이후 Awake에서 연결하는 버튼 콜백이 사라진 상태여서5개 전환이 실패했다. Runtime-InheritedPlay.txt에 보존했으며 통과로 숨기지 않았다. 선택창을 새로 실행하여 확인한 결과만 최종 증거로 사용했다. 이후 사용자가 보던 타이틀 Play 화면으로 다시 열어두었다. 전투/성장밸런스/전체 메뉴 회귀 검증은 이번 작업에 포함하지 않았다.

## 백업과 쉬운 복원

Before/에 이번 요청 시작 시점의 여섯 PNG와 메타 파일을 모두 보존했다. PaletteAndNewMonk/Before/에는 색상/몽크 전체 교체 직전의 두 PNG도 별도로 남겼다.

아래 명령은 작업 시작 전으로 되돌린다. 메타/장면/UI는 보존하고 PNG만 복원한다. 캐릭터별로도 선택 가능하다.

```powershell
& 'E:/Github/Proj_Behemoth/Docs/Production/OutgameIllustrated-2026-10-29/SelectionScaleRevision/Rollback-Art.ps1'
```

몽크만 복원하려면 위 명령 끝에 `-Character Monk`, 바바리안은 `-Character Barbarian`, 위자드는 `-Character Wizard`를 붙인다. `-CheckOnly`는 파일을 바꾸지 않고 백업/경로/해시를 검사한다. 최신 결과 기준3장 모두 이 사전 검증을 통과했다. 이후 수동 수정이 감지되면 덮어쓰지 않고 중단한다. 실제 복원은 사용자 결과를 되돌리는 일이므로 실행하지 않았다.

## 생성 기록

Built-in image_gen 사용, API/CLI 대체 사용 없음. Prompts.json / Refinement-Prompts.json 및 Monk-*-Prompt.txt는 최초 크기 조정 기록. 사용자가 요청한 새 장면·원래 색감의 최종 프롬프트/참고 역할은 PaletteAndNewMonk/Prompts.json, Head-Proportion-Prompt.txt, References.md. 최종 생성 원본 경로는 SelectedSources.json. 원본 이미지 출처/자료의 워터마크는 참고 문서에서 유지했고 생성 자산에 복제하지 않았다.
