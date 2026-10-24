# UI readability revision — 2026-10-24

## Requirements
- [x] Content Description fields show at least five lines by default (equipment, perks, characters, buffs, spells, events).
- [x] Perk/relic tooltip descriptions and focused reward/shop descriptions use 9px logical text (previously 11px); tooltip headings use 10px.
- [x] Ability details replace the left introduction with a compact six-row HP/MP/guard/attack summary. Secondary charge/guard numbers remain in the indicated hover surface.
- [x] Detail toggle sits beside the class name and class symbol. Back occupies the lower-left navigation slot. Selection, relic hover and Start remain usable with details open.
- [x] Fatigue portrait sits against the logical canvas top-left corner; HP/gold shift right to leave space.
- [x] Layouts are saved in OutGameScene/MainGameScene and prepared idempotently without a menu step. Korean copy uses the text database and CSV.

## Scoped runtime verification
Verified in the existing Unity Editor at 384×224 (3× Game view):

| Case | Result |
| --- | --- |
| Equipment Inspector Description | PASS: Starter_Monk description displays six authored lines without the old three-line viewport; TextArea minimum is five. |
| Warrior/Rogue/Barbarian/Samurai/Monk/Wizard detail selection | PASS: all six update HP/MP/guard/attack values with details kept open; names and stat text report no overflow. |
| Detail toggle, Escape, lower-left Back, reentry | PASS: introduction restores; Back returns to title; reentry starts with introduction. |
| Additional ability stats hover | PASS: charge preparation, guard regeneration and damage reduction remain readable; no tooltip overflow. |
| Wizard/Barbarian relic descriptions | PASS: 9px body, 10px title; long Barbarian description fits (82.5px preferred height in an 85px body). |
| Inventory perk description | PASS: 9px body and no overflow; inventory remains usable. |
| Start with details open | PASS: normal MainGameScene transition without new errors. |
| Fatigue face and hover | PASS: top-left anchors/pivot (0,1), position (0,0), 34×34px; HP/gold have space; fatigue tooltip fits. |
| Map/inventory after portrait move | PASS: both open; hovering/clicking the face does not block their controls. |

Live measurements are in `Live.txt`; logical-frame screenshots are in `Screenshots/`.
The existing error log retained its pre-test size/timestamp throughout the scoped run. Reward/shop focused description fonts were serialized and inspected; separate reward/shop screen flows were not replayed.

No DPS/balance suite or unrelated encounter regression is part of this UI change. Real-device visual review remains pending.

Final 9px secondary-stat hint was replayed after saving. See Screenshots/FinalDetails.png and Screenshots/TopLeftFatigue.png. Play mode was stopped after verification.
