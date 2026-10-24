# Visual and runtime audit — 2026-10-24

The rejected modern illustration and side-view passes are superseded.

| Requirement | Evidence |
| --- | --- |
| First-person view into a dungeon corridor | Selection.png and combat screenshots: central depth, no side-scrolling stage |
| Back-facing large player, waist cropped | Hero-*-Guard.png: all six class backs and prepared guard poses |
| Front-facing monsters with four representative states | Enemy-Ready/Attack/Dead.png; 53 pose sets, all 55 MonsterData references assigned |
| Rear dog companion at player's side, only Idle/Attack | Companion-Attack.png; real automatic damage while player guards; no dog Guard data |
| Exactly one image per state | 6 hero profiles with3, 53 enemy profiles with4, 1 dog profile with2 non-null sprite fields; no frame playback |
| Actual224p / low color / Korean pixel type | Native screenshots384x224, maximum16 colors; Galmuri11 bitmap TMP, nearest scaling |
| Decision-oriented UI | Contextual map/bag, perk/shop focus then confirm; two-choice event pagination; spellbook outside combat; result returns to selection |
| Art-only original geometry restoration | Geometry-Rollback.png; 355 bindings round trip, identical gameplay fingerprint |

The named screenshots are in Screenshots/. Logs: Arcade224-State-Wiring.txt, Arcade224-Live-Poses.txt, AshenVow-Live-Input.txt, AshenVow-Live-Events.txt, Arcade224-Rollback.txt, Arcade224-Native-Audit.txt.

Scoped live results:42 pose checks,9 input checks,11 event/book checks passed. Actual desktop clicks verified map/bag, perk selection and grant, reward confirm, shop purchase30G->10G and sold-out/exit, result return and ability detail. Input probes used two independently tracked contacts with actual frame progression. Physical mobile multitouch/readability remain device tests. No full balance/regression suite, extra Unity instance, or other-project modifications.

Official first-person reference inspected: https://www.nintendo.com/jp/topics/article/6746519a-3a1a-452f-aa71-333c6faca071 . Original generated assets were used; reference game images were not copied into the project.

Visual acceptance by the human is still their judgment. This audit confirms the requested perspective, state counts, native raster, color limit, wiring and tested behavior.
