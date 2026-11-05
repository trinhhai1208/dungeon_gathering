using System.Collections.Generic;
using DungeonGathering.Core;
using DungeonGathering.Inventory;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>Trang bị mặc (ENTITIES §5.2): chỉ số cho mọi Role + hiệu ứng đặc biệt khi đúng Role.</summary>
    [CreateAssetMenu(fileName = "Equip_", menuName = "Dungeon Gathering/Equipment Data")]
    public class EquipmentData : ItemData
    {
        public override bool IsWeapon => false;

        public EquipmentSlotType slot;
        public List<StatModifier> baseModifiers = new();
        public CharacterRole synergyRole = CharacterRole.Any;
        public EquipmentPassiveSO synergyPassive;

        /// <summary>Đúng Role → kích hoạt synergyPassive. Role Any trên trang bị = mọi nhân vật.</summary>
        public bool SynergyActive(CharacterRole role)
            => synergyPassive != null && (synergyRole == CharacterRole.Any || synergyRole == role);
    }
}
