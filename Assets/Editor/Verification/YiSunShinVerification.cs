using System.IO;
using System.Linq;
using System.Text;
using DungeonSlash;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Explicit requests only, limited to font migration and affected text surfaces in the live Editor.
[InitializeOnLoad]
internal static class YiSunShinVerification
{
    const string Request = "Temp/YiSunShin.request";
    const string Folder = "Docs/Production/Typography/YiSunShin/";
    static string capture;
    static double captureAt;
    static YiSunShinVerification() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (capture != null && EditorApplication.timeSinceStartup > captureAt)
        {
            Audit(capture); capture = null;
        }
        if (!File.Exists(Request)) return;
        var command = File.ReadAllText(Request).Trim(); File.Delete(Request);
        Directory.CreateDirectory(Folder);
        if (command == "save-references")
        {
            YiSunShinPreparation.SyncSavedReferences(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(YiSunShinPreparation.FontPath));
            return;
        }
        if (command == "symbols")
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(YiSunShinPreparation.FontPath);
            var symbols = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/TextMeshPro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (!font.fallbackFontAssetTable.Contains(symbols)) font.fallbackFontAssetTable.Add(symbols);
            EditorUtility.SetDirty(font); AssetDatabase.SaveAssets();
            return;
        }
        if (command.StartsWith("open:"))
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty) return;
            var scene = command.Substring(5) == "title" ? "OutGameScene" : "MainGameScene";
            EditorSceneManager.OpenScene("Assets/Scenes/" + scene + ".unity");
            EditorApplication.isPlaying = true;
            return;
        }
        if (!EditorApplication.isPlaying) return;
        if (command.StartsWith("capture:"))
        {
            capture = command.Substring(8); captureAt = EditorApplication.timeSinceStartup + .5;
        }
        if (command == "event")
        {
            var story = DungeonSlash.DesignerAssetPaths.Load<RandomEventData>("Assets/Data/Dungeon/Events/Data_Event_내일의_한숨.asset");
            Object.FindFirstObjectByType<RunExpansionView>().ShowModal(story.displayName, story.narrative,
                story.options.Select(o => new PresentationChoice { Title = o.title, Description = o.description }), story.illustration);
        }
        if (command == "damage")
        {
            Object.FindFirstObjectByType<RunExpansionView>().HideModal();
            var combat = Object.FindFirstObjectByType<CombatController>();
            typeof(CombatController).GetMethod("ShowHitDamage", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(combat, new object[] { Vector2.zero, 123f, true });
            capture = "CombatNumbers"; captureAt = EditorApplication.timeSinceStartup + .1;
        }
    }

    static void Audit(string name)
    {
        var expected = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(YiSunShinPreparation.FontPath);
        Canvas.ForceUpdateCanvases();
        var report = new StringBuilder("Scoped live Unity font verification / " + Screen.width + "x" + Screen.height + "\n");
        var wrong = 0; var missing = 0; var count = 0;
        foreach (var label in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(t => t.isActiveAndEnabled && !string.IsNullOrWhiteSpace(t.text)))
        {
            label.GetComponent<TypographyRoleBinding>()?.Apply(); label.ForceMeshUpdate(); count++;
            if (label.font != expected) wrong++;
            var unavailable = label.textInfo.characterInfo.Take(label.textInfo.characterCount)
                .Where(c => !char.IsWhiteSpace(c.character) && (c.textElement == null || c.textElement.glyphIndex == 0)).Select(c => c.character).Distinct().ToArray();
            missing += unavailable.Length;
            report.AppendLine(label.name + " | " + label.font.name + " | size=" + label.fontSize + " | overflow=" + label.isTextOverflowing + " | missing=" + new string(unavailable) + " | " + label.text.Replace('\n', ' '));
        }
        report.AppendLine("Labels=" + count + "; wrong font=" + wrong + "; missing glyphs=" + missing);
        File.WriteAllText(Folder + name + ".txt", report.ToString());
        ScreenCapture.CaptureScreenshot(Folder + name + ".png");
    }
}
