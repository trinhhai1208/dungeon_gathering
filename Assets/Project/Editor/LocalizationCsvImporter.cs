using System.Collections.Generic;
using System.IO;
using System.Text;
using DungeonGathering.Localization;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace DungeonGathering.EditorTools
{
    /// <summary>
    /// Nhập Design/localization/DG_Multilanguage.csv (Key,en,vi) vào String Table Collection "DG_Multilanguage".
    /// Tạo LocalizationSettings, 2 locale en/vi, bảng và LocalizationFontConfig nếu chưa có. Chạy lại để cập nhật.
    /// CSV được sinh từ Design/localization/strings_src.py — cùng bộ key với bản thiết kế UI.
    /// </summary>
    public static class LocalizationCsvImporter
    {
        const string Root = "Assets/Project/Localization";
        const string LocalesDir = Root + "/Locales";
        const string TablesDir = Root + "/Tables";
        const string FontConfigPath = "Assets/Project/Core/Data/LocalizationFontConfig.asset";
        static string CsvPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Design", "localization", "DG_Multilanguage.csv"));

        [MenuItem("Tools/Dungeon Gathering/Localization/Import CSV")]
        public static void Import()
        {
            if (!File.Exists(CsvPath)) { Debug.LogError($"[Localization] Không thấy {CsvPath}"); return; }
            Directory.CreateDirectory(LocalesDir);
            Directory.CreateDirectory(TablesDir);
            Directory.CreateDirectory("Assets/Project/Core/Data");

            EnsureSettings();
            var locales = new Dictionary<string, Locale>
            {
                [GameLanguage.English] = EnsureLocale(GameLanguage.English, "English"),
                [GameLanguage.Vietnamese] = EnsureLocale(GameLanguage.Vietnamese, "Vietnamese"),
            };

            var collection = LocalizationEditorSettings.GetStringTableCollection(LocalizationFontConfig.TableName)
                             ?? LocalizationEditorSettings.CreateStringTableCollection(LocalizationFontConfig.TableName, TablesDir);
            foreach (var locale in locales.Values)
                if (collection.GetTable(locale.Identifier) == null) collection.AddNewTable(locale.Identifier);

            List<string[]> rows = ParseCsv(File.ReadAllText(CsvPath, Encoding.UTF8));
            string[] header = rows[0];
            int added = 0, updated = 0;
            for (int r = 1; r < rows.Count; r++)
            {
                string[] row = rows[r];
                if (row.Length == 0 || string.IsNullOrWhiteSpace(row[0])) continue;
                string key = row[0].Trim();
                if (collection.SharedData.GetEntry(key) == null) { collection.SharedData.AddKey(key); added++; } else updated++;
                for (int c = 1; c < header.Length && c < row.Length; c++)
                {
                    if (!locales.TryGetValue(header[c].Trim(), out var locale)) continue;
                    var table = (StringTable)collection.GetTable(locale.Identifier);
                    table.AddEntry(key, row[c]);
                    EditorUtility.SetDirty(table);
                }
            }
            EditorUtility.SetDirty(collection.SharedData);
            EditorUtility.SetDirty(collection);

            EnsureFontConfig();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Localization] Import xong: {added} key mới, {updated} key cập nhật, {locales.Count} ngôn ngữ.");
        }

        static void EnsureSettings()
        {
            if (LocalizationEditorSettings.ActiveLocalizationSettings != null) return;
            var settings = ScriptableObject.CreateInstance<LocalizationSettings>();
            AssetDatabase.CreateAsset(settings, Root + "/LocalizationSettings.asset");
            LocalizationEditorSettings.ActiveLocalizationSettings = settings;
        }

        static Locale EnsureLocale(string code, string displayName)
        {
            foreach (var l in LocalizationEditorSettings.GetLocales())
                if (GameLanguage.Normalize(l.Identifier.Code) == code) return l;
            var locale = Locale.CreateLocale(new LocaleIdentifier(code));
            locale.name = $"{displayName} ({code})";
            AssetDatabase.CreateAsset(locale, $"{LocalesDir}/{locale.name}.asset");
            LocalizationEditorSettings.AddLocale(locale);
            return locale;
        }

        static void EnsureFontConfig()
        {
            string path = FontConfigPath;
            if (AssetDatabase.LoadAssetAtPath<LocalizationFontConfig>(path) != null) return;
            var config = ScriptableObject.CreateInstance<LocalizationFontConfig>();
            var so = new SerializedObject(config);
            var entries = so.FindProperty("entries");
            entries.arraySize = 2;
            entries.GetArrayElementAtIndex(0).FindPropertyRelative("localeCode").stringValue = GameLanguage.English;
            entries.GetArrayElementAtIndex(1).FindPropertyRelative("localeCode").stringValue = GameLanguage.Vietnamese;
            entries.GetArrayElementAtIndex(0).FindPropertyRelative("sizeScale").floatValue = 1f;
            entries.GetArrayElementAtIndex(1).FindPropertyRelative("sizeScale").floatValue = 1f;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(config, path);
        }

        /// <summary>CSV RFC 4180 tối giản: hỗ trợ ô có dấu phẩy, xuống dòng và "" bên trong ngoặc kép.</summary>
        static List<string[]> ParseCsv(string text)
        {
            var rows = new List<string[]>();
            var row = new List<string>();
            var cell = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (quoted)
                {
                    if (ch == '"' && i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                    else if (ch == '"') quoted = false;
                    else cell.Append(ch);
                    continue;
                }
                switch (ch)
                {
                    case '"': quoted = true; break;
                    case ',': row.Add(cell.ToString()); cell.Clear(); break;
                    case '\r': break;
                    case '\n': row.Add(cell.ToString()); cell.Clear(); rows.Add(row.ToArray()); row.Clear(); break;
                    default: cell.Append(ch); break;
                }
            }
            if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); rows.Add(row.ToArray()); }
            return rows;
        }
    }
}
