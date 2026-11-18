using System;
using System.Collections.Generic;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>Chỉ số cộng: final = (base + ΣFlat) × (1 + ΣPercent). PercentValue 0.1 = +10%.</summary>
    [Serializable]
    public struct StatModifier
    {
        [Tooltip("Chỉ số bị tác động")] public StatType Stat;
        [Tooltip("Cộng thẳng vào chỉ số gốc trước khi nhân %: final = (base + ΣFlat) × (1 + ΣPct)")] public float FlatValue;
        [Tooltip("Phần trăm cộng dồn, áp sau Flat: 0.1 = +10%, -0.3 = -30%")] public float PercentValue;

        public StatModifier(StatType stat, float flat, float percent = 0f)
        {
            Stat = stat; FlatValue = flat; PercentValue = percent;
        }
    }

    /// <summary>Chỉ số gốc của nhân vật (HeroDefinitionSO).</summary>
    [Serializable]
    public struct BaseStats
    {
        [Tooltip("Máu tối đa (điểm HP)")] public float maxHp;
        [Tooltip("Giáp tối đa: đỡ sát thương trước HP, tự hồi sau khi không bị đánh một lúc")] public float maxArmor;
        [Tooltip("Mana tối đa; vũ khí / skill tốn mana trừ vào đây")] public float maxMana;
        [Tooltip("Hệ số sát thương (1 = 100%).")] public float attackDamage;
        [Tooltip("Hệ số tốc đánh (1 = 100%).")] public float attackSpeed;
        [Tooltip("Tốc chạy (world-unit / giây)")] public float moveSpeed;
        [Range(0, 1)] public float critChance;
        [Tooltip("Hệ số sát thương chí mạng (2 = 200%).")] public float critDamage;
        [Range(0, 0.5f)] [Tooltip("Giảm hồi chiêu (0.1 = 10%, tối đa 50%).")] public float cooldownReduction;
        [Tooltip("Mana tự hồi mỗi giây.")] public float manaRegen;
        [Range(0, 1)] [Tooltip("Giảm đẩy lùi nhận vào (0 = bị đẩy đủ, 1 = miễn).")] public float knockbackResist;

        public static BaseStats Default => new()
        {
            maxHp = 6, maxArmor = 4, maxMana = 180, attackDamage = 1f, attackSpeed = 1f, moveSpeed = 5f, critChance = 0.05f,
            critDamage = 2f, cooldownReduction = 0f, manaRegen = 2f,
        };

        public float Get(StatType t) => t switch
        {
            StatType.MaxHp => maxHp,
            StatType.MaxArmor => maxArmor,
            StatType.MaxMana => maxMana,
            StatType.AttackDamage => attackDamage,
            StatType.AttackSpeed => attackSpeed,
            StatType.MoveSpeed => moveSpeed,
            StatType.CritChance => critChance,
            StatType.CritDamage => critDamage > 0f ? critDamage : 2f,
            StatType.CooldownReduction => cooldownReduction,
            StatType.ManaRegen => manaRegen > 0f ? manaRegen : 2f,
            StatType.KnockbackResist => knockbackResist,
            _ => 0f,
        };

        /// <summary>Ghi một chỉ số gốc (StatBlock dùng để áp ghi đè).</summary>
        public void Set(StatType t, float v)
        {
            switch (t)
            {
                case StatType.MaxHp: maxHp = v; break;
                case StatType.MaxArmor: maxArmor = v; break;
                case StatType.MaxMana: maxMana = v; break;
                case StatType.AttackDamage: attackDamage = v; break;
                case StatType.AttackSpeed: attackSpeed = v; break;
                case StatType.MoveSpeed: moveSpeed = v; break;
                case StatType.CritChance: critChance = v; break;
                case StatType.CritDamage: critDamage = v; break;
                case StatType.CooldownReduction: cooldownReduction = v; break;
                case StatType.ManaRegen: manaRegen = v; break;
                case StatType.KnockbackResist: knockbackResist = v; break;
            }
        }
    }

    /// <summary>
    /// Tổng hợp chỉ số gốc + modifier theo nguồn (trang bị, buff tạm). Dùng ở PlayerStats và màn Loadout (xem trước chênh lệch).
    /// </summary>
    public sealed class StatSheet
    {
        static readonly int StatCount = Enum.GetValues(typeof(StatType)).Length;

        BaseStats baseStats;
        readonly Dictionary<object, List<StatModifier>> bySource = new();
        readonly float[] flat = new float[StatCount];
        readonly float[] pct = new float[StatCount];
        bool dirty = true;

        public event Action Changed;

        public StatSheet(BaseStats b) => baseStats = b;

        public BaseStats Base => baseStats;

        public void SetBase(BaseStats b) { baseStats = b; MarkDirty(); }

        public void Add(object source, StatModifier m)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!bySource.TryGetValue(source, out var list)) bySource[source] = list = new List<StatModifier>();
            list.Add(m);
            MarkDirty();
        }

        public void AddRange(object source, IEnumerable<StatModifier> mods)
        {
            if (mods == null) return;
            foreach (var m in mods) Add(source, m);
        }

        public bool RemoveSource(object source)
        {
            if (source == null || !bySource.Remove(source)) return false;
            MarkDirty();
            return true;
        }

        public void Clear() { bySource.Clear(); MarkDirty(); }

        public float Get(StatType t)
        {
            if (dirty) Rebuild();
            int i = (int)t;
            float v = (baseStats.Get(t) + flat[i]) * (1f + pct[i]);
            if (t == StatType.CritChance) return Mathf.Clamp01(v);
            if (t == StatType.CooldownReduction) return Mathf.Clamp(v, 0f, 0.5f);
            if (t == StatType.KnockbackResist) return Mathf.Clamp01(v);
            return Mathf.Max(0f, v);
        }

        void MarkDirty()
        {
            dirty = true;
            Changed?.Invoke();
        }

        void Rebuild()
        {
            Array.Clear(flat, 0, flat.Length);
            Array.Clear(pct, 0, pct.Length);
            foreach (var list in bySource.Values)
                foreach (var m in list)
                {
                    flat[(int)m.Stat] += m.FlatValue;
                    pct[(int)m.Stat] += m.PercentValue;
                }
            dirty = false;
        }
    }
}
