using DungeonGathering.Catalog;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>Trúng đòn có xác suất gây làm chậm / thiêu đốt / độc / xuất huyết / choáng.</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Passives/Weapon On-Hit Status", fileName = "WP_OnHitStatus")]
    public class OnHitStatusPassive : HookWeaponPassive
    {
        [Range(0, 1)] public float chance = 1f;
        [Tooltip("Trạng thái gây lên mục tiêu còn sống khi trúng đòn và qua xác suất chance; rỗng = không gây gì.")]
        public StatusPayload status;
        [Tooltip("Prefab VFX phát tại mục tiêu mỗi lần gây trạng thái; null = không (VFX lặp của trạng thái vẫn hiện).")]
        public GameObject hitVfx;

        protected override void OnHit(IPlayerContext player, PassiveState state, in HitReport hit)
        {
            if (hit.Target == null || hit.Killed || chance <= 0f || UnityEngine.Random.value > chance) return;
            status.ApplyTo(hit.Target, player.GameObject);
            if (hitVfx != null) CombatFx.Play(hitVfx, hit.Target.transform.position, Quaternion.identity);
        }
    }
}
