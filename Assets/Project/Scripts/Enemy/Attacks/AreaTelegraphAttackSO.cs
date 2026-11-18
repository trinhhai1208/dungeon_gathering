using System.Collections.Generic;
using DungeonGathering.Combat;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Enemies
{
    /// <summary>Hiện vòng cảnh báo dưới chân mục tiêu rồi phát nổ gây sát thương vùng sau windupTime.</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Enemy Attacks/Area Telegraph", fileName = "Atk_AreaTelegraph")]
    public class AreaTelegraphAttackSO : EnemyAttackSO
    {
        [Tooltip("Sát thương mỗi vùng nổ (HP) × AttackDamage của quái (đã gồm hệ số bậc).")] public float damage = 2f;
        [Tooltip("Bán kính mỗi vùng nổ (world-unit); vòng cảnh báo được scale theo đường kính này.")] public float radius = 1.8f;
        [Tooltip("Lực đẩy Player ra xa tâm vùng nổ (0 = không đẩy).")] public float knockback = 5f;
        [Min(1)] public int zonesCount = 1;
        [Tooltip("Độ tản của các vùng phụ quanh Player (world-unit, ngẫu nhiên 0.8 đến giá trị này); chỉ dùng khi zonesCount > 1.")] public float zoneScatterRadius = 2.5f;
        [Tooltip("Prefab vòng cảnh báo hiện trong windupTime tại mỗi vùng; null = nổ không báo trước.")] public GameObject warningPrefab;
        [Tooltip("VFX phát tại mỗi vùng khi nổ; null = không có.")] public GameObject explosionVfx;

        public override async Awaitable Execute(EnemyAttackContext ctx)
        {
            ctx.Self.Anim?.Face(ctx.DirToTarget.x);
            ctx.Self.Anim?.Play(AnimKey.Attack, true);

            int nZones = Mathf.Max(1, zonesCount);
            var points = new List<Vector2>(nZones) { ctx.TargetPos };
            for (int i = 1; i < nZones; i++)
            {
                float a = (360f * i / nZones + Random.Range(-25f, 25f)) * Mathf.Deg2Rad;
                Vector2 offset = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(0.8f, Mathf.Max(1f, zoneScatterRadius));
                points.Add(ctx.TargetPos + offset);
            }

            var warnings = new List<GameObject>(nZones);
            var origScales = new List<Vector3>(nZones);
            if (warningPrefab != null && Services.Pool != null)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    var w = Services.Pool.Get(warningPrefab, points[i], Quaternion.identity);
                    if (w != null)
                    {
                        origScales.Add(w.transform.localScale);
                        var sr = w.GetComponentInChildren<SpriteRenderer>();
                        float baseWidth = (sr != null && sr.sprite != null) ? sr.sprite.bounds.size.x : 1f;
                        float diameter = radius * 2f;
                        float scale = baseWidth > 0.01f ? diameter / baseWidth : diameter;
                        w.transform.localScale = Vector3.one * scale;
                        warnings.Add(w);
                    }
                }
            }

            try
            {
                await Wait(windupTime, ctx.Token);
            }
            finally
            {
                if (Services.Pool != null)
                {
                    for (int i = 0; i < warnings.Count; i++)
                    {
                        if (warnings[i] == null) continue;
                        warnings[i].transform.localScale = origScales[i];
                        Services.Pool.Return(warnings[i]);
                    }
                }
            }

            if (ctx.Self.IsDead) return;

            for (int i = 0; i < points.Count; i++)
            {
                Vector2 pt = points[i];
                CombatFx.Play(explosionVfx, pt, Quaternion.identity);
                Sfx.PlayAt(SfxId.Explosion, pt);
                var hits = CombatUtil.RentList();
                try
                {
                    CombatUtil.DamageArea(pt, radius, CombatLayers.PlayerTargets,
                        new DamageInfo(ctx.Self.ScaleDamage(damage), knockback, Vector2.zero, 0f, false, ctx.Self.gameObject, Team.Enemy), hits);
                    if (!status.IsEmpty)
                    {
                        for (int h = 0; h < hits.Count; h++)
                            status.ApplyTo(hits[h], ctx.Self.gameObject);
                    }
                }
                finally { CombatUtil.ReturnList(hits); }
            }
        }
    }
}
