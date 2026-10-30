using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DungeonSlash.EditorTools
{
    // Draw the production UI meshes, including CombatSpriteAnimator's padding compensation.
    // A private preview scene keeps these objects out of saved scenes and Play mode searches.
    internal sealed class MonsterWeakPointPreview : IDisposable
    {
        internal const string MonsterPrefabPath = "Assets/Prefabs/Combat/MonsterView.prefab";
        internal const string WeakPointPrefabPath = "Assets/Prefabs/Combat/WeakPointView.prefab";
        private readonly List<Mesh> meshes = new();
        private readonly List<Graphic> graphics = new();
        private readonly List<WeakPointView> points = new();
        private Scene scene;
        private GameObject canvasObject;
        private MonsterData previewData;
        private MonsterAttackData previewAttack;
        private RenderTexture texture;
        private string signature;
        private Rect bounds;
        private bool renderDirty = true;
        internal Rect? FixedBounds;
        internal MonsterView Monster { get; private set; }
        internal Image Artwork { get; private set; }
        internal MonsterRuntime Runtime { get; private set; }
        internal IReadOnlyList<WeakPointView> Points => points;
        internal Rect ViewBounds => bounds;

        internal bool Refresh(MonsterData data, MonsterAttackData attack, CombatVisualState pose = CombatVisualState.Ready)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<MonsterView>(MonsterPrefabPath);
            var pointPrefab = AssetDatabase.LoadAssetAtPath<WeakPointView>(WeakPointPrefabPath);
            if (prefab == null || pointPrefab == null) return false;
            // Include referenced presentation assets so edits in another Inspector are reflected too.
            var next = EditorJsonUtility.ToJson(data) + (attack != null ? EditorJsonUtility.ToJson(attack) : "")
                + (data.combatAnimation != null ? EditorJsonUtility.ToJson(data.combatAnimation) : "")
                + EditorUtility.GetDirtyCount(prefab) + EditorUtility.GetDirtyCount(pointPrefab)
                + (prefab.presentation != null ? EditorJsonUtility.ToJson(prefab.presentation) : "")
                + (pointPrefab.reticle != null ? EditorJsonUtility.ToJson(pointPrefab.reticle.Style) : "") + pose;
            if (signature == next && canvasObject != null) return true;
            ClearObjects();
            signature = next;
            renderDirty = true;
            scene = EditorSceneManager.NewPreviewScene();
            canvasObject = new GameObject("Monster weak-point Inspector preview", typeof(RectTransform), typeof(Canvas));
            canvasObject.hideFlags = HideFlags.HideAndDontSave;
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(2048, 2048);

            // Never edit the source assets, including support-gated attacks or localized strings.
            previewData = Object.Instantiate(data);
            previewData.hideFlags = HideFlags.HideAndDontSave;
            previewAttack = attack != null ? Object.Instantiate(attack) : ScriptableObject.CreateInstance<MonsterAttackData>();
            previewAttack.hideFlags = HideFlags.HideAndDontSave;
            previewAttack.type = MonsterAttackType.Charge;
            previewAttack.requiredSupportId = "";
            previewData.attacks = new List<MonsterAttackData> { previewAttack };
            Runtime = new MonsterRuntime(previewData, visualScale: data.combatScale);
            Runtime.StartCharge(Vector2.zero);
            Monster = Object.Instantiate(prefab, canvasObject.transform, false);
            Monster.Root.anchoredPosition = Vector2.zero;
            Monster.Root.localScale = Vector3.one * Runtime.VisualScale;
            // Calling the exact production Bind twice also settles legacy stretch-to-parent layouts.
            // Keep charge definitions for the overlay while selecting the production painting.
            var visualRuntime = pose == CombatVisualState.Idle ? new MonsterRuntime(previewData, visualScale: data.combatScale) : Runtime;
            Monster.Bind(visualRuntime);
            Monster.Bind(visualRuntime);
            using (var monsterObject = new SerializedObject(Monster))
                Artwork = (Image)monsterObject.FindProperty("body").objectReferenceValue;
            foreach (Transform child in Monster.Root)
                if (Artwork == null || child != Artwork.transform) child.gameObject.SetActive(false);
            foreach (var definition in Runtime.WeakPoints)
            {
                var view = Object.Instantiate(pointPrefab, canvasObject.transform, false);
                view.Bind(definition);
                points.Add(view);
            }
            Canvas.ForceUpdateCanvases();
            foreach (var graphic in canvasObject.GetComponentsInChildren<Graphic>())
            {
                if (!graphic.isActiveAndEnabled) continue;
                graphic.Rebuild(CanvasUpdate.PreRender);
                var source = graphic.canvasRenderer.GetMesh();
                if (source == null || source.vertexCount == 0) continue;
                // CanvasRenderer owns a native mesh. Upload a copy with all UI vertex channels.
                var mesh = new Mesh { hideFlags = HideFlags.HideAndDontSave, name = "Weak-point preview UI",
                    vertices = source.vertices, colors32 = source.colors32, triangles = source.triangles };
                var coordinates = new List<Vector4>();
                for (var channel = 0; channel < 4; channel++) { source.GetUVs(channel, coordinates); mesh.SetUVs(channel, coordinates); }
                meshes.Add(mesh);
                graphics.Add(graphic);
            }
            FitBounds();
            if (FixedBounds.HasValue) bounds = FixedBounds.Value;
            return true;
        }

        private void FitBounds()
        {
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            for (var i = 0; i < meshes.Count; i++)
                foreach (var vertex in meshes[i].vertices)
                {
                    Vector2 point = graphics[i].transform.TransformPoint(vertex);
                    min = Vector2.Min(min, point); max = Vector2.Max(max, point);
                }
            if (Runtime != null)
                foreach (var shape in Runtime.Data.BodyShapes(Vector2.zero, Runtime.VisualScale))
                {
                    min = Vector2.Min(min, shape.Center - Vector2.one * shape.Radius);
                    max = Vector2.Max(max, shape.Center + Vector2.one * shape.Radius);
                }
            if (meshes.Count == 0) { bounds = new Rect(-200, -200, 400, 400); return; }
            var margin = Mathf.Max(24, Mathf.Max(max.x - min.x, max.y - min.y) * .08f);
            bounds = Rect.MinMaxRect(min.x - margin, min.y - margin, max.x + margin, max.y + margin);
        }

        internal Texture Render(int width, int height)
        {
            width = Mathf.Clamp(width, 32, 2048); height = Mathf.Clamp(height, 32, 2048);
            if (texture == null || texture.width != width || texture.height != height)
            {
                ReleaseTexture();
                texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
                    { hideFlags = HideFlags.HideAndDontSave, name = "Monster Ready and weak points", antiAliasing = 4 };
                texture.Create();
                renderDirty = true;
            }
            if (!renderDirty) return texture;
            var previous = RenderTexture.active;
            using var commands = new CommandBuffer { name = "Monster weak-point Inspector" };
            try
            {
                commands.SetRenderTarget(texture);
                commands.ClearRenderTarget(false, true, new Color(.105f, .11f, .12f, 1));
                commands.SetViewProjectionMatrices(Matrix4x4.identity,
                    GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(0, width, 0, height, -100, 100), false));
                var scale = Mathf.Min(width / bounds.width, height / bounds.height);
                var translation = new Vector3(width * .5f - bounds.center.x * scale, height * .5f - bounds.center.y * scale, 0);
                var fit = Matrix4x4.TRS(translation, Quaternion.identity, new Vector3(scale, scale, 1));
                for (var i = 0; i < meshes.Count; i++)
                {
                    var graphic = graphics[i];
                    var source = graphic.canvasRenderer.GetMaterial();
                    if (source == null) source = graphic.materialForRendering;
                    var properties = new MaterialPropertyBlock();
                    properties.SetTexture("_MainTex", graphic.mainTexture != null ? graphic.mainTexture : Texture2D.whiteTexture);
                    // Use the same per-draw override as the game's native UI composite.
                    properties.SetFloat("_BehemothHideRasterText", 0);
                    commands.DrawMesh(meshes[i], fit * graphic.transform.localToWorldMatrix, source, 0, 0, properties);
                }
                Graphics.ExecuteCommandBuffer(commands);
            }
            finally { RenderTexture.active = previous; }
            renderDirty = false;
            return texture;
        }

        internal Vector2 PointToGui(Vector2 point, Rect area)
        {
            var scale = Mathf.Min(area.width / bounds.width, area.height / bounds.height);
            return area.center + new Vector2(point.x - bounds.center.x, bounds.center.y - point.y) * scale;
        }

        internal Vector2 GuiToPoint(Vector2 point, Rect area)
        {
            var scale = Mathf.Min(area.width / bounds.width, area.height / bounds.height);
            var delta = (point - area.center) / scale;
            return bounds.center + new Vector2(delta.x, -delta.y);
        }

        internal void Refit() { FixedBounds = null; FitBounds(); renderDirty = true; }

        private void ClearObjects()
        {
            foreach (var mesh in meshes) Object.DestroyImmediate(mesh);
            meshes.Clear(); graphics.Clear(); points.Clear();
            if (canvasObject != null) Object.DestroyImmediate(canvasObject);
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            if (previewData != null) Object.DestroyImmediate(previewData);
            if (previewAttack != null) Object.DestroyImmediate(previewAttack);
            Monster = null; Artwork = null; Runtime = null;
        }

        private void ReleaseTexture()
        {
            if (texture == null) return;
            texture.Release(); Object.DestroyImmediate(texture); texture = null;
        }

        public void Dispose() { ClearObjects(); ReleaseTexture(); signature = null; }
    }
}
