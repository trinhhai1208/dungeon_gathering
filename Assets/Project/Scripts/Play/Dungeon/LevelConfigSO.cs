using System.Collections.Generic;
using DungeonGathering.Enemies;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DungeonGathering.Play
{
    /// <summary>Một lần sinh quái: prefab + định nghĩa áp lên (null = dùng data gắn sẵn trên prefab).</summary>
    public readonly struct SpawnUnit
    {
        public readonly Enemy Prefab;
        public readonly MonsterDefinitionSO Definition;
        /// <summary>Sinh ra ở bậc Elite (tỉ lệ Elite của level / phòng Elite).</summary>
        public readonly bool Elite;
        public SpawnUnit(Enemy prefab, MonsterDefinitionSO definition, bool elite = false) { Prefab = prefab; Definition = definition; Elite = elite; }
        public bool IsValid => Prefab != null;
        public SpawnUnit AsElite(bool elite) => new(Prefab, Definition, elite);
    }

    [System.Serializable]
    public class EnemySpawnEntry
    {
        [Tooltip("Định nghĩa quái — sinh bằng prefab nền dùng chung PlayPrefabSet.monsterBase")]
        public MonsterDefinitionSO monster;
        [Min(0)] public int weight = 10;
        [Tooltip("Số instance tạo trước khi vào tầng")] [Min(0)] public int prewarm = 4;

        public MonsterDefinitionSO Definition => monster;
        public int Cost => monster != null ? Mathf.Max(1, monster.spawnCost) : 1;
        public bool IsValid => monster != null;

        /// <summary>Prefab nền dùng chung + định nghĩa (không có prefab nền → không sinh được).</summary>
        public SpawnUnit Resolve(Enemy monsterBase) => monster != null && monsterBase != null ? new SpawnUnit(monsterBase, monster) : default;
    }

    /// <summary>Số phòng đặc biệt của một loại trong level (Treasure, Shop, Elite…).</summary>
    [System.Serializable]
    public struct SpecialRoomCount
    {
        [Tooltip("Loại phòng đặc biệt (Treasure, Shop, Elite…). Start / Boss / Combat bị bỏ qua.")]
        public RoomType type;
        [Min(0)] public int count;
    }

    /// <summary>
    /// Cấu hình một level (map): số phòng có quái, số phòng đặc biệt theo loại, cấu hình từng loại phòng
    /// (RoomTypeConfigSO), bảng quái + tỉ lệ Elite, boss, lootLuck, tile theo theme.
    /// Thứ tự level trong run do CampaignSO quyết định. Address: theme/&lt;id&gt;/config/floor-&lt;n&gt;.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelConfig_", menuName = "Dungeon Gathering/Level Config")]
    public class LevelConfigSO : ScriptableObject
    {
        [Header("Level")]
        [Tooltip("Theme của level; chỉ Editor dùng để đăng ký address theme/<themeId>/config/floor-<floor>.")]
        public string themeId = "dungeon";
        [Tooltip("Số thứ tự level (hiện \"Tầng N\" trên HUD)")] public int floor = 1;
        [Tooltip("Khóa localization tên level (hiện chưa được code runtime đọc).")]
        public string nameKey;

        [Header("Số phòng")]
        [Tooltip("Số phòng có quái (min, max) — không tính Start / Boss / phòng đặc biệt")]
        public Vector2Int combatRoomCount;
        [Tooltip("Phòng đặc biệt: mỗi phòng nằm ở cuối một nhánh")]
        public List<SpecialRoomCount> specialRooms = new();
        [Tooltip("Cấu hình theo loại phòng (template, đợt quái, Elite, rương). Phòng Elite thiếu cấu hình thì dùng template Combat")]
        public List<RoomTypeConfigSO> roomTypes = new();

        [Header("Độ khó & thưởng")]
        [Range(0, 1)] [Tooltip("Tỉ lệ một con quái thường sinh ra thành Elite")] public float eliteChance;
        [Tooltip("Dời bậc vật phẩm rơi lên (map càng cao càng lớn)")] public float lootLuck;

        [Header("Lưới map")]
        [Tooltip("Kích thước lưới ô phòng (số phòng tối đa theo chiều ngang / dọc)")] public Vector2Int gridSize = new(7, 7);
        [Tooltip("Khoảng cách giữa tâm 2 ô lưới (tile) — lớn hơn MaxRoomSize")] public int cellSpacing = 40;

        [Header("Tile theo theme")]
        [Tooltip("Tile sàn hành lang nối các phòng. Trống = không vẽ hành lang (kể cả tường).")]
        public TileBase corridorFloorTile;
        [Tooltip("Tile tường hai bên hành lang. Trống = hành lang không có tường.")]
        public TileBase corridorWallTile;
        [Tooltip("Tile bịt Door Socket không dùng")] public TileBase sealWallTile;
        [Min(1)] public int corridorWidth = 3;
        [Min(1)] public int sealDepth = 2;

        [Header("Quái")]
        [Tooltip("Bảng quái của level: chọn ngẫu nhiên theo weight trong số quái có spawnCost vừa ngân sách. Trống = không sinh quái.")]
        public List<EnemySpawnEntry> enemyTable = new();
        [Tooltip("Ngân sách điểm spawnCost của đợt đầu")] public int spawnBudget = 6;
        [Tooltip("Ngân sách tăng thêm mỗi đợt")] public int budgetGrowth = 2;
        [Tooltip("Số đợt quái mỗi phòng (min, max, gồm cả max); RoomTypeConfig có waves > 0 sẽ ghi đè.")]
        public Vector2Int wavesPerRoom = new(2, 3);
        [Tooltip("Hệ số nhân máu gốc mọi quái của level (nhân thêm với hệ số bậc Elite/Boss; tối thiểu 0.1).")]
        public float enemyHealthMultiplier = 1f;
        [Tooltip("Thời gian (giây) hiện dấu báo trước khi quái xuất hiện.")]
        public float spawnWarningTime = 1f;

        [Header("Boss")]
        [Tooltip("Address boss (tải nền qua Addressables, label boss-<id>)")]
        public string bossAddress;
        [Tooltip("Thời gian tối đa (giây) chờ boss tải nền khi vào phòng boss; quá hạn thì thử tải lại một lần.")]
        public float bossLoadTimeout = 20f;

        [Header("Thưởng")]
        [Range(0, 1)] public float chestOnClearChance = 0.25f;

        public GeneratorSettings ToGeneratorSettings(System.Random rng = null)
        {
            int lo = Mathf.Max(0, Mathf.Min(combatRoomCount.x, combatRoomCount.y));
            int hi = Mathf.Max(combatRoomCount.x, combatRoomCount.y);
            var s = new GeneratorSettings
            {
                GridSize = gridSize,
                CombatRooms = rng != null ? rng.Next(lo, hi + 1) : hi,
                Specials = new List<RoomType>(),
            };
            foreach (var sp in specialRooms)
                for (int i = 0; i < sp.count; i++)
                    if (sp.type != RoomType.Start && sp.type != RoomType.Boss && sp.type != RoomType.Combat) s.Specials.Add(sp.type);
            return s;
        }

        public RoomTypeConfigSO TypeConfig(RoomType t)
        {
            if (roomTypes != null) foreach (var c in roomTypes) if (c != null && c.roomType == t) return c;
            return null;
        }

        static readonly List<RoomTemplate> None = new();

        /// <summary>Template của loại phòng (RoomTypeConfigSO). Phòng Elite thiếu cấu hình → template Combat.</summary>
        public List<RoomTemplate> RoomsFor(RoomType t)
        {
            var cfg = TypeConfig(t);
            if (cfg != null && cfg.templates.Count > 0) return cfg.templates;
            if (t == RoomType.Elite) return RoomsFor(RoomType.Combat);
            if (t != RoomType.Combat) { var combat = TypeConfig(RoomType.Combat); if (combat != null && combat.templates.Count > 0) return combat.templates; }
            return None;
        }
    }
}
