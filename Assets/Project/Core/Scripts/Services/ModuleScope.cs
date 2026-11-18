using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DungeonGathering.Core
{
    public interface IAssetService
    {
        /// <summary>Scope gốc sống suốt game (Core, Shared). Không release tự động.</summary>
        ModuleScope Global { get; }
        /// <summary>Tạo scope mới cho module/scene/screen; Dispose() = Release mọi handle bên trong.</summary>
        ModuleScope CreateScope(string moduleId);
    }

    /// <summary>
    /// Giữ handle Addressables của một module (MODULES §7.2). Quy tắc async:
    /// cache HANDLE (kể cả đang tải — R3), không cache Awaitable; sau Dispose mọi Load ném ObjectDisposedException,
    /// handle xong muộn được Release ngay và Awaitable ném OperationCanceledException (R4).
    /// </summary>
    public sealed class ModuleScope : IDisposable
    {
        static int nextInstanceId;

        readonly Dictionary<string, AsyncOperationHandle> handles = new();
        readonly List<AsyncOperationHandle> retained = new();
        // Lỗi của handle đã Release (để các Load khác đang chờ cùng handle báo đúng lỗi). Nhỏ: chỉ handle lỗi.
        readonly Dictionary<AsyncOperationHandle, string> failures = new();
        readonly CancellationTokenSource cts = new();

        public string ModuleId { get; }
        /// <summary>Tăng dần mỗi lần tạo scope; dùng làm owner (PoolManager, UIService) — R2.</summary>
        public int InstanceId { get; }
        public bool IsDisposed { get; private set; }
        public CancellationToken Token { get; }
        /// <summary>Số handle đang giữ (test rò handle).</summary>
        public int HandleCount => handles.Count + retained.Count;
        /// <summary>Scene của module (nếu có). Instantiate không parent sẽ đặt object vào scene này.</summary>
        public Scene Scene { get; internal set; }

        public event Action<ModuleScope> Disposed;

        internal ModuleScope(string moduleId)
        {
            ModuleId = moduleId;
            InstanceId = Interlocked.Increment(ref nextInstanceId);
            Token = cts.Token;
        }

        public override string ToString() => $"Scope({ModuleId}#{InstanceId})";

        public Awaitable<T> Load<T>(string address) where T : Object
        {
            ThrowIfDisposed();
            if (string.IsNullOrEmpty(address)) throw new ArgumentException("address rỗng");
            string key = typeof(T).FullName + "|" + address;
            if (!handles.TryGetValue(key, out var handle))
            {
                handle = Addressables.LoadAssetAsync<T>(address);
                handles[key] = handle;
            }
            return Await<T>(key, handle);
        }

        public Awaitable<T> Load<T>(AssetReferenceT<T> reference) where T : Object
        {
            ThrowIfDisposed();
            if (reference == null || !reference.RuntimeKeyIsValid())
                throw new ArgumentException("AssetReference không hợp lệ");
            string key = typeof(T).FullName + "|ref:" + reference.RuntimeKey;
            if (!handles.TryGetValue(key, out var handle))
            {
                handle = Addressables.LoadAssetAsync<T>(reference.RuntimeKey);
                handles[key] = handle;
            }
            return Await<T>(key, handle);
        }

        public async Awaitable<IList<T>> LoadByLabel<T>(string label) where T : Object
        {
            ThrowIfDisposed();
            string key = typeof(T).FullName + "|label:" + label;
            if (!handles.TryGetValue(key, out var handle))
            {
                handle = Addressables.LoadAssetsAsync<T>(label, null);
                handles[key] = handle;
            }
            await WaitDone(key, handle);
            return (IList<T>)handle.Result;
        }

        /// <summary>Load prefab một lần + Object.Instantiate. Trả null nếu parent bị hủy trong lúc chờ.</summary>
        public async Awaitable<GameObject> Instantiate(string address, Transform parent = null)
        {
            bool hadParent = parent != null;
            var prefab = await Load<GameObject>(address);
            if (IsDisposed) throw new OperationCanceledException(ToString());
            if (hadParent && parent == null) return null;
            var go = Object.Instantiate(prefab, parent, false);
            if (!hadParent && Scene.IsValid() && Scene.isLoaded && go.scene != Scene)
                SceneManager.MoveGameObjectToScene(go, Scene);
            return go;
        }

        async Awaitable<T> Await<T>(string key, AsyncOperationHandle handle) where T : Object
        {
            await WaitDone(key, handle);
            return handle.Result as T;
        }

        async Awaitable WaitDone(string key, AsyncOperationHandle handle)
        {
            if (handle.IsValid() && !handle.IsDone)
            {
                var acs = new AwaitableCompletionSource();
                handle.Completed += _ => acs.TrySetResult();
                if (handle.IsDone) acs.TrySetResult();
                await acs.Awaitable;
            }
            if (IsDisposed) throw new OperationCanceledException(ToString());
            // Nhiều Load cùng chờ một handle lỗi: người đầu tiên đã Release → người sau đọc lỗi đã ghi lại
            // thay vì chạm vào handle không hợp lệ.
            if (!handle.IsValid())
            {
                failures.TryGetValue(handle, out var prevError);
                throw new Exception($"[Assets] Tải '{key}' thất bại: {prevError ?? "handle đã bị giải phóng"}");
            }
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                string error = handle.OperationException?.Message;
                // Bỏ handle lỗi khỏi cache để lần sau thử lại được.
                if (handles.TryGetValue(key, out var cached) && cached.Equals(handle))
                {
                    handles.Remove(key);
                    failures[handle] = error;
                    Addressables.Release(handle);
                }
                throw new Exception($"[Assets] Tải '{key}' thất bại: {error}");
            }
        }

        void ThrowIfDisposed()
        {
            if (IsDisposed) throw new ObjectDisposedException(ToString());
        }

        /// <summary>Nhận quyền sở hữu một handle (ví dụ từ DownloadService) và tự Release khi scope Dispose.</summary>
        public void Retain(AsyncOperationHandle handle)
        {
            if (!handle.IsValid()) return;
            if (IsDisposed) { Addressables.Release(handle); return; }
            retained.Add(handle);
        }

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            cts.Cancel();
            foreach (var h in handles.Values)
            {
                if (!h.IsValid()) continue;
                if (h.IsDone) Addressables.Release(h);
                else h.Completed += op => { if (op.IsValid()) Addressables.Release(op); };
            }
            handles.Clear();
            for (int i = retained.Count - 1; i >= 0; i--)
            {
                var h = retained[i];
                if (!h.IsValid()) continue;
                if (h.IsDone) Addressables.Release(h);
                else h.Completed += op => { if (op.IsValid()) Addressables.Release(op); };
            }
            retained.Clear();
            var d = Disposed;
            Disposed = null;
            try { d?.Invoke(this); } catch (Exception e) { Debug.LogException(e); }
        }
    }

    public sealed class AssetService : IAssetService
    {
        public ModuleScope Global { get; } = new("global");
        public ModuleScope CreateScope(string moduleId) => new(moduleId);
    }

    public static class AddressablesUtil
    {
        /// <summary>Key (address/label) có trong catalog? Chỉ đúng sau Addressables.InitializeAsync.</summary>
        public static bool KeyExists(object key) => KeyExists(key, null);

        /// <summary>Key (address/label) có trong catalog và gán được sang kiểu <typeparamref name="T"/>?</summary>
        public static bool KeyExists<T>(object key) => KeyExists(key, typeof(T));

        public static bool KeyExists(object key, Type type)
        {
            if (key == null) return false;
            foreach (var locator in Addressables.ResourceLocators)
                if (locator.Locate(key, type, out var locs) && locs != null && locs.Count > 0) return true;
            return false;
        }

        /// <summary>Chờ handle xong (không ném lỗi; tự kiểm tra Status sau đó).</summary>
        public static async Awaitable Wait(AsyncOperationHandle handle, CancellationToken token = default)
        {
            while (handle.IsValid() && !handle.IsDone)
                await Awaitable.NextFrameAsync(token);
        }
    }
}
