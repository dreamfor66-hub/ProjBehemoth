# Revision 3 decisions

## Naming
DEEP SIX is rejected. User wants a coined single word, not two intact words joined together. Proposals: ABYRN / 아비른 (abyss + labyrinth), DREVYS / 드레비스 (dread + abyss), LABYON / 라비온 (labyrinth + beyond). No replacement title is finalized; do not generate another final logo until the name direction is accepted.

## Critical hits — latest direct clarification
- Separate CriticalChance and CriticalDamageMultiplier stats. Defaults: 0% and 150%.
- A stunned target guarantees a critical hit. Charge attacks alone do not guarantee critical.
- Preserve the old StunDamageMultiplier stat, perks and relics. Stun damage = base × existing stunned-target multiplier × critical multiplier, each applied once.
- Shared perks: 예리한 감각 +5 percentage points critical chance; 결정타 +15 percentage points critical damage. Each stacks five times, available immediately. No new critical relics in this revision.
- Direct weapon, additional/equipment and damaging spell hits use the same resolver. Periodic status damage retains its existing tick path.
- Chance rolls use a separate encounter-seeded RNG, with no roll consumed at 0%, 100%, or for guaranteed stunned criticals.
- This explicitly changes damage during stun. Broad DPS/balance comparison remains pending and is not authorized by this request.

## Presentation
384×224 native framebuffer remains. Six individually extended selection illustrations keep approved likenesses; all choices are symbols on one page. Original descriptions and starting-relic hover remain.
Spellbook uses four directional slots, a separate preview and explicit prepare button; all learned alternatives are retained.
Weak-point art is a familiar red/white bullseye with black separation. Visual diameter is 24 native pixels minimum; collision shapes remain unchanged. Signal colors must be verified in the final framebuffer, because the shared palette previously turned red into orange.
Hundred Fists uses three actual hits at a data-authored .12s interval. OnAttack and action completion run once; OnHit and weak-point hits run separately per strike.
Normal numbers are white, critical numbers orange with !! and black outline. Critical motion launches laterally, then bounces twice. Screen flash was reduced after native visual review.
