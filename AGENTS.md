# Behemoth production conventions

- Gameplay content must be designer-editable data, not branches keyed to an item name, item ID, or a one-off Power enum. Extend reusable trigger, condition, target, effect and formula primitives when a desired effect does not fit. Compose multiple functions with EquipmentData ability lists.
- Keep PerkData focused on simple permanent stat choices. Triggered effects, conditional mechanics, spells and companions belong in inspectable equipment abilities and related data assets.
- Use TextMeshPro for all in-game text. Maintain the Korean text-key/CSV workflow when adding or changing UI and content.
- Preserve the designer-tuned Warrior and existing B1 Maze/B2/B3 monsters as the pacing baseline. Compare other classes at comparable progression, including pre-weak-point damage, break opportunities and post-break follow-up, not only theoretical burst damage. Warrior's advantage is forgiving health/guard management, not inferior DPS.
- Use the supplied Slay the Spire event references directly to preserve the choice experience. Do not substitute a universal deferred penalty purchase for distinct event outcomes. Record references in event data.
- Production folders live directly under Assets: Art, Data, Editor, Prefabs, Scenes, Scripts. Prefer saved, editor-visible assets and idempotent editor preparation. Never require a manual scene-baking menu step.
- Preserve existing designer edits and staged work. Verify interactive mechanics in the live Unity runtime and record pending real-device or balance limitations honestly.
