using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DungeonGathering.Localization
{
    /// <summary>
    /// Font TMP theo từng locale (giống LocalizationFontManager của Stickman).
    /// EN: Pixelta · VI: dearpix (font pixel có đủ dấu tiếng Việt).
    /// Runtime nạp qua <see cref="SetInstance"/> (Bootstrap / Addressables Core_Local); Editor fallback từ Core/Data.
    /// </summary>
    [CreateAssetMenu(fileName = "LocalizationFontConfig", menuName = "Dungeon Gathering/Localization Font Config")]
    public class LocalizationFontConfig : ScriptableObject
    {
        [System.Serializable]
        public class LocaleFontEntry
        {
            [Tooltip("Mã ngôn ngữ (en, vi), so khớp với ngôn ngữ hiện tại sau khi chuẩn hóa.")]
            public string localeCode = GameLanguage.English;
            [Tooltip("Font TMP cho locale này; null = dùng fallbackFont.")]
            public TMP_FontAsset fontAsset;
            [Tooltip("Scale cỡ chữ cho font này (font pixel khác nhau có chiều cao khác nhau).")]
            public float sizeScale = 1f;
        }

        public const string TableName = "DG_Multilanguage";

        [Tooltip("Font TMP theo từng locale; locale không có entry (hoặc thiếu font) sẽ dùng fallbackFont.")]
        [SerializeField] List<LocaleFontEntry> entries = new();
        [Tooltip("Font dùng khi locale không có entry hay entry thiếu font; null = giữ font gốc của label.")]
        [SerializeField] TMP_FontAsset fallbackFont;

        static LocalizationFontConfig instance;
        public static event System.Action InstanceChanged;

        public static LocalizationFontConfig Instance
        {
            get
            {
                if (instance != null) return instance;
#if UNITY_EDITOR
                instance = UnityEditor.AssetDatabase.LoadAssetAtPath<LocalizationFontConfig>(
                    "Assets/Project/Core/Data/LocalizationFontConfig.asset");
#endif
                return instance;
            }
        }

        public static void SetInstance(LocalizationFontConfig config)
        {
            if (config == null) return;
            instance = config;
            InstanceChanged?.Invoke();
        }

        /// <summary>Fast Enter Play Mode: bỏ tham chiếu asset (Addressables) của phiên trước.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null;
            InstanceChanged = null;
        }

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
