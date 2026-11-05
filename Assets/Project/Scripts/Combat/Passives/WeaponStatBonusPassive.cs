using System;
using System.Collections.Generic;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>Cộng chỉ số khi đang cầm vũ khí (vd +20% knockback quy về AttackDamage...).</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Passives/Weapon Stat Bonus", fileName = "WP_StatBonus")]
    public class WeaponStatBonusPassive : WeaponPassiveSO
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
