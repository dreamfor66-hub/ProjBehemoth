# 2026-10-24 지하미궁 타이틀

Built-in `image_gen` used once for the new background. No CLI/API fallback, no portrait regeneration, no frame generation.

Final project asset: `Assets/Art/Arcade224/TitleLabyrinth.png`.
Generated source: `C:/Users/user/.codex/generated_images/01a165fb-89c0-7853-894b-62442f48a155/exec-88275139-7000-4809-bac2-7570125b16f3.png`.

Final prompt:

> Create a single classic low-color PIXEL ART game TITLE BACKGROUND, wide 12:7 landscape. This is a sword-and-sorcery dungeon expedition about SIX HUMAN HEROES exploring an underground labyrinth created by a demon king, NOT gothic vampires. View: ancient broad stone entrance descending underground into a vast enigmatic labyrinth, square stone lintels and huge worn doorposts, warm torchlight near the entrance contrasting a deep jade/indigo underground glow. SIX tiny rear-view adventurer silhouettes stand together facing the downward stairs at the bottom edge, differentiated by shield, hood, axe, katana, bare fists, wizard hat. Environment dominates. Large quiet dark negative space in the upper middle for a game logo and centered menu. ABSOLUTE STYLE: design as a real 192x112 pixel image enlarged with nearest neighbor, VERY LARGE clean square pixel clusters, hard pixel edges, only 12 flat colors, no dithering, no texture grain, no stipple, no subpixel lines, no glossy bevels, no realistic lighting, no moon, no tall castle, no crosses, no hearts, no blood-red vampire imagery. No text or logo. Rectangular opaque full-bleed composition. It must remain clean at actual 384x224 game display.

The DEEP SIX wordmark is authored directly as pixel geometry in `PixelWordmark.cs`: square blocks, integer native pixel alignment, ivory/gold, no generated raster logo resampling. The six approved portraits remain unchanged as source art. Their import rectangles are384×576 for the96×144 native portrait and320×320 for40×40 faces, exact4:1 and8:1 sampling. Point filtering, no compression or mipmaps. Native screen is384×224 with integer display magnification. MainGame retains its low-color palette, plus the exact PDF guard blue-gray (147,171,182):17 allowed colors. OutGame portraits retain source art colors; only its background uses LowColorBackground.mat with17 colors. The final title screenshot contains18 colors including UI.

Actual in-game captures: `Screenshots/Title.png`, `Selection-*.png`, `Combat-Final.png`. Inspect these rather than accepting the large generated source alone.
