# Monster Inspector follow-up

- [x] Ready / Idle painting switch with shared authored hitboxes and weak-point coordinates.
- [x] Investigate duplicated hitboxes; remove only exact duplicates, retaining collision union and footprint.
- [x] Back side mouse button (Unity button 3 / Mouse 4) deletes selected or held weak point / hitbox; Undo supported.
- [x] Separate weak-point creation tab, click / drag placement, unique IDs and existing default radius / hit count.
- [x] Preview right-click menu copies / pastes the entire selected attack pattern. Paste replaces its list and supports Undo.
- [x] Scoped existing-Editor preview and native Inspector verification; production designer edits preserved.

## Duplicate cause and correction

The old capsule generator emitted two identical circles when its straight segment had zero length (`i=0..1`, extent=0). The previous migration preserved both. The live Unity audit removed 31 redundant circles from 31 monster assets. Only exact center-and-radius matches were removed; partially overlapping circles remain.

`Deduplication.txt` confirms identical collision unions, bounds and automatic footprints. `DataPreservation.txt` compares all 31 backups with current assets and confirms every other authored field is unchanged.

## Verification

- `Checks.txt`: 180 checks, zero failures. All 44 monsters select their actual Idle painting, retain authored weak-point coordinates across poses, avoid data mutation during preview and contain no exact duplicate hitboxes. Clipboard checks cover full field preservation, independent copies, invalid text and empty patterns.
- `Gestures.txt` and `Gesture-*.json`: native Inspector Idle / Ready rendering, drag-created weak point, context copy, selected-point deletion, context paste, Ctrl+Z and Ctrl+Y. Three-point source/paste/redo snapshots match exactly; two-point deletion/undo snapshots match exactly. Hitboxes remain unchanged.
- Physical Mouse 4 hardware verification remains pending: the desktop automation tool supports left/right/middle clicks only. Synthetic mouse-down events were converted to Ignore by Unity's GUI dispatcher and are not counted as passing tests. The common selected-weak-point deletion path passed through native Delete input.

No gameplay logic, tuning or pose images were changed by this follow-up. The existing scene was not saved or reverted. Temporary test assets are cleaned after verification.
