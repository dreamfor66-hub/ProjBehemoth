using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DungeonSlash;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Temporary, scoped sprite import/render verification in the existing Editor. No desktop input.
[InitializeOnLoad]
internal static class SpriteSeparationVerification
{
    const string Request = "Temp/SpriteSeparation.request";
    const string Output = "Docs/Production/SpriteSeparation-2026-10-25/";
    [Serializable] sealed class Plan { public Entry[] sprites; public string[] sources; }
    [Serializable] sealed class Entry { public string destination, guid, name; }
    static GameObject overlay;
    static Image image;
    static CombatSpriteAnimator animator;
    static List<(CombatSpriteAnimation library, CombatVisualState state)> cases;
    static int index, frame;
    static bool background;
    static StringBuilder matrix;
    static double deadline;
    static SpriteSeparationVerification() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (cases != null) { try { Step(); } catch (Exception e) { Finish(e); } return; }
        if (!File.Exists(Request)) return;
        string command;
        try { command = File.ReadAllText(Request).Trim(); File.Delete(Request); } catch (IOException) { return; }
        try
        {
            Directory.CreateDirectory(Output);
            if (command == "lock")
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before replacing serialized sprite references.");
                AssetDatabase.DisallowAutoRefresh(); EditorApplication.LockReloadAssemblies();
                File.WriteAllText("Temp/SpriteSeparation.locked", "locked");
            }
            else if (command == "unlock")
            {
                AssetDatabase.AllowAutoRefresh(); EditorApplication.UnlockReloadAssemblies();
                File.Delete("Temp/SpriteSeparation.locked"); AssetDatabase.Refresh();
            }
            else if (command == "verify") Verify();
            else if (command == "runtime") Begin();
            else throw new InvalidOperationException(command);
            File.WriteAllText("Logs/SpriteSeparation-Status.txt", command + " PASS " + DateTime.Now.ToString("O"));
        }
        catch (Exception e) { File.WriteAllText("Logs/SpriteSeparation-Status.txt", command + " FAIL " + e); Debug.LogException(e); }
    }
    static void Verify()
    {
        var plan = JsonUtility.FromJson<Plan>(File.ReadAllText("Docs/Production/SpriteSeparation-2026-10-25/Manifest.json"));
        var log = new StringBuilder(); var failures = 0;
        foreach (var entry in plan.sprites)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(entry.destination);
            var importer = AssetImporter.GetAtPath(entry.destination) as TextureImporter;
            var ok = sprite != null && importer != null && importer.spriteImportMode == SpriteImportMode.Single
                && sprite.texture.filterMode == FilterMode.Point && AssetDatabase.AssetPathToGUID(entry.destination) == entry.guid;
            if (!ok) { failures++; log.AppendLine("FAIL " + entry.destination); }
        }
        foreach (var source in plan.sources)
            if (File.Exists(source) || AssetDatabase.LoadMainAssetAtPath(source) != null) { failures++; log.AppendLine("OLD SHEET " + source); }
        foreach (var guid in AssetDatabase.FindAssets("t:CombatSpriteAnimation", new[] { "Assets/Data/Presentation/Animations" }))
        {
            var library = AssetDatabase.LoadAssetAtPath<CombatSpriteAnimation>(AssetDatabase.GUIDToAssetPath(guid));
            foreach (CombatVisualState state in Enum.GetValues(typeof(CombatVisualState)))
            {
                var sprite = library.For(state); if (sprite == null) continue;
                if ((AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite)) as TextureImporter)?.spriteImportMode != SpriteImportMode.Single)
                { failures++; log.AppendLine("NOT SINGLE " + library.name + "/" + state); }
            }
        }
        log.AppendLine("Imported single sprites " + plan.sprites.Length + "; failures " + failures);
        File.WriteAllText(Output + "UnityImports.txt", log.ToString());
        if (failures > 0) throw new InvalidOperationException(log.ToString());
    }
    static void Begin()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Runtime verification requires Play mode.");
        var canvas = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).First(c => c.isRootCanvas);
        overlay = new GameObject("Scoped sprite render review", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlay.layer = canvas.gameObject.layer; overlay.transform.SetParent(canvas.transform, false);
        var rect = (RectTransform)overlay.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var shade = overlay.GetComponent<Image>(); shade.color = new Color32(34, 38, 45, 255); shade.raycastTarget = false;
        var art = new GameObject("State image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        art.layer = overlay.layer; art.transform.SetParent(overlay.transform, false);
        image = art.GetComponent<Image>(); image.preserveAspect = true; image.raycastTarget = false;
        animator = art.AddComponent<CombatSpriteAnimator>();
        animator.presentation = AssetDatabase.LoadAssetAtPath<ArcadePresentation>("Assets/Data/Presentation/Arcade224.asset");
        cases = new List<(CombatSpriteAnimation, CombatVisualState)>();
        foreach (var guid in AssetDatabase.FindAssets("t:CombatSpriteAnimation", new[] { "Assets/Data/Presentation/Animations" }))
        {
            var library = AssetDatabase.LoadAssetAtPath<CombatSpriteAnimation>(AssetDatabase.GUIDToAssetPath(guid));
            foreach (CombatVisualState state in Enum.GetValues(typeof(CombatVisualState))) if (library.For(state) != null) cases.Add((library, state));
        }
        background = Application.runInBackground; Application.runInBackground = true;
        matrix = new StringBuilder("Scoped live Unity Image + CombatSpriteAnimator state render matrix. No physics/balance suite.\n");
        Directory.CreateDirectory(Output + "Runtime"); index = -1; frame = -1;
        Next();
    }
    static void Next()
    {
        index++;
        if (index >= cases.Count) { Finish(null); return; }
        var item = cases[index]; var scale = item.library.canvasScale;
        image.rectTransform.sizeDelta = new Vector2(1000, 620) / scale;
        animator.Play(item.library, item.state); Canvas.ForceUpdateCanvases();
        frame = Time.renderedFrameCount; deadline = EditorApplication.timeSinceStartup + 15;
    }
    static void Step()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play ended before the render matrix finished.");
        if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("No rendered frame for " + cases[index]);
        if (Time.renderedFrameCount < frame + 2) { EditorApplication.QueuePlayerLoopUpdate(); return; }
        var item = cases[index]; var mesh = image.canvasRenderer.GetMesh();
        var sprite = item.library.For(item.state); var bounds = mesh.bounds;
        var expected = image.rectTransform.rect.size; var aspect = sprite.rect.width / sprite.rect.height;
        if (expected.x / expected.y > aspect) expected.x = expected.y * aspect; else expected.y = expected.x / aspect;
        expected *= item.library.canvasScale;
        var ok = image.sprite == sprite && mesh.vertexCount >= 4 && Mathf.Abs(bounds.size.x - expected.x) < 2 && Mathf.Abs(bounds.size.y - expected.y) < 2;
        if (!ok) throw new InvalidOperationException("Sprite/mesh mismatch " + item.library.name + "/" + item.state + " " + bounds.size + " expected " + expected);
        matrix.AppendLine("PASS " + item.library.name + "/" + item.state + " single sprite; padded mesh " + bounds.size);
        var raster = Object.FindFirstObjectByType<ArcadeRasterCamera>();
        if (raster?.Frame != null)
        {
            var old = RenderTexture.active; RenderTexture.active = raster.Frame;
            var png = new Texture2D(raster.Frame.width, raster.Frame.height, TextureFormat.RGB24, false);
            png.ReadPixels(new Rect(0, 0, png.width, png.height), 0, 0); png.Apply();
            File.WriteAllBytes(Output + "Runtime/" + item.library.name + "_" + item.state + ".png", png.EncodeToPNG());
            Object.DestroyImmediate(png); RenderTexture.active = old;
        }
        Next();
    }
    static void Finish(Exception error)
    {
        if (overlay != null) Object.DestroyImmediate(overlay);
        Application.runInBackground = background;
        matrix?.AppendLine(error == null ? "COMPLETE " + cases.Count + " cases" : "FAIL " + error);
        File.WriteAllText(Output + "RuntimeMatrix.txt", matrix?.ToString() ?? error?.ToString());
        File.WriteAllText("Logs/SpriteSeparation-Status.txt", error == null ? "runtime PASS " + cases.Count : "runtime FAIL " + error);
        cases = null; overlay = null;
    }
}
