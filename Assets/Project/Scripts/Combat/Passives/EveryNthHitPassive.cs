using System;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>Sách Cổ Ngữ (Mage): mỗi đòn trúng thứ N triệu hồi vòng xoáy phép tại mục tiêu.</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Passives/Equip Every Nth Hit", fileName = "EP_NthHit")]
    public class EveryNthHitPassive : EquipmentPassiveSO
    {
        [Min(1)] public int everyN = 3;
        [Tooltip("Hệ số sát thương vòng xoáy, nhân với AttackDamage của người chơi; trúng mọi quái trong radius.")]
        public float damage = 15f;
        [Tooltip("Bán kính vòng xoáy (world unit) quanh mục tiêu của đòn trúng thứ N.")]
        public float radius = 1.5f;
        [Tooltip("Prefab VFX phát tại mục tiêu khi vòng xoáy kích hoạt; null = không hiệu ứng.")]
        public GameObject vfx;

        public override void OnEquip(IPlayerContext player)
        {
            var rt = PassiveRuntime.For(player);
            if (rt == null) return;
            var state = rt.GetOrCreate(this);
            if (++state.RefCount > 1) return;
            state.Counter = 0;
            state.Busy = false;
            Action<HitReport> h = r =>
            {
                // Bộ đếm + cờ chống đệ quy nằm trong state theo Player (không trên SO).
                if (state.Busy || r.Target == null) return;
                if (++state.Counter < everyN) return;
                state.Counter = 0;
                state.Busy = true;
                try
                {
                    Vector2 c = r.Target.transform.position;
                    CombatFx.Play(vfx, c, Quaternion.identity);
                    CombatUtil.DamageArea(c, radius, CombatLayers.EnemyTargets,
                        new DamageInfo(damage * player.GetStat(StatType.AttackDamage), 2f, Vector2.zero, 0f, false, player.GameObject, Team.Player));
                }
                finally { state.Busy = false; }
            };
            state.Handler = h;
            player.Hooks.HitDealt += h;
        }

        public override void OnUnequip(IPlayerContext player)
        {
            var rt = PassiveRuntime.For(player, false);
            if (rt == null || !rt.TryGet(this, out var state)) return;
            if (--state.RefCount > 0) return;
            if (state.Handler is Action<HitReport> h) player.Hooks.HitDealt -= h;
            rt.Remove(this);
        }
    }
}
