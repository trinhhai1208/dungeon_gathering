using System.Collections.Generic;
using UnityEngine;

namespace DungeonGathering.Play
{
    /// <summary>
    /// Cấu hình một loại phòng: template (prefab phòng vẽ sẵn — kích thước nằm trên RoomTemplate.size), số đợt quái,
    /// ngân sách quái, tỉ lệ Elite, tỉ lệ rương khi dọn xong. Dùng lại được cho nhiều level.
    /// </summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Room Type Config", fileName = "RoomType_")]
    public sealed class RoomTypeConfigSO : ScriptableObject
    {
        [Tooltip("Loại phòng mà cấu hình này áp dụng; LevelConfig lấy cấu hình đầu tiên khớp loại trong roomTypes.")]
        public RoomType roomType = RoomType.Combat;
        [Tooltip("Prefab phòng chọn ngẫu nhiên (cùng loại). Kích thước phòng = RoomTemplate.size của từng prefab")]
        public List<RoomTemplate> templates = new();

        [Header("Quái (phòng Combat / Elite / Boss)")]
        [Tooltip("Số đợt quái (min, max). (0, 0) = dùng wavesPerRoom của level")] public Vector2Int waves;
        [Tooltip("Ngân sách điểm spawnCost của đợt đầu. 0 = dùng của level")] public int spawnBudget;
        [Tooltip("Ngân sách tăng mỗi đợt. -1 = dùng của level")] public int budgetGrowth = -1;
        [Range(0, 1)] [Tooltip("Cộng thêm vào tỉ lệ Elite của level (phòng Elite: 1 = mọi con là Elite)")] public float eliteChanceBonus;

        [Header("Thưởng")]
        [Tooltip("Tỉ lệ rương khi dọn xong (0..1). Âm = dùng chestOnClearChance của level")] public float chestChance = -1f;

        public Vector2Int Size => templates.Count > 0 && templates[0] != null ? templates[0].size : Vector2Int.zero;
    }
}
