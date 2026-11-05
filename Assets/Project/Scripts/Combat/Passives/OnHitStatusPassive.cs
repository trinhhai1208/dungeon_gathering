using DungeonGathering.Catalog;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>Trúng đòn có xác suất gây làm chậm / thiêu đốt / độc / xuất huyết / choáng.</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Passives/Weapon On-Hit Status", fileName = "WP_OnHitStatus")]
    public class OnHitStatusPassive : HookWeaponPassive
    {
        [Range(0, 1)] public float chance = 1f;
        public StatusPayload status;
        public GameObject hitVfx;

        protected override void OnHit(IPlayerContext player, PassiveState state, in HitReport hit)
        {
            if (hit.Target == null || hit.Killed || UnityEngine.Random.value > chance) return;
            status.ApplyTo(hit.Target, player.GameObject);
            if (hitVfx != null) CombatFx.Play(hitVfx, hit.Target.transform.position, Quaternion.identity);
        }
    }
}
