using System.Collections.Generic;
using System.Reflection;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using DungeonGathering.Inventory;
using NUnit.Framework;
using UnityEngine;

namespace DungeonGathering.Tests
{
    /// <summary>Giai đoạn 7: dời bậc D…S, rơi đồ theo bậc quái + level, chọn vật phẩm theo bậc (lùi bậc khi thiếu).</summary>
    public class LootTests
    {
        readonly List<Object> created = new();
        T Make<T>() where T : ScriptableObject { var o = ScriptableObject.CreateInstance<T>(); created.Add(o); return o; }

        [TearDown]
        public void Cleanup()
        {
            foreach (var o in created) Object.DestroyImmediate(o);
            created.Clear();
        }

        [Test]
        public void Shift_OneStep_MovesEveryRankUp_KeepsTotal()
        {
            var w = new RankWeights { D = 60, C = 35, B = 5 };
            var s = w.Shift(1f);
            Assert.AreEqual(0f, s.D, 1e-4);
            Assert.AreEqual(60f, s.C, 1e-4);
            Assert.AreEqual(35f, s.B, 1e-4);
            Assert.AreEqual(5f, s.A, 1e-4);
            Assert.AreEqual(w.Total, s.Total, 1e-3);
        }

        [Test]
        public void Shift_SaturatesAtS_AndHalfSteps()
        {
            var w = new RankWeights { A = 10, S = 10 };
            Assert.AreEqual(20f, w.Shift(3f).S, 1e-4, "Dời quá S thì dồn ở S");
            var h = new RankWeights { D = 100 }.Shift(0.5f);
            Assert.AreEqual(50f, h.D, 1e-4);
            Assert.AreEqual(50f, h.C, 1e-4);
            Assert.AreEqual(100f, new RankWeights { C = 100 }.Shift(-1f).D, 1e-4, "Dời âm = xuống bậc");
        }

        [Test]
        public void WeightsForLevel_BeyondListKeepsShifting()
        {
            var cfg = Make<LootConfigSO>();
            cfg.rankWeightsByLevel = new List<RankWeights> { new() { D = 100 } };
            cfg.rankShiftPerExtraLevel = 1f;
            Assert.AreEqual(100f, cfg.WeightsForLevel(1).D, 1e-4);
            Assert.AreEqual(100f, cfg.WeightsForLevel(3).B, 1e-4, "Level 3 = vượt 2 level → dời 2 bậc");
        }

        [Test]
        public void Boss_GuaranteedItems_NormalMayDropNone()
        {
            var cfg = Make<LootConfigSO>();
            cfg.tiers = new List<TierLoot>
            {
                new() { tier = MonsterTier.Normal, gold = new Vector2Int(1, 1), itemChance = 0f },
                new() { tier = MonsterTier.Boss, gold = new Vector2Int(10, 10), itemChance = 1f, guaranteedItems = 2 },
            };
            var rng = new System.Random(1);
            Assert.AreEqual(0, DropResolver.ForMonster(cfg, MonsterTier.Normal, 1, 0f, rng).ItemRanks.Count);
            Assert.AreEqual(2, DropResolver.ForMonster(cfg, MonsterTier.Boss, 1, 0f, rng).ItemRanks.Count);
        }

        [Test]
        public void Gold_ScalesWithLevel()
        {
            var cfg = Make<LootConfigSO>();
            cfg.goldPerLevel = 0.5f;
            cfg.tiers = new List<TierLoot> { new() { tier = MonsterTier.Normal, gold = new Vector2Int(10, 10) } };
            var rng = new System.Random(1);
            Assert.AreEqual(10, DropResolver.ForMonster(cfg, MonsterTier.Normal, 1, 0f, rng).Gold);
            Assert.AreEqual(20, DropResolver.ForMonster(cfg, MonsterTier.Normal, 3, 0f, rng).Gold);
        }

        [Test]
        public void HigherTier_DropsBetterRanks_OnAverage()
        {
            var cfg = Make<LootConfigSO>();
            cfg.rankWeightsByLevel = new List<RankWeights> { new() { D = 60, C = 35, B = 5 } };
            cfg.tiers = new List<TierLoot>
            {
                new() { tier = MonsterTier.Normal, itemChance = 1f },
                new() { tier = MonsterTier.Elite, itemChance = 1f, rankShift = 1f },
            };
            float Avg(MonsterTier t)
            {
                var rng = new System.Random(42);
                float sum = 0f;
                for (int i = 0; i < 2000; i++) sum += (int)DropResolver.ForMonster(cfg, t, 1, 0f, rng).ItemRanks[0];
                return sum / 2000f; // số nhỏ = bậc cao
            }
            Assert.Less(Avg(MonsterTier.Elite), Avg(MonsterTier.Normal) - 0.5f);
        }

        [Test]
        public void PickItem_FallsBackToLowerThenHigherRank()
        {
            var cat = Make<ItemCatalog>();
            var a = Make<EquipmentSO>(); a.id = "a"; a.rarity = Rarity.A;
            var c = Make<EquipmentSO>(); c.id = "c"; c.rarity = Rarity.C;
            typeof(ItemCatalog).GetField("items", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(cat, new List<ItemData> { a, c });
            var rng = new System.Random(3);
            Assert.AreSame(c, cat.PickItem(Rarity.C, rng));
            Assert.AreSame(c, cat.PickItem(Rarity.B, rng), "Không có B → lùi xuống C");
            Assert.AreSame(a, cat.PickItem(Rarity.S, rng), "Không có S → lùi xuống A");
            Assert.AreSame(a, cat.PickItem(Rarity.A, rng));
            Assert.IsNull(cat.PickItem(Rarity.D, rng, weapons: true, equipment: false), "Không có vũ khí");
        }
    }
}
