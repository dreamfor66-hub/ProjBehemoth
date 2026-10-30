# Monster weak-point Inspector preview

## Requirements

- [x] Preview appears above MonsterData fields in the existing Inspector.
- [x] Preview shows Ready artwork and production weak-point UI without a background scene.
- [x] Select each authored Charge attack and edit its actual weak-point list inline.
- [x] Position, layout scale, combat scale, hit radius, and required hits update the preview.
- [x] Use MonsterRuntime.StartCharge, MonsterView.Bind, CombatSpriteAnimator, and WeakPointView.Bind directly; no independent spatial formula.
- [x] Preserve the existing Odin Inspector and Korean text-key/CSV workflow.
- [x] Show missing Ready / no Charge attack / empty weak-point-list states.
- [x] Use a hidden preview scene; do not modify saved scenes, monster tuning, or authored coordinates.
- [x] Verify coordinate changes and Undo/Redo on disposable data clones in the existing Unity Editor.
- [x] Compare actual live Skeleton and Sphinx target collision centers, radii, UI centers, and UI sizes.

## Use

Select a MonsterData asset. In **Ready · 약점 미리보기**, select the Charge attack, expand an entry in **약점 목록**, and edit **Normalized Position**. The upper preview refreshes automatically. **약점 판정 범위 표시** overlays the actual collision radius; it is independent of the reticle's minimum visual diameter.

The inline list edits the referenced MonsterAttackData asset. Other monsters sharing that attack also receive those edits. Ctrl+Z / Ctrl+Y work through SerializedObject. No bake or menu step is required.

## Verification

See `Verification.txt` and the adjacent PNGs. Scope is this Inspector and its directly affected runtime presentation, not balance/DPS regression.

Actual screen-space Canvas pixel snapping can move artwork vertices by less than one logical unit (Skeleton observed 0.433). Before that screen rounding, artwork geometry agrees to floating-point tolerance. Target centers and hit geometry agree independently of that rounding. The Inspector fits the simulation to available width, so it does not represent physical screen size.

Repeat scoped verification only when explicitly needed: write `edit`, `live:skeleton`, or `live:boss` to `Temp/MonsterWeakPointPreview.request` in the project root. Live commands require the existing Editor to be playing MainGameScene, prepare a cloned practice encounter, and do not create another Unity instance.
