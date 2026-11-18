using System.Collections.Generic;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>
    /// Thứ tự level của một run. Số level = số phần tử (thay maxFloor cố định 3). Lưu address (không tham chiếu trực
    /// tiếp) để catalog không kéo theo prefab phòng / quái của mọi level vào cùng bundle.
    /// </summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Campaign", fileName = "Campaign_")]
    public sealed class CampaignSO : ScriptableObject
    {
        [Tooltip("ID campaign (định danh; hiện chưa được code runtime đọc).")]
        public string campaignId = "dungeon";
        [Tooltip("Theme của campaign (hiện chưa được đọc; runtime dùng themeId của session khi không có address).")]
        public string themeId = "dungeon";
        [Tooltip("Address của LevelConfig theo thứ tự (level 1, 2, …)")]
        public List<string> levelAddresses = new();

        public int LevelCount => levelAddresses?.Count ?? 0;

        /// <summary>Address của level (1-based); null khi vượt danh sách.</summary>
        public string AddressOf(int level) =>
            levelAddresses != null && level >= 1 && level <= levelAddresses.Count ? levelAddresses[level - 1] : null;
    }
}
