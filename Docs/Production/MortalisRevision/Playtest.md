# Scoped live verification — 2026-10-24

Existing Unity6000.0.59f2 instance only. Native384×224 framebuffer, integer3× Game view. No broad DPS, balance or regression suite.

30 distinct latest checks pass, listed in Final-Checks.txt. Full iterative log retained in Live-History.txt; historical errors were not erased.

- Confirmed Dungeon Mortalis product/localization title. New two-line raster wordmark on black background; rejected wordmark and Korean subtitle hidden. Start opens the existing illustrated selection page; Back returns to title. Original generated PNG retained, display palette8 colors, point sampling. Geometry mode uses a TMP title fallback.
- Quick bar hover opens an anchored side tooltip. Click opens a112×96native action menu, preserving potion count. Invalid drinks/throws are disabled. Choice/targeting pauses combat; closing releases its own pause without overriding other drawers. One-frame input suppression prevents the closing click from becoming a combat gesture.
- Healing drink: exactly35HP, one potion consumed. Full-health drink rejected without consuming. Discard removes the selected potion without effects.
- Stone potion: drink disabled; throw enabled only in live combat. Blank target and cancel consume nothing. Valid enemy click produces brief bottle flight then authored26 damage to that enemy. No more next-combat automatic stone damage. Physical mouse clicked the quick-bar stone, Throw and bat target successfully.
- Poison potion: three stacks applied to enemy by throw; later periodic HP loss checked. Drink applies a visible harmful owner buff with three stacks; later owner damage checked. Existing cleanse removes it. Authored data:8s,2s cadence,3 damage per stack. Generic owner status/buff potency handling supports the self-targeted effect; no potion-ID runtime dispatch.
- Inventory icons refresh when potions are added/consumed/discarded without needing a room transition. Inventory click opens the same action menu; Throw closes inventory and enters combat targeting. Final native Inventory-Final.png shows two remaining potions after discarding stone.
- Aiming uses the correct overlay-canvas camera conversion. Golden target brackets and bottle cursor now lie inside the native canvas, visually checked in PotionAim-Final.png. Physical Escape canceled aiming and retained all three potions; PotionCancel-Manual.png.
- Art-only geometry/arcade round trip:370 asset reference bindings, restored True, gameplay fingerprint unchanged True. Art-Rollback.txt. No Warrior/B1/B2/B3 combat tuning changes.

Iteration notes: duplicate title checks were accidentally queued while the unfocused Editor was not advancing frames, causing a false navigation failure; one subsequent serialized run passed. An inventory QA lookup initially searched only active objects; corrected to open the drawer first. A later check inspected a closed drawer's cached icons; corrected to reopen before checking the displayed result. Both QA issues are retained in history, followed by passing checks. Actual issues fixed during review: aiming cursor coordinates and inventory icon refresh after direct potion actions.

New poison potion is registered in GameContent and the saved RunController equipment pool, available for shop/drop immediately. Source/data/scene construction is idempotent editor preparation; no manual bake step. Perk offer weights, MP perk values and elemental conversion/proc relics remain unchanged; Design-Consultation.md contains proposals only.

Real-device touch/long-hold and full balance remain outside this scoped test. Poison durations advance with the existing combat buff clock. QA uses runtime clones; no QA objects or granted inventory are saved. Final handoff: OutGameScene, Play off, clean scene.

Final loaded-scene missing-script check: none. Native title logo region384×115:9 colors including black/source background. Final-Editor-State.txt records Play off and clean saved scene.
