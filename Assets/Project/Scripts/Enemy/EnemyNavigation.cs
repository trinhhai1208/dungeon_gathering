using UnityEngine;
using UnityEngine.AI;

namespace DungeonGathering.Enemies
{
    /// <summary>
    /// Thông số dùng chung cho NavMesh 2D của quái (AI Navigation).
    /// NavMesh nằm trên mặt phẳng XY: dữ liệu được nướng với phép xoay <see cref="PlaneRotation"/>
    /// (trục "up" cục bộ của NavMesh → -Z thế giới), nên x/y của NavMesh trùng x/y thế giới, z = 0.
    /// Play (DungeonNavMesh) nướng lưới và bật <see cref="Ready"/>; EnemyMotor dùng NavMeshAgent để lấy hướng đi.
    /// </summary>
    public static class EnemyNavigation
    {
        /// <summary>Agent type dùng chung (Humanoid mặc định) — bán kính được ghi đè khi nướng.</summary>
        public const int AgentTypeId = 0;
        /// <summary>Bán kính quái trên NavMesh (≈ CircleCollider2D 0.4 của prefab, chừa khe hành lang 3 ô).</summary>
        public const float AgentRadius = 0.35f;
        public const float AgentHeight = 1f;

        public static readonly Quaternion PlaneRotation = Quaternion.Euler(-90f, 0f, 0f);

        /// <summary>Đã có NavMesh cho tầng hiện tại. Khi chưa có, mọi điểm coi như đi được và quái đi thẳng.</summary>
        public static bool Ready { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() => Ready = false;

        public static Vector3 ToNav(Vector2 world) => new(world.x, world.y, 0f);

        /// <summary>Điểm có nằm trên NavMesh (sai số maxDistance) không. Chưa có NavMesh → true.</summary>
        public static bool IsWalkable(Vector2 world, float maxDistance = 0.3f)
            => !Ready || NavMesh.SamplePosition(ToNav(world), out _, maxDistance, NavMesh.AllAreas);

        /// <summary>Kéo điểm về vị trí gần nhất trên NavMesh trong bán kính maxDistance.</summary>
        public static bool TrySnap(Vector2 world, float maxDistance, out Vector2 snapped)
        {
            snapped = world;
            if (!Ready) return true;
            if (!NavMesh.SamplePosition(ToNav(world), out var hit, maxDistance, NavMesh.AllAreas)) return false;
            snapped = hit.position;
            return true;
        }
    }
}
