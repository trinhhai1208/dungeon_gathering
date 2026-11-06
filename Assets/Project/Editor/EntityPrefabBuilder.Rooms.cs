using System.Collections.Generic;
using System.Linq;
using DungeonGathering.Play;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DungeonGathering.EditorTools
{
    public static partial class EntityPrefabBuilder
    {
        // ==========================================
        // 0. TILESETS & DUNGEON TILES
        // ==========================================
        static Sprite GetTilesetSprite(int cx, int cy)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TilesetTexturePath);
            if (tex == null) return null;
            var p = new Vector2(cx * 16 + 8, tex.height - (cy * 16 + 8));
            return AssetDatabase.LoadAllAssetsAtPath(TilesetTexturePath).OfType<Sprite>().FirstOrDefault(s => s.rect.Contains(p));
        }

        static Tile GetOrCreateTile(string fileName, int cx, int cy, Tile.ColliderType colliderType)
        {
            string path = $"{TilesDir}/{fileName}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }
            tile.sprite = GetTilesetSprite(cx, cy);
            tile.color = Color.white;
            tile.colliderType = colliderType;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        static Dictionary<(int cx, int cy), Tile> BuildDungeonTiles()
        {
            var map = new Dictionary<(int cx, int cy), Tile>();
            // Tạo các tile sàn (cx: 6..9, cy: 0..2)
            for (int cy = 0; cy <= 2; cy++)
                for (int cx = 6; cx <= 9; cx++)
                    map[(cx, cy)] = GetOrCreateTile($"Tile_Floor_{cx}_{cy}", cx, cy, Tile.ColliderType.None);

            // Tạo các tile tường viền & góc (cx: 0..5, cy: 0..5)
            for (int cy = 0; cy <= 5; cy++)
                for (int cx = 0; cx <= 5; cx++)
                    map[(cx, cy)] = GetOrCreateTile($"Tile_Wall_{cx}_{cy}", cx, cy, Tile.ColliderType.Grid);

            // Tạo các tile trang trí trên sàn/tường
            (int cx, int cy, string name)[] decors =
            {
                (8, 3, "Crate"), (8, 6, "Bones"), (7, 7, "SkullBone"), (4, 7, "Banner"),
                (9, 4, "Rubble1"), (9, 5, "Rubble2"), (2, 9, "Brazier"), (5, 7, "Chalice"),
                (4, 6, "WebL"), (5, 6, "WebR"), (6, 6, "GateL"), (7, 6, "GateR")
            };
            foreach (var (cx, cy, name) in decors)
                map[(cx, cy)] = GetOrCreateTile($"Tile_Decor_{name}", cx, cy, Tile.ColliderType.None);

            // Tạo 3 tile chuẩn cho DungeonConfig (corridorFloorTile, corridorWallTile, sealWallTile)
            GetOrCreateTile("Dungeon_Floor", 6, 0, Tile.ColliderType.None);
            GetOrCreateTile("Dungeon_Wall", 1, 0, Tile.ColliderType.Grid);
            GetOrCreateTile("Dungeon_SealWall", 1, 5, Tile.ColliderType.Grid);

            return map;
        }

        // ==========================================
        // 5. ROOM TEMPLATES (ENTITIES §3.5.3)
        // ==========================================
        public static void BuildRoomTemplates(Dictionary<(int cx, int cy), Tile> tiles)
        {
            BuildSingleRoom(RoomTemplatePath, "RoomTemplate", RoomType.Combat, new Vector2Int(20, 14), tiles, true, true);
            BuildSingleRoom("Assets/Project/Prefabs/Rooms/Room_Start.prefab", "Room_Start", RoomType.Start, new Vector2Int(20, 14), tiles, false, false);
            BuildSingleRoom("Assets/Project/Prefabs/Rooms/Room_Combat.prefab", "Room_Combat", RoomType.Combat, new Vector2Int(20, 14), tiles, true, true);
            BuildSingleRoom("Assets/Project/Prefabs/Rooms/Room_Treasure.prefab", "Room_Treasure", RoomType.Treasure, new Vector2Int(20, 14), tiles, false, false);
            BuildSingleRoom("Assets/Project/Prefabs/Rooms/Room_Shop.prefab", "Room_Shop", RoomType.Shop, new Vector2Int(20, 14), tiles, false, false);
            BuildSingleRoom("Assets/Project/Prefabs/Rooms/Room_Boss.prefab", "Room_Boss", RoomType.Boss, new Vector2Int(24, 18), tiles, true, true);
        }

        static void BuildSingleRoom(string path, string name, RoomType type, Vector2Int size,
            Dictionary<(int cx, int cy), Tile> tiles, bool addObstacles, bool addTraps)
        {
            var root = new GameObject(name);
            var template = root.AddComponent<RoomTemplate>();
            template.roomType = type;
            template.size = size;

            // Grid
            var gridGo = new GameObject("Grid");
            gridGo.transform.SetParent(root.transform, false);
            gridGo.AddComponent<Grid>();

            // Ground
            var groundGo = new GameObject("Ground");
            groundGo.transform.SetParent(gridGo.transform, false);
            template.ground = groundGo.AddComponent<Tilemap>();
            var groundRend = groundGo.AddComponent<TilemapRenderer>();
            groundRend.sortingLayerName = "Default";
            groundRend.sortingOrder = -10;

            // Wall
            var wallGo = new GameObject("Wall");
            wallGo.transform.SetParent(gridGo.transform, false);
            wallGo.layer = LayerMask.NameToLayer("Wall");
            template.wall = wallGo.AddComponent<Tilemap>();
            var wallRend = wallGo.AddComponent<TilemapRenderer>();
            wallRend.sortingLayerName = "Default";
            wallRend.sortingOrder = -5;
            wallGo.AddComponent<TilemapCollider2D>();

            // Decor
            var decorGo = new GameObject("Decor");
            decorGo.transform.SetParent(gridGo.transform, false);
            template.decor = decorGo.AddComponent<Tilemap>();
            var decorRend = decorGo.AddComponent<TilemapRenderer>();
            decorRend.sortingLayerName = "Default";
            decorRend.sortingOrder = -2;

            // Traps
            var trapsGo = new GameObject("Traps");
            trapsGo.transform.SetParent(gridGo.transform, false);
            trapsGo.layer = LayerMask.NameToLayer("Trap");
            template.traps = trapsGo.AddComponent<Tilemap>();
            var trapsRend = trapsGo.AddComponent<TilemapRenderer>();
            trapsRend.sortingLayerName = "Default";
            trapsRend.sortingOrder = -1;

            int hx = size.x / 2;
            int hy = size.y / 2;

            // Vẽ sàn & tường (chừa 4 khe cửa rộng 3 ô [-1..1] ở chính giữa mỗi cạnh để khớp hành lang và Seal)
            var rng = new System.Random(name.GetHashCode());
            (int cx, int cy)[] floorCoords =
            {
                (6, 0), (7, 0), (8, 0), (9, 0),
                (6, 1), (7, 1), (8, 1), (9, 1),
                (6, 2), (7, 2), (8, 2), (9, 2)
            };

            for (int y = -hy; y <= hy; y++)
            {
                for (int x = -hx; x <= hx; x++)
                {
                    var fCoord = rng.NextDouble() < 0.6 ? (6, 0) : floorCoords[rng.Next(floorCoords.Length)];
                    if (tiles.TryGetValue(fCoord, out var ft))
                        template.ground.SetTile(new Vector3Int(x, y, 0), ft);

                    bool isTop = y == hy;
                    bool isBot = y == -hy;
                    bool isLeft = x == -hx;
                    bool isRight = x == hx;
                    if (!isTop && !isBot && !isLeft && !isRight) continue;

                    (int cx, int cy) wCoord;
                    if (isTop && isLeft) wCoord = (0, 0);
                    else if (isTop && isRight) wCoord = (5, 0);
                    else if (isBot && isLeft) wCoord = (0, 4);
                    else if (isBot && isRight) wCoord = (5, 4);
                    else if (isTop) wCoord = (1 + Mathf.Abs(x) % 4, 0);
                    else if (isBot) wCoord = (1 + Mathf.Abs(x) % 4, 4);
                    else if (isLeft) wCoord = (0, 1 + Mathf.Abs(y) % 3);
                    else wCoord = (5, 1 + Mathf.Abs(y) % 3);

                    if (tiles.TryGetValue(wCoord, out var wt))
                        template.wall.SetTile(new Vector3Int(x, y, 0), wt);
                }
            }

            // Vật cản nội bộ (tile tường → bị trừ khỏi NavMesh của quái, quái đi vòng)
            if (addObstacles && tiles.TryGetValue((1, 5), out var blockTile))
            {
                int ox = hx - 5;
                int oy = hy - 4;
                Vector2Int[] pillars = { new(-ox, oy), new(ox - 1, oy), new(-ox, -oy), new(ox - 1, -oy) };
                foreach (var p in pillars)
                {
                    template.wall.SetTile(new Vector3Int(p.x, p.y, 0), blockTile);
                    template.wall.SetTile(new Vector3Int(p.x + 1, p.y, 0), blockTile);
                }
            }

            // Trang trí (Decor tile + Prop Đuốc/Bình)
            if (tiles.TryGetValue((8, 6), out var bones))
                template.decor.SetTile(new Vector3Int(-hx + 3, -hy + 2, 0), bones);
            if (tiles.TryGetValue((9, 4), out var rubble))
                template.decor.SetTile(new Vector3Int(hx - 3, hy - 3, 0), rubble);

            var torchWall = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Prefabs/Rooms/Props/Prop_Torch_Wall.prefab");
            var vaseProp = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Prefabs/Rooms/Props/Prop_Vase_Shining.prefab");
            if (torchWall != null)
            {
                foreach (var tx in new[] { -4f, 4f })
                {
                    var tGo = (GameObject)PrefabUtility.InstantiatePrefab(torchWall, decorGo.transform);
                    tGo.transform.localPosition = new Vector3(tx, hy - 1.2f, 0f);
                }
            }
            if (vaseProp != null && type is RoomType.Start or RoomType.Treasure or RoomType.Shop)
            {
                foreach (var vx in new[] { -hx + 2.5f, hx - 2.5f })
                {
                    var vGo = (GameObject)PrefabUtility.InstantiatePrefab(vaseProp, decorGo.transform);
                    vGo.transform.localPosition = new Vector3(vx, hy - 2.2f, 0f);
                }
            }

            // Đặt Bẫy sàn trong phòng Combat / Boss
            if (addTraps)
            {
                var spikeTimed = AssetDatabase.LoadAssetAtPath<GameObject>($"{TrapsDir}/Trap_SpikeTimed.prefab");
                var spikeTrigger = AssetDatabase.LoadAssetAtPath<GameObject>($"{TrapsDir}/Trap_SpikeTrigger.prefab");
                if (spikeTimed != null)
                {
                    foreach (var pos in new[] { new Vector3(-2.5f, 0f, 0f), new Vector3(2.5f, 0f, 0f) })
                    {
                        var tr = (GameObject)PrefabUtility.InstantiatePrefab(spikeTimed, trapsGo.transform);
                        tr.transform.localPosition = pos;
                    }
                }
                if (spikeTrigger != null && type == RoomType.Boss)
                {
                    foreach (var pos in new[] { new Vector3(0f, -3.5f, 0f), new Vector3(0f, 3.5f, 0f) })
                    {
                        var tr = (GameObject)PrefabUtility.InstantiatePrefab(spikeTrigger, trapsGo.transform);
                        tr.transform.localPosition = pos;
                    }
                }
            }

            // DoorSockets chính giữa các cạnh (x=0 với N/S, y=0 với E/W)
            var doorSocketsGo = new GameObject("DoorSockets");
            doorSocketsGo.transform.SetParent(root.transform, false);

            var dn = new GameObject("DoorNorth");
            dn.transform.SetParent(doorSocketsGo.transform, false);
            dn.transform.localPosition = new Vector3(0.5f, hy, 0f);
            template.doorNorth = dn.transform;

            var de = new GameObject("DoorEast");
            de.transform.SetParent(doorSocketsGo.transform, false);
            de.transform.localPosition = new Vector3(hx, 0.5f, 0f);
            template.doorEast = de.transform;

            var ds = new GameObject("DoorSouth");
            ds.transform.SetParent(doorSocketsGo.transform, false);
            ds.transform.localPosition = new Vector3(0.5f, -hy, 0f);
            template.doorSouth = ds.transform;

            var dw = new GameObject("DoorWest");
            dw.transform.SetParent(doorSocketsGo.transform, false);
            dw.transform.localPosition = new Vector3(-hx, 0.5f, 0f);
            template.doorWest = dw.transform;

            // Points
            var pointsGo = new GameObject("Points");
            pointsGo.transform.SetParent(root.transform, false);

            var pSpawn = new GameObject("PlayerSpawn");
            pSpawn.transform.SetParent(pointsGo.transform, false);
            template.playerSpawn = pSpawn.transform;

            var cPoint = new GameObject("ChestPoint");
            cPoint.transform.SetParent(pointsGo.transform, false);
            cPoint.transform.localPosition = type == RoomType.Treasure ? Vector3.zero : new Vector3(0f, 2f, 0f);
            template.chestPoint = cPoint.transform;

            var sPoint = new GameObject("ShopPoint");
            sPoint.transform.SetParent(pointsGo.transform, false);
            template.shopPoint = sPoint.transform;

            var bPoint = new GameObject("BossPoint");
            bPoint.transform.SetParent(pointsGo.transform, false);
            bPoint.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            template.bossPoint = bPoint.transform;

            var portalPoint = new GameObject("PortalPoint");
            portalPoint.transform.SetParent(pointsGo.transform, false);
            portalPoint.transform.localPosition = new Vector3(0f, 3.5f, 0f);
            template.portalPoint = portalPoint.transform;

            // SpawnPoints (4 điểm góc trong phòng)
            Vector2[] spawnOffsets = { new(-hx + 4f, hy - 3.5f), new(hx - 4f, hy - 3.5f), new(-hx + 4f, -hy + 3.5f), new(hx - 4f, -hy + 3.5f) };
            for (int i = 0; i < spawnOffsets.Length; i++)
            {
                var sp = new GameObject($"SpawnPoint_{i}");
                sp.transform.SetParent(pointsGo.transform, false);
                sp.transform.localPosition = (Vector3)spawnOffsets[i];
                template.spawnPoints.Add(sp.transform);
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }
    }
}
