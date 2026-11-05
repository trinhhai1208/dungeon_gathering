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
        public float damage = 30f;
        public float radius = 3f;
        public float knockback = 6f;
        public bool centerOnNearestEnemy;
        public float targetRange = 8f;
        [Tooltip("Số lần đánh (vd sàn lửa đánh nhiều nhịp)")] [Min(1)] public int pulses = 1;
        public float pulseInterval = 0.5f;
        public StatusPayload status;
        public GameObject vfx;

        protected override SfxId DefaultActivateSfx => SfxId.SkillBurst;

        public override void Activate(IPlayerContext ctx)
        {
            Vector2 center = ctx.Transform.position;
            if (centerOnNearestEnemy)
            {
                var t = CombatUtil.FindNearest(center, targetRange, CombatLayers.EnemyTargets, CombatLayers.Walls);
                if (t != null) center = t.transform.position;
            }
            else
            {
                // Căn chỉnh tâm VFX nổ sát mặt đất/chân nhân vật (thay vì lơ lửng ngang ngực/cổ)
                center.y -= 0.35f;
            }
            Run(ctx, center);
        }

        async void Run(IPlayerContext ctx, Vector2 center)
        {
            var fxGo = CombatFx.Play(vfx, center, Quaternion.identity, null, Mathf.Max(1f, pulses * pulseInterval));
            if (fxGo != null)
            {
                foreach (var sr in fxGo.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    sr.sortingLayerName = "Characters";
                    sr.sortingOrder = 15;
                }
            }
            for (int i = 0; i < pulses; i++)
            {
                // Dừng khi Player bị hủy / tắt / chết (không đánh tiếp sau khi rời phòng hay kết thúc run).
                if (!PassiveRuntime.IsAlive(ctx)) return;
                float dmg = damage * ctx.GetStat(StatType.AttackDamage);
                var hits = CombatUtil.RentList();
                try
                {
                    CombatUtil.DamageArea(center, radius, CombatLayers.EnemyTargets,
                        new DamageInfo(dmg, knockback, Vector2.zero, 0f, false, ctx.GameObject, Team.Player), hits);
                    for (int k = 0; k < hits.Count; k++)
                    {
                        var h = hits[k];
                        var go = (h as Component)?.gameObject;
                        status.ApplyTo(go, ctx.GameObject);
                        ctx.Hooks.RaiseHit(new HitReport { Target = go, Point = center, Damage = dmg, Killed = h.IsDead });
                    }
                }
                finally { CombatUtil.ReturnList(hits); }
                if (i < pulses - 1 && !await PassiveRuntime.WaitWhileAlive(ctx, pulseInterval)) return;
            }
        }
    }
}
