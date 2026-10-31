using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DungeonSlash
{
    [Serializable] public sealed class GameTextEntry { public string key; [TextArea(2, 10)] public string kr; public string source; }
    [CreateAssetMenu(menuName = "Dungeon Slash/Korean Text Table")]
    public sealed class GameTextDatabase : ScriptableObject
    {
        public List<GameTextEntry> entries = new();
        private Dictionary<string, GameTextEntry> cache;
        public void Invalidate() => cache = null;
        private void EnsureCache() { if (cache == null) cache = entries.Where(item => !string.IsNullOrEmpty(item.key)).GroupBy(item => item.key).ToDictionary(group => group.Key, group => group.Last()); }
        public string Get(string key) { EnsureCache(); return cache.TryGetValue(key, out var entry) ? entry.kr : key; }
        public bool Contains(string key) { EnsureCache(); return cache.ContainsKey(key); }
        public bool Set(string key, string text, string source = null)
        {
            EnsureCache();
            if (cache.TryGetValue(key, out var entry)) { if (entry.kr == text) return false; entry.kr = text; return true; }
            entry = new GameTextEntry { key = key, kr = text ?? "", source = source ?? "" }; entries.Add(entry); cache[key] = entry; return true;
        }
        private static string Cell(string value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
        public string ExportCsv() => "key,kr,source\r\n" + string.Join("\r\n", entries.OrderBy(item => item.key, StringComparer.Ordinal).Select(item => Cell(item.key) + "," + Cell(item.kr) + "," + Cell(item.source))) + "\r\n";
        public void ImportCsv(string csv)
        {
            var rows = ParseCsv(csv.TrimStart('\uFEFF'));
            if (rows.Count == 0 || rows[0].Count < 2 || rows[0][0] != "key" || rows[0][1] != "kr") throw new FormatException("CSV header must be key,kr (optional: source).");
            var incoming = new List<GameTextEntry>(); var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in rows.Skip(1))
            {
                if (row.All(string.IsNullOrEmpty)) continue;
                if (row.Count < 2 || string.IsNullOrWhiteSpace(row[0]) || !keys.Add(row[0])) throw new FormatException("Missing or duplicate key: " + row.FirstOrDefault());
                // Validate composite format strings before mutating the live table.
                try { string.Format(CultureInfo.InvariantCulture, row[1], Enumerable.Repeat<object>(0, 256).ToArray()); }
                catch (FormatException exception) { throw new FormatException("Invalid placeholders: " + row[0], exception); }
                incoming.Add(new GameTextEntry { key = row[0], kr = row[1], source = row.Count > 2 ? row[2] : "" });
            }
            // A partial CSV updates matching keys; omission never silently deletes game text.
            foreach (var row in incoming) Set(row.key, row.kr, row.source);
        }
        private static List<List<string>> ParseCsv(string csv)
        {
            var rows = new List<List<string>>(); var row = new List<string>(); var cell = new StringBuilder(); var quoted = false;
            for (var i = 0; i < csv.Length; i++)
            {
                var c = csv[i];
                if (c == '"') { if (quoted && i + 1 < csv.Length && csv[i + 1] == '"') { cell.Append('"'); i++; } else if (cell.Length == 0 || quoted) quoted = !quoted; else throw new FormatException("Unexpected CSV quote."); }
                else if (!quoted && (c == ',' || c == '\n' || c == '\r'))
                {
                    row.Add(cell.ToString()); cell.Clear();
                    if (c != ',') { rows.Add(row); row = new List<string>(); if (c == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n') i++; }
                }
                else cell.Append(c);
            }
            if (quoted) throw new FormatException("Unclosed CSV quote.");
            if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); rows.Add(row); }
            return rows;
        }
    }
    public static class GameText
    {
        public static GameTextDatabase Database => database != null ? database : database = Resources.Load<GameTextDatabase>("BehemothKr");
        private static GameTextDatabase database;
        public static string Get(string key) => Database != null ? Database.Get(key) : key;
        public static string Format(string key, params object[] args) => string.Format(CultureInfo.CurrentCulture, Get(key), args);
    }
}
