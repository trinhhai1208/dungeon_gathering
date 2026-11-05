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
    }

    /// <summary>
    /// Tải phụ thuộc theo label (MODULES §7.4). Ensure gặp label đang Prefetch thì gộp vào thao tác đó.
    /// Label chưa tồn tại trong catalog (chưa làm M2) bị bỏ qua kèm cảnh báo thay vì ném lỗi.
    /// </summary>
    public sealed class DownloadService : IDownloadService
    {
        /// <summary>
        /// Một lần tải label, có thể có nhiều người chờ (Prefetch + Ensure). Người chờ đầu tiên thấy handle xong
        /// sẽ chốt kết quả vào Job rồi Release handle; người chờ sau đọc kết quả từ Job (không đụng handle đã Release).
        /// </summary>
        sealed class Job
        {
            public AsyncOperationHandle Handle;
            public bool Finished;
            public bool Succeeded;
            public string Error;
        }

        readonly Dictionary<string, Job> running = new();
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

        /// <summary>Chốt kết quả + Release handle đúng một lần (người chờ đầu tiên làm).</summary>
        void Complete(string label, Job job)
        {
            if (job.Finished) return;
            job.Finished = true;
            if (job.Handle.IsValid())
            {
                job.Succeeded = job.Handle.Status == AsyncOperationStatus.Succeeded;
                if (!job.Succeeded) job.Error = job.Handle.OperationException?.Message;
                Addressables.Release(job.Handle);
            }
            else
            {
                job.Succeeded = false;
                job.Error = "handle không hợp lệ";
            }
            job.Handle = default;
            if (running.TryGetValue(label, out var cur) && cur == job) running.Remove(label);
        }

        public async Awaitable Ensure(IEnumerable<string> labels, IProgress<float> progress)
        {
            var keys = Filter(labels);
            keys.RemoveAll(done.Contains);
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
