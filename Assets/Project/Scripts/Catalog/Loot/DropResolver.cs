using System.Collections.Generic;
using DungeonGathering.Core;
using DungeonGathering.Inventory;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>Kết quả tung đồ (chưa sinh object): LootSpawner biến thành coin, orb, bình, vật phẩm.</summary>
    public sealed class DropResult
    {
        public int Gold;
        public int ManaOrbs;
        public int Potions;
        public readonly List<Rarity> ItemRanks = new();
    }

    /// <summary>
    /// Tính rơi đồ — thuần C#, có test. Bậc quái quyết định lượng vàng / xác suất / dời bậc; level quyết định trọng số
    /// bậc và hệ số vàng; <paramref name="lootLuck"/> (level hoặc vật phẩm) dời thêm bậc.
    /// </summary>
    public static class DropResolver
    {
        public static DropResult ForMonster(LootConfigSO cfg, MonsterTier tier, int level, float lootLuck, System.Random rng)
        {
            var r = new DropResult();
            if (cfg == null || rng == null) return r;
            var t = cfg.Get(tier);
            float goldMul = 1f + Mathf.Max(0, level - 1) * cfg.goldPerLevel;
            int lo = Mathf.Max(0, Mathf.Min(t.gold.x, t.gold.y)), hi = Mathf.Max(t.gold.x, t.gold.y);
            r.Gold = Mathf.RoundToInt(rng.Next(lo, hi + 1) * goldMul);
            if (rng.NextDouble() < t.manaOrbChance) r.ManaOrbs = t.manaOrbs;
            if (rng.NextDouble() < t.potionChance) r.Potions = 1;

            var weights = cfg.WeightsForLevel(level).Shift(t.rankShift + lootLuck);
            // Có số món chắc chắn (boss) → đúng bấy nhiêu món; không có → một món theo xác suất
            int items = t.guaranteedItems > 0 ? t.guaranteedItems : (rng.NextDouble() < t.itemChance ? 1 : 0);
            for (int i = 0; i < items; i++) r.ItemRanks.Add(weights.Roll(rng));
            return r;
        }

        /// <summary>Bậc một món từ rương / shop: trọng số level dời thêm <paramref name="shift"/>.</summary>
        public static Rarity RollRank(LootConfigSO cfg, int level, float shift, System.Random rng)
        {
            if (cfg == null) return Rarity.D;
            return cfg.WeightsForLevel(level).Shift(shift).Roll(rng);
        }
    }
}
