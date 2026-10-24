# MP perk shared offer group — 2026-10-24

Requirements:
- [x] Maximum MP and MP regeneration count as one ordinary perk's combined offer weight.
- [x] At most one of these appears in each three-option reward.
- [x] Both remain available before learning a spell; no unlock or ownership gate.
- [x] Perk values and stack caps stay unchanged; respect each member's availability and cap.
- [x] Saved designer-editable PerkOfferGroup asset; no name/ID-based runtime selection.
- [x] Scoped runtime selection checks and reward UI verification.

`Assets/Data/Behemoth/PerkOfferGroups/MP.asset` has weight1, matching one ungrouped perk. Eligible buckets are sampled without replacement by weight; one eligible member is then selected uniformly per bucket. A capped member drops out without disabling the other member. A bucket with no eligible members disappears. With fewer than three eligible buckets, return fewer choices rather than repeat a group.

Elemental enchantment spells, elemental conversion relics and elemental perk rules are unchanged. The discussion recommends existing Fire/Ice/Lightning enchantment spells as temporary, MP-funded entry and rare elemental weapon relics as sustained conversion/payoff. Common permanent conversion relics are a separate possible design, not the earlier recommendation and not implemented here. Existing enchantment spells add elemental weapon damage; they do not convert all weapon damage. Avoid making recasting enchantment the mandatory best opening in every encounter: MP allocation and the timing of sustained weapon attacks must remain a decision.

Verification:11 scoped checks passed in the existing Unity Editor; no full balance or regression suite. Six classes ×1000 seed samples produced zero double-MP offers and preserved three distinct eligible options and determinism. The current fresh pool produced186 MP offers per1000 seeds, split91/95 between members. Controlled four-equal-bucket sampling over4000 offers produced3011/2998/3000/2991 appearances (the last is the combined MP group); member split1519/1472. Single-member/full-group stack-cap cases passed. Actual reward UI preview and confirmation acquired the selected perk into a runtime-only state. Full record: ManaOffer-Checks.txt. No authored combat values, perk magnitudes, unlock rules, spells or relics changed.
