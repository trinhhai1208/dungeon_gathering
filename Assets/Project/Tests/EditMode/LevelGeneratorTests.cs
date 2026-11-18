using System.Collections.Generic;
using System.Linq;
using DungeonGathering.Play;
using NUnit.Framework;
using UnityEngine;

namespace DungeonGathering.Tests
{
    /// <summary>Giai đoạn 8: sinh map theo số phòng chính xác, phòng đặc biệt ở cuối nhánh, dự phòng, cùng seed cùng map.</summary>
    public class LevelGeneratorTests
    {
        static GeneratorSettings Counts(int combat, Vector2Int grid, params RoomType[] specials) => new()
        {
            GridSize = grid, CombatRooms = combat, Specials = specials.ToList(),
        };

        static void AssertLayout(DungeonLayout l, GeneratorSettings s)
        {
            Assert.AreEqual(s.CombatRooms, l.Rooms.Count(r => r.Type == RoomType.Combat), "Số phòng Combat");
            foreach (var g in s.Specials.GroupBy(t => t))
                Assert.AreEqual(g.Count(), l.Rooms.Count(r => r.Type == g.Key), $"Số phòng {g.Key}");
            Assert.AreEqual(1, l.Rooms.Count(r => r.Type == RoomType.Boss));
            Assert.AreEqual(1, l.Rooms.Count(r => r.Type == RoomType.Start));
            Assert.AreEqual(l.Rooms.Count - 1, l.Links().Count(), "Map dạng cây: số cạnh = số phòng − 1 (liên thông, không vòng)");
            Assert.AreEqual(l.Rooms.Count, l.ByCell.Count, "Không có hai phòng chung một ô");
        }

        [Test]
        public void ExactCounts_ManySeeds()
        {
            var s = Counts(5, new Vector2Int(9, 9), RoomType.Treasure, RoomType.Shop, RoomType.Elite);
            for (int seed = 1; seed <= 200; seed++) AssertLayout(DungeonGenerator.Generate(seed, s), s);
        }

        [Test]
        public void SpecialRooms_AreLeaves()
        {
            var s = Counts(4, new Vector2Int(8, 8), RoomType.Treasure, RoomType.Shop);
            for (int seed = 1; seed <= 100; seed++)
            {
                var l = DungeonGenerator.Generate(seed, s);
                foreach (var r in l.Rooms.Where(r => r.Type is RoomType.Treasure or RoomType.Shop))
                    Assert.AreEqual(1, Doors(r), "Phòng đặc biệt nằm ở cuối nhánh (chỉ một cửa)");
                Assert.AreEqual(1, Doors(l.Boss), "Boss là lá cuối đường chính");
            }
        }

        [Test]
        public void TinyGrid_UsesFallback_StillExactCounts()
        {
            var s = Counts(6, new Vector2Int(3, 3), RoomType.Treasure, RoomType.Shop, RoomType.Elite);
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            var l = DungeonGenerator.Generate(7, s);
            AssertLayout(l, s);
        }

        [Test]
        public void SameSeed_SameMap()
        {
            var s = Counts(5, new Vector2Int(9, 9), RoomType.Treasure, RoomType.Shop);
            var a = DungeonGenerator.Generate(123, s);
            var b = DungeonGenerator.Generate(123, s);
            CollectionAssert.AreEqual(a.Rooms.Select(r => (r.Cell, r.Type)), b.Rooms.Select(r => (r.Cell, r.Type)));
        }

        [Test]
        public void LevelConfig_BuildsSettingsFromCounts()
        {
            var cfg = ScriptableObject.CreateInstance<LevelConfigSO>();
            cfg.combatRoomCount = new Vector2Int(3, 3);
            cfg.specialRooms = new List<SpecialRoomCount> { new() { type = RoomType.Shop, count = 2 }, new() { type = RoomType.Boss, count = 1 } };
            var s = cfg.ToGeneratorSettings(new System.Random(1));
            Assert.AreEqual(3, s.CombatRooms);
            CollectionAssert.AreEqual(new[] { RoomType.Shop, RoomType.Shop }, s.Specials, "Boss / Start / Combat không được khai báo là phòng đặc biệt");
            Object.DestroyImmediate(cfg);
        }

        static int Doors(RoomNode r) => DoorMaskExt.All.Count(d => (r.Doors & d) != 0);
    }
}
