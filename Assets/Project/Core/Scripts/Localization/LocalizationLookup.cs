using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace DungeonGathering.Localization
{
    /// <summary>Lấy chuỗi đã dịch theo key khi chỗ gọi không gắn <see cref="LocalizedTMPText"/> (vd: text tạo trong code).</summary>
    public static class LocalizationLookup
    {
        /// <summary>
        /// Chuỗi đã dịch, hoặc <paramref name="fallback"/> (mặc định: chính key) khi chưa init xong, bảng chưa tải
        /// hoặc thiếu key/bản dịch. Không bao giờ chờ đồng bộ (WaitForCompletion không chạy được trên WebGL):
        /// bảng chưa sẵn sàng thì trả fallback và bắt đầu tải bảng cho lần gọi sau.
        /// </summary>
        public static string Get(string key, string fallback = null, params object[] args)
        {
            if (string.IsNullOrEmpty(key)) return fallback ?? string.Empty;
            try
            {
                if (!LocalizationSettings.InitializationOperation.IsDone) return fallback ?? key;
                var op = LocalizationSettings.StringDatabase.GetTableAsync(LocalizationFontConfig.TableName);
                if (!op.IsDone || op.Status != AsyncOperationStatus.Succeeded || op.Result == null) return fallback ?? key;

                // GetEntry trả null khi key không có trong bảng → dùng fallback thay vì chuỗi "No translation found".
                var entry = op.Result.GetEntry(key);
                if (entry == null || string.IsNullOrEmpty(entry.Value)) return fallback ?? key;
                string value = args != null && args.Length > 0 ? entry.GetLocalizedString(args) : entry.GetLocalizedString();
                return string.IsNullOrEmpty(value) ? fallback ?? key : value;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Localization] Không lấy được key '{key}': {e.Message}");
                return fallback ?? key;
            }
        }
    }
}
