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
        /// <summary>Editor bật "mở full" → dùng file save riêng, không đụng save thật.</summary>
        static string FileName => EditorUnlockAll.Active ? EditorUnlockAll.SaveFileName : "save.json";
        const string WebGLDir = "/idbfs/dungeon-gathering";
        /// <summary>Thời gian tối đa chờ callback FS.syncfs (WebGL).</summary>
        const float SyncTimeoutSeconds = 5f;

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
            if (DG_AcquireTabLock() == 0)
                LoseWrite(false, "[Save] Game đang mở ở tab khác — tab này sẽ không ghi save.");
            await SyncFs(true);
            DG_RegisterPageHide(OnPageHide, OnPageShow);
#endif
            await Awaitable.MainThreadAsync();
            Data = ReadBest() ?? new SaveData();
            Data.Upgrade();
            if (Data.FromNewerVersion)
                LoseWrite(true, "[Save] Save của bản game mới hơn — tab này chỉ đọc, không ghi đè.");
            Loaded = true;
            EditorUnlockAll.OnSaveLoaded(Data);
        }

        /// <summary>Chuyển sang CanWrite=false (vĩnh viễn trong phiên) và phát SaveLockLostEvent một lần.</summary>
        void LoseWrite(bool newerVersion, string reason)
        {
            if (!CanWrite && !newerVersion) return;
            CanWrite = false;
            dirty = false;
            Debug.LogWarning(reason);
            EventBus<SaveLockLostEvent>.Raise(new SaveLockLostEvent { NewerSaveVersion = newerVersion });
        }

        static SaveData ReadBest()
        {
            try { if (!Directory.Exists(Dir)) Directory.CreateDirectory(Dir); }
            catch (Exception e) { Debug.LogWarning($"[Save] Không tạo được thư mục: {e.Message}"); }

            // Thứ tự ghi: tmp (đủ) → main→bak → tmp→main. Mất main nghĩa là chết giữa hai lần Move:
            // tmp đã ghi đủ và mới hơn bak, nên đọc tmp trước bak.
            foreach (var path in new[] { MainPath, TmpPath, BakPath })
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
                LoseWrite(false, "[Save] Tab khác đã giành quyền ghi save — tab này ngừng ghi.");
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
        static readonly System.Collections.Generic.HashSet<int> pendingSyncs = new();

        /// <summary>
        /// FS.syncfs + chờ callback, tối đa <see cref="SyncTimeoutSeconds"/> (đếm realtime) để LoadAsync/WriteLoop
        /// không treo vĩnh viễn nếu IndexedDB không trả lời. Hết hạn → bỏ qua (callback muộn bị phớt lờ).
        /// </summary>
        static async Awaitable SyncFs(bool populate)
        {
            int id = ++nextSyncId;
            pendingSyncs.Add(id);
            DG_SyncFs(populate ? 1 : 0, id, OnSyncDone);
            double deadline = Time.realtimeSinceStartupAsDouble + SyncTimeoutSeconds;
            while (pendingSyncs.Contains(id))
            {
                if (Time.realtimeSinceStartupAsDouble >= deadline)
                {
                    pendingSyncs.Remove(id);
                    Debug.LogWarning($"[Save] syncfs quá {SyncTimeoutSeconds:0}s không phản hồi — bỏ qua.");
                    return;
                }
                await Awaitable.NextFrameAsync();
            }
        }

        [AOT.MonoPInvokeCallback(typeof(Action<int, int>))]
        static void OnSyncDone(int id, int ok)
        {
            if (ok == 0) Debug.LogWarning("[Save] syncfs báo lỗi.");
            pendingSyncs.Remove(id);
        }

        [AOT.MonoPInvokeCallback(typeof(Action))]
        static void OnPageHide()
        {
            // WebGL không gọi OnApplicationQuit khi đóng tab: chốt thưởng nếu tầng đã thắng rồi mới flush.
            Systems.SettleWonRunOnPageHide();
            Services.Save?.FlushNow();
        }

        [AOT.MonoPInvokeCallback(typeof(Action))]
        static void OnPageShow() => Services.Save?.RevalidateLock();
#endif
    }
}
