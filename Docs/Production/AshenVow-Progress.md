# ASHEN VOW / 잿빛 서약 — production checkpoint

Scope: ONLY chat `01a165fb-89c0-7853-894b-62442f48a155`, project `E:/Github/Proj_Behemoth`.

## Requirement checklist
- [x] 39 perks active without meta unlock gating: 9 shared + 5 per each of six classes; class-filtered offers, repeat scaling and inspectable EquipmentData ability sources.
- [x] MP: base30, encounter refill, natural .1/s, Wizard guard +.9/s. Cast times largely .4s, advanced .6s. Costs, cooldown, casting progress and learned spellbook UI.
- [x] 16 spells: basic attacks, poison mist, three universal enchants, cleanse, lightning mark, four advanced spells. Advanced lessons ADD spells and retain earlier spells; learned spells can be re-equipped outside combat.
- [x] 20 castle event sequences + three magic teacher encounters. Paragraph/choice/result presentation, growth requirements, refusals and missed lesson routes; meat/stray dog companion sequence.
- [x] Six original character portraits and combat sprites, relic/perk/spell icons, three castle interiors, event illustrations, enemy atlases and companion dog. Galmuri Korean pixel TMP font with OFL; saved data/scenes/prefabs and idempotent editor preparation.
- [x] Working title ASHEN VOW / 잿빛 서약 and pixel dark fantasy visual audit.
- [x] Focused15/15 EditMode tests, 16/16 live spell casts, 9/9 input checks, 11/11 event/spellbook checks. Direct selection/start, dog choices, perk reward, shop purchase/insufficient gold/sold-out/exit and final visual checks.
- [x] This-thread-only quota-aware heartbeat built and used after quota replenished. No credits, resets or bypass. Disable on completion.

## Final implementation state — 2026-10-23 13:37 KST

GameContent schema21, gameplayRevision3 / artRevision11 / narrativeRevision2. MainGameScene and OutGameScene are saved. Latest code compiled and final runtime spell names/MP/actor art inspected. No new runtime errors during final checks; Logs/AshenVow-Errors.txt retains historical QA setup errors from invoking probes in the wrong scene and the corrected editor assembly helper.

The B1 bat atlas bleed was fixed by revising original art and native Unity silhouette sprite rectangles, preserving IDs. Wizard resource values were confirmed numerically65/65 HP,20/20 guard; a scaled editor screenshot had been misread. Initial resource cap synchronization is now explicit. Story text/choices, icon tint, companion art, perk text sizing and idle spell labels were polished from live findings.

Evidence:
- `Docs/Production/AshenVow-Playtest-Matrix.md`
- `Docs/Production/AshenVow-Visual-Audit.md`
- `Docs/Production/Screenshots/{Selection,Teacher,Perks,Shop,Combat}.png`
- `Docs/Production/Art-Sources-and-Prompts.md`
- `Logs/Behemoth-TestStatus.txt`, `Behemoth-TestCases.txt`, `Behemoth-TestResults.xml`
- `Logs/AshenVow-Live-Spells.txt`, `AshenVow-Live-Input.txt`, `AshenVow-Live-Events.txt`

## Deliberate verification limits

Physical mobile multitouch and small-device readability remain device tests. Actual input controller overlap/lift/repress was checked with two contacts and real frame progression. Broad class DPS/long-run balance/full regression suites were NOT run, per user instructions. Existing designer Warrior/B1/B2/B3 pacing values and staged work were preserved; QA enemies were runtime-only clones. No extra Unity instance or background suite was launched for this task.

## Protected work / operation

Initial dirty tree: `Docs/Production/Initial-WorkingTree.txt`. Do not reset/stash or invoke legacy destructive BehemothContentBuilder.Build. Production folders live directly under Assets. Other narrative chat was read-only (`01a16614-7448-70f0-80fc-90d8f412a256`, title 움짤 형식과 연출 파악), copied to EventSequences-Castle.md. No messages were sent to other chats.

Native heartbeat ID `behemoth`,30minute schedule, target this chat only. Once completion is recorded and paused, subsequent stale queued heartbeats must perform no production work unless the human requests new changes.

Completion: scoped production and required editor verification completed; native heartbeat behemoth paused. Final editor state: OutGameScene, Play mode off, scene clean; no QA object saved.


## Superseding human art direction — 2026-10-24

The earlier completion/art acceptance statements above are historical and superseded. Active direction and checklist: `ArcadeRevision/Progress.md`. Native224p/16-color FIRST-PERSON view, rear player, front enemy; exactly one image per player3 / enemy4 / companion dog2 states. Do not resume abandoned frame generation. Keep all implemented gameplay/event data and art-only geometry rollback.


## Human feedback revision — 2026-10-24

Completed the22-page short-play PDF feedback and the subsequent explicit portrait correction. Detailed checklist and reference extract: Feedback20261024/Checklist.md. Selection now uses six distinct expressive face crops matching rear equipment, with always-visible base HP/normal damage and starter relic. Repaint used built-in image_gen with PDF page2 and existing rear silhouettes; prompt provenance in Feedback20261024/Art-Prompts.md.

Legacy room events fit224p and wait for Confirm. Monster art is enlarged via an independent child Image without changing body hit geometry; normal windups and charged patterns show names/countdowns, weakpoints have contrasting reticles. Resource layout, charge colors, monk/rogue status, inventory/tooltips/quick bar, map routes, dog density and TunnelRat death crop were revised. Rogue guard no longer counts as a concealment detection action. Barbarian starter now converts at24Fury (two normals); Rage cooldown multiplier is−45%. Warrior/B1/B2/B3 tuning was not changed.

Existing live Unity instance:20 focused checks passed (Logs/Feedback224-Live.txt); manual face selection/start, inventory hover, map routes and fountain choice/result Confirm inspected. Final native captures are in Feedback20261024/Screenshots and use384x224 at no more than16colors. Art-only geometry/arcade round trip passed355reference bindings with identical gameplay fingerprints. No whole balance suite, additional Unity instance or other-project writes. Physical mobile testing and broad long-run balance remain unrun. Earlier temporary QA lookup/compile errors are retained in the historical log, followed by successful checks.

Heartbeat remains paused; this manual feedback request does not reactivate it. No frame generation or animation playback was introduced: player3 / monster4 / dog2 representative poses remain.

Final feedback handoff: OutGameScene, Play mode off, scene clean; no QA objects saved. Final selection screenshot: Feedback20261024/Screenshots/Selection.png.


## OutGame expansion and hover correction — 2026-10-24

Latest human requests supersede the earlier selection layout. Completed `OutGameShell/Progress.md` and scoped live checks in `OutGameShell/Playtest.md`. Working title GRIMHEART · 그림하트 with a centered generated pixel logo, distinct castle exterior title, Start/Options/Quit. Character selection shows all six faces on ONE screen with a large approved portrait, brief story/ability, stats, starter relic and Castle action. Source art colors/resolution are preserved in OutGame; MainGame remains384×224/16colors.

Starter relic name/icon open an anchored reusable HoverTooltipTrigger + ItemTooltipView side description using actual equipment data. Owner-scoped dismissal, keyboard focus, refresh-on-rebind, screen clamping and long-description width expansion are documented in `OutGameShell/Hover-Popup.md`. Long Monk description, shared MainGame tooltip compatibility, options persistence/audio, full selection/start/return/quit were physically checked in the existing Unity instance. Art-only rollback again passed355 references and unchanged gameplay fingerprints. Existing designer balances and all event/perk/spell systems are preserved.

Final saved scene: OutGameScene, Play off, clean. Native heartbeat remains paused. Standalone OS display switching/process exit, physical mobile and broad balance remain outside this scoped Editor verification.

## Latest direct feedback — 2026-10-24 · supersedes GRIMHEART/previous layout

Completed the latest images1–8 and original PDF2/4/18/20p corrections. Checklist and actual native captures: `FeedbackRevision2/Checklist.md`, `FeedbackRevision2/Playtest.md`. Working title is now DEEP SIX · 딥 식스, six heroes entering the demon king's underground labyrinth. Pixel geometry wordmark,18-color native title capture,384×224 framebuffer. Approved portrait source retained with exact4:1 portrait/8:1 square face sampling. All six original PlayerCombatData.description strings displayed on one selection page, no overflow; relic side popup retained.

MainGame: exact PDF blue-gray slanted guard gauge, MP before learning spells, centered class state/monk arrows, small42px monster HP bar, rendered high-contrast weakpoints and remaining-hit segments. Ordinary/manual-charge draw uses 발도!, parry draw uses 패리 발도. Sheath parry0.3s was checked at0.24/0.36s. Guard requires downward drag and hold. Casting is independent Shift/right-button hold; Shift+left stroke or right stroke casts. Bolts follow the stroke ray; lightning strikes the endpoint. Spell D-pad lower-right, Menu at far upper-right. Aimed spells retain data-driven bonuses/echo directions. All event/perk/spell data and designer Warrior/B1/B2/B3 tuning remain.

Live verification in the existing Editor:42 distinct scoped combat/input/UI checks passed;6 selection checks passed. Input contacts and Shift state injected into production handlers over real frames; physical key/button long-hold and actual mobile remain device/manual coverage. Initial iterative Shift QA failures are preserved in FeedbackRevision2/Iteration-Checks.txt; clean compile/restart and subsequent repeats passed with0.65s traces. Historical error file retained. No full balance/DPS suite, background verification, extra Unity instance or other-chat messages.

Final geometry/arcade round trip:355 reference bindings restored and identical gameplay fingerprints, including new background material removal/restoration. Logs/Arcade224-Rollback.txt. No missing scripts in loaded scene. Final saved scene: OutGameScene, Play off, clean, no saved QA object. Native heartbeat remains paused. New background prompt/tool provenance: FeedbackRevision2/Art-Prompts.md. Production request completed; await human play feedback, no autonomous redesign.
## Latest manual revision 3 — 2026-10-24 (supersedes previous title and weak-point claims)

User rejected DEEP SIX and its wordmark. They want a coined single word; ABYRN/아비른, DREVYS/드레비스, LABYON/라비온 were proposed. No new name/logo is finalized; existing title remains pending their choice.

Implemented six expanded selection illustrations from approved likenesses with class-colored full backgrounds, six hand-drawn pixel class symbols on one page, original descriptions and relic hover. Dedicated spellbook: four directional cooldown slots, side description, retained learned alternatives and explicit Prepare. Weakpoints now conventional red/white/black bullseyes; exact signal colors verified in final native bat and skeleton captures. Hundred Fists now deals three actual timed hits (.12s data interval), rather than multiplied one-hit damage.

Latest user clarification implemented: separate CriticalChance and CriticalDamageMultiplier, defaults0%/150%; stunned enemies guarantee a critical. Existing StunDamageMultiplier/perks/relics remain and stack multiplicatively once. Charge alone does not crit. Two shared perks now available immediately: 예리한 감각 +5% chance and 결정타 +15% critical damage, each five stacks. No new critical relics. Numeric feedback: white normal, outlined orange critical with !!, lateral launch/two bounces, subtle short flash. Status messages retain their own motion.

59 distinct scoped runtime checks passed; earlier heading overflow and tiny floating-point chance residue were fixed and rechecked. Physical mouse tested selection/entry and spellbook slot, preview, prepare. Native screenshots, final checks, limitations, source paths and built-in imagegen prompts: Docs/Production/FeedbackRevision3/. Art-only rollback passes369 reference bindings and identical gameplay fingerprints; new selection art/materials and perk icons included, prefab fonts and fallback labels handled. Warrior/B1/B2/B3 authored combat values preserved. Stun critical damage is an explicit new mechanic; broad pacing/DPS and physical mobile remain pending, no full suite or duplicate Unity instance run. Native heartbeat stays paused; no other-chat messages or project edits.

Final selection review: original PNGs preserved; each displayed illustration region has33 colors (32 figure colors plus original flat class background), with one-color background patches. Final scrollbar correction was physically checked at the bottom of the spell list; thumb stays within its track and closing restores exploration. Capture: FeedbackRevision3/Screenshots/Spellbook-Scroll-Final.png. Final handoff: OutGameScene, Play off, scene clean. No missing scripts in loaded scenes. Name/logo decision remains pending.

## Dungeon Mortalis confirmation and potion revision — 2026-10-24

User confirmed **Dungeon Mortalis**, superseding every pending title proposal. Applied a generated two-line pixel logo on a black title page, preserved source PNG and added an8-color display material. Product name/Korean CSV updated. Selection, Start/Options/Quit retained; geometry mode has a TMP title fallback. Asset and exact built-in imagegen prompt: MortalisRevision/Art-Prompts.md.

Potions now show an anchored hover description and click into Drink/Throw/Discard actions. Shared quick-bar/inventory menu, invalid actions disabled, cancel/Escape/outside close, input pause scoped to the interaction. Stone is now a direct26-damage combat throw to the clicked enemy, preserving magnitude and removing pre-consumption. New saved PoisonPotion uses OnConsume/OnThrow ability data: self/enemy poison respectively, real periodic damage, visible harmful owner status and existing cleanse. Immediate shop/drop registration; no item-ID gameplay branch. Inventory follows consumption/discard immediately. Pointer conversion and stale inventory issues found in live review were fixed.

30 distinct scoped latest checks passed; physical stone throw and Escape cancel, native screenshots and iteration history recorded under Docs/Production/MortalisRevision/. Art-only rollback370 bindings and identical gameplay fingerprints. Final editor: OutGameScene, Play off, clean; no saved QA inventory/objects. No extra Unity instance or full balance suite. Native heartbeat stays paused.

MP perk frequency and rare elemental conversion/proc relics were consultation only, deliberately not implemented. Keep pre-spell availability; proposed reducing the combined MP offer weight. Common enchantments can provide entry while rare conversion/proc relics reward elemental investment. Broad damage pacing and physical device tests remain pending separate request.

## Approved MP offer grouping — 2026-10-24

Latest user accepted grouping the two MP perks as one offer weight and prohibiting their co-occurrence. Implemented reusable PerkOfferGroup metadata and saved MP.asset (weight1). ManaVessel/ManaFlow reference it; sample buckets without replacement, then one eligible member per bucket. Both remain available before spell acquisition; values/caps unchanged. Single-member caps leave the other available. No IDs/names in selection dispatch.

Existing Editor live verification:11 scoped checks passed, six class pools ×1000 seeds, controlled equal-bucket distribution, determinism, caps, actual reward UI preview/confirm. Record: ManaOfferRevision.md and ManaOffer-Checks.txt. No full balance suite or extra instance. Enchantment spells versus common conversion relics remain discussion only; clarified recommendation meant the existing elemental enchantment spells. Native heartbeat remains paused. Final handoff: OutGameScene, Play off, no saved QA objects.

## Ordinary spellbook omission restored — 2026-10-24

User approved only restoring omissions, explicitly rejecting a forced enchantment shop slot. Random opportunities should create pressure to use acquired equipment and make later elemental investments valuable; do not personalize or guarantee relevant drops. Restored six ordinary book references absent from the saved RunController pool: EnchantFire/Ice/Lightning, PoisonMist, Cleanse, LightningMark. They already existed in GameContent but chest/shop drew from a stale serialized list. One-time additive editor migration preserves existing entries, costs, weights, eligibility and advanced event spells. Saved pool92→98; all12 ordinary books now registered.

Twenty-three focused checks passed in the existing live Unity instance, including1024 natural chest/first-merchant seeds, six restored books appearing in both sources, unchanged3+3 shop composition, actual preview/purchase/learning, unchanged cached stock and chest acquisition with occupied-direction storage. Native384x224 shop capture visually inspected. Evidence and checklist: RewardPoolNormalization.md, RewardPoolNormalization-Checks.txt. Final editor OutGameScene, Play off, clean; no saved QA objects, no full balance suite or extra instance. Heartbeat remains paused.

Elemental conversion rare relics were READ ONLY. None currently convert normal/charge weapon damage; StormBottle/ThunderousCharge add lightning8 instead, and EquipmentData has no rarity tier. Additional audit finding: those lightning effects use Damage rather than ElementDamage, so their authored lightning bonus does not receive ElementMultiplier in that resolver path. Left unchanged; report to user before any implementation of conversion/rarity or correction of those effects.

## Requested lightning bug fix, exchange-event exclusion and catalog — 2026-10-24

User approved fixing the identified lightning multiplier bug, temporarily removing perk/relic exchange events, and listing all current perks/relics/spells with acquisition routes/timing before further design changes. StormBottle and ThunderousCharge now use ElementDamage; magnitude8, charge-only trigger, prices and other effects preserved. Legacy construction recipe also corrected. No new conversion relics or rarity system added.

New generic RandomEventData.excludedFromRandomPool flag filters the random draw. Five authored events excluded: Symbiosis, Hourglass, Bell, Forge, MasterTrial. Full pages/options retained for later redesign. This also pauses Parasite/Hourglass/GuardianBell acquisition; listed explicitly. Delivery's perk→gold choice and Training's existing-perk upgrades retained because they do not replace a perk/relic with a new perk/relic.

Existing Unity instance:20 focused runtime checks passed, both actual relics8→8.8→9.6 with lightning perks, fire unaffected, physical charge base unchanged, all-damage25%×lightning20% gives12,384 B1/B2/B3 random event draws never choose excluded events. Event history restored after sampling. No whole DPS/balance suite or extra instance.

Delivered Docs/Production/ContentReview/Index.md, Perks.md41, Relics.md84 plus12 ordinary spellbooks, Spells.md16, Findings.md and Runtime-Checks.txt. Read-only exporter compares GameContent and saved MainGameScene pools; source snapshot and reproducible Markdown renderer retained. Catalog shows current78 obtainable relics,3 paused event relics,3 existing common relic omissions. Notes distinguish static description/effect mismatches (HunterLantern,ThunderWire,Starter_Warrior) and stale spell names; these newly found issues were documented, not silently retuned. Warrior/B1/B2/B3 designer values preserved. Final editor OutGameScene, Play off, clean; no saved QA objects. Native heartbeat remains paused.

## Fatigue and fixed camp — 2026-10-24

Implemented user's latest 피로도 specification:100→0, death at0, actual successful cell movement/backtracking consumes hidden walking grace then fatigue; waiting/wall input do not. Attack speed is the first/primary penalty, final damage starts later and applies to direct/aimed spell/poison/burn damage. Curves, grace formula and all recovery values are saved in Assets/Data/Behemoth/Settings/Fatigue.asset; authored Warrior/B1/B2/B3 combat values preserved.

One fixed reachable camp per floor, may leave/revisit. Camp once restores100+HP10+walking grace; repeatable brief rest restores only up to60, no HP or grace. Full camp shifts living ordinary encounters into adjacent empty paths (including cleared routes), preserving encounter origin identity/count; no boss/elite movement or resurrection. No ambush. Generic RecoverFatigue equipment effect with separate travelGraceSteps; new common fatigue potion30+10grace, natural pool/shop, no forced slot.

Upper-left32px portrait has6classes×4expression sprites; face pose, border/color fixed, eyes/brows/mouth change. No permanent fatigue number/gauge; shared hover popup gives value/stage/penalties and exact <=10 warning. New camp art, three choices together on one page, concise summaries plus shared hover detail. Fixed interrupted PixelDitherTransition leaving text obscured; modal backdrop covers viewport so underlying HUD fragments do not peek through.

27 focused live checks PASS in existing Unity6000.0.59f2;96 B1/B2/B3 generated maps, actual moves/cooldown/damage/recovery/death/nextfloor reset. Physical mouse tested full camp, brief rest and continue-to-navigation.24native expression captures plus camp/danger/death screenshots. Docs/Production/Fatigue/Progress.md contains checklist, tuning values, validation and limitations; Runtime-Checks.txt and Art-Prompts.md contain evidence/source prompts/asset paths. Built-in image_gen used, originals preserved. New assets included in existing art-reference mechanism; geometry camp art suppressed. Full rollback/balance/DPS suites and physical-mobile tests not run. Long-run camp discovery/pacing and class completion balance remain pending. No additional Unity instance, other-chat messages or project changes; native heartbeat remains paused.

Final focused rerun completed27PASS/0FAIL/COMPLETE with deterministic QA Fresh seed0, restoring prior ForcedSeed in finally. An earlier random QA cleanup run hit the pre-existing dungeon distance-constraint retry limit; logged separately in Fatigue/Progress.md, generator retry/distance design unchanged. Final handoff OutGameScene, Play off, scene clean; no saved QA objects.

