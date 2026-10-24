# Arcade revision — implemented and scoped verification complete

Latest human specification supersedes every earlier animation prompt: ONE image per state, switched at gameplay state changes. Never resume the abandoned intermediate-frame generation.

## Current implementation
- [x] Native 384x224 framebuffer, 16-color palette, point scaling, Galmuri11 Korean TMP bitmap font.
- [x] First-person corridor into the screen. Front-facing enemies. Rear-facing player enlarged and cut at the waist by the bottom screen edge.
- [x] Six player classes: exactly Idle / Attack / Guard. No frame loops, elapsed-frame selection or FPS playback.
- [x] 53 unique monster pose sets, attached to all 55 MonsterData assets: exactly Idle / Ready / Attack / Dead. Training variants share their wooden dummy set.
- [x] Companion dog: REAR Idle / Attack only, next to the player. No Guard state or animation.
- [x] Real accepted attacks/spells, guard input, enemy windup/hit/death, and actual companion attacks trigger the image changes.
- [x] Saved state data/prefabs/scenes are inspectable; no manual bake step.
- [x] Character selection simplified; full-width battle; contextual map/bag; focus then confirm for perks and shop; paged story choices. MP moved above the battle; book/menu hidden during decisions.
- [x] 39 perks, 16 spells, 23 events and previous systems/data retained. Designer Warrior/B1/B2/B3 pacing fields were not changed.
- [x] Original geometric art references and old source art retained for art-only rollback. Never restore entire gameplay assets from the baseline zip.

## Scoped live verification
- [x] 42 representative-image checks passed in the existing Unity instance: all six classes, dog Idle/Attack while player guards, enemy Ready/Attack/Dead.
- [x] Nine input checks passed with Wizard selected: swipe, two simultaneous contacts, lift/repress while guard remains held, MP/cooldown rejection, guard MP recovery and cancel. An earlier fixture run with Warrior incorrectly expected Wizard MP recovery; that was a QA precondition mistake, not a production balance change.
- [x] Map and bag opened and closed with actual desktop clicks.
- [x] Perk focus/description/confirm/reward return clicked. Fixed reward confirm label clipping, added focused text colors and hid background decision controls.
- [x] Final shop layout, focus, 30G to 10G purchase, sold-out state and exit checked with desktop clicks. Fixed header/description overlap and clipped leave label.
- [x] Eleven live story/book checks passed after the compact layout: teacher rescue, missed lesson, refusal, growth, additional Meteor preserving Firebolt, re-equipping Firebolt and paid dog recruitment.
- [x] Art-only rollback round trip: 355 bindings restored; gameplay fingerprint identical before / geometry / restored. Actual geometric runtime rendered and captured. Native screenshots audited at 384x224 and <=16 colors.
- [x] Result screen and return-to-selection button clicked; character ability detail opened/closed. Final editor: OutGameScene, Play off, scene clean, no QA clone saved.

Physical mobile multitouch/small-device readability remain device verification. Do not run full balance or regression suites or spawn another Unity instance.

## Evidence and source notes
- Logs/Arcade224-State-Wiring.txt: 6 hero + 53 enemy + 1 companion profiles, exact 3/4/2 non-null states.
- Logs/Arcade224-Live-Poses.txt and Logs/AshenVow-Live-Input.txt.
- Screenshots/: native 384x224 runtime captures.
- State-Prompts.json and Assets/Art/Arcade224/States/PoseSheets.json: final representative-pose production.
- Animation-Prompts.json / Additional-Animation-Prompts.json: SUPERSEDED abandoned frame batches. Old generated source sheets are retained, but only three or four explicitly selected sprites are imported and referenced.
- CompanionDog.png and SpecialStates.png were generated with the built-in image_gen tool. Dog source exec-9fc5660f-5142-456d-bf61-7238e2090d55.png; special nonhumanoid source exec-66dd283c-3201-4309-b5c5-633029f92df5.png. Both are copied into Assets/Art/Arcade224/States.

Automation behemoth is paused. Stale queued heartbeats must not resume obsolete frame generation. Manual production in this chat is the current authority.

Final checkpoint 2026-10-24: 42/42 pose checks, 9/9 input checks and 11/11 story/book checks passed. Shop purchase/insufficient-gold/sold-out/exit and result/detail navigation were clicked in the live editor. MP top-bar visibility was checked after fixing draw order. No new errors since the corrected QA precondition run; historical error log retained. Latest code compiled. No balance suite was run. No more work is queued; physical-device verification is the stated limit.
