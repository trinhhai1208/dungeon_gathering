using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGathering.Play
{
    /// <summary>Loại phòng. Elite = phòng quái mà mọi con sinh ra là Elite. Thêm mới luôn nối vào cuối.</summary>
    public enum RoomType { Start, Combat, Treasure, Shop, Boss, Elite }

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

    /// <summary>Tham số sinh map (LevelConfigSO.ToGeneratorSettings).</summary>
    public struct GeneratorSettings
    {
        public Vector2Int GridSize;
        /// <summary>Số phòng có quái chính xác (không tính Start / Boss / phòng đặc biệt).</summary>
        public int CombatRooms;
        /// <summary>Các phòng đặc biệt, mỗi phòng ở cuối một nhánh riêng.</summary>
        public List<RoomType> Specials;
    }

    /// <summary>
    /// Sinh bố cục phòng theo seed: đường chính Start → phòng Combat → Boss, mỗi phòng đặc biệt ở cuối một nhánh riêng,
    /// đúng số phòng theo cấu hình. Cùng seed → cùng map. Thuần C# (không phụ thuộc scene), có EditMode test.
    /// </summary>
    public static class DungeonGenerator
    {
        public static DungeonLayout Generate(int seed, GeneratorSettings s)
        {
            s.GridSize = new Vector2Int(Mathf.Max(3, s.GridSize.x), Mathf.Max(3, s.GridSize.y));
            var rng = new System.Random(seed);
            for (int attempt = 0; attempt < 64; attempt++)
            {
                var layout = TryGenerateCounts(rng, s);
                if (layout != null) { layout.Seed = seed; return layout; }
            }
            Debug.LogWarning($"[Dungeon] Không sinh được map đủ điều kiện với seed {seed} — dùng đường thẳng.");
            return FallbackCounts(seed, s);
        }

        /// <summary>
        /// Đúng <see cref="GeneratorSettings.CombatRooms"/> phòng Combat + mỗi phòng đặc biệt ở cuối một nhánh riêng.
        /// Đường chính: Start → ~60% phòng Combat → Boss (lá cuối đường chính). Phần Combat còn lại chia vào các nhánh
        /// (nhánh có phòng đặc biệt thì phòng đặc biệt ở cuối; thừa Combat thì thêm nhánh cụt).
        /// </summary>
        static DungeonLayout TryGenerateCounts(System.Random rng, GeneratorSettings s)
        {
            int combat = Mathf.Max(0, s.CombatRooms);
            var specials = s.Specials ?? new List<RoomType>();
            int mainCombat = specials.Count == 0 && combat <= 3 ? combat : Mathf.Clamp(Mathf.CeilToInt(combat * 0.6f), Mathf.Min(1, combat), combat);
            int restCombat = combat - mainCombat;

            var layout = new DungeonLayout();
            var startNode = Add(layout, new Vector2Int(s.GridSize.x / 2, s.GridSize.y / 2), 0, true);
            startNode.Type = RoomType.Start;
            layout.Start = startNode;

            var cur = startNode;
            for (int i = 0; i <= mainCombat; i++)            // mainCombat phòng Combat + 1 phòng Boss
            {
                var next = Step(layout, rng, cur.Cell, s.GridSize);
                if (next == null) return null;
                var node = Add(layout, next.Value, cur.Depth + 1, true);
                Link(cur, node);
                cur = node;
            }
            cur.Type = RoomType.Boss;
            layout.Boss = cur;
            var mainRooms = new List<RoomNode>(layout.Rooms);
            mainRooms.Remove(cur);                              // không mọc nhánh từ phòng Boss

            // Kế hoạch nhánh: (số Combat trên nhánh, loại phòng cuối nhánh — Combat nếu là nhánh cụt)
            var plans = new List<(int combat, RoomType end)>();
            foreach (var sp in specials) plans.Add((0, sp));
            if (plans.Count == 0 && restCombat > 0)
                for (int i = 0, n = Mathf.CeilToInt(restCombat / 3f); i < n; i++) plans.Add((0, RoomType.Combat)); // nhánh cụt ≤ ~3 phòng
            for (int b = 0; restCombat > 0; b = (b + 1) % plans.Count)
            {
                plans[b] = (plans[b].combat + 1, plans[b].end);
                restCombat--;
            }

            for (int p = 0; p < plans.Count; p++)
            {
                var (branchCombat, end) = plans[p];
                bool grown = false;
                for (int tries = 0; tries < 16 && !grown; tries++)
                    grown = TryGrowExactBranch(layout, rng, s, mainRooms[rng.Next(mainRooms.Count)], p, branchCombat, end);
                for (int i = 0; i < mainRooms.Count && !grown; i++)
                    grown = TryGrowExactBranch(layout, rng, s, mainRooms[i], p, branchCombat, end);
                if (!grown) return null;
            }
            return layout;
        }

        /// <summary>Mọc nhánh đủ <paramref name="combat"/> phòng Combat rồi phòng <paramref name="end"/>; thất bại thì gỡ hết.</summary>
        static bool TryGrowExactBranch(DungeonLayout layout, System.Random rng, GeneratorSettings s, RoomNode from,
                                       int branchIndex, int combat, RoomType end)
        {
            if (from == null) return false;
            var added = new List<RoomNode>();
            var c = from;
            int total = end == RoomType.Combat ? combat : combat + 1;
            for (int i = 0; i < total; i++)
            {
                var next = Step(layout, rng, c.Cell, s.GridSize);
                if (next == null)
                {
                    Rollback(layout, from, added);
                    return false;
                }
                var node = Add(layout, next.Value, c.Depth + 1, false);
                node.BranchIndex = branchIndex;
                Link(c, node);
                added.Add(node);
                c = node;
            }
            if (end != RoomType.Combat) c.Type = end;
            return true;
        }

        static void Rollback(DungeonLayout layout, RoomNode from, List<RoomNode> added)
        {
            for (int i = added.Count - 1; i >= 0; i--)
            {
                var n = added[i];
                var parent = i == 0 ? from : added[i - 1];
                var d = DoorMaskExt.FromOffset(n.Cell - parent.Cell);
                parent.Doors &= ~d;
                layout.ByCell.Remove(n.Cell);
                layout.Rooms.Remove(n);
            }
            for (int i = 0; i < layout.Rooms.Count; i++) layout.Rooms[i].Id = i;
        }

        /// <summary>Dự phòng hệ mới: đường thẳng Start → Combat… → Boss, phòng đặc biệt treo phía trên (cách cột).</summary>
        static DungeonLayout FallbackCounts(int seed, GeneratorSettings s)
        {
            var layout = new DungeonLayout { Seed = seed };
            int combat = Mathf.Max(0, s.CombatRooms);
            var specials = s.Specials ?? new List<RoomType>();
            int length = combat + 2;                       // Start + Combat… + Boss: đúng số phòng Combat
            RoomNode prev = null;
            for (int i = 0; i < length; i++)
            {
                var n = Add(layout, new Vector2Int(i, 0), i, true);
                n.Type = i == 0 ? RoomType.Start : i == length - 1 ? RoomType.Boss : RoomType.Combat;
                if (prev != null) Link(prev, n);
                prev = n;
            }
            layout.Start = layout.Rooms[0];
            layout.Boss = prev;
            // Phòng đặc biệt: cột dọc phía trên các phòng (trừ Boss); hết cột thì xếp chồng lên cột đầu.
            int hosts = length - 1;
            var top = new RoomNode[hosts];
            for (int k = 0; k < specials.Count; k++)
            {
                int col = k % hosts;
                var below = top[col] ?? layout.Rooms[col];
                var sp = Add(layout, below.Cell + Vector2Int.up, below.Depth + 1, false);
                sp.Type = specials[k];
                Link(below, sp);
                top[col] = sp;
            }
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
