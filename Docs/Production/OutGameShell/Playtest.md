# OutGame / hover popup scoped playtest

Unity 6000.0.59f2, existing editor instance, 2026-10-24. Physical pointer/keyboard interaction through the live Game view. No second Unity instance, balance suite or gameplay tuning changes.

| Check | Result |
| --- | --- |
| Title centered logo and three actions | PASS, Title-Final.png |
| Game start opens selection | PASS |
| All six characters stay visible on one page | PASS |
| Warrior/Rogue/Barbarian/Samurai/Monk/Wizard selection updates art/story/ability/stats/starter | PASS, physical face clicks |
| Approved art full-size import | PASS, 1254×1254 source, 418×627 full cells, no forced OutGame palette |
| Gold selection frame after art round trip | PASS after clearing obsolete card sprite bindings |
| Relic icon AND name hover | PASS, pointer moved without clicking via zero-delta scroll |
| Real relic effects in side popup | PASS, Warrior/Wizard/Monk descriptions inspected |
| Long Monk effect fits screen | PASS after adaptive width, Relic-Hover-Long.png |
| Keyboard Left while hovering | PASS, Wizard → Monk updates popup immediately |
| Pointer exit | PASS, popup closes before selecting another face |
| Details open / Escape closes | PASS, returns to same selected character |
| Options | PASS, physical volume drag, fullscreen/scale/shake clicks |
| Audio output setting | PASS, Volume 0.4590909 = AudioListener.volume |
| Option reentry | PASS, all four altered values retained |
| Escape / Back | PASS, returns to title |
| Castle button enters MainGameScene as selected Samurai | PASS |
| Existing MainGame tooltip API | PASS, starter icon hover |
| MainGame raster | PASS, 384×224, quantize=true, captured frame14 colors |
| MainGame menu → title | PASS |
| Quit | PASS, stopped editor Play mode |
| Art-only geometry/arcade round trip | PASS,355 archived references, identical gameplay fingerprint |
| End state | OutGameScene, Play off, scene clean, no saved QA object |

Options restored to their pre-test values: volume1, fullscreenfalse, window scale3, shake true. Screenshots and numeric snapshots are in this directory. No new compilation/runtime errors; historical Feedback224 test failures are retained in the existing log.

Verification limits: actual desktop display switching and executable process quit require a standalone build; Editor intentionally preserves its display. Physical mobile and broad class/long-run balance remain unrun. Current request changes OutGame layout and shared tooltip presentation; no full combat/balance suite was run.
