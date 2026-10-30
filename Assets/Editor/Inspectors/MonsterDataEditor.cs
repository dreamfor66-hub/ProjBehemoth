using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DungeonSlash.EditorTools
{
    [CustomEditor(typeof(MonsterData))]
    internal sealed class MonsterDataEditor : LocalizedDataInspector
    {
        [SerializeField] MonsterAttackData selectedAttack;
        [SerializeField] bool showWeakAreas, showHitBoxes = true;
        [SerializeField] int tool, selectedBox = -1, selectedWeakPoint = -1;
        [SerializeField] CombatVisualState previewPose = CombatVisualState.Ready;
        string feedback;
        MonsterWeakPointPreview preview;
        int dragKind, dragIndex, undoGroup, control;
        Vector2 dragStart, initialCenter;
        Object dragAsset;
        internal Rect PreviewArea { get; private set; }
        internal MonsterWeakPointPreview Preview => preview;
        protected override void OnEnable() { base.OnEnable(); Undo.undoRedoPerformed += OnUndoRedo; }
        protected override void OnDisable()
        { Undo.undoRedoPerformed -= OnUndoRedo; preview?.Dispose(); preview = null; base.OnDisable(); }
        void OnUndoRedo()
        { if (dragKind != 0) { dragKind = 0; GUIUtility.hotControl = 0; } selectedBox = selectedWeakPoint = -1; Repaint(); }
        public override bool RequiresConstantRepaint() => true;
        public override void OnInspectorGUI()
        {
            var data = (MonsterData)target;
            EditorGUILayout.LabelField("약점 / 히트박스 편집", EditorStyles.boldLabel);
            var poseIndex = GUILayout.Toolbar(previewPose == CombatVisualState.Idle ? 1 : 0, new[] { "Ready", "Idle" });
            var nextPose = poseIndex == 1 ? CombatVisualState.Idle : CombatVisualState.Ready;
            if (nextPose != previewPose) { FinishDrag(); previewPose = nextPose; }
            var attacks = data.GetAttacks(MonsterAttackType.Charge).Distinct().ToArray();
            if (!attacks.Contains(selectedAttack)) selectedAttack = attacks.FirstOrDefault();
            if (attacks.Length > 0)
            {
                var index = EditorGUILayout.Popup("미리 볼 공격", System.Array.IndexOf(attacks, selectedAttack), attacks.Select(a => a.displayName + " · " + a.name).ToArray());
                if (selectedAttack != attacks[index]) { FinishDrag(); selectedWeakPoint = -1; selectedAttack = attacks[index]; preview?.Dispose(); preview = null; }
                selectedAttack = attacks[index];
            }
            if (data.combatAnimation == null || data.combatAnimation.For(previewPose) == null)
                EditorGUILayout.HelpBox(previewPose + " 이미지가 없습니다. 게임과 같은 대체 이미지로 표시합니다.", MessageType.Warning);
            if (selectedAttack == null) EditorGUILayout.HelpBox("차징 공격이 없어 약점을 생성할 수 없습니다. 모션과 히트박스는 확인할 수 있습니다.", MessageType.Info);
            else if (selectedAttack.chargeWeakPoints.Count == 0) EditorGUILayout.HelpBox("약점 생성 탭에서 그림을 클릭하거나 드래그해 추가하세요.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                var nextTool = GUILayout.Toolbar(tool, new[] { "선택 / 이동", "히트박스 생성", "약점 생성" });
                if (nextTool != tool) { FinishDrag(); tool = nextTool; }
            }
            preview ??= new MonsterWeakPointPreview();
            var ready = preview.Refresh(data, selectedAttack, previewPose);
            var area = GUILayoutUtility.GetRect(100, Mathf.Clamp(EditorGUIUtility.currentViewWidth * .7f, 260, 420), GUILayout.ExpandWidth(true));
            PreviewArea = area;
            control = GUIUtility.GetControlID("Monster authoring".GetHashCode(), FocusType.Keyboard, area);
            if (ready)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    var pixels = EditorGUIUtility.pixelsPerPoint;
                    GUI.DrawTexture(area, preview.Render(Mathf.RoundToInt(area.width * pixels), Mathf.RoundToInt(area.height * pixels)), ScaleMode.StretchToFill, false);
                    DrawOverlay(data, area);
                }
                if (!EditorApplication.isPlaying) HandleInput(data, area);
            }
            else EditorGUI.HelpBox(area, "MonsterView / WeakPointView 프리팹이 필요합니다.", MessageType.Error);
            using (new EditorGUILayout.HorizontalScope())
            {
                showHitBoxes = GUILayout.Toggle(showHitBoxes, "히트박스 표시");
                showWeakAreas = GUILayout.Toggle(showWeakAreas, "약점 판정 표시");
                if (GUILayout.Button("화면 맞춤", GUILayout.Width(80))) preview.Refit();
            }
            EditorGUILayout.LabelField(tool == 1 ? "중심에서 바깥으로 드래그 → 원형 히트박스 생성" : tool == 2 ? "클릭 또는 드래그 → 약점 생성 · 우클릭 → 약점 패턴 복사 / 붙여넣기" : "선택 / 드래그 → 이동 · 원 오른쪽 손잡이 → 크기 · 측면 뒤로 가기 / Delete → 삭제 · 우클릭 → 패턴", EditorStyles.wordWrappedMiniLabel);
            if (!string.IsNullOrEmpty(feedback)) EditorGUILayout.HelpBox(feedback, MessageType.Info);
            if (EditorApplication.isPlaying) EditorGUILayout.HelpBox("Play 모드에서는 편집할 수 없습니다.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                serializedObject.Update();
                var boxes = serializedObject.FindProperty("hitBoxes"); boxes.isExpanded = true;
                EditorGUILayout.PropertyField(boxes, new GUIContent("Hit Boxes"), true);
                if (serializedObject.ApplyModifiedProperties()) Repaint();
                EditorGUILayout.LabelField("Footprint Y (자동 계산)", data.FootprintY.ToString("0.##"));
                if (data.hitBoxes.Count == 0) EditorGUILayout.HelpBox("히트박스가 없어 몸체 공격 판정이 없습니다. 위에서 드래그해 생성하세요.", MessageType.Warning);
                if (selectedBox >= 0 && selectedBox < data.hitBoxes.Count && GUILayout.Button($"선택한 히트박스 {selectedBox + 1} 삭제")) DeleteBox(data);
                if (selectedWeakPoint >= 0 && selectedAttack != null && selectedWeakPoint < selectedAttack.chargeWeakPoints.Count
                    && GUILayout.Button($"선택한 약점 {selectedWeakPoint + 1} 삭제")) DeleteSelected(data);
                if (selectedAttack != null)
                {
                    using var attackObject = new SerializedObject(selectedAttack); attackObject.Update();
                    var points = attackObject.FindProperty("chargeWeakPoints"); points.isExpanded = true;
                    EditorGUILayout.PropertyField(points, new GUIContent("선택한 공격의 약점"), true);
                    if (attackObject.ApplyModifiedProperties()) Repaint();
                    EditorGUILayout.LabelField("약점은 공격 에셋에 저장됩니다. 공유 중인 몬스터에도 적용됩니다.", EditorStyles.wordWrappedMiniLabel);
                }
            }
            EditorGUILayout.Space(8); EditorGUILayout.LabelField("몬스터 데이터", EditorStyles.boldLabel);
            serializedObject.Update();
            var iterator = serializedObject.GetIterator(); var enter = true;
            while (iterator.NextVisible(enter))
            {
                enter = false;
                if (iterator.name is "hitBoxes" or "textKeys" or "textOwnerGuid") continue;
                using (new EditorGUI.DisabledScope(iterator.name == "m_Script" || (iterator.name is "orbitRadius" or "orbitSpeed") && !data.useOrbit))
                    EditorGUILayout.PropertyField(iterator, true);
                if (iterator.name == "portrait") DrawPortrait((Sprite)iterator.objectReferenceValue);
            }
            if (serializedObject.ApplyModifiedProperties())
            { BehemothTextEditor.SyncAsset(data); EditorApplication.delayCall += () => { if (data != null) AssetDatabase.SaveAssetIfDirty(data); }; }
            DrawLocalizationTools(data);
        }
        static void DrawPortrait(Sprite sprite)
        {
            if (sprite == null) { EditorGUILayout.HelpBox("Portrait 이미지가 지정되지 않았습니다.", MessageType.Info); return; }
            var rect = GUILayoutUtility.GetRect(100, 150, GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint) return;
            EditorGUI.DrawRect(rect, new Color(.105f, .11f, .12f));
            var size = sprite.rect.size * Mathf.Min(rect.width / sprite.rect.width, rect.height / sprite.rect.height);
            var uv = sprite.rect; uv.x /= sprite.texture.width; uv.width /= sprite.texture.width;
            uv.y /= sprite.texture.height; uv.height /= sprite.texture.height;
            GUI.DrawTextureWithTexCoords(new Rect(rect.center - size / 2, size), sprite.texture, uv, true);
        }
        float Scale(Rect area) => Mathf.Min(area.width / preview.ViewBounds.width, area.height / preview.ViewBounds.height);
        void DrawOverlay(MonsterData data, Rect area)
        {
            Handles.BeginGUI(); var old = Handles.color;
            var scale = Scale(area); var actorScale = preview.Runtime.VisualScale;
            if (showHitBoxes)
            {
                for (var i = 0; i < data.hitBoxes.Count; i++)
                {
                    var box = data.hitBoxes[i]; if (box == null) continue;
                    var center = preview.PointToGui(box.center * actorScale, area);
                    Handles.color = i == selectedBox ? new Color(1, .8f, .2f, 1) : new Color(.3f, 1, .65f, .85f);
                    var radius = box.radius * actorScale * scale;
                    Handles.DrawWireDisc(center, Vector3.forward, radius);
                    Handles.DrawLine(center - Vector2.right * 4, center + Vector2.right * 4);
                    Handles.DrawLine(center - Vector2.up * 4, center + Vector2.up * 4);
                    GUI.Label(new Rect(center + new Vector2(5, -18), new Vector2(35, 20)), (i + 1).ToString(), EditorStyles.whiteMiniLabel);
                    if (i == selectedBox) EditorGUI.DrawRect(new Rect(center + Vector2.right * radius - Vector2.one * 4, Vector2.one * 8), Handles.color);
                }
                Handles.color = new Color(.3f, 1, .65f, .5f);
                var y = preview.PointToGui(Vector2.up * data.FootprintY * actorScale, area).y;
                Handles.DrawLine(new Vector2(area.x, y), new Vector2(area.xMax, y));
            }
            if (showWeakAreas)
            {
                Handles.color = Color.cyan;
                foreach (var point in preview.Runtime.WeakPoints)
                    Handles.DrawWireDisc(preview.PointToGui(point.HitShape.Center, area), Vector3.forward, point.HitShape.Radius * scale);
            }
            if (selectedWeakPoint >= 0 && selectedWeakPoint < preview.Runtime.WeakPoints.Count)
            {
                var point = preview.Runtime.WeakPoints[selectedWeakPoint]; Handles.color = new Color(1, .8f, .2f);
                var center = preview.PointToGui(point.HitShape.Center, area);
                Handles.DrawWireDisc(center, Vector3.forward, 24);
                GUI.Label(new Rect(center + new Vector2(27, -10), new Vector2(80, 20)), "약점 " + (selectedWeakPoint + 1), EditorStyles.whiteMiniLabel);
            }
            Handles.color = old; Handles.EndGUI();
        }
        void BeginDrag(int kind, int index, Object asset, Vector2 local)
        {
            dragKind = kind; dragIndex = index; dragAsset = asset; dragStart = local; preview.FixedBounds = preview.ViewBounds;
            Undo.IncrementCurrentGroup(); undoGroup = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Edit monster preview");
            Undo.RegisterCompleteObjectUndo(asset, "Edit monster preview"); GUI.FocusControl(null);
            GUIUtility.hotControl = control; GUIUtility.keyboardControl = control;
        }
        void HandleInput(MonsterData data, Rect area)
        {
            var e = Event.current; var actorScale = preview.Runtime.VisualScale;
            var local = preview.GuiToPoint(e.mousePosition, area) / actorScale;
            if ((e.type == EventType.MouseDown && e.button == 3 && (area.Contains(e.mousePosition) || dragKind != 0))
                || (e.type == EventType.KeyDown && GUIUtility.keyboardControl == control && e.keyCode is KeyCode.Delete or KeyCode.Backspace))
            { DeleteSelected(data); e.Use(); return; }
            if (e.type == EventType.ContextClick && area.Contains(e.mousePosition))
            { FinishDrag(); ShowPatternMenu(); e.Use(); return; }
            if (e.type == EventType.KeyDown && dragKind != 0 && e.keyCode == KeyCode.Escape)
            { Undo.RevertAllDownToGroup(undoGroup); dragKind = 0; GUIUtility.hotControl = 0; selectedBox = selectedWeakPoint = -1; e.Use(); return; }
            if (e.type == EventType.MouseDown && e.button == 0 && area.Contains(e.mousePosition))
            {
                selectedBox = Mathf.Min(selectedBox, data.hitBoxes.Count - 1);
                if (tool == 1)
                {
                    selectedWeakPoint = -1;
                    showHitBoxes = true; BeginDrag(3, data.hitBoxes.Count, data, local);
                    data.hitBoxes.Add(new MonsterHitBox { center = local, radius = 1 }); selectedBox = dragIndex;
                }
                else if (tool == 2)
                {
                    if (selectedAttack == null) return;
                    selectedBox = -1; BeginDrag(4, selectedAttack.chargeWeakPoints.Count, selectedAttack, local);
                    var suffix = 1;
                    while (selectedAttack.chargeWeakPoints.Any(p => p.id == "weak-point-" + suffix)) suffix++;
                    selectedAttack.chargeWeakPoints.Add(new WeakPointDefinition { id = "weak-point-" + suffix, localPosition = local });
                    selectedWeakPoint = dragIndex;
                }
                else
                {
                    for (var i = preview.Runtime.WeakPoints.Count - 1; i >= 0; i--)
                    {
                        var point = preview.Runtime.WeakPoints[i];
                        if (Vector2.Distance(e.mousePosition, preview.PointToGui(point.HitShape.Center, area)) > 20) continue;
                        selectedBox = -1; selectedWeakPoint = i;
                        BeginDrag(1, i, selectedAttack, local); initialCenter = selectedAttack.chargeWeakPoints[i].localPosition;
                        e.Use(); return;
                    }
                    if (showHitBoxes)
                    {
                        if (selectedBox >= 0)
                        {
                            var box = data.hitBoxes[selectedBox];
                            var handle = preview.PointToGui((box.center + Vector2.right * box.radius) * actorScale, area);
                            if (Vector2.Distance(e.mousePosition, handle) < 10) BeginDrag(3, selectedBox, data, local);
                        }
                        if (dragKind == 0)
                            for (var i = data.hitBoxes.Count - 1; i >= 0; i--)
                                if (Vector2.Distance(local, data.hitBoxes[i].center) <= data.hitBoxes[i].radius)
                                { selectedWeakPoint = -1; selectedBox = i; BeginDrag(2, i, data, local); initialCenter = data.hitBoxes[i].center; break; }
                    }
                }
                if (dragKind != 0) { EditorUtility.SetDirty(dragAsset); e.Use(); }
            }
            if (dragKind != 0 && e.type == EventType.MouseDrag)
            {
                if (dragKind == 1) selectedAttack.chargeWeakPoints[dragIndex].localPosition = initialCenter + local - dragStart;
                else if (dragKind == 2) data.hitBoxes[dragIndex].center = initialCenter + local - dragStart;
                else if (dragKind == 3) data.hitBoxes[dragIndex].radius = Mathf.Max(1, Vector2.Distance(local, data.hitBoxes[dragIndex].center));
                else selectedAttack.chargeWeakPoints[dragIndex].localPosition = local;
                EditorUtility.SetDirty(dragAsset); Repaint(); e.Use();
            }
            if (dragKind != 0 && e.type == EventType.MouseUp)
            { FinishDrag(); e.Use(); }
            EditorGUIUtility.AddCursorRect(area, tool != 0 ? MouseCursor.ArrowPlus : MouseCursor.MoveArrow);
        }
        void FinishDrag()
        {
            if (dragKind == 0) return;
            Undo.CollapseUndoOperations(undoGroup); if (dragAsset != null) AssetDatabase.SaveAssetIfDirty(dragAsset);
            dragKind = 0; GUIUtility.hotControl = 0;
        }
        void DeleteSelected(MonsterData data)
        {
            if (selectedWeakPoint >= 0 && selectedAttack != null && selectedWeakPoint < selectedAttack.chargeWeakPoints.Count)
            {
                var wasDragging = dragKind != 0;
                if (!wasDragging) Undo.RegisterCompleteObjectUndo(selectedAttack, "Delete weak point");
                selectedAttack.chargeWeakPoints.RemoveAt(selectedWeakPoint); selectedWeakPoint = -1;
                EditorUtility.SetDirty(selectedAttack); if (wasDragging) FinishDrag(); else AssetDatabase.SaveAssetIfDirty(selectedAttack);
                Repaint(); return;
            }
            DeleteBox(data);
        }
        void ShowPatternMenu()
        {
            var menu = new GenericMenu();
            var attack = selectedAttack;
            if (attack != null) menu.AddItem(new GUIContent("약점 패턴 복사"), false, () =>
            { if (attack == null) return; MonsterWeakPointPatternClipboard.Copy(attack); feedback = $"약점 {attack.chargeWeakPoints.Count}개 패턴을 복사했습니다."; Repaint(); });
            else menu.AddDisabledItem(new GUIContent("약점 패턴 복사"));
            if (attack != null && MonsterWeakPointPatternClipboard.TryRead(out _)) menu.AddItem(new GUIContent("약점 패턴 붙여넣기"), false, () =>
            {
                if (target == null || attack == null || EditorApplication.isPlaying) return;
                if (!MonsterWeakPointPatternClipboard.Paste(attack)) return;
                selectedBox = selectedWeakPoint = -1;
                feedback = $"약점 {attack.chargeWeakPoints.Count}개로 패턴을 교체했습니다. Ctrl+Z로 되돌릴 수 있습니다."; Repaint();
            });
            else menu.AddDisabledItem(new GUIContent("약점 패턴 붙여넣기"));
            menu.ShowAsContext();
        }
        void DeleteBox(MonsterData data)
        {
            if (selectedBox < 0 || selectedBox >= data.hitBoxes.Count) return;
            var wasDragging = dragKind != 0;
            if (!wasDragging) Undo.RegisterCompleteObjectUndo(data, "Delete monster hitbox");
            data.hitBoxes.RemoveAt(selectedBox); selectedBox = -1;
            EditorUtility.SetDirty(data); if (wasDragging) FinishDrag(); else AssetDatabase.SaveAssetIfDirty(data); Repaint();
        }
    }
}
