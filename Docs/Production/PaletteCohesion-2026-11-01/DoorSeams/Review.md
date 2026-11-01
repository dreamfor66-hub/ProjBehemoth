# B1 side-opening extraction — 2026-11-01

Scope: B1-Left.png and B1-Right.png only. No Unity, importer, source-code, tint, camera, or production PNG edits have been made by this cleanup review.

The current base B1-Walls and common oath room, approved original B1, actual DD 01/03, and both original transparent overlays were directly viewed. Prompts.json and Prompts2.json record the actual inputs and built-in imagegen prompts. Before/ preserves both original production PNGs and their untouched meta files.

## Candidate decisions

- Attempt 1 rejected: Left changed canvas to1602×981 and enlarged/moved its opening; Right retained1672×941 but moved its opening by roughly80px. Neither is installed.
- Attempt 2: both canvases1672×941, no scaling/translation/repainting in post-processing. The old foreground-column rectangle and unrelated floor/rubble pieces have been removed by imagegen. ReadyCandidates contains the unchanged tool-output PNGs for root review; production remains original.
- Left candidate 2 retains the broad old stone wedge above the inner arch at y90–190. It is no longer a rectangular patch, and its basic-room join is good; in the oath room its broad surface still covers the upper decorative frieze. Therefore a strict reading of 'thin arch/jamb only' remains conditional and root must decide whether contour clipping is required. Do not label this resolved by numeric matching alone.
- Right candidate 2 removes the offending old near-column flat grey rectangle. Its arch/jamb is thin and the two full-room composites show the underlying new column unobstructed. The top triangular infill is slightly re-authored; the unoccluded actual black opening edges remain within1px at y340–430. Lower old black bounds were occluded by the old column, so their newly exposed extent is not a meaningful direct width comparison.

## Evidence

Base-Both-Full2.jpg and Oath-Both-Full2.jpg are actual alpha composites at native1672×941. The four *-Compare2.png crops show old and candidate side by side without rescaling. The *-Candidate2-Checker.png crops show transparency explicitly. These are offline source composites, NOT Unity runtime captures.

ReviewGeometry.json and *-EdgeProfile.json compare opaque interior dark edges in the unoccluded vertical opening: Left mean absolute edge delta0.243px/max3px; Right0.593px/max1px. No fitted transform was applied. This does not establish equality of upper masonry or resolve the Left wedge concern.

MaterialSamples.json records small same-coordinate jamb samples in before/candidate PNGs with median RGB and linear-sRGB Y. They are shading diagnostics, not pass thresholds. No global colour filter was applied. Full composites were also inspected for join temperature, solid-black interior, local line density and threshold grounding.

Remaining: root acceptance and actual Unity check using selected source or native contour clipping. Preserve both PNG metas/GUIDs and the full-canvas geometry when installing.

## Targeted Left trim, attempt 3

The excessive stone wedge was removed by imagegen: solid-alpha bounds now begin at y179, leaving a thin pointed arch. Black interior edge comparison against original at y320–430: mean0.333px/max3px, no transform. See Left3Geometry.json, Left-{Base,Oath}-Compare3.png, and {Base,Oath}-Both-Full3.jpg. These source composites were directly viewed at native resolution. The new narrow stone arch is warmer/brighter than candidate2; this is explicitly recorded in Left3Color.json and root has been notified for scene-cohesion acceptance. Right2 has been installed by atomic byte-for-byte replacement with meta unchanged; Left remains original until root concludes the candidate3 scene review.

## Final selected installation

Right2 and Left4 are installed for actual Unity QA. Left4 strict lighting edit lowers the sampled jamb from rejected Left3 Y0.256 toY0.0807; the narrow grey stone frame no longer presents a broad yellow highlight, and was directly inspected on both base/oath full composites. Left4 tool-output canvas was1671×941; exactly one fully transparent rightmost column was added to restore1672×941. A pixel-array equality assertion verifies that all original1671 columns, including RGBA, remain unchanged at(0,0). No resampling, paint, colour filtering or alpha erasure was performed. Both original production meta files are byte-identical. Installed.json records hashes and alpha extents. Final actual Unity review remains root-owned and pending.
