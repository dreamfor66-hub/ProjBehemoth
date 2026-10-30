# Icon production / review

144 separate built-in image_gen calls produced 41 perk, 96 relic and 7 potion paintings. No contact-sheet slicing or vector substitutes were used. Each output source is recorded in IconSources; all item-specific prompts are in IconJobs.json. PerkAbilities reuse the corresponding perk painting (183 data references total).

Primary input: Icon-Style-Inputs.jpg, composed from the actual Warrior front painting and user-supplied original fighter/brawler drawings. Secondary input for subsequent calls: the first approved Exploit icon, for stroke/palette only.

The requested native 512 size was not honored: actual source PNGs are 1254×1254 RGBA. Full source resolution is preserved. Unity imports at max512 with bilinear filtering, alpha, no mipmaps and high-quality compression. All 144 alpha ranges include 0 and255. Final complete contact sheets Icons-Perks.jpg, Icons-Relics.jpg and Icons-Potions.jpg were visually reviewed for silhouette, focal object, solid blacks, simplicity and absence of obvious structural failures before final runtime review.

Generation suffix used after the per-item prompt: Input 1 is the primary actual project style reference. Input 2 is the first icon's stroke/palette reference only; do not copy its armor or scratches into this item. Render natively at 512 x 512 pixels, square, with real transparent alpha. Keep the drawing simple and crisp rather than adding detail to fill resolution.
