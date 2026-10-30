# 이벤트 일러스트와 팝업 · 2026-10-29

현재 아트: [InkStyleRevision/Review.md](InkStyleRevision/Review.md). 초기 그림은 사용자에게 반실사 묘사·약한 먹선 블랙·불명확한 초점으로 거부되었다. 초기 화풍 통과 판단은 철회한다. 아래 초기 과정은 이력이며 현재 화풍 승인 근거가 아니다.

## 범위
현재 데이터의 거울(Mirror), 순례자(Pilgrim) 이벤트 두 개를 사용한다. 이벤트 수/출현율, 보상/조건/결과 문구, 전투 밸런스는 수정하지 않았다.
그림은 built-in image_gen으로 각각 새로 제작했다. 초기 프롬프트는 Prompts.json, 현재 재작화 프롬프트와 실제 입력 이미지 목록은 InkStyleRevision/Prompts.json. 두 PNG는 1086×1448, 가로:세로 3:4다.

## 비교와 선택
- A: 같은 원화를 중앙 1:2로 잘라 실제 팝업에 배치. 읽기 영역은 넓지만 거울 프레임과 촛불, 순례자의 팔과 돌자루 주변 맥락이 줄어든다.
- B(선택): 3:4 전체 장면. 공간 설명과 인물/사물의 관계를 유지한다. 그림 왼쪽, 제목/설명 오른쪽 위, 선택지 오른쪽 아래.
- 선택지를 한 화면에 모은다(거울 3개, 순례자 4개). 긴 설명만 스크롤하며 선택지는 고정된다.
- 새 illustratedPopup 옵션을 켠 두 이벤트만 페이지 안의 문단을 함께 보여준다. 다음 이야기 페이지와 결과 이동, 조건/비용/보상은 기존 데이터로 동작한다.
- 첫 런타임 검사는 기하학적 겹침을 통과했지만 순례자 설명 영역이 96 canvas unit밖에 안 남았다. 이를 시각적으로 실패로 판단하고 폰트/행 여백을 조정했다. Iteration1은 그 이력이다. 최종 런타임 기록은 Runtime.txt.

## 그림 검토
- 거울: 옷방의 커다란 거울, 독립적으로 검을 든 익명의 반사상, 별도 촛불. 거울 밖에 특정 플레이어 클래스를 그리지 않아 클래스와 충돌하지 않게 했다.
- 순례자: 예배실의 노인, 돌을 받친 손, 자루 입구를 잡은 손과 무릎에 놓인 자루. 얼굴→손→돌의 시선 흐름을 확인했다.
- 초기 검수 오류: 반실사 얼굴·옷·돌 묘사, 부족한 먹선 블랙과 초점 문제를 작은 질감 차이로 축소해서 판단했다. 사용자는 이를 명확히 거부했다. 현재 두 그림은 실제 프로젝트 이미지를 입력으로 다시 그렸으며 InkStyleRevision/Review.md에서 별도로 비교한다.
- 1:2 비교판은 별도 그림 생성이나 왜곡이 아닌 동일 원화의 크롭이다.

## 레퍼런스
원본 캐릭터 레퍼런스 03-armored-hood-fighter.jpg, 04-brawler-back.jpg를 큰 형태/선/넓은 음영의 우선 기준으로 직접 관찰했다.
- Darkest Dungeon 공식 DD2 제단 이미지: https://mobile.x.com/DarkestDungeon/status/1573005615290789889
  이미지: https://pbs.twimg.com/media/FdRuLzPakAUz3UB.jpg
  중앙 초점과 따뜻한 광원/어두운 주변부 관계만 참고했다.
- DD1 Ancestor 이미지의 Steam 커뮤니티 미러: https://steamcommunity.com/sharedfiles/filedetails/?id=530463929
  이미지: https://images.steamusercontent.com/ugc/383162071970380779/1A928FB95397611CAA4E042D74684CD6C6925695/
  원저작 게임 아트의 커뮤니티 게시본이며 공식 제작 자료 페이지로 단정하지 않는다. 얼굴/손/광원의 명암 관계만 참고했다.
- 기존 프로젝트 DD 공식 UI 스크린샷: ../InkUI-2026-10-27/References/Official-4.jpg
참고 이미지는 References에 분리 보관하며 게임 소비 자산으로 쓰지 않는다.

## 적용 파일
- Assets/Art/UI/Events/Mirror.png, Pilgrim.png
- Assets/Data/Dungeon/Events/Mirror.asset, Pilgrim.asset
- Assets/Scripts/UI/IllustratedEventLayout.cs: 별도 팝업 배치. RunExpansionView의 인스펙터에서 비율/여백/본문·선택지 글자 크기 조절 가능.
- Assets/Scripts/UI/EventSequencePresentation.cs: illustratedPopup에 한해 문단을 모아서 표시.
- Assets/Scenes/MainGameScene.unity: 직렬화된 4개 전용 선택지와 배치 설정.
- IllustratedEventPreparation은 한 번 설치 후 Installed.txt로 재설치를 막아 디자이너 수정을 유지한다. 수동 Bake 메뉴가 필요 없다.

## 복원
Before에는 수정 직전 두 이벤트 데이터, 메인 씬, 세 원본 스크립트를 보관했다.
이 두 이벤트만 기존 표시로 되돌리려면 이벤트 데이터의 illustratedPopup을 끄고 Before의 원래 illustration/page illustration 참조를 복원하면 된다. 원화 파일과 전용 배치 코드는 유지해도 다른 팝업에 적용되지 않는다.
전체 코드 복원은 RunExpansionView와 새 partial 파일(및 이를 참조하는 준비/검증 코드)을 함께 처리해야 한다. 원본 스크립트만 단독 덮어쓰지 않는다.

## 최종 검증
기존 Unity Editor의 MainGameScene, 1920×1080에서 범위 한정 런타임 검사 43 PASS / 0 FAIL. 실제 두 이벤트의 전체 본문, 3/4개 선택지, 비활성 선택지 명도, 마우스 휠 스크롤, 끝까지 읽기, hover/press, 거울의 다음 실제 페이지, 일반 모달 복귀를 확인했다. 최초 오류 기록은 Iteration1에 보관하고 설치 완료 후 최종 검사를 수행했다. 두 이벤트의 문구·조건·비용·효과·라우팅이 백업과 같음을 Data-Scope-Check.json에서 별도로 확인했다. 전체 이벤트 개수/동선/결과 문구의 정리는 이번 범위에 포함하지 않았다.
