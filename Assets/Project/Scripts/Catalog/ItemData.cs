using System.Collections.Generic;
using DungeonGathering.Core;
using DungeonGathering.Inventory;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    public enum ItemKind { Weapon = 0, Equipment = 1, Consumable = 2 }

    /// <summary>
    /// Định danh vật phẩm thống nhất cho kho đồ, shop, rơi đồ, UI: mọi chỗ hiển thị đọc qua đây (không đọc field).
    /// Vũ khí lấy tên / icon / bậc / giá từ entry thư viện vũ khí khi có; trang bị lấy từ chính EquipmentSO.
    /// </summary>
    public interface IItemDefinition
    {
        string Id { get; }
        string NameKey { get; }
        string DescKey { get; }
        Rarity Rank { get; }
        Sprite Icon { get; }
        int Price { get; }
        ItemKind Kind { get; }
        ActiveSkillSO ActiveSkill { get; }
    }

    /// <summary>Base của WeaponData / EquipmentSO. id ngắn là khóa chung (save, catalog, i18n).</summary>
    public abstract class ItemData : ScriptableObject, IItemDefinition
    {
        [Header("Định danh")]
        public string id;                    // "sword-ice"
        [Tooltip("Bậc hiếm (S cao nhất → D thấp nhất): lọc rơi đồ/shop và màu ô. Vũ khí lấy bậc từ entry thư viện, field này chỉ dự phòng.")]
        public Rarity rarity = Rarity.D;
        public Sprite icon;                  // ô 16×16 hoặc kiếm 32×32
        public string nameKey;               // key DG_Multilanguage
        [Tooltip("Key mô tả trong bảng DG_Multilanguage; rỗng = không có mô tả. Vũ khí ưu tiên descKey của entry thư viện.")]
        public string descKey;
        [Tooltip("Kỹ năng chủ động (tùy chọn) — tính vào giới hạn 2 kỹ năng kích hoạt.")]
        public ActiveSkillSO activeSkill;
        [Min(0)] public int shopPrice = 100;

        public bool HasActive => ActiveSkill != null;
        public abstract bool IsWeapon { get; }

        // ---- IItemDefinition (đọc qua đây; lớp con có thể lấy từ nguồn khác)
        public string Id => id;
        public virtual string NameKey => nameKey;
        public virtual string DescKey => descKey;
        public virtual Rarity Rank => rarity;
        public virtual Sprite Icon => icon;
        public virtual int Price => shopPrice;
        public virtual ActiveSkillSO ActiveSkill => activeSkill;
        public virtual ItemKind Kind => IsWeapon ? ItemKind.Weapon : ItemKind.Equipment;

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
