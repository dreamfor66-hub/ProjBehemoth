# Weak-point target and break visual — 2026-10-24

## Requirements
- [x] Remove the white band and reduce the heavy closed-ring/lifebuoy silhouette.
- [x] Use the supplied red crosshair sketch as the design reference: interrupted arcs, four thin directional marks, a small central vulnerable core.
- [x] Replace old gold rectangle particles with fragments of the current target.
- [x] Share style/shape/color data and inherit actual destroyed target size, so later target changes propagate to the break effect.
- [x] Save inspectable style data and both prefabs; prepare automatically without a menu action.
- [x] Verify the target and shatter in live Unity: normal swipe, one-hit charge break, multi-hit partial/final break, optional target isolation, inherited style, cleanup.

Design reference: user attachments f89fc466-bf90-43e4-9b1e-bb8418305a41.png, e3dcc3f5-60f1-4ee9-9ddf-57e3621cac6f.png, and the hand-drawn 87930587-b42b-4113-85db-50f48c781074.png. The sketch supplies the shape; the palette is muted warm crimson to fit dungeon art.

Implementation: Assets/Data/Presentation/WeakPointStyle.asset drives WeakPointReticle and WeakPointShatterGraphic. CombatController passes the destroyed instance's style, tint and diameter. The intact target disappears at the final hit; pieces separate on the pixel grid and fade over the impact lifetime. Existing authored gold shards are disabled in the FX prefab.

Verification is scoped to the changed presentation in the existing Editor, using cloned practice monster attacks. No saved Warrior/monster pacing data or hit-radius values are changed. No DPS/balance suite is run. Real-device visual review remains pending.

## Final scoped runtime result

13 checks passed in the existing Unity Editor at 384×224. Production pointer handlers received synthetic contacts with real frame timing; practice monster/attack data was cloned.

| Case | Result |
| --- | --- |
| White band / actual final target color | PASS: no white target pixels; authored crimson survives the final palette pass. |
| Normal stroke | PASS: weak point stays intact. |
| One charged hit | PASS: one-hit target breaks, intact marker disappears, matching fragments appear. |
| Shared style, tint and diameter | PASS: burst inherits the destroyed instance; alternate shape/style requires no independent particle rebuild. |
| Multi-hit target | PASS: partial hit reduces the counter without final shatter; final hit shatters. |
| Optional target isolation | PASS: striking the required target does not consume the optional target. |
| Old FX / cleanup / hit radius | PASS: legacy gold bars inactive; new fragments clean up; hit radius unchanged. |
| Actual final fragment color | PASS: crimson pixels persist after the brief existing critical-hit screen flash. |

Measurements: Live.txt. Screenshots: Screenshots/Final-Target.png, Screenshots/OneHit-Break-Start.png, Screenshots/OneHit-Break-Fragments.png, and the multi-hit frames.

The palette reserves the three exact UI colors read from the shared style; the existing artwork color mapping stays in place. Particle fade uses pixel dropout so fading does not quantize crimson back to ochre. The initial color probe sampled during an unrelated critical-hit flash; the corrected final probe samples surviving particles after that flash. No product exception occurred during the final run. Play mode was stopped after verification.