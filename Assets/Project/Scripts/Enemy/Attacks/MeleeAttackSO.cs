using DungeonGathering.Combat;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Enemies
{
    /// <summary>Áp sát cắn/chém: windup rồi gây sát thương trong vòng tròn trước mặt.</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Enemy Attacks/Melee", fileName = "Atk_Melee")]
    public class MeleeAttackSO : EnemyAttackSO
    {
        [Tooltip("Sát thương mỗi đòn (HP) × AttackDamage của quái (đã gồm hệ số bậc).")] public float damage = 1f;
        [Tooltip("Bán kính vùng chém (world-unit), tâm đặt trước mặt quái 0.8 × hitRadius.")] public float hitRadius = 0.8f;
        [Tooltip("Lực đẩy Player khi trúng đòn (0 = không đẩy).")] public float knockback = 4f;
        [Tooltip("VFX vệt chém xoay theo hướng đánh, tự thu nhỏ theo hitRadius; null = không có.")] public GameObject slashVfx;

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
            var hits = CombatUtil.RentList();
            try
            {
                CombatUtil.DamageArea(center, hitRadius, CombatLayers.PlayerTargets,
                    new DamageInfo(ctx.Self.ScaleDamage(damage), knockback, dir, 0f, false, ctx.Self.gameObject, Team.Enemy), hits);
                if (!status.IsEmpty)
                {
                    for (int i = 0; i < hits.Count; i++)
                        status.ApplyTo(hits[i], ctx.Self.gameObject);
                }
            }
            finally { CombatUtil.ReturnList(hits); }
        }
    }
}
