using DungeonGathering.Catalog;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>Hút máu: tích lũy % sát thương, đủ 1 HP thì hồi. Phần tích lũy nằm trong PassiveState (theo Player).</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Passives/Weapon Lifesteal", fileName = "WP_Lifesteal")]
    public class LifestealPassive : HookWeaponPassive
    {
        [Range(0, 1)] public float percent = 0.04f;
        public float damagePerHp = 25f; // HP của Player nhỏ (kiểu Soul Knight): quy đổi sát thương → HP
        public GameObject vfx;

        protected override void OnEquipped(IPlayerContext p, PassiveState state) => state.Accum = 0f;

        protected override void OnHit(IPlayerContext player, PassiveState state, in HitReport hit)
        {
            state.Accum += hit.Damage * percent / Mathf.Max(1f, damagePerHp) * 10f;
            if (state.Accum < 1f) return;
            int hp = Mathf.FloorToInt(state.Accum);
            state.Accum -= hp;
            player.Heal(hp);
            if (vfx != null) CombatFx.Play(vfx, player.Transform.position, Quaternion.identity, player.Transform);
        }
    }
}
