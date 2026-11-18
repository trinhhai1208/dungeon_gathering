using System.Collections.Generic;
using DungeonGathering.Core;
using DungeonGathering.Inventory;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    public enum WeaponHoldStyle
    {
        OneHandedSide = 0,      // Kiếm 1 tay / Dao găm: cầm ở hông, hơi chếch lên
        HeavyShoulder = 1,      // Đại kiếm / Rìu: vác vai (shoulder carry)
        Ranged = 2,             // Súng: cầm ngang ngực/hông, hướng thẳng phía trước
        Spear = 3               // Thương/Giáo: cầm ngang đâm thẳng
    }

    public enum MeleeAttackStyle { Swing = 0, Thrust = 1, Spin360 = 2 }

    /// <summary>
    /// Vật phẩm vũ khí: chỉ là định danh trong kho / save / shop (id). Mọi thứ khác — chỉ số, hình, hitbox, đạn, VFX, tên,
    /// icon, bậc, giá, skill, passive — nằm ở entry cùng id trong Sword / Ranged Library (ItemCatalog đăng ký).
    /// Các field kế thừa từ ItemData (nameKey, icon, rarity, shopPrice…) chỉ là dự phòng khi thiếu entry.
    /// </summary>
    [CreateAssetMenu(fileName = "Weapon_", menuName = "Dungeon Gathering/Weapon Item")]
    public class WeaponData : ItemData
    {
        public override bool IsWeapon => true;

        /// <summary>Thông số đã resolve (SO gốc ← đạn ← entry). Thiếu entry → spec tối thiểu + cảnh báo.</summary>
        public WeaponSpec Spec => WeaponLibrary.Resolve(this);

        WeaponEntryBase Entry => WeaponLibrary.TryGetEntry(id, out var e) ? e : null;
        public override string NameKey => Entry is { } e && !string.IsNullOrEmpty(e.nameKey) ? e.nameKey : nameKey;
        public override string DescKey => Entry is { } e && !string.IsNullOrEmpty(e.descKey) ? e.descKey : descKey;
        public override Rarity Rank => Entry is { } e ? e.rank : rarity;
        public override Sprite Icon => Entry is { } e && e.icon != null ? e.icon : icon;
        public override int Price => Entry is { } e && e.shopPrice > 0 ? e.shopPrice : shopPrice;
        public override ActiveSkillSO ActiveSkill => Entry is { } e && e.activeSkill != null ? e.activeSkill : activeSkill;
        /// <summary>Passive vũ khí (từ entry thư viện).</summary>
        public WeaponPassiveSO Passive => Entry?.passive;
    }
}
