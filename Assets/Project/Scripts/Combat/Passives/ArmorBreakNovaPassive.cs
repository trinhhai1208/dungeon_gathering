using System;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>Giáp Lửa Kỵ Sĩ (Warrior): vỡ giáp → nổ vòng lửa đẩy lùi quái xung quanh.</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Passives/Equip Armor Break Nova", fileName = "EP_ArmorBreakNova")]
    public class ArmorBreakNovaPassive : EquipmentPassiveSO
    {
        public float damage = 20f;
        public float radius = 2.5f;
        public float knockback = 10f;
        public GameObject vfx;

        public override void OnEquip(IPlayerContext player)
        {
            OnUnequip(player);
            var rt = PassiveRuntime.For(player);
            if (rt == null) return;
            var state = rt.GetOrCreate(this);
            Action h = () =>
            {
                // Có thể chạy LỒNG trong DamageArea của quái (đòn quái → vỡ giáp) — DamageArea đã an toàn khi lồng nhau.
                Vector2 c = player.Transform.position;
                CombatFx.Play(vfx, c, Quaternion.identity);
                CombatUtil.DamageArea(c, radius, CombatLayers.EnemyTargets,
                    new DamageInfo(damage * player.GetStat(StatType.AttackDamage), knockback, Vector2.zero, 0f, false, player.GameObject, Team.Player));
            };
            state.Handler = h;
            player.Hooks.ArmorBroken += h;
        }

        public override void OnUnequip(IPlayerContext player)
        {
            var rt = PassiveRuntime.For(player, false);
            if (rt == null || !rt.TryGet(this, out var state)) return;
            if (state.Handler is Action h) player.Hooks.ArmorBroken -= h;
            rt.Remove(this);
        }
    }
}
