using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

public sealed class BehemothLocaleWindow : EditorWindow
{
    [SerializeField] private string query = "";
    [SerializeField] private string selectedKey = "";
    [SerializeField] private string original = "";
    [SerializeField] private string draft = "";
    private Vector2 resultsScroll, detailScroll;
    private List<GameTextEntry> results = new();
    private List<BehemothLocaleEditing.Usage> usages = new();
    private bool searchedUsages;
    private string message;
    private MessageType messageType;
    private bool Dirty => draft != original;

    [MenuItem("Behemoth/텍스트/로케일 검색·편집", priority = 0)]
    public static void Open() => OpenSearch("");

    public static void OpenSearch(string text)
    {
        var window = GetWindow<BehemothLocaleWindow>("로케일 검색·편집");
        window.minSize = new Vector2(740, 520);
        window.query = text;
        window.Search();
        if (!window.docked)
        {
            var main = EditorGUIUtility.GetMainWindowPosition();
            window.position = new Rect(main.center.x - 500, main.center.y - 340, 1000, 680);
        }
        window.Show();
    }

    private void OnEnable() { Search(); searchedUsages = false; usages = new List<BehemothLocaleEditing.Usage>(); saveChangesMessage = "편집한 로케일 문구를 저장할까요?"; }
    public override void SaveChanges() { Save(); if (!Dirty) base.SaveChanges(); }
    public override void DiscardChanges() { draft = original; base.DiscardChanges(); }
    private void Search() => results = BehemothLocaleEditing.Search(BehemothTextEditor.Table, query);

    private bool ConfirmDraft() => !Dirty || EditorUtility.DisplayDialog("저장하지 않은 문구", "편집 중인 문구를 버리고 이동할까요?", "버리고 이동", "계속 편집");

    private void Select(GameTextEntry entry)
    {
        if (entry.key == selectedKey || !ConfirmDraft()) return;
        selectedKey = entry.key;
        original = draft = entry.kr ?? "";
        message = null;
        usages.Clear(); searchedUsages = false;
        detailScroll = Vector2.zero;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox("화면에 보이는 문구의 일부를 검색하세요. 결과를 선택하면 키·한국어 원문·사용처를 확인하고 수정할 수 있습니다.", MessageType.Info);
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            EditorGUI.BeginChangeCheck();
            query = GUILayout.TextField(query, EditorStyles.toolbarSearchField);
            if (EditorGUI.EndChangeCheck()) { Search(); resultsScroll = Vector2.zero; }
            if (GUILayout.Button("지우기", EditorStyles.toolbarButton, GUILayout.Width(48))) { query = ""; GUI.FocusControl(null); Search(); }
            if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(70))) Search();
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(Mathf.Clamp(position.width * .42f, 280, 450))))
            {
                EditorGUILayout.LabelField($"검색 결과 {results.Count}개", EditorStyles.boldLabel);
                resultsScroll = EditorGUILayout.BeginScrollView(resultsScroll);
                foreach (var entry in results)
                {
                    var label = entry.kr?.Replace("\r", "").Replace("\n", " / ") ?? "";
                    if (label.Length > 65) label = label.Substring(0, 65) + "…";
                    var style = new GUIStyle(EditorStyles.miniButton) { alignment = TextAnchor.MiddleLeft, wordWrap = true, fixedHeight = 0, fontStyle = entry.key == selectedKey ? FontStyle.Bold : FontStyle.Normal };
                    if (GUILayout.Toggle(entry.key == selectedKey, new GUIContent(label + "\n" + entry.key, entry.kr), style, GUILayout.MinHeight(46))) Select(entry);
                }
                if (results.Count == 0) EditorGUILayout.HelpBox("일치하는 문구나 키가 없습니다. 더 짧은 단어로 검색해 보세요.", MessageType.Info);
                EditorGUILayout.EndScrollView();
            }
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) DrawDetails();
        }
        hasUnsavedChanges = Dirty;
    }

    private void DrawDetails()
    {
        var entry = BehemothTextEditor.Table.entries.FirstOrDefault(e => e.key == selectedKey);
        if (entry == null) { EditorGUILayout.LabelField("왼쪽 검색 결과에서 문구를 선택하세요."); return; }
        detailScroll = EditorGUILayout.BeginScrollView(detailScroll);
        EditorGUILayout.LabelField("로케일 키", EditorStyles.boldLabel);
        EditorGUILayout.SelectableLabel(entry.key, EditorStyles.textField, GUILayout.Height(36));
        if (GUILayout.Button("키 복사", GUILayout.Width(80))) GUIUtility.systemCopyBuffer = entry.key;
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("한국어 문구" + (Dirty ? " · 수정 중" : ""), EditorStyles.boldLabel);
        draft = EditorGUILayout.TextArea(draft, new GUIStyle(EditorStyles.textArea) { wordWrap = true }, GUILayout.MinHeight(150));
        var valid = BehemothLocaleEditing.Validate(original, draft, out var error);
        if (!valid) EditorGUILayout.HelpBox(error, MessageType.Error);
        if (entry.kr != original) EditorGUILayout.HelpBox("이 문구가 다른 곳에서 변경되었습니다. ‘현재 값 다시 읽기’ 후 편집하세요.", MessageType.Warning);
        if (EditorApplication.isPlayingOrWillChangePlaymode) EditorGUILayout.HelpBox("검색은 Play 중에도 가능합니다. 영구 저장은 Play 종료 후 사용하세요.", MessageType.Info);
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(!Dirty || !valid || EditorApplication.isPlayingOrWillChangePlaymode || entry.kr != original))
                if (GUILayout.Button("저장 · 테이블 / 데이터 / CSV", GUILayout.Height(28))) Save();
            if (GUILayout.Button("현재 값 다시 읽기", GUILayout.Height(28)) && ConfirmDraft())
            { original = draft = entry.kr ?? ""; message = null; }
        }
        if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, messageType);
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("등록된 출처", EditorStyles.boldLabel);
        EditorGUILayout.SelectableLabel(entry.source ?? "", EditorStyles.textField, GUILayout.Height(36));
        if (GUILayout.Button("이 키의 사용처 찾기 (코드·데이터·씬·프리팹)"))
        {
            try { usages = BehemothLocaleEditing.FindUsages(entry.key); searchedUsages = true; }
            catch (Exception exception) { message = exception.Message; messageType = MessageType.Error; }
        }
        if (searchedUsages)
        {
            EditorGUILayout.LabelField($"직접 참조 {usages.Count}곳", EditorStyles.boldLabel);
            foreach (var usage in usages)
            {
                if (GUILayout.Button(new GUIContent($"{usage.path}:{usage.line}\n{usage.preview}", usage.path), new GUIStyle(EditorStyles.miniButton) { alignment = TextAnchor.MiddleLeft, wordWrap = true, fixedHeight = 0 }, GUILayout.MinHeight(40)))
                {
                    var asset = AssetDatabase.LoadMainAssetAtPath(usage.path);
                    EditorGUIUtility.PingObject(asset);
                    if (usage.path.EndsWith(".cs", StringComparison.Ordinal)) AssetDatabase.OpenAsset(asset, usage.line);
                    else Selection.activeObject = asset;
                }
            }
            if (usages.Count == 0) EditorGUILayout.HelpBox("직접 참조가 없습니다. 조합된 키는 코드의 공통 접두어로 확인하세요.", MessageType.Info);
        }
        EditorGUILayout.EndScrollView();
    }

    private void Save()
    {
        try
        {
            BehemothLocaleEditing.Save(selectedKey, original, draft);
            original = draft;
            message = "저장했습니다. 테이블·연결 데이터·kr.csv에 반영되었습니다. 게임 화면은 다음 표시부터 적용됩니다.";
            messageType = MessageType.Info;
            Search();
        }
        catch (Exception exception) { message = exception.Message; messageType = MessageType.Error; }
    }
}

internal static class BehemothLocaleEditing
{
    internal sealed class Usage { public string path, preview; public int line; }
    private static string Normalize(string text) => Regex.Replace(text ?? "", @"\s+", " ").Trim();
    internal static List<GameTextEntry> Search(GameTextDatabase table, string query)
    {
        var needle = Normalize(query);
        return table.entries.Where(e => new[] { e.key, e.kr, e.source }.Any(value => Normalize(value).IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0))
            .OrderBy(e => Normalize(e.kr).Equals(needle, StringComparison.OrdinalIgnoreCase) ? 0 : 1).ThenBy(e => e.key, StringComparer.Ordinal).ToList();
    }

    internal static bool Validate(string previous, string next, out string error)
    {
        try
        {
            string.Format(CultureInfo.InvariantCulture, next, Enumerable.Repeat<object>(0, 256).ToArray());
            if (!Arguments(previous).SetEquals(Arguments(next))) { error = "기존 치환자 번호({0}, {1} 등)를 유지하세요. 순서는 바꿀 수 있습니다."; return false; }
            error = null; return true;
        }
        catch (FormatException) { error = "치환자 형식이 잘못되었습니다. 예: {0}, {1:0.0}. 일반 중괄호는 {{ 또는 }}로 입력하세요."; return false; }
    }
    private static HashSet<string> Arguments(string text)
    {
        var indices = new HashSet<string>();
        for (var i = 0; i < (text ?? "").Length; i++)
        {
            if (text[i] != '{') continue;
            if (i + 1 < text.Length && text[i + 1] == '{') { i++; continue; }
            var start = ++i;
            while (i < text.Length && char.IsDigit(text[i])) i++;
            if (i > start) indices.Add(int.Parse(text.Substring(start, i - start), CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture));
        }
        return indices;
    }

    internal static void Save(string key, string previous, string next)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play 종료 후 저장하세요.");
        if (!Validate(previous, next, out var error)) throw new FormatException(error);
        var table = BehemothTextEditor.Table;
        if (!table.Contains(key) || table.Get(key) != previous) throw new InvalidOperationException("원문이 변경되었습니다. 현재 값을 다시 읽으세요.");
        var entry = table.entries.First(e => e.key == key);
        var csv = File.Exists(BehemothTextEditor.CsvPath) ? File.ReadAllText(BehemothTextEditor.CsvPath) : table.ExportCsv();
        var patched = PatchCsv(csv, key, previous, next, entry.source);
        var owners = AssetDatabase.FindAssets("t:LocalizedData", new[] { "Assets/Data" })
            .Select(guid => AssetDatabase.LoadAssetAtPath<LocalizedData>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(data => data != null && data.textKeys.Any(binding => binding.key == key)).ToArray();
        foreach (var data in owners)
        {
            var serialized = new SerializedObject(data);
            foreach (var binding in data.textKeys.Where(binding => binding.key == key))
            {
                var property = serialized.FindProperty(binding.propertyPath);
                if (property != null && property.stringValue != previous) throw new InvalidOperationException("연결 데이터가 변경되었습니다. 원본 Inspector에서 확인한 후 다시 검색하세요: " + data.name);
            }
        }
        // CSV 충돌·입력 오류를 모두 검사한 뒤 선택한 행만 기록한다.
        File.WriteAllText(BehemothTextEditor.CsvPath, patched, new UTF8Encoding(true));
        table.Set(key, next); EditorUtility.SetDirty(table);
        foreach (var data in owners) { BehemothTextEditor.SyncAsset(data, true); AssetDatabase.SaveAssetIfDirty(data); }
        AssetDatabase.SaveAssetIfDirty(table);
        AssetDatabase.ImportAsset(BehemothTextEditor.CsvPath);
        foreach (var binding in UnityEngine.Object.FindObjectsByType<GameTextBinding>(FindObjectsInactive.Include, FindObjectsSortMode.None)) binding.Refresh();
    }

    internal static string PatchCsv(string csv, string key, string previous, string next, string source)
    {
        var parsed = ScriptableObject.CreateInstance<GameTextDatabase>();
        try
        {
            parsed.ImportCsv(csv);
            if (parsed.Contains(key) && parsed.Get(key) != previous && parsed.Get(key) != next)
                throw new InvalidOperationException("CSV의 같은 문구가 외부에서 변경되었습니다. CSV 가져오기 후 다시 편집하세요.");
        }
        finally { UnityEngine.Object.DestroyImmediate(parsed); }
        string Cell(string value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
        var replacement = Cell(key) + "," + Cell(next) + "," + Cell(source);
        var start = 0; var quoted = false; var header = true;
        for (var i = 0; i <= csv.Length; i++)
        {
            if (i < csv.Length && csv[i] == '"')
            {
                if (quoted && i + 1 < csv.Length && csv[i + 1] == '"') { i++; continue; }
                quoted = !quoted;
            }
            if (i < csv.Length && (quoted || (csv[i] != '\r' && csv[i] != '\n'))) continue;
            var record = csv.Substring(start, i - start).TrimStart('\uFEFF');
            if (!header && (record.StartsWith(Cell(key) + ",", StringComparison.Ordinal) || record.StartsWith(key + ",", StringComparison.Ordinal)))
                return csv.Substring(0, start) + replacement + csv.Substring(i);
            header = false;
            if (i < csv.Length && csv[i] == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n') i++;
            start = i + 1;
        }
        return csv + (csv.EndsWith("\n") ? "" : "\r\n") + replacement + "\r\n";
    }

    internal static List<Usage> FindUsages(string key)
    {
        var result = new List<Usage>();
        var reference = new Regex(@"(?<![\w.])" + Regex.Escape(key) + @"(?![\w.])");
        var extensions = new HashSet<string> { ".cs", ".asset", ".prefab", ".unity" };
        var roots = new[] { "Assets/Scripts/", "Assets/Editor/", "Assets/Data/", "Assets/Scenes/", "Assets/Prefabs/" };
        foreach (var path in AssetDatabase.GetAllAssetPaths().Where(path => roots.Any(root => path.StartsWith(root, StringComparison.Ordinal)) && !path.StartsWith("Assets/Editor/Verification/", StringComparison.Ordinal) && extensions.Contains(Path.GetExtension(path)) && path != BehemothTextEditor.TablePath))
        {
            var lines = File.ReadAllLines(path);
            for (var i = 0; i < lines.Length; i++)
                if (reference.IsMatch(lines[i])) result.Add(new Usage { path = path, line = i + 1, preview = lines[i].Trim() });
        }
        return result;
    }
}
