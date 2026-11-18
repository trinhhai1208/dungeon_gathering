using DungeonGathering.Catalog;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>Kỹ năng chủ động triệu hồi đệ tử chiến đấu và thu hút quái.</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Skills/Summon Companion", fileName = "Skill_SummonCompanion")]
    public class SummonCompanionSkill : ActiveSkillSO
    {
        [Tooltip("Sprite của đệ tử; null = mượn hình của hero (mờ, ánh tím) — nên gán sprite riêng.")] public Sprite companionSprite;
        [Min(1)] public int count = 2;
        [Tooltip("Máu của mỗi đệ tử (HP tuyệt đối, tối thiểu 1).")] public float companionHp = 35f;
        [Tooltip("Sát thương mỗi đòn của đệ tử × AttackDamage của Player (tối thiểu ×0.5), đánh mỗi 0.7 giây.")] public float companionDamage = 10f;
        [Tooltip("Tốc độ di chuyển của đệ tử (world-unit/giây, tối thiểu 0.5).")] public float moveSpeed = 3.8f;
        [Tooltip("Thời gian tồn tại của đệ tử (giây); ≤ 0 = dùng duration của skill, nếu cũng 0 thì 12 giây.")] public float summonDuration = 12f;
        [Tooltip("Bán kính khiêu khích (world-unit): quái trong vùng chuyển mục tiêu sang đệ tử; 0 = không khiêu khích.")] public float tauntRadius = 5f;
        [Tooltip("VFX phát tại vị trí xuất hiện của mỗi đệ tử; null = không có.")] public GameObject summonVfx;

        protected override SfxId DefaultActivateSfx => SfxId.Buff;

        public override void Activate(IPlayerContext ctx)
        {
            if (ctx == null || ctx.IsDead) return;
            float dur = summonDuration > 0f ? summonDuration : (duration > 0f ? duration : 12f);
            float dmg = companionDamage * Mathf.Max(0.5f, ctx.GetStat(StatType.AttackDamage));
            int n = Mathf.Max(1, count);
            Vector2 origin = ctx.Transform.position;
            for (int i = 0; i < n; i++)
            {
                float angle = (360f * i / n) * Mathf.Deg2Rad;
                Vector2 dir = new(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 spawn2D = origin + dir * 1.1f;
                var wallHit = Physics2D.Raycast(origin, dir, 1.35f, CombatLayers.Walls);
                if (wallHit.collider != null)
                    spawn2D = wallHit.distance > 0.35f ? origin + dir * (wallHit.distance - 0.3f) : origin;
                Vector3 pos = spawn2D;
                CombatFx.Play(summonVfx, pos, Quaternion.identity);
                var go = new GameObject($"Companion_{skillId}_{i}");
                go.transform.position = pos;
                var actor = go.AddComponent<CompanionActor>();
                actor.Init(ctx, companionSprite, companionHp, dmg, moveSpeed, dur, tauntRadius);
            }
        }
    }
}
