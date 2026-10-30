# Player PNG canvas expansion

The previous UI-frame-only change was a misunderstanding and is reverted. This revision adds transparent pixels to the PNG itself. Six current player characters each have Idle/Ready/Attack/Guard expanded, plus the accepted trial Warrior reference:25 PNG files total.

Original drawing pixels are copied without resampling or repainting. Added left724/right724/top218/bottom0 pixels. Typical1448×1086 becomes2896×1304; Samurai Guard1447×1087 becomes2895×1305. Every original RGBA byte was verified identical after cropping out the new padding; all new borders have alpha0. Expansion.json records original dimensions/hashes and Before contains recoverable sources/meta/library files.

CombatSpriteAnimation.canvasPaddingPixels records left,bottom,right,top. CombatSpriteAnimator restores the original pixel-to-canvas scale after Image preserveAspect fits the expanded file, and compensates the upward canvas-center shift caused by top padding. It is the same static compensation in every state; no pose-specific or keyed transform is added. The original640×680 player UI frame and combat zoom remain. Existing positions/guard bounds are not edited.

Use the new transparent side margins for broad weapon arcs. The top margin is available but should be used conservatively. For new pose images preserve the expanded canvas and baseline region at x724/y218 (top-left PNG coordinates). Keep the padded file dimensions for the given pose; do not shrink the character to fit the old canvas. No portrait/selection art, monsters, legacy unused pixel character art or balance changed.

Verification: byte-exact pixel/transparent border proof in Expansion.json. UnityVerification.txt records actual Unity Image geometry + fixed padding compensation comparison with original backup sprites at rest640×680 and combat960×1020. No Play mode, combat suite or designer scene mutation.

## Completed checks

Unity verification passed27 pose references (24 production poses plus3 trial references) at2 frame sizes each; original source-coordinate corners and center map to the same screen points within0.002 canvas units. This uses actual Unity Image mesh generation, Sprite UVs/trim settings and production CombatSpriteAnimator. Initial verification helpers used the wrong reference Sprite mesh type/assumed full UV; those measurement assumptions were corrected, no production padding formula change was necessary. Original source RGBA hashes were rechecked after Unity import:25/25 identical.
