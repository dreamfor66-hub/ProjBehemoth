# Behemoth typography assets

## Current: 이순신체 (2026-10-27)

All eleven roles in `Assets/Data/Presentation/Typography.asset` now use **Base → YiSunShin**.
This includes titles, narrative, descriptions, buttons, HUD, small counters and damage numbers.
Change a specific role's **Face** later to make a deliberate exception. Narrow-face substitution
and native-pixel size snapping are disabled. Original role bindings, localization keys/CSV,
layout and autosizing remain editable.

`YiSunShin.asset` is a dynamic 64px SDFAA TMP font with bilinear 2048px multi-atlas textures,
prewarmed from the Korean CSV. Its unmodified source TTFs and official provenance are in
`../YiSunShin/`. The sole source-missing CSV character is the em dash (—), which uses the
smooth Liberation Sans SDF fallback. Regular is the active weight; Bold is retained for later art direction.

Default TMP settings and every existing scene/prefab text reference are saved to the new font.
`YiSunShinPreparation` performs the initial migration automatically, skips dirty loaded scenes,
and leaves later designer choices alone. No manual menu/bake step is required.

Scoped live verification and screenshots are recorded in `Docs/Production/Typography/YiSunShin/`.
The old Python bitmap preview/rebuild utilities below target the retained pixel fonts; they are
not the rebuild workflow for this SDF font. Unity owns SDF creation and dynamic glyph population.

## Previous pixel-font setup (retained assets, inactive assignments)

The active role mapping is saved in `Assets/Data/Presentation/Typography.asset`.

DOS Story uses a mixed source: Korean/punctuation/symbols retain DOS Story; ASCII English letters
and digits use [DOS Gothic](../DosGothic/SOURCE.md) within the same TMP asset and material.
This works inside a single label such as `B1 · 잊힌 미궁`, including dynamically populated glyphs.
The separate derivative TTF keeps both original font sources available. Condensed and Galmuri7
roles (including the Tangba trial replacement) retain their own English letters and digits.

**Temporary Tangba trial:** in the active profile, uncheck **Use Tangba For Condensed**
to restore Galmuri11 Condensed for every Condensed assignment, including narrow-label alternatives.
The original Condensed asset and rules are retained. The checkbox is currently enabled.

| Role | Face | Reason |
| --- | --- | --- |
| Base, title, heading, action buttons | DOS Story (Base) | Default voice and short action labels |
| Descriptions, tooltips and story/event narrative | Tangba12 trial (Galmuri11 Condensed when disabled) | Long explanatory text within information panels |
| HUD values, costs, timers, damage, contextual hints | DOS Story (Base) | Consistent short labels and changing values |
| Tiny counts, page numbers and symbols | Galmuri7 | Compact numeric marks; word-bearing labels use Base (DOS Story) |

Titles and action labels may use Condensed when their measured text exceeds the available width. Role, localized content, width, design size and canvas scale are considered together.

Event popups explicitly separate modal title/choice titles (Story) from narrative/choice descriptions
(Tangba trial, or Condensed after rollback), for both normal and compact choice arrays.
Nested choice references are resolved by their serialized field paths, rather than treating every
label underneath a Button as a button title. Popup fonts are applied before measuring row heights.

To try Galmuri11 as the base, change **Base Face** in the active profile to **Galmuri11**. All roles using Base follow it, including buttons and HUD; explicit description/narrative and micro choices stay assigned. No scene rebuild is required. Regular Galmuri11 and Galmuri9 currently have no primary role assignments; Galmuri11 remains a missing-glyph fallback.

Each TMP label has an editor-visible `TypographyRoleBinding` with a role, a design size and a per-label pixel-snapping override. Normal output sizes snap to whole multiples of the font's native atlas sampling size. Very small output sizes do not force labels above their intended layout size. Authored autosizing remains available.

The new atlases sample DOS Story at 16 px, Galmuri11/Condensed/Tangba12 at 12 px, Galmuri9 at 10 px and Galmuri7 at 8 px. The Galmuri name is not the required rasterization size; see the [author's size table](https://github.com/quiple/galmuri#summary-of-supported-ranges).

Atlases are prewarmed from the Korean CSV and remain dynamic, with their occupied region reserved and source TTFs retained. Future text can add glyphs without changing the localization workflow. Missing source glyphs fall back to Galmuri11, then the original ArcadeFont. The original font asset and its source remain available.

Saved scenes and prefabs are ready for normal Unity import. `TypographyPreparation` assigns roles to newly authored labels on later project opens, preserving existing bindings and skipping dirty designer scenes. There is no manual scene-baking step.

Offline rebuild: `build_typography_assets.py` in `Assets/Editor/Presentation` requires Python, Pillow and fontTools. It preserves existing role assignments and the active profile. Pass `--sync-faces` to update saved primary font/material references from the active profile while preserving roles, sizes and content. `verify_typography_assets.py` checks these references and renders saved atlas pixels. These checks do not replace Unity runtime verification.
