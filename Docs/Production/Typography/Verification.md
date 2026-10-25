# Font roles — 2026-10-25

## Local hierarchy polish

The four supplied screenshots were used as the design reference. The typeface trial, character art,
class names, gameplay HUD and shop grid arrangement were retained. Local text size changes are
serialized on the original TMP labels and TypographyRoleBinding components.

| Surface | Local adjustment |
| --- | --- |
| Shared event/confirmation popup | Header 44 → 48; narrative 44 → 36; choice explanations 40 → 32; choice names retain 44 |
| Character selection | Long story/ability/detail bodies 44 → 36; class name and compact HP/attack values retained |
| Both shared tooltips | Title 40 → 44; body 36 → 32; typography applied before height measurement; wrapped headings receive their measured height |
| Shop | Listed item titles retained; prices 32 → 38; wallet 44 → 48; focused description 36 → 30 with a separate Story title at 40 |

The changed labels use their authored sizes without the coarse whole-font-size snap. Previously,
Story and Tangba rounded in different 16 px / 12 px steps, occasionally reversing header/body
hierarchy. Other labels retain their existing snapping settings. Exact output pixel appearance at
fractional canvas scales still requires live visual review.

The new shop title is a saved TMP object with an inspectable Heading binding and serialized
`ShopView.focusedTitle` reference. It uses 52 units inside the existing 208-unit detail area;
the body uses 144 with a 12-unit gap. The overall detail bounds, shop grid and purchase controls
are unchanged. Existing scenes without the optional reference retain the combined-text fallback.

- [x] Runtime and Editor C# compiler checks pass (existing RewardPopupView warning only).
- [x] Saved references, native atlas assets, all 283 role bindings and localized glyph coverage pass.
- [x] Scoped before/after verification: 31 local labels (including the new heading), all 263 existing scene text fields and 3,161 unrelated scene objects preserved. The only existing transform changes in this pass are the shop detail body rectangle and its parent's new child reference.
- [x] `HierarchyPolish.json` records exact per-label sizes. `HierarchyPolish.png` was reviewed as an offline glyph-size comparison at reference scale; it is not a game screenshot.
- [ ] Live confirmation popup hierarchy and scrolling.
- [ ] Live six-character introduction/detail wrapping.
- [ ] Live tooltip long-title wrapping, content fit and placement.
- [ ] Live shop focus switching, price/wallet readability and purchasing controls.

The existing Editor status bridge reported an active user run in MainGameScene. It was not stopped,
reloaded or redirected for this pass. No Computer Use, extra Unity instance, balance or broad
regression suite was used. Live verification remains pending a normal refresh/reload of the saved UI.

## Tangba trial + event role repair

- [x] Tangba12 proportional Regular v1.1 from the author's official TTF release is retained with its OFL notice and a separate TMP bitmap/dynamic asset at native 12 px.
- [x] **Use Tangba For Condensed** in `Assets/Data/Presentation/Typography.asset` is enabled. Uncheck this single field to restore original Condensed assignments; no role edits or rebuilding required.
- [x] The original Galmuri11 Condensed asset/reference is retained. This substitution also covers narrow-label alternatives; the font-choice cache now notices changes to their resolved alternative.
- [x] Event modal title and choice titles retain Story; narrative and all five normal/compact choice descriptions use the body face (Tangba now, Condensed after rollback).
- [x] Root cause repaired: the offline script mistook `ExpansionChoiceButton` for the main class in `RunExpansionView.cs`, and nested choice references were not resolved as `choices.description` / `compactChoices.description`. Both offline and Unity editor-time role lookup now handle these references.
- [x] Only existing event title/narrative/choice roles were migrated. Other authored bindings and their sizes were preserved. Popup fonts are applied before measuring rows.
- [x] Runtime and Editor C# compiler checks pass. The temporary Editor compile response file was updated to include the existing `BehemothLocaleWindow.cs` dependency; no changes to localization code were made.
- [x] Saved assets verified: 282 bindings (229 DOS Story, 27 Tangba, 26 Galmuri7), all 12 event-popup field references and 754 localized codepoints with no missing characters after fallback.
- [x] Scene writes were checked to contain only font/material GUID and role changes; existing layout, strings and designer edits were retained. Current HEAD comparison records 16 pre-existing transform changes separately.
- [ ] Live event rendering/scroll/input and repeated on/off/on rollback, including cached narrow labels, still pending. The existing Editor bridge consumed `event-font-audit` without emitting the new audit, so it is not counted as a pass. The scoped Play attempt was stopped and the selection scene requested again; no additional Editor instance or Computer Use was used.

`TangbaTrial.png` compares the same authored Bell event text using saved atlas pixels, **not a game screenshot**.
`StaticVerification.json` lists the exact event label roles. `NativeTextVerification` has an opt-in
`event-font-audit` command for the existing Editor: it tests a cloned profile on/off/on with authored
event text and restores the original profile references without saving scene or data assets.

Source: [user's Tangba reference](https://noonnu.cc/font_page/1768), [author and native-size guidance](https://monadabxy.com/fonts/tangba/).

## DOS Story + DOS Gothic revision

- [x] DOS Story roles now use DOS Gothic for ASCII A–Z/a–z/0–9 inside the same TMP font asset/material.
- [x] Korean, punctuation and other symbols retain the original DOS Story outlines and metrics.
- [x] Existing asset GUID/profile assignments are retained; no scene, prefab or CSV edits were needed for this revision.
- [x] Both original sources, MIT notices and author attribution are retained. The separate derivative is named `Behemoth DOS Story Mixed`.
- [x] The real mixed TTF remains the dynamic TMP source, so newly populated glyphs follow the same policy.
- [x] Independent source/atlas validation: all 62 alphanumerics match DOS Gothic pixels and metrics at 16 px; 17,145 other supported codepoints retain original mappings/outlines/metrics; source coverage and line metrics unchanged.
- [x] Existing saved font/binding/localization verification and Editor compiler check pass.
- [ ] Live imported source, mixed heading/HUD rendering and dynamic glyph addition audited in Unity.

The current Editor consumed the `mixed-font` request without producing its new audit (the updated
verification code was not loaded). The existing capture bridge emitted a native text draw-list at
1920×1080, but its screenshot did not complete; the cause was not verified without observing the
Game view. This is not counted as a visual pass. No stop/restart of the user's active run was used
to force this verification.

`MixedFontVerification.json` records focused source/atlas checks. `DosStoryGothicLatin.png` is the
before/after offline comparison. The DOS Gothic reference is [the supplied Noonnu page](https://noonnu.cc/font_page/239),
with the exact WOFF it serves retained under `Assets/Art/Fonts/DosGothic`.

Regenerate with `build_typography_assets.py --fonts-only`, then run `verify_dos_story_mixed.py`.
This only rebuilds fonts; it does not assign or rewrite scenes/prefabs. The opt-in existing Editor file bridge
also accepts `mixed-font` during Play to record imported-source identity, active alphanumeric labels and a
temporary offscreen dynamic glyph probe. This is scoped typography verification, without Computer Use.

Attribution: 글꼴(폰트)로 도스이야기 및 도스고딕(leedheo 제작), 갈무리(Lee Minseo 제작)를 사용하였습니다.

## Implemented

- [x] DOS Story is the base face, with a single profile field for a future Galmuri11 base switch.
- [x] All five requested faces are supplied as separate TMP assets.
- [x] 282 text labels across two scenes and seven prefabs have saved semantic bindings.
- [x] Font choice considers text role and available width; numeric micro-labels and word-bearing labels have different choices.
- [x] Native sampling is 16 / 12 / 12 / 10 / 8 px. Integer output scaling is applied when the output is large enough, with a per-label override.
- [x] Font assignment preserves the working tree's authored text and transforms; Korean CSV and original font sources are not edited by this workflow.
- [x] Existing text separation from the artwork pixelation pass is preserved.
- [x] New labels can be prepared automatically in the Editor; saved current assets require no assembly menu step.
- [x] Font source files, author notices and licenses are retained.
- [ ] New assets imported and exercised in the running Unity Editor.
- [ ] Live typography/layout/input checks listed below.

## Role choices

| Text role | Default face |
| --- | --- |
| Default voice, class names, headings, short actions and menu buttons | DOS Story (Base) |
| Equipment, spell and tooltip descriptions; class/story/event narrative | Galmuri11 Condensed |
| HP/MP, gold, cost, timers, damage, contextual hints | DOS Story (Base) |
| Tiny numeric counts, page markers and symbols | Galmuri7; words use Base (DOS Story) |

Headings and action labels can choose Condensed when the actual text is wider than their rectangle. The active rules, fonts and base choice are in `Assets/Data/Presentation/Typography.asset`. Per-label roles remain editable on the authored text objects.

To reverse the base choice, set **Base Face = Galmuri11** in that profile. Base/title/heading/buttons/HUD change together without rebuilding scenes or rewriting content. Long text retains Condensed. Regular Galmuri11 and Galmuri9 have no primary role assignments in the current policy; missing source glyphs can still use the Galmuri11 fallback.

Galmuri7's MicroValue role is assigned to WeakPointView.remainingHits, RunHudIconView.stackLabel, MapRoomIcon.label/facingArrow and RunExpansionView.pageLabel (26 serialized bindings including scene instances). These are remaining-hit counts, HUD stack counts, minimap symbols and page markers, rather than body text. Actual visibility in gameplay remains pending live verification. Word-bearing map labels use DOS Story.

## Checks performed without Computer Use

| Check | Result |
| --- | --- |
| Runtime C# compile against this project's Unity references | PASS; existing unrelated `RewardPopupView.animation` hiding warning |
| Editor C# compile against the new runtime reference assembly | PASS |
| Serialized object IDs, bindings, matching font/material GUIDs and current role policy | PASS; 282 labels: 235 DOS Story, 21 Condensed, 26 Galmuri7 |
| Native bitmap shader, texture byte lengths, glyph bounds and nonempty glyph pixels | PASS; five font assets |
| Korean CSV codepoint coverage including fallbacks | PASS; 754 distinct non-whitespace characters, zero missing |
| Git HEAD comparison of RectTransforms and serialized text | 679 transforms and all 282 text fields match; nine existing MainGameScene transform edits retained and listed in the report |
| Working-tree layout/text preservation during font assignment | PASS; assignment asserts original working-tree transforms and text are unchanged before writing |
| Re-running asset generation | PASS; existing bindings/profile preserved |
| Actual saved glyph rendering | Reviewed `TypographyRoles.png` |

`StaticVerification.json` and `AssetAudit.json` record the details. The static check was run with `--allow-head-differences` after reviewing the existing HUD layout changes; this only allows recorded HEAD layout/text differences and does not relax font/binding checks. `TypographyRoles.png` is an offline rendering of the saved TMP atlas pixels, **not a game screenshot**.

During the initial font setup, the running Editor's existing file-command bridge was usable for status and stopping Play mode; its log then showed no import/compilation of the new scripts. This policy revision also used no desktop activation, mouse/keyboard injection, Computer Use tool or extra Unity Editor instance. Compilation above used Unity's C# compiler and reference response files into `Temp/TypographyCompile`; it did not replace loaded Editor assemblies or establish live import/playtest results for the revised fonts.

## What remains unverified

| Live case | Status |
| --- | --- |
| Initial import of all generated font assets, profile and binding scripts | Pending next normal Unity refresh/open |
| Title/character selection line wrapping and visual hierarchy | Pending |
| Tooltip sizing, overlapping panels and hover/focus transitions with the new metrics | Pending |
| Event/spellbook scroll masks after font changes | Pending |
| Damage-number outlines, animation and CanvasGroup fades with the new faces | Pending |
| Adding new glyphs through Unity's dynamic atlas packer | Pending |
| Base switch to Galmuri11 in live UI | Pending |
| Multiple output resolutions and real-device DPI/GPU/performance | Pending |

Computer Use was most useful for the final visual and real-input loop: observing the actual Game view, testing hover/click/scroll, and adjusting spacing based on that result. File editing, font acquisition, saved assets, glyph checks, independent compilation and an atlas-based comparison remained possible without it. No balance/physics rules or full regression suite were changed/run.

## Sources and notices

The supplied references are [DOS Story](https://noonnu.cc/font_page/1140), [Galmuri11](https://noonnu.cc/font_page/801), [Condensed](https://noonnu.cc/font_page/1609), [Galmuri9](https://noonnu.cc/font_page/800), and [Galmuri7](https://noonnu.cc/font_page/923).

DOS Story uses the exact WOFF2 served by the supplied page, decompressed to TTF; its family name is `DOSIyagiBoldface`. Source/author and MIT notice are in `Assets/Art/Fonts/DosStory`. See the [original collection](https://github.com/hurss/fonts).

New Galmuri Condensed/9/7 source files are from [the author's v2.40.4 release](https://github.com/quiple/galmuri/releases/tag/v2.40.4). The existing Galmuri11 2.40.3 source was retained, with a new atlas at its proper 12 px sampling size. The [OFL](https://github.com/quiple/galmuri/blob/main/OFL.md) is retained under `Assets/Art/Fonts/Galmuri`.

Attribution: 글꼴(폰트)로 도스이야기(leedheo 제작) 및 갈무리(Lee Minseo 제작)를 사용하였습니다.
