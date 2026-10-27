# Original state-image contract

Source: Docs/Production/ArcadeRevision/Progress.md and Visual-Audit.md.

- Six player classes: rear-view Idle / Attack / Guard; one representative image each.
- 41 active monster concepts: front-view Idle / Ready / Attack / Dead; one representative image each.
- Companion dog: rear Idle / Attack only; no Guard.
- No frame loops, FPS playback or generated intermediate frames.
- Preserve approved Idle artwork, camera, species, equipment, palette, hero waist crop, body scale and canvas alignment.
- Separate state poses; do not clone Idle into every slot or replace a drawn pose with rotation.
- Keep selection art, HUD, collision, all attack timing and combat data unchanged.
- Save inspector-visible animation data; auto editor preparation, no manual baking.
- Verify production input/state transitions and reject/cooldown behavior in the existing Unity Editor only; no balance suite.
