using System;
using System.IO;
using System.Linq;
using System.Text;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

// 기존 Editor에서 요청한 로케일 동작만 확인한다. 게임 진행이나 전체 테스트를 실행하지 않는다.
[InitializeOnLoad]
internal static class BehemothLocaleVerification
{
    private const string Request = "Temp/BehemothLocale.request";
    static BehemothLocaleVerification() => EditorApplication.update += Poll;
    private static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Request)) return;
        var command = File.ReadAllText(Request).Trim();
        if (command == "verify" && EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Request);
        if (command == "open") BehemothLocaleWindow.OpenSearch("야영은 이 층");
        if (command == "verify") Verify();
    }

    private static void Verify()
    {
        var report = new StringBuilder();
        void Check(bool condition, string label) { if (!condition) throw new Exception(label); report.AppendLine("PASS: " + label); }
        var table = BehemothTextEditor.Table;
        var sandbox = ScriptableObject.CreateInstance<GameTextDatabase>();
        try
        {
            Check(BehemothLocaleEditing.Search(table, "야영은 이 층에서 한 번만 할 수 있다").Any(e => e.key == "ui.camp.intro"), "화면 문구로 ui.camp.intro 찾기");
            Check(BehemothLocaleEditing.Search(table, "UI.CAMP.INTRO").Any(e => e.key == "ui.camp.intro"), "키 대소문자 무시 검색");
            Check(BehemothLocaleEditing.Search(table, "__없는_로케일_검증__").Count == 0, "검색 결과 없음");
            Check(BehemothLocaleEditing.Validate("{0} / {1}", "{1} / {0:0.0}", out _), "치환자 순서·서식 편집");
            Check(!BehemothLocaleEditing.Validate("{0}", "{1}", out _), "치환자 누락·추가 차단");
            Check(!BehemothLocaleEditing.Validate("{0}", "{0", out _), "잘못된 중괄호 차단");
            Check(BehemothLocaleEditing.Validate("문구", "{{0}} 문구", out _), "이스케이프된 중괄호 허용");
            const string csv = "key,kr,source\r\nother,외부 수정 유지,Source\r\nkey,원문,Source\r\n";
            var patched = BehemothLocaleEditing.PatchCsv(csv, "key", "원문", "쉼표, 인용 \"문구\"\n다음 줄", "Source");
            sandbox.ImportCsv(patched);
            Check(sandbox.Get("key") == "쉼표, 인용 \"문구\"\n다음 줄", "CSV 쉼표·따옴표·개행 왕복");
            Check(patched.Contains("other,외부 수정 유지,Source\r\n"), "다른 CSV 행의 내용·형식 보존");
            var conflict = false;
            try { BehemothLocaleEditing.PatchCsv(csv, "key", "이전 값", "새 문구", "Source"); }
            catch (InvalidOperationException) { conflict = true; }
            Check(conflict, "CSV 외부 동일 행 변경 충돌 차단");
            Check(BehemothLocaleEditing.FindUsages("ui.camp.intro").Any(u => u.path.EndsWith("CampPresentation.cs")), "실제 코드 사용처 찾기");
            VerifySave("ui.camp.intro", null, Check);
            var owner = AssetDatabase.FindAssets("t:LocalizedData", new[] { "Assets/Data" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<LocalizedData>(AssetDatabase.GUIDToAssetPath(guid)))
                .First(data => data != null && data.textKeys.Any(binding => binding.propertyPath == "displayName"));
            VerifySave(owner.textKeys.First(binding => binding.propertyPath == "displayName").key, owner, Check);
            report.AppendLine("완료: 검증용 문구를 모두 원래 값으로 복구함.");
        }
        catch (Exception exception) { report.AppendLine("FAIL: " + exception); }
        finally { UnityEngine.Object.DestroyImmediate(sandbox); }
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/Behemoth-LocaleVerification.txt", report.ToString());
    }

    private static void VerifySave(string key, LocalizedData owner, Action<bool, string> check)
    {
        var previous = BehemothTextEditor.Table.Get(key);
        var next = previous + " [로케일 저장 검증]";
        try
        {
            BehemothLocaleEditing.Save(key, previous, next);
            check(BehemothTextEditor.Table.Get(key) == next, key + " 테이블 저장");
            var csvTable = ScriptableObject.CreateInstance<GameTextDatabase>();
            try { csvTable.ImportCsv(File.ReadAllText(BehemothTextEditor.CsvPath)); check(csvTable.Get(key) == next, key + " CSV 저장"); }
            finally { UnityEngine.Object.DestroyImmediate(csvTable); }
            if (owner != null) check(new SerializedObject(owner).FindProperty("displayName").stringValue == next, key + " 연결 데이터 동기화");
        }
        finally
        {
            if (BehemothTextEditor.Table.Get(key) == next) BehemothLocaleEditing.Save(key, next, previous);
        }
        check(BehemothTextEditor.Table.Get(key) == previous, key + " 원문 복구");
    }
}
