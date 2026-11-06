using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DungeonGathering.Localization
{
    /// <summary>
    /// Font TMP theo từng locale (giống LocalizationFontManager của Stickman).
    /// EN: Pixelta · VI: dearpix (font pixel có đủ dấu tiếng Việt).
    /// Có thể gán qua <see cref="SetInstance"/> (Bootstrap/Addressables Core_Local) hoặc fallback Resources.Load.
    /// </summary>
    [CreateAssetMenu(fileName = "LocalizationFontConfig", menuName = "Dungeon Gathering/Localization Font Config")]
    public class LocalizationFontConfig : ScriptableObject
    {
        [System.Serializable]
        public class LocaleFontEntry
        {
            public string localeCode = GameLanguage.English;
            public TMP_FontAsset fontAsset;
            [Tooltip("Scale cỡ chữ cho font này (font pixel khác nhau có chiều cao khác nhau).")]
            public float sizeScale = 1f;
        }

        public const string TableName = "DG_Multilanguage";

        [SerializeField] List<LocaleFontEntry> entries = new();
        [SerializeField] TMP_FontAsset fallbackFont;

        static LocalizationFontConfig instance;
        public static LocalizationFontConfig Instance =>
            instance != null ? instance : instance = Resources.Load<LocalizationFontConfig>("LocalizationFontConfig");

        public static void SetInstance(LocalizationFontConfig config)
        {
            if (config != null) instance = config;
        }

        /// <summary>Fast Enter Play Mode: bỏ tham chiếu asset (Addressables) của phiên trước.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;

        public LocaleFontEntry GetEntry(string code)
        {
            code = GameLanguage.Normalize(code);
            for (int i = 0; i < entries.Count; i++)
                if (GameLanguage.Normalize(entries[i].localeCode) == code) return entries[i];
            return null;
        }

        /// <summary>Font cho locale; null = giữ font đang đặt trên label.</summary>
        public TMP_FontAsset GetFont(string code)
        {
            var entry = GetEntry(code);
            return entry != null && entry.fontAsset != null ? entry.fontAsset : fallbackFont;
        }

        public float GetSizeScale(string code) => GetEntry(code)?.sizeScale ?? 1f;
    }
}
