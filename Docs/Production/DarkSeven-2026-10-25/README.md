# Native-resolution seven-ink art revision

The rejected64x64 body conversions and integer letterboxing were reverted. Production textures retain their original canvas dimensions, sprite GUIDs/import settings, animation padding and canvasScale values. The existing384x224 presentation profile and full camera viewport are unchanged.

New artwork uses deliberate flat planes and purposeful structural contours. No dithering, texture-island filtering or low-resolution intermediate raster is used. The palette is black, four violet-charcoal grays, warm bone, and vermilion:000000 /19151f /332c3b /64586b /a398aa /eee5d5 /ef6548. This avoids the reference's teal/pink palette. Elements have no newly assigned color metaphor.

233 production PNGs were repainted/indexed. AssetAudit.json compares every canvas with Git HEAD and checks all opaque RGB values and binary alpha. All dimensions are preserved; all pixels use the allowed inks.

## Saved deliverables

- Production art: Assets/Art/Characters, Assets/Art/Enemies/Combat, Assets/Art/Environments, Assets/Art/UI/Title.
- Selected source sheets: Assets/Art/DarkSeven/Redrawn; source logo: Assets/Art/DarkSeven/Sources/Logo.png.
- Automatic native-size import: Assets/Art/DarkSeven/Import.json and Assets/Editor/Presentation/FlatArtImport.cs. No manual bake menu is required. Palette: Assets/Data/Presentation/ObsidianSeven.asset.
- Built-in image_gen was used for the repaints. The final prompt set is Prompts.json. Original reference packing and discarded early concepts are archived outside Assets in References/ and DiscardedSources/.

## Scoped runtime verification

The existing Unity Editor was used. Live.txt and Screenshots/ contain final1920x1080 camera captures, including native TMP composition: title, options, all six selections, details, all six player combat bodies, B1/B2/B3 boss artwork, charge/weak-point presentation, slash/break follow-up and guard. Every final capture contains only the seven inks (two-byte readback tolerance) with a full camera viewport.

The art practice encounter clones monster/attack data and leaves designer pacing and combat data unchanged. This is a visual/feedback check, not a balance or DPS suite. Real-device touch verification and broad balance regression were not run because this revision changes art/rendering.

Final assertions: charged production swipe consumed the authored one-hit weak point (remaining=0) and entered Stunned; guard was active while the raised-shield pose was captured. Both PASS in Live.txt. No new runtime/compiler errors were appended during final verification.
