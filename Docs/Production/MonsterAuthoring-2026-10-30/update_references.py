from pathlib import Path
import re

root = Path('Assets')
for folder in ('Scripts', 'Editor', 'Tests'):
    for path in (root / folder).rglob('*.cs'):
        if path.name in ('PrototypeTypes.cs', 'MonsterAuthoringBaseline.cs'):
            continue
        text = path.read_text(encoding='utf-8-sig')
        updated = text.replace('normalizedPosition', 'localPosition')
        updated = updated.replace('orbit.pattern=MonsterPattern.Orbit;', 'orbit.useOrbit=true;')
        updated = updated.replace('enemy.Data.bodyHitRadius', 'enemy.Data.HitBoxBounds.extents.y')
        updated = updated.replace('data.bodyHitRadius', 'data.HitBoxBounds.extents.y')
        updated = updated.replace('goblin.bodyHitRadius', 'goblin.HitBoxBounds.extents.y')
        updated = updated.replace('skeleton.bodyHitRadius', 'skeleton.HitBoxBounds.extents.y')
        updated = updated.replace('tiny.bodyHitRadius = 9;', 'tiny.hitBoxes = new() { new MonsterHitBox { radius = 9 } };')
        updated = updated.replace('Vector2.Scale(new Vector2(40, 23), data.weakPointLayoutScale)', 'new Vector2(40, 23)')
        if updated != text:
            path.write_text(updated, encoding='utf-8')

# Retire the old one-off shape/canvas installer. Saved production assets are migrated in place.
path = root / 'Editor/Presentation/ActOneFootprintPreparation.cs'
archive = Path('Docs/Production/MonsterAuthoring-2026-10-30/Before/Assets/Editor/Presentation')
archive.mkdir(parents=True, exist_ok=True)
if not (archive / path.name).exists():
    (archive / path.name).write_bytes(path.read_bytes())
path.write_text('// Superseded by MonsterAuthoringMigration. Existing saved illustrations and encounters are preserved.\n', encoding='utf-8')

path = root / 'Editor/Presentation/CombatUxPreparation.cs'
text = path.read_text(encoding='utf-8-sig')
text = re.sub(r'    static float IdleHeight\(MonsterData m\).*?    static void PreparePairs\(\)',
    '    static void PrepareSpecies()\n    {\n        // Species sizes are now carried by native Sprite geometry and authored hitboxes.\n        // Reapplying legacy body/artwork/weak-point multipliers would corrupt designer edits.\n    }\n    static void PreparePairs()', text, flags=re.S)
path.write_text(text, encoding='utf-8')

path = root / 'Scripts/Combat/Presentation/MonsterView.cs'
text = path.read_text(encoding='utf-8-sig')
replacement = '''            var library = runtime.Data.combatAnimation;
            var pose = !runtime.IsAlive ? CombatVisualState.Dead : Time.time < attackUntil ? CombatVisualState.Attack
                : runtime.State is MonsterState.Charging or MonsterState.Attacking ? CombatVisualState.Ready : CombatVisualState.Idle;
            var sprite = library != null ? library.For(pose) : runtime.Data.portrait;
            if (sprite == null && library != null) sprite = library.idle;
            if (sprite == null) sprite = runtime.Data.portrait;
            if (body != null)
            {
                body.overrideSprite = null;
                body.sprite = sprite;
                body.enabled = sprite != null;
                body.color = runtime.State == MonsterState.Stunned ? new Color(1f, .89f, .65f) : Color.white;
                body.preserveAspect = true;
                body.useSpriteMesh = false;
                body.rectTransform.anchorMin = body.rectTransform.anchorMax = new Vector2(.5f, .5f);
                body.rectTransform.pivot = new Vector2(.5f, .5f);
                body.rectTransform.localScale = Vector3.one;
                if (sprite != null)
                {
                    var pixelSize = MonsterData.ArtworkUnitsPerWorldUnit / sprite.pixelsPerUnit;
                    var size = sprite.rect.size * pixelSize;
                    root.sizeDelta = size;
                    body.rectTransform.sizeDelta = size;
                    body.rectTransform.anchoredPosition = (sprite.rect.size * .5f - sprite.pivot) * pixelSize;
                }
                Animate(runtime.Data, pose);
            }
            var cropped = library != null && library.tightPoseFrames;
            var bounds = runtime.Data.ArtworkBounds;
            if (cropped && library != null)
                foreach (var frame in new[] { library.idle, library.ready, library.attack })
                    if (frame != null)
                    {
                        var b = frame.bounds;
                        bounds.yMax = Mathf.Max(bounds.yMax, b.max.y * MonsterData.ArtworkUnitsPerWorldUnit);
                        bounds.xMin = Mathf.Min(bounds.xMin, b.min.x * MonsterData.ArtworkUnitsPerWorldUnit);
                        bounds.xMax = Mathf.Max(bounds.xMax, b.max.x * MonsterData.ArtworkUnitsPerWorldUnit);
                    }
            var top = bounds.yMax + (cropped ? 36 : 65);
            var scene = GetComponentInParent<CombatSceneView>();
            if (cropped && scene != null)
                top = Mathf.Min(top, scene.LogicalArea.rect.yMax - 130 - scene.LogicalArea.InverseTransformPoint(root.position).y);
            if (stateLabel != null)
            {
                stateLabel.rectTransform.anchoredPosition = new Vector2(0, top);
                if (cropped)
                {
                    stateLabel.rectTransform.sizeDelta = new Vector2(Mathf.Max(300, bounds.width * .4f), 40);
                    stateLabel.fontSize = 28;
                }
            }
            if (hpFill != null && hpFill.transform.parent is RectTransform bar)
                bar.anchoredPosition = new Vector2(0, top - (cropped ? 38 : 43));
            if (cropped && directionalGuardLabel != null)
            {
                directionalGuardLabel.rectTransform.anchoredPosition = new Vector2(0, top - 73);
                directionalGuardLabel.rectTransform.sizeDelta = new Vector2(310, 32);
                directionalGuardLabel.fontSize = 22;
                directionalGuardLabel.outlineColor = Color.black;
                directionalGuardLabel.outlineWidth = .18f;
            }
            if (cropped && directionalGuardFill != null && directionalGuardFill.transform.parent is RectTransform guardBar)
                guardBar.anchoredPosition = new Vector2(0, top - 98);
            if (cropped && scene != null && bounds.width > scene.LogicalArea.rect.width * .6f)
            {
                var actionPosition = new Vector2(400, -12);
                var timerPosition = new Vector2(400, -74);
                if (attackNameLabel != null) { attackNameLabel.rectTransform.anchoredPosition = actionPosition; attackNameLabel.rectTransform.sizeDelta = new Vector2(350, 50); attackNameLabel.fontSize = 30; }
                if (attackNameBackdrop != null) { attackNameBackdrop.rectTransform.anchoredPosition = actionPosition; attackNameBackdrop.rectTransform.sizeDelta = new Vector2(370, 54); }
                if (chargeTimeLabel != null) { chargeTimeLabel.rectTransform.anchoredPosition = timerPosition; chargeTimeLabel.rectTransform.sizeDelta = new Vector2(240, 56); chargeTimeLabel.fontSize = 40; }
                if (chargeTimeBackdrop != null) { chargeTimeBackdrop.rectTransform.anchoredPosition = timerPosition; chargeTimeBackdrop.rectTransform.sizeDelta = new Vector2(260, 60); }
            }
            if (stateLabel != null)
            {
                stateLabel.text'''
text, count = re.subn(r'            var artScale = .*?            if \(stateLabel != null\)\n            \{\n                stateLabel.text', replacement, text, count=1, flags=re.S)
assert count == 1, 'MonsterView layout block not found'
path.write_text(text, encoding='utf-8')
