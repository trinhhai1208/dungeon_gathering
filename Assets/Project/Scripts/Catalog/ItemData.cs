using System.Collections.Generic;
using DungeonGathering.Core;
using DungeonGathering.Inventory;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>Base của WeaponData/EquipmentData (ENTITIES §1.1). id ngắn là khóa chung (save, catalog, i18n).</summary>
    public abstract class ItemData : ScriptableObject
    {
        [Header("Định danh")]
        public string id;                    // "sword-ice"
        public Rarity rarity = Rarity.F;
        public Sprite icon;                  // ô 16×16 hoặc kiếm 32×32
        public string nameKey;               // key DG_Multilanguage
        public string descKey;
        [Tooltip("Kỹ năng chủ động (tùy chọn) — tính vào giới hạn 2 kỹ năng kích hoạt.")]
        public ActiveSkillSO activeSkill;
        [Min(0)] public int shopPrice = 100;

        public bool HasActive => activeSkill != null;
        public abstract bool IsWeapon { get; }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(id)) id = name.ToLowerInvariant().Replace(' ', '-').Replace('_', '-');
        }
#endif
    }

    public enum WeaponType { Melee, Ranged }

    public enum RangedFireMode { Projectile, Hitscan }
}
