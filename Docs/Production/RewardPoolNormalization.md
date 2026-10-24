# Ordinary spellbook pool normalization — 2026-10-24

User direction: restore missing content only. No forced enchantment shop slot, personalized offers, first-shop guarantees, early spell locks, offer weights, prices or perk-frequency changes. Randomly offered equipment may be weak for the current build; making use of that investment should create value in later perk, shop and relic choices.

- [x] Find why existing enchantments are absent from early chest/shop offers.
- [x] Restore the six missing ordinary spellbook references in the saved RunController pool.
- [x] Verify each can occur naturally in chest/merchant offers without slot guarantees.
- [x] Verify acquiring a chest book and purchasing a merchant book teach/retain the spell.
- [x] Inspect elemental conversion relics; report only, no effects or assets added/retuned.

Cause: AshenVowGameplayPreparation.Book added new utility books to GameContent.equipment, but the saved MainGameScene RunController equipment list stayed at the earlier six offensive books. Runtime chest/shop draw from this serialized list, not directly from GameContent. No spell ownership/early-game gate prevented these six books; the source references were absent.

Migration is additive and one-time, preserving all existing designer entries. It registers catalog shared regular spellbooks (tier<2) and leaves advanced-spell event acquisition alone. The migration changes no selection algorithm. Actual missing books: EnchantFire, EnchantIce, EnchantLightning, PoisonMist, Cleanse, LightningMark.

Read-only relic audit: no general weapon-element conversion primitive or equipped relic that replaces normal/charge weapon damage with an element. StormBottle and ThunderousCharge describe additional lightning damage on a charged hit, not conversion. They are not the proposed rare conversion relics. EquipmentData currently has scope/character/price, not a rarity tier. No new conversion or rarity data added.

Additional read-only finding: the additional lightning8 effects on those two relics are EffectType.Damage with an element color. EquipmentAbilityResolver's Damage branch applies AllDamageMultiplier but not ElementMultiplier; the separate ElementDamage branch does. Thus the authored lightning bonus does not currently receive lightning-perk scaling in that path. This is outside the authorized book registration fix and is left unchanged for user review.

Verification: existing Unity Editor compiled and migrated twelve ordinary books, adding only the six absent references. The saved equipment pool grows from92 to98. Twenty-three scoped runtime checks passed:1024 natural chest/first-merchant seeds included every restored book; merchants without enchantments also remain possible. Stock remains three shared items plus three potions. Actual ShopView preview/purchase spent38 gold and learned an enchantment; actual relic acquisition retained another enchantment while preserving the prepared spell. Cached merchant stock did not refill/reroll. No selection weights, prices, perks, conversion effects or rarity data changed.

Evidence: RewardPoolNormalization-Checks.txt; MortalisRevision/Screenshots/NaturalEnchantmentShop.png (native384x224, visually inspected; full focused description and purchase button readable). This is scoped reward registration/interaction coverage, not a balance suite. Final handoff: OutGameScene, Play off; no runtime QA objects saved. Heartbeat remains paused.
