using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace DungeonGathering.Localization
{
    /// <summary>
    /// Gắn lên TMP label: lấy chuỗi theo key trong bảng DG_Multilanguage và đổi font theo locale
    /// (port rút gọn từ LocalizedTMPText của Stickman). Có thể đặt raw text (tên người chơi, số...) bằng <see cref="SetRaw"/>.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    [DisallowMultipleComponent]
    public class LocalizedTMPText : MonoBehaviour
    {
        [SerializeField] LocalizedString localizedString = new() { TableReference = LocalizationFontConfig.TableName };
        [SerializeField] bool upperCase;

        TMP_Text text;
        TMP_FontAsset originalFont;
        Material originalMaterial;
        float originalSize;
        bool rawMode;
        string rawText;
        bool waitingInit;

        void Awake()
        {
            text = GetComponent<TMP_Text>();
            originalFont = text.font;
            originalMaterial = text.fontSharedMaterial;
            originalSize = text.fontSize;
        }

        void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            if (!rawMode && !localizedString.IsEmpty) localizedString.StringChanged += ApplyText;
            Refresh();
        }

        void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            localizedString.StringChanged -= ApplyText;
        }

        void OnLocaleChanged(Locale _)
        {
            if (this == null) return;
            // Khi có key localizedString và không ở rawMode, StringChanged sẽ tự phát và gọi ApplyText (kèm ApplyFont).
            // Chỉ đổi font trực tiếp khi rawMode hoặc chưa gán key để tránh gán text 2 lần (MODULES_ADDRESSABLES_WEBGL §9.3).
            if (rawMode || localizedString.IsEmpty) ApplyFont();
        }

        /// <summary>Đổi key lúc chạy, có thể kèm tham số Smart String ({0}, {1}...).</summary>
        public void SetKey(string key, params object[] args)
        {
            rawMode = false;
            rawText = null;
            if (isActiveAndEnabled) localizedString.StringChanged -= ApplyText;
            localizedString.SetReference(LocalizationFontConfig.TableName, key);
            localizedString.Arguments = args != null && args.Length > 0 ? args : null;
            if (isActiveAndEnabled) localizedString.StringChanged += ApplyText;
            Refresh();
        }

        public void SetArguments(params object[] args)
        {
            localizedString.Arguments = args;
            localizedString.RefreshString();
        }

        /// <summary>Text không qua key (dữ liệu động); vẫn đổi font theo ngôn ngữ.</summary>
        public void SetRaw(string value)
        {
            rawMode = true;
            rawText = value;
            localizedString.StringChanged -= ApplyText;
            ApplyText(value);
        }

        public void Refresh()
        {
            var init = LocalizationSettings.InitializationOperation;
            if (!init.IsDone)
            {
                if (!waitingInit)
                {
                    waitingInit = true;
                    init.Completed += _ =>
                    {
                        waitingInit = false;
                        if (this != null && isActiveAndEnabled) Refresh();
                    };
                }
                return;
            }
            if (rawMode) { ApplyText(rawText); return; }
            if (!localizedString.IsEmpty) localizedString.RefreshString();
            else ApplyFont();
        }

        void ApplyText(string value)
        {
            if (text == null) return;
            ApplyFont();
            text.text = upperCase && value != null ? value.ToUpperInvariant() : value ?? string.Empty;
        }

        void ApplyFont()
        {
            if (text == null) return;
            var config = LocalizationFontConfig.Instance;
            string code = GameLanguage.Current;
            TMP_FontAsset font = config != null ? config.GetFont(code) : null;
            if (font == null || font == originalFont)
            {
                if (text.font != originalFont) text.font = originalFont;
                text.fontSharedMaterial = originalMaterial;
                text.fontSize = originalSize;
                return;
            }
            if (text.font != font) text.font = font;
            text.fontSize = originalSize * config.GetSizeScale(code);
        }
    }
}
