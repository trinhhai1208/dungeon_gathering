using System.IO;
using System.Linq;
using System.Text;
using DungeonGathering.Localization;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace DungeonGathering.EditorTools
{
    /// <summary>
    /// Tạo TMP font asset (Dynamic, raster pixel-perfect) cho EN = Pixelify Sans, VI = Tiny5,
    /// điền sẵn mọi ký tự có trong DG_Multilanguage.csv và gán vào LocalizationFontConfig.
    /// </summary>
    public static class LocalizationFontBuilder
    {
        const string FontsDir = "Assets/Project/Art/Fonts";
        const string ConfigPath = "Assets/Project/Localization/Resources/LocalizationFontConfig.asset";
        static string CsvPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Design", "localization", "DG_Multilanguage.csv"));

        const string Ascii = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~·×→–";

        [MenuItem("Tools/Dungeon Gathering/Localization/Build Font Assets")]
        public static void Build()
        {
            string csv = File.Exists(CsvPath) ? File.ReadAllText(CsvPath, Encoding.UTF8) : string.Empty;
            string chars = new string((Ascii + csv).Where(c => !char.IsControl(c)).Distinct().ToArray());

            var en = Create("PixelifySans-Regular", 32, chars);
            var vi = Create("Tiny5-Regular", 32, chars);
            // Pixelify Sans không có dấu tiếng Việt và vài ký hiệu (→): rơi về Tiny5.
            en.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { vi };
            EditorUtility.SetDirty(en);

            var config = AssetDatabase.LoadAssetAtPath<LocalizationFontConfig>(ConfigPath);
            if (config == null) { Debug.LogError("[Localization] Chạy Import CSV trước để có LocalizationFontConfig."); return; }
            var so = new SerializedObject(config);
            var entries = so.FindProperty("entries");
            for (int i = 0; i < entries.arraySize; i++)
            {
                var e = entries.GetArrayElementAtIndex(i);
                string code = e.FindPropertyRelative("localeCode").stringValue;
                e.FindPropertyRelative("fontAsset").objectReferenceValue = code == GameLanguage.Vietnamese ? vi : en;
            }
            so.FindProperty("fallbackFont").objectReferenceValue = en;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Localization] Font asset: {AssetDatabase.GetAssetPath(en)}, {AssetDatabase.GetAssetPath(vi)}");
        }

        static TMP_FontAsset Create(string fileName, int samplingSize, string chars)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>($"{FontsDir}/{fileName}.ttf");
            string path = $"{FontsDir}/{fileName} SDF.asset";
            var old = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (old != null) AssetDatabase.DeleteAsset(path);

            // RASTER_HINTED + Point filter: nét pixel sắc, không bị nhòe như SDF.
            var asset = TMP_FontAsset.CreateFontAsset(font, samplingSize, 2, GlyphRenderMode.RASTER_HINTED, 1024, 1024, AtlasPopulationMode.Dynamic, false);
            asset.name = $"{fileName} SDF";
            AssetDatabase.CreateAsset(asset, path);

            asset.atlasTextures[0].name = asset.name + " Atlas";
            asset.atlasTextures[0].filterMode = FilterMode.Point;
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            asset.material.name = asset.name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);

            asset.TryAddCharacters(chars, out string missing);
            asset.atlasTextures[0].filterMode = FilterMode.Point;
            if (!string.IsNullOrEmpty(missing))
                Debug.LogWarning($"[Localization] {fileName} thiếu {missing.Length} ký tự: {missing}");
            EditorUtility.SetDirty(asset);
            return asset;
        }
    }
}
