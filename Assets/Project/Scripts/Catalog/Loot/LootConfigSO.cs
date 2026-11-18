using System;
using System.Collections.Generic;
using DungeonGathering.Core;
using DungeonGathering.Inventory;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>
    /// Trọng số bậc vật phẩm: D, C, B, A, S = trọng số tương đối của từng bậc (D thấp nhất, S cao nhất; số thực ≥ 0,
    /// không cần cộng đủ 100 — xác suất bậc = trọng số / tổng; tổng 0 → luôn D). <see cref="Shift"/> dời trọng số lên bậc cao hơn:
    /// 1 = dời trọn một bậc (D→C, C→B…, S giữ ở S), 0.5 = dời một nửa.
    /// </summary>
    [Serializable]
    public struct RankWeights
    {
        [Tooltip("Trọng số tương đối của bậc (D thấp → S cao); xác suất = trọng số / tổng 5 bậc.")]
        public float D, C, B, A, S;

        public float Total => D + C + B + A + S;

        public float Get(Rarity r) => r switch { Rarity.D => D, Rarity.C => C, Rarity.B => B, Rarity.A => A, _ => S };

        /// <summary>Dời <paramref name="steps"/> bậc lên trên (âm = dời xuống). Giữ nguyên tổng trọng số.</summary>
        public RankWeights Shift(float steps)
        {
            var w = this;
            if (Mathf.Approximately(steps, 0f)) return w;
            bool up = steps > 0f;
            float rest = Mathf.Abs(steps);
            while (rest > 1e-5f)
            {
                float f = Mathf.Min(1f, rest);
                w = up ? w.StepUp(f) : w.StepDown(f);
                rest -= f;
            }
            return w;
        }

        RankWeights StepUp(float f) => new()
        {
            D = D * (1f - f),
            C = C * (1f - f) + D * f,
            B = B * (1f - f) + C * f,
            A = A * (1f - f) + B * f,
            S = S + A * f,
        };

        RankWeights StepDown(float f) => new()
        {
            S = S * (1f - f),
            A = A * (1f - f) + S * f,
            B = B * (1f - f) + A * f,
            C = C * (1f - f) + B * f,
            D = D + C * f,
        };

        public Rarity Roll(System.Random rng)
        {
            float total = Total;
            if (total <= 0f) return Rarity.D;
            double r = rng.NextDouble() * total;
            if ((r -= S) < 0) return Rarity.S;
            if ((r -= A) < 0) return Rarity.A;
            if ((r -= B) < 0) return Rarity.B;
            if ((r -= C) < 0) return Rarity.C;
            return Rarity.D;
        }
    }

    /// <summary>Rơi đồ của một bậc quái (hoặc một loại rương).</summary>
    [Serializable]
    public sealed class TierLoot
    {
        [Tooltip("Bậc quái áp dụng dòng này (Normal / Elite / Boss). Không tìm thấy bậc → dùng dòng đầu tiên.")]
        public MonsterTier tier;
        [Tooltip("Vàng (min, max)")] public Vector2Int gold = new(1, 3);
        [Range(0, 1)] [Tooltip("Xác suất rơi vũ khí / trang bị")] public float itemChance = 0.03f;
        [Min(0)] [Tooltip("Số món chắc chắn rơi (boss)")] public int guaranteedItems;
        [Range(0, 1)] public float manaOrbChance = 0.3f;
        [Min(0)] public int manaOrbs = 1;
        [Range(0, 1)] public float potionChance = 0.02f;
        [Tooltip("Dời bậc vật phẩm lên (Elite / Boss rơi đồ tốt hơn). 1 = một bậc")] public float rankShift;
    }

    /// <summary>
    /// Cấu hình rơi đồ của cả game (một asset): trọng số bậc D…S theo level, rơi đồ theo bậc quái, rương.
    /// Bậc vật phẩm = trọng số của level (level vượt danh sách thì dời tiếp <see cref="rankShiftPerExtraLevel"/>) dời thêm
    /// theo bậc quái + lootLuck của level. Vàng nhân thêm <see cref="goldPerLevel"/> mỗi level.
    /// </summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Loot/Loot Config", fileName = "LootConfig")]
    public sealed class LootConfigSO : ScriptableObject
    {
        [Header("Bậc vật phẩm theo level (index 0 = level 1)")]
        [Tooltip("Trọng số bậc D…S theo level (phần tử 0 = level 1). Level vượt danh sách dùng phần tử cuối + dời bậc. Trống = luôn D.")]
        public List<RankWeights> rankWeightsByLevel = new()
        {
            new RankWeights { D = 60, C = 35, B = 5 },
            new RankWeights { C = 50, B = 35, A = 15 },
            new RankWeights { B = 40, A = 45, S = 15 },
        };
        [Tooltip("Level vượt danh sách: mỗi level dời thêm bấy nhiêu bậc")] public float rankShiftPerExtraLevel = 0.5f;
        [Tooltip("Vàng tăng thêm mỗi level (0.25 = +25% / level)")] public float goldPerLevel = 0.25f;

        [Header("Theo bậc quái")]
        [Tooltip("Rơi đồ theo bậc quái (vàng, vật phẩm, mana, bình máu, dời bậc). Mỗi bậc một dòng.")]
        public List<TierLoot> tiers = new()
        {
            new TierLoot { tier = MonsterTier.Normal, gold = new Vector2Int(1, 3), itemChance = 0.03f, manaOrbChance = 0.3f, manaOrbs = 1, potionChance = 0.02f },
            new TierLoot { tier = MonsterTier.Elite, gold = new Vector2Int(4, 8), itemChance = 0.2f, manaOrbChance = 0.7f, manaOrbs = 2, potionChance = 0.1f, rankShift = 1f },
            new TierLoot { tier = MonsterTier.Boss, gold = new Vector2Int(20, 30), itemChance = 1f, guaranteedItems = 1, manaOrbChance = 1f, manaOrbs = 3, potionChance = 0.5f, rankShift = 1.5f },
        };

        [Header("Rương")]
        [Tooltip("Rương phòng thường / rương thưởng: dời bậc vật phẩm")] public float chestRankShift = 0.5f;

        public TierLoot Get(MonsterTier tier)
        {
            foreach (var t in tiers) if (t != null && t.tier == tier) return t;
            return tiers.Count > 0 ? tiers[0] : new TierLoot();
        }

        /// <summary>Trọng số bậc của level (đã dời theo level vượt danh sách) — chưa tính bậc quái.</summary>
        public RankWeights WeightsForLevel(int level)
        {
            if (rankWeightsByLevel == null || rankWeightsByLevel.Count == 0) return new RankWeights { D = 1 };
            int i = Mathf.Clamp(level - 1, 0, rankWeightsByLevel.Count - 1);
            int extra = Mathf.Max(0, level - rankWeightsByLevel.Count);
            return rankWeightsByLevel[i].Shift(extra * rankShiftPerExtraLevel);
        }

        public static LootConfigSO Current { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() => Current = null;
    }
}
