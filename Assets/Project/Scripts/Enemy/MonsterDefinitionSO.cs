using System.Collections.Generic;
using DungeonGathering.Catalog;
using UnityEngine;

namespace DungeonGathering.Enemies
{
    /// <summary>
    /// Định nghĩa quái / boss: định danh, bậc (Normal / Elite / Boss), hình + animation, VFX đòn đánh, chỉ số
    /// (StatProfile + ghi đè), di chuyển, skill (SkillId), phase boss, rơi đồ. Quái thường dùng chung prefab Enemy_Base;
    /// định nghĩa áp hình / animator / kích thước lúc sinh.
    /// </summary>
    [CreateAssetMenu(fileName = "Enemy_", menuName = "Dungeon Gathering/Monster Definition")]
    public class MonsterDefinitionSO : ScriptableObject
    {
        [Tooltip("ID quái (chỉ dùng để nhận diện trong log / kiểm tra dữ liệu Editor).")]
        public string enemyId = "skeleton-spear";
        [Tooltip("Khóa localization tên quái; boss dùng để hiện tên trên thanh máu boss.")]
        public string nameKey;

        [Header("Hình")]
        [Tooltip("Animator controller của quái (trống = giữ của prefab)")] public RuntimeAnimatorController animator;
        [Tooltip("Sprite mặc định (trống = giữ của prefab)")] public Sprite sprite;
        [Tooltip("Kích thước (scale gốc của quái; bậc Elite nhân thêm)")] [Min(0.1f)] public float visualScale = 1f;

        [Header("VFX đòn đánh")]
        [Tooltip("Phát tại điểm ra đòn mỗi lần quái tung skill / đòn")] public GameObject attackVfx;
        [Tooltip("Phát trên mục tiêu mỗi khi đòn của quái trúng (Player)")] public GameObject hitVfx;

        [Header("Chạm & di chuyển")]
        [Tooltip("Sát thương khi chạm Player (nhân AttackDamage của chỉ số)")]
        public float contactDamage = 1f;
        [Tooltip("Thời gian chờ (giây) giữa 2 lần gây sát thương chạm lên Player.")]
        public float contactCooldown = 0.8f;
        [Tooltip("Bật = bay: bỏ qua NavMesh, di chuyển thẳng tới mục tiêu (xuyên chướng ngại).")]
        public bool isFlying;
        [Tooltip("Khoảng cách giữ với Player khi đuổi (quái bắn xa nên > 0)")]
        public float preferredDistance = 0.6f;
        [Tooltip("Nếu > 0 và khoảng cách tới mục tiêu nhỏ hơn mức này, quái sẽ lùi lại (dành cho quái bắn xa)")]
        public float retreatDistance = 0f;
        [Tooltip("Tầm phát hiện Player (world-unit) để bắt đầu đuổi; bỏ qua khi phòng đã kích hoạt (luôn đuổi).")]
        public float aggroRange = 12f;

        [Header("Chỉ số")]
        [Tooltip("StatProfile + ghi đè (máu, tốc chạy, kháng đẩy lùi…). AttackDamage = hệ số nhân sát thương mọi đòn")]
        public StatBlock stats = new();
        [Tooltip("Bậc quái: hệ số chỉ số theo MonsterTierConfig (Elite có thể do spawner đặt lúc sinh).")]
        public Core.MonsterTier tier = Core.MonsterTier.Normal;

        [Header("Sinh quái")]
        [Min(1)] public int spawnCost = 1;
        [Tooltip("Thời gian (giây) trạng thái xuất hiện sau khi sinh: chưa hành động, chưa gây sát thương chạm.")]
        public float spawnDuration = 0.4f;
        [Tooltip("Thời gian (giây) chờ animation chết trước khi thu hồi quái (tối thiểu 0.1).")]
        public float deathDuration = 0.8f;

        [Header("Tấn công")]
        [Tooltip("Skill / đòn (chọn trong SkillLibrary)")]
        public List<SkillId> skills = new();

        [Header("Boss Phases (tùy chọn)")]
        [Tooltip("Phase boss theo ngưỡng máu; có phần tử thì ghi đè phase trên BossPhaseController của prefab. Trống = dùng của prefab.")]
        public List<BossPhaseController.Phase> phases = new();

        [Header("Rơi đồ")]
        [Tooltip("Tắt (mặc định): rơi đồ theo bậc quái trong LootConfig. Bật: dùng bảng riêng bên dưới.")]
        public bool customLoot;
        [Tooltip("Bảng rơi riêng, chỉ dùng khi bật customLoot (hoặc khi chưa có LootConfig); tắt thì LootConfig theo bậc quyết định.")]
        public LootTable lootTable = new();

        /// <summary>Bộ đòn theo SkillId trong thư viện.</summary>
        public List<EnemyAttackSO> ResolveAttacks() => SkillRegistry.GetAll<EnemyAttackSO>(skills);

        static readonly BaseStats MonsterDefault = new()
        {
            maxHp = 40f, maxArmor = 0f, maxMana = 0f, attackDamage = 1f, attackSpeed = 1f, moveSpeed = 2.5f,
            critChance = 0f, critDamage = 2f, cooldownReduction = 0f, manaRegen = 0f, knockbackResist = 0f,
        };

        /// <summary>
        /// Chỉ số gốc của quái. AttackDamage = 1 là hệ số nhân lên sát thương từng đòn (bậc Elite/level tăng nó lên).
        /// </summary>
        public BaseStats ResolveStats() => stats != null ? stats.Resolve(MonsterDefault) : MonsterDefault;
    }
}
