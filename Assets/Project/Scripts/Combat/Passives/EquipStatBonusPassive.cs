using System;
using System.Collections.Generic;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>Trang bị chỉ cộng chỉ số đặc biệt khi đúng Role (ngoài baseModifiers).</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Passives/Equip Stat Bonus", fileName = "EP_StatBonus")]
    public class EquipStatBonusPassive : EquipmentPassiveSO
    {
        public List<StatModifier> modifiers = new();
        public override void OnEquip(IPlayerContext player)
        {
            player.RemoveModifiers(this);
            foreach (var m in modifiers) player.AddModifier(this, m);
        }
        public override void OnUnequip(IPlayerContext player) => player?.RemoveModifiers(this);
    }
}
