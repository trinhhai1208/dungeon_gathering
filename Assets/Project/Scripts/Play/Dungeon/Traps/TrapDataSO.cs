using System.Collections.Generic;
using DungeonGathering.Combat;
using UnityEngine;

namespace DungeonGathering.Play
{
    /// <summary>Cấu hình chỉ số bẫy dạng ScriptableObject (giảm lặp dữ liệu trên prefab bẫy).</summary>
    [CreateAssetMenu(fileName = "Trap_", menuName = "Dungeon Gathering/Trap Data")]
    public class TrapDataSO : ScriptableObject
    {
        [Tooltip("ID bẫy (định danh; hiện chưa được code runtime đọc).")]
        public string trapId;
        [Tooltip("Sát thương mỗi lần trúng (mỗi mục tiêu tối đa 1 lần / chu kỳ kích hoạt).")]
        public float damage = 1f;
        [Tooltip("Lực đẩy lùi mục tiêu ra xa tâm bẫy khi trúng (0 = không đẩy).")]
        public float knockback = 2f;

        [Header("Chu kỳ thời gian (Timed / Trigger)")]
        [Tooltip("Bẫy Timed: thời gian (giây) nghỉ trước khi báo hiệu lần tiếp theo.")]
        public float inactiveTime = 1.5f;
        [Tooltip("Thời gian (giây) báo hiệu trước khi bẫy đâm lên (Timed và Trigger).")]
        public float warningTime = 0.5f;
        [Tooltip("Thời gian (giây) bẫy ở trạng thái gây sát thương (Timed và Trigger).")]
        public float activeTime = 0.6f;
        [Tooltip("Bẫy Trigger: thời gian (giây) hồi sau khi đâm trước khi có thể kích hoạt lại.")]
        public float cooldown = 1f;

        [Header("Hiệu ứng khi trúng")]
        [Tooltip("Hiệu ứng trạng thái áp lên mục tiêu khi bẫy gây sát thương thành công. Trống = không có.")]
        public StatusPayload onHitStatus;
        [Header("Sàn nguyên tố (Hazard)")]
        [Tooltip("Sàn nguyên tố: hiệu ứng làm mới liên tục khi đứng trên (thời gian = thời gian vương lại sau khi rời sàn)")]
        public List<DungeonGathering.Catalog.StatusEffectRef> zoneEffects = new();
    }
}
