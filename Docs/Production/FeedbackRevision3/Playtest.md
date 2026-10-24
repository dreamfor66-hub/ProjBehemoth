# Scoped playtest record — 2026-10-24

Existing Unity 6000.0.59f2 Editor, one running instance. Native framebuffer 384×224, displayed at integer 3× scale. No whole-system regression or DPS/balance suite run.

## Results
- Six character selections: original descriptions fit, illustrations switch, all six symbols stay visible, starting-relic side popup works. Native captures under Screenshots/Selection-{Character}.png. Final palette review found 33 colors in each illustration region (32 figure colors plus the flat original background); a left background patch contains exactly one color. Source PNGs are preserved. Total screens have 121–153 colors including relic icons/text.
- Physical mouse: selected Warrior symbol and entered MainGameScene through 성으로. Spellbook Up slot displayed Energy Bolt; clicking 정화 previewed it while Energy Bolt remained prepared; 준비하기 replaced only that slot. All sixteen spell descriptions fit in the final layout. Same-direction alternatives remain in a scroll list with a scrollbar.
- Hundred Fists: actual HP reductions 7.148 at 0.000s, 0.125s, 0.246s on a runtime clone. Three independent strikes, not one multiplied packet. OnAttack/action completion occur once, OnHit/weak-point processing once per strike.
- Critical stats: default 0%/150%; five stacks of the two shared perks produce 25%/225%, stack caps hold. Threshold checked at .2499/.25. Removing all chance stacks restores exact zero. Stunned targets crit without consuming a random roll. Existing stun-damage bonus stacks once with critical damage and survives removal of critical perks. Live weapon and spell damage match the formula. Ordinary charge alone does not crit.
- Feedback: normal white number with black outline; critical orange !!, lateral launch, two floor bounces and short low-intensity two-pulse flash. Status messages keep their separate floating-text motion.
- Weak points: native bat capture contains 148 exact #FF2828 red pixels, 44 white, 116 black; skeleton has 296/88/232 respectively. Familiar bullseye silhouette visible on both. Marker size changes do not change collision data.
- Art-only rollback: geometry references True, arcade restored True, gameplay fingerprint unchanged True, 369 bindings. New selection illustration/material refs and perk art included; prefab fonts and selection fallback labels participate.

Latest result per check is saved in Final-Checks.txt: **59 PASS, 0 unresolved FAIL**. Iteration-Checks.txt retains earlier failed checks: undersized spell-heading rect and floating-point residue after removing chance stacks. Both were fixed and rechecked. Historical Logs/AshenVow-Errors.txt remains intact and includes those earlier development errors.

## Limits
No new critical relics. No title is finalized; ABYRN, DREVYS and LABYON are proposals. The rejected existing DEEP SIX title is still present pending the naming decision.
Warrior and B1/B2/B3 authored HP/guard/attack values were not retuned. Guaranteed critical during stun intentionally increases damage; full pacing comparison remains pending explicit opt-in. Physical mobile and long-hold input regression are outside this revision; input mapping itself was preserved.

Art source paths, prompts and built-in imagegen provenance are in Art-Prompts.md.

Final physical scroll check: scrollbar thumb remains inside the track at the bottom of the learned-spell list; close returns to exploration. Native capture: Screenshots/Spellbook-Scroll-Final.png. Editor left in OutGameScene, Play off, scene clean; loaded-scene missing-script check passed.
