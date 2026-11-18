using System;
using UnityEngine;

namespace DungeonGathering.Core
{
    /// <summary>
    /// Chế độ "mở full" để test trong Unity Editor: mọi hero mở khóa, mọi vũ khí / trang bị đã sở hữu, nhiều coin.
    /// Bật / tắt: <c>Tools ▸ Dungeon Gathering ▸ Debug ▸ Mở full hero + vũ khí (Editor)</c>, có hiệu lực từ lần Play sau.
    /// <list type="bullet">
    /// <item>Chỉ chạy trong Editor — bản build (WebGL) luôn tắt, không có code nào trong build đọc cờ này.</item>
    /// <item>Dùng file save riêng (<see cref="SaveFileName"/>) nên save thật không bị ghi đè; tiến trình test (loadout,
    /// coin…) vẫn được lưu trong file riêng đó giữa các lần Play.</item>
    /// <item>Core không biết danh sách hero / vũ khí: ItemCatalog đăng ký <see cref="Granter"/> khi nạp; việc mở khóa chạy
    /// khi cả save lẫn catalog đã sẵn sàng (thứ tự nạp nào cũng được).</item>
    /// </list>
    /// </summary>
    public static class EditorUnlockAll
    {
        public const string PrefKey = "DungeonGathering.EditorUnlockAll";
        public const string SaveFileName = "save_editor_unlockall.json";
        /// <summary>Coin tối thiểu khi bật chế độ (đủ mua mọi thứ trong shop).</summary>
        public const int Coins = 999_999;

        /// <summary>true chỉ khi đang ở Editor và đã bật trong menu.</summary>
        public static bool Active
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.EditorPrefs.GetBool(PrefKey, false);
#else
                return false;
#endif
            }
        }

        static Action<SaveData> granter;
        static SaveData loaded;

        /// <summary>Catalog đăng ký hàm mở khóa (biết danh sách hero / vũ khí / trang bị).</summary>
        public static Action<SaveData> Granter
        {
            set { granter = value; TryGrant(); }
        }

        /// <summary>SaveSystem gọi sau khi tải save.</summary>
        public static void OnSaveLoaded(SaveData data)
        {
            loaded = data;
            TryGrant();
        }

        static void TryGrant()
        {
            if (!Active || granter == null || loaded == null) return;
            granter(loaded);
            if (loaded.coins < Coins) loaded.coins = Coins;
            EventBus<WalletChangedEvent>.Raise(new WalletChangedEvent { Coins = loaded.coins });
            Debug.LogWarning($"[Editor] Chế độ MỞ FULL đang bật — mọi hero / vũ khí / trang bị đã mở, save riêng '{SaveFileName}'. " +
                             "Tắt ở Tools ▸ Dungeon Gathering ▸ Debug.");
            Services.Save?.RequestSave();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() { granter = null; loaded = null; }
    }
}
