using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace DungeonGathering.Core
{
    public interface IDownloadService
    {
        Awaitable<long> GetSize(IEnumerable<string> labels);
        Awaitable Ensure(IEnumerable<string> labels, IProgress<float> progress);
        Awaitable Prefetch(IEnumerable<string> labels);
        void Adopt(IEnumerable<string> labels, ModuleScope scope);
        void ReleaseUnadopted(IEnumerable<string> keep);
    }

    /// <summary>
    /// Tải phụ thuộc theo label (MODULES §7.4). Ensure gặp label đang Prefetch thì gộp vào thao tác đó.
    /// Handle DownloadDependenciesAsync được giữ lại cho tới khi Adopt vào ModuleScope hoặc ReleaseUnadopted.
    /// Label chưa tồn tại trong catalog (chưa làm M2) bị bỏ qua kèm cảnh báo thay vì ném lỗi.
    /// </summary>
    public sealed class DownloadService : IDownloadService
    {
        /// <summary>
        /// Một lần tải label, có thể có nhiều người chờ (Prefetch + Ensure). Người chờ đầu tiên thấy handle xong
        /// sẽ chốt kết quả vào Job; handle thành công được giữ trong retained để Adopt vào ModuleScope.
        /// </summary>
        sealed class Job
        {
            public AsyncOperationHandle Handle;
            public bool Finished;
            public bool Succeeded;
            public string Error;
        }

        readonly Dictionary<string, Job> running = new();
        readonly Dictionary<string, AsyncOperationHandle> retained = new();
        // Handle đã giao cho (ít nhất một) ModuleScope — để Acquire thêm khi module mới cần cùng label.
        readonly Dictionary<string, AsyncOperationHandle> adopted = new();
        readonly HashSet<string> done = new();
        readonly HashSet<string> warnedMissing = new();

        List<string> Filter(IEnumerable<string> labels)
        {
            var list = new List<string>();
            if (labels == null) return list;
            foreach (var l in labels)
            {
                if (string.IsNullOrEmpty(l) || list.Contains(l)) continue;
                if (!AddressablesUtil.KeyExists(l))
                {
                    if (warnedMissing.Add(l)) Debug.LogWarning($"[Download] Label '{l}' chưa có trong Addressables — bỏ qua.");
                    continue;
                }
                list.Add(l);
            }
            return list;
        }

        public async Awaitable<long> GetSize(IEnumerable<string> labels)
        {
            var keys = Filter(labels);
            if (keys.Count == 0) return 0;
            var h = Addressables.GetDownloadSizeAsync((IEnumerable<object>)keys.ConvertAll(k => (object)k));
            await AddressablesUtil.Wait(h, Services.AppToken);
            long size = h.Status == AsyncOperationStatus.Succeeded ? h.Result : 0;
            Addressables.Release(h);
            return size;
        }

        Job Start(string label)
        {
            if (running.TryGetValue(label, out var job) && !job.Finished) return job;
            job = new Job { Handle = Addressables.DownloadDependenciesAsync(label, false) };
            running[label] = job;
            return job;
        }

        static bool IsDone(Job job) => job.Finished || !job.Handle.IsValid() || job.Handle.IsDone;

        /// <summary>Chốt kết quả job đúng một lần; handle thành công được giữ trong retained cho tới khi Adopt/ReleaseUnadopted.</summary>
        void Complete(string label, Job job)
        {
            if (job.Finished) return;
            job.Finished = true;
            if (job.Handle.IsValid())
            {
                job.Succeeded = job.Handle.Status == AsyncOperationStatus.Succeeded;
                if (job.Succeeded)
                {
                    if (retained.TryGetValue(label, out var prev) && prev.IsValid())
                        Addressables.Release(prev);
                    retained[label] = job.Handle;
                }
                else
                {
                    job.Error = job.Handle.OperationException?.Message;
                    Addressables.Release(job.Handle);
                }
            }
            else
            {
                job.Succeeded = false;
                job.Error = "handle không hợp lệ";
            }
            job.Handle = default;
            if (running.TryGetValue(label, out var cur) && cur == job) running.Remove(label);
        }

        /// <summary>
        /// Giao handle của label cho scope. Label đã nằm trong scope khác (vd chuyển Play → Play: Ensure bỏ qua vì đã
        /// <c>done</c>, retained trống) → Acquire thêm một tham chiếu trên cùng handle để scope mới cũng giữ bundle;
        /// scope cũ Dispose chỉ trừ refcount, bundle chỉ bị unload khi không còn scope nào giữ.
        /// </summary>
        public void Adopt(IEnumerable<string> labels, ModuleScope scope)
        {
            if (labels == null || scope == null) return;
            foreach (var raw in labels)
            {
                if (string.IsNullOrEmpty(raw)) continue;
                string label = raw;
                AsyncOperationHandle h;
                if (retained.Remove(label, out var fresh))
                {
                    h = fresh;
                }
                else if (adopted.TryGetValue(label, out var shared) && shared.IsValid())
                {
                    Addressables.ResourceManager.Acquire(shared);   // +1 refcount, scope mới Release phần của mình
                    h = shared;
                }
                else continue;

                adopted[label] = h;
                scope.Retain(h);
                if (!scope.IsDisposed) scope.Disposed += _ => OnScopeReleased(label);
                else OnScopeReleased(label);
            }
        }

        /// <summary>Một scope vừa Release handle của label: chỉ coi là "chưa tải" khi không còn scope nào giữ handle.</summary>
        void OnScopeReleased(string label)
        {
            if (adopted.TryGetValue(label, out var h) && h.IsValid()) return;   // scope khác vẫn giữ
            adopted.Remove(label);
            if (!retained.ContainsKey(label)) done.Remove(label);
        }

        /// <summary>Label đã tải và handle vẫn còn người giữ (retained hoặc scope nào đó).</summary>
        bool IsHeld(string label)
        {
            if (!done.Contains(label)) return false;
            if (retained.TryGetValue(label, out var r) && r.IsValid()) return true;
            if (adopted.TryGetValue(label, out var a) && a.IsValid()) return true;
            done.Remove(label);
            adopted.Remove(label);
            return false;
        }

        public void ReleaseUnadopted(IEnumerable<string> keep)
        {
            if (retained.Count == 0) return;
            HashSet<string> keepSet = keep != null ? new HashSet<string>(keep) : null;
            var toRemove = new List<string>();
            foreach (var kv in retained)
            {
                if (keepSet != null && keepSet.Contains(kv.Key)) continue;
                if (kv.Value.IsValid()) Addressables.Release(kv.Value);
                toRemove.Add(kv.Key);
            }
            foreach (var k in toRemove)
            {
                retained.Remove(k);
                // Còn scope giữ (đã Adopt trước đó) thì label vẫn "đã tải".
                if (!(adopted.TryGetValue(k, out var a) && a.IsValid())) done.Remove(k);
            }
        }

        public void ReleaseAll()
        {
            foreach (var kv in retained)
                if (kv.Value.IsValid()) Addressables.Release(kv.Value);
            retained.Clear();
            foreach (var kv in running)
                if (kv.Value.Handle.IsValid()) Addressables.Release(kv.Value.Handle);
            running.Clear();
            adopted.Clear();   // handle trong scope do scope tự Release
            done.Clear();
        }

        public async Awaitable Ensure(IEnumerable<string> labels, IProgress<float> progress)
        {
            var keys = Filter(labels);
            keys.RemoveAll(IsHeld);
            if (keys.Count == 0) { progress?.Report(1f); return; }

            var ops = new List<(string label, Job job)>();
            foreach (var k in keys) ops.Add((k, Start(k)));

            var token = Services.AppToken;
            while (true)
            {
                bool allDone = true;
                float sum = 0f;
                foreach (var (_, job) in ops)
                {
                    if (IsDone(job)) { sum += 1f; continue; }
                    allDone = false;
                    sum += job.Handle.GetDownloadStatus().Percent;
                }
                progress?.Report(sum / ops.Count);
                if (allDone) break;
                await Awaitable.NextFrameAsync(token);
            }

            Exception error = null;
            foreach (var (label, job) in ops)
            {
                Complete(label, job);
                if (job.Succeeded) done.Add(label);
                else error ??= new Exception($"[Download] Lỗi tải label '{label}': {job.Error}");
            }
            if (error != null) throw error;
        }

        public async Awaitable Prefetch(IEnumerable<string> labels)
        {
            try { await Ensure(labels, null); }
            catch (OperationCanceledException) { }
            catch (Exception e) { Debug.LogWarning($"[Download] Prefetch lỗi (sẽ thử lại ở lần Ensure sau): {e.Message}"); }
        }
    }
}
