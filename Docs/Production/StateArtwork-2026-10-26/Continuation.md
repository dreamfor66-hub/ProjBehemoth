# Active manual production checkpoint

The user has extended the task from four monster Idle mood redraws to restoring the original state-image contract for all active actors. Continue until done; do not stop at Idle wiring.

- Original spec: Docs/Production/ArcadeRevision/Progress.md. Player Idle/Attack/Guard, monster Idle/Ready/Attack/Dead, dog Idle/Attack; ONE image each, no frame playback or intermediate frames.
- Six hero classes, 41 active monster concepts; 135 missing state images. Dog remains pixel and needs two matching 2D rear poses or an explicit pending limitation.
- Current approved Idle files must remain byte-identical. MonsterMood four redraws completed/installed/verified before making pose variants.
- Built-in image_gen only. User did NOT authorize CLI fallback. No subagents authorized.
- functions exec cell 73 is RUNNING four workers over all 123 monster state jobs. Each generated PNG and exact-prompt metadata is persisted immediately to Outputs/. Do NOT launch duplicate bulk jobs. Wait on cell 73; errors also persisted per job.
- Hero generation cell 70 finished all 12 poses. Review contact sheets Review/Heroes-1.jpg and Heroes-2.jpg. First sheet inspected: Warrior and Rogue acceptable; Barbarian Attack axe seems missing one blade, inspect full image and regenerate if confirmed. Second sheet view tool is pending in cell 74.
- Cell 75 wrote StateArtworkPreparation.cs and manifest/backups; collect its completion. Atomic import validation was subsequently fixed before any actor approval.
- StateArtworkPreparation installs only actors with Approved/<actor-id>.txt. DO NOT approve before visual/anatomy check. It clears disallowed state slots and disables illustratedMotion, keeping illustratedLayout/placement and current Idle.
- Added CombatSpriteAnimation.illustratedLayout and UsesIllustratedLayout; MonsterView label rules use new property. No numeric timing/input changes.
- Manifest.json / HeroJobs.json / MonsterJobs.json / Actors.json / Idle-Hashes.json / Requirements.md describe scope.
- Need build state review gallery, approve inspected actor sets, make source-appropriate retries, live-test actual input/state transitions in one existing Unity Editor, verify no timing/stats/Idle changes. No full DPS/balance suite.
- MonsterMood/BuildReview.py and Gallery.html show style refs and previous/new four Idle art. Main MonsterComparison/BuildComparison.py already points to the new mood page and copies current four images.
- User receives concise Korean updates; do not leave >60 seconds without meaningful commentary.

FINAL: all 137 new PNGs generated, visually reviewed and installed for 48 actors. All 48 current installation hashes live-verified. See Audit.json, Verification.md, FinalPromptSet.json and Gallery.html. No generation job remains.
