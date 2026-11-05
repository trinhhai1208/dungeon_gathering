using DungeonGathering.Combat;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Enemies
{
    /// <summary>Hiện đường cảnh báo đỏ rồi lao nhanh về phía Player, gây sát thương khi chạm.</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Enemy Attacks/Charge", fileName = "Atk_Charge")]
    public class ChargeAttackSO : EnemyAttackSO
    {
        public float damage = 1f;
        public float distance = 5f;
        public float chargeSpeed = 14f;
        public float hitRadius = 0.6f;
        public float knockback = 6f;
        [Tooltip("Prefab cảnh báo (sprite đường đỏ, pivot ở đầu trái, dài 1 đơn vị) — scale X theo distance")]
        public GameObject warningPrefab;

        public override async Awaitable Execute(EnemyAttackContext ctx)
        {
            Vector2 dir = ctx.DirToTarget;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            ctx.Self.Anim?.Face(dir.x);
            GameObject warn = null;
            if (warningPrefab != null && Services.Pool != null)
            {
                warn = Services.Pool.Get(warningPrefab, ctx.Origin, Quaternion.Euler(0f, 0f, angle));
                if (warn != null) warn.transform.localScale = new Vector3(distance, 1f, 1f);
            }
            try { await Wait(windupTime, ctx.Token); }
            finally { if (warn != null) Services.Pool?.Return(warn); }
            if (ctx.Self.IsDead) return;

            ctx.Self.Anim?.Play(AnimKey.Attack, true);
            var hit = false;
            float travelled = 0f;
            var motor = ctx.Self.Motor;
            while (travelled < distance && !ctx.Self.IsDead)
            {
                float step = chargeSpeed * Time.fixedDeltaTime;
                motor.SetVelocity(dir * chargeSpeed);
                travelled += step;
                if (!hit)
                {
                    int n = CombatUtil.DamageArea(ctx.Self.transform.position, hitRadius, CombatLayers.PlayerTargets,
                        new DamageInfo(damage, knockback, dir, 0f, false, ctx.Self.gameObject, Team.Enemy));
                    hit = n > 0;
                }
                if (motor.HitWallLastStep) break;
                await Awaitable.FixedUpdateAsync(ctx.Token);
            }
            motor.SetVelocity(Vector2.zero);
        }
    }
}
