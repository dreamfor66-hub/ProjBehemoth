# HUD structure and pointer/guard trial

Requirements
- [x] Remove the countdown duration gauge, preserve its ink shadow and typography.
- [x] Replace 18 relic + 4 potion baked quick slots with GridLayoutGroup/VerticalLayoutGroup and a shared prefab pool. Active layouts contain only rented items; returned icons live in inactive Pool storage. No production quick-slot limit of 18.
- [x] Reuse the same pool component for inventory perk/relic/potion grids.
- [x] Put combat HP and mana under HUD/PlayerStatusGauges.
- [x] Group canvas roots into WorldPresentation, CombatRoot, Drawers, GameplayMessages, DeveloperTools, CenterModalLayer, ScreenTransitions, HUD and ItemOverlays. CombatRoot has Exploration, Actors, CombatEffects, LegacyHud, PlayerGuard, PointerFeedback and ScreenEffects.
- [x] Move cooldown sword to PointerFeedback with a small pointer-relative offset and edge clamping. AttackCooldownAnchor.placement can switch Pointer / Monster / Authored without another hierarchy migration.
- [x] Move guard to bottom-center below the foreground player. Show the current/max resource, active guard edge, recent loss trail and broken-state color. Keep combat rules and recovery values intact.
- [x] Preserve existing designer assets and staged work; save prepared scene and prefabs automatically. Migration is versioned on HudSceneStructure and skips dirty loaded scenes.
- [x] Focused runtime acceptance: 28 PASS, 0 FAIL, COMPLETE; plus observed native mouse movement in the same Editor.

Migration.txt records zero world-corner drift for every existing reparented node before the explicitly requested layout changes. ReferenceAudit.json found no changed owners/scripts among surviving original components. Before/MainGameScene.unity preserves the pre-migration scene.

Production files: Assets/Scenes/MainGameScene.unity; Assets/Prefabs/UI/QuickInventoryIcon.prefab; shared RunHudIconPool; HudSceneStructure; AttackCooldownAnchor.

The non-build Angular01 trial keeps the legacy quick-slot compatibility path. MainGameScene serializes the new pools and empty legacy arrays. Gameplay data, monster tuning, actor art and attack/guard timings were not changed.


Verification
- Live.txt: 28/28 checks passed. Covers >18 relics, wrapping, return/re-rent instance identity, tooltip rebinding, potion click/discard/reacquisition, inventory bindings and pause restoration, pointer translation/clamping, cooldown progress/ready, sustained two-hand overlap/lift/repress (three cycles without clearing both contacts), real guard cost, loss trail lifetime, broken guard, visible Korean values and unchanged HP/MP/attack cooldown stats.
- The final countdown mesh is black ink only (28 vertices, RGBA 0/0/0/204); the vermilion duration line is gone. Final-HUD.png inspected at 1920x1080.
- Guard-Impact.png and Guard-Broken.png inspected: guard stays at the foreground player's lower center, damage loss is red, current/max values remain readable, and broken regeneration uses the existing gameplay rules.
- Native desktop observation: the sword followed the actual mouse from the right side (~1595/500) to the left (~876/424), and back to the right/lower field (~1555/620). The left click was driven through the Unity Game view; user cursor motion was also observed. Desktop input ownership was respected by refreshing state after intervening user input. Pointer-Left.png / Pointer-Right.png preserve the game captures.
- Unity Play mode stopped after these scoped checks. No additional Unity instance or balance/DPS suite was run.
- Actual mobile hardware ergonomics remain unverified. Contact ownership and overlap behavior were exercised through production input handlers.
