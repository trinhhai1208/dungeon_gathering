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
        [Tooltip("Hệ số sát thương vụ nổ, nhân với AttackDamage của người chơi; nổ khi giáp về 0 sau đòn không chí tử.")]
        public float damage = 20f;
        [Tooltip("Bán kính vụ nổ (world unit) quanh người chơi; quái trong vòng bị trúng.")]
        public float radius = 2.5f;
        [Tooltip("Lực đẩy lùi quái ra xa tâm vụ nổ; 0 = không đẩy.")]
        public float knockback = 10f;
        [Tooltip("Prefab VFX phát tại người chơi khi nổ; null = không hiệu ứng (vẫn gây sát thương).")]
        public GameObject vfx;

        public override void OnEquip(IPlayerContext player)
        {
            var rt = PassiveRuntime.For(player);
            if (rt == null) return;
            var state = rt.GetOrCreate(this);
            if (++state.RefCount > 1) return;
            Action h = () =>
            {
                if (player == null || player.IsDead) return;
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
            if (--state.RefCount > 0) return;
            if (state.Handler is Action h) player.Hooks.ArmorBroken -= h;
            rt.Remove(this);
        }
    }
}
