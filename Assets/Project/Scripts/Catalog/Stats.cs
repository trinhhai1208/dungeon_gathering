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
        public StatType Stat;
        public float FlatValue;
        public float PercentValue;

        public StatModifier(StatType stat, float flat, float percent = 0f)
        {
            Stat = stat; FlatValue = flat; PercentValue = percent;
        }
    }

    /// <summary>Chỉ số gốc của nhân vật (CharacterData).</summary>
    [Serializable]
    public struct BaseStats
    {
        public float maxHp;
        public float maxArmor;
        public float maxMana;
        [Tooltip("Hệ số sát thương (1 = 100%).")] public float attackDamage;
        [Tooltip("Hệ số tốc đánh (1 = 100%).")] public float attackSpeed;
        public float moveSpeed;
        [Range(0, 1)] public float critChance;

        public static BaseStats Default => new()
        {
            maxHp = 6, maxArmor = 4, maxMana = 180, attackDamage = 1f, attackSpeed = 1f, moveSpeed = 5f, critChance = 0.05f,
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
            _ => 0f,
        };
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
            return t == StatType.CritChance ? Mathf.Clamp01(v) : Mathf.Max(0f, v);
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
