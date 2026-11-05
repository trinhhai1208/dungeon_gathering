using DungeonGathering.Combat;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Enemies
{
    /// <summary>Bắn đạn: đơn, chùm (count + spreadAngle), liên thanh (bursts), tỏa tròn 360 (radial).</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Enemy Attacks/Shoot", fileName = "Atk_Shoot")]
    public class ShootAttackSO : EnemyAttackSO
    {
        public GameObject projectilePrefab;
        public GameObject impactVfx;
        public float damage = 1f;
        public float speed = 6f;
        public float knockback = 2f;
        public float lifetime = 4f;
        [Min(1)] public int count = 1;
        [Tooltip("Tổng góc tỏa của chùm (độ)")] public float spreadAngle = 30f;
        public bool radial;
        [Min(1)] public int bursts = 1;
        public float burstInterval = 0.15f;
        [Tooltip("Xoay thêm mỗi loạt khi radial (tạo xoắn ốc)")] public float radialSpin = 10f;

        public override async Awaitable Execute(EnemyAttackContext ctx)
        {
            ctx.Self.Anim?.Face(ctx.DirToTarget.x);
            ctx.Self.Anim?.Play(AnimKey.Attack, true);
            await Wait(windupTime, ctx.Token);
            for (int b = 0; b < bursts; b++)
            {
                if (ctx.Self.IsDead) return;
                Fire(ctx, b);
                if (b < bursts - 1) await Wait(burstInterval, ctx.Token);
            }
        }

        void Fire(EnemyAttackContext ctx, int burstIndex)
        {
            if (projectilePrefab == null || Services.Pool == null) return;
            Vector2 aim = ctx.DirToTarget;
            float baseAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            Sfx.PlayAt(SfxId.EnemyShot, ctx.Origin);
            for (int i = 0; i < count; i++)
            {
                float a;
                if (radial) a = baseAngle + burstIndex * radialSpin + 360f * i / count;
                else a = count == 1 ? baseAngle : baseAngle - spreadAngle * 0.5f + spreadAngle * i / (count - 1);
                Vector2 dir = new(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                var go = Services.Pool.Get(projectilePrefab, ctx.Origin, Quaternion.identity);
                if (go == null) continue;
                if (!go.TryGetComponent<Projectile>(out var p)) { Services.Pool.Return(go); continue; }
                p.Launch(new ProjectileSpec
                {
                    Damage = damage, Speed = speed, Knockback = knockback, Lifetime = lifetime,
                    Team = Team.Enemy, ImpactVfx = impactVfx, Source = ctx.Self.gameObject,
                }, dir);
            }
        }
    }
}
