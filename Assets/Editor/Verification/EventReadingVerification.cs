using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonSlash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Explicit requests only, in the existing Editor. Does not start runs or pay event costs.
[InitializeOnLoad]
internal static class EventReadingVerification
{
    const string Request = "Temp/EventReading.request";
    const string Folder = "Docs/Production/EventReading-2026-10-25/";
    const BindingFlags Private = BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic;
    static int captureFrames;
    static string captureLabel;
    static EventReadingVerification() => EditorApplication.update += Poll;

    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (captureFrames > 0 && --captureFrames == 0) Capture(captureLabel);
        if (!File.Exists(Request)) return;
        string command;
        try { command = File.ReadAllText(Request).Trim(); File.Delete(Request); }
        catch (IOException) { return; }
        try
        {
            if (command == "open-main")
            {
                for (var i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Unsaved scene edits; refusing to replace loaded scenes.");
                EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity"); return;
            }
            if (command == "play") { EditorApplication.isPlaying = true; return; }
            if (!EditorApplication.isPlaying) return;
            var view = Object.FindFirstObjectByType<RunExpansionView>();
            var story = AssetDatabase.LoadAssetAtPath<RandomEventData>("Assets/Data/Dungeon/Events/MagicTeacherFirst.asset");
            if (command.StartsWith("capture:"))
            {
                captureLabel = command.Substring(8); captureFrames = 8; return;
            }
            if (command.StartsWith("page:"))
            {
                var index = int.Parse(command.Substring(5));
                typeof(GameFlowController).GetMethod("ShowEventPage", Private).Invoke(Object.FindFirstObjectByType<GameFlowController>(),
                    new object[] { Object.FindFirstObjectByType<DungeonController>().State.Player.CurrentRoom, story, index, 0 });
            }
            if (command is "full:1" or "full:2" or "plain" or "description" or "dense")
            {
                var options = story.pages[1].options.Take(command == "full:2" ? 2 : 1)
                    .Select(o => new PresentationChoice { Title = o.title, Selected = view.HideModal }).ToArray();
                var narrative = story.pages[1].narrative;
                if (command == "plain") narrative = string.Join("\n\n", Enumerable.Repeat(narrative, 6));
                if (command == "description")
                    options = new[] { new PresentationChoice { Title = story.pages[2].options[0].title, Description = story.pages[2].options[0].description, Requirements = "30G", Selected = view.HideModal }, new PresentationChoice { Title = GameText.Get("ui.event.leave"), Selected = view.HideModal } };
                if (command == "dense") options = Enumerable.Range(0, 4).Select(_ => new PresentationChoice { Title = GameText.Get("ui.event.continue"), Description = story.pages[2].options[0].description, Selected = view.HideModal }).ToArray();
                view.ShowModal(story.displayName, narrative, options, command == "plain" ? null : story.illustration, command == "dense");
            }
            if (command == "paragraphs")
            {
                Directory.CreateDirectory(Folder);
                var split = typeof(GameFlowController).GetMethod("SplitEventParagraphs", Private);
                foreach (var ending in new[] { "\n", "\r\n", "\r" })
                {
                    var result = (string[])split.Invoke(null, new object[] { "A" + ending + ending + "B" });
                    File.AppendAllText(Folder + "Runtime.txt", (result.SequenceEqual(new[] { "A", "B" }) ? "PASS " : "FAIL ") + "paragraph separator " + ending.Replace("\r", "CR").Replace("\n", "LF") + "\n");
                }
            }
        }
        catch (Exception error) { Directory.CreateDirectory(Folder); File.AppendAllText(Folder + "Runtime.txt", error + "\n"); Debug.LogException(error); }
    }

    static Rect Bounds(RectTransform target, RectTransform relativeTo)
    {
        var corners = new Vector3[4]; target.GetWorldCorners(corners);
        var min = relativeTo.InverseTransformPoint(corners[0]); var max = relativeTo.InverseTransformPoint(corners[2]);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    static void Capture(string label)
    {
        Directory.CreateDirectory(Folder + "Screenshots");
        var view = Object.FindFirstObjectByType<RunExpansionView>();
        var scroll = view.narrativeScroll;
        var root = (RectTransform)scroll.transform.parent;
        var body = Bounds(scroll.viewport, root);
        var bar = Bounds((RectTransform)scroll.verticalScrollbar.transform, root);
        var rows = view.ActiveChoices.Where(c => c.button.gameObject.activeSelf).ToArray();
        var overlap = rows.Any(c => body.Overlaps(Bounds((RectTransform)c.button.transform, root)));
        var text = $"\n{label} / {Screen.width}x{Screen.height}\nbody={body}; scrollbar={bar}; content={scroll.content.rect.size}; scroll={scroll.verticalNormalizedPosition:F3}; visible={scroll.verticalScrollbar.gameObject.activeInHierarchy}\n";
        text += (bar.xMin >= body.xMax + view.narrativeScrollbarGap - .1f ? "PASS " : "FAIL ") + "scrollbar outside text\n";
        text += (!overlap ? "PASS " : "FAIL ") + "body and choices separated\n";
        foreach (var row in rows) text += row.title.text + " / row=" + Bounds((RectTransform)row.button.transform, root) + " / description=" + row.description.gameObject.activeSelf + " / overflow=" + row.description.isTextOverflowing + "\n";
        text += "narrative=" + view.modalNarrative.text.Replace("\r", "").Replace("\n", " / ") + "\n";
        File.AppendAllText(Folder + "Runtime.txt", text);
        ScreenCapture.CaptureScreenshot(Folder + "Screenshots/" + label + ".png");
    }
}
