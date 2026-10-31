using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Captures the pre-migration working tree, including unsaved designer asset values.
[InitializeOnLoad]
internal static class MonsterAuthoringBaseline
{
    internal const string Folder = "Docs/Production/MonsterAuthoring-2026-10-30/";
    [Serializable] internal sealed class Set { public List<Entry> monsters = new(); }
    [Serializable] internal sealed class Entry
    {
        public string path, json;
        public Vector2 oldBodySize, oldArtSize, weakScale;
        public bool authored, orbit, tight;
        public float oldHalfHeight;
        public List<Circle> hitBoxes = new();
        public List<Frame> frames = new();
        public List<string> attacks = new();
    }
    [Serializable] internal sealed class Circle { public Vector2 center; public float radius; }
    [Serializable] internal sealed class Frame
    {
        public string state, spritePath, texturePath;
        public long spriteId;
        public Rect sourceRect, renderedRect;
        public float labelY, fontSize;
        public Vector2 labelSize, hpPosition;
    }
    static MonsterAuthoringBaseline() => EditorApplication.update += Poll;
    static void Poll()
    {
        const string request = "Temp/MonsterAuthoringBaseline.request";
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(request)) return;
        File.Delete(request);
        try { Capture(); }
        catch (Exception e) { Directory.CreateDirectory(Folder); File.WriteAllText(Folder + "BaselineError.txt", e.ToString()); Debug.LogException(e); }
    }
    internal static void Capture()
    {
        if (File.Exists(Folder + "Baseline.json")) return; // Frozen evidence must never be replaced after conversion.
        Directory.CreateDirectory(Folder);
        var output = new Set();
        var prefab = AssetDatabase.LoadAssetAtPath<MonsterView>("Assets/Prefabs/Combat/MonsterView.prefab");
        var scene = EditorSceneManager.NewPreviewScene();
        var canvasObject = new GameObject("Monster structure baseline", typeof(RectTransform), typeof(Canvas));
        canvasObject.hideFlags = HideFlags.HideAndDontSave;
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasObject, scene);
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        try
        {
            foreach (var guid in AssetDatabase.FindAssets("t:MonsterData", new[] { "Assets/Data/Combat/Monsters" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var data = DungeonSlash.DesignerAssetPaths.Load<MonsterData>(path);
                using var serialized = new SerializedObject(data);
                var record = new Entry { path = path, json = EditorJsonUtility.ToJson(data),
                    oldBodySize = serialized.FindProperty("bodySize").vector2Value,
                    oldArtSize = serialized.FindProperty("artworkSize").vector2Value,
                    weakScale = serialized.FindProperty("weakPointLayoutScale").vector2Value,
                    authored = serialized.FindProperty("useAuthoredFootprint").boolValue,
                    orbit = (int)data.pattern == 3, tight = data.combatAnimation != null && data.combatAnimation.tightPoseFrames };
                var radius = serialized.FindProperty("bodyHitRadius").floatValue;
                record.oldHalfHeight = record.authored ? Mathf.Max(1, record.oldBodySize.y) * .5f : radius;
                foreach (var shape in data.BodyShapes(Vector2.zero)) record.hitBoxes.Add(new Circle { center = shape.Center, radius = shape.Radius });
                record.attacks = data.attacks.Where(a => a != null).Select(AssetDatabase.GetAssetPath).ToList();
                foreach (var state in new[] { "Idle", "Ready", "Attack", "Dead" })
                {
                    var runtime = new MonsterRuntime(data);
                    foreach (var attack in data.GetAttacks(MonsterAttackType.Charge))
                        if (!string.IsNullOrEmpty(attack.requiredSupportId)) runtime.LivingSupports.Add(attack.requiredSupportId);
                    var view = Object.Instantiate(prefab, canvasObject.transform, false);
                    if (state == "Ready") runtime.StartCharge(Vector2.zero);
                    if (state == "Attack") view.PlayAttack();
                    if (state == "Dead") runtime.TakeDamage(runtime.MaxHp + 1);
                    view.Bind(runtime); view.Bind(runtime);
                    Canvas.ForceUpdateCanvases();
                    using var viewData = new SerializedObject(view);
                    var art = (Image)viewData.FindProperty("body").objectReferenceValue;
                    art.Rebuild(CanvasUpdate.PreRender);
                    var vertices = art.canvasRenderer.GetMesh()?.vertices;
                    if (art.sprite != null && vertices != null && vertices.Length > 0)
                    {
                        var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                        var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                        foreach (var vertex in vertices)
                        {
                            Vector2 p = view.Root.InverseTransformPoint(art.transform.TransformPoint(vertex));
                            min = Vector2.Min(min, p); max = Vector2.Max(max, p);
                        }
                        var label = (TMPro.TextMeshProUGUI)viewData.FindProperty("stateLabel").objectReferenceValue;
                        var hp = (Image)viewData.FindProperty("hpFill").objectReferenceValue;
                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(art.sprite, out string _, out long id);
                        record.frames.Add(new Frame { state = state, spritePath = AssetDatabase.GetAssetPath(art.sprite), spriteId = id,
                            texturePath = AssetDatabase.GetAssetPath(art.sprite.texture), sourceRect = art.sprite.rect,
                            renderedRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y), labelY = label.rectTransform.anchoredPosition.y,
                            labelSize = label.rectTransform.sizeDelta, fontSize = label.fontSize,
                            hpPosition = ((RectTransform)hp.transform.parent).anchoredPosition });
                    }
                    Object.DestroyImmediate(view.gameObject);
                }
                output.monsters.Add(record);
                var backup = Folder + "Before/" + path;
                Directory.CreateDirectory(Path.GetDirectoryName(backup));
                if (!File.Exists(backup)) File.Copy(path, backup);
            }
            File.WriteAllText(Folder + "Baseline.json", JsonUtility.ToJson(output, true));
            File.WriteAllText(Folder + "BaselineComplete.txt", output.monsters.Count + " monsters, four authored poses, original collision circles, current designer data.\n");
        }
        finally { Object.DestroyImmediate(canvasObject); EditorSceneManager.ClosePreviewScene(scene); }
    }
}
