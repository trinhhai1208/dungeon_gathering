using System.Collections.Generic;
using DungeonGathering.Core;
using DungeonGathering.Inventory;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>
    /// Trang bị: ô, bậc, chỉ số (modifier) cho mọi Role, hiệu ứng khi trúng (StatusEffectRef), passive khi đúng Role, bộ set
    /// — cấu hình trọn trong SO.
    /// </summary>
    [CreateAssetMenu(fileName = "Equip_", menuName = "Dungeon Gathering/Equipment")]
    public class EquipmentSO : ItemData
    {
        public override bool IsWeapon => false;

        [Tooltip("Ô trang bị món này chiếm; mỗi ô chỉ mặc 1 món, mặc món mới cùng ô sẽ thay món cũ.")]
        public EquipmentSlotType slot;
        [Tooltip("Chỉ số cộng cho mọi Role khi mặc, theo (gốc + ΣFlat) × (1 + ΣPercent); rỗng = không cộng chỉ số.")]
        public List<StatModifier> baseModifiers = new();
        [Tooltip("Role kích hoạt synergyPassive; Any = mọi nhân vật đều kích hoạt.")]
        public CharacterRole synergyRole = CharacterRole.Any;
        [Tooltip("Nội tại chỉ bật khi nhân vật đúng synergyRole; null = trang bị không có nội tại.")]
        public EquipmentPassiveSO synergyPassive;
        [Tooltip("Bộ set chứa món này; mặc đủ 2 / 4 món cùng set kích hoạt thưởng. null = không thuộc set nào.")]
        public EquipmentSetSO equipmentSet;
        [Tooltip("Hiệu ứng gây lên mục tiêu mỗi khi đòn của người mặc trúng (vd Slow 10%, Burn) — mọi Role")]
        public List<StatusEffectRef> onHitStatus = new();

        /// <summary>Đúng Role → kích hoạt synergyPassive. Role Any trên trang bị = mọi nhân vật.</summary>
        public bool SynergyActive(CharacterRole role)
            => synergyPassive != null && (synergyRole == CharacterRole.Any || synergyRole == role);
    }
}
