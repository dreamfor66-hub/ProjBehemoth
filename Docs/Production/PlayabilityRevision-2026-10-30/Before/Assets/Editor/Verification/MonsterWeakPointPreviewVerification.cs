using System;
using System.IO;
using System.Linq;
using DungeonSlash;
using DungeonSlash.EditorTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Explicit, scoped checks in the existing Editor; no additional Unity instance or balance suite.
[InitializeOnLoad]
internal static class MonsterWeakPointPreviewVerification
{
    private const string Folder = "Docs/Production/WeakPointInspector-2026-10-29/";
    private static string pending;
    private static double readyAt;
    static MonsterWeakPointPreviewVerification() => EditorApplication.update += Poll;
    private static void Check(bool ok, string text)
    {
        Directory.CreateDirectory(Folder);
        File.AppendAllText(Folder + "Verification.txt", (ok ? "PASS " : "FAIL ") + text + "\n");
        if (!ok) Debug.LogError("Weak-point Inspector: " + text);
    }
    private static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        const string request = "Temp/MonsterWeakPointPreview.request";
        try
        {
            if (File.Exists(request))
            {
                var command = File.ReadAllText(request).Trim(); File.Delete(request);
                if (command == "edit") EditChecks();
                else if (command.StartsWith("live:"))
                {
                    if (!EditorApplication.isPlaying) { Check(false, "Live check requires existing Play mode."); return; }
                    var path = command.EndsWith("boss") ? "Assets/Data/Combat/Monsters/Data_Monster_B1_Boss_MazeGuardian.asset"
                        : "Assets/Data/Combat/Monsters/Data_Monster_B1_Minion_Skeleton.asset";
                    var practice = Object.FindFirstObjectByType<OctoberReviewLiveProbe>()
                        ?? new GameObject("Scoped weak-point Inspector verification").AddComponent<OctoberReviewLiveProbe>();
                    practice.Setup(CharacterKind.Warrior, false, path);
                    var combat = Object.FindFirstObjectByType<CombatController>();
                    var view = Object.FindFirstObjectByType<MonsterView>();
                    var area = Object.FindFirstObjectByType<CombatSceneView>();
                    var center = area.MonsterPosition + view.Root.anchoredPosition;
                    combat.Enemies[0].StartCharge(center);
                    pending = command; readyAt = EditorApplication.timeSinceStartup + .7;
                }
                else if (command == "select")
                    Selection.activeObject = AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/Combat/Monsters/Data_Monster_B1_Minion_Skeleton.asset");
            }
            if (pending != null && EditorApplication.timeSinceStartup > readyAt)
            {
                var name = pending; pending = null; LiveCheck(name);
            }
        }
        catch (Exception exception) { pending = null; Check(false, exception.ToString()); }
    }
    private static void EditChecks()
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder + "Verification.txt", "Scoped Ready/weak-point Inspector matrix, existing Unity Editor.\n");
        var source = AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/Combat/Monsters/Data_Monster_B1_Minion_Skeleton.asset");
        var data = Object.Instantiate(source);
        var attack = Object.Instantiate(source.GetAttack(MonsterAttackType.Charge));
        var animation = Object.Instantiate(data.combatAnimation);
        data.hideFlags = attack.hideFlags = animation.hideFlags = HideFlags.HideAndDontSave;
        data.combatAnimation = animation;
        var sourceText = EditorJsonUtility.ToJson(source);
        var attackText = EditorJsonUtility.ToJson(source.GetAttack(MonsterAttackType.Charge));
        try
        {
            using var preview = new MonsterWeakPointPreview();
            Check(preview.Refresh(data, attack), "Production prefabs build in an isolated preview scene.");
            Check(preview.Artwork.sprite == animation.ready, "Ready painting selected.");
            Save(preview.Render(640, 420), "Skeleton-Ready.png");
            var old = preview.Runtime.WeakPoints[0].HitShape.Center;
            Undo.IncrementCurrentGroup();
            Undo.RecordObject(attack, "Verify weak-point Inspector coordinate");
            attack.chargeWeakPoints[0].localPosition += new Vector2(40, 23);
            Undo.FlushUndoRecordObjects();
            preview.Refresh(data, attack);
            var expected = new Vector2(40, 23) * data.combatScale;
            Check(Vector2.Distance(preview.Runtime.WeakPoints[0].HitShape.Center - old, expected) < .001f, "Coordinate edit immediately changes preview by the runtime-scaled delta.");
            Undo.PerformUndo(); preview.Refresh(data, attack);
            Check(Vector2.Distance(preview.Runtime.WeakPoints[0].HitShape.Center, old) < .001f, "Undo restores preview position.");
            Undo.PerformRedo(); preview.Refresh(data, attack);
            Check(Vector2.Distance(preview.Runtime.WeakPoints[0].HitShape.Center - old, expected) < .001f, "Redo restores edited preview position.");
            Undo.ClearUndo(attack);
            data.combatScale = 1.7f;
            attack.chargeWeakPoints[0].requiredChargeHits = 3;
            attack.chargeWeakPoints[0].hitRadius = 80;
            preview.Refresh(data, attack);
            Check(preview.Points[0].GetComponent<RectTransform>().rect.width == 272, "Non-unit monster scale and large target radius use production UI size.");
            Check(preview.Points[0].GetComponentsInChildren<Image>().Count(i => i.name == "Fill") == 3, "Multi-hit target displays the actual three-hit UI.");
            Save(preview.Render(640, 420), "Scaled-Multihit.png");
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var drawnGraphics = (System.Collections.Generic.List<Graphic>)typeof(MonsterWeakPointPreview).GetField("graphics", flags).GetValue(preview);
            var drawnMeshes = (System.Collections.Generic.List<Mesh>)typeof(MonsterWeakPointPreview).GetField("meshes", flags).GetValue(preview);
            File.WriteAllLines(Folder + "Draw-Order.txt", drawnGraphics.Select(g => $"{g.name} {g.GetType().Name} depth={g.depth}, shader={g.materialForRendering.shader.name}, tint={g.materialForRendering.color}"));
            var artIndex = drawnGraphics.IndexOf(preview.Artwork);
            var artMesh = drawnMeshes[artIndex];
            drawnGraphics.RemoveAt(artIndex); drawnMeshes.RemoveAt(artIndex);
            typeof(MonsterWeakPointPreview).GetField("renderDirty", flags).SetValue(preview, true);
            Save(preview.Render(640, 420), "Targets-only.png");
            Object.DestroyImmediate(artMesh);
            File.WriteAllLines(Folder + "Multihit-Meshes.txt", preview.Points[0].GetComponentsInChildren<Graphic>(true).Select(g =>
                $"{g.name} {g.GetType().Name}: active={g.isActiveAndEnabled}, vertices={g.canvasRenderer.GetMesh()?.vertexCount}, indices={g.canvasRenderer.GetMesh()?.triangles.Length}, world={g.transform.position}, scale={g.transform.lossyScale}, rect={g.rectTransform.rect}, color={g.color}, meshColors={string.Join(",", g.canvasRenderer.GetMesh()?.colors32.Select(c=>c.ToString()) ?? Enumerable.Empty<string>())}, material={g.materialForRendering.name}"));
            animation.ready = null; preview.Refresh(data, attack);
            Check(preview.Artwork.sprite == animation.idle, "Missing Ready uses runtime Idle fallback.");
            attack.chargeWeakPoints.Clear(); preview.Refresh(data, attack);
            Check(preview.Points.Count == 0 && preview.Artwork.sprite != null, "Empty target list still displays artwork.");
            Check(EditorJsonUtility.ToJson(source) == sourceText && EditorJsonUtility.ToJson(source.GetAttack(MonsterAttackType.Charge)) == attackText, "Preview and checks leave production monster/attack data unchanged.");
        }
        finally { Undo.ClearUndo(attack); Object.DestroyImmediate(attack); Object.DestroyImmediate(animation); Object.DestroyImmediate(data); }
        Check(!EditorSceneManagerPreviewCountLeaked(), "Disposed preview leaves no preview objects in runtime object searches.");
    }
    private static bool EditorSceneManagerPreviewCountLeaked() => Object.FindObjectsByType<MonsterView>(FindObjectsSortMode.None)
        .Any(v => UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(v.gameObject.scene));
    private static void LiveCheck(string name)
    {
        var combat = Object.FindFirstObjectByType<CombatController>();
        var runtime = combat.Enemies[0];
        var view = Object.FindFirstObjectByType<MonsterView>();
        var area = Object.FindFirstObjectByType<CombatSceneView>();
        Vector2 center = area.LogicalArea.InverseTransformPoint(view.Root.position);
        using var preview = new MonsterWeakPointPreview();
        preview.Refresh(runtime.Data, runtime.CurrentChargeAttack);
        Check(runtime.State == MonsterState.Charging, name + " actual encounter is charging.");
        using var serialized = new SerializedObject(view);
        var image = (Image)serialized.FindProperty("body").objectReferenceValue;
        Canvas.ForceUpdateCanvases(); image.Rebuild(CanvasUpdate.PreRender);
        var actual = image.canvasRenderer.GetMesh().vertices;
        var simulated = preview.Artwork.canvasRenderer.GetMesh().vertices;
        var max = actual.Length == simulated.Length ? 0f : float.PositiveInfinity;
        for (var i = 0; i < Mathf.Min(actual.Length, simulated.Length); i++)
        {
            Vector2 p = area.LogicalArea.InverseTransformPoint(image.transform.TransformPoint(actual[i]));
            Vector2 q = preview.Artwork.transform.TransformPoint(simulated[i]);
            max = Mathf.Max(max, Vector2.Distance(p - center, q));
        }
        Check(image.sprite == preview.Artwork.sprite && max < 1f, name + " Ready painting/layout match within the game's pixel snapping; max logical-unit difference=" + max);
        var canvas = image.canvas.rootCanvas;
        var pixelPerfect = canvas.pixelPerfect;
        try
        {
            canvas.pixelPerfect = false;
            image.SetVerticesDirty(); image.Rebuild(CanvasUpdate.PreRender);
            actual = image.canvasRenderer.GetMesh().vertices;
            max = 0;
            for (var i = 0; i < actual.Length; i++)
            {
                Vector2 p = area.LogicalArea.InverseTransformPoint(image.transform.TransformPoint(actual[i]));
                Vector2 q = preview.Artwork.transform.TransformPoint(simulated[i]);
                max = Mathf.Max(max, Vector2.Distance(p - center, q));
            }
            Check(max < .01f, name + " artwork geometry matches before screen pixel snapping; max difference=" + max);
        }
        finally { canvas.pixelPerfect = pixelPerfect; image.SetVerticesDirty(); image.Rebuild(CanvasUpdate.PreRender); }
        var actualPoints = Object.FindObjectsByType<WeakPointView>(FindObjectsSortMode.None);
        Check(actualPoints.Length == preview.Points.Count, name + " same number of live weak-point UI elements.");
        for (var i = 0; i < runtime.WeakPoints.Count; i++)
        {
            var p = runtime.WeakPoints[i]; var q = preview.Runtime.WeakPoints[i];
            Check(Vector2.Distance(p.HitShape.Center - center, q.HitShape.Center) < .01f && Mathf.Approximately(p.HitShape.Radius, q.HitShape.Radius), name + " target " + i + " collision center/radius match game.");
            var match = actualPoints.OrderBy(v => Vector2.Distance((Vector2)area.LogicalArea.InverseTransformPoint(v.transform.position), p.HitShape.Center)).First();
            Vector2 actualCenter = area.LogicalArea.InverseTransformPoint(match.transform.position);
            Check(Vector2.Distance(actualCenter - center, (Vector2)preview.Points[i].transform.position) < .01f
                && match.GetComponent<RectTransform>().rect.size == preview.Points[i].GetComponent<RectTransform>().rect.size, name + " target " + i + " visible UI center/size match game.");
        }
        Save(preview.Render(640, 420), name.Replace(':', '-') + "-Preview.png");
        ScreenCapture.CaptureScreenshot(Folder + name.Replace(':', '-') + "-Game.png");
        Check(true, name + " COMPLETE");
    }
    private static void Save(Texture texture, string name)
    {
        var previous = RenderTexture.active;
        RenderTexture.active = (RenderTexture)texture;
        var copy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        copy.ReadPixels(new Rect(0, 0, copy.width, copy.height), 0, 0); copy.Apply();
        File.WriteAllBytes(Folder + name, copy.EncodeToPNG());
        Object.DestroyImmediate(copy); RenderTexture.active = previous;
    }
}
