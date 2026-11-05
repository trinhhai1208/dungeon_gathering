using System;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Enemies
{
    /// <summary>Phòng truyền cho quái lúc spawn: gọi quái con, giới hạn (tìm đường dùng NavMesh — EnemyNavigation).</summary>
    public sealed class EnemySpawnContext
    {
        /// <summary>Phòng spawn + đăng ký quái con vào tập alive. Null → quái con không được gọi.</summary>
        public Func<Enemy, Vector2, Enemy> Summon;
        public int RoomId = -1;
        /// <summary>Hệ số độ khó (Tiếp tục tầng kế: HP/sát thương tăng).</summary>
        public float HealthMultiplier = 1f;

        public static readonly EnemySpawnContext Empty = new();
    }

    /// <summary>Phát khi quái chết (một lần). Play nghe để rơi loot + đếm kill.</summary>
    public struct EnemyDiedEvent : IEvent
    {
        public Enemy Enemy;
        public EnemyData Data;
        public Vector3 Position;
        public bool IsBoss;
    }
}
