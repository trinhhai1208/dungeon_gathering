using System;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Enemies
{
    /// <summary>Phòng truyền cho quái lúc spawn: gọi quái con, giới hạn (tìm đường dùng NavMesh — EnemyNavigation).</summary>
    public sealed class EnemySpawnContext
    {
        /// <summary>Phòng spawn + đăng ký quái con vào tập alive. Null → quái con không được gọi.</summary>
        /// <summary>Gọi quái con theo định nghĩa (phòng sinh bằng prefab nền dùng chung + đăng ký vào tập alive).</summary>
        public Func<MonsterDefinitionSO, Vector2, Enemy> Summon;
        public int RoomId = -1;
        /// <summary>Hệ số độ khó (Tiếp tục tầng kế: HP/sát thương tăng).</summary>
        public float HealthMultiplier = 1f;
        /// <summary>Ép bậc cho lần sinh này (spawner tung Elite). Null → dùng MonsterDefinitionSO.tier.</summary>
        public Core.MonsterTier? TierOverride;

        public static readonly EnemySpawnContext Empty = new();
    }

    /// <summary>Phát khi quái chết (một lần). Play nghe để rơi loot + đếm kill.</summary>
    public struct EnemyDiedEvent : IEvent
    {
        public Enemy Enemy;
        public MonsterDefinitionSO Data;
        public Vector3 Position;
        public bool IsBoss;
        public Core.MonsterTier Tier;
    }
}
