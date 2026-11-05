using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGathering.Core
{
    public interface IEvent { }

    /// <summary>Xóa mọi EventBus&lt;T&gt; khi vào Play Mode (an toàn với Fast Enter Play Mode) — ARCHITECTURE §10.</summary>
    public static class EventBusRegistry
    {
        static readonly HashSet<Action> clearActions = new();
        public static void Register(Action clearAction) => clearActions.Add(clearAction);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetAll()
        {
            foreach (var clear in clearActions) clear.Invoke();
            clearActions.Clear();
        }
    }

    /// <summary>
    /// Bus sự kiện tĩnh theo kiểu. Subscribe trong OnEnable, Unsubscribe trong OnDisable.
    /// Không sticky: thành phần mở muộn phải tự đọc trạng thái hiện tại (R7).
    /// Raise lồng cùng kiểu (handler Raise lại chính kiểu đó) được XẾP HÀNG và phát sau khi lượt hiện tại xong,
    /// theo đúng thứ tự — mọi handler đều thấy đủ chuỗi sự kiện. Giới hạn số sự kiện xếp hàng để chặn vòng lặp vô hạn.
    /// </summary>
    public static class EventBus<T> where T : struct, IEvent
    {
        const int MaxQueuedPerRaise = 64;

        static event Action<T> handlers;
        static bool raising;
        static readonly Queue<T> queued = new();

        public static void Subscribe(Action<T> h)
        {
            handlers += h;
            EventBusRegistry.Register(Clear);
        }

        public static void Unsubscribe(Action<T> h) => handlers -= h;

        public static void Raise(T e)
        {
            if (raising)
            {
                queued.Enqueue(e);
                return;
            }
            raising = true;
            try
            {
                Dispatch(e);
                int processed = 0;
                while (queued.Count > 0)
                {
                    if (++processed > MaxQueuedPerRaise)
                    {
                        Debug.LogError($"[EventBus] Quá {MaxQueuedPerRaise} Raise lồng nhau cho {typeof(T).Name} — nghi vòng lặp, bỏ {queued.Count} sự kiện.");
                        queued.Clear();
                        break;
                    }
                    Dispatch(queued.Dequeue());
                }
            }
            finally
            {
                raising = false;
                queued.Clear();
            }
        }

        static void Dispatch(T e)
        {
            var list = handlers; // snapshot
            if (list == null) return;
            foreach (Action<T> h in list.GetInvocationList())
            {
                try { h(e); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }

        internal static void Clear() { handlers = null; raising = false; queued.Clear(); }
    }
}
