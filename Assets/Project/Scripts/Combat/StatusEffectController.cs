using System.Collections.Generic;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>
    /// Làm chậm / sát thương theo thời gian (thiêu đốt, độc, xuất huyết) / choáng / đóng băng.
    /// Gắn lên quái và Player. Motor đọc SpeedMultiplier + IsStunned.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StatusEffectController : MonoBehaviour, IPoolable
    {
        struct Dot
        {
            public float Dps, Remaining, TickTimer;
            public string Id;
            public GameObject Source;
        }

        [SerializeField] SpriteRenderer tintTarget;
        [SerializeField] Color slowTint = new(0.6f, 0.8f, 1f);
        [SerializeField] Color freezeTint = new(0.5f, 0.75f, 1f);
        [SerializeField] Transform vfxSocket;
        const float TickInterval = 0.5f;

        IDamageable owner;
        float slowPct, slowRemaining;
        float stunRemaining;
        bool frozen;
        readonly List<Dot> dots = new();
        Color baseColor = Color.white;
        readonly Dictionary<string, GameObject> loopVfx = new();

        public float SpeedMultiplier => IsStunned ? 0f : 1f - slowPct;
        public bool IsStunned => stunRemaining > 0f;
        public bool IsFrozen => frozen && IsStunned;

        void Awake()
        {
            owner = GetComponentInParent<IDamageable>(); // gồm cả chính object
            if (tintTarget == null) tintTarget = GetComponentInChildren<SpriteRenderer>();
            if (tintTarget != null) baseColor = tintTarget.color;
            if (vfxSocket == null) vfxSocket = transform;
        }

        public void ApplySlow(float percent, float duration)
        {
            if (owner != null && owner.IsDead) return;
            percent = Mathf.Clamp01(percent);
            if (percent >= slowPct || slowRemaining <= 0f) slowPct = percent;
            slowRemaining = Mathf.Max(slowRemaining, duration);
            RefreshTint();
        }

        public void ApplyStun(float duration, bool freeze = false)
        {
            if (owner != null && owner.IsDead) return;
            stunRemaining = Mathf.Max(stunRemaining, duration);
            frozen |= freeze;
            RefreshTint();
        }

        /// <summary>DoT cùng id không cộng dồn, chỉ làm mới thời gian + lấy dps lớn hơn.</summary>
        public void ApplyDot(string id, float dps, float duration, GameObject source = null, GameObject loopVfxPrefab = null)
        {
            if (owner != null && owner.IsDead) return;
            for (int i = 0; i < dots.Count; i++)
            {
                if (dots[i].Id != id) continue;
                var d = dots[i];
                d.Dps = Mathf.Max(d.Dps, dps);
                d.Remaining = Mathf.Max(d.Remaining, duration);
                dots[i] = d;
                return;
            }
            dots.Add(new Dot { Id = id, Dps = dps, Remaining = duration, TickTimer = TickInterval, Source = source });
            if (loopVfxPrefab != null && !loopVfx.ContainsKey(id) && Services.Pool != null)
            {
                var fx = Services.Pool.Get(loopVfxPrefab, vfxSocket.position, Quaternion.identity, vfxSocket);
                if (fx != null) loopVfx[id] = fx;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (slowRemaining > 0f && (slowRemaining -= dt) <= 0f) { slowPct = 0f; RefreshTint(); }
            if (stunRemaining > 0f && (stunRemaining -= dt) <= 0f) { frozen = false; RefreshTint(); }

            for (int i = dots.Count - 1; i >= 0; i--)
            {
                var d = dots[i];
                d.Remaining -= dt;
                d.TickTimer -= dt;
                if (d.TickTimer <= 0f)
                {
                    d.TickTimer += TickInterval;
                    if (owner != null && !owner.IsDead)
                    {
                        owner.TakeDamage(new DamageInfo(d.Dps * TickInterval, 0f, Vector2.zero, 0f, false, d.Source, Team.Neutral));
                        // Đòn DoT giết mục tiêu → HandleDeath gọi ClearAll() xóa list ngay trong vòng lặp này: dừng, không RemoveAt nữa.
                        if (i >= dots.Count) return;
                    }
                }
                if (d.Remaining <= 0f || owner == null || owner.IsDead)
                {
                    dots.RemoveAt(i);
                    StopLoopVfx(d.Id);
                }
                else dots[i] = d;
            }
        }

        void StopLoopVfx(string id)
        {
            if (!loopVfx.TryGetValue(id, out var fx)) return;
            loopVfx.Remove(id);
            if (fx != null) Services.Pool?.Return(fx);
        }

        void RefreshTint()
        {
            if (tintTarget == null) return;
            tintTarget.color = IsFrozen ? freezeTint : slowRemaining > 0f ? slowTint : baseColor;
        }

        /// <summary>Gỡ hết (khi chết): VFX loop không được là con của object sắp vào pool.</summary>
        public void ClearAll()
        {
            slowPct = slowRemaining = stunRemaining = 0f;
            frozen = false;
            dots.Clear();
            // Không cấp phát: trả từng VFX rồi xóa dictionary một lần.
            foreach (var fx in loopVfx.Values) if (fx != null) Services.Pool?.Return(fx);
            loopVfx.Clear();
            RefreshTint();
        }

        public void OnSpawn() => ClearAll();
        public void OnDespawn() => ClearAll();
    }
}
