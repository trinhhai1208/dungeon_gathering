using System.Collections.Generic;
using DungeonGathering.Enemies;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Tilemaps;

namespace DungeonGathering.Play
{
    /// <summary>
    /// NavMesh cho quái của tầng hiện tại, nướng lúc runtime sau khi DungeonBuilder dựng xong.
    /// AI Navigation không tự gom nguồn 2D (Tilemap/Collider2D), nên nguồn được dựng tay:
    /// ô sàn (ground phòng + sàn hành lang) trừ ô tường (tường phòng, cột, tường hành lang) → gộp thành dải theo hàng
    /// → hộp NavMeshBuildSource nằm trên mặt phẳng XY. Dữ liệu được gắn qua NavMeshSurface (xoay -90° quanh X).
    /// Cửa phòng không đưa vào NavMesh: quái chỉ đuổi trong phòng đang khóa, cửa đóng chặn bằng collider như cũ.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DungeonNavMesh : MonoBehaviour
    {
        [Tooltip("Kích thước voxel khi nướng (nhỏ hơn = sát tường hơn, nướng lâu hơn)")]
        [SerializeField] float voxelSize = 0.1f;
        [SerializeField] float minRegionArea = 0.5f;

        NavMeshSurface surface;
        readonly HashSet<Vector2Int> floor = new();
        readonly HashSet<Vector2Int> walls = new();
        readonly Dictionary<int, List<int>> rows = new();
        readonly List<NavMeshBuildSource> sources = new();

        public bool HasData => surface != null && surface.navMeshData != null;

        public void Build(IEnumerable<Tilemap> floorMaps, IEnumerable<Tilemap> wallMaps)
        {
            Clear();
            floor.Clear();
            walls.Clear();
            foreach (var m in wallMaps) Collect(m, walls);
            foreach (var m in floorMaps) Collect(m, floor);
            floor.ExceptWith(walls);
            if (floor.Count == 0)
            {
                Debug.LogWarning("[Nav] Không có ô sàn nào để nướng NavMesh — quái sẽ đi thẳng.");
                return;
            }

            // Gộp ô liền kề trên cùng hàng thành dải để giảm số nguồn.
            rows.Clear();
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var c in floor)
            {
                if (!rows.TryGetValue(c.y, out var xs)) rows[c.y] = xs = new List<int>();
                xs.Add(c.x);
                if (c.x < minX) minX = c.x;
                if (c.x > maxX) maxX = c.x;
                if (c.y < minY) minY = c.y;
                if (c.y > maxY) maxY = c.y;
            }
            sources.Clear();
            foreach (var kv in rows)
            {
                var xs = kv.Value;
                xs.Sort();
                int start = xs[0], prev = xs[0];
                for (int i = 1; i < xs.Count; i++)
                {
                    if (xs[i] == prev + 1) { prev = xs[i]; continue; }
                    AddStrip(start, prev, kv.Key);
                    start = prev = xs[i];
                }
                AddStrip(start, prev, kv.Key);
            }

            var settings = NavMesh.GetSettingsByID(EnemyNavigation.AgentTypeId);
            settings.agentTypeID = EnemyNavigation.AgentTypeId;
            settings.agentRadius = EnemyNavigation.AgentRadius;
            settings.agentHeight = EnemyNavigation.AgentHeight;
            settings.overrideVoxelSize = true;
            settings.voxelSize = voxelSize;
            settings.minRegionArea = minRegionArea;

            // Không gian cục bộ của NavMesh: x = X thế giới, z = Y thế giới, y = độ cao (hướng -Z thế giới).
            var localBounds = new Bounds(
                new Vector3((minX + maxX + 1) * 0.5f, 0f, (minY + maxY + 1) * 0.5f),
                new Vector3(maxX - minX + 3, 4f, maxY - minY + 3));
            var rot = EnemyNavigation.PlaneRotation;
            var data = NavMeshBuilder.BuildNavMeshData(settings, sources, localBounds, Vector3.zero, rot);
            if (data == null)
            {
                Debug.LogError("[Nav] Nướng NavMesh thất bại — quái sẽ đi thẳng.");
                return;
            }
            data.name = "DungeonNavMesh";

            if (surface == null) surface = CreateSurface();
            surface.transform.SetPositionAndRotation(Vector3.zero, rot);
            surface.navMeshData = data;
            surface.AddData();
            EnemyNavigation.Ready = true;
        }

        public void Clear()
        {
            EnemyNavigation.Ready = false;
            if (surface == null) return;
            surface.RemoveData();
            var data = surface.navMeshData;
            surface.navMeshData = null;
            if (data != null)
            {
                if (Application.isPlaying) Destroy(data);
                else DestroyImmediate(data);
            }
        }

        void OnDestroy() => Clear();

        NavMeshSurface CreateSurface()
        {
            var go = new GameObject("EnemyNavMesh");
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<NavMeshSurface>();
            s.agentTypeID = EnemyNavigation.AgentTypeId;
            s.collectObjects = CollectObjects.Volume; // không tự gom — dữ liệu do Build() cấp
            s.size = Vector3.zero;
            return s;
        }

        /// <summary>Ô có tile → khóa ô nguyên trong không gian thế giới (lưới phòng/hành lang đều 1 đơn vị, căn nguyên).</summary>
        static void Collect(Tilemap map, HashSet<Vector2Int> into)
        {
            if (map == null) return;
            var b = map.cellBounds;
            foreach (var cell in b.allPositionsWithin)
            {
                if (!map.HasTile(cell)) continue;
                var w = map.GetCellCenterWorld(cell);
                into.Add(new Vector2Int(Mathf.FloorToInt(w.x), Mathf.FloorToInt(w.y)));
            }
        }

        void AddStrip(int x0, int x1, int y)
        {
            int w = x1 - x0 + 1;
            // Hộp dày 1 đơn vị nằm sau mặt phẳng (z ∈ [0, 1]) → mặt trên (local +y = -Z thế giới) ở z = 0.
            sources.Add(new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                transform = Matrix4x4.TRS(new Vector3(x0 + w * 0.5f, y + 0.5f, 0.5f), EnemyNavigation.PlaneRotation, Vector3.one),
                size = new Vector3(w, 1f, 1f),
                area = 0,
            });
        }
    }
}
