# Merchant and rear approach

## Authored behavior

- Each merchant freezes ten original slots: three different potion types, three available perks and four shared relics. All acts use the same total. Buying marks a slot sold out; reopening never replaces it.
- `Data_ShopInventory_행상재고` authors the category counts. Every real `PerkData` authors its own `shopPrice` and `soldByMerchant` eligibility. Equipment prices and eligibility continue using their existing data.
- Canceling conversation or closing the shop does not dismiss a merchant. A successful purchase remembers the transaction. Only a real move into another room changes the purchased merchant to departed; the player can reopen the shop during the current visit.
- A subject's optional `NpcApproachData` owns its rear response. After leaving an unresolved NPC and approaching its actual rear painting on a later visit, the player can ambush, speak or leave. Choosing to speak disarms the rear opportunity until another visit. Resolved subjects do not reopen.
- NPC events reference their own existing hostile terminal option by event/page/option. Shared drawings do not force shared outcomes. Animals and objects receive no invented combat or generic reaction.
- An ordinary merchant counterattacks with a concealed short blade, escapes with the baggage, and leaves thirty coins scattered. The authored action costs eight HP; the merchant is removed permanently. Only the existing concealed five-percent merchant identity in acts two and three reveals the real installed monster and enters actual combat.

## Verification handoff

The focused live probe is `MerchantApproachLiveProbe`. Root controls the existing Unity Editor and starts it serially through `Temp/MerchantApproach.request` with `run`, after compilation and automatic installation have completed. It records pointer-targeted shop purchases, unchanged stock, actual room departures/re-entry, all authored rear response menus, the normal merchant aftermath and the real concealed encounter. Output: `RuntimeReview.txt` and `Screens/` in this directory.

At implementation handoff, runtime verification remains pending. No Unity instance or broad regression suite was started by the implementation agent.
