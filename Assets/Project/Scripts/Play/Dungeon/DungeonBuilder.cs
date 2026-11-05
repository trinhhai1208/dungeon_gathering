using System.Collections.Generic;
using DungeonGathering.Combat;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DungeonGathering.Play
{
    /// <summary>
    /// Dựng DungeonLayout thành scene (ARCHITECTURE §2 bước 3–4): instantiate RoomTemplate tại cell × spacing,
    /// vẽ hành lang thẳng trục giữa 2 Door Socket đối diện, bịt socket không dùng bằng tile tường.
    /// Dùng được cả ở runtime (LevelDirector) và Editor (Dungeon Preview, director = null).
    /// </summary>
    public sealed class DungeonBuilder : MonoBehaviour
    {
        [SerializeField] Grid corridorGrid;
        [SerializeField] Tilemap corridorFloor;
        [SerializeField] Tilemap corridorWall;
        [SerializeField] Transform roomsRoot;

        readonly List<RoomController> rooms = new();
        readonly List<Tilemap> navFloors = new();
        readonly List<Tilemap> navWalls = new();
        DungeonNavMesh navMesh;
        public IReadOnlyList<RoomController> Rooms => rooms;
        public RoomController StartRoom { get; private set; }
        public RoomController BossRoom { get; private set; }

        public void Clear()
        {
            foreach (var r in rooms) if (r != null) DestroySafe(r.gameObject);
            rooms.Clear();
            if (corridorFloor != null) corridorFloor.ClearAllTiles();
            if (corridorWall != null) corridorWall.ClearAllTiles();
            StartRoom = BossRoom = null;
            if (navMesh != null) navMesh.Clear();
        }

        public void Build(DungeonLayout layout, DungeonConfig config, LevelDirector director, PlayPrefabSet prefabs)
        {
            Clear();
            EnsureCorridorMaps();
            var rng = new System.Random(layout.Seed ^ 0x5bd1e995);
            var byNode = new Dictionary<RoomNode, RoomTemplate>();

            foreach (var node in layout.Rooms)
            {
                var pool = config.RoomsFor(node.Type);
                if (pool == null || pool.Count == 0)
                {
                    Debug.LogError($"[Dungeon] DungeonConfig '{config.name}' không có RoomTemplate cho {node.Type}.");
                    continue;
                }
                var prefab = pool[rng.Next(pool.Count)];
                Vector3 pos = new(node.Cell.x * config.cellSpacing, node.Cell.y * config.cellSpacing, 0f);
                var t = Instantiate(prefab, pos, Quaternion.identity, roomsRoot != null ? roomsRoot : transform);
                t.name = $"Room_{node.Id}_{node.Type}";
                byNode[node] = t;

                var doors = new List<RoomDoor>();
                foreach (var d in DoorMaskExt.All)
                {
                    bool used = (node.Doors & d) != 0;
                    var door = GetDoor(t, d, used, prefabs);
                    if (used)
                    {
                        if (door != null) doors.Add(door);
                        OpenDoorway(t, d, config);
                    }
                    else
                    {
                        if (door != null) door.gameObject.SetActive(false);
                        Seal(t, d, config);
                    }
                }

                var rc = t.GetComponent<RoomController>();
                if (rc == null) rc = t.gameObject.AddComponent<RoomController>();
                rc.Setup(node, t, director, doors);
                SetupTriggerAndCamera(rc, t);
                rooms.Add(rc);
                if (node == layout.Start) StartRoom = rc;
                if (node == layout.Boss) BossRoom = rc;

                if (director != null && prefabs != null) PlaceContent(rc, t, prefabs, director);
            }

            foreach (var (a, b, dir) in layout.Links())
                if (byNode.TryGetValue(a, out var ta) && byNode.TryGetValue(b, out var tb))
                    DrawCorridor(ta.SocketPosition(dir), tb.SocketPosition(dir.Opposite()), dir, config);

            if (corridorWall != null)
            {
                var col = corridorWall.GetComponent<TilemapCollider2D>();
                if (col != null) col.ProcessTilemapChanges();
            }

            if (Application.isPlaying) BuildNavMesh();
        }

        /// <summary>Nướng NavMesh cho quái từ sàn/tường của mọi phòng + hành lang (sau khi đã mở cửa/bịt socket/vẽ hành lang).</summary>
        void BuildNavMesh()
        {
            if (navMesh == null && !TryGetComponent(out navMesh)) navMesh = gameObject.AddComponent<DungeonNavMesh>();
            navFloors.Clear();
            navWalls.Clear();
            foreach (var r in rooms)
            {
                if (r == null || r.Template == null) continue;
                if (r.Template.ground != null) navFloors.Add(r.Template.ground);
                if (r.Template.wall != null) navWalls.Add(r.Template.wall);
            }
            if (corridorFloor != null) navFloors.Add(corridorFloor);
            if (corridorWall != null) navWalls.Add(corridorWall);
            navMesh.Build(navFloors, navWalls);
        }

        static RoomDoor GetDoor(RoomTemplate t, DoorMask d, bool used, PlayPrefabSet prefabs)
        {
            int i = RoomTemplate.DoorIndex(d);
            RoomDoor door = null;
            if (t.doors != null && i >= 0 && i < t.doors.Length && t.doors[i] != null)
            {
                door = t.doors[i];
            }
            else if (used && prefabs != null && prefabs.door != null)
            {
                door = Instantiate(prefabs.door, t.SocketPosition(d), Quaternion.identity, t.transform);
                door.name = "Door_" + d;
            }
            if (door != null)
            {
                door.SetupOrientation(d);
            }
            return door;
        }

        void EnsureCorridorMaps()
        {
            if (corridorGrid == null)
            {
                corridorGrid = new GameObject("Corridors").AddComponent<Grid>();
                corridorGrid.transform.SetParent(transform, false);
            }
            if (corridorFloor == null) corridorFloor = CreateMap("CorridorFloor", false, -10);
            if (corridorWall == null) corridorWall = CreateMap("CorridorWall", true, -5);
        }

        Tilemap CreateMap(string n, bool collider, int order)
        {
            var go = new GameObject(n, typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(corridorGrid.transform, false);
            go.GetComponent<TilemapRenderer>().sortingOrder = order;
            if (collider)
            {
                int wall = CombatLayers.Layer(CombatLayers.Wall);
                if (wall >= 0) go.layer = wall;
                go.AddComponent<TilemapCollider2D>();
            }
            return go.GetComponent<Tilemap>();
        }

        void DrawCorridor(Vector3 from, Vector3 to, DoorMask dir, DungeonConfig config)
        {
            if (config.corridorFloorTile == null) return;
            var a = corridorFloor.WorldToCell(from);
            var b = corridorFloor.WorldToCell(to);
            int half = config.corridorWidth / 2;
            bool vertical = dir is DoorMask.North or DoorMask.South;
            if (vertical)
            {
                int x = a.x, y0 = Mathf.Min(a.y, b.y), y1 = Mathf.Max(a.y, b.y);
                for (int y = y0; y <= y1; y++)
                {
                    for (int dx = -half; dx <= half; dx++) corridorFloor.SetTile(new Vector3Int(x + dx, y, 0), config.corridorFloorTile);
                    if (config.corridorWallTile != null)
                    {
                        corridorWall.SetTile(new Vector3Int(x - half - 1, y, 0), config.corridorWallTile);
                        corridorWall.SetTile(new Vector3Int(x + half + 1, y, 0), config.corridorWallTile);
                    }
                }
            }
            else
            {
                int y = a.y, x0 = Mathf.Min(a.x, b.x), x1 = Mathf.Max(a.x, b.x);
                for (int x = x0; x <= x1; x++)
                {
                    for (int dy = -half; dy <= half; dy++) corridorFloor.SetTile(new Vector3Int(x, y + dy, 0), config.corridorFloorTile);
                    if (config.corridorWallTile != null)
                    {
                        corridorWall.SetTile(new Vector3Int(x, y - half - 1, 0), config.corridorWallTile);
                        corridorWall.SetTile(new Vector3Int(x, y + half + 1, 0), config.corridorWallTile);
                    }
                }
            }
        }

        static void OpenDoorway(RoomTemplate t, DoorMask d, DungeonConfig config)
        {
            if (t.wall == null) return;
            var center = t.wall.WorldToCell(t.SocketPosition(d));
            int half = config != null ? config.corridorWidth / 2 : 1;
            bool vertical = d is DoorMask.North or DoorMask.South;
            for (int k = -half; k <= half; k++)
            {
                var c = center + (vertical ? new Vector3Int(k, 0, 0) : new Vector3Int(0, k, 0));
                t.wall.SetTile(c, null);
            }
        }

        /// <summary>Bịt Door Socket không có hàng xóm bằng tile tường (vào Tilemap Wall của phòng).</summary>
        static void Seal(RoomTemplate t, DoorMask d, DungeonConfig config)
        {
            if (t.wall == null || config == null || config.sealWallTile == null) return;
            var center = t.wall.WorldToCell(t.SocketPosition(d));
            int half = config.corridorWidth / 2;
            bool vertical = d is DoorMask.North or DoorMask.South;
            for (int k = -half; k <= half; k++)
            {
                var c = center + (vertical ? new Vector3Int(k, 0, 0) : new Vector3Int(0, k, 0));
                if (t.wall.GetTile(c) == null)
                    t.wall.SetTile(c, config.sealWallTile);
            }
        }

        static void SetupTriggerAndCamera(RoomController rc, RoomTemplate t)
        {
            var trigger = t.roomTrigger;
            if (trigger == null)
            {
                var go = new GameObject("RoomTrigger");
                go.transform.SetParent(t.transform, false);
                var box = go.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(Mathf.Max(1, t.size.x - 3), Mathf.Max(1, t.size.y - 3)); // qua vạch cửa mới tính vào phòng
                trigger = box;
            }
            var relay = trigger.GetComponent<RoomTriggerRelay>();
            if (relay == null) relay = trigger.gameObject.AddComponent<RoomTriggerRelay>();
            relay.Room = rc;

            var cam = t.cameraBounds;
            if (cam == null)
            {
                var go = new GameObject("CameraBounds");
                go.transform.SetParent(t.transform, false);
                var poly = go.AddComponent<PolygonCollider2D>();
                poly.isTrigger = true;
                Vector2 h = (Vector2)t.size * 0.5f + Vector2.one; // dư 1 tile để thấy cửa
                poly.SetPath(0, new[] { new Vector2(-h.x, -h.y), new Vector2(h.x, -h.y), new Vector2(h.x, h.y), new Vector2(-h.x, h.y) });
                go.layer = 2; // Ignore Raycast
                cam = poly;
            }
            rc.CameraBounds = cam;
        }

        static void PlaceContent(RoomController rc, RoomTemplate t, PlayPrefabSet prefabs, LevelDirector director)
        {
            switch (rc.Type)
            {
                case RoomType.Treasure when prefabs.chest != null:
                    var chest = Instantiate(prefabs.chest, (t.chestPoint != null ? t.chestPoint : t.transform).position, Quaternion.identity, t.transform);
                    chest.Setup(director, ChestKind.Item);
                    break;
                case RoomType.Shop when prefabs.shopStand != null:
                    var stand = Instantiate(prefabs.shopStand, (t.shopPoint != null ? t.shopPoint : t.transform).position, Quaternion.identity, t.transform);
                    stand.Setup(director);
                    break;
            }
        }

        static void DestroySafe(Object o)
        {
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }
    }
}
