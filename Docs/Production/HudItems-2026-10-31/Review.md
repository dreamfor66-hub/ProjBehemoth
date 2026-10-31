# HUD items and feedback — 2026-10-31

User references: codex-clipboard-00eebbfc-97a1-4bd5-99db-52125d236b0a.png (bag, potion scale and spacing), codex-clipboard-2ba6d213-6d3c-446c-aa8d-725ef7d129d6.png (yellow line = relic start).

Requirements:
- [x] Charge gauge draws over the top HUD, keeping its cursor-relative position.
- [x] Remove hit/guard impact shapes; retain the red damage/low-health edges.
- [x] Saved, editor-visible vertical potion bag below the portrait; occupied and empty slots; existing potion actions retained.
- [x] Relics begin near x=164 in the 1536-wide authored canvas and match potion icon scale; overflow still wraps.

Scoped runtime matrix completed in the user's existing Unity Editor. All 16 checks passed; see [Live.txt](Live.txt). Captures: [bag and relics](Bag-And-Relics.png), [full charge over top bar](Charge-Over-TopBar.png), [damage edges without hit shapes](Damage-Edges-Only.png), [guard without impact](Guard-No-Impact.png), [potion actions](Potion-Actions.png), [relic wrapping](Relic-Wrap.png). The charge partial/full position and release behavior was also checked in the immediately preceding charge-offset task.

Checked 0/2/3 potions, correct capacity-derived empty slots, pointer raycasts through the decorative bag, hover text, action opening, throw targeting, discard, reacquisition/pool reuse, matching enlarged relic/potion sizes, left start and overflow. Normal and guarded hits used the production ApplyMonsterAttack path. Saved both MainGameScene and Angular01_Main. No broad balance suite; physical-device behavior remains untested.

Bag art: built-in imagegen, installed at `Assets/Art/UI/PotionBag.png`. Actual installed Warrior-Idle, HealingPotion and B1-Walls plus the user's first attachment supplied as inputs. Prompt: isolated tall brown leather carrier, empty center, bold black contours and shadow shapes, broad restrained brown planes, transparent background. Revision removes repetitive contour teeth and texture noise. Final art was compared against the supplied reference and installed character/background in Bag-And-Relics.png at HUD size: strong dark silhouette, quiet central face, broad brown planes, no repeated contour teeth. Slot backgrounds and items are separate serialized UI elements.

Final revision prompt: "Keep the vertically long narrow brown leather carrier composition, top mouth and bottom loop; no potions or slots. Match the clean broad flat forms of the installed Warrior and background. Remove repetitive sawtooth notches along the sides and bottom; use mostly smooth continuous edges with only a few large bends. Remove grain, use broad brown planes and solid black rim shadows, simplify the rope, keep the center quiet for icons, preserve transparency and the complete silhouette."
