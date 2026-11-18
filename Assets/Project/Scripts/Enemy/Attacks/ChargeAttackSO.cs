using DungeonGathering.Combat;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Enemies
{
    /// <summary>Hiện đường cảnh báo đỏ rồi lao nhanh về phía Player, gây sát thương khi chạm.</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Enemy Attacks/Charge", fileName = "Atk_Charge")]
    public class ChargeAttackSO : EnemyAttackSO
    {
        [Tooltip("Sát thương khi lao trúng (HP) × AttackDamage của quái; chỉ gây 1 lần mỗi cú lao.")] public float damage = 1f;
        [Tooltip("Quãng đường lao tối đa (world-unit); cũng là độ dài vạch cảnh báo; dừng sớm nếu đụng tường.")] public float distance = 5f;
        [Tooltip("Tốc độ lao (world-unit/giây, tối thiểu 0.1).")] public float chargeSpeed = 14f;
        [Tooltip("Bán kính va chạm quanh quái khi đang lao (world-unit).")] public float hitRadius = 0.6f;
        [Tooltip("Lực đẩy Player theo hướng lao (0 = không đẩy).")] public float knockback = 6f;
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
            else if (windupTime > 0f)
            {
                ctx.Self.Anim?.Flash(new Color(1f, 0.25f, 0.2f), Mathf.Clamp(windupTime, 0.1f, 0.4f));
            }
            try { await Wait(windupTime, ctx.Token); }
            finally { if (warn != null) Services.Pool?.Return(warn); }
            if (ctx.Self.IsDead) return;

            ctx.Self.Anim?.Play(AnimKey.Attack, true);
            var hit = false;
            float travelled = 0f;
            var motor = ctx.Self.Motor;
            if (motor == null) return;
            try
            {
                // chargeSpeed <= 0 (cấu hình sai) → travelled không tăng → lặp vô hạn: chặn dưới.
                float speed = Mathf.Max(0.1f, chargeSpeed);
                while (travelled < distance && !ctx.Self.IsDead)
                {
                    float step = speed * Time.fixedDeltaTime;
                    // Chỉ dừng khi tường chắn ĐÚNG hướng lao; trượt dọc tường bên sườn vẫn lao tiếp.
                    if (motor.IsBlockedToward(dir, step + 0.02f)) break;
                    motor.SetVelocity(dir * speed);
                    travelled += step;
                    if (!hit)
                    {
                        var hits = CombatUtil.RentList();
                        try
                        {
                            int n = CombatUtil.DamageArea(ctx.Self.transform.position, hitRadius, CombatLayers.PlayerTargets,
                                new DamageInfo(ctx.Self.ScaleDamage(damage), knockback, dir, 0f, false, ctx.Self.gameObject, Team.Enemy), hits);
                            hit = n > 0;
                            if (hit && !status.IsEmpty)
                            {
                                for (int i = 0; i < hits.Count; i++)
                                    status.ApplyTo(hits[i], ctx.Self.gameObject);
                            }
                        }
                        finally { CombatUtil.ReturnList(hits); }
                    }
                    await Awaitable.FixedUpdateAsync(ctx.Token);
                }
            }
            finally
            {
                motor.SetVelocity(Vector2.zero);
            }
        }
    }
}
