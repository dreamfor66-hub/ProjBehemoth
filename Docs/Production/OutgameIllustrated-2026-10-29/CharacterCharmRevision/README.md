# Character selection — character charm revision

2026-10-29 사용자 후속 지시에 따른 여섯 전체화면 선택창 그림 교체. 이전 OutgameIllustrated ArtReview.md/생성 그림의 배경 방향은 이 개정이 대체한다.

- 적용 PNG: Assets/Art/UI/Outgame/{Warrior,Rogue,Barbarian,Samurai,Monk,Wizard}-Illustration.png
- 실제 Unity 여섯 화면: Selection-Overview.jpg 및 Selection-*-Live.png
- 생성 그림만 비교: Artwork-Overview.jpg
- 변경 전 여섯 PNG: Before/
- 생성 방식: built-in image_gen. 원본 파일의 복사본을 프로젝트 경로에 적용했으며 CLI/API 대체 사용 없음.
- 최종 생성 원본 경로: SelectedSources.json
- 프롬프트: Prompts.json, Refinement-Prompts.json
- 직접 이미지 검토: ArtReview.md
- 실제 Unity 검증: Runtime.txt, Verification.json — 25 PASS / 0 FAIL. 이번 수정 범위의 여섯 선택창만 검증했고 전투/밸런스/전체 메뉴 회귀는 실행하지 않음.

입력 참고: 사용자 제공 원본 Assets/Art/References/AngularCharacterStyle/Images/03-armored-hood-fighter.jpg, 04-brawler-back.jpg는 선/명암/형태 경제성의 원본 기준이다. 각 캐릭터의 기존 정면 PNG(상위 SourceCharacters.json, SourceArtwork-Hashes.json)는 정체성/장비 참고이며 자세·카메라의 복사 기준으로 삼지 않았다. 기존 정면 PNG는 전부 SHA256 일치 보존. 최종 그림도 각각 원본과 현행 6종 배열 및 실제 UI로 대조했다.

영구 지침은 프로젝트 root AGENTS.md 및 Assets/Art/AGENTS.md와 Art/References/AngularCharacterStyle/STYLE_GUIDE.md의 2026-10-29 섹션. 이후 새 캐릭터/배경/타이틀 아트에도 동일하게 큰 형태와 절제된 외곽선·명암을 사용한다.
