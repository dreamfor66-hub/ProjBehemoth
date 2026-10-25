# Native text separation verification

Date: 2026-10-24 (Asia/Seoul)
Environment: existing Unity Editor 6000.0.59f2, DX11, Game view 1920x1080.

## Requirements

- [x] All existing TextMeshPro UI text in OutGameScene and MainGameScene bypasses the 384x224 framebuffer and palette conversion.
- [x] Keep the current Galmuri11 font, font sizes, localized strings and authored UI hierarchy.
- [x] Preserve the pixelated artwork, button graphics and procedural symbols.
- [x] Save shader references in both production scenes; no scene-baking menu action.
- [x] Preserve authored ordering, CanvasGroup tint/opacity and RectMask2D clipping.

## Focused live matrix

| Surface / action | Result | Evidence |
| --- | --- | --- |
| Title menu and button labels | PASS; native text, original pixelated logo/buttons | Screenshots/title-fullhd.png |
| Wizard selection introduction | PASS; Korean strokes readable without changing font | Screenshots/wizard-fullhd.png |
| Starting relic tooltip | PASS; readable text, covered labels erased below popup | Screenshots/tooltip-fullhd.png |
| Click ability details | PASS; opens panel and changes label, no old description bleeding through | Screenshots/details-fullhd.png |
| Click start, transition to MainGameScene | PASS; native HUD / HP / gold / navigation hints | Screenshots/hud-fullhd.png |
| Open spellbook | PASS; heading, spell metrics and description visible, underlying HUD covered | Screenshots/spellbook-fullhd.png |
| Existing event ScrollRect with repeated localized narrative (runtime only) | PASS; actual wheel scroll changes position, text stays inside viewport and does not cover choices | Screenshots/scroll-top-fullhd.png; Screenshots/scroll-middle-fullhd.png |
| Instantiate existing critical damage number prefab (runtime only) | PASS; dynamic number, black outline and animation visible | Screenshots/damage-fullhd.png |
| Capture the 384x224 art framebuffer | PASS; menu labels absent from low-resolution texture, proving text separation | Logs/Arcade224-Native.png |
| Final compilation and runtime log | PASS; no new compile errors or rendering errors after final play-session start | Unity Editor.log |

No balance data or gameplay/input/physics rules were changed. No DPS suite, background Unity process or whole-system regression suite was run.

## Implementation / limits

The camera captures the current Canvas meshes. TMP shaders skip the artwork pass; a native-resolution layer replays text and erases coverage beneath subsequent UI graphics, then composites it over the pixelated artwork. No interactive objects are reparented or cloned. Meshes and draw data are reused; destroyed transient graphics are retired from the cache.

`ArcadeRasterCamera.Frame` now contains artwork only. Final UI screenshots must use ScreenCapture rather than that low-resolution texture.

At an actual output resolution of 320x224, the output itself still limits small Korean text. For font comparison use normal output resolutions; the Editor Game view was changed from 320x224 to Full HD for this review.

Pending: physical-device GPU / DPI / performance checks and alternative fonts or shader graphs. Current scenes use Screen Space Camera canvases with RectMask2D and the same bitmap TMP font. Stencil Mask components, world-space mesh text and arbitrary custom UI fragment alpha shaders are outside this scoped live matrix.