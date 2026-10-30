# F1 image catalog — 2026-10-29

- [x] Monster, perk, relic and potion tabs with image cells instead of overlapping names.
- [x] All 44 authored monsters available, including encounter-only and training monsters omitted by the legacy combat lists.
- [x] 44 individually framed face portraits, saved as native Sprite assets under `Assets/Art/Enemies/Portraits/Catalog`. Each uses the installed Idle painting's original texture; combat artwork is preserved. `Portraits-Review.png` shows the crops at icon size; `Sources-*.png` record the originals.
- [x] All 41 perks, 92 relics and 7 potions use their installed icons. New catalog entries can grow the cell pool without a fixed button limit.
- [x] Side popup with localized name, description and monster pattern/stats; every current description checked for overflow in Play mode.
- [x] Clipped vertical list, wheel/drag support, automatic scrollbar visibility when content exceeds the viewport.
- [x] Gold selection border, selected name, category-specific grant/battle action, duplicate relic and full potion-slot feedback.
- [x] Entire panel fits the live canvas, including the footer; infinite-battle button displays its ON/OFF label.
- [x] Saved, editor-visible scene/prefab and Korean keys/CSV/table; idempotent preparation requires no menu action.

`LiveMatrix.txt` records the scoped 1920×1080 Unity Play-mode matrix. Only the changed F1 surface and its grant/battle entry actions were exercised. Combat tuning and DPS/balance comparisons were not run. Real-device testing remains pending.

Physical-input checks and the final scrollbar geometry review are recorded in `PhysicalInput.txt`.
