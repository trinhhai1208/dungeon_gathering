using System.Collections.Generic;
using DungeonGathering.Catalog;
using UnityEngine;

namespace DungeonGathering.Enemies
{
    /// <summary>Dữ liệu quái (ENTITIES §3.2). Prefab Enemy_&lt;Name&gt; gán sẵn data này.</summary>
    [CreateAssetMenu(fileName = "Enemy_", menuName = "Dungeon Gathering/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        public string enemyId = "skeleton-spear";
        public string nameKey;

        [Header("Chỉ số")]
        public float maxHp = 40f;
        public float moveSpeed = 2.5f;
        [Tooltip("Sát thương khi chạm Player (HP Player nhỏ, kiểu Soul Knight)")]
        public float contactDamage = 1f;
        public float contactCooldown = 0.8f;
        public bool isFlying;
        [Range(0, 1)] public float knockbackResist;
        [Tooltip("Khoảng cách giữ với Player khi đuổi (quái bắn xa nên > 0)")]
        public float preferredDistance = 0.6f;
        [Tooltip("Nếu > 0 và khoảng cách tới mục tiêu nhỏ hơn mức này, quái sẽ lùi lại (dành cho quái bắn xa)")]
        public float retreatDistance = 0f;
        public float aggroRange = 12f;

        [Header("Sinh quái")]
        [Min(1)] public int spawnCost = 1;
        public float spawnDuration = 0.4f;
        public float deathDuration = 0.8f;

        [Header("Tấn công")]
        public List<EnemyAttackSO> attacks = new();

        [Header("Rơi đồ")]
        public LootTable lootTable = new();
    }
}
