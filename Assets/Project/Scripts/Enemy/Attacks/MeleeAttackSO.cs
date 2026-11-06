using DungeonGathering.Combat;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Enemies
{
    /// <summary>Áp sát cắn/chém: windup rồi gây sát thương trong vòng tròn trước mặt.</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Enemy Attacks/Melee", fileName = "Atk_Melee")]
    public class MeleeAttackSO : EnemyAttackSO
    {
        public float damage = 1f;
        public float hitRadius = 0.8f;
        public float knockback = 4f;
        public GameObject slashVfx;

        public override async Awaitable Execute(EnemyAttackContext ctx)
        {
            Vector2 dir = ctx.DirToTarget;
            ctx.Self.Anim?.Face(dir.x);
            ctx.Self.Anim?.Play(AnimKey.Attack, true);
            await Wait(windupTime, ctx.Token);
            if (ctx.Self.IsDead) return;
            Vector2 center = ctx.Origin + dir * (hitRadius * 0.8f);
            var fx = CombatFx.Play(slashVfx, center, Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg));
            if (fx != null)
            {
                // Thu nhỏ vệt chém khớp với bán kính đòn đánh (hitRadius), tránh VFX to khổng lồ
                float s = Mathf.Clamp(hitRadius * 2f / 4.5f, 0.25f, 0.45f);
                fx.transform.localScale = new Vector3(s, s, 1f);
            }
            CombatUtil.DamageArea(center, hitRadius, CombatLayers.PlayerTargets,
                new DamageInfo(damage, knockback, dir, 0f, false, ctx.Self.gameObject, Team.Enemy));
        }
    }
}
