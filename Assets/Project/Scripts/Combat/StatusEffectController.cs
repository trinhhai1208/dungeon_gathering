using System.Collections.Generic;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>
    /// Bộ chạy hiệu ứng trạng thái gắn trên Player và quái (giữ tên cũ để không phải sửa prefab).
    /// Logic cộng dồn / tick / hết hạn nằm ở <see cref="StatusEffectSet"/>; component này lo VFX lặp, màu phủ,
    /// và nối hiệu ứng với StatSheet (Slow / buff là modifier) và IDamageable (DoT bỏ qua i-frame).
    /// Gây hiệu ứng: <see cref="Apply(in StatusEffectRef, GameObject)"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StatusEffectController : MonoBehaviour, IPoolable, IStatusTarget
    {
        [SerializeField] SpriteRenderer tintTarget;
        [SerializeField] Transform vfxSocket;

        IDamageable owner;
        IStatSheetProvider statsProvider;
        SpriteAnimDriver anim;
        Color baseColor = Color.white;
        StatusEffectSet set;
        readonly Dictionary<StatusInstance, GameObject> loopVfx = new();
        /// <summary>Phương án dự phòng khi mục tiêu không có StatSheet: hệ số chậm tự tính.</summary>
        float fallbackSlow;

        public StatusEffectSet Effects => EnsureSet();
        public bool IsStunned => set != null && set.IsCrowdControlled;
        public bool IsFrozen
        {
            get
            {
                if (set == null) return false;
                foreach (var i in set.Active) if (i.Effect is StunEffectSO s && s.freeze) return true;
                return false;
            }
        }

        /// <summary>Choáng → 0. Slow đã nằm trong chỉ số MoveSpeed; chỉ mục tiêu không có StatSheet mới nhân thêm ở đây.</summary>
        public float SpeedMultiplier => IsStunned ? 0f : 1f - fallbackSlow;

        // ---- IStatusTarget
        bool IStatusTarget.IsDead => owner != null && owner.IsDead;
        StatSheet IStatusTarget.Stats => statsProvider?.Stats;
        void IStatusTarget.ApplyStatusDamage(float amount, GameObject source)
        {
            if (owner == null || owner.IsDead || amount <= 0f) return;
            owner.TakeDamage(new DamageInfo(amount, 0f, Vector2.zero, 0f, false, source, Team.Neutral, DamageFlags.DoT));
        }

        void Awake()
        {
            owner = GetComponentInParent<IDamageable>(); // gồm cả chính object
            statsProvider = GetComponentInParent<IStatSheetProvider>();
            anim = GetComponentInChildren<SpriteAnimDriver>();
            if (tintTarget == null) tintTarget = GetComponentInChildren<SpriteRenderer>();
            if (tintTarget != null) baseColor = tintTarget.color;
            if (vfxSocket == null) vfxSocket = transform;
            EnsureSet();
        }

        StatusEffectSet EnsureSet()
        {
            if (set != null) return set;
            set = new StatusEffectSet(this);
            set.Added += OnAdded;
            set.Removed += OnRemoved;
            set.Changed += RefreshVisual;
            return set;
        }

        // ------------------------------------------------------------------ API mới

        /// <summary>Gây hiệu ứng theo tham chiếu (có ghi đè thời gian / độ mạnh, xác suất).</summary>
        public StatusInstance Apply(in StatusEffectRef r, GameObject source = null)
        {
            if (!r.IsValid) return null;
            if (r.Chance < 1f && Random.value > r.Chance) return null;
            return EnsureSet().Apply(r, source);
        }

        public StatusInstance Apply(StatusEffectSO effect, float duration, float magnitude, GameObject source = null) =>
            EnsureSet().Apply(effect, duration, magnitude, source);

        public bool Remove(StatusEffectSO effect) => set != null && set.Remove(effect);

        // ------------------------------------------------------------------ Vòng đời

        void Update()
        {
            if (set != null && set.Active.Count > 0) set.Tick(Time.deltaTime);
        }

        void OnAdded(StatusInstance inst)
        {
            if (inst.Effect is SlowEffectSO && statsProvider == null) RecomputeFallbackSlow();
            if (inst.LoopVfxPrefab == null || Services.Pool == null) return;
            bool overhead = inst.Effect is StunEffectSO s && !s.freeze;
            var fx = CombatFx.AttachScaled(inst.LoopVfxPrefab, vfxSocket, overhead, fallbackDuration: -1f);
            if (fx != null) loopVfx[inst] = fx;
        }

        void OnRemoved(StatusInstance inst)
        {
            if (inst.Effect is SlowEffectSO && statsProvider == null) RecomputeFallbackSlow();
            if (!loopVfx.TryGetValue(inst, out var fx)) return;
            loopVfx.Remove(inst);
            if (fx != null) Services.Pool?.Return(fx);
        }

        void RecomputeFallbackSlow()
        {
            fallbackSlow = 0f;
            if (set == null) return;
            foreach (var i in set.Active)
                if (i.Effect is SlowEffectSO) fallbackSlow = Mathf.Max(fallbackSlow, SlowEffectSO.Clamp(i.Magnitude));
        }

        void RefreshVisual()
        {
            if (statsProvider == null) RecomputeFallbackSlow();
            Color? overlay = null;
            int best = int.MinValue;
            if (set != null)
                foreach (var i in set.Active)
                {
                    var c = i.Effect.tint;
                    if (c.a <= 0f || i.Effect.tintPriority < best) continue;
                    best = i.Effect.tintPriority;
                    overlay = new Color(c.r, c.g, c.b, 1f);
                }
            if (anim != null && anim.Renderer == tintTarget)
            {
                anim.SetStatusTint(overlay);
                return;
            }
            if (tintTarget != null) tintTarget.color = overlay ?? baseColor;
        }

        /// <summary>Gỡ hết (khi chết / vào pool): trả modifier, trả VFX lặp về pool.</summary>
        public void ClearAll()
        {
            set?.Clear();
            foreach (var fx in loopVfx.Values) if (fx != null) Services.Pool?.Return(fx);
            loopVfx.Clear();
            fallbackSlow = 0f;
            RefreshVisual();
        }

        public void OnSpawn() => ClearAll();
        public void OnDespawn() => ClearAll();
    }
}
