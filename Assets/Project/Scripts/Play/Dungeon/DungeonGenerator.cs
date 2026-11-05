using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGathering.Play
{
    public enum RoomType { Start, Combat, Treasure, Shop, Boss }

    [Flags]
    public enum DoorMask { None = 0, North = 1, East = 2, South = 4, West = 8 }

    public static class DoorMaskExt
    {
        public static readonly DoorMask[] All = { DoorMask.North, DoorMask.East, DoorMask.South, DoorMask.West };

        public static Vector2Int ToOffset(this DoorMask d) => d switch
        {
            DoorMask.North => Vector2Int.up,
            DoorMask.East => Vector2Int.right,
            DoorMask.South => Vector2Int.down,
            DoorMask.West => Vector2Int.left,
            _ => Vector2Int.zero,
        };

        public static DoorMask Opposite(this DoorMask d) => d switch
        {
            DoorMask.North => DoorMask.South,
            DoorMask.South => DoorMask.North,
            DoorMask.East => DoorMask.West,
            DoorMask.West => DoorMask.East,
            _ => DoorMask.None,
        };

        public static DoorMask FromOffset(Vector2Int o)
        {
            if (o == Vector2Int.up) return DoorMask.North;
            if (o == Vector2Int.right) return DoorMask.East;
            if (o == Vector2Int.down) return DoorMask.South;
            if (o == Vector2Int.left) return DoorMask.West;
            return DoorMask.None;
        }
    }

    /// <summary>Một phòng trên lưới thô.</summary>
    public sealed class RoomNode
    {
        public int Id;
        public Vector2Int Cell;
        public RoomType Type = RoomType.Combat;
        public DoorMask Doors;
        public int Depth;          // khoảng cách (số phòng) từ Start
        public bool OnMainPath;
        public int BranchIndex = -1;
    }

    public sealed class DungeonLayout
    {
        public int Seed;
        public readonly List<RoomNode> Rooms = new();
        public readonly Dictionary<Vector2Int, RoomNode> ByCell = new();
        public RoomNode Start;
        public RoomNode Boss;

        public RoomNode At(Vector2Int c) => ByCell.TryGetValue(c, out var n) ? n : null;

        /// <summary>Các cặp phòng nối nhau (mỗi cạnh một lần, hướng từ a sang b).</summary>
        public IEnumerable<(RoomNode a, RoomNode b, DoorMask dir)> Links()
        {
            foreach (var r in Rooms)
                foreach (var d in new[] { DoorMask.North, DoorMask.East })
                    if ((r.Doors & d) != 0 && At(r.Cell + d.ToOffset()) is { } other)
                        yield return (r, other, d);
        }
    }

    /// <summary>Tham số sinh map (lấy từ DungeonConfig).</summary>
    public struct GeneratorSettings
    {
        public int MainPathLength;     // số phòng trên đường chính (gồm Start + Boss)
        public int BranchCount;
        public Vector2Int BranchLength; // min..max
        public Vector2Int GridSize;
        public bool Treasure;
        public bool Shop;
    }

    /// <summary>
    /// Sinh bố cục phòng theo seed (ARCHITECTURE §2): random walk đường chính + 1–2 nhánh phụ.
    /// Phòng lá xa Start nhất = Boss; cuối nhánh phụ = Treasure / Shop. Cùng seed → cùng map.
    /// Thuần C# (không phụ thuộc scene) để dùng chung cho runtime và Dungeon Preview.
    /// </summary>
    public static class DungeonGenerator
    {
        public static DungeonLayout Generate(int seed, GeneratorSettings s)
        {
            s.MainPathLength = Mathf.Max(2, s.MainPathLength);
            s.GridSize = new Vector2Int(Mathf.Max(3, s.GridSize.x), Mathf.Max(3, s.GridSize.y));
            var rng = new System.Random(seed);
            for (int attempt = 0; attempt < 64; attempt++)
            {
                var layout = TryGenerate(rng, s);
                if (layout != null) { layout.Seed = seed; return layout; }
            }
            Debug.LogWarning($"[Dungeon] Không sinh được map đủ điều kiện với seed {seed} — dùng đường thẳng.");
            return Fallback(seed, s);
        }

        static DungeonLayout TryGenerate(System.Random rng, GeneratorSettings s)
        {
            var layout = new DungeonLayout();
            var start = new Vector2Int(s.GridSize.x / 2, s.GridSize.y / 2);
            var startNode = Add(layout, start, 0, true);
            layout.Start = startNode;

            // Đường chính
            var cur = startNode;
            for (int i = 1; i < s.MainPathLength; i++)
            {
                var next = Step(layout, rng, cur.Cell, s.GridSize);
                if (next == null) return null;
                var node = Add(layout, next.Value, cur.Depth + 1, true);
                Link(cur, node);
                cur = node;
            }
            var mainEnd = cur;

            // Nhánh phụ (không mọc từ Start hay cuối đường chính)
            var mainRooms = new List<RoomNode>(layout.Rooms);
            for (int b = 0; b < s.BranchCount; b++)
            {
                bool grown = false;
                for (int tries = 0; tries < 12 && !grown; tries++)
                {
                    var from = mainRooms[rng.Next(1, Mathf.Max(2, mainRooms.Count - 1))];
                    if (from == mainEnd) continue;
                    int len = rng.Next(Mathf.Max(1, s.BranchLength.x), Mathf.Max(1, s.BranchLength.y) + 1);
                    var branch = new List<RoomNode>();
                    var c = from;
                    for (int i = 0; i < len; i++)
                    {
                        var next = Step(layout, rng, c.Cell, s.GridSize);
                        if (next == null) break;
                        var node = Add(layout, next.Value, c.Depth + 1, false);
                        node.BranchIndex = b;
                        Link(c, node);
                        branch.Add(node);
                        c = node;
                    }
                    grown = branch.Count > 0;
                }
            }

            // Boss = phòng lá xa Start nhất (ưu tiên trên đường chính khi bằng nhau)
            RoomNode boss = null;
            foreach (var r in layout.Rooms)
            {
                if (r == startNode || CountDoors(r) != 1) continue;
                if (boss == null || r.Depth > boss.Depth || (r.Depth == boss.Depth && r.OnMainPath && !boss.OnMainPath)) boss = r;
            }
            if (boss == null) return null;
            boss.Type = RoomType.Boss;
            layout.Boss = boss;
            startNode.Type = RoomType.Start;

            // Treasure / Shop ở cuối các nhánh
            var leaves = new List<RoomNode>();
            foreach (var r in layout.Rooms)
                if (r != boss && r != startNode && CountDoors(r) == 1) leaves.Add(r);
            leaves.Sort((x, y) => y.Depth.CompareTo(x.Depth));
            int li = 0;
            if (s.Treasure && li < leaves.Count) leaves[li++].Type = RoomType.Treasure;
            if (s.Shop && li < leaves.Count) leaves[li++].Type = RoomType.Shop;
            return layout;
        }

        static DungeonLayout Fallback(int seed, GeneratorSettings s)
        {
            var layout = new DungeonLayout { Seed = seed };
            RoomNode prev = null;
            for (int i = 0; i < s.MainPathLength; i++)
            {
                var n = Add(layout, new Vector2Int(i, 0), i, true);
                if (prev != null) Link(prev, n);
                prev = n;
            }
            layout.Start = layout.Rooms[0];
            layout.Start.Type = RoomType.Start;
            layout.Boss = prev;
            prev.Type = RoomType.Boss;
            return layout;
        }

        static RoomNode Add(DungeonLayout layout, Vector2Int cell, int depth, bool main)
        {
            var n = new RoomNode { Id = layout.Rooms.Count, Cell = cell, Depth = depth, OnMainPath = main };
            layout.Rooms.Add(n);
            layout.ByCell[cell] = n;
            return n;
        }

        static void Link(RoomNode a, RoomNode b)
        {
            var d = DoorMaskExt.FromOffset(b.Cell - a.Cell);
            a.Doors |= d;
            b.Doors |= d.Opposite();
        }

        static int CountDoors(RoomNode r)
        {
            int c = 0;
            foreach (var d in DoorMaskExt.All) if ((r.Doors & d) != 0) c++;
            return c;
        }

        /// <summary>Ô kề trống ngẫu nhiên; tránh ô có hàng xóm khác (giữ map dạng cây, hành lang không chạm nhau).</summary>
        static Vector2Int? Step(DungeonLayout layout, System.Random rng, Vector2Int from, Vector2Int grid)
        {
            var options = new List<Vector2Int>(4);
            foreach (var d in DoorMaskExt.All)
            {
                var c = from + d.ToOffset();
                if (c.x < 0 || c.y < 0 || c.x >= grid.x || c.y >= grid.y || layout.ByCell.ContainsKey(c)) continue;
                int neighbours = 0;
                foreach (var d2 in DoorMaskExt.All)
                    if (c + d2.ToOffset() != from && layout.ByCell.ContainsKey(c + d2.ToOffset())) neighbours++;
                if (neighbours == 0) options.Add(c);
            }
            if (options.Count == 0) return null;
            return options[rng.Next(options.Count)];
        }
    }
}
