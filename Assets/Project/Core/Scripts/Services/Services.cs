using System;
using System.Threading;
using UnityEngine;

namespace DungeonGathering.Core
{
    /// <summary>
    /// Điểm truy cập service toàn cục (MODULES §7.1). Systems (Bootstrap) gán khi Awake.
    /// Reset hoàn toàn ở SubsystemRegistration để chạy đúng với Fast Enter Play Mode (R14).
    /// </summary>
    public static class Services
    {
        public static IAssetService Assets { get; internal set; }
        public static ISceneService Scenes { get; internal set; }
        public static IUIService UI { get; internal set; }
        public static IDownloadService Downloads { get; internal set; }

        // Bổ sung so với hợp đồng gốc (được phép thêm member).
        public static PoolManager Pool { get; internal set; }
        public static SaveSystem Save { get; internal set; }
        public static SessionData Session { get; internal set; }
        public static GameManager Game { get; internal set; }
        public static AudioManager Audio { get; internal set; }
        public static ModuleRegistry Modules { get; internal set; }

        /// <summary>true sau khi mọi service đã gán VÀ Addressables + Save + Localization khởi tạo xong.</summary>
        public static bool Ready { get; private set; }

        /// <summary>Phát một lần khi Ready chuyển true. Đăng ký muộn hơn thì kiểm tra Ready trước (hoặc dùng WhenReady).</summary>
        public static event Action BecameReady;

        static CancellationTokenSource appCts = new();
        /// <summary>Bị Cancel khi thoát game / thoát Play Mode.</summary>
        public static CancellationToken AppToken => appCts.Token;

        internal static void MarkReady()
        {
            if (Ready) return;
            Ready = true;
            var h = BecameReady;
            BecameReady = null;
            try { h?.Invoke(); } catch (Exception e) { Debug.LogException(e); }
        }

        /// <summary>Gọi cb ngay nếu đã Ready, ngược lại khi Ready.</summary>
        public static void WhenReady(Action cb)
        {
            if (cb == null) return;
            if (Ready) cb();
            else BecameReady += cb;
        }

        internal static void CancelApp()
        {
            if (!appCts.IsCancellationRequested) appCts.Cancel();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            CancelApp();
            appCts.Dispose();
            appCts = new CancellationTokenSource();
            Assets = null; Scenes = null; UI = null; Downloads = null;
            Pool = null; Save = null; Session = null; Game = null; Audio = null; Modules = null;
            Ready = false;
            BecameReady = null;
            Application.quitting -= CancelApp;
            Application.quitting += CancelApp;
        }
    }
}
