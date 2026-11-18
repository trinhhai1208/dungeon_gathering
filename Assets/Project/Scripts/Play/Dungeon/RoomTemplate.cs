using System.Collections.Generic;
using DungeonGathering.Enemies;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DungeonGathering.Play
{
    [System.Serializable]
    public class FixedSpawn
    {
        public Enemy prefab;
        [Tooltip("Index trong spawnPoints; -1 = ngẫu nhiên")] public int spawnPointIndex = -1;
    }

    [System.Serializable]
    public class WaveDefinition
    {
        public List<FixedSpawn> spawns = new();
    }

    /// <summary>
    /// Component gốc của prefab phòng (ARCHITECTURE §2). Gốc phòng = tâm phòng; Door Socket nằm chính giữa mỗi cạnh
    /// (x = 0 với N/S, y = 0 với E/W) để hành lang giữa 2 ô kề luôn thẳng trục.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomTemplate : MonoBehaviour
    {
        public const int MaxRoomSize = 32;

        [Header("Loại & kích thước (tile)")]
        public RoomType roomType = RoomType.Combat;
        public Vector2Int size = new(20, 14);

        [Header("Tilemaps")]
        public Tilemap ground;
        public Tilemap wall;
        public Tilemap decor;
        public Tilemap traps;

        [Header("Door sockets")]
        public Transform doorNorth;
        public Transform doorEast;
        public Transform doorSouth;
        public Transform doorWest;
        [Tooltip("RoomDoor đặt sẵn ở từng socket (tùy chọn; thiếu → builder tạo từ PlayPrefabSet.door)")]
        public RoomDoor[] doors = new RoomDoor[4];

        [Header("Điểm đặt")]
        public List<Transform> spawnPoints = new();
        public Transform playerSpawn;
        public Transform chestPoint;
        public Transform shopPoint;
        public Transform bossPoint;
        public Transform portalPoint;

        [Header("Sinh quái")]
        public bool useSpawnBudget = true;
        public List<WaveDefinition> fixedWaves = new();

        [Header("Camera & trigger (tùy chọn — thiếu thì builder tự tạo theo size)")]
        public Collider2D cameraBounds;
        public Collider2D roomTrigger;

        public Transform GetSocket(DoorMask d) => d switch
        {
            DoorMask.North => doorNorth,
            DoorMask.East => doorEast,
            DoorMask.South => doorSouth,
            DoorMask.West => doorWest,
            _ => null,
        };

        public Vector3 SocketPosition(DoorMask d)
        {
            var s = GetSocket(d);
            if (s != null) return s.position;
            Vector2 half = (Vector2)size * 0.5f;
            Vector2 off = d switch
            {
                DoorMask.North => new Vector2(0, half.y),
                DoorMask.East => new Vector2(half.x, 0),
                DoorMask.South => new Vector2(0, -half.y),
                DoorMask.West => new Vector2(-half.x, 0),
                _ => Vector2.zero,
            };
            return transform.position + (Vector3)off;
        }

        public static int DoorIndex(DoorMask d) => d switch
        {
            DoorMask.North => 0, DoorMask.East => 1, DoorMask.South => 2, DoorMask.West => 3, _ => -1,
        };

        /// <summary>
        /// Vùng phòng trong thế giới — lấy từ tilemap tường thật (tile vẽ lệch nửa ô và rộng hơn size 1 ô, nên dùng size
        /// sẽ hụt mép phải / trên → loot rơi sát tường bị coi là ngoài phòng). Không có tilemap thì dùng size.
        /// </summary>
        public Bounds WorldBounds
        {
            get
            {
                if (wall != null && wall.cellBounds.size.x > 0 && wall.cellBounds.size.y > 0)
                {
                    var cb = wall.cellBounds;
                    Vector3 min = wall.CellToWorld(cb.min), max = wall.CellToWorld(cb.max);
                    var b = new Bounds((min + max) * 0.5f, new Vector3(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y), 1f));
                    return b;
                }
                return new Bounds(transform.position, new Vector3(size.x, size.y, 1f));
            }
        }

        /// <summary>Kiểm tra quy chuẩn (Room Builder / Validate). Trả danh sách lỗi.</summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            if (size.x > MaxRoomSize || size.y > MaxRoomSize) errors.Add($"Kích thước {size} vượt MaxRoomSize {MaxRoomSize}.");
            if (wall == null) errors.Add("Thiếu Tilemap Wall.");
            else if (wall.GetComponent<TilemapCollider2D>() == null) errors.Add("Tilemap Wall thiếu TilemapCollider2D.");
            foreach (var d in DoorMaskExt.All)
            {
                var s = GetSocket(d);
                if (s == null) { errors.Add($"Thiếu Door Socket {d}."); continue; }
                Vector3 local = s.position - transform.position;
                bool ns = d is DoorMask.North or DoorMask.South;
                if (ns ? Mathf.Abs(local.x) > 0.01f : Mathf.Abs(local.y) > 0.01f)
                    errors.Add($"Door Socket {d} không nằm chính giữa cạnh ({(ns ? "x" : "y")} phải = 0).");
            }
            if (roomType == RoomType.Combat && spawnPoints.Count == 0) errors.Add("Phòng Combat cần ít nhất 1 SpawnPoint.");
            return errors;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 0f));
            Gizmos.color = Color.yellow;
            foreach (var d in DoorMaskExt.All) Gizmos.DrawWireSphere(SocketPosition(d), 0.5f);
            Gizmos.color = Color.red;
            foreach (var p in spawnPoints) if (p != null) Gizmos.DrawWireSphere(p.position, 0.35f);
            if (playerSpawn != null) { Gizmos.color = Color.green; Gizmos.DrawWireSphere(playerSpawn.position, 0.4f); }
        }
#endif
    }
}
