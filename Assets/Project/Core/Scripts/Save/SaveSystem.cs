using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DungeonGathering.Core
{
    /// <summary>
    /// Lưu/đọc SaveData (ARCHITECTURE §9, MODULES §10.2).
    /// - Ghi .tmp → giữ .bak → thay save.json → xóa .bak (luôn còn một bản hợp lệ).
    /// - Hàng đợi: tối đa 1 lần đang ghi + 1 lần chờ (gộp yêu cầu, giữ bản mới nhất) — R9.
    /// - WebGL: thư mục IDBFS cố định + FS.syncfs qua SaveSync.jslib; flush khi pagehide/visibilitychange; khóa tab.
    ///   Khóa tab được kiểm tra lại trước mỗi lần ghi và khi tab hiện lại; mất khóa → CanWrite=false vĩnh viễn.
    /// </summary>
    public sealed class SaveSystem
    {
        const string FileName = "save.json";
        const string WebGLDir = "/idbfs/dungeon-gathering";

        public SaveData Data { get; private set; } = new();
        public bool Loaded { get; private set; }
        /// <summary>false khi game đang mở ở tab khác (WebGL) — không ghi để tránh đè.</summary>
        public bool CanWrite { get; private set; } = true;
        public bool IsWriting => writing;

        bool writing;
        bool dirty;

        public event Action Saved;

        static string Dir =>
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLDir;
#else
            Application.persistentDataPath;
#endif
        static string MainPath => Path.Combine(Dir, FileName);
        static string TmpPath => MainPath + ".tmp";
        static string BakPath => MainPath + ".bak";

        public async Awaitable LoadAsync()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CanWrite = DG_AcquireTabLock() != 0;
            if (!CanWrite) Debug.LogWarning("[Save] Game đang mở ở tab khác — tab này sẽ không ghi save.");
            await SyncFs(true);
            DG_RegisterPageHide(OnPageHide, OnPageShow);
#endif
            await Awaitable.MainThreadAsync();
            Data = ReadBest() ?? new SaveData();
            Data.Upgrade();
            Loaded = true;
        }

        static SaveData ReadBest()
        {
            try { if (!Directory.Exists(Dir)) Directory.CreateDirectory(Dir); }
            catch (Exception e) { Debug.LogWarning($"[Save] Không tạo được thư mục: {e.Message}"); }

            foreach (var path in new[] { MainPath, BakPath, TmpPath })
            {
                var data = TryRead(path);
                if (data == null) continue;
                if (path != MainPath) Debug.LogWarning($"[Save] Khôi phục từ {Path.GetFileName(path)}.");
                return data;
            }
            return null;
        }

        static SaveData TryRead(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return null;
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] File hỏng {Path.GetFileName(path)}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Kiểm tra lại khóa ghi (WebGL). Tab khác đã giành khóa → CanWrite=false và không bao giờ bật lại
        /// (dữ liệu trong bộ nhớ của tab này có thể cũ hơn save tab kia đã ghi).
        /// </summary>
        public bool RevalidateLock()
        {
            if (!CanWrite) return false;
#if UNITY_WEBGL && !UNITY_EDITOR
            if (DG_CheckTabLock() == 0)
            {
                CanWrite = false;
                dirty = false;
                Debug.LogWarning("[Save] Tab khác đã giành quyền ghi save — tab này ngừng ghi.");
                return false;
            }
#endif
            return true;
        }

        /// <summary>Yêu cầu lưu. Gộp các yêu cầu sát nhau; không bao giờ ghi chồng.</summary>
        public void RequestSave()
        {
            if (!Loaded || !CanWrite) return;
            dirty = true;
            if (!writing) _ = WriteLoop();
        }

        async Awaitable WriteLoop()
        {
            writing = true;
            try
            {
                while (dirty)
                {
                    dirty = false;
                    if (!RevalidateLock()) break;
                    string json = JsonUtility.ToJson(Data); // snapshot bản mới nhất
                    WriteAtomic(json);
#if UNITY_WEBGL && !UNITY_EDITOR
                    await SyncFs(false);
#else
                    await Awaitable.NextFrameAsync();
#endif
                    Saved?.Invoke();
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { Debug.LogError($"[Save] Lỗi ghi: {e.Message}"); }
            finally { writing = false; }
        }

        static void WriteAtomic(string json)
        {
            if (!Directory.Exists(Dir)) Directory.CreateDirectory(Dir);
            File.WriteAllText(TmpPath, json);
            if (File.Exists(MainPath))
            {
                if (File.Exists(BakPath)) File.Delete(BakPath);
                File.Move(MainPath, BakPath);
            }
            File.Move(TmpPath, MainPath);
            if (File.Exists(BakPath)) File.Delete(BakPath);
        }

        /// <summary>
        /// Ghi đồng bộ ngay (rời trang / ẩn tab). syncfs vẫn async — chấp nhận có thể không kịp.
        /// Chạy cả khi WriteLoop đang chờ syncfs: WriteAtomic là đồng bộ nên không xen giữa một lần ghi khác,
        /// và thay đổi phát sinh sau snapshot của WriteLoop (dirty) không bị mất khi đóng tab.
        /// </summary>
        public void FlushNow()
        {
            if (!Loaded || !CanWrite) return;
            if (writing && !dirty) return; // bản mới nhất đã nằm trên FS, syncfs đang chạy
            if (!RevalidateLock()) return;
            try
            {
                dirty = false;
                WriteAtomic(JsonUtility.ToJson(Data));
#if UNITY_WEBGL && !UNITY_EDITOR
                DG_SyncFs(0, 0, OnSyncDone);
#endif
            }
            catch (Exception e) { Debug.LogWarning($"[Save] Flush lỗi: {e.Message}"); }
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void DG_SyncFs(int populate, int callbackId, Action<int, int> callback);
        [DllImport("__Internal")] static extern void DG_RegisterPageHide(Action hideCallback, Action showCallback);
        [DllImport("__Internal")] static extern int DG_AcquireTabLock();
        [DllImport("__Internal")] static extern int DG_CheckTabLock();

        static int nextSyncId;
        static readonly System.Collections.Generic.Dictionary<int, AwaitableCompletionSource> syncWaiters = new();

        static Awaitable SyncFs(bool populate)
        {
            var acs = new AwaitableCompletionSource();
            int id = ++nextSyncId;
            syncWaiters[id] = acs;
            DG_SyncFs(populate ? 1 : 0, id, OnSyncDone);
            return acs.Awaitable;
        }

        [AOT.MonoPInvokeCallback(typeof(Action<int, int>))]
        static void OnSyncDone(int id, int ok)
        {
            if (ok == 0) Debug.LogWarning("[Save] syncfs báo lỗi.");
            if (syncWaiters.TryGetValue(id, out var acs))
            {
                syncWaiters.Remove(id);
                acs.TrySetResult();
            }
        }

        [AOT.MonoPInvokeCallback(typeof(Action))]
        static void OnPageHide() => Services.Save?.FlushNow();

        [AOT.MonoPInvokeCallback(typeof(Action))]
        static void OnPageShow() => Services.Save?.RevalidateLock();
#endif
    }
}
