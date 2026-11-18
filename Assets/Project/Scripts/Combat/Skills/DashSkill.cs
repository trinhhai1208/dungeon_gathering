using System.Collections.Generic;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>Lướt (có thể bất tử trong lúc lướt): Bóng Đêm, Blink.</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Skills/Dash", fileName = "Skill_Dash")]
    public class DashSkill : ActiveSkillSO
    {
        [Tooltip("Quãng đường lướt (world-unit) theo hướng di chuyển, không di chuyển thì theo hướng ngắm.")] public float distance = 4f;
        [Tooltip("Thời gian lướt (giây).")] public float dashDuration = 0.18f;
        [Tooltip("Bất tử trong lúc lướt (dashDuration + 0.1 giây).")] public bool invulnerable = true;
        [Tooltip("VFX phát tại điểm xuất phát khi lướt; null = không có.")] public GameObject vfx;

        [Header("Tùy chọn: Chém khi lướt & Cường hóa đòn kế")]
        [Tooltip("Hệ số sát thương chém khi lướt × AttackDamage của Player; 0 = không chém.")] public float slashDamage = 0f;
        [Tooltip("Bán kính vùng chém (world-unit), tâm ở giữa quãng lướt thực tế (dừng trước tường); quái sau tường không trúng.")] public float slashRadius = 1.1f;
        [Tooltip("Lực đẩy lùi quái trúng đòn chém (0 = không đẩy).")] public float knockback = 4f;
        [Tooltip("Hiệu ứng trạng thái áp lên quái trúng đòn chém; để trống = không áp.")] public StatusPayload status;
        [Tooltip("Hệ số sát thương cho đòn đánh thường kế tiếp sau khi lướt; ≤ 1 = không cường hóa.")] public float empowerNextMultiplier = 1f;
        [Tooltip("VFX vệt chém xoay theo hướng lướt, chỉ phát khi slashDamage > 0; null = không có.")] public GameObject slashVfx;

        protected override SfxId DefaultActivateSfx => SfxId.Dash;

        public override void Activate(IPlayerContext ctx)
        {
            Vector2 dir = ctx.MoveInput.sqrMagnitude > 0.01f ? ctx.MoveInput : ctx.AimDirection;
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;
            dir.Normalize();
            Vector2 start = ctx.Transform.position;
            CombatFx.Play(vfx, start, Quaternion.identity);
            if (invulnerable) InvulnFor(ctx, dashDuration + 0.1f);
            ctx.Dash(dir, distance, dashDuration);

            if (slashDamage > 0f)
            {
                // Tâm chém ở giữa quãng lướt THỰC TẾ: tường chặn giữa đường thì quãng lướt ngắn lại, không chém xuyên tường
                float reach = distance;
                var wall = Physics2D.Raycast(start, dir, distance, CombatLayers.Walls);
                if (wall.collider != null) reach = Mathf.Max(0f, wall.distance - 0.3f);
                Vector2 center = start + dir * (reach * 0.5f);
                if (slashVfx != null)
                {
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    CombatFx.Play(slashVfx, center, Quaternion.Euler(0f, 0f, angle));
                }
                float dmg = slashDamage * Mathf.Max(0f, ctx.GetStat(StatType.AttackDamage));
                var hits = CombatUtil.RentList();
                try
                {
                    CombatUtil.DamageArea(center, slashRadius, CombatLayers.EnemyTargets,
                        new DamageInfo(dmg, knockback, dir, 0f, false, ctx.GameObject, Team.Player), hits,
                        blockMask: CombatLayers.Walls);
                    for (int i = 0; i < hits.Count; i++)
                    {
                        var h = hits[i];
                        if (!status.IsEmpty) status.ApplyTo(h, ctx.GameObject);
                        ctx.Hooks?.RaiseHit(new HitReport
                        {
                            Target = (h as Component)?.gameObject,
                            Point = h.transform.position,
                            Direction = dir,
                            Damage = dmg,
                            Killed = h.IsDead,
                        });
                    }
                }
                finally { CombatUtil.ReturnList(hits); }
            }

            if (empowerNextMultiplier > 1f)
                ctx.QueueEmpoweredAttack(empowerNextMultiplier, 0);
        }

        static async void InvulnFor(IPlayerContext ctx, float t)
        {
            var key = new object();
            ctx.SetInvulnerable(key, true);
            // Hủy sớm khi Player bị Destroy (token gắn destroyCancellationToken + AppToken); gỡ bất tử nếu Player còn.
            await PassiveRuntime.WaitWhileAlive(ctx, t);
            if (ctx != null && ctx.GameObject != null) ctx.SetInvulnerable(key, false);
        }
    }
}
