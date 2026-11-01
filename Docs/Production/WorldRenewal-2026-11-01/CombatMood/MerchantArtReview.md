# Three enemy families: production art review

Selected fourteen independent authored paintings and three independently editable portrait PNGs. Built-in `image_gen` only. This painting installation changed no combat data, attacks, monster tuning, native Sprite geometry, pivots, PPU or GUIDs. Root's subsequent native-pose runtime review moved one B1 Bell charge marker onto its raised hand; count, radius, required hits, exposure time and damage remain unchanged. See `NativePoseRuntimeReview.md` for that later, separately verified correction.

The exact installed sources were opened before generation: all fourteen poses, three portraits, B1 walls, original project archer and sphinx, and the user's Darkest Dungeon houndmaster. Each generation included actual project material and the user's mood reference. The later mimic poses use a mechanically assembled reference board so both B1 and the original archer remain in the five-input limit.

## Judgments and corrections

- Goblin hunter: reduced the broad bright eyes into small hostile slits inside a connected black hood/cheek mass. Retained olive hood, brown light equipment, straight spear and sheathed dagger. The first gritty output was rejected and regenerated with calm, untextured planes. All five poses use that clean rendering. Spear wind-up and thrust remain separate authored drawings.
- Ossuary bell: preserved the large bell, ring, bands, hands, feet, chain/clapper and eye location. Reduced the eye and made its upper lid severe; removed the bright clapper glint. The first Idle/Ready pair added scratched bronze and was rejected. Final bronze is broad clean matte planes; darkness comes from the actual cavity and overlaps. Dead eye stays closed.
- Iron-tooth mimic: matched the new ordinary chest's dull olive timber, two matte iron straps and square central lock plate. Replaced ornate golden star trim, bright pink tongue and large shiny mascot eye with the iron/keyhole eye, deep black mouth and dark maroon muscle. Source tongue/limb trajectories remain intact. Ready withdraws the tongue; Attack extends it. Dead eye stays closed. A tiny Ready eye glint remains after two local edit attempts; root accepted judging it by actual on-screen expression rather than treating one bright pixel as an automatic failure. Neither local attempt replaced the selected source.

`MerchantArtReview.jpg` compares the previous Idle with every selected pose at reduced display size. It was actually opened and reviewed after installation: no grain/scratch noise, consistent material identity, substantial interior blacks and identifiable weapon/body parts. Final GameView acceptance belongs to root's serial runtime review; this source-board check does not replace it.

## Installation evidence

- Production paths: `MerchantAssetFiles.json` (seventeen PNGs, usable by root's art-revision hash).
- Source output paths, original/source/output SHA256, original canvas dimensions, static framing compensation, preserved native metadata hashes: `MerchantInstallation.json`.
- All original PNGs, metas and native wrappers are copied under `../Before/Assets/Art/...` before replacement.
- Generated canvases occasionally have a different size; installation uses one uniform static fit onto each original canvas, never aspect stretching or procedural pose motion.
- Each portrait's previous exact crop was found against its old Idle at correlation ≥0.9999999, then the same pixel crop was taken from the new Idle. All original portrait dimensions and wrapper metadata stay unchanged.
- Prompts: `../CombatPrompts/`. The six-input draft prompts for Goblin Ready/Attack and Bell Attack were rejected by tool validation before producing images; the executed versions are the `-FiveInputs.json` files. Nineteen output images were produced: fourteen selected, three noisy early outputs rejected, two tiny-eye edit attempts unused.

The silhouette overlap measurement is diagnostic rather than proof of gameplay alignment: most selected poses exceed 0.94 IoU after uniform framing; Goblin Attack is 0.867, Goblin Stunned/Dead and Bell Stunned are 0.909–0.927. They keep the original pose concept and support points but are newly painted edges, and root should inspect those attack/weak-point views particularly closely.
