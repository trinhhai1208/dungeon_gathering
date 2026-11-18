using System;
using System.Collections.Generic;
using System.Threading;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>
    /// Trạng thái runtime của passive/kỹ năng theo từng Player (handler đã đăng ký, bộ đếm, cờ chống đệ quy, buff đang chạy).
    /// SO passive là asset dùng chung → KHÔNG giữ state trên SO (khi tắt Domain Reload, field của SO sống qua các lần Play).
    /// Component gắn lên GameObject của Player nên state chết cùng Player.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PassiveRuntime : MonoBehaviour
    {
        readonly Dictionary<ItemPassiveSO, PassiveState> states = new();
        readonly List<PassiveState> ticking = new();

        /// <summary>Lấy runtime của Player (tạo nếu create = true). Null nếu Player đã bị hủy.</summary>
        public static PassiveRuntime For(IPlayerContext player, bool create = true)
        {
            if (player == null) return null;
            var go = player.GameObject;
            if (go == null) return null;
            var rt = go.GetComponent<PassiveRuntime>();
            if (rt == null && create) rt = go.AddComponent<PassiveRuntime>();
            return rt;
        }

        public PassiveState GetOrCreate(ItemPassiveSO key)
        {
            if (!states.TryGetValue(key, out var s)) states[key] = s = new PassiveState();
            return s;
        }

        public bool TryGet(ItemPassiveSO key, out PassiveState state) => states.TryGetValue(key, out state);

        public void Remove(ItemPassiveSO key)
        {
            if (!states.TryGetValue(key, out var s)) return;
            states.Remove(key);
            ticking.Remove(s);
        }

        /// <summary>Đăng ký state cần Tick mỗi frame (buff có hạn). Tick trả false → tự gỡ.</summary>
        public void StartTicking(PassiveState s)
        {
            if (s != null && !ticking.Contains(s)) ticking.Add(s);
        }

        void Update()
        {
            for (int i = ticking.Count - 1; i >= 0; i--)
            {
                if (i >= ticking.Count) continue;
                var s = ticking[i];
                bool keep;
                try { keep = s.Tick != null && s.Tick(); }
                catch (Exception e) { Debug.LogException(e, this); keep = false; }
                if (!keep && i < ticking.Count && ticking[i] == s) ticking.RemoveAt(i);
            }
        }

        // ---------------- Hỗ trợ kỹ năng async ----------------

        /// <summary>Player còn tồn tại, đang bật và chưa chết.</summary>
        public static bool IsAlive(IPlayerContext ctx)
        {
            if (ctx == null) return false;
            var go = ctx.GameObject;
            return go != null && go.activeInHierarchy && !ctx.IsDead;
        }

        /// <summary>
        /// Chờ seconds giây; hủy khi app tắt hoặc Player bị Destroy. Trả true nếu chờ xong và Player vẫn còn sống/bật.
        /// </summary>
        public static async Awaitable<bool> WaitWhileAlive(IPlayerContext ctx, float seconds)
        {
            var rt = For(ctx);
            if (rt == null) return false;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(Services.AppToken, rt.destroyCancellationToken);
            try { await Awaitable.WaitForSecondsAsync(Mathf.Max(0f, seconds), cts.Token); }
            catch (OperationCanceledException) { return false; }
            return IsAlive(ctx);
        }
    }

    /// <summary>State của một passive trên một Player (không cấp phát mỗi đòn).</summary>
    public sealed class PassiveState
    {
        public Delegate Handler;
        public Func<bool> Tick;
        public float Accum;
        public float Until;
        public int Counter;
        public int RefCount;
        public bool Busy;
        public bool Active;
    }
}
