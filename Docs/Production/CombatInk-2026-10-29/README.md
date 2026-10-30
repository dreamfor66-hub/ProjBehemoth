# Combat ink HUD — 2026-10-29

Requirements
- [x] Attack-name plate: thick straight black brush, existing -3 degree angle retained, no popup rails.
- [x] Countdown: separate frameless narrow brush shadow, smaller seconds unit, live shrinking vermilion stroke.
- [x] Weak points: remove 4-unit pixel construction, purple lower half and crosshair spokes; continuous tapered vermilion ink arcs and bone center.
- [x] Break feedback: intact curved strokes split and fade, no square pixel dropout.
- [x] Multi-hit marks: native painted tallies, no gold outlined pixel bars.
- [x] Native composition: weak target / break / tallies render with existing native HUD, bypass low-resolution raster and palette projection.
- [x] Saved inspectable assets: MonsterView prefab skins, shared WeakPointStyle, existing target / break prefabs. Automatic editor preparation; no manual bake.
- [x] Scoped live matrix: 17/17 checks passed; final authored-position screenshot reviewed separately.

Reference: the user's three attached screenshots / black flat-brush example. Target and telegraphs use code-authored continuous geometry, avoiding raster downsampling as a substitute for simplification. Existing character/background paintings and combat numbers are preserved.

Verification scope: live timer decrement and hide, normal vs charged swipe, one-hit / multi-hit / optional point, break shape/style inheritance and cleanup. No DPS/balance suite. Actual device touch remains outside this desktop visual change.


Final visual review
- Reviewed Final-HUD.png at 1920x1080 and in the Unity Game view (0.64 scale): attack name retains -3 degrees; flat black stroke and quiet ends; countdown uses tapered translucent ink without rails and a separate vermilion duration mark; seconds unit is subordinate.
- Preview uses the Skeleton's existing authored charge attack and body-center placement, not test offsets. Both points remain legible over arm/shield without square pixels or purple stepped shading.
- Reviewed OneHit-Break-Fragments.png: curved marks disperse in the same drawing language. Remaining-hit tallies were included in the scoped matrix.
- Final Live.txt: 17 PASS, 0 FAIL, COMPLETE. Compiled in Unity 6000.0.59f2. Final preview left paused in the user's existing Editor for inspection.
- During compilation the new Inspector preview exposed an incompatible CanvasRenderer.GetMesh(mesh) overload; updated its mesh copy to Unity 6's GetMesh() API while preserving the existing preview implementation.
- Mobile hardware and broad balance regressions were not run; combat tuning and hit radii were unchanged.
