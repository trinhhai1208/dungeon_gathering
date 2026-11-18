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
        [Tooltip("Chỉ số cộng khi đang trang bị vũ khí này, theo (gốc + ΣFlat) × (1 + ΣPercent); rỗng = không cộng.")]
        public List<StatModifier> modifiers = new();

        public override void OnEquip(IPlayerContext player)
        {
            if (player == null) return;
            var rt = PassiveRuntime.For(player);
            if (rt != null) rt.GetOrCreate(this).RefCount++;
            foreach (var m in modifiers) player.AddModifier(this, m);
        }

        public override void OnUnequip(IPlayerContext player)
        {
            if (player == null) return;
            player.RemoveModifiers(this);
            var rt = PassiveRuntime.For(player, false);
            if (rt == null || !rt.TryGet(this, out var state)) return;
            if (--state.RefCount > 0)
            {
                for (int i = 0; i < state.RefCount; i++)
                    foreach (var m in modifiers) player.AddModifier(this, m);
            }
            else rt.Remove(this);
        }
    }
}
