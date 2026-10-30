# Warrior Attack + Act 1 growth balance: active work

User request 2026-10-28 KST: finish coherent Warrior Attack using actual pose/motion references, attach references, iterate until sound; then finish Act 1 growth/difficulty balance. Periodically check/resume if usage interrupts. Do not claim completion from code or prompts alone.

## Current stage
ART REVISED — previous completion withdrawn after user rejection. Hidden-hand Attack replacement installed; user art review pending. Growth values were not reviewed/accepted by user and are unchanged this revision.

## Required order and acceptance
- [x] Obtain and inspect actual descending one-hand sword/shield pose references (start/impact/follow-through). Record source and attach accessible images/links.
- [x] Preserve original Warrior Ready and Idle. Redraw Attack with actual torso/shoulder follow-through left, rather than a fixed proud right shoulder and moved sword. Keep head near Ready registration or left, coherent shoulder armor/arms, normal grip, original sword/shield identity and blade visible in combat framing. No actor key/tween motion. Compare successive images before installing; iterate defects.
- [x] Save selected Attack plus references/prompt/provenance and honest art review. Prior user-rejected variants do not count as complete.
- [x] Then calibrate Act 1 required growth and perk/relic acquisition budgets/values from real production content; do not assign every triggered relic 2U by fiat. Account for early, middle, pre-boss growth scenarios.
- [x] Balance the entire actual Act 1 roster/formations for a challenging health/fatigue-management game. Tight Warrior guard/weakpoint play should permit no HP damage. Preserve Warrior offensive baseline and B2/B3 tuning; defense role is more forgiving health/guard than other classes, not worse DPS.
- [x] Verify only changed/directly affected Act 1 balance in existing Unity Editor with declared deterministic progression; no extra Unity instances or broad class DPS/full-system suite. Record pending human/mobile limits honestly. No runtime/system verification for each image iteration, per user's workflow preference.
- [x] Save reviewable final artifacts and results, then disable this task's resume heartbeat and report actual completion.

## Constraints and context
Workspace E:/Github/Proj_Behemoth/Assets. Existing dirty designer/staged work preserved. Read ../AGENTS.md and Art/AGENTS.md.
Previously installed Warrior Attack was user-rejected. Final replacement is exec-04c90327 after ten iterations; see Art/Review.md. Original Ready/Idle preserved. Original Ready lives Art/World/Angular/States/Heroes/Warrior-Ready.png; original Idle Art/World/Angular/Heroes/Warrior-Rear.png.
Existing balance/growth implementation and unresolved diagnostics in Docs/Production/ActOneExperience-2026-10-28/{GrowthBudget.md,VerificationSummary.md,Encounters.txt}. Growth values/conditional relic values remain provisional. The user now authorizes continuing into balance once art is actually sound, without another OK gate.
No other character images requested in this execution; focus Warrior Attack and Act 1 growth balance.

## Resume behavior
Read this status and latest artifacts. If work is active, avoid overlapping verification. If interrupted/not running and either checklist incomplete, resume the unfinished step. Check usage with get_usage_limits but do not consume resets/credits or bypass limits. Save progress before a limit stop. Keep quiet on unchanged/unavailable checks; notify only meaningful completion, failure or a required user action. A usage delay is not task completion. Work sequentially; no extra agent/Unity instances.



## Latest continuation details (11:22 KST)
Attack final upper-chest/continuous-hilt candidate exec-04c90327 is saved in Art/Warrior-Attack.png and has now been copied to production. Initial 27-case actual-build matrix completed: FirstPass/Balance*. Required failures: Bell baseline/spell 18 HP each, Sphinx guard build 52.8 HP. Root cause found: Starter_Warrior OnAcquire RuleModifier(+2 guard/sec) became a stage buff and was cleared at BeginCombat. Calibration now separates permanent passive RuleModifier from OnAcquire BrokenShieldRegen+5, keeping numerical values. Recompile/preparation requested; then rerun scoped matrix with invariant +2 surviving repeated combat start, ThunderWire and HunterLantern actual effects, and pressure trace. No boss nerfs or Aegis buffs have been applied. New matrix is 2x simulation (elapsed telemetry game seconds) and must run alone. Asset/schema/spec files written; pending successful preparation and second matrix completion. Scope does not include full class DPS or extra Unity instances.

## Latest continuation details (11:32 KST)
Second 27-case matrix completed with passive guard fix. All middle fights and all three Required boss builds had 0 HP loss. Guard build Sphinx now 0 HP without Aegis buff or boss nerf; baseline Bell0HP. One opening Goblin REVIEW10.8HP is a capture-timing artifact: PressureTrace first hit at2.34s after screenshot at2.0, shield still35/Ready (AI missed guard while capture stalled). FirstPass at1x had Goblin0HP. Optional captureEvidence is now false in source to prevent future measurement hitches; latest change may await refresh. Only scoped failed Goblin retry needed, not another full matrix.
Negative-control mode growth-pressure was compiled and requested after matrix stopped. It runs one Opening/Middle/PreBoss actual build with guard always held and NO weak-point attacks. Separate outputs MistakeLive.txt/MistakeTelemetry.tsv/MistakeTrace.tsv. Observe completion before starting any other suite. Need verify that ignoring weakpoints actually costs HP; if Sphinx can be guard-spammed without HP due to35/(5+5+2)=2.92s broken recovery shorter than3.12s single-hit interval, the challenge requirement remains UNFINISHED despite clean tight-pilot results. Investigate actual negative results first. Possible data-only tuning is Warrior starting broken regen5→3 (natural passive2 preserved) to restore3.5s break recovery while keeping normal recovery advantage; or boss patterns, but DO NOT apply blindly. Preserve authored Warrior DPS and B2/B3. Update description+locale key+CSV if tuning values. No Aegis buff/boss nerf/core guard rule change has been made.
GrowthBalance.md has model, full53 context valuations,8 builds. Supply now6.548U after exact shop3-choice enumeration, required4.55U. Need final VerificationSummary.md, supersede old ActOneExperience docs, final art/ref presentation, and disable heartbeat only when both art and challenge/balance genuinely finished. Latest usage93% at earlier check; heartbeat must resume if allowance interrupts, without spending reset credit.


## Latest continuation (12:51 KST)
PressureV3 completed: guard-held/no-break Sphinx105.6HP, Minotaur27, Armor9; Bell/Duchess/Mantis still0. Applied starter broken bonus3 (total10/s,35/10=3.5s), elite Armor/Mantis HP420, Bell820, Duchess760. Added two-hit failed charge at.45s to Mantis/Bell/Duchess through existing data mechanics. FinalTight27-case matrix now running alone with capture disabled. All early fights including Goblin0HP so far, middle normal0HP and Minotaur0HP. Need observe remaining bosses/defense alternative. Then targeted negative control only changed Mantis/Bell/Duchess. Spell valuation corrected: remove blanket0.8U floor, exact20s MP/cooldown schedule and shared attack-cooldown cost; 53 contextual valuation rows, expected supply6.046U, required4.55 unchanged. GrowthBalance updated, final VerificationSummary pending. Automation remainsactive.


## Final acceptance (2026-10-28 KST)
FinalTight completed27/27 won with0HP. FinalPressure reran only the three altered charge patterns: Mantis32HP, Bell90HP, Duchess96HP; PressureV3 unchanged Sphinx105.6HP, Minotaur27HP, Armor9HP. All elites/bosses now punish guard-held/no-weakpoint repetition. Same middle XP115/two actual perks/shared relic and own starter, six identical19.2 hits: Warrior1HP hit/19.2, Rogue3/57.6. Role and mechanical acceptance passed. Existing Editor exitedPlay; no second instance or overlapping suite. FinalResults.json contains all27positive/12negative rows and provenance.
53 individual context valuations,8 actual builds, XP373.53/4levelups, expected supply6.046U, recommended4.55U; no automatic run bonuses. GrowthBalance.md records all assumptions, costs and limitations. Old ActOneExperience growth/verification docs are marked historical/superseded. New saved calibrated budget is protected from legacy draft regeneration even if its Docs stamp disappears.
Ready/Idle untouched; final Attack SHA256 BEB2AD8F9EC8ABB0448DFD88DEE282BFECE1FBA6153ADB6A41E817889553C3F6. Actual references attached in References/References.md, review in Art/Review.md. No other hero images changed. Scheduled resume must now be disabled; full class DPS and real-device feel were not run or claimed.

Resume heartbeat id1 was successfully updated to PAUSED after acceptance. No credit or reset was consumed. No verification remains running.


## Latest user correction — hidden sword hand
User rejected the visible sword fist and clarified body occlusion; earlier interpretation placing fist between back and camera was wrong. New selected exec-fa40d9c3 installed. See Art/HandOcclusionRevision. Original Idle/Ready unchanged, no system tests or balance edits. Prior mechanical balance logs remain historical evidence only; user explicitly has not reviewed growth numbers. Automation remains paused.


## Latest art direction — dynamic follow-through
User selected clipboard0a01 as closest base, requested forward lean instead of stiffness. New exec29bcb995 installed: curved forward-left torso/neck line, shoulder turn/foreshortening, cloth follow-through, sword-hand occlusion maintained. Art/DynamicFollowThrough contains supplied base, previous installed, new PNG and exact prompt. Original Idle/Ready unchanged. No runtime/system/balance work; growth user review still pending.


## Latest revision — swing, not bow
User rejected DynamicFollowThrough as bowing. SwingFollowThrough/Warrior-Attack.png now installed: rotational ribcage/shoulder follow-through, far sword arm hidden, blade extended left-down, shield counterbalance, cape rotation. Duplicated shield-arm pauldron corrected. Prompt and short visual review saved. Prior lean-only acceptance superseded. No growth/system work; user art review pending.


## Actual PNG canvas expansion (latest user correction)
UI-frame-only enlargement was rejected and reverted.25 current player combat PNG files now have transparent left724/right724/top218/bottom0 padding, normal1448x1086 becomes2896x1304. Original RGBA region is byte-identical. Seven player libraries include static canvasPaddingPixels compensation; rest frame640x680/combat zoom retained, no pose motion. See sibling PlayerCanvas-2026-10-28/README.md and verification records. Previous raw file hashes change due padding, while original drawing pixels are preserved exactly. Future hero art must use expanded canvas.

Canvas expansion verification complete:25/25 original RGBA pixel crops identical, Unity geometry27 pose references×2 frame sizes retains original displayed artwork size/position. No Play mode or scene positioning changes.


## 양방향 베기 프로토 후속 작업
기존 우→좌 및 사용자 편집 파일은 보존하고, 별도 좌→우 Ready/Attack을 추가했다. 화면 왼쪽 접촉 시작은 좌→우, 오른쪽은 우→좌로 고정하며 접촉별 방향을 기억한다. 확장 PNG 캔버스와 기존 씬/프리팹 배치를 유지한다. 원화/등록/실제 입력 검증 및 하단 크롭 한계는 ../DirectionalSwing-2026-10-28/README.md, Runtime.txt, Comparison.png에 기록한다. 성장 밸런스는 변경하지 않았고 기존 재개 예약은 계속 일시 정지다.
