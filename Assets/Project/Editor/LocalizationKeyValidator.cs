using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;

namespace DungeonGathering.EditorTools
{
    /// <summary>
    /// Đối chiếu key đa ngôn ngữ đang được dùng (prefab, scene, field *Key, code gọi Get/SetKey) với bảng
    /// DG_Multilanguage: báo key thiếu trong bảng và key thiếu bản dịch. Chạy lại sau mỗi lần thêm UI/nội dung.
    /// </summary>
    public static class LocalizationKeyValidator
    {
        const string Root = "Assets/Project";
        const string TableName = "DG_Multilanguage";

        static readonly Regex PrefabKeyRef = new(@"m_TableEntryReference:\s*\n\s+m_KeyId: (\d+)\s*\n\s+m_Key: ?(.*)");
        static readonly Regex PrefabKeyField = new(@"\n\s+\w*[kK]ey\w*: ([a-z][a-z0-9_]*\.[a-z0-9_.]+)\s*\n");
        static readonly Regex CodeKeyLiteral = new("\"([a-z][a-z0-9_]*\\.[a-z0-9_.]+)\"");
        static readonly Regex CodeKeyContext = new(@"(SetKey|LocalizationLookup|[kK]ey\b)");
        static readonly string[] IgnorePrefixes = { "ui/", "scene/", "audio/", "data/", "theme/", "enemy/", "boss/", "vfx/" };

        [MenuItem("Tools/Dungeon Gathering/Localization/Validate Keys")]
        public static void ValidateMenu()
        {
            var report = Run(out int problems);
            if (problems == 0) Debug.Log(report);
            else Debug.LogWarning(report);
        }

        /// <summary>Trả về báo cáo dạng text; <paramref name="problems"/> = số key thiếu + số bản dịch thiếu.</summary>
        public static string Run(out int problems)
        {
            problems = 0;
            var collection = LocalizationEditorSettings.GetStringTableCollection(TableName);
            if (collection == null) return $"[Localization] Không tìm thấy String Table Collection '{TableName}'.";

            var shared = collection.SharedData;
            var keys = new HashSet<string>(shared.Entries.Select(e => e.Key));
            var idToKey = shared.Entries.ToDictionary(e => e.Id, e => e.Key);

            var used = new Dictionary<string, HashSet<string>>();
            void Use(string key, string where)
            {
                if (string.IsNullOrEmpty(key) || IgnorePrefixes.Any(key.StartsWith)) return;
                if (!used.TryGetValue(key, out var set)) used[key] = set = new HashSet<string>();
                set.Add(where);
            }

            foreach (var path in Directory.GetFiles(Root, "*.prefab", SearchOption.AllDirectories)
                         .Concat(Directory.GetFiles(Root + "/Scenes", "*.unity"))
                         .Concat(Directory.GetFiles(Root + "/Data", "*.asset", SearchOption.AllDirectories)))
            {
                string text = File.ReadAllText(path);
                string rel = path.Replace('\\', '/');
                foreach (Match m in PrefabKeyRef.Matches(text))
                {
                    string k = m.Groups[2].Value.Trim();
                    if (k.Length > 0) Use(k, rel);
                    else if (long.TryParse(m.Groups[1].Value, out long id) && id != 0)
                        Use(idToKey.TryGetValue(id, out var byId) ? byId : $"<id:{id}>", rel);
                }
                foreach (Match m in PrefabKeyField.Matches(text)) Use(m.Groups[1].Value, rel);
            }

            foreach (var path in Directory.GetFiles(Root + "/Scripts", "*.cs", SearchOption.AllDirectories)
                         .Concat(Directory.GetFiles(Root + "/Core", "*.cs", SearchOption.AllDirectories))
                         .Concat(Directory.GetFiles(Root + "/Shared", "*.cs", SearchOption.AllDirectories)))
            {
                var lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (!CodeKeyContext.IsMatch(lines[i])) continue;
                    foreach (Match m in CodeKeyLiteral.Matches(lines[i]))
                    {
                        string k = m.Groups[1].Value;
                        if (k.EndsWith(".cs") || k.EndsWith(".asset") || k.EndsWith(".prefab") || k.EndsWith(".json")) continue;
                        Use(k, $"{path.Replace('\\', '/')}:{i + 1}");
                    }
                }
            }

            var sb = new StringBuilder();
            var missing = used.Where(kv => !keys.Contains(kv.Key)).OrderBy(kv => kv.Key).ToList();
            sb.AppendLine($"[Localization] Bảng {TableName}: {keys.Count} key · đang dùng {used.Count} key.");
            sb.AppendLine($"Key dùng nhưng KHÔNG có trong bảng: {missing.Count}");
            foreach (var kv in missing) sb.AppendLine($"  {kv.Key}  ←  {string.Join(", ", kv.Value.Take(3))}");

            int untranslated = 0;
            foreach (var table in collection.StringTables)
            {
                foreach (var entry in shared.Entries)
                {
                    var e = table.GetEntry(entry.Id);
                    if (e != null && !string.IsNullOrEmpty(e.Value)) continue;
                    if (untranslated == 0) sb.AppendLine("Key thiếu bản dịch:");
                    untranslated++;
                    sb.AppendLine($"  {entry.Key} [{table.LocaleIdentifier.Code}]");
                }
            }
            if (untranslated == 0) sb.AppendLine("Key thiếu bản dịch: 0");
            sb.AppendLine("Nguồn bảng: Design/localization/DG_Multilanguage.csv → Localization/Import CSV. " +
                          "Không chạy strings_src.py (bản cũ, thiếu ~60 key so với CSV).");

            problems = missing.Count + untranslated;
            return sb.ToString();
        }
    }
}
