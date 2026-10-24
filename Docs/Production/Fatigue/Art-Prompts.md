# 피로도 아트 기록

2026-10-24. Built-in `image_gen` 사용. CLI/API 경로는 사용하지 않았다. 생성 원본은 Codex generated_images에 보존하고 프로젝트에 복사했다.

## 최종 사용 에셋

- `Assets/Art/Arcade224/FatigueFacesCloseup.png`: 6직업 × 4표정. `Assets/Data/Behemoth/Presentation/FatigueFaces.asset`에서 직업/표정별 Sprite를 교체할 수 있다. 동일 직업의 얼굴 방향·자세·배경·테두리 색은 고정하고 눈/눈썹/입으로 상태를 표현한다. 224×224의 정사각 영역을 32×32 논리 픽셀 HUD로 표시한다. Point 필터, 무압축, mipmap 없음, 비율 변형 없음.
- `Assets/Art/Arcade224/AdventurerCamp.png`: 임시캠프. 기존 사건 UI의 정사각 일러스트 영역에서 비율을 유지한다.
- `Assets/Art/Arcade224/FatigueFaces.png`: 첫 시안 보존. 작은 크기에서 표정 차이가 약해서 최종 HUD는 Closeup을 사용한다.

원본 파일: `C:/Users/user/.codex/generated_images/01a165fb-89c0-7853-894b-62442f48a155/` 아래 `exec-4df7c0b5-1a2a-4e28-be7f-506008cf46f9.png`(첫 시안), `exec-02f6ed0a-4b16-459d-9359-dad0e176c6ef.png`(최종 얼굴), `exec-1c2cd6fb-4bcc-453d-8f67-fd1d4ca5de80.png`(캠프).

## 얼굴 프롬프트 세트 · 제작 명세

Reference: existing HeroFacePortraits.png; preserve approved character likenesses. Production pixel-art sheet for a 224p first-person dungeon RPG, exactly four columns and six rows: Warrior, Rogue, Barbarian, Samurai, Monk, Wizard. Four expressions per character: rested, tired, exhausted, near collapse. Keep the same face direction, head pose, silhouette, palette, background and border within each row. Express fatigue through eyebrows, eyes and mouth, never through pose or colored borders. True low-color arcade pixel art, large clean clusters, no gradients or antialiasing, 12-color aesthetic, no lettering/UI. Each tile independently readable at 32x32.

Closeup revision: use the first sheet as the edit/reference target. Crop much closer to each face so eyes and mouth dominate the tile. Make the four expressions clearly distinct at native 32-pixel display while preserving the same identity, viewing angle and colors. Do not add body movement, border changes, labels or fatigue color overlays. Keep the 4x6 arrangement and square tile proportions.

## 캠프 프롬프트

Use case: illustration-story. One production pixel art event illustration for Dungeon Mortalis, a retro 224p first-person dungeon RPG. A safe temporary adventurers' camp tucked into an alcove in the demon king's underground stone labyrinth. FIRST-PERSON view from the entrance, looking at a modest small orange campfire with a low stone ring, one rolled blanket and one open bedroll, a leather backpack and a kettle, heavy blue-gray dungeon masonry behind it. No people, monsters, portraits, titles, logo, lettering or UI. Square composition for a small event illustration. True classic low-color pixel art, large clean pixel clusters, 64x64 native sprite aesthetic enlarged as nearest-neighbor pixels; maximum 12 flat colors: deep navy shadows, slate stone, muted brown canvas and warm amber fire. At tiny size the bedroll and campfire must be immediately readable, strong simple silhouettes with almost no tiny detail. Cozy fragile refuge within a dangerous labyrinth. No modern polished illustration, no gradients, no antialiasing, no excessive dithering, no exterior castle, no side-scrolling scene.

## 224p 검수

실제384×224 화면 캡처: `Screenshots/Face-{직업}-{100,80,40,8}.png`, `Camp-Choices.png`, `Danger-Tooltip.png`. 정사각 크롭, 같은 자세/색, 경고 가독성, 캠프 불/침낭 식별을 확인했다. 생성 고해상도 이미지를 화면 전체에 늘리지 않는다. 픽셀 군집의 최종 미감은 후속 플레이 피드백으로 조정할 수 있다.
