using System;
using System.Collections.Generic;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>
    /// Hệ số theo bậc quái (Normal / Elite / Boss). Áp vào StatSheet của quái như một nguồn modifier riêng
    /// (phần trăm), nên không cần code riêng cho từng bậc. Hệ số rớt đồ dùng ở hệ loot.
    /// </summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Stats/Monster Tier Config", fileName = "MonsterTierConfig")]
    public sealed class MonsterTierConfigSO : ScriptableObject
    {
        [Serializable]
        public sealed class Tier
        {
            [Tooltip("Bậc quái áp dụng dòng này; bậc không có dòng → mọi hệ số = 1")] public MonsterTier tier;
            [Tooltip("1 = giữ nguyên, 2.5 = HP ×2.5")] public float hpMultiplier = 1f;
            [Tooltip("Hệ số sát thương: 1 = giữ nguyên, 1.4 = +40% (cộng % vào AttackDamage)")] public float damageMultiplier = 1f;
            [Tooltip("Hệ số tốc chạy: 1 = giữ nguyên, 1.1 = +10% (cộng % vào MoveSpeed)")] public float moveSpeedMultiplier = 1f;
            [Tooltip("Cộng thêm kháng đẩy lùi (0..1)")] [Range(0, 1)] public float bonusKnockbackResist;
            [Tooltip("Hệ số kích thước hiển thị")] public float scale = 1f;
            [Tooltip("Màu viền/tint nhận diện (alpha 0 = không tint)")] public Color tint = new(1f, 1f, 1f, 0f);
        }

        [Tooltip("Hệ số cho từng bậc quái Normal / Elite / Boss (mỗi bậc một dòng)")] public List<Tier> tiers = new()
        {
            new Tier { tier = MonsterTier.Normal },
            new Tier { tier = MonsterTier.Elite, hpMultiplier = 2.5f, damageMultiplier = 1.4f, moveSpeedMultiplier = 1.1f,
                       bonusKnockbackResist = 0.3f, scale = 1.25f, tint = new Color(1f, 0.75f, 0.2f, 0.35f) },
            new Tier { tier = MonsterTier.Boss, bonusKnockbackResist = 0.8f },
        };

        static readonly Tier Neutral = new();

        public Tier Get(MonsterTier tier)
        {
            if (tiers != null)
                foreach (var t in tiers) if (t != null && t.tier == tier) return t;
            return Neutral;
        }

        /// <summary>Modifier phần trăm tương ứng bậc (×1.4 → +0.4) để thêm vào StatSheet.</summary>
        public static IEnumerable<StatModifier> Modifiers(Tier t)
        {
            if (t == null) yield break;
            if (!Mathf.Approximately(t.hpMultiplier, 1f)) yield return new StatModifier(StatType.MaxHp, 0f, t.hpMultiplier - 1f);
            if (!Mathf.Approximately(t.damageMultiplier, 1f)) yield return new StatModifier(StatType.AttackDamage, 0f, t.damageMultiplier - 1f);
            if (!Mathf.Approximately(t.moveSpeedMultiplier, 1f)) yield return new StatModifier(StatType.MoveSpeed, 0f, t.moveSpeedMultiplier - 1f);
            if (t.bonusKnockbackResist > 0f) yield return new StatModifier(StatType.KnockbackResist, t.bonusKnockbackResist);
        }

        /// <summary>Asset đang dùng (LevelDirector gán từ PlayPrefabSet). Null → mọi bậc hệ số 1.</summary>
        public static MonsterTierConfigSO Current { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() => Current = null;
    }
}
