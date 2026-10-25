# UI finish — 2026-10-25

## Requirement checklist

- Complete four-sided frames: 74 saved scene/prefab Image components now use `CrispPanelImage`, retaining their component IDs and Button targets. Four-unit edges match the art sampling grid. Body and border remain Inspector-editable.
- Popups: full parent anchors with equal 48-unit insets, including opaque story, shop, room event, reward, result, inventory, perk and spellbook panels. Map drawer has a 48-unit left inset.
- Decorative duplicate yellow lines: disabled the separate MainGameScene `TopAccent`/`Accent` objects; encounter banner keeps a complete gold frame. The full-screen event parent is only a dim backdrop; the inset StoryPanel is the opaque reading surface.
- HP/gold: one row, explicit widths and no wrapping. Status background spans both screen edges.
- Exploration and first combat guidance: deliberate narrative/confirmation modal, localized through CSV and the runtime table. The prepared first encounter starts only after acknowledgement and release of the confirming input; later encounters skip the general tutorial. Old bottom/central hints are inactive.
- Guard: four-unit border and neutral gray fill/track. Exact guard colors bypass palette remapping so the fill stays gray on screen.
- Relics: moved right into a separate 18-slot lane with 68-unit pitch. Character transforms are preserved.
- Compact shop rows hide their old description ScrollRect surface as well as its label, preventing it from covering the row frame. Purchase/leave buttons are bottom-anchored inside the panel.

## Verification

- Runtime, Editor and test assemblies compile. Unity's existing `RewardPopupView.animation` hiding warning is unrelated.
- Offline saved-asset/localization check: PASS. 74 retained frame references, 267 preserved serialized text fields, five matching new localization keys, neutral guard color, relic separation and equal popup margins.
- Existing typography coverage verifier: PASS after rebuilding the six font atlases for the tutorial text.
- Live Unity verification used the user's existing Editor at 1920×1080. No extra Unity instance or broad regression suite was started.

| Case | Result |
| --- | --- |
| Saved frame mesh, opaque reading panel, equal popup margins, status width, HP/gold no-wrap | PASS — scoped Editor test |
| Exploration modal closes; first combat clocks remain held; acknowledgement resumes; later combat skips tutorial | PASS — scoped live play-mode test; enemy timer, player cooldown and HP stay unchanged while reading |
| Exploration and combat tutorial modal, equal insets, no yellow ornaments | PASS — live screen inspection; screenshots saved |
| Compact shop frames and lower button clearance; leave button | PASS — live screen inspection and click |
| HUD full-width background, HP/gold one row, portrait/relic separation | PASS — live screen inspection |
| Neutral guard fill and reduced rim after palette pass | PASS — live screen inspection; `Screenshots/guard.png` |
| Encounter announcement frame | Saved gold four-edge mesh verified; short announcement was traversed, but no dedicated final banner screenshot was captured |

The keyboard Submit attempt did not dismiss the tutorial in the live Editor; the explicit Continue button did. Keyboard-only confirmation is not claimed as verified. The user-requested click confirmation flow passed.

Other aspect ratios currently have analytical layout checks only. Mobile device input and unrelated balance/regression suites were not run.

Final rerun: 2 passed, 0 failed. The test harness now waits for GameFlow run initialization and enables background updates only during the scoped test, restoring the prior value afterward. Earlier retries raced initialization; final XML and case logs are copied here. Final runtime assembly (including compact shop cleanup) and test assembly compiled in the live Editor.
