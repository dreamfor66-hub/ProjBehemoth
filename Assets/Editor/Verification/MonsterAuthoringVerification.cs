using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DungeonSlash;
using DungeonSlash.EditorTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
internal static class MonsterAuthoringVerification
{
    const string Folder = MonsterAuthoringBaseline.Folder;
    static int checks, failures;
    static MonsterAuthoringVerification() => EditorApplication.update += Poll;
    static void Check(bool ok, string message)
    { checks++; if (!ok) failures++; File.AppendAllText(Folder + "Verification.txt", (ok ? "PASS " : "FAIL ") + message + "\n"); }
    static void Poll()
    {
        const string request = "Temp/MonsterAuthoringVerification.request";
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(request)) return;
        string command;
        try { command = File.ReadAllText(request).Trim(); File.Delete(request); }
        catch (IOException) { return; }
        try
        {
            if (command == "compare") Compare();
            if (command == "serialization-smoke")
            {
                var records = JsonUtility.FromJson<MonsterAuthoringBaseline.Set>(File.ReadAllText(Folder + "Baseline.json"));
                foreach (var record in records.monsters)
                {
                    var value = ScriptableObject.CreateInstance<MonsterData>();
                    ReadValues(record.json, value); Object.DestroyImmediate(value);
                }
                File.WriteAllText(Folder + "VerificationHarness.txt", "PASS Value-only baseline deserialization: " + records.monsters.Count + " monsters, no referenced-asset loading.\n");
            }
            if (command == "live" && EditorApplication.isPlaying)
                (Object.FindFirstObjectByType<MonsterAuthoringLiveProbe>() ?? new GameObject("Scoped authoring verification").AddComponent<MonsterAuthoringLiveProbe>()).Run();
            if (command == "fixture") Fixture();
            if (command == "reserialize")
            {
                foreach (var guid in AssetDatabase.FindAssets("t:MonsterAttackData", new[] { "Assets/Data" }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid); var backup = Folder + "Before/" + path;
                    Directory.CreateDirectory(Path.GetDirectoryName(backup)); if (!File.Exists(backup)) File.Copy(path, backup);
                    var attack = AssetDatabase.LoadAssetAtPath<MonsterAttackData>(path); EditorUtility.SetDirty(attack); AssetDatabase.SaveAssetIfDirty(attack);
                }
                AssetDatabase.DeleteAsset("Assets/Data/Presentation/NativeMonsterFrames/OutlineBake.txt");
                File.WriteAllText(Folder + "SerializationComplete.txt", "Weak points serialized under localPosition; unused legacy fields removed.\n");
            }
            if (command == "inspect")
            {
                var data = Selection.activeObject as MonsterData;
                if (data != null) File.WriteAllText(Folder + "GestureState.json", JsonUtility.ToJson(data, true)
                    + "\nATTACK\n" + JsonUtility.ToJson(data.GetAttack(MonsterAttackType.Charge), true));
            }
            if (command == "cleanup")
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/Combat/Monsters/Data_Monster_B1_Minion_Skeleton.asset");
                AssetDatabase.DeleteAsset("Assets/Data/Combat/AuthoringGestureFixture.asset");
                AssetDatabase.DeleteAsset("Assets/Data/Combat/AuthoringGestureAttack.asset");
                var table = BehemothTextEditor.Table;
                if (table.entries.RemoveAll(e => e.source != null && e.source.StartsWith("Assets/Data/Combat/AuthoringGesture", StringComparison.Ordinal)) > 0)
                { EditorUtility.SetDirty(table); AssetDatabase.SaveAssetIfDirty(table); }
            }
        }
        catch (Exception e) { File.WriteAllText(Folder + "VerificationError.txt", e.ToString()); Debug.LogException(e); }
    }
    static void Fixture()
    {
        const string p = "Assets/Data/Combat/AuthoringGestureFixture.asset", a = "Assets/Data/Combat/AuthoringGestureAttack.asset";
        AssetDatabase.DeleteAsset(p); AssetDatabase.DeleteAsset(a);
        var source = AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/Combat/Monsters/Data_Monster_B1_Minion_Skeleton.asset");
        var copy = Object.Instantiate(source); var attack = Object.Instantiate(source.GetAttack(MonsterAttackType.Charge));
        attack.name = "AuthoringGestureAttack"; copy.name = "AuthoringGestureFixture";
        AssetDatabase.CreateAsset(attack, a); copy.attacks = new() { attack }; AssetDatabase.CreateAsset(copy, p);
        Selection.activeObject = copy;
        File.WriteAllText(Folder + "GestureInitial.json", JsonUtility.ToJson(copy, true) + "\nATTACK\n" + JsonUtility.ToJson(attack, true));
    }
    static void Compare()
    {
        checks = failures = 0; File.WriteAllText(Folder + "Verification.txt", "Frozen pre-migration geometry vs current production assets. No balance suite.\n");
        var baseline = JsonUtility.FromJson<MonsterAuthoringBaseline.Set>(File.ReadAllText(Folder + "Baseline.json"));
        var prefab = AssetDatabase.LoadAssetAtPath<MonsterView>("Assets/Prefabs/Combat/MonsterView.prefab");
        var scene = EditorSceneManager.NewPreviewScene();
        var canvas = new GameObject("Geometry comparison", typeof(RectTransform), typeof(Canvas));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvas, scene);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        try
        {
            foreach (var record in baseline.monsters)
            {
                var data = AssetDatabase.LoadAssetAtPath<MonsterData>(record.path); var name = data.name;
                if (name.Contains("MazeGuardian")) File.WriteAllText(Folder + "NativeBounds.json", JsonUtility.ToJson(data.ArtworkBounds) + "\n" + string.Join("\n", data.combatAnimation.idle.vertices.Select(v => v.ToString("F4"))));
                var expected = ScriptableObject.CreateInstance<MonsterData>();
                // Deserialize value fields only. Unity may load referenced ScriptableObjects on a
                // loading thread inside EditorJsonUtility, recursively invoking their OnEnable.
                ReadValues(record.json, expected);
                var portraitBefore = Regex.Match(record.json, @"""portrait"":(\{[^}]+\})").Groups[1].Value;
                var portraitAfter = Regex.Match(EditorJsonUtility.ToJson(data), @"""portrait"":(\{[^}]+\})").Groups[1].Value;
                expected.portrait = data.portrait;
                expected.combatAnimation = data.combatAnimation; expected.hitBoxes = data.hitBoxes;
                expected.useOrbit = record.orbit; if (record.orbit) expected.pattern = MonsterPattern.Standard;
                expected.attacks = data.attacks; expected.normalAttackSequence = data.normalAttackSequence;
                Check(portraitBefore == portraitAfter && JsonUtility.ToJson(expected) == JsonUtility.ToJson(data), name + " all non-migrated stats / identity / portrait / text unchanged");
                Object.DestroyImmediate(expected);
                var shapes = data.BodyShapes(Vector2.zero).ToArray();
                Check(shapes.All(s => record.hitBoxes.Any(c => Vector2.Distance(s.Center, c.center) < .001f && Mathf.Abs(s.Radius - c.radius) < .001f))
                    && record.hitBoxes.All(c => shapes.Any(s => Vector2.Distance(s.Center, c.center) < .001f && Mathf.Abs(s.Radius - c.radius) < .001f)), name + " exact collision union (duplicate circles ignored)");
                Check(Mathf.Abs(data.FootprintY - shapes.Min(s => s.Center.y - s.Radius)) < .001f, name + " footprint from lowest circle edge");
                Check(data.useOrbit == record.orbit, name + " orbit enabled state");
                foreach (var frame in record.frames)
                {
                    var runtime = new MonsterRuntime(data);
                    foreach (var a in data.GetAttacks(MonsterAttackType.Charge)) if (!string.IsNullOrEmpty(a.requiredSupportId)) runtime.LivingSupports.Add(a.requiredSupportId);
                    var view = Object.Instantiate(prefab, canvas.transform, false);
                    if (frame.state == "Ready") runtime.StartCharge(Vector2.zero);
                    if (frame.state == "Attack") view.PlayAttack();
                    if (frame.state == "Dead") runtime.TakeDamage(runtime.MaxHp + 1);
                    view.Bind(runtime); view.Bind(runtime); Canvas.ForceUpdateCanvases();
                    using var serialized = new SerializedObject(view);
                    var art = (Image)serialized.FindProperty("body").objectReferenceValue; art.Rebuild(CanvasUpdate.PreRender);
                    var min = Vector2.one * float.PositiveInfinity; var max = Vector2.one * float.NegativeInfinity;
                    foreach (var vertex in art.canvasRenderer.GetMesh().vertices)
                    { Vector2 p = view.Root.InverseTransformPoint(art.transform.TransformPoint(vertex)); min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
                    var actual = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                    var delta = Vector2.Distance(actual.min, frame.renderedRect.min) + Vector2.Distance(actual.max, frame.renderedRect.max);
                    Check(delta < .03f, name + " " + frame.state + " artwork quad delta=" + delta + " expected=" + frame.renderedRect + " actual=" + actual);
                    Check(AssetDatabase.GetAssetPath(art.sprite.texture) == frame.texturePath && art.sprite.rect == frame.sourceRect, name + " " + frame.state + " same pixels / crop");
                    var label = (TMPro.TextMeshProUGUI)serialized.FindProperty("stateLabel").objectReferenceValue;
                    var hp = (Image)serialized.FindProperty("hpFill").objectReferenceValue;
                    Check(Mathf.Abs(label.rectTransform.anchoredPosition.y - frame.labelY) < .03f && Vector2.Distance(((RectTransform)hp.transform.parent).anchoredPosition, frame.hpPosition) < .03f,
                        name + " " + frame.state + " header / HP unchanged " + label.rectTransform.anchoredPosition.y + " vs " + frame.labelY);
                    Check(Vector2.Distance(label.rectTransform.sizeDelta, frame.labelSize) < .03f && Mathf.Abs(label.fontSize - frame.fontSize) < .03f,
                        name + " " + frame.state + " name size / typography unchanged " + label.rectTransform.sizeDelta + " vs " + frame.labelSize);
                    Object.DestroyImmediate(view.gameObject);
                }
                for (var i = 0; i < record.attacks.Count; i++)
                {
                    var before = AssetDatabase.LoadAssetAtPath<MonsterAttackData>(record.attacks[i]); var after = data.attacks[i];
                    Check(before.chargeWeakPoints.Count == after.chargeWeakPoints.Count && before.chargeWeakPoints.Select((p, n) => Vector2.Distance(Vector2.Scale(p.localPosition, record.weakScale), after.chargeWeakPoints[n].localPosition) < .001f).All(v => v), name + " attack " + i + " exact weak-point positions");
                    var expectedAttack = Object.Instantiate(before);
                    foreach (var pointDefinition in expectedAttack.chargeWeakPoints) pointDefinition.localPosition = Vector2.Scale(pointDefinition.localPosition, record.weakScale);
                    expectedAttack.textKeys = after.textKeys; expectedAttack.textOwnerGuid = after.textOwnerGuid;
                    Check(JsonUtility.ToJson(expectedAttack) == JsonUtility.ToJson(after), name + " attack " + i + " damage / timing / mechanics / weak-point radius / hits unchanged");
                    Object.DestroyImmediate(expectedAttack);
                }
                using var preview = new MonsterWeakPointPreview(); preview.Refresh(data, data.GetAttack(MonsterAttackType.Charge));
                var area = new Rect(0, 0, 600, 400); var point = new Vector2(38, -52);
                Check(Vector2.Distance(preview.GuiToPoint(preview.PointToGui(point, area), area), point) < .001f, name + " preview coordinate roundtrip");
            }
        }
        finally { Object.DestroyImmediate(canvas); EditorSceneManager.ClosePreviewScene(scene); }
        File.AppendAllText(Folder + "Verification.txt", $"RESULT {checks} checks, {failures} failures\n");
    }
    static void ReadValues(string json, MonsterData target)
    {
        const string reference = @"\{""fileID"":-?\d+(?:,""guid"":""[^""]+"",""type"":\d+)?\}";
        EditorJsonUtility.FromJsonOverwrite(Regex.Replace(json, reference, "{\"fileID\":0}"), target);
    }
}
