using DungeonGathering.Catalog;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>
    /// Đòn diện rộng: Hỏa Trận, Băng Trận, Lôi Giáng, Bổ Trảm... Tâm = bản thân hoặc quái gần nhất.
    /// Sát thương = damage × AttackDamage của Player.
    /// </summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Skills/Area Burst", fileName = "Skill_AreaBurst")]
    public class AreaBurstSkill : ActiveSkillSO
    {
        [Tooltip("Hệ số sát thương mỗi nhịp, nhân với AttackDamage của Player (1 = 100%).")] public float damage = 30f;
        [Tooltip("Bán kính vùng nổ (world-unit) quanh tâm.")] public float radius = 3f;
        [Tooltip("Lực đẩy lùi quái ra xa tâm nổ (0 = không đẩy).")] public float knockback = 6f;
        [Tooltip("Bật: đặt tâm vào quái gần nhất trong targetRange (dò lại mỗi nhịp); tắt hoặc không có quái: tâm quanh Player / hướng ngắm.")] public bool centerOnNearestEnemy;
        [Tooltip("Bật cho skill vòng bảo vệ quanh người: các nhịp liên tiếp bám theo vị trí Player.")] public bool followPlayer;
        [Tooltip("Tầm dò quái gần nhất (world-unit), chỉ dùng khi bật centerOnNearestEnemy.")] public float targetRange = 8f;
        [Tooltip("Số lần đánh (vd sàn lửa đánh nhiều nhịp)")] [Min(1)] public int pulses = 1;
        [Tooltip("Khoảng cách giữa các nhịp đánh (giây), chỉ có tác dụng khi pulses > 1.")] public float pulseInterval = 0.5f;
        [Tooltip("Hiệu ứng trạng thái áp lên mỗi quái trúng đòn (mỗi nhịp); để trống = không áp.")] public StatusPayload status;
        [Tooltip("VFX phát tại tâm mỗi nhịp; null = không có hiệu ứng hình.")] public GameObject vfx;

        protected override SfxId DefaultActivateSfx => SfxId.SkillBurst;

        public override void Activate(IPlayerContext ctx)
        {
            Vector2 center = centerOnNearestEnemy ? ResolveEnemyCenter(ctx) : (Vector2)ctx.Transform.position;
            Run(ctx, center);
        }

        Vector2 ResolveEnemyCenter(IPlayerContext ctx)
        {
            Vector2 origin = ctx.Transform.position;
            var t = CombatUtil.FindNearest(origin, targetRange, CombatLayers.EnemyTargets, CombatLayers.Walls, requireLineOfSight: true)
                    ?? CombatUtil.FindNearest(origin, targetRange, CombatLayers.EnemyTargets, 0, requireLineOfSight: false);
            if (t != null) return t.transform.position;
            Vector2 aim = ctx.AimDirection.sqrMagnitude > 0.01f ? ctx.AimDirection.normalized : Vector2.right;
            return origin + aim * 2.5f;
        }

        async void Run(IPlayerContext ctx, Vector2 initialCenter)
        {
            bool loopAttached = false;
            for (int i = 0; i < pulses; i++)
            {
                // Dừng khi Player bị hủy / tắt / chết (không đánh tiếp sau khi rời phòng hay kết thúc run).
                if (!PassiveRuntime.IsAlive(ctx)) return;

                Vector2 strikeCenter = initialCenter;
                if (centerOnNearestEnemy)
                    strikeCenter = ResolveEnemyCenter(ctx);
                else if (followPlayer)
                    strikeCenter = ctx.Transform.position;

                if (vfx != null)
                {
                    if (followPlayer && vfx.GetComponent<VFX.AutoDestroyEffect>() == null)
                    {
                        if (!loopAttached)
                        {
                            float totalDur = Mathf.Max(0.6f, pulses * pulseInterval);
                            CombatFx.AttachScaled(vfx, ctx.Transform, overhead: false, fallbackDuration: totalDur, minScale: 1f, maxScale: 1.4f);
                            loopAttached = true;
                        }
                    }
                    else
                    {
                        CombatFx.Play(vfx, strikeCenter, Quaternion.identity, null, Mathf.Max(0.5f, pulseInterval));
                    }
                }

                float dmg = damage * ctx.GetStat(StatType.AttackDamage);
                var hits = CombatUtil.RentList();
                try
                {
                    CombatUtil.DamageArea(strikeCenter, radius, CombatLayers.EnemyTargets,
                        new DamageInfo(dmg, knockback, Vector2.zero, 0f, false, ctx.GameObject, Team.Player), hits);
                    for (int k = 0; k < hits.Count; k++)
                    {
                        var h = hits[k];
                        var go = (h as Component)?.gameObject;
                        status.ApplyTo(go, ctx.GameObject);
                        ctx.Hooks.RaiseHit(new HitReport { Target = go, Point = strikeCenter, Damage = dmg, Killed = h.IsDead });
                    }
                }
                finally { CombatUtil.ReturnList(hits); }
                if (i < pulses - 1 && !await PassiveRuntime.WaitWhileAlive(ctx, pulseInterval)) return;
            }
        }
    }
}
