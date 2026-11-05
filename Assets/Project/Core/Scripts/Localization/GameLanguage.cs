using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace DungeonGathering.Localization
{
    /// <summary>
    /// Chọn ngôn ngữ game theo mã ngắn ("en", "vi"), cùng cách làm với Stickman (GameLanguage + Unity Localization).
    /// Nguồn sự thật duy nhất: <c>SaveData.settings.language</c> (rỗng = theo máy: Vietnamese ⇒ vi, còn lại ⇒ en).
    /// PlayerPrefs "dg-lang" chỉ còn được ĐỌC để chuyển dữ liệu cũ sang save (không ghi nữa).
    /// </summary>
    public static class GameLanguage
    {
        public const string English = "en";
        public const string Vietnamese = "vi";
        public const string Fallback = English;
        public static readonly string[] Supported = { English, Vietnamese };

        /// <summary>Key PlayerPrefs của phiên bản cũ — chỉ dùng để migrate.</summary>
        const string LegacyPrefKey = "dg-lang";

        static string pendingCode;
        static bool waitingInit;
        static int requestId;

        /// <summary>Phát ra sau khi locale đã đổi xong (mã ngắn).</summary>
        public static event Action<string> Changed;

        /// <summary>Fast Enter Play Mode (không reload domain): xóa trạng thái phiên trước (R14).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            pendingCode = null;
            waitingInit = false;
            requestId = 0;
            Changed = null;
        }

        /// <summary>Ngôn ngữ đã lưu ở PlayerPrefs bởi bản cũ (rỗng nếu không có).</summary>
        public static string LegacyPref
        {
            get
            {
                string code = PlayerPrefs.GetString(LegacyPrefKey, string.Empty);
                return string.IsNullOrEmpty(code) ? string.Empty : Normalize(code);
            }
        }

        static string SaveLanguage
        {
            get
            {
                var save = DungeonGathering.Core.Services.Save;
                return save != null && save.Loaded ? save.Data.settings.language : null;
            }
        }

        public static string Current
        {
            get
            {
                if (!string.IsNullOrEmpty(pendingCode)) return pendingCode;
                var locale = LocalizationSettings.SelectedLocale;
                return locale != null ? Normalize(locale.Identifier.Code) : Saved;
            }
        }

        public static string Saved
        {
            get
            {
                string code = SaveLanguage;
                if (!string.IsNullOrEmpty(code)) return Normalize(code);
                code = LegacyPref;
                if (!string.IsNullOrEmpty(code)) return code;
                return Application.systemLanguage == SystemLanguage.Vietnamese ? Vietnamese : English;
            }
        }

        /// <summary>Gọi một lần khi khởi động (Bootstrap) để áp ngôn ngữ đã lưu.</summary>
        public static void ApplySaved() => Apply(Saved, save: false);

        /// <summary>
        /// Đổi locale sang <paramref name="code"/>; không có locale khớp thì về <see cref="Fallback"/>.
        /// <paramref name="save"/> = ghi lựa chọn vào SaveData.settings.language (lưu có gộp).
        /// </summary>
        public static void Apply(string code, bool save = true)
        {
            code = Normalize(code);
            int myRequest = ++requestId;
            if (save)
            {
                var s = DungeonGathering.Core.Services.Save;
                if (s != null && s.Loaded && s.Data.settings.language != code)
                {
                    s.Data.settings.language = code;
                    s.RequestSave();
                }
            }

            var init = LocalizationSettings.InitializationOperation;
            if (!init.IsDone)
            {
                pendingCode = code;
                if (!waitingInit)
                {
                    waitingInit = true;
                    init.Completed += _ =>
                    {
                        waitingInit = false;
                        string target = pendingCode;
                        pendingCode = null;
                        if (!string.IsNullOrEmpty(target)) Select(target, requestId);
                    };
                }
                return;
            }

            pendingCode = null;
            Select(code, myRequest);
        }

        static void Select(string code, int reqId)
        {
            if (reqId != requestId) return;
            var locales = LocalizationSettings.AvailableLocales?.Locales;
            if (locales == null || locales.Count == 0) return;

            Locale match = Find(locales, code) ?? Find(locales, Fallback) ?? locales[0];
            if (Find(locales, code) == null)
                Debug.LogWarning($"[Localization] Không có locale cho '{code}', dùng '{match?.Identifier.Code}'.");

            if (match != null && LocalizationSettings.SelectedLocale != match)
                LocalizationSettings.SelectedLocale = match;
            Changed?.Invoke(Normalize(match?.Identifier.Code));
        }

        static Locale Find(IList<Locale> locales, string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            for (int i = 0; i < locales.Count; i++)
            {
                string id = locales[i]?.Identifier.Code;
                if (!string.IsNullOrEmpty(id) && id.StartsWith(code, StringComparison.OrdinalIgnoreCase))
                    return locales[i];
            }
            return null;
        }

        /// <summary>"vi-VN" / "VI" ⇒ "vi".</summary>
        public static string Normalize(string code)
        {
            if (string.IsNullOrEmpty(code)) return Fallback;
            code = code.Trim();
            int sep = code.IndexOfAny(new[] { '-', '_' });
            if (sep > 0) code = code.Substring(0, sep);
            return code.ToLowerInvariant();
        }
    }
}
