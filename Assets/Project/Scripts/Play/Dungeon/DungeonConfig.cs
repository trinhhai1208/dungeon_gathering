using System.Collections.Generic;
using DungeonGathering.Enemies;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DungeonGathering.Play
{
    [System.Serializable]
    public class EnemySpawnEntry
    {
        public Enemy prefab;
        [Min(0)] public int weight = 10;
        [Tooltip("Số instance tạo trước khi vào tầng")] [Min(0)] public int prewarm = 4;
        public int Cost => prefab != null && prefab.Data != null ? Mathf.Max(1, prefab.Data.spawnCost) : 1;
    }

    /// <summary>
    /// Cấu hình một tầng (ARCHITECTURE §2): số phòng, pool RoomTemplate theo loại, tile theo theme,
    /// bảng quái + ngân sách độ khó, Boss. Address: theme/&lt;id&gt;/config/floor-&lt;n&gt;.
    /// </summary>
    [CreateAssetMenu(fileName = "DungeonConfig_Floor", menuName = "Dungeon Gathering/Dungeon Config")]
    public class DungeonConfig : ScriptableObject
    {
        [Header("Bố cục")]
        public string themeId = "dungeon";
        public int floor = 1;
        [Min(2)] public int mainPathLength = 6;
        [Min(0)] public int branchCount = 2;
        public Vector2Int branchLength = new(1, 2);
        public Vector2Int gridSize = new(7, 7);
        [Tooltip("Khoảng cách giữa tâm 2 ô lưới (tile) — lớn hơn MaxRoomSize")] public int cellSpacing = 40;
        public bool includeTreasure = true;
        public bool includeShop = true;

        [Header("Room templates")]
        public List<RoomTemplate> startRooms = new();
        public List<RoomTemplate> combatRooms = new();
        public List<RoomTemplate> treasureRooms = new();
        public List<RoomTemplate> shopRooms = new();
        public List<RoomTemplate> bossRooms = new();

        [Header("Tile theo theme")]
        public TileBase corridorFloorTile;
        public TileBase corridorWallTile;
        [Tooltip("Tile bịt Door Socket không dùng")] public TileBase sealWallTile;
        [Min(1)] public int corridorWidth = 3;
        [Min(1)] public int sealDepth = 2;

        [Header("Quái")]
        public List<EnemySpawnEntry> enemyTable = new();
        [Tooltip("Ngân sách điểm spawnCost của đợt đầu")] public int spawnBudget = 6;
        [Tooltip("Ngân sách tăng thêm mỗi đợt")] public int budgetGrowth = 2;
        public Vector2Int wavesPerRoom = new(2, 3);
        public float enemyHealthMultiplier = 1f;
        public float spawnWarningTime = 1f;

        [Header("Boss")]
        [Tooltip("Address boss (tải nền, label boss-<id>). Trống → dùng bossPrefab")]
        public string bossAddress;
        public Enemy bossPrefab;
        public float bossLoadTimeout = 20f;

        [Header("Thưởng")]
        [Range(0, 1)] public float chestOnClearChance = 0.25f;

        public GeneratorSettings ToGeneratorSettings() => new()
        {
            MainPathLength = mainPathLength,
            BranchCount = branchCount,
            BranchLength = branchLength,
            GridSize = gridSize,
            Treasure = includeTreasure,
            Shop = includeShop,
        };

        public List<RoomTemplate> RoomsFor(RoomType t) => t switch
        {
            RoomType.Start => startRooms.Count > 0 ? startRooms : combatRooms,
            RoomType.Treasure => treasureRooms.Count > 0 ? treasureRooms : combatRooms,
            RoomType.Shop => shopRooms.Count > 0 ? shopRooms : combatRooms,
            RoomType.Boss => bossRooms.Count > 0 ? bossRooms : combatRooms,
            _ => combatRooms,
        };
    }
}
