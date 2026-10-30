# Illustrated outgame — 2026-10-29

Implemented in Assets/Scenes/OutGameScene.unity. New title wordmark / entrance painting and six full-screen character scenes use the playable main-game hall's angular shapes, heavy black contours and restrained palette. Original character concept art is cropped into six bottom-row selection buttons; source paintings are unchanged. Left introduction / ability / starting relic, bottom horizontal choices and right embark action follow the requested STS layout and DD-style ink surfaces. Existing localization keys and TMP workflow retained.

## Latest user correction
The first Monk / Barbarian compositions imitated display poses too closely. Monk now uses touch to navigate a broken walkway; Barbarian follows through a two-handed chop into a gate beam. See ArtReview.md and Narrative-Revision-Comparison.jpg. The other four scenes were re-inspected for scene action, gaze, grip, shoulder/hip relation and environment contact. Identity references constrain character design, not the illustration's camera or stance.

## Assets and authorship
- Assets/Art/UI/Outgame/: six *-Illustration.png, six original-art *-Button.png crops, TitleLogo.png and TitleBackdrop.png.
- Built-in image_gen used for eight initial artworks and two targeted narrative revisions. Prompts.json and Revision2-Prompts.json contain the exact prompt set and reference roles.
- Data/Presentation/IllustratedOutgame.asset preserves authored color at 1920x1080.
- IllustratedOutgamePreparation is a one-time idempotent saved-scene migration; all art and UI references exist before Play. No bake menu step.
- Existing main-game scene, combat art and combat pacing were outside this task.

## Verification
Actual saved OutGameScene was run in the existing Unity Editor. Runtime.txt records 6 character clicks, full-screen art swaps, portrait hit targets, explicit right-navigation links, text bounds, details toggle, title/options/back and starting MainGameScene with the selected character. The runtime renderer issue found during visual review was fixed by serializing the shade's CanvasRenderer; stale earlier captures were superseded. All eight final title/selection/details captures were visually inspected. A later scoped details/pointer-feedback check is in Details-Verification.txt. Real-device touch and physical keyboard presses were not tested; navigation links and UI event handlers were exercised. No DPS/balance or broad regression suite was run.

Final visual evidence: Title-Live.png, Selection-*-Live.png, Details-Live.png, Selection-Overview.jpg. Earlier rejected composition art is in Revision1/. Backups and source-image hashes are retained here.

Final verification: 65 PASS rows / 0 FAIL. Pointer hover/down/up/exit reaches the ink surface. Detail layout is confined to the left column, with an independent extra-stat hover anchor. Original source art hash check passed.
