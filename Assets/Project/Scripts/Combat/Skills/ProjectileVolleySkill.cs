using DungeonGathering.Catalog;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>Kỹ năng chủ động phóng chùm đạn theo hình quạt (Ví dụ: Mưa Tên).</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Skills/Projectile Volley", fileName = "Skill_ProjectileVolley")]
    public class ProjectileVolleySkill : ActiveSkillSO
    {
        [Tooltip("Prefab đạn (cần component Projectile); null = skill không bắn gì.")] public GameObject projectilePrefab;
        [Tooltip("VFX khi đạn trúng mục tiêu; null = không có.")] public GameObject impactVfx;
        [Tooltip("Hệ số sát thương mỗi viên × AttackDamage của Player (có thể chí mạng).")] public float damage = 14f;
        [Tooltip("Tốc độ bay của đạn (world-unit/giây).")] public float speed = 13f;
        [Tooltip("Lực đẩy lùi quái trúng đạn (0 = không đẩy).")] public float knockback = 2.5f;
        [Min(1)] public int count = 5;
        [Tooltip("Tổng góc tỏa của chùm đạn (độ), chia đều theo count; bỏ qua khi count = 1.")] public float spreadAngle = 35f;
        [Tooltip("Số quái đạn xuyên qua thêm trước khi biến mất (0 = dừng ở mục tiêu đầu tiên).")] public int pierce = 1;
        [Tooltip("Hiệu ứng trạng thái áp lên quái trúng đạn; để trống = không áp.")] public StatusPayload status;

        protected override SfxId DefaultActivateSfx => SfxId.BowShot;

        public override void Activate(IPlayerContext ctx)
        {
            if (ctx == null || ctx.IsDead || projectilePrefab == null || Services.Pool == null) return;
            Vector2 aim = ctx.AimDirection;
            if (aim.sqrMagnitude < 0.0001f) aim = Vector2.right;
            aim.Normalize();

            float baseAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            float baseDmg = damage * Mathf.Max(0f, ctx.GetStat(StatType.AttackDamage));
            float critChance = ctx.GetStat(StatType.CritChance);
            float critMult = ctx.GetStat(StatType.CritDamage);
            if (critMult <= 0f) critMult = WeaponBase.CritMultiplier;

            int shots = Mathf.Max(1, count);
            for (int i = 0; i < shots; i++)
            {
                float a = shots == 1
                    ? baseAngle
                    : baseAngle - spreadAngle * 0.5f + spreadAngle * i / (shots - 1);
                Vector2 dir = new(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                var go = Services.Pool.Get(projectilePrefab, ctx.Transform.position, Quaternion.identity);
                if (go == null) continue;
                if (!go.TryGetComponent<Projectile>(out var p)) { Services.Pool.Return(go); continue; }
                float dmg = CombatUtil.RollDamage(baseDmg, critChance, critMult, out bool crit);
                p.Launch(new ProjectileSpec
                {
                    Damage = dmg,
                    Speed = speed,
                    Knockback = knockback,
                    Crit = crit,
                    Pierce = pierce,
                    Team = Team.Player,
                    ImpactVfx = impactVfx,
                    Source = ctx.GameObject,
                    Owner = ctx,
                    Status = status,
                }, dir);
            }
        }
    }
}
