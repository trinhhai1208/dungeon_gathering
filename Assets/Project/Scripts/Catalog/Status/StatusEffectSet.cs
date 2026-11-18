using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>
    /// Danh sách hiệu ứng đang chạy trên một mục tiêu: cộng dồn theo <see cref="StackPolicy"/>, tick, hết hạn.
    /// Thuần C# (EditMode test được); MonoBehaviour <c>StatusEffectController</c> bọc lại + lo VFX và màu.
    /// </summary>
    public sealed class StatusEffectSet
    {
        readonly IStatusTarget target;
        readonly List<StatusInstance> active = new();
        int version; // tăng khi Clear — vòng tick đang chạy biết mà dừng

        public StatusEffectSet(IStatusTarget target) => this.target = target ?? throw new ArgumentNullException(nameof(target));

        public IReadOnlyList<StatusInstance> Active => active;

        /// <summary>Gọi khi một hiệu ứng mới bắt đầu (để gắn VFX lặp).</summary>
        public event Action<StatusInstance> Added;
        /// <summary>Gọi khi một hiệu ứng kết thúc (hết hạn, bị gỡ, Clear).</summary>
        public event Action<StatusInstance> Removed;
        /// <summary>Tập hiệu ứng đổi (thêm / bớt / đổi độ mạnh) — để cập nhật màu, cờ khống chế.</summary>
        public event Action Changed;

        public bool IsCrowdControlled
        {
            get
            {
                foreach (var i in active) if (i.Effect.IsCrowdControl) return true;
                return false;
            }
        }

        public StatusInstance Find(StatusEffectSO effect)
        {
            foreach (var i in active) if (i.Effect == effect) return i;
            return null;
        }

        public bool Has(StatusEffectSO effect) => Find(effect) != null;

        /// <summary>Gây hiệu ứng. Trả về instance đang chạy (mới hoặc cũ đã cộng dồn); null nếu bị bỏ qua.</summary>
        public StatusInstance Apply(StatusEffectSO effect, float duration, float magnitude, GameObject source = null, GameObject loopVfx = null)
        {
            if (effect == null || target.IsDead || duration <= 0f) return null;

            var cur = Find(effect);
            if (cur == null)
            {
                var inst = new StatusInstance
                {
                    Effect = effect, BaseMagnitude = magnitude, Magnitude = magnitude, Remaining = duration,
                    Source = source, LoopVfxPrefab = loopVfx != null ? loopVfx : effect.loopVfx,
                };
                active.Add(inst);
                effect.OnApply(target, inst);
                Added?.Invoke(inst);
                Changed?.Invoke();
                return inst;
            }

            float oldMag = cur.Magnitude;
            switch (effect.stacking)
            {
                case StackPolicy.Refresh:
                    cur.BaseMagnitude = cur.Magnitude = magnitude;
                    cur.Remaining = duration;
                    break;
                case StackPolicy.Extend:
                    cur.Remaining += duration;
                    if (magnitude > cur.Magnitude) cur.BaseMagnitude = cur.Magnitude = magnitude;
                    break;
                case StackPolicy.Stack:
                    cur.Stacks = Mathf.Min(cur.Stacks + 1, Mathf.Max(1, effect.maxStacks));
                    cur.BaseMagnitude = magnitude;
                    cur.Magnitude = magnitude * cur.Stacks;
                    cur.Remaining = duration;
                    break;
                default: // KeepStrongest
                    if (magnitude > cur.Magnitude + 1e-5f)
                    {
                        cur.BaseMagnitude = cur.Magnitude = magnitude;
                        cur.Remaining = duration;
                    }
                    else if (Mathf.Abs(magnitude - cur.Magnitude) <= 1e-5f)
                        cur.Remaining = Mathf.Max(cur.Remaining, duration);
                    else
                        return cur; // yếu hơn: bỏ qua, không kéo dài hiệu ứng mạnh đang chạy
                    break;
            }
            if (source != null) cur.Source = source;
            if (!Mathf.Approximately(oldMag, cur.Magnitude)) effect.OnMagnitudeChanged(target, cur);
            Changed?.Invoke();
            return cur;
        }

        public StatusInstance Apply(in StatusEffectRef r, GameObject source = null) =>
            r.IsValid ? Apply(r.effect, r.Duration, r.Magnitude, source, r.LoopVfx) : null;

        public bool Remove(StatusEffectSO effect)
        {
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].Effect != effect) continue;
                RemoveAt(i);
                Changed?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>Gỡ hết (chết, vào pool). Gọi OnRemove để trả modifier.</summary>
        public void Clear()
        {
            if (active.Count == 0) return;
            version++;
            for (int i = active.Count - 1; i >= 0; i--) RemoveAt(i);
            Changed?.Invoke();
        }

        public void Tick(float dt)
        {
            if (active.Count == 0 || dt <= 0f) return;
            int v = version;
            bool changed = false;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (i >= active.Count) continue;
                var inst = active[i];
                inst.Effect.OnTick(target, inst, dt);
                // DoT có thể giết mục tiêu → luồng chết gọi Clear() ngay trong vòng này: dừng lại.
                if (v != version) return;
                inst.Remaining -= dt;
                if (inst.Remaining <= 0f || target.IsDead)
                {
                    int idx = active.IndexOf(inst);
                    if (idx >= 0) { RemoveAt(idx); changed = true; }
                }
            }
            if (changed) Changed?.Invoke();
        }

        void RemoveAt(int index)
        {
            var inst = active[index];
            active.RemoveAt(index);
            try { inst.Effect.OnRemove(target, inst); }
            finally { Removed?.Invoke(inst); }
        }
    }
}
