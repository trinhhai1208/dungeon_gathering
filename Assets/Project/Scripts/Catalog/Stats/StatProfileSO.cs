using System;
using System.Collections.Generic;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>
    /// Bảng chỉ số gốc dùng chung cho hero và quái (vd "Stats_Hero_Warrior", "Stats_Monster_Melee").
    /// Nơi dùng gắn qua <see cref="StatBlock"/> và chỉ ghi đè vài chỉ số cần khác.
    /// </summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Stats/Stat Profile", fileName = "Stats_")]
    public sealed class StatProfileSO : ScriptableObject
    {
        [Tooltip("Chỉ số gốc của profile; nơi dùng (StatBlock) chép ra rồi ghi đè vài chỉ số")] public BaseStats stats = BaseStats.Default;

        [Header("Hồi phục")]
        [Tooltip("Giây không bị đánh trước khi giáp bắt đầu hồi.")] public float armorRegenDelay = 3f;
        [Tooltip("Điểm giáp hồi mỗi giây sau khi hết delay.")] public float armorRegenRate = 1f;
    }

    /// <summary>Ghi đè một chỉ số gốc (giá trị tuyệt đối, không phải cộng thêm).</summary>
    [Serializable]
    public struct StatOverride
    {
        [Tooltip("Chỉ số cần ghi đè")] public StatType stat;
        [Tooltip("Giá trị gốc mới (tuyệt đối), thay hẳn giá trị trong profile chứ không cộng thêm")] public float value;

        public StatOverride(StatType stat, float value) { this.stat = stat; this.value = value; }
    }

    /// <summary>
    /// "Profile + ghi đè": hero/quái chỉ chọn một <see cref="StatProfileSO"/> rồi ghi đè vài chỉ số.
    /// Thiếu profile → dùng chỉ số dự phòng do nơi gọi truyền vào (dữ liệu cũ).
    /// </summary>
    [Serializable]
    public sealed class StatBlock
    {
        [Tooltip("Bảng chỉ số gốc. Trống → dùng chỉ số dự phòng do nơi gọi truyền vào (dữ liệu cũ)")] public StatProfileSO profile;
        [Tooltip("Ghi đè vài chỉ số của profile (giá trị tuyệt đối, áp theo thứ tự)")] public List<StatOverride> overrides = new();

        public bool HasProfile => profile != null;

        /// <summary>Chỉ số gốc sau khi áp ghi đè. Không ghi vào SO.</summary>
        public BaseStats Resolve(in BaseStats fallback)
        {
            var b = profile != null ? profile.stats : fallback;
            if (overrides != null)
                foreach (var o in overrides) b.Set(o.stat, o.value);
            return b;
        }

        public float ArmorRegenDelay(float fallback) => profile != null ? profile.armorRegenDelay : fallback;
        public float ArmorRegenRate(float fallback) => profile != null ? profile.armorRegenRate : fallback;
    }
}
