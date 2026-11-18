using System;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>Cách cộng dồn khi cùng một hiệu ứng được gây lại lên mục tiêu đang mang nó.</summary>
    public enum StackPolicy
    {
        /// <summary>Thay bằng lần mới (thời gian + độ mạnh của lần mới).</summary>
        Refresh = 0,
        /// <summary>Cộng thêm thời gian, giữ độ mạnh lớn hơn.</summary>
        Extend = 1,
        /// <summary>Tăng số tầng (tối đa maxStacks); độ mạnh = độ mạnh mỗi tầng × số tầng; làm mới thời gian.</summary>
        Stack = 2,
        /// <summary>Mạnh hơn → thay cả cặp (độ mạnh, thời gian); bằng → lấy thời gian dài hơn; yếu hơn → bỏ qua.</summary>
        KeepStrongest = 3,
    }

    [Flags]
    public enum StatusTag
    {
        None = 0,
        CrowdControl = 1 << 0,
        DamageOverTime = 1 << 1,
        Buff = 1 << 2,
        Debuff = 1 << 3,
    }

    /// <summary>Đối tượng mang hiệu ứng (runtime cài đặt). Catalog không biết MonoBehaviour cụ thể.</summary>
    public interface IStatusTarget
    {
        bool IsDead { get; }
        /// <summary>Null nếu đối tượng không có hệ chỉ số (vd đồng hành) — hiệu ứng chỉ số sẽ dùng phương án dự phòng.</summary>
        StatSheet Stats { get; }
        /// <summary>Sát thương từ hiệu ứng (DoT): bỏ qua i-frame, không đặt lại i-frame.</summary>
        void ApplyStatusDamage(float amount, GameObject source);
    }

    /// <summary>Thành phần có StatSheet (PlayerStats, EnemyHealth) — để hiệu ứng tìm được bảng chỉ số.</summary>
    public interface IStatSheetProvider
    {
        StatSheet Stats { get; }
    }

    /// <summary>Một hiệu ứng đang chạy trên mục tiêu. Runtime, không lưu vào SO.</summary>
    public sealed class StatusInstance
    {
        public StatusEffectSO Effect;
        public float Magnitude;          // độ mạnh hiện tại (đã nhân số tầng)
        public float BaseMagnitude;      // độ mạnh mỗi tầng (lần gây gần nhất)
        public float Remaining;
        public int Stacks = 1;
        public GameObject Source;
        public GameObject LoopVfxPrefab; // VFX lặp (ghi đè hoặc của SO)
        public float TickTimer;
        /// <summary>Khóa nguồn modifier trên StatSheet (mỗi instance một khóa riêng).</summary>
        public readonly object ModifierKey = new();
    }

    /// <summary>
    /// Định nghĩa một hiệu ứng trạng thái (Stun, Slow, Burn…). Một asset = một hiệu ứng với tham số mặc định;
    /// nơi gây hiệu ứng tham chiếu asset qua <see cref="StatusEffectRef"/> và ghi đè thời gian / độ mạnh nếu cần.
    /// </summary>
    public abstract class StatusEffectSO : ScriptableObject
    {
        [Tooltip("Định danh duy nhất (vd \"stun\", \"burn\")")] public string id;
        [Tooltip("Key localization tên hiệu ứng (hiện chưa UI nào đọc)")] public string nameKey;
        [Tooltip("Icon hiệu ứng cho UI (hiện chưa UI nào đọc)")] public Sprite icon;

        [Header("Mặc định")]
        [Min(0f)] public float defaultDuration = 2f;
        [Tooltip("Ý nghĩa tùy loại: Slow = phần trăm (0.3 = 30%), DoT = sát thương mỗi giây, StatModifier = hệ số nhân")]
        public float defaultMagnitude = 1f;

        [Header("Cộng dồn")]
        [Tooltip("Khi gây lại lúc đang dính: Refresh / Extend / Stack (tối đa maxStacks) / KeepStrongest")] public StackPolicy stacking = StackPolicy.KeepStrongest;
        [Min(1)] public int maxStacks = 1;

        [Header("Hiển thị")]
        [Tooltip("Phân loại; có CrowdControl → mục tiêu không đi / không đánh / không dùng skill")] public StatusTag tags;
        [Tooltip("Màu phủ lên sprite khi đang dính (alpha 0 = không phủ)")] public Color tint = new(1f, 1f, 1f, 0f);
        [Tooltip("Nhiều hiệu ứng cùng có màu → lấy cái ưu tiên cao nhất")] public int tintPriority;
        [Tooltip("VFX lặp gắn theo mục tiêu khi đang dính (lấy từ pool)")] public GameObject loopVfx;

        /// <summary>Hiệu ứng khống chế (mục tiêu không đi / không đánh / không dùng skill).</summary>
        public virtual bool IsCrowdControl => (tags & StatusTag.CrowdControl) != 0;

        public virtual void OnApply(IStatusTarget target, StatusInstance inst) { }
        /// <summary>Độ mạnh đổi do cộng dồn (vd Slow phải cập nhật modifier).</summary>
        public virtual void OnMagnitudeChanged(IStatusTarget target, StatusInstance inst) { }
        public virtual void OnTick(IStatusTarget target, StatusInstance inst, float dt) { }
        public virtual void OnRemove(IStatusTarget target, StatusInstance inst) { }
    }

    /// <summary>
    /// Gây một hiệu ứng với tham số ghi đè tùy chọn. Ví dụ skill A: Stun + thời gian 1.5s; skill B: Stun + 3s.
    /// </summary>
    [Serializable]
    public struct StatusEffectRef
    {
        [Tooltip("Asset hiệu ứng cần gây. Trống → không gây gì")] public StatusEffectSO effect;
        [Tooltip("Thời gian hiệu lực (giây). Bỏ tick = defaultDuration của hiệu ứng")] public Overridable<float> duration;
        [Tooltip("Độ mạnh tùy loại: Slow 0.3 = chậm 30%, DoT = sát thương / giây, Buff = hệ số nhân. Bỏ tick = defaultMagnitude")] public Overridable<float> magnitude;
        [Range(0f, 1f)] [Tooltip("Xác suất gây (1 = luôn)")] public float chance;
        [Tooltip("Ghi đè VFX lặp của hiệu ứng")] public GameObject loopVfxOverride;

        public StatusEffectRef(StatusEffectSO effect, float? duration = null, float? magnitude = null, float chance = 1f)
        {
            this.effect = effect;
            this.duration = duration.HasValue ? new Overridable<float>(duration.Value) : default;
            this.magnitude = magnitude.HasValue ? new Overridable<float>(magnitude.Value) : default;
            this.chance = chance;
            loopVfxOverride = null;
        }

        public bool IsValid => effect != null;
        public float Duration => effect != null ? duration.Resolve(effect.defaultDuration) : 0f;
        public float Magnitude => effect != null ? magnitude.Resolve(effect.defaultMagnitude) : 0f;
        /// <summary>chance = 0 trong asset cũ (chưa đặt) được hiểu là 1.</summary>
        public float Chance => chance <= 0f ? 1f : chance;
        public GameObject LoopVfx => loopVfxOverride != null ? loopVfxOverride : effect != null ? effect.loopVfx : null;
    }
}
