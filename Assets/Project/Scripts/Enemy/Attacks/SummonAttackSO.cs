using DungeonGathering.Combat;
using UnityEngine;

namespace DungeonGathering.Enemies
{
    /// <summary>Triệu hồi quái con. Quái con đăng ký vào phòng qua Enemy.SpawnContext.Summon (vào tập alive của RoomController).</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Enemy Attacks/Summon", fileName = "Atk_Summon")]
    public class SummonAttackSO : EnemyAttackSO
    {
        [Tooltip("Định nghĩa quái con (sinh bằng prefab nền dùng chung)")]
        public MonsterDefinitionSO minion;
        [Min(1)] public int count = 2;
        [Tooltip("Khoảng cách từ quái tới điểm sinh quái con (world-unit, tối thiểu 0.25); kẹt tường thì sinh tại chỗ.")] public float radius = 1.5f;
        [Tooltip("Số quái con tối đa còn sống cùng lúc do con này gọi")] public int maxAlive = 6;

        public override bool CanUse(Enemy self, ITargetable target, float distance)
            => minion != null && self.SummonCount < maxAlive && distance <= range;

        public override async Awaitable Execute(EnemyAttackContext ctx)
        {
            ctx.Self.Anim?.Play(AnimKey.Attack, true);
            await Wait(windupTime, ctx.Token);
            if (ctx.Self.IsDead) return;
            for (int i = 0; i < count && ctx.Self.SummonCount < maxAlive; i++)
            {
                Vector2 pos = PickSpawnPoint(ctx.Origin);
                ctx.Self.SpawnMinion(minion, pos);
            }
        }

        Vector2 PickSpawnPoint(Vector2 origin)
        {
            float r = Mathf.Max(0.25f, radius);
            for (int i = 0; i < 8; i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                Vector2 candidate = origin + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (Physics2D.OverlapCircle(candidate, 0.35f, CombatLayers.Walls) != null) continue;
                if (Physics2D.Linecast(origin, candidate, CombatLayers.Walls).collider != null) continue;
                return candidate;
            }
            return origin;
        }
    }
}
